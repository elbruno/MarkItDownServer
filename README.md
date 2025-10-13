# MarkItDown Web Server

A production-ready web server application built using FastAPI that receives binary data from various document formats and converts them to Markdown using the MarkItDown library.

> **💡 Quick Answer: Is there a concurrency limit?**  
> By default, the server runs with 1 worker and no rate limiting. You can configure workers and rate limits using environment variables.  
> See [CONCURRENCY_SUMMARY.md](./CONCURRENCY_SUMMARY.md) for a quick guide or [CONCURRENCY.md](./CONCURRENCY.md) for detailed information.

## 🚀 Features

- **Multiple Format Support**: Convert DOC, DOCX, PPT, PPTX, PDF, XLS, XLSX, ODT, ODS, ODP, and TXT files to Markdown
- **FastAPI Framework**: Modern, fast, and well-documented REST API
- **Health Checks**: Built-in health monitoring endpoints
- **Input Validation**: Comprehensive file size, type, and content validation
- **Error Handling**: Robust error handling with detailed error messages
- **CORS Support**: Configurable CORS for web client integration
- **Security Headers**: Built-in security headers middleware
- **Docker Support**: Containerized deployment ready
- **Azure Compatible**: Ready for Azure Container Apps deployment

## 📋 Table of Contents

