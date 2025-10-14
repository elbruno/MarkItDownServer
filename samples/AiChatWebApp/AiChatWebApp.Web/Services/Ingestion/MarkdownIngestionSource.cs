using Microsoft.SemanticKernel.Text;

namespace AiChatWebApp.Web.Services.Ingestion;

/// <summary>
/// Ingestion source for markdown content that's already been converted.
/// Used for directly ingesting uploaded files that have been converted to Markdown.
/// </summary>
public class MarkdownIngestionSource : IIngestionSource
{
    private readonly IngestedDocument _document;
    private readonly string _markdownContent;
    private bool _hasBeenProcessed = false;

    public MarkdownIngestionSource(string fileName, string markdownContent)
    {
        _document = new IngestedDocument
        {
            Key = Guid.CreateVersion7().ToString(),
            SourceId = $"{nameof(MarkdownIngestionSource)}:Upload",
            DocumentId = fileName,
            DocumentVersion = DateTime.UtcNow.ToString("o")
        };
        _markdownContent = markdownContent;
    }

    public string SourceId => _document.SourceId;

    public Task<IEnumerable<IngestedDocument>> GetNewOrModifiedDocumentsAsync(IReadOnlyList<IngestedDocument> existingDocuments)
    {
        // If we haven't processed this document yet, return it as new
        if (!_hasBeenProcessed)
        {
            return Task.FromResult<IEnumerable<IngestedDocument>>(new[] { _document });
        }

        // Otherwise, return empty
        return Task.FromResult(Enumerable.Empty<IngestedDocument>());
    }

    public Task<IEnumerable<IngestedDocument>> GetDeletedDocumentsAsync(IReadOnlyList<IngestedDocument> existingDocuments)
    {
        // This source doesn't delete documents
        return Task.FromResult(Enumerable.Empty<IngestedDocument>());
    }

    public Task<IEnumerable<IngestedChunk>> CreateChunksForDocumentAsync(IngestedDocument document)
    {
        // Mark as processed
        _hasBeenProcessed = true;

        // Chunk the markdown text
#pragma warning disable SKEXP0050 // Type is for evaluation purposes only
        var chunks = TextChunker.SplitPlainTextParagraphs([_markdownContent], 500);
#pragma warning restore SKEXP0050 // Type is for evaluation purposes only

        var ingestedChunks = chunks.Select((text, index) => new IngestedChunk
        {
            Key = Guid.CreateVersion7().ToString(),
            DocumentId = document.DocumentId,
            PageNumber = 1, // We don't track pages for markdown
            Text = text,
        });

        return Task.FromResult(ingestedChunks);
    }
}
