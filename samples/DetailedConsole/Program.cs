using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

// Load configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

string url = configuration["MarkItDownServer:Url"] ?? "http://127.0.0.1:8490/process_file";
string filePath = configuration["MarkItDownServer:FilePath"] ?? "Benefit_Options.pdf";
int timeoutMinutes = int.Parse(configuration["MarkItDownServer:TimeoutMinutes"] ?? "5");

Console.WriteLine("===========================================");
Console.WriteLine("MarkItDown Server - Client Application");
Console.WriteLine("===========================================");
Console.WriteLine();

using var client = new HttpClient
{
    Timeout = TimeSpan.FromMinutes(timeoutMinutes)
};

try
{
    Console.WriteLine($"Server URL: {url}");
    Console.WriteLine($"File to process: {filePath}");
    Console.WriteLine();

    if (!File.Exists(filePath))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Error: File not found: {filePath}");
        Console.ResetColor();
        return 1;
    }

    var fileInfo = new FileInfo(filePath);
    Console.WriteLine($"File size: {fileInfo.Length / 1024.0:N2} KB");
    Console.WriteLine();

    Console.WriteLine("Uploading file to server...");
    
    using var content = new MultipartFormDataContent();
    byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
    var fileContent = new ByteArrayContent(fileBytes);
    
    // Set content type based on file extension
    string contentType = Path.GetExtension(filePath).ToLower() switch
    {
        ".pdf" => "application/pdf",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".doc" => "application/msword",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".xls" => "application/vnd.ms-excel",
        ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        ".ppt" => "application/vnd.ms-powerpoint",
        ".txt" => "text/plain",
        _ => "application/octet-stream"
    };
    
    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
    content.Add(fileContent, "file", Path.GetFileName(filePath));

    var response = await client.PostAsync(url, content);

    Console.WriteLine($"Response status: {(int)response.StatusCode} {response.StatusCode}");
    Console.WriteLine();

    if (response.IsSuccessStatusCode)
    {
        string responseBody = await response.Content.ReadAsStringAsync();
        
        try
        {
            var jsonResponse = JsonSerializer.Deserialize<MarkdownResponse>(responseBody);
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ File converted successfully!");
            Console.ResetColor();
            Console.WriteLine();
            Console.WriteLine("=== Markdown Content ===");
            Console.WriteLine(jsonResponse?.Markdown);
            Console.WriteLine();
            Console.WriteLine($"Content length: {jsonResponse?.Markdown?.Length ?? 0} characters");
            
            // Save to file
            string outputFile = Path.ChangeExtension(filePath, ".md");
            if (jsonResponse?.Markdown != null)
            {
                await File.WriteAllTextAsync(outputFile, jsonResponse.Markdown);
                Console.WriteLine($"✓ Markdown saved to: {outputFile}");
            }
        }
        catch (JsonException)
        {
            Console.WriteLine("Raw response:");
            Console.WriteLine(responseBody);
        }
    }
    else
    {
        string errorBody = await response.Content.ReadAsStringAsync();
        
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"✗ Error: {response.StatusCode}");
        Console.ResetColor();
        Console.WriteLine($"Details: {errorBody}");
        
        return 1;
    }
    
    return 0;
}
catch (HttpRequestException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"✗ Network error: {ex.Message}");
    Console.ResetColor();
    Console.WriteLine();
    Console.WriteLine("Please ensure the MarkItDown Server is running.");
    Console.WriteLine($"Expected URL: {url}");
    return 1;
}
catch (TaskCanceledException ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"✗ Request timeout: {ex.Message}");
    Console.ResetColor();
    Console.WriteLine($"The request took longer than {timeoutMinutes} minutes.");
    return 1;
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"✗ Unexpected error: {ex.Message}");
    Console.ResetColor();
    Console.WriteLine();
    Console.WriteLine("Stack trace:");
    Console.WriteLine(ex.StackTrace);
    return 1;
}

// Response model
record MarkdownResponse(string Markdown);
