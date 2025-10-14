# AI Chat Web App - User Manual

## Table of Contents
- [Introduction](#introduction)
- [Getting Started](#getting-started)
- [User Interface Guide](#user-interface-guide)
- [Features](#features)
- [Troubleshooting](#troubleshooting)
- [FAQ](#faq)

## Introduction

The AI Chat Web App is a modern web application that allows you to chat with your documents using AI. Upload documents in various formats (PDF, Word, Excel, PowerPoint, and more), and the application will convert them to a searchable format, allowing you to ask questions and get intelligent answers with citations.

### Key Capabilities
- **Document Upload**: Support for multiple document formats
- **Real-time Conversion**: Automatic conversion to searchable Markdown
- **AI-Powered Chat**: Ask questions about your documents in natural language
- **Citation Support**: Answers include citations from source documents
- **PDF Viewer**: View PDF documents directly in the browser

## Getting Started

### Prerequisites
- Modern web browser (Chrome, Edge, Firefox, or Safari)
- Internet connection (for AI services)
- Documents you want to chat with

### First Time Setup

1. **Access the Application**
   - Open your web browser
   - Navigate to the application URL (typically `https://localhost:7281` for local development)
   - You should see the chat interface with example documents

2. **Try the Example Documents**
   - Two example documents are pre-loaded:
     - Example_GPS_Watch.pdf
     - Example_Emergency_Survival_Kit.pdf
   - Click on these to see how citations work

3. **Ask Your First Question**
   - Type a question in the chat input at the bottom
   - Example: "What features does the GPS watch have?"
   - Press Enter or click Send
   - Watch as the AI searches the documents and provides an answer with citations

## User Interface Guide

### Main Chat Area

The main chat area displays:
- **System Messages**: Information about uploaded documents
- **Your Questions**: Messages you send appear on the right
- **AI Responses**: Answers from the AI appear on the left with an icon
- **Citations**: Referenced documents appear as clickable cards below answers

### Upload Button

Located at the top of the chat interface:
- Click "Upload Document" to select files from your computer
- Supported formats: PDF, Word (DOC/DOCX), PowerPoint (PPT/PPTX), Excel (XLS/XLSX), Text (TXT), Markdown (MD), HTML
- Maximum file size: 50MB
- Upload progress is shown with a spinner
- Success/error messages appear after upload

### Chat Input

At the bottom of the screen:
- Text input field for typing questions
- Send button (or press Enter)
- Suggested questions appear above the input (when available)

### Citations

Citations appear as cards with:
- Document icon
- File name
- Quoted text from the document
- For PDF files: clickable link to view the document

## Features

### 1. Document Upload

**How to Upload Documents:**

1. Click the "Upload Document" button at the top of the chat
2. Select one or more files from your computer
3. Wait for the upload spinner to complete
4. You'll see a confirmation message: "✓ File uploaded and converted successfully"
5. The document is now searchable

**What Happens During Upload:**
- File is saved to the server
- Document is converted to Markdown format
- Content is indexed in the search database
- Document becomes immediately searchable

**Supported File Types:**
- **PDF** (.pdf) - Fully supported with viewer
- **Microsoft Word** (.doc, .docx)
- **Microsoft PowerPoint** (.ppt, .pptx)
- **Microsoft Excel** (.xls, .xlsx)
- **Text Files** (.txt)
- **Markdown** (.md)
- **HTML** (.html)

### 2. Asking Questions

**Tips for Better Results:**

- **Be Specific**: Instead of "Tell me about the watch," ask "What is the battery life of the GPS watch?"
- **Use Keywords**: Include specific terms from your documents
- **Ask One Thing at a Time**: Break complex questions into simpler ones
- **Reference Documents**: You can ask about specific files, e.g., "In the GPS watch document, what are the dimensions?"

**Example Questions:**
- "What does the emergency kit include?"
- "How much does the GPS watch weigh?"
- "What are the key features mentioned in PerksPlus.pdf?"
- "Summarize the health and wellness benefits"

### 3. Understanding Citations

When the AI answers your question, it includes citations showing where the information came from:

**Citation Components:**
- **File Name**: The source document
- **Quote**: Exact text from the document that supports the answer
- **Page Number**: For PDF documents, the page where the information was found
- **Link**: For PDFs, click to view the document at that page

**Citation Example:**
```
📄 Example_GPS_Watch.pdf
"battery life up to 20 hours"
```

### 4. PDF Viewer

**Viewing PDF Documents:**

1. Click on a citation for a PDF document
2. The PDF viewer opens in a new tab
3. Features:
   - Navigation controls
   - Zoom in/out
   - Search highlighting (shows your search term highlighted in the document)
   - Page navigation

**Note**: Only PDF files have the interactive viewer. Other document types are converted to text for searching but can't be viewed in the original format.

### 5. Document Search

The AI uses semantic search to find relevant information:
- **Semantic Understanding**: Finds related content even if exact words don't match
- **Context-Aware**: Understands the meaning of your question
- **Multi-Document**: Searches across all uploaded documents
- **Ranked Results**: Shows the most relevant information first

### 6. Conversation History

- Chat history persists during your session
- You can reference previous questions and answers
- The AI remembers context from earlier in the conversation
- Click "New Chat" to start fresh

## Troubleshooting

### Upload Issues

**Problem**: "File size exceeds 50MB limit"
- **Solution**: Split large documents into smaller files or compress PDFs

**Problem**: "File type not supported"
- **Solution**: Convert your file to a supported format (PDF, DOCX, PPTX, etc.)

**Problem**: Upload spinner never completes
- **Solution**: 
  - Check your internet connection
  - Ensure the MarkItDown service is running
  - Try uploading a smaller file first

### Search/Chat Issues

**Problem**: "No results found" or answers don't make sense
- **Solution**:
  - Wait a few seconds after upload for indexing to complete
  - Rephrase your question with different keywords
  - Make sure your question relates to the uploaded documents
  - Try asking a more specific question

**Problem**: Citations show "(uploaded)" but no link
- **Solution**: This is normal for non-PDF documents that were uploaded. Only PDFs have the viewer feature.

**Problem**: PDF viewer shows 404 error
- **Solution**:
  - Ensure the document was successfully uploaded
  - Check that you're clicking on a PDF citation
  - Try re-uploading the document

### General Issues

**Problem**: Application won't load
- **Solution**:
  - Refresh the browser
  - Clear browser cache
  - Check that the server is running
  - Verify your network connection

**Problem**: Slow responses
- **Solution**:
  - This is normal for complex questions
  - The AI needs time to search and generate answers
  - Larger documents take longer to process

## FAQ

### General Questions

**Q: How many documents can I upload?**
A: There's no hard limit, but performance may degrade with many large documents. We recommend staying under 100 documents total.

**Q: Can I delete uploaded documents?**
A: Currently, documents remain uploaded for the duration of your session. Refresh the page to start with a clean slate.

**Q: Is my data secure?**
A: Documents are processed locally on the server. In production environments, ensure proper security measures are in place.

**Q: Can I download my uploaded documents?**
A: PDF documents can be viewed and downloaded through the citation viewer. Other formats are converted to searchable text.

### Technical Questions

**Q: What AI model is used?**
A: The application uses GitHub Models (gpt-4o-mini) for natural language understanding and response generation.

**Q: How does document indexing work?**
A: Documents are converted to Markdown, split into chunks, and stored in a vector database with embeddings for semantic search.

**Q: What happens to my documents after upload?**
A: Documents are saved to the server's Data folder and indexed in the search database. They remain available until removed.

**Q: Can I use this offline?**
A: No, the application requires an internet connection to access the AI services (GitHub Models) for generating responses.

### Feature Questions

**Q: Can I chat with multiple documents at once?**
A: Yes! The AI searches across all uploaded documents and can reference multiple sources in a single answer.

**Q: Does the AI remember previous questions?**
A: Yes, within a single session, the AI maintains conversation context. Click "New Chat" to start fresh.

**Q: Can I export the chat history?**
A: This feature is not currently available but may be added in future versions.

**Q: What languages are supported?**
A: The AI primarily works with English documents and queries. Other languages may work but with reduced accuracy.

## Tips and Best Practices

### For Best Results

1. **Upload Quality Documents**: Clear, well-formatted documents work best
2. **Descriptive Filenames**: Use meaningful names for your documents
3. **Start Specific**: Begin with specific questions before asking for summaries
4. **Verify Citations**: Always check the cited sources to verify information
5. **One Topic Per Upload**: Group related documents together

### Document Preparation

- **PDFs**: Ensure text is selectable (not scanned images without OCR)
- **Word Docs**: Keep formatting simple; complex layouts may not convert well
- **Excel**: Works best with tabular data; formulas are not executed
- **PowerPoint**: Bullet points and text are extracted; images are not processed

### Query Strategies

- **Start Broad**: "What topics are covered in this document?"
- **Then Specific**: "What is the warranty period for the GPS watch?"
- **Compare**: "What are the differences between the two health plans?"
- **Summarize**: "Summarize the key benefits in the emergency kit guide"

## Support and Feedback

For issues, questions, or feedback:
- Check the [Implementation Documentation](../IMPLEMENTATION.md)
- Review the [Quick Start Guide](../QUICKSTART.md)
- Visit the [GitHub Repository](https://github.com/elbruno/MarkItDownServer)

---

**Last Updated**: October 2025
**Version**: 1.0
