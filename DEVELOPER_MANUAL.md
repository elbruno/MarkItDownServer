# MarkItDown Server - Developer Manual

## 📘 Introduction

This manual provides comprehensive instructions for developers who want to use the MarkItDown Server in their applications. Whether you're pulling the published Docker image or integrating the API into your codebase, this guide has you covered.

## 🎯 Quick Start

### Pull and Run the Published Image

The easiest way to get started is to pull the pre-built Docker image from the GitHub Container Registry:

```bash
# Pull the latest image
docker pull ghcr.io/elbruno/markitdownserver:latest

# Run the container
docker run -d \
  --name markitdownserver \
  -p 8490:8490 \
  ghcr.io/elbruno/markitdownserver:latest

# Verify it's running
curl http://localhost:8490/health
```

## 📦 Available Images

### GitHub Container Registry (Recommended)

```bash
# Latest version
ghcr.io/elbruno/markitdownserver:latest

# Specific version
ghcr.io/elbruno/markitdownserver:v1.0.0

# Main branch (development)
ghcr.io/elbruno/markitdownserver:main
```

### Docker Hub (Alternative)

```bash
# Latest version
docker.io/elbruno/markitdownserver:latest
```

## 🚀 Usage Scenarios

### Scenario 1: Development Environment

Run the server locally for development and testing:

```bash
# Run with auto-restart on failure
docker run -d \
  --name markitdown-dev \
  -p 8490:8490 \
  --restart unless-stopped \
  ghcr.io/elbruno/markitdownserver:latest

# View logs
docker logs -f markitdown-dev
```

### Scenario 2: Custom Port

Run the server on a different port:

```bash
docker run -d \
  --name markitdownserver \
  -p 9000:8490 \
  ghcr.io/elbruno/markitdownserver:latest

# Server will be available at http://localhost:9000
```

### Scenario 3: Docker Compose

Create a `docker-compose.yml` file:

```yaml
version: '3.8'

services:
  markitdown:
    image: ghcr.io/elbruno/markitdownserver:latest
    container_name: markitdownserver
    ports:
      - "8490:8490"
    restart: unless-stopped
    environment:
      - MAX_FILE_SIZE=52428800
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8490/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 10s
```

Run with:

```bash
docker-compose up -d
```

### Scenario 4: Production Deployment with Limits

Run with resource limits for production:

```bash
docker run -d \
  --name markitdownserver \
  -p 8490:8490 \
  --restart always \
  --memory="512m" \
  --cpus="0.5" \
  --health-cmd="curl -f http://localhost:8490/health || exit 1" \
  --health-interval=30s \
  --health-timeout=10s \
  --health-retries=3 \
  ghcr.io/elbruno/markitdownserver:latest
```

## 🔧 Configuration

### Environment Variables

Configure the server using environment variables:

```bash
docker run -d \
  --name markitdownserver \
  -p 8490:8490 \
  -e PORT=8490 \
  -e MAX_FILE_SIZE=104857600 \
  -e LOG_LEVEL=INFO \
  ghcr.io/elbruno/markitdownserver:latest
```

Available environment variables:

| Variable | Description | Default |
|----------|-------------|---------|
| `PORT` | Server port | 8490 |
| `HOST` | Server host | 0.0.0.0 |
| `MAX_FILE_SIZE` | Maximum file size in bytes | 52428800 (50MB) |
| `LOG_LEVEL` | Logging level (DEBUG, INFO, WARNING, ERROR) | INFO |

### Volume Mounting

Mount a volume for persistent logs or temporary files:

```bash
docker run -d \
  --name markitdownserver \
  -p 8490:8490 \
  -v $(pwd)/logs:/app/logs \
  ghcr.io/elbruno/markitdownserver:latest
```

## 💻 Client Integration Examples

### C# / .NET

#### Simple Integration

```csharp
using System.Net.Http.Headers;

var client = new HttpClient();
var url = "http://localhost:8490/process_file";

var fileBytes = await File.ReadAllBytesAsync("document.pdf");
var content = new MultipartFormDataContent();
var fileContent = new ByteArrayContent(fileBytes);
fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
content.Add(fileContent, "file", "document.pdf");

var response = await client.PostAsync(url, content);
var result = await response.Content.ReadAsStringAsync();
Console.WriteLine(result);
```

