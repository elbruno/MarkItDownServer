using System.Net.Http.Headers;

HttpClient client = new HttpClient();

string url = "http://127.0.0.1:8490/process_file";

string filePath = "Benefit_Options.pdf";

using (var content = new MultipartFormDataContent())
{
    byte[] fileBytes = File.ReadAllBytes(filePath);
    var fileContent = new ByteArrayContent(fileBytes);
    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
    content.Add(fileContent, "file", Path.GetFileName(filePath));

    var response = await client.PostAsync(url, content);

    if (response.IsSuccessStatusCode)
    {
        string responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"MarkDown for {filePath}\n\n{responseBody}");
    }
    else
    {
        Console.WriteLine($"Error: {response.StatusCode}");
    }
}
