# Concurrency and Performance Guide

This document explains the concurrency limits and performance characteristics of MarkItDown Server.

## 📊 Current Concurrency Behavior

### Default Configuration

By default, MarkItDown Server runs with:
- **Single worker process** (Uvicorn default)
- **Async request handling** via FastAPI/ASGI
- **No explicit rate limiting**
- **Concurrent requests**: Limited by system resources and async event loop

### How It Works

The server uses FastAPI with Uvicorn, which provides:
1. **Asynchronous I/O**: Multiple requests can be handled concurrently within a single worker
2. **Event loop**: Non-blocking request handling
3. **File operations**: Currently synchronous (blocking), but isolated per request

### Performance Characteristics

With the default single-worker configuration:
- ✅ **Good for**: Low to moderate traffic (10-50 concurrent requests)
- ✅ **Pros**: Simple deployment, low memory footprint (~100-200MB)
- ⚠️ **Limitations**: CPU-bound operations (file conversion) can block other requests
- ⚠️ **Not suitable for**: High-traffic production without scaling

## ⚙️ Configuration Options

### 1. Multi-Worker Configuration

Run multiple worker processes to handle more concurrent requests:

```bash
# Set the number of workers via environment variable
export WORKERS=4
python app.py
```

Or with Docker:
```bash
docker run -d \
  --name markitdownserver \
  -p 8490:8490 \
  -e WORKERS=4 \
  ghcr.io/elbruno/markitdownserver:latest
```

**Recommended worker count**: `(2 × CPU_cores) + 1`

### 2. Rate Limiting (Optional)

To prevent abuse, you can enable rate limiting:

```bash
# Limit to 10 requests per minute per IP
export ENABLE_RATE_LIMIT=true
export RATE_LIMIT="10/minute"
python app.py
```

### 3. Request Timeout

Configure timeout for long-running conversions:

```bash
# Set timeout to 300 seconds (5 minutes)
export REQUEST_TIMEOUT=300
```

## 🚀 Production Deployment Recommendations

### Small Scale (< 100 requests/minute)

**Docker with resource limits:**
```bash
docker run -d \
  --name markitdownserver \
  -p 8490:8490 \
  -e WORKERS=2 \
  --memory="512m" \
  --cpus="1.0" \
  ghcr.io/elbruno/markitdownserver:latest
```

### Medium Scale (100-1000 requests/minute)

**Kubernetes with auto-scaling:**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: markitdownserver
spec:
  replicas: 3
  template:
    spec:
      containers:
      - name: markitdownserver
        image: ghcr.io/elbruno/markitdownserver:latest
        env:
        - name: WORKERS
          value: "4"
        resources:
          requests:
            memory: "512Mi"
            cpu: "500m"
          limits:
            memory: "1Gi"
            cpu: "1000m"
```

### Large Scale (> 1000 requests/minute)

**Azure Container Apps with auto-scaling:**
```bash
az containerapp create \
  --name markitdown-server \
  --resource-group rg-markitdown \
  --environment env-markitdown \
  --image ghcr.io/elbruno/markitdownserver:latest \
  --target-port 8490 \
  --ingress external \
  --min-replicas 2 \
  --max-replicas 10 \
  --cpu 1.0 \
  --memory 2.0Gi \
  --env-vars WORKERS=4
```

Auto-scaling triggers:
- **Concurrent requests**: 10 per replica
- **CPU utilization**: 70%
- **Memory utilization**: 80%

## 📈 Benchmarking

### Testing Concurrency

Test your server's concurrency limits with Apache Bench:

```bash
# Install Apache Bench
sudo apt-get install apache2-utils  # Ubuntu/Debian
brew install httpd  # macOS

# Test with 100 concurrent requests (1000 total)
ab -n 1000 -c 100 http://localhost:8490/health

# Test file upload endpoint
ab -n 100 -c 10 -p test.pdf -T multipart/form-data \
  http://localhost:8490/process_file
```

### Load Testing with Locust

Create `locustfile.py`:
```python
from locust import HttpUser, task, between

