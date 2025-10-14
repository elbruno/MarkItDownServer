# AI Chat Web App with Document Upload and MarkItDown Integration

This project is an AI chat application that demonstrates how to chat with custom data using an AI language model. It integrates with the MarkItDown server to convert various document formats to Markdown, enabling users to upload and chat with their documents.

## Features

- **AI-Powered Chat**: Chat with your documents using GitHub Models or Azure AI Foundry
- **Document Upload**: Upload documents (PDF, Word, PowerPoint, Excel, Text files) through the web interface
- **MarkItDown Integration**: Automatic conversion of uploaded documents to Markdown format
- **Vector Search**: Semantic search across ingested documents using embeddings
- **Aspire Orchestration**: Built with .NET Aspire for easy development and deployment
- **Real-time Processing**: Documents are processed and indexed immediately after upload

>[!NOTE]
> Before running this project you need to configure the API keys or endpoints for the providers you have chosen. See below for details specific to your choices.

### Known Issues

#### Errors running Ollama or Docker

A recent incompatibility was found between Ollama and Docker Desktop. This issue results in runtime errors when connecting to Ollama, and the workaround for that can lead to Docker not working for Aspire projects.

This incompatibility can be addressed by upgrading to Docker Desktop 4.41.1. See [ollama/ollama#9509](https://github.com/ollama/ollama/issues/9509#issuecomment-2842461831) for more information and a link to install the version of Docker Desktop with the fix.

# Configure the AI Model Provider

## Using GitHub Models
To use models hosted by GitHub Models, you will need to create a GitHub personal access token. The token should not have any scopes or permissions. See [Managing your personal access tokens](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens).

From the command line, configure your token for this project using .NET User Secrets by running the following commands:

```sh
cd AiChatWebApp.AppHost
dotnet user-secrets set ConnectionStrings:openai "Endpoint=https://models.inference.ai.azure.com;Key=YOUR-API-KEY"
```

Learn more about [prototyping with AI models using GitHub Models](https://docs.github.com/github-models/prototyping-with-ai-models).

# Configure MarkItDown Service

This application uses the MarkItDown server to convert uploaded documents to Markdown format. You need to have the MarkItDown server running before starting the application.

## Option 1: Run MarkItDown Server Locally

The easiest way is to run the MarkItDown server from the repository root:

```sh
# From the MarkItDownServer repository root
cd ../../
python app.py
```

The server will be available at `http://localhost:8490` by default.

## Option 2: Use Docker

If you have Docker available, you can run the MarkItDown server in a container:

```sh
# Build the Docker image (from repository root)
docker build -t markitdownserver:local .

# Run the container
docker run -d --name markitdownserver -p 8490:8490 markitdownserver:local
```

## Option 3: Use a Remote MarkItDown Server

If you have a MarkItDown server running elsewhere, configure the URL in the AppHost:

```sh
cd AiChatWebApp.AppHost
dotnet user-secrets set "MarkItDownServiceUrl" "http://your-markitdown-server:8490"
```

# Running the application

## Using Visual Studio

1. Open the `.sln` file in Visual Studio.
2. Press `Ctrl+F5` or click the "Start" button in the toolbar to run the project.

## Using Visual Studio Code

1. Open the project folder in Visual Studio Code.
2. Install the [C# Dev Kit extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) for Visual Studio Code.
3. Once installed, Open the `Program.cs` file in the AiChatWebApp.AppHost project.
4. Run the project by clicking the "Run" button in the Debug view.

## Trust the localhost certificate

Several Aspire templates include ASP.NET Core projects that are configured to use HTTPS by default. If this is the first time you're running the project, an exception might occur when loading the Aspire dashboard. This error can be resolved by trusting the self-signed development certificate with the .NET CLI.

See [Troubleshoot untrusted localhost certificate in Aspire](https://learn.microsoft.com/dotnet/aspire/troubleshooting/untrusted-localhost-certificate) for more information.

# Updating JavaScript dependencies

This template leverages JavaScript libraries to provide essential functionality. These libraries are located in the wwwroot/lib folder of the AiChatWebApp.Web project. For instructions on updating each dependency, please refer to the README.md file in each respective folder.

# Using the Application

## Uploading Documents

1. Click the **"Upload Document"** button at the top of the chat interface
2. Select a document from your computer (supported formats: PDF, Word, PowerPoint, Excel, Text, Markdown, HTML)
3. The document will be:
   - Saved to the `/Data/` folder for viewing and downloading
   - Converted to Markdown using the MarkItDown service
   - Automatically chunked into searchable segments
   - Indexed in the vector store with embeddings
   - Immediately available for chat queries
4. A confirmation message will appear in the chat when the document is ready
5. You can now ask questions about the document content
6. PDF files can be viewed through the citation links

**Note**: Uploaded documents are saved to `wwwroot/Data/` and are also converted to Markdown for indexing. PDF citations include a viewer link to see the original document. Other document types (Word, Excel, etc.) will show the citation but won't have a preview viewer.

## Chatting with Documents

Once documents are uploaded or the example PDFs are available, you can ask questions like:

- "What does the GPS watch document say about battery life?"
- "Summarize the emergency survival kit guide"
- "What are the key features mentioned in the uploaded document?"

The AI will search through the ingested documents and provide answers with citations.

## Architecture

This application consists of several components:

1. **AiChatWebApp.Web** - Blazor Server web application with chat UI and document upload
2. **AiChatWebApp.AppHost** - .NET Aspire orchestration host
3. **AiChatWebApp.ServiceDefaults** - Shared service configuration
4. **MarkItDown Service** - External service for document conversion (runs separately)

### Document Processing Flow

1. User uploads a document through the web interface
2. Document is saved to the `wwwroot/uploads` directory
3. Upload API endpoint triggers the ingestion process
4. UploadedFileSource reads the file and calls MarkItDown service to convert it to Markdown
5. The Markdown content is chunked and stored in the SQLite vector database
6. Embeddings are generated for semantic search
7. The document is now available for chat queries

## Troubleshooting

### MarkItDown Service Not Available

If you see errors about connecting to the MarkItDown service:

1. Ensure the MarkItDown server is running (see configuration section above)
2. Check that the service URL is correctly configured
3. Verify network connectivity to the service

### Upload Fails

If document uploads fail:

1. Check the file size is under 50MB
2. Ensure the file format is supported
3. Check the application logs for detailed error messages

### Chat Not Finding Documents

If the AI can't find information from your documents:

1. Wait a few seconds after upload for processing to complete
2. Try rephrasing your question
3. Check that the document contains the information you're looking for

# Learn More
To learn more about development with .NET and AI, check out the following links:

* [AI for .NET Developers](https://learn.microsoft.com/dotnet/ai/)
* [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
* [GitHub Models](https://docs.github.com/github-models)
* [MarkItDown Project](https://github.com/microsoft/markitdown)
