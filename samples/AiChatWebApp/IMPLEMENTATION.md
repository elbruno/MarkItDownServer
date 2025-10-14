# AI Chat Web App - Implementation Summary

This document provides a technical overview of the AI Chat Web App implementation, following the plan outlined in `plans/ai-chat-web-app-plan.md`.

## Overview

The AI Chat Web App is a .NET Aspire-orchestrated application that integrates:

- **Blazor Server** for the web UI
- **GitHub Models** for AI chat capabilities
- **MarkItDown Server** for document conversion
- **SQLite Vector Store** for semantic search
- **RAG (Retrieval-Augmented Generation)** for context-aware responses

## Implementation Components

### 1. Aspire Orchestration (`AiChatWebApp.AppHost`)

**File**: `AiChatWebApp.AppHost/AppHost.cs`

The AppHost configures:

- OpenAI connection string for GitHub Models
- The MarkItDown container and its HTTP endpoint
- Reference injection for the web application

```csharp
var openai = builder.AddConnectionString("openai");
var markitdown = builder.AddContainer("markitdownserver", "markitdownserver", "local")
   .WithHttpEndpoint(port: 8490, targetPort: 8490, name: "http");
var webApp = builder.AddProject<Projects.AiChatWebApp_Web>("aichatweb-app");
webApp.WithReference(openai);
webApp.WithEnvironment("MarkItDownServiceUrl", markitdown.GetEndpoint("http"));
```

### 2. MarkItDown Integration

#### MarkItDownService (`Services/MarkItDownService.cs`)

HTTP client wrapper for the MarkItDown API:

- Multipart form upload to `/process_file` endpoint
- JSON response parsing
- Health check support
- Error handling and logging

**Key Method**:

```csharp
public async Task<string> ConvertToMarkdownAsync(Stream fileStream, string fileName)
```

#### UploadedFileSource (`Services/Ingestion/UploadedFileSource.cs`)

Implements `IIngestionSource` interface for document ingestion:

- Monitors uploaded files in `wwwroot/uploads` directory
- Calls MarkItDown service for conversion
- Chunks Markdown content for vector storage
- Tracks document versions

**Key Methods**:

- `GetNewOrModifiedDocumentsAsync()` - Detects new/changed files
- `CreateChunksForDocumentAsync()` - Converts and chunks documents

### 3. Document Upload API

**File**: `Api/DocumentUploadEndpoint.cs`

RESTful endpoint at `/api/upload`:

- File validation (size, type)
- Temporary storage in `wwwroot/uploads`
- Triggers ingestion pipeline
- Returns upload status

**Supported File Types**:

- PDF, Word (DOC/DOCX), PowerPoint (PPT/PPTX)
- Excel (XLS/XLSX), Text (TXT), Markdown (MD), HTML

**Validation**:

- Max file size: 50MB
- Allowed extensions check
- Error handling with detailed messages

### 4. File Upload UI

#### FileUpload Component (`Components/Pages/Chat/FileUpload.razor`)

Blazor component features:

- Hidden file input with styled button
- Real-time upload progress
- Success/error status display
- Event callback on completion

**CSS** (`FileUpload.razor.css`):

- Modern, responsive design
- Loading spinner animation
- Status indicators (success/error)
- Hover effects

#### Integration with Chat (`Chat.razor`)

Added to chat interface:

- Positioned above chat suggestions
- Triggers document ingestion on upload
- Updates chat with system message on completion

### 5. Configuration

#### Application Settings

**AppHost/appsettings.json**:

```json
{
  "MarkItDownServiceUrl": "http://localhost:8490"
}
```

**Web/appsettings.json**:

```json
{
  "MarkItDownServiceUrl": "http://localhost:8490"
}
```

#### Service Registration (`Program.cs`)

```csharp
// MarkItDown service with HttpClient
builder.Services.AddHttpClient<MarkItDownService>(client =>
{
    client.BaseAddress = new Uri(markItDownServiceUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

// Upload endpoint
app.MapDocumentUploadEndpoint();
```

## Data Flow

### Document Upload Flow

```
1. User selects file in browser
   ↓
2. FileUpload.razor handles InputFile event
   ↓
3. File uploaded to /api/upload endpoint
   ↓
4. DocumentUploadEndpoint saves file to wwwroot/uploads
   ↓
5. UploadedFileSource triggered by DataIngestor
   ↓
6. File sent to MarkItDown service for conversion
   ↓
7. Markdown content chunked and embedded
   ↓
8. Chunks stored in SQLite vector database
   ↓
9. Document ready for chat queries
```

