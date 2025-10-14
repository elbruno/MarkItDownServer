var builder = DistributedApplication.CreateBuilder(args);

// You will need to set the connection string to your own value
// You can do this using Visual Studio's "Manage User Secrets" UI, or on the command line:
//   cd this-project-directory
//   dotnet user-secrets set ConnectionStrings:openai "Endpoint=https://models.inference.ai.azure.com;Key=YOUR-API-KEY"
var openai = builder.AddConnectionString("openai");

// Add MarkItDown service as a container or external endpoint
// Option 1: If running MarkItDown locally or externally, configure the endpoint
// var markitdownServiceUrl = builder.Configuration["MarkItDownServiceUrl"] ?? "http://localhost:8490";
// var markitdown = builder.AddParameter("markitdown-url", markitdownServiceUrl);

// Option 2: If you have the MarkItDown Docker image available, uncomment this:
var markitdown = builder.AddContainer("markitdownserver", "markitdownserver", "latest")
    .WithHttpEndpoint(port: 8490, targetPort: 8490, name: "http");

var webApp = builder.AddProject<Projects.AiChatWebApp_Web>("aichatweb-app");
webApp.WithReference(openai);
webApp.WithEnvironment("MarkItDownServiceUrl", markitdown.GetEndpoint("http"));

builder.Build().Run();
