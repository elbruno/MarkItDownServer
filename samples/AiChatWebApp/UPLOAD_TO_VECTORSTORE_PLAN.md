# Plan: Upload Document and Add to Vector Store

## Objective
When a file is uploaded via the FileUpload component, convert it to Markdown and immediately add it to the vector store for search and RAG capabilities.

## Current State Analysis

### What Works
- FileUpload.razor converts files to Markdown using MarkItDownService
- The component emits a tuple (FileName, Markdown) on successful conversion
- Chat.razor receives the event but doesn't properly ingest the document

### Issues to Fix
1. Chat.razor has undefined `source` variable in HandleFileUpload method
2. No direct ingestion of the markdown content into the vector store
3. Need to create a proper ingestion source for markdown content

## Implementation Plan

### Step 1: Create Markdown Ingestion Source
Create a new `MarkdownIngestionSource` class that:
- Accepts markdown content directly (not from file)
- Creates chunks from the markdown text
- Returns IngestedDocument and IngestedChunk objects

### Step 2: Fix HandleFileUpload Method
Update the HandleFileUpload method in Chat.razor to:
- Create a temporary IngestedDocument for the uploaded file
- Use MarkdownIngestionSource to chunk the markdown content
- Directly insert chunks into the vector store collections
- Show proper feedback to the user

### Step 3: Direct Vector Store Insertion
Since we already have the markdown content:
- Create chunks using TextChunker
- Create embeddings for each chunk
- Insert directly into the SQLite vector store collections
- Avoid saving to disk unnecessarily

## Detailed Implementation

### MarkdownIngestionSource
```csharp
public class MarkdownIngestionSource : IIngestionSource
{
    private readonly IngestedDocument _document;
    private readonly string _markdownContent;
    
    public MarkdownIngestionSource(string fileName, string markdownContent)
    {
        _document = new IngestedDocument
        {
            Key = Guid.CreateVersion7().ToString(),
            SourceId = $"Upload:{fileName}",
            DocumentId = fileName,
            DocumentVersion = DateTime.UtcNow.ToString("o")
        };
        _markdownContent = markdownContent;
    }
    
    // Implementation methods...
}
```

### Updated HandleFileUpload
```csharp
private async Task HandleFileUpload((string FileName, string Markdown) uploadResult)
{
    var (fileName, markdown) = uploadResult;
    
    // Create ingestion source from markdown
    var source = new MarkdownIngestionSource(fileName, markdown);
    
    // Ingest into vector store
    await dataIngestor.IngestDataAsync(source);
    
    // Show success message
    var systemMessage = new ChatMessage(ChatRole.Assistant, 
        $"Document '{fileName}' has been uploaded, converted to markdown, and indexed for search.");
    messages.Add(systemMessage);
    ChatMessageItem.NotifyChanged(systemMessage);
    await InvokeAsync(StateHasChanged);
}
```

## Benefits

1. **Immediate Availability**: Documents are searchable immediately after upload
2. **No Disk I/O**: Markdown content is processed in-memory
3. **Clean Architecture**: Separation of concerns between conversion and ingestion
4. **Consistent with Existing Pattern**: Uses the same IIngestionSource interface

## Testing Plan

1. Upload a PDF document
2. Verify markdown conversion works
3. Confirm document appears in vector store
4. Test search functionality with uploaded document
5. Verify citations reference the uploaded document

## Future Enhancements

1. Show progress indicator during ingestion
2. Display chunk count to user
3. Add document preview capability
4. Support batch uploads
5. Allow document deletion from vector store