#### Production-Ready Integration

Use the provided `DetailedConsole` sample:

```bash
cd samples/DetailedConsole
dotnet run
```

Features:
- Configuration file support
- Comprehensive error handling
- Automatic file type detection
- Markdown file output
- Colored console feedback

### Python

```python
import requests

def convert_to_markdown(file_path: str, server_url: str = "http://localhost:8490") -> str:
    """Convert a document to Markdown using the MarkItDown Server."""
    
    with open(file_path, 'rb') as f:
        files = {'file': (file_path, f)}
        response = requests.post(f"{server_url}/process_file", files=files)
        
        if response.status_code == 200:
            return response.json()['markdown']
        else:
            raise Exception(f"Error: {response.status_code} - {response.text}")

# Usage
try:
    markdown = convert_to_markdown("document.pdf")
    print(markdown)
except Exception as e:
    print(f"Failed to convert: {e}")
```

### JavaScript / Node.js

```javascript
const FormData = require('form-data');
const fs = require('fs');
const axios = require('axios');

async function convertToMarkdown(filePath, serverUrl = 'http://localhost:8490') {
    const form = new FormData();
    form.append('file', fs.createReadStream(filePath));
    
    try {
        const response = await axios.post(
            `${serverUrl}/process_file`,
            form,
            { headers: form.getHeaders() }
        );
        return response.data.markdown;
    } catch (error) {
        console.error('Conversion failed:', error.response?.data || error.message);
        throw error;
    }
}

// Usage
convertToMarkdown('document.pdf')
    .then(markdown => console.log(markdown))
    .catch(err => console.error(err));
```

### cURL

```bash
# Basic usage
curl -X POST "http://localhost:8490/process_file" \
  -F "file=@document.pdf"

# With output to file
curl -X POST "http://localhost:8490/process_file" \
  -F "file=@document.pdf" \
  -o output.json

# Extract markdown from JSON response
curl -X POST "http://localhost:8490/process_file" \
  -F "file=@document.pdf" \
  | jq -r '.markdown' > output.md
```

### PowerShell

```powershell
function Convert-ToMarkdown {
    param(
        [string]$FilePath,
        [string]$ServerUrl = "http://localhost:8490"
    )
    
    $fileBytes = [System.IO.File]::ReadAllBytes($FilePath)
    $boundary = [System.Guid]::NewGuid().ToString()
    $LF = "`r`n"
    
    $bodyLines = (
        "--$boundary",
        "Content-Disposition: form-data; name=`"file`"; filename=`"$(Split-Path $FilePath -Leaf)`"",
        "Content-Type: application/octet-stream$LF",
        [System.Text.Encoding]::UTF8.GetString($fileBytes),
        "--$boundary--$LF"
    ) -join $LF
    
    $response = Invoke-RestMethod `
        -Uri "$ServerUrl/process_file" `
        -Method Post `
        -ContentType "multipart/form-data; boundary=$boundary" `
        -Body $bodyLines
    
    return $response.markdown
}

# Usage
$markdown = Convert-ToMarkdown -FilePath "document.pdf"
Write-Output $markdown
```

### Go

```go
package main

import (
    "bytes"
    "encoding/json"
    "fmt"
    "io"
    "mime/multipart"
    "net/http"
    "os"
)

type MarkdownResponse struct {
    Markdown string `json:"markdown"`
}

