# Code Quality Improvements and Azure Deployment Guide

## Executive Summary

This document provides comprehensive recommendations for improving the MarkItDown Server codebase, upgrading to the latest package versions, and deploying to Azure Container Apps. The suggestions focus on code quality, security, performance, scalability, and production-readiness.

---

## Table of Contents

1. [Code Quality Improvements](#1-code-quality-improvements)
2. [Package Version Upgrades](#2-package-version-upgrades)
3. [Azure Container Apps Deployment](#3-azure-container-apps-deployment)
4. [Security Enhancements](#4-security-enhancements)
5. [Performance Optimizations](#5-performance-optimizations)
6. [Monitoring and Observability](#6-monitoring-and-observability)
7. [CI/CD Pipeline](#7-cicd-pipeline)

---

## 1. Code Quality Improvements

### 1.1 Python Application (`app.py`)

#### Current Issues and Recommendations

**Issue 1: Missing Type Hints**
```python
# Current
def allowed_file(filename):
    return '.' in filename and filename.rsplit('.', 1)[1].lower() in ALLOWED_EXTENSIONS

# Recommended
def allowed_file(filename: str | None) -> bool:
    """Check if the uploaded file has an allowed extension.
    
    Args:
        filename: Name of the file to check
        
    Returns:
        True if file extension is allowed, False otherwise
    """
    if not filename:
        return False
    return '.' in filename and filename.rsplit('.', 1)[1].lower() in ALLOWED_EXTENSIONS
```

**Issue 2: Potential Variable Reference Error**
The `temp_file_path` variable may not be defined if an exception occurs before it's set, causing an error in the `finally` block.

```python
# Current - problematic
async def process_file(file: UploadFile = File(...)):
    # ...
    finally:
        if os.path.exists(temp_file_path):  # temp_file_path might not be defined!
            os.remove(temp_file_path)

# Recommended
async def process_file(file: UploadFile = File(...)):
    temp_file_path = None
    if not allowed_file(file.filename):
        return JSONResponse(content={'error': 'File type not allowed'}, status_code=400)
    
    try:
        with tempfile.NamedTemporaryFile(delete=False) as temp_file:
            temp_file.write(await file.read())
            temp_file_path = temp_file.name
            logger.info(f"Temporary file path: {temp_file_path}")
        
        markdown_content = convert_to_md(temp_file_path)
        logger.info("File converted to markdown successfully")
        return JSONResponse(content={'markdown': markdown_content})
        
    except Exception as e:
        logger.error(f"An error occurred: {str(e)}")
        return JSONResponse(content={'error': str(e)}, status_code=500)
        
    finally:
        if temp_file_path and os.path.exists(temp_file_path):
            os.remove(temp_file_path)
            logger.info(f"Temporary file deleted: {temp_file_path}")
```

**Issue 3: Insufficient Input Validation**
```python
# Add file size validation
MAX_FILE_SIZE = 50 * 1024 * 1024  # 50MB

async def process_file(file: UploadFile = File(...)):
    temp_file_path = None
    
    # Validate filename
    if not file.filename:
        return JSONResponse(
            content={'error': 'Filename is required'}, 
            status_code=400
        )
    
    if not allowed_file(file.filename):
        return JSONResponse(
            content={'error': f'File type not allowed. Allowed types: {", ".join(ALLOWED_EXTENSIONS)}'}, 
            status_code=400
        )
    
    try:
        file_content = await file.read()
        
        # Validate file size
        if len(file_content) > MAX_FILE_SIZE:
            return JSONResponse(
                content={'error': f'File too large. Maximum size: {MAX_FILE_SIZE / (1024*1024):.0f}MB'}, 
                status_code=413
            )
        
        # Validate file is not empty
        if len(file_content) == 0:
            return JSONResponse(
                content={'error': 'File is empty'}, 
                status_code=400
            )
        
        with tempfile.NamedTemporaryFile(delete=False, suffix=Path(file.filename).suffix) as temp_file:
            temp_file.write(file_content)
            temp_file_path = temp_file.name
```

**Issue 4: Lack of API Documentation and Metadata**
```python
from fastapi import FastAPI, UploadFile, File, HTTPException
from fastapi.responses import JSONResponse
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel

app = FastAPI(
    title="MarkItDown Server",
    description="API for converting various document formats to Markdown",
    version="1.0.0",
    contact={
        "name": "El Bruno",
        "url": "https://github.com/elbruno/MarkItDownServer",
    },
    license_info={
        "name": "MIT",
        "url": "https://github.com/elbruno/MarkItDownServer/blob/main/LICENSE",
    },
)

# Response models
class MarkdownResponse(BaseModel):
    markdown: str

class ErrorResponse(BaseModel):
    error: str

@app.post(
    '/process_file',
    response_model=MarkdownResponse,
    responses={
        400: {"model": ErrorResponse, "description": "Invalid file type or file too large"},
        413: {"model": ErrorResponse, "description": "File too large"},
        500: {"model": ErrorResponse, "description": "Internal server error"},
    },
    summary="Convert document to Markdown",
    description="Upload a document file and receive its content in Markdown format"
)
async def process_file(
    file: UploadFile = File(..., description="Document file to convert to Markdown")
) -> MarkdownResponse:
    # ... implementation
```

**Issue 5: Missing CORS Configuration**
```python
# Add CORS middleware for web client support
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Configure appropriately for production
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)
```

**Issue 6: Better Logging Configuration**
```python
import logging
import sys
from datetime import datetime

# Better logging configuration
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    handlers=[
        logging.StreamHandler(sys.stdout),
        logging.FileHandler(f'app_{datetime.now().strftime("%Y%m%d")}.log')
    ]
)
logger = logging.getLogger(__name__)
```

**Issue 7: Health Check Endpoint**
```python
from datetime import datetime

@app.get(
    "/health",
    summary="Health check endpoint",
    description="Returns the health status of the service"
)
def health_check():
    return {
        "status": "healthy",
        "timestamp": datetime.utcnow().isoformat(),
        "service": "MarkItDown Server",
        "version": "1.0.0"
    }

@app.get("/")
def read_root():
    return {
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

### 1.2 Unused Utility Module

**Issue**: The `utils/file_handler.py` module is not being used in the application.

**Recommendation**: Either remove the unused module or integrate it:

```python
# Option 1: Remove the file if not needed

# Option 2: Integrate into app.py for better code organization
from utils.file_handler import save_temp_file, delete_file

async def process_file(file: UploadFile = File(...)):
    temp_file_path = None
    try:
        file_content = await file.read()
        temp_file_path = save_temp_file(file_content, file.filename)
        markdown_content = convert_to_md(temp_file_path)
        return JSONResponse(content={'markdown': markdown_content})
    finally:
        if temp_file_path:
            delete_file(temp_file_path)
```

### 1.3 C# Client Application

**Issue 1: No Error Handling for Network Failures**
```csharp
// Current
using System.Net.Http.Headers;

HttpClient client = new HttpClient();

// Recommended
using System.Net.Http.Headers;
using System.Text.Json;

var client = new HttpClient
{
    Timeout = TimeSpan.FromMinutes(5) // Set appropriate timeout
};

try
{
    string url = "http://127.0.0.1:8490/process_file";
    string filePath = "Benefit_Options.pdf";

    if (!File.Exists(filePath))
    {
        Console.WriteLine($"Error: File not found: {filePath}");
        return;
    }

    using var content = new MultipartFormDataContent();
    byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
    var fileContent = new ByteArrayContent(fileBytes);
    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
    content.Add(fileContent, "file", Path.GetFileName(filePath));

    var response = await client.PostAsync(url, content);

    if (response.IsSuccessStatusCode)
    {
        string responseBody = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonSerializer.Deserialize<MarkdownResponse>(responseBody);
        Console.WriteLine($"MarkDown for {filePath}:\n\n{jsonResponse?.Markdown}");
    }
    else
    {
        string errorBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Error: {response.StatusCode}");
        Console.WriteLine($"Details: {errorBody}");
    }
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"Network error: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
finally
{
    client.Dispose();
}

// Response model
record MarkdownResponse(string Markdown);
```

**Issue 2: Hardcoded URL**
```csharp
// Use configuration
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

string url = configuration["MarkItDownServer:Url"] ?? "http://127.0.0.1:8490/process_file";
```

---

## 2. Package Version Upgrades

### 2.1 Python Dependencies

**Current `requirements.txt`:**
```
markitdown
fastapi
python-multipart
uvicorn
```

**Recommended `requirements.txt` with specific versions:**
```
# Core dependencies with latest stable versions
fastapi==0.119.0
uvicorn[standard]==0.34.0
python-multipart==0.0.20
markitdown==0.1.3

# Additional recommended dependencies
pydantic==2.10.6
pydantic-settings==2.7.1

# Production server
gunicorn==23.0.0

# Type checking and code quality (dev dependencies)
mypy==1.14.0
black==25.1.0
ruff==0.9.3
pytest==8.3.5
pytest-asyncio==0.25.2
httpx==0.28.1  # for testing FastAPI

# Security scanning
safety==3.3.1
bandit==1.8.0
```

**Create `requirements-dev.txt` for development:**
```
-r requirements.txt
mypy==1.14.0
black==25.1.0
ruff==0.9.3
pytest==8.3.5
pytest-asyncio==0.25.2
httpx==0.28.1
safety==3.3.1
bandit==1.8.0
```

**Create `pyproject.toml` for tool configuration:**
```toml
[tool.black]
line-length = 100
target-version = ['py312']

[tool.ruff]
line-length = 100
target-version = "py312"
select = ["E", "F", "I", "N", "W", "B", "C4"]

[tool.mypy]
python_version = "3.12"
strict = true
warn_return_any = true
warn_unused_configs = true

[tool.pytest.ini_options]
asyncio_mode = "auto"
testpaths = ["tests"]
```

### 2.2 .NET Dependencies

The C# project is already using .NET 9.0, which is the latest version. No upgrades needed.

---

## 3. Azure Container Apps Deployment

### 3.1 Improved Dockerfile

**Current Issues:**
- Uses generic port mapping
- No health check
- No non-root user
- Missing security best practices

**Recommended Dockerfile:**
```dockerfile
# Multi-stage build for smaller image size
FROM python:3.12-slim AS builder

# Set working directory
WORKDIR /app

# Install system dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    gcc \
    && rm -rf /var/lib/apt/lists/*

# Copy requirements first for better caching
COPY requirements.txt .

# Install Python dependencies
RUN pip install --no-cache-dir --user -r requirements.txt

# Final stage
FROM python:3.12-slim

# Create non-root user
RUN useradd -m -u 1000 appuser && \
    mkdir -p /app /tmp/uploads && \
    chown -R appuser:appuser /app /tmp/uploads

# Set working directory
WORKDIR /app

# Copy Python dependencies from builder
COPY --from=builder --chown=appuser:appuser /root/.local /home/appuser/.local

# Copy application code
COPY --chown=appuser:appuser app.py .
COPY --chown=appuser:appuser utils/ ./utils/

# Set PATH to include user-installed packages
ENV PATH=/home/appuser/.local/bin:$PATH \
    PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    PORT=8080

# Switch to non-root user
USER appuser

# Expose port (Azure Container Apps uses 8080 by default)
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD python -c "import urllib.request; urllib.request.urlopen('http://localhost:8080/health')"

# Run the application
CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "8080"]
```

**Create `.dockerignore`:**
```
__pycache__
*.pyc
*.pyo
*.pyd
.Python
*.so
*.egg
*.egg-info
dist
build
.git
.gitignore
.vscode
.idea
*.md
!README.md
tests
.pytest_cache
.coverage
htmlcov
.env
.venv
venv
env
.DS_Store
*.log
src/
images/
```

### 3.2 Azure Container Apps Deployment Scripts

**Create `azure-deploy.sh`:**
```bash
#!/bin/bash

# Azure Container Apps Deployment Script
# Prerequisites: Azure CLI installed and logged in

set -e

# Configuration
RESOURCE_GROUP="rg-markitdown"
LOCATION="eastus"
CONTAINER_APP_ENV="env-markitdown"
CONTAINER_APP_NAME="markitdown-server"
CONTAINER_REGISTRY="crmarkitdown"
IMAGE_NAME="markitdown-server"
IMAGE_TAG="latest"

echo "🚀 Starting Azure Container Apps deployment..."

# 1. Create Resource Group
echo "📦 Creating resource group..."
az group create \
    --name $RESOURCE_GROUP \
    --location $LOCATION

# 2. Create Azure Container Registry (ACR)
echo "🏗️  Creating Azure Container Registry..."
az acr create \
    --resource-group $RESOURCE_GROUP \
    --name $CONTAINER_REGISTRY \
    --sku Basic \
    --admin-enabled true

# 3. Build and push Docker image to ACR
echo "🔨 Building and pushing Docker image..."
az acr build \
    --registry $CONTAINER_REGISTRY \
    --image $IMAGE_NAME:$IMAGE_TAG \
    --file dockerfile \
    .

# 4. Create Container Apps Environment
echo "🌍 Creating Container Apps environment..."
az containerapp env create \
    --name $CONTAINER_APP_ENV \
    --resource-group $RESOURCE_GROUP \
    --location $LOCATION

# 5. Get ACR credentials
ACR_LOGIN_SERVER=$(az acr show --name $CONTAINER_REGISTRY --query loginServer --output tsv)
ACR_USERNAME=$(az acr credential show --name $CONTAINER_REGISTRY --query username --output tsv)
ACR_PASSWORD=$(az acr credential show --name $CONTAINER_REGISTRY --query passwords[0].value --output tsv)

# 6. Create Container App
echo "🚢 Creating Container App..."
az containerapp create \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --environment $CONTAINER_APP_ENV \
    --image "${ACR_LOGIN_SERVER}/${IMAGE_NAME}:${IMAGE_TAG}" \
    --registry-server $ACR_LOGIN_SERVER \
    --registry-username $ACR_USERNAME \
    --registry-password $ACR_PASSWORD \
    --target-port 8080 \
    --ingress external \
    --min-replicas 1 \
    --max-replicas 10 \
    --cpu 0.5 \
    --memory 1.0Gi \
    --env-vars \
        PORT=8080 \
        PYTHONUNBUFFERED=1

# 7. Get the Container App URL
echo "✅ Deployment complete!"
CONTAINER_APP_URL=$(az containerapp show \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --query properties.configuration.ingress.fqdn \
    --output tsv)

echo ""
echo "🎉 Your MarkItDown Server is now running!"
echo "📍 URL: https://${CONTAINER_APP_URL}"
echo ""
echo "To test the service:"
echo "  curl https://${CONTAINER_APP_URL}/health"
```

**Create `azure-update.sh` for updates:**
```bash
#!/bin/bash

# Update existing Container App with new image
set -e

RESOURCE_GROUP="rg-markitdown"
CONTAINER_APP_NAME="markitdown-server"
CONTAINER_REGISTRY="crmarkitdown"
IMAGE_NAME="markitdown-server"
IMAGE_TAG=$(date +%Y%m%d-%H%M%S)

echo "🔄 Updating Container App..."

# Build and push new image
echo "🔨 Building new image..."
az acr build \
    --registry $CONTAINER_REGISTRY \
    --image $IMAGE_NAME:$IMAGE_TAG \
    --image $IMAGE_NAME:latest \
    --file dockerfile \
    .

# Update container app
echo "📦 Updating Container App..."
az containerapp update \
    --name $CONTAINER_APP_NAME \
    --resource-group $RESOURCE_GROUP \
    --image "${CONTAINER_REGISTRY}.azurecr.io/${IMAGE_NAME}:${IMAGE_TAG}"

echo "✅ Update complete!"
```

**Create `bicep/main.bicep` for Infrastructure as Code:**
```bicep
@description('Location for all resources')
param location string = resourceGroup().location

@description('Name of the Container App')
param containerAppName string = 'markitdown-server'

@description('Name of the Container Registry')
param containerRegistryName string = 'crmarkitdown'

@description('Name of the Container App Environment')
param environmentName string = 'env-markitdown'

@description('Container image')
param containerImage string = 'markitdown-server:latest'

// Container Registry
resource containerRegistry 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: containerRegistryName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
}

// Container Apps Environment
resource environment 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: environmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
    }
  }
}

// Container App
resource containerApp 'Microsoft.App/containerApps@2023-05-01' = {
  name: containerAppName
  location: location
  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
      }
      registries: [
        {
          server: containerRegistry.properties.loginServer
          username: containerRegistry.listCredentials().username
          passwordSecretRef: 'registry-password'
        }
      ]
      secrets: [
        {
          name: 'registry-password'
          value: containerRegistry.listCredentials().passwords[0].value
        }
      ]
    }
    template: {
      containers: [
        {
          name: containerAppName
          image: '${containerRegistry.properties.loginServer}/${containerImage}'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            {
              name: 'PORT'
              value: '8080'
            }
            {
              name: 'PYTHONUNBUFFERED'
              value: '1'
            }
          ]
          probes: [
            {
              type: 'liveness'
              httpGet: {
                path: '/health'
                port: 8080
              }
              initialDelaySeconds: 10
              periodSeconds: 30
            }
            {
              type: 'readiness'
              httpGet: {
                path: '/health'
                port: 8080
              }
              initialDelaySeconds: 5
              periodSeconds: 10
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 10
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '10'
              }
            }
          }
        ]
      }
    }
  }
}

output containerAppFQDN string = containerApp.properties.configuration.ingress.fqdn
output containerRegistryLoginServer string = containerRegistry.properties.loginServer
```

### 3.3 Environment Configuration

**Create `.env.example`:**
```
# Server Configuration
PORT=8080
HOST=0.0.0.0

# Application Settings
MAX_FILE_SIZE=52428800  # 50MB in bytes
ALLOWED_EXTENSIONS=doc,docx,ppt,pptx,pdf,xls,xlsx,odt,ods,odp,txt

# Logging
LOG_LEVEL=INFO
LOG_FORMAT=json

# CORS (comma-separated origins)
CORS_ORIGINS=*

# Azure Application Insights (optional)
APPLICATIONINSIGHTS_CONNECTION_STRING=

# Health Check
HEALTH_CHECK_ENABLED=true
```

**Update `app.py` to use environment variables:**
```python
from pydantic_settings import BaseSettings
from functools import lru_cache

class Settings(BaseSettings):
    port: int = 8080
    host: str = "0.0.0.0"
    max_file_size: int = 50 * 1024 * 1024  # 50MB
    allowed_extensions: set[str] = {
        'doc', 'docx', 'ppt', 'pptx', 'pdf', 
        'xls', 'xlsx', 'odt', 'ods', 'odp', 'txt'
    }
    log_level: str = "INFO"
    cors_origins: list[str] = ["*"]
    
    class Config:
        env_file = ".env"
        case_sensitive = False

@lru_cache
def get_settings():
    return Settings()

settings = get_settings()
MAX_FILE_SIZE = settings.max_file_size
ALLOWED_EXTENSIONS = settings.allowed_extensions
```

---

## 4. Security Enhancements

### 4.1 Rate Limiting

**Install slowapi:**
```bash
pip install slowapi
```

**Add to `app.py`:**
```python
from slowapi import Limiter, _rate_limit_exceeded_handler
from slowapi.util import get_remote_address
from slowapi.errors import RateLimitExceeded

limiter = Limiter(key_func=get_remote_address)
app.state.limiter = limiter
app.add_exception_handler(RateLimitExceeded, _rate_limit_exceeded_handler)

@app.post('/process_file')
@limiter.limit("10/minute")  # 10 requests per minute per IP
async def process_file(request: Request, file: UploadFile = File(...)):
    # ... implementation
```

### 4.2 API Key Authentication (Optional)

```python
from fastapi import Security, HTTPException, status
from fastapi.security import APIKeyHeader

API_KEY_NAME = "X-API-Key"
api_key_header = APIKeyHeader(name=API_KEY_NAME, auto_error=False)

async def get_api_key(api_key: str = Security(api_key_header)):
    if not api_key or api_key != os.getenv("API_KEY"):
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Invalid or missing API Key"
        )
    return api_key

@app.post('/process_file')
async def process_file(
    file: UploadFile = File(...),
    api_key: str = Depends(get_api_key)  # Add when API key is required
):
    # ... implementation
```

### 4.3 Security Headers

```python
from fastapi.middleware.trustedhost import TrustedHostMiddleware
from starlette.middleware.sessions import SessionMiddleware

# Add security middleware
app.add_middleware(
    TrustedHostMiddleware, 
    allowed_hosts=["*.azurecontainerapps.io", "localhost"]
)

@app.middleware("http")
async def add_security_headers(request: Request, call_next):
    response = await call_next(request)
    response.headers["X-Content-Type-Options"] = "nosniff"
    response.headers["X-Frame-Options"] = "DENY"
    response.headers["X-XSS-Protection"] = "1; mode=block"
    response.headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains"
    return response
```

### 4.4 Dependency Scanning

**Create `azure-pipelines.yml` for CI/CD with security scanning:**
```yaml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: UsePythonVersion@0
  inputs:
    versionSpec: '3.12'
    
- script: |
    pip install -r requirements.txt
    pip install safety bandit
  displayName: 'Install dependencies'

- script: |
    safety check --json
  displayName: 'Security: Check for vulnerable dependencies'
  continueOnError: true

- script: |
    bandit -r app.py -f json -o bandit-report.json
  displayName: 'Security: Static code analysis with Bandit'
  continueOnError: true

- script: |
    docker build -t markitdown-server:$(Build.BuildId) .
  displayName: 'Build Docker image'
```

---

## 5. Performance Optimizations

### 5.1 Async File Operations

```python
import aiofiles

async def process_file(file: UploadFile = File(...)):
    temp_file_path = None
    try:
        file_content = await file.read()
        
        # Use async file operations
        async with aiofiles.tempfile.NamedTemporaryFile(
            mode='wb', 
            delete=False, 
            suffix=Path(file.filename).suffix
        ) as temp_file:
            await temp_file.write(file_content)
            temp_file_path = temp_file.name
```

### 5.2 Caching Strategy

```python
from functools import lru_cache
from hashlib import md5

# Cache conversion results for identical files
conversion_cache = {}

def get_file_hash(content: bytes) -> str:
    return md5(content).hexdigest()

async def process_file(file: UploadFile = File(...)):
    file_content = await file.read()
    file_hash = get_file_hash(file_content)
    
    # Check cache
    if file_hash in conversion_cache:
        logger.info("Cache hit for file")
        return JSONResponse(content={'markdown': conversion_cache[file_hash]})
    
    # Process and cache
    markdown_content = convert_to_md(temp_file_path)
    conversion_cache[file_hash] = markdown_content
    
    return JSONResponse(content={'markdown': markdown_content})
```

### 5.3 Connection Pooling

```python
# Use production-grade server
# Update CMD in Dockerfile:
CMD ["gunicorn", "app:app", "--workers", "4", "--worker-class", "uvicorn.workers.UvicornWorker", "--bind", "0.0.0.0:8080"]
```

---

## 6. Monitoring and Observability

### 6.1 Azure Application Insights Integration

```python
# Add to requirements.txt
# opencensus-ext-azure==1.1.13

from opencensus.ext.azure.log_exporter import AzureLogHandler
from opencensus.ext.azure import metrics_exporter
from opencensus.stats import aggregation as aggregation_module
from opencensus.stats import measure as measure_module
from opencensus.stats import stats as stats_module
from opencensus.stats import view as view_module
from opencensus.tags import tag_map as tag_map_module

# Configure Application Insights
connection_string = os.getenv("APPLICATIONINSIGHTS_CONNECTION_STRING")
if connection_string:
    logger.addHandler(AzureLogHandler(connection_string=connection_string))
```

### 6.2 Structured Logging

```python
import json
from datetime import datetime

class JSONFormatter(logging.Formatter):
    def format(self, record):
        log_data = {
            "timestamp": datetime.utcnow().isoformat(),
            "level": record.levelname,
            "logger": record.name,
            "message": record.getMessage(),
            "module": record.module,
            "function": record.funcName,
        }
        if record.exc_info:
            log_data["exception"] = self.formatException(record.exc_info)
        return json.dumps(log_data)

handler = logging.StreamHandler(sys.stdout)
handler.setFormatter(JSONFormatter())
logger.addHandler(handler)
```

### 6.3 Metrics Endpoint

```python
from prometheus_client import Counter, Histogram, generate_latest
from prometheus_client import CollectorRegistry, CONTENT_TYPE_LATEST

# Metrics
REQUEST_COUNT = Counter('http_requests_total', 'Total HTTP requests')
REQUEST_DURATION = Histogram('http_request_duration_seconds', 'HTTP request duration')
CONVERSION_COUNT = Counter('conversions_total', 'Total conversions', ['file_type'])

@app.get("/metrics")
def metrics():
    return Response(generate_latest(), media_type=CONTENT_TYPE_LATEST)
```

---

## 7. CI/CD Pipeline

### 7.1 GitHub Actions Workflow

**Create `.github/workflows/deploy.yml`:**
```yaml
name: Build and Deploy to Azure Container Apps

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

env:
  AZURE_CONTAINER_REGISTRY: crmarkitdown
  CONTAINER_APP_NAME: markitdown-server
  RESOURCE_GROUP: rg-markitdown
  IMAGE_NAME: markitdown-server

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Set up Python
      uses: actions/setup-python@v5
      with:
        python-version: '3.12'
        
    - name: Install dependencies
      run: |
        pip install -r requirements.txt
        pip install -r requirements-dev.txt
        
    - name: Run linting
      run: |
        ruff check app.py
        black --check app.py
        
    - name: Run type checking
      run: mypy app.py
      
    - name: Run security checks
      run: |
        safety check
        bandit -r app.py
        
    - name: Run tests
      run: pytest tests/
  
  build-and-push:
    needs: build-and-test
    runs-on: ubuntu-latest
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Log in to Azure
      uses: azure/login@v2
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}
    
    - name: Build and push to ACR
      run: |
        az acr build \
          --registry ${{ env.AZURE_CONTAINER_REGISTRY }} \
          --image ${{ env.IMAGE_NAME }}:${{ github.sha }} \
          --image ${{ env.IMAGE_NAME }}:latest \
          --file dockerfile \
          .
  
  deploy:
    needs: build-and-push
    runs-on: ubuntu-latest
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'
    
    steps:
    - name: Log in to Azure
      uses: azure/login@v2
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}
    
    - name: Deploy to Azure Container Apps
      run: |
        az containerapp update \
          --name ${{ env.CONTAINER_APP_NAME }} \
          --resource-group ${{ env.RESOURCE_GROUP }} \
          --image ${{ env.AZURE_CONTAINER_REGISTRY }}.azurecr.io/${{ env.IMAGE_NAME }}:${{ github.sha }}
    
    - name: Verify deployment
      run: |
        FQDN=$(az containerapp show \
          --name ${{ env.CONTAINER_APP_NAME }} \
          --resource-group ${{ env.RESOURCE_GROUP }} \
          --query properties.configuration.ingress.fqdn \
          --output tsv)
        
        echo "Deployed to: https://${FQDN}"
        curl -f https://${FQDN}/health || exit 1
```

---

## 8. Testing Strategy

### 8.1 Unit Tests

**Create `tests/test_app.py`:**
```python
import pytest
from fastapi.testclient import TestClient
from app import app

client = TestClient(app)

def test_read_root():
    response = client.get("/")
    assert response.status_code == 200
    assert "service" in response.json()

def test_health_check():
    response = client.get("/health")
    assert response.status_code == 200
    assert response.json()["status"] == "healthy"

def test_process_file_invalid_extension():
    response = client.post(
        "/process_file",
        files={"file": ("test.invalid", b"content", "application/octet-stream")}
    )
    assert response.status_code == 400
    assert "error" in response.json()

def test_process_file_empty_file():
    response = client.post(
        "/process_file",
        files={"file": ("test.txt", b"", "text/plain")}
    )
    assert response.status_code == 400

@pytest.mark.asyncio
async def test_file_cleanup():
    # Test that temporary files are cleaned up
    import tempfile
    import os
    
    temp_count_before = len(os.listdir(tempfile.gettempdir()))
    
    response = client.post(
        "/process_file",
        files={"file": ("test.txt", b"test content", "text/plain")}
    )
    
    temp_count_after = len(os.listdir(tempfile.gettempdir()))
    assert temp_count_after <= temp_count_before
```

---

## 9. Documentation Updates

### 9.1 Update README.md

Add sections for:
- API documentation link
- Environment variables
- Azure deployment instructions
- Security considerations
- Production deployment checklist

### 9.2 Create API Documentation

The FastAPI automatic documentation will be available at:
- Swagger UI: `https://your-app.azurecontainerapps.io/docs`
- ReDoc: `https://your-app.azurecontainerapps.io/redoc`

---

## 10. Implementation Checklist

### Phase 1: Code Quality (High Priority)
- [ ] Fix variable initialization bug in `app.py`
- [ ] Add comprehensive input validation
- [ ] Add type hints throughout codebase
- [ ] Implement proper error handling
- [ ] Add health check endpoint
- [ ] Add API documentation with Pydantic models
- [ ] Configure CORS appropriately

### Phase 2: Security (High Priority)
- [ ] Add security headers middleware
- [ ] Implement rate limiting
- [ ] Add file size validation
- [ ] Review and restrict CORS origins
- [ ] Add .dockerignore file
- [ ] Implement non-root user in Docker
- [ ] Add dependency scanning to CI/CD

### Phase 3: Azure Deployment (Medium Priority)
- [ ] Update Dockerfile with multi-stage build
- [ ] Create deployment scripts
- [ ] Set up Azure Container Registry
- [ ] Create Container Apps environment
- [ ] Configure environment variables
- [ ] Set up health probes
- [ ] Configure auto-scaling rules

### Phase 4: Monitoring (Medium Priority)
- [ ] Integrate Application Insights
- [ ] Add structured logging
- [ ] Create metrics endpoint
- [ ] Set up alerts for errors
- [ ] Configure log analytics

### Phase 5: CI/CD (Low Priority)
- [ ] Create GitHub Actions workflow
- [ ] Add automated testing
- [ ] Add security scanning
- [ ] Configure deployment automation
- [ ] Add rollback capability

### Phase 6: Performance (Low Priority)
- [ ] Implement async file operations
- [ ] Add caching strategy
- [ ] Use production-grade server (Gunicorn)
- [ ] Optimize Docker image size
- [ ] Load testing and optimization

---

## 11. Cost Estimation (Azure)

**Estimated Monthly Costs:**
- Container Apps (1-10 replicas, 0.5 vCPU, 1GB memory): ~$30-150/month
- Container Registry (Basic): ~$5/month
- Log Analytics: ~$10-50/month (depending on usage)
- **Total: ~$45-205/month**

**Cost Optimization Tips:**
- Use Azure Consumption plan for development/testing
- Implement proper scaling rules to minimize idle resources
- Use spot instances when appropriate
- Monitor and optimize based on actual usage

---

## 12. Additional Recommendations

### 12.1 Code Organization
- Consider splitting `app.py` into multiple modules (routes, models, services)
- Create a proper package structure with `__init__.py` files
- Add configuration management module

### 12.2 Documentation
- Add inline code comments for complex logic
- Create architecture diagram
- Document API endpoints comprehensively
- Add troubleshooting guide

### 12.3 Development Workflow
- Set up pre-commit hooks for code formatting and linting
- Create local development environment with Docker Compose
- Add VS Code debugging configuration
- Create development guidelines document

---

## Conclusion

This document provides a comprehensive roadmap for improving the MarkItDown Server codebase and deploying it to Azure Container Apps. The recommendations are prioritized based on impact and urgency:

1. **High Priority**: Security and code quality improvements
2. **Medium Priority**: Azure deployment and monitoring
3. **Low Priority**: Performance optimizations and advanced features

Start with the high-priority items in Phase 1 and Phase 2, then proceed to deployment in Phase 3. The modular approach allows you to implement improvements incrementally without disrupting the current functionality.

For questions or assistance with implementation, refer to:
- FastAPI documentation: https://fastapi.tiangolo.com/
- Azure Container Apps documentation: https://learn.microsoft.com/azure/container-apps/
- Python security best practices: https://owasp.org/www-project-top-ten/
