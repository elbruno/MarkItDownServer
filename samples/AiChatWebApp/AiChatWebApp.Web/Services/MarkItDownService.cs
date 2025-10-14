using System.Net.Http.Headers;

namespace AiChatWebApp.Web.Services;

/// <summary>
/// Client service for interacting with the MarkItDown server API
/// </summary>
public class MarkItDownService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MarkItDownService> _logger;

    public MarkItDownService(HttpClient httpClient, ILogger<MarkItDownService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Converts a document to Markdown using the MarkItDown server
    /// </summary>
    /// <param name="fileStream">The document file stream</param>
    /// <param name="fileName">The name of the file</param>
    /// <returns>The converted Markdown text</returns>
    public async Task<string> ConvertToMarkdownAsync(Stream fileStream, string fileName)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(streamContent, "file", fileName);

            var response = await _httpClient.PostAsync("/process_file", content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<MarkdownResponse>();
            return result?.Markdown ?? throw new Exception("No markdown content returned");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting file {FileName} to markdown", fileName);
            throw;
        }
    }

    /// <summary>
    /// Checks if the MarkItDown service is healthy
    /// </summary>
    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/health");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking MarkItDown service health");
            return false;
        }
    }

    private class MarkdownResponse
    {
        public string Markdown { get; set; } = string.Empty;
    }
}