func convertToMarkdown(filePath string, serverURL string) (string, error) {
    file, err := os.Open(filePath)
    if err != nil {
        return "", err
    }
    defer file.Close()

    body := &bytes.Buffer{}
    writer := multipart.NewWriter(body)
    
    part, err := writer.CreateFormFile("file", filePath)
    if err != nil {
        return "", err
    }
    
    _, err = io.Copy(part, file)
    if err != nil {
        return "", err
    }
    
    writer.Close()

    req, err := http.NewRequest("POST", serverURL+"/process_file", body)
    if err != nil {
        return "", err
    }
    
    req.Header.Set("Content-Type", writer.FormDataContentType())

    client := &http.Client{}
    resp, err := client.Do(req)
    if err != nil {
        return "", err
    }
    defer resp.Body.Close()

    if resp.StatusCode != http.StatusOK {
        return "", fmt.Errorf("server returned status %d", resp.StatusCode)
    }

    var result MarkdownResponse
    if err := json.NewDecoder(resp.Body).Decode(&result); err != nil {
        return "", err
    }

    return result.Markdown, nil
}

func main() {
    markdown, err := convertToMarkdown("document.pdf", "http://localhost:8490")
    if err != nil {
        fmt.Println("Error:", err)
        return
    }
    fmt.Println(markdown)
}
```

## 🔍 API Reference

### Base URL

When running locally:
```
http://localhost:8490
```

### Endpoints

#### GET /

Returns service information.

**Response:**
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

#### GET /health

Health check endpoint.

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2025-01-07T12:00:00",
  "service": "MarkItDown Server",
  "version": "1.0.0"
}
```

#### GET /docs

Interactive API documentation (Swagger UI).

#### POST /process_file

Convert a document to Markdown.

**Request:**
- Content-Type: `multipart/form-data`
- Body: `file` - Document file

**Supported File Types:**
- Microsoft Office: `.doc`, `.docx`, `.xls`, `.xlsx`, `.ppt`, `.pptx`
- PDF: `.pdf`
- OpenDocument: `.odt`, `.ods`, `.odp`
- Text: `.txt`

**Limits:**
- Maximum file size: 50MB (configurable)

**Success Response (200 OK):**
```json
{
  "markdown": "# Document Title\n\nContent..."
}
```

**Error Responses:**

- **400 Bad Request**: Invalid file type or empty file
  ```json
  {
    "error": "File type not allowed. Allowed types: doc, docx, pdf, ..."
  }
  ```

- **413 Payload Too Large**: File exceeds size limit
  ```json
  {
    "error": "File too large. Maximum size: 50MB"
  }
  ```

- **500 Internal Server Error**: Conversion failed
  ```json
  {
    "error": "Conversion error message"
  }
  ```

## 🛠️ Troubleshooting

### Container Won't Start

**Problem**: Container exits immediately

**Solution**: Check the logs
```bash
docker logs markitdownserver
```

Common issues:
- Port already in use: Use a different port with `-p 9000:8490`
- Memory limit too low: Increase with `--memory="1g"`

### Connection Refused

**Problem**: Can't connect to http://localhost:8490

**Solution**: Verify the container is running
```bash
docker ps | grep markitdownserver
```

If not running:
```bash
docker start markitdownserver
```

### Conversion Fails

**Problem**: File conversion returns 500 error

**Solutions**:
1. Check file format is supported
2. Verify file is not corrupted
3. Ensure file size is under limit
4. Check container logs for specific error

### Slow Performance

**Problem**: File conversion takes too long

**Solutions**:
1. Increase container resources:
   ```bash
   docker update --memory="1g" --cpus="1.0" markitdownserver
   ```

2. Use faster storage for volumes

3. Process files in batches with proper queuing

## 📊 Monitoring

### Health Checks

Monitor the server health:

```bash
# Simple health check
curl http://localhost:8490/health

# Continuous monitoring
watch -n 5 'curl -s http://localhost:8490/health | jq'
```

### Logs

View container logs:

```bash
# Follow logs
docker logs -f markitdownserver

# Last 100 lines
docker logs --tail 100 markitdownserver

# Logs since timestamp
docker logs --since 2025-01-07T12:00:00 markitdownserver
```

### Metrics

Check container resource usage:

```bash
docker stats markitdownserver
```

## 🔐 Security Best Practices

### 1. Network Security

Run in isolated network:

```bash
docker network create markitdown-net

docker run -d \
  --name markitdownserver \
  --network markitdown-net \
  -p 8490:8490 \
  ghcr.io/elbruno/markitdownserver:latest
```