### Chat Query Flow

```
1. User asks question in chat
   ↓
2. SemanticSearch retrieves relevant chunks
   ↓
3. Context passed to GitHub Models (gpt-4o-mini)
   ↓
4. AI generates response with citations
   ↓
5. Response displayed in chat UI
```

## Architecture Decisions

### Why External MarkItDown Service?

- **Flexibility**: Works with any MarkItDown deployment (local, Docker, cloud)
- **Separation of Concerns**: Document conversion is independent
- **Scalability**: MarkItDown can scale independently
- **Development**: No SSL/certificate issues in CI/CD

### Why SQLite Vector Store?

- **Simplicity**: Single file, no external database needed
- **Performance**: Fast for small-to-medium datasets
- **Development**: Easy local development experience
- **Portability**: Database file can be backed up/restored easily

### Why Aspire?

- **Orchestration**: Manages multiple services (web app, MarkItDown)
- **Configuration**: Centralized config and secrets management
- **Monitoring**: Built-in dashboard and telemetry
- **Production Ready**: Easy path to cloud deployment

## Security Considerations

### Input Validation

- File size limits (50MB)
- File type restrictions
- Filename sanitization

### Storage

- Uploaded files in isolated directory
- `.gitignore` prevents committing sensitive files
- Cleanup mechanism for old files (consider adding scheduled task)

### API Security

- Anti-forgery disabled for API endpoint (consider adding API key auth)
- CORS configured in ServiceDefaults
- HTTPS enforced in production

## Performance Optimization

### Current Implementation

- Synchronous upload and processing
- Single-threaded document conversion
- In-memory stream handling

### Future Improvements

1. **Async Processing**: Queue uploads for background processing
2. **Caching**: Cache converted Markdown to avoid re-conversion
3. **Batch Processing**: Process multiple files in parallel
4. **Streaming**: Stream large files to avoid memory issues

## Testing Considerations

### Unit Tests (Recommended)

- `MarkItDownService` HTTP client mocking
- `UploadedFileSource` document processing logic
- Upload endpoint validation logic

### Integration Tests (Recommended)

- End-to-end upload flow
- MarkItDown service integration
- Vector store ingestion

### Manual Testing Checklist

- [ ] Upload various file types (PDF, Word, Excel, etc.)
- [ ] Test file size validation (>50MB should fail)
- [ ] Test unsupported file types
- [ ] Verify chat responses reference uploaded docs
- [ ] Test MarkItDown service unavailable scenario
- [ ] Test concurrent uploads

## Deployment Scenarios

### Local Development

```bash
# Terminal 1: MarkItDown server
python app.py

# Terminal 2: Aspire app
cd AiChatWebApp.AppHost
dotnet run
```

### Docker Compose

```yaml
services:
  markitdown:
    image: markitdownserver:latest
    ports:
      - "8490:8490"
  
  aichatweb:
    image: aichatweb:latest
    environment:
      - MarkItDownServiceUrl=http://markitdown:8490
    depends_on:
      - markitdown
```

### Azure Container Apps

- Deploy MarkItDown as separate container app
- Configure web app with MarkItDown URL
- Use managed identity for GitHub Models
- Store secrets in Key Vault

## Monitoring and Observability

### Built-in Metrics

- Aspire dashboard shows service health
- HTTP request telemetry
- Document processing metrics

### Logging

- `ILogger` used throughout
- Structured logging with context
- Log levels configurable per service

### Recommended Additions

1. Custom metrics for upload success/failure rate
2. Document processing duration tracking
3. Vector store query performance
4. MarkItDown service availability alerts

## Known Limitations

1. **File Size**: 50MB limit (configurable but affects memory)
2. **Concurrency**: No queue for uploads (consider adding)
3. **Storage**: Local file system (consider blob storage)
4. **Authentication**: No user isolation (files shared across users)
5. **Cleanup**: No automatic cleanup of old uploads

## Future Enhancements

### Short Term

- Add upload queue with background processing
- Implement file cleanup scheduler
- Add user authentication and file isolation
- Improve error messages and retry logic

### Medium Term

- Azure Blob Storage integration
- Document preview before upload
- Batch upload support
- Download original files

### Long Term

- Custom document processing pipelines
- Plugin system for different converters
- Real-time collaboration features
- Advanced analytics and insights

## References

- [Implementation Plan](../../plans/ai-chat-web-app-plan.md)
- [Quick Start Guide](QUICKSTART.md)
- [README](README.md)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [GitHub Models](https://github.com/marketplace/models)
- [MarkItDown Library](https://github.com/microsoft/markitdown)
