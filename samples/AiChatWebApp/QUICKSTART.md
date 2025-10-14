# Quick Start Guide - AI Chat Web App

This guide will help you get the AI Chat Web App running in under 5 minutes.

## Prerequisites

- .NET 9 SDK
- Python 3.12 (for MarkItDown server)
- GitHub Models API token (free - [get one here](https://github.com/settings/tokens/new))

## Step 1: Start the MarkItDown Server

Open a terminal and navigate to the repository root:

```bash
cd ../../
pip install -r requirements.txt
python app.py
```

The MarkItDown server will start at `http://localhost:8490`.

Keep this terminal open.

## Step 2: Configure GitHub Models API Key

Open a **new terminal** and navigate to the AppHost directory:

```bash
cd samples/AiChatWebApp/AiChatWebApp.AppHost
```

Set your GitHub Models API token using .NET User Secrets:

```bash
dotnet user-secrets set ConnectionStrings:openai "Endpoint=https://models.inference.ai.azure.com;Key=YOUR_GITHUB_TOKEN"
```

Replace `YOUR_GITHUB_TOKEN` with your actual GitHub personal access token.

> **Note**: The token doesn't need any special scopes or permissions. Just create a basic token.

## Step 3: Run the Application

From the AppHost directory, run:

```bash
dotnet run
```

The Aspire dashboard will open automatically in your browser. Click on the "aichatweb-app" endpoint to access the chat interface.

## Step 4: Try It Out

1. **Chat with Example Documents**  
   Try asking: "What features does the GPS watch have?"

2. **Upload Your Own Document**  
   - Click "Upload Document"
   - Select a PDF, Word, or other supported file
   - Wait for processing (usually 5-10 seconds)
   - Ask questions about your document!

## Supported File Types

- PDF (`.pdf`)
- Microsoft Word (`.docx`, `.doc`)
- PowerPoint (`.pptx`, `.ppt`)
- Excel (`.xlsx`, `.xls`)
- Text files (`.txt`, `.md`)
- HTML (`.html`)

## Troubleshooting

### "Cannot connect to MarkItDown service"

- Make sure the MarkItDown server is running (Step 1)
- Check that it's accessible at `http://localhost:8490/health`

### "Authentication failed"

- Verify your GitHub token is correct
- Make sure you set it in the AppHost project (Step 2)

### "Document upload failed"

- Check file size is under 50MB
- Ensure file format is supported
- Look at the browser console for detailed error messages

## What's Next?

- Check out the [full README](README.md) for detailed documentation
- Explore the code to see how MarkItDown integration works
- Customize the chat prompts and UI to fit your needs

## Architecture Overview

```
┌─────────────────┐
│   Browser UI    │  ← Upload files & chat
└────────┬────────┘
         │
┌────────▼────────┐
│  Blazor Server  │  ← Chat logic & RAG
│   (Web App)     │
└────┬────────┬───┘
     │        │
     │        └──────────┐
     │                   │
┌────▼────────┐   ┌──────▼──────────┐
│  GitHub     │   │   MarkItDown    │
│  Models     │   │   Server        │
│  (AI)       │   │ (Doc Converter) │
└─────────────┘   └─────────────────┘
```

The app uses Retrieval-Augmented Generation (RAG) to:
1. Convert documents to searchable Markdown
2. Store content in a vector database
3. Retrieve relevant chunks for AI responses
4. Cite sources in answers