### 2. Read-Only Filesystem

Run with read-only root filesystem:

```bash
docker run -d \
  --name markitdownserver \
  -p 8490:8490 \
  --read-only \
  --tmpfs /tmp \
  ghcr.io/elbruno/markitdownserver:latest
```

### 3. Drop Capabilities

Run with minimal Linux capabilities:

```bash
docker run -d \
  --name markitdownserver \
  -p 8490:8490 \
  --cap-drop ALL \
  --cap-add NET_BIND_SERVICE \
  ghcr.io/elbruno/markitdownserver:latest
```

### 4. Use Secrets for Sensitive Data

For production deployments with authentication:

```bash
echo "your-api-key" | docker secret create api_key -

docker service create \
  --name markitdownserver \
  --secret api_key \
  -p 8490:8490 \
  ghcr.io/elbruno/markitdownserver:latest
```

## 🚢 Production Deployment

### Kubernetes Deployment

Create `markitdown-deployment.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: markitdownserver
spec:
  replicas: 3
  selector:
    matchLabels:
      app: markitdownserver
  template:
    metadata:
      labels:
        app: markitdownserver
    spec:
      containers:
      - name: markitdownserver
        image: ghcr.io/elbruno/markitdownserver:latest
        ports:
        - containerPort: 8490
        livenessProbe:
          httpGet:
            path: /health
            port: 8490
          initialDelaySeconds: 10
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /health
            port: 8490
          initialDelaySeconds: 5
          periodSeconds: 10
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
---
apiVersion: v1
kind: Service
metadata:
  name: markitdownserver
spec:
  selector:
    app: markitdownserver
  ports:
  - port: 80
    targetPort: 8490
  type: LoadBalancer
```

Deploy:

```bash
kubectl apply -f markitdown-deployment.yaml
```

### Docker Swarm

```bash
docker swarm init

docker service create \
  --name markitdownserver \
  --replicas 3 \
  --publish 8490:8490 \
  --update-parallelism 1 \
  --update-delay 10s \
  --rollback-parallelism 1 \
  --rollback-delay 5s \
  ghcr.io/elbruno/markitdownserver:latest
```

## 🔄 Updating

### Pull Latest Image

```bash
# Pull new version
docker pull ghcr.io/elbruno/markitdownserver:latest

# Stop and remove old container
docker stop markitdownserver
docker rm markitdownserver

# Run new container
docker run -d \
  --name markitdownserver \
  -p 8490:8490 \
  ghcr.io/elbruno/markitdownserver:latest
```

### Zero-Downtime Update

```bash
# Start new container on different port
docker run -d \
  --name markitdownserver-new \
  -p 8491:8490 \
  ghcr.io/elbruno/markitdownserver:latest

# Verify new container works
curl http://localhost:8491/health

# Stop old container
docker stop markitdownserver

# Update port mapping (requires stopping and starting)
docker rm markitdownserver
docker rename markitdownserver-new markitdownserver
docker stop markitdownserver

docker run -d \
  --name markitdownserver \
  -p 8490:8490 \
  ghcr.io/elbruno/markitdownserver:latest
```

## 📞 Support and Resources

### Documentation
- **Main README**: [README.md](./README.md)
- **Code Quality Guide**: [CODE_QUALITY_IMPROVEMENTS.md](./CODE_QUALITY_IMPROVEMENTS.md)
- **API Docs**: http://localhost:8490/docs

### Source Code
- **GitHub Repository**: https://github.com/elbruno/MarkItDownServer

### Getting Help
- **Issues**: https://github.com/elbruno/MarkItDownServer/issues
- **Discussions**: https://github.com/elbruno/MarkItDownServer/discussions

### Sample Code
- **Simple Console**: `samples/SimpleConsole/`
- **Detailed Console**: `samples/DetailedConsole/`

## 📝 License

This project is licensed under the MIT License. See [LICENSE](./LICENSE) for details.

---

**Happy Converting!** 🎉

For more information, visit the [project repository](https://github.com/elbruno/MarkItDownServer).