class MarkItDownUser(HttpUser):
    wait_time = between(1, 3)
    
    @task
    def health_check(self):
        self.client.get("/health")
    
    @task(3)
    def convert_file(self):
        files = {"file": open("sample.pdf", "rb")}
        self.client.post("/process_file", files=files)
```

Run load test:
```bash
pip install locust
locust -f locustfile.py --host=http://localhost:8490
```

## 🔒 Security Considerations

### Rate Limiting

Without rate limiting, the server is vulnerable to:
- **DoS attacks**: Overwhelming the server with requests
- **Resource exhaustion**: Large file uploads consuming memory
- **Cost overruns**: Excessive API usage in cloud deployments

**Recommendation**: Enable rate limiting in production:
```bash
export ENABLE_RATE_LIMIT=true
export RATE_LIMIT="60/minute"
```

### File Size Limits

Current limit: **50MB per file**

To adjust:
```bash
export MAX_FILE_SIZE=104857600  # 100MB in bytes
```

## 📊 Monitoring

### Metrics to Monitor

1. **Request rate**: Requests per second/minute
2. **Response time**: p50, p95, p99 latency
3. **Error rate**: 4xx and 5xx responses
4. **Resource usage**: CPU, memory, disk I/O
5. **Concurrent requests**: Active requests at any time
6. **Queue depth**: Pending requests (if using queue)

### Health Check

The `/health` endpoint provides basic health status:
```bash
curl http://localhost:8490/health
```

Response:
```json
{
  "status": "healthy",
  "timestamp": "2025-10-13T23:45:18",
  "service": "MarkItDown Server",
  "version": "1.0.0"
}
```

### Application Insights (Azure)

When deployed to Azure, integrate Application Insights:
```bash
export APPLICATIONINSIGHTS_CONNECTION_STRING="..."
```

## ❓ FAQ

### Q: How many concurrent requests can the server handle?

**A**: With default configuration (1 worker):
- **Light load**: 10-50 concurrent requests
- **With 4 workers**: 40-200 concurrent requests
- **Actual limit**: Depends on file size, conversion complexity, and system resources

### Q: Does the server have a built-in concurrency limit?

**A**: No built-in hard limit by default. Limits are determined by:
- System resources (CPU, memory)
- Number of workers configured
- Optional rate limiting (if enabled)
- Deployment platform limits (e.g., Azure Container Apps)

### Q: What happens when the limit is reached?

**A**: Requests will queue up and may experience:
- Increased response time
- Timeout errors (if conversion takes too long)
- HTTP 429 errors (if rate limiting is enabled)
- HTTP 503 errors (if server is overloaded)

### Q: How can I increase the concurrency limit?

**A**: Multiple approaches:
1. Increase worker count: `WORKERS=4`
2. Scale horizontally: Deploy multiple instances
3. Optimize resources: Increase CPU/memory allocation
4. Use async file operations (future enhancement)

### Q: Is there a rate limit?

**A**: Not by default. You can enable optional rate limiting:
```bash
export ENABLE_RATE_LIMIT=true
export RATE_LIMIT="100/minute"
```

## 🎯 Best Practices

1. **Start with 2-4 workers** for production
2. **Enable rate limiting** to prevent abuse
3. **Monitor resource usage** and adjust worker count
4. **Use load balancer** for horizontal scaling
5. **Set appropriate timeouts** for long conversions
6. **Configure health checks** for auto-recovery
7. **Test under load** before production deployment

## 🔄 Future Enhancements

Planned improvements for better concurrency:
- [ ] Async file operations with aiofiles
- [ ] Redis-based request queue
- [ ] Response caching for identical files
- [ ] Streaming response for large files
- [ ] Worker pool for CPU-bound operations
- [ ] Advanced rate limiting with token bucket
- [ ] Circuit breaker for failing conversions

## 📚 Additional Resources

- [FastAPI Deployment Guide](https://fastapi.tiangolo.com/deployment/)
- [Uvicorn Deployment](https://www.uvicorn.org/deployment/)
- [Docker Production Best Practices](https://docs.docker.com/develop/dev-best-practices/)
- [Azure Container Apps Scaling](https://learn.microsoft.com/azure/container-apps/scale-app)

---

**Need help?** Open an issue on [GitHub](https://github.com/elbruno/MarkItDownServer/issues)
