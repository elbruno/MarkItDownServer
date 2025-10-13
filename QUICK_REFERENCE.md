# MarkItDown Server - Quick Reference

## 🚀 Quick Start

```bash
# Pull and run
docker pull ghcr.io/elbruno/markitdownserver:latest
docker run -d --name markitdownserver -p 8490:8490 ghcr.io/elbruno/markitdownserver:latest

# Test
curl http://localhost:8490/health
```

## 📦 Docker Commands

### Pull Image
```bash
docker pull ghcr.io/elbruno/markitdownserver:latest
```

### Run Container
```bash
# Basic
docker run -d --name markitdownserver -p 8490:8490 ghcr.io/elbruno/markitdownserver:latest

# With resource limits
docker run -d --name markitdownserver -p 8490:8490 --memory="512m" --cpus="0.5" ghcr.io/elbruno/markitdownserver:latest

# Custom port
docker run -d --name markitdownserver -p 9000:8490 ghcr.io/elbruno/markitdownserver:latest
```

### Manage Container
```bash
# Start
docker start markitdownserver

# Stop
docker stop markitdownserver

# Restart
docker restart markitdownserver

# Remove
docker rm markitdownserver

# Logs
docker logs -f markitdownserver

# Stats
docker stats markitdownserver
```

## 🌐 API Endpoints

### Base URL
```
http://localhost:8490
```

### Root
```bash
curl http://localhost:8490/
```

### Health Check
```bash
curl http://localhost:8490/health
```

### Convert File
```bash
curl -X POST "http://localhost:8490/process_file" \
  -F "file=@document.pdf"
```

### API Documentation
```
http://localhost:8490/docs
```

## 📝 Supported File Types

- **Word**: .doc, .docx
- **Excel**: .xls, .xlsx
- **PowerPoint**: .ppt, .pptx
- **PDF**: .pdf
- **OpenDocument**: .odt, .ods, .odp
- **Text**: .txt

**File Size Limit**: 50MB

## 💻 Client Code Examples

### C# (.NET)
```csharp
using System.Net.Http.Headers;

var client = new HttpClient();
var fileBytes = await File.ReadAllBytesAsync("document.pdf");
var content = new MultipartFormDataContent();
var fileContent = new ByteArrayContent(fileBytes);
fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
content.Add(fileContent, "file", "document.pdf");

var response = await client.PostAsync("http://localhost:8490/process_file", content);
var result = await response.Content.ReadAsStringAsync();
Console.WriteLine(result);
```

### Python
```python
import requests

files = {'file': open('document.pdf', 'rb')}
response = requests.post('http://localhost:8490/process_file', files=files)
print(response.json()['markdown'])
```

### JavaScript (Node.js)
```javascript
const FormData = require('form-data');
const fs = require('fs');
const axios = require('axios');

const form = new FormData();
form.append('file', fs.createReadStream('document.pdf'));

axios.post('http://localhost:8490/process_file', form, {
    headers: form.getHeaders()
}).then(res => console.log(res.data.markdown));
```

### cURL
```bash
curl -X POST "http://localhost:8490/process_file" -F "file=@document.pdf"
```

## ⚙️ Environment Variables

```bash
docker run -d \
  --name markitdownserver \
  -p 8490:8490 \
  -e PORT=8490 \
  -e MAX_FILE_SIZE=104857600 \
  -e LOG_LEVEL=INFO \
  ghcr.io/elbruno/markitdownserver:latest
```

| Variable | Default | Description |
|----------|---------|-------------|
| PORT | 8490 | Server port |
| HOST | 0.0.0.0 | Server host |
| MAX_FILE_SIZE | 52428800 | Max file size (bytes) |
| LOG_LEVEL | INFO | Log level |

## 🐳 Docker Compose

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
```

```bash
docker-compose up -d
```

## 🔍 Troubleshooting

### Check if running
```bash
docker ps | grep markitdownserver
```

### View logs
```bash
docker logs markitdownserver
```

### Test health
```bash
curl http://localhost:8490/health
```

### Restart container
```bash
docker restart markitdownserver
```

### Port conflict
```bash
# Use different port
docker run -d --name markitdownserver -p 9000:8490 ghcr.io/elbruno/markitdownserver:latest
```

## 📊 Response Format

### Success (200 OK)
```json
{
  "markdown": "# Document Title\n\nContent..."
}
```

### Error (400 Bad Request)
```json
{
  "error": "File type not allowed. Allowed types: doc, docx, pdf, ..."
}
```

### Error (413 Payload Too Large)
```json
{
  "error": "File too large. Maximum size: 50MB"
}
```

### Error (500 Internal Server Error)
```json
{
  "error": "Conversion error message"
}
```

## 📚 Resources

- **Full Documentation**: [README.md](./README.md)
- **Developer Manual**: [DEVELOPER_MANUAL.md](./DEVELOPER_MANUAL.md)
- **Code Quality Guide**: [CODE_QUALITY_IMPROVEMENTS.md](./CODE_QUALITY_IMPROVEMENTS.md)
- **Repository**: https://github.com/elbruno/MarkItDownServer
- **Issues**: https://github.com/elbruno/MarkItDownServer/issues

## 🔐 Security Notes

- Run with resource limits in production
- Use network isolation when possible
- Keep the image updated to the latest version
- Monitor logs for suspicious activity
- Configure firewall rules appropriately

## 📞 Getting Help

1. Check the [Developer Manual](./DEVELOPER_MANUAL.md)
2. Review [API Documentation](http://localhost:8490/docs)
3. Search [existing issues](https://github.com/elbruno/MarkItDownServer/issues)
4. Create a [new issue](https://github.com/elbruno/MarkItDownServer/issues/new)

---

**Version**: 1.0.0  
**Last Updated**: January 2025
