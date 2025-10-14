# Concurrency Limits - Quick Answer

## Is there a concurrency limit?

**Yes and No** - It depends on your configuration:

### Default Configuration
By default, MarkItDown Server runs with:
- ✅ **1 worker process**
- ✅ **Async request handling** (multiple concurrent requests within the worker)
- ❌ **No explicit rate limiting**
- ⚠️ **Practical limit**: ~10-50 concurrent requests depending on system resources and file complexity

### Configuring Limits

You can configure concurrency limits using environment variables:

#### 1. Increase Worker Count (Recommended for Production)
```bash
# Run with 4 workers for better concurrency
docker run -d -p 8490:8490 -e WORKERS=4 markitdownserver:latest
```

#### 2. Enable Rate Limiting (Recommended to Prevent Abuse)
```bash
# Limit to 100 requests per minute per IP
docker run -d -p 8490:8490 \
  -e ENABLE_RATE_LIMIT=true \
  -e RATE_LIMIT=100/minute \
  markitdownserver:latest
```

**Note**: Rate limiting requires the `slowapi` package:
```bash
pip install slowapi
```

### Check Current Configuration

The `/health` endpoint shows the current concurrency settings:

```bash
curl http://localhost:8490/health
```

Response:
```json
{
  "status": "healthy",
  "timestamp": "2025-10-13T23:50:54.509932",
  "service": "MarkItDown Server",
  "version": "1.0.0",
  "workers": 1,
  "rate_limit_enabled": false,
  "rate_limit": null
}
```

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `WORKERS` | 1 | Number of worker processes |
| `ENABLE_RATE_LIMIT` | false | Enable rate limiting |
| `RATE_LIMIT` | 60/minute | Rate limit (requests per time period) |
| `PORT` | 8490 | Server port |
| `MAX_FILE_SIZE` | 52428800 | Max file size in bytes (50MB) |

### Recommended Settings

| Scale | Workers | Rate Limit | Notes |
|-------|---------|------------|-------|
| Development | 1 | Disabled | Single developer testing |
| Small (< 100 req/min) | 2 | 60/minute | Basic production use |
| Medium (100-1000 req/min) | 4 | 100/minute | Standard production |
| Large (> 1000 req/min) | 4-8 + horizontal scaling | 200/minute | High traffic, use load balancer |

### Detailed Documentation

For comprehensive information about concurrency, performance tuning, and benchmarking, see:
- 📚 [CONCURRENCY.md](./CONCURRENCY.md) - Complete concurrency guide
- 📖 [README.md](./README.md#concurrency-and-performance) - Configuration examples
- 🚀 [CODE_QUALITY_IMPROVEMENTS.md](./CODE_QUALITY_IMPROVEMENTS.md) - Azure deployment with auto-scaling

---

**Quick Start Example:**
```bash
# Production deployment with 4 workers and rate limiting
docker run -d \
  --name markitdownserver \
  -p 8490:8490 \
  -e WORKERS=4 \
  -e ENABLE_RATE_LIMIT=true \
  -e RATE_LIMIT=100/minute \
  --memory="1g" \
  --cpus="2.0" \
  ghcr.io/elbruno/markitdownserver:latest
```
