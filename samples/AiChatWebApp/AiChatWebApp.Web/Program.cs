using Microsoft.Extensions.AI;
using AiChatWebApp.Web.Components;
using AiChatWebApp.Web.Services;
using AiChatWebApp.Web.Services.Ingestion;
using AiChatWebApp.Web.Api;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var openai = builder.AddAzureOpenAIClient("openai");
openai.AddChatClient("gpt-5-mini")
    .UseFunctionInvocation()
    .UseOpenTelemetry(configure: c =>
        c.EnableSensitiveData = builder.Environment.IsDevelopment());
openai.AddEmbeddingGenerator("text-embedding-3-small");

var vectorStorePath = Path.Combine(AppContext.BaseDirectory, "vector-store.db");
var vectorStoreConnectionString = $"Data Source={vectorStorePath}";
builder.Services.AddSqliteCollection<string, IngestedChunk>("data-aichatwebapp-chunks", vectorStoreConnectionString);
builder.Services.AddSqliteCollection<string, IngestedDocument>("data-aichatwebapp-documents", vectorStoreConnectionString);
builder.Services.AddScoped<DataIngestor>();
builder.Services.AddSingleton<SemanticSearch>();

// Configure MarkItDown service
var markItDownServiceUrl = builder.Configuration["MarkItDownServiceUrl"] ?? "http://localhost:8490";
builder.Services.AddHttpClient<MarkItDownService>(client =>
{
    client.BaseAddress = new Uri(markItDownServiceUrl);
    client.Timeout = TimeSpan.FromMinutes(5);
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.UseStaticFiles();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Map document upload endpoint
app.MapDocumentUploadEndpoint();

// By default, we ingest PDF files from the /wwwroot/Data directory. You can ingest from
// other sources by implementing IIngestionSource.
// Important: ensure that any content you ingest is trusted, as it may be reflected back
// to users or could be a source of prompt injection risk.
await DataIngestor.IngestDataAsync(
    app.Services,
    new PDFDirectorySource(Path.Combine(builder.Environment.WebRootPath, "Data")));

app.Run();