- [Quick Start](#quick-start)
- [Installation](#installation)
  - [Local Development](#local-development)
  - [Docker Deployment](#docker-deployment)
- [Usage](#usage)
  - [API Endpoints](#api-endpoints)
  - [Client Examples](#client-examples)
- [Configuration](#configuration)
- [Concurrency and Performance](#concurrency-and-performance)
- [Testing](#testing)
- [Deployment](#deployment)
  - [Local Deployment](#local-deployment)
  - [Azure Container Apps](#azure-container-apps)
- [Development](#development)
- [API Documentation](#api-documentation)
- [Dependencies](#dependencies)
- [License](#license)

## ⚡ Quick Start

### Using Docker (Recommended)

```bash
# Build the Docker image
docker build -t markitdownserver .

# Run the container
docker run -d --name markitdownserver -p 8490:8490 markitdownserver

# Test the health endpoint
curl http://localhost:8490/health
```

### Using Python (Development)

```bash
# Install dependencies
pip install -r requirements.txt

# Run the server
python app.py
```

The server will be available at `http://localhost:8490`

## 📦 Installation

### Prerequisites

- **Python 3.12+** (for local development)
- **Docker** (for containerized deployment)
- **.NET 9.0 SDK** (for running C# client examples)

### Local Development

1. **Clone the repository**:
   ```bash
   git clone https://github.com/elbruno/MarkItDownServer.git
   cd MarkItDownServer
   ```

2. **Create a virtual environment** (recommended):
   ```bash
   python -m venv venv
   source venv/bin/activate  # On Windows: venv\Scripts\activate
   ```

3. **Install dependencies**:
   ```bash
   pip install -r requirements.txt
   ```

4. **Run the server**:
   ```bash
   python app.py
   ```

   The server will start on `http://0.0.0.0:8490`

### Docker Deployment

1. **Build the Docker image**:
   ```bash
   docker build -t markitdownserver:latest .
   ```

2. **Run the container**:
   ```bash
   docker run -d \
     --name markitdownserver \
     -p 8490:8490 \
     markitdownserver:latest
   ```

3. **Verify the container is running**:
   ```bash
   docker ps | grep markitdownserver
   ```

4. **View logs**:
   ```bash
   docker logs markitdownserver
   ```

5. **Stop the container**:
   ```bash
   docker stop markitdownserver
   docker rm markitdownserver
   ```

## 📖 Usage

### API Endpoints

#### Root Endpoint
```http
GET /
```

Returns service information and available endpoints.

**Response**:
```json
{
  "service": "MarkItDown Server",
  "description": "API for converting documents to Markdown",
  "version": "1.0.0",
  "endpoints": {
    "health": "/health",
    "docs": "/docs",
    "process": "/process_file"
  }
}
```

#### Health Check
```http
GET /health
```

Returns the health status of the service.

**Response**:
```json
{
  "status": "healthy",
  "timestamp": "2025-01-07T12:00:00",
  "service": "MarkItDown Server",
  "version": "1.0.0"
}
```

#### Process File
```http
POST /process_file
```

Upload a document file and receive its content in Markdown format.

**Parameters**:
- `file`: The document file to convert (multipart/form-data)

**Supported File Types**:
- Microsoft Office: DOC, DOCX, XLS, XLSX, PPT, PPTX
- PDF: PDF
- OpenDocument: ODT, ODS, ODP
- Text: TXT

**File Size Limit**: 50MB

**Response**:
```json
{
  "markdown": "# Document Title\n\nContent in markdown format..."
}
```

**Error Responses**:
- `400 Bad Request`: Invalid file type or empty file
- `413 Payload Too Large`: File exceeds 50MB
- `500 Internal Server Error`: Conversion error

### Client Examples

#### Simple Console Application

Located in `samples/SimpleConsole/`, this is a basic example showing minimal code to use the API.

```bash
cd samples/SimpleConsole
dotnet run
```

**Code**:
```csharp
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
}
```

#### Detailed Console Application

Located in `samples/DetailedConsole/`, this includes comprehensive error handling, configuration, and features.

```bash
cd samples/DetailedConsole
dotnet run
```

**Features**:
- Configuration file support (`appsettings.json`)
- Comprehensive error handling
- Timeout configuration
- File validation
- Colored console output
- Automatic markdown file saving
- Content type detection

**Configuration** (`appsettings.json`):
```json
{
  "MarkItDownServer": {
    "Url": "http://127.0.0.1:8490/process_file",
    "FilePath": "Benefit_Options.pdf",
    "TimeoutMinutes": "5"
  }
}
```

#### cURL Example

```bash
curl -X POST "http://localhost:8490/process_file" \
  -F "file=@document.pdf" \
  -H "Content-Type: multipart/form-data"
```

#### Python Example

```python
import requests

url = "http://localhost:8490/process_file"
files = {"file": open("document.pdf", "rb")}

response = requests.post(url, files=files)
if response.status_code == 200:
    markdown = response.json()["markdown"]
    print(markdown)
else:
    print(f"Error: {response.status_code}")
    print(response.json())
```

#### PowerShell Example

```powershell
$url = "http://localhost:8490/process_file"
$filePath = "document.pdf"

$fileContent = [System.IO.File]::ReadAllBytes($filePath)
$boundary = [System.Guid]::NewGuid().ToString()
$LF = "`r`n"

$bodyLines = (
    "--$boundary",
    "Content-Disposition: form-data; name=`"file`"; filename=`"$(Split-Path $filePath -Leaf)`"",
    "Content-Type: application/pdf$LF",
    [System.Text.Encoding]::UTF8.GetString($fileContent),
    "--$boundary--$LF"
) -join $LF

Invoke-RestMethod -Uri $url -Method Post -ContentType "multipart/form-data; boundary=$boundary" -Body $bodyLines
```

## ⚙️ Configuration

### Environment Variables

The server can be configured using environment variables:

- `PORT`: Server port (default: 8490)
- `HOST`: Server host (default: 0.0.0.0)
- `MAX_FILE_SIZE`: Maximum file size in bytes (default: 52428800 = 50MB)
- `LOG_LEVEL`: Logging level (default: INFO)
- `WORKERS`: Number of worker processes (default: 1)
- `ENABLE_RATE_LIMIT`: Enable rate limiting (default: false)
- `RATE_LIMIT`: Rate limit (default: 60/minute)

### Docker Environment

```bash
docker run -d \
  --name markitdownserver \
  -p 8490:8490 \
  -e PORT=8490 \
  -e MAX_FILE_SIZE=104857600 \
  -e WORKERS=4 \
  -e ENABLE_RATE_LIMIT=true \
  -e RATE_LIMIT=100/minute \
  markitdownserver:latest
```

## 🚦 Concurrency and Performance

### Default Behavior

By default, the server runs with:
- **1 worker process** (single worker)
- **Async request handling** via FastAPI
- **No rate limiting**

### Configuring Concurrency

**Multi-worker setup** for better performance:
```bash
# Run with 4 workers
docker run -d -p 8490:8490 -e WORKERS=4 markitdownserver:latest
```

**Enable rate limiting** to prevent abuse:
```bash
# Limit to 100 requests per minute per IP
docker run -d -p 8490:8490 \
  -e ENABLE_RATE_LIMIT=true \
  -e RATE_LIMIT=100/minute \
  markitdownserver:latest
```

**Note**: Rate limiting requires `slowapi` package. Install with:
```bash
pip install slowapi
```

### Performance Recommendations

- **Small scale** (< 100 req/min): 1-2 workers
- **Medium scale** (100-1000 req/min): 2-4 workers  
- **Large scale** (> 1000 req/min): Use horizontal scaling with load balancer

**📚 For detailed concurrency information**, see [CONCURRENCY.md](./CONCURRENCY.md)

## 🧪 Testing

### Test the Server

1. **Start the server**:
   ```bash
   python app.py
   ```

2. **Run health check**:
   ```bash
   curl http://localhost:8490/health
   ```

3. **Test file conversion**:
   ```bash
   curl -X POST "http://localhost:8490/process_file" \
     -F "file=@samples/SimpleConsole/Benefit_Options.pdf"
   ```

### Run Client Examples

**Simple Console**:
```bash
cd samples/SimpleConsole
dotnet run
```

**Detailed Console**:
```bash
cd samples/DetailedConsole
dotnet run
```

## 🚀 Deployment

### Local Deployment

For development and testing:

```bash
# Using Python
python app.py

# Using uvicorn directly
uvicorn app:app --host 0.0.0.0 --port 8490 --reload
```

### Docker Deployment

For production:

```bash
# Build
docker build -t markitdownserver:1.0.0 .

# Run
docker run -d \
  --name markitdownserver \
  -p 8490:8490 \
  --restart unless-stopped \
  markitdownserver:1.0.0

# View logs
docker logs -f markitdownserver
```

### Azure Container Apps

See the comprehensive [CODE_QUALITY_IMPROVEMENTS.md](./CODE_QUALITY_IMPROVEMENTS.md) document for detailed Azure deployment instructions, including:

- Multi-stage Dockerfile optimization
- Azure CLI deployment scripts
- Bicep templates for Infrastructure as Code
- Environment configuration
- Security best practices
- Cost estimation and optimization

**Quick Azure Deployment**:

```bash
# Set variables
RESOURCE_GROUP="rg-markitdown"
LOCATION="eastus"
CONTAINER_APP_NAME="markitdown-server"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Deploy (see CODE_QUALITY_IMPROVEMENTS.md for complete script)
az containerapp up \
  --name $CONTAINER_APP_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --source .
```

## 👨‍💻 Development

### Project Structure

```
MarkItDownServer/
├── app.py                          # Main FastAPI application
├── requirements.txt                # Python dependencies
├── dockerfile                      # Docker configuration
├── README.md                       # This file
├── CODE_QUALITY_IMPROVEMENTS.md    # Comprehensive improvement guide
├── samples/
│   ├── SimpleConsole/              # Basic C# client example
│   │   ├── Program.cs
│   │   ├── SimpleConsole.csproj
│   │   └── Benefit_Options.pdf
│   └── DetailedConsole/            # Advanced C# client example
│       ├── Program.cs
│       ├── DetailedConsole.csproj
│       ├── appsettings.json
│       └── Benefit_Options.pdf
├── src/                            # Legacy client (preserved)
│   └── ...
└── utils/
    └── file_handler.py             # Utility functions
```

### Adding New Features

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

### Code Quality

The project follows Python best practices:
- Type hints for better code clarity
- Comprehensive error handling
- Input validation
- Security headers
- Structured logging

## 📚 API Documentation

The server provides automatic interactive API documentation:

- **Swagger UI**: http://localhost:8490/docs
- **ReDoc**: http://localhost:8490/redoc

These interfaces allow you to:
- Explore all available endpoints
- Test API calls directly from the browser
- View request/response schemas
- See example requests and responses

## 📦 Dependencies

### Python Dependencies

- **fastapi** (0.115.5): Modern web framework for building APIs
- **uvicorn** (0.32.1): ASGI server for FastAPI
- **python-multipart** (0.0.20): Multipart form data support
- **markitdown** (0.0.1a2): Document to Markdown conversion
- **pydantic** (2.10.3): Data validation using Python type hints

### System Requirements

- Python 3.12 or higher
- 512MB RAM minimum (1GB recommended)
- 100MB disk space

## 🔍 Troubleshooting

### Server won't start

**Issue**: Port already in use
```
Error: [Errno 48] Address already in use
```

**Solution**: Change the port or stop the conflicting service
```bash
# Find process using port 8490
lsof -i :8490

# Kill the process
kill -9 <PID>

# Or use a different port
python app.py --port 8491
```

### File conversion fails

**Issue**: "File type not allowed"

**Solution**: Ensure your file has a supported extension (doc, docx, pdf, etc.)

**Issue**: "File too large"

**Solution**: Files must be under 50MB. Compress or split large files.

### Docker issues

**Issue**: Cannot connect to Docker daemon

**Solution**: Ensure Docker Desktop is running
```bash
docker ps  # Test Docker connection
```

**Issue**: Container exits immediately

**Solution**: Check container logs
```bash
docker logs markitdownserver
```

## 📞 Support

For issues, questions, or contributions:

- **GitHub Issues**: [Create an issue](https://github.com/elbruno/MarkItDownServer/issues)
- **Documentation**: See [CODE_QUALITY_IMPROVEMENTS.md](./CODE_QUALITY_IMPROVEMENTS.md)

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with [FastAPI](https://fastapi.tiangolo.com/)
- Powered by [MarkItDown](https://github.com/microsoft/markitdown)
- Developed by [El Bruno](https://github.com/elbruno)

## 📈 Version History

- **v1.0.0** (2025-01): Initial release with production-ready features
  - Multi-format document conversion
  - Comprehensive error handling
  - Health check endpoints
  - Docker support
  - Azure deployment ready

---

**Ready to convert your documents to Markdown?** 🚀

Start the server and visit http://localhost:8490/docs to explore the interactive API documentation!