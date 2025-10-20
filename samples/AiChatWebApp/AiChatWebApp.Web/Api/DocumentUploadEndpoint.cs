using AiChatWebApp.Web.Services;
using AiChatWebApp.Web.Services.Ingestion;

namespace AiChatWebApp.Web.Api;

public static class DocumentUploadEndpoint
{
    public static void MapDocumentUploadEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/upload", async (
            IFormFile file,
            IWebHostEnvironment env,
            MarkItDownService markItDownService,
            DataIngestor dataIngestor,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("DocumentUpload");
            try
            {
                // Validate file
                if (file == null || file.Length == 0)
                {
                    return Results.BadRequest(new { error = "No file provided" });
                }

                // Check file size (50MB limit)
                const long maxFileSize = 50 * 1024 * 1024;
                if (file.Length > maxFileSize)
                {
                    return Results.BadRequest(new { error = "File size exceeds 50MB limit" });
                }

                // Check file extension
                var allowedExtensions = new[] { 
                    // Document formats
                    ".pdf", ".docx", ".doc", ".pptx", ".ppt", ".xlsx", ".xls", ".txt", ".md", ".html",
                    // Image formats
                    ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp", ".svg",
                    // Audio formats
                    ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a", ".wma"
                };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    return Results.BadRequest(new { error = $"File type '{extension}' is not supported. Allowed types: {string.Join(", ", allowedExtensions)}" });
                }

                // Create uploads directory
                var uploadsDir = Path.Combine(env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsDir);

                // Save the uploaded file
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                logger.LogInformation("File uploaded: {FileName} ({Length} bytes)", file.FileName, file.Length);

                // Ingest the uploaded file using MarkItDown
                var uploadSource = new UploadedFileSource(
                    uploadsDir,
                    markItDownService,
                    loggerFactory.CreateLogger<UploadedFileSource>());

                await dataIngestor.IngestDataAsync(uploadSource);

                return Results.Ok(new
                {
                    fileName = file.FileName,
                    savedAs = fileName,
                    size = file.Length,
                    message = "File uploaded and processed successfully"
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error uploading file");
                return Results.Problem(
                    title: "Upload failed",
                    detail: ex.Message,
                    statusCode: 500);
            }
        })
        .DisableAntiforgery() // For API endpoint
        .WithName("UploadDocument");
    }
}
