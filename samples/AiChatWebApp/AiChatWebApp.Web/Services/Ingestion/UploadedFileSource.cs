using Microsoft.SemanticKernel.Text;

namespace AiChatWebApp.Web.Services.Ingestion;

/// <summary>
/// Ingestion source for files uploaded through the web interface.
/// Uses MarkItDown service to convert various file types to Markdown.
/// </summary>
public class UploadedFileSource(
    string sourceDirectory,
    MarkItDownService markItDownService,
    ILogger<UploadedFileSource> logger) : IIngestionSource
{
    public static string SourceFileId(string path) => Path.GetFileName(path);
    public static string SourceFileVersion(string path) => File.GetLastWriteTimeUtc(path).ToString("o");

    public string SourceId => $"{nameof(UploadedFileSource)}:{sourceDirectory}";

    public Task<IEnumerable<IngestedDocument>> GetNewOrModifiedDocumentsAsync(IReadOnlyList<IngestedDocument> existingDocuments)
    {
        var results = new List<IngestedDocument>();
        
        // Ensure directory exists
        if (!Directory.Exists(sourceDirectory))
        {
            Directory.CreateDirectory(sourceDirectory);
            return Task.FromResult((IEnumerable<IngestedDocument>)results);
        }

        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.*");
        var existingDocumentsById = existingDocuments.ToDictionary(d => d.DocumentId);

        foreach (var sourceFile in sourceFiles)
        {
            var sourceFileId = SourceFileId(sourceFile);
            var sourceFileVersion = SourceFileVersion(sourceFile);
            var existingDocumentVersion = existingDocumentsById.TryGetValue(sourceFileId, out var existingDocument) 
                ? existingDocument.DocumentVersion 
                : null;
            
            if (existingDocumentVersion != sourceFileVersion)
            {
                results.Add(new() 
                { 
                    Key = Guid.CreateVersion7().ToString(), 
                    SourceId = SourceId, 
                    DocumentId = sourceFileId, 
                    DocumentVersion = sourceFileVersion 
                });
            }
        }

        return Task.FromResult((IEnumerable<IngestedDocument>)results);
    }

    public Task<IEnumerable<IngestedDocument>> GetDeletedDocumentsAsync(IReadOnlyList<IngestedDocument> existingDocuments)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            return Task.FromResult(Enumerable.Empty<IngestedDocument>());
        }

        var currentFiles = Directory.GetFiles(sourceDirectory, "*.*");
        var currentFileIds = currentFiles.ToLookup(SourceFileId);
        var deletedDocuments = existingDocuments.Where(d => !currentFileIds.Contains(d.DocumentId));
        return Task.FromResult(deletedDocuments);
    }

    public async Task<IEnumerable<IngestedChunk>> CreateChunksForDocumentAsync(IngestedDocument document)
    {
        var filePath = Path.Combine(sourceDirectory, document.DocumentId);
        
        try
        {
            // Convert the file to Markdown using MarkItDown service
            using var fileStream = File.OpenRead(filePath);
            var markdownText = await markItDownService.ConvertToMarkdownAsync(fileStream, document.DocumentId);

            // Chunk the markdown text
#pragma warning disable SKEXP0050 // Type is for evaluation purposes only
            var chunks = TextChunker.SplitPlainTextParagraphs([markdownText], 500);
#pragma warning restore SKEXP0050 // Type is for evaluation purposes only

            return chunks.Select((text, index) => new IngestedChunk
            {
                Key = Guid.CreateVersion7().ToString(),
                DocumentId = document.DocumentId,
                PageNumber = 1, // We don't track pages for markdown
                Text = text,
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing document {DocumentId}", document.DocumentId);
            
            // Return a chunk with error information
            return new[]
            {
                new IngestedChunk
                {
                    Key = Guid.CreateVersion7().ToString(),
                    DocumentId = document.DocumentId,
                    PageNumber = 1,
                    Text = $"Error processing document: {ex.Message}",
                }
            };
        }
    }
}
