# Docker Guide - Portless Proxy

## Quick Start

### Build the image

```bash
docker build -t portless-proxy .
```

### Run the proxy

```bash
docker run -d --name portless -p 1355:1355 portless-proxy
```

### Using Docker Compose

```bash
docker compose up -d
```

## Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `PORTLESS_PORT` | `1355` | HTTP listen port |
| `PORTLESS_HTTPS_ENABLED` | `false` | Enable HTTPS with auto-generated certs |
| `PORTLESS_STATE_DIR` | `/home/portless/.portless` | Directory for persistent state |
| `DOTNET_ENVIRONMENT` | `Production` | ASP.NET Core environment |

### Ports

- **1355** - HTTP proxy traffic
- **1356** - HTTPS proxy traffic (when enabled)

### Volumes

Mount a volume to `/home/portless/.portless` to persist route configuration across container restarts:

```bash
docker run -d --name portless \
  -p 1355:1355 \
  -v portless-state:/home/portless/.portless \
  portless-proxy
```

## Usage Examples

### With HTTPS enabled

```bash
docker run -d --name portless \
  -p 1355:1355 \
  -p 1356:1356 \
  -e PORTLESS_HTTPS_ENABLED=true \
  portless-proxy
```

### Register a route via API

```bash
curl -X POST http://localhost:1355/api/v1/routes \
  -H 'Content-Type: application/json' \
  -d '{"hostname":"myapp.localhost","targetAddress":"http://host.docker.internal:5000"}'
```

### Check health

```bash
curl http://localhost:1355/health
```

### View metrics

```bash
curl http://localhost:1355/metrics
```

## Connecting to Host Services

Use `host.docker.internal` as the hostname to reach services running on the host machine from inside the Docker container. For example, if your dev server runs on port 5000 on the host:

```bash
curl -X POST http://localhost:1355/api/v1/routes \
  -H 'Content-Type: application/json' \
  -d '{"hostname":"myapp.localhost","targetAddress":"http://host.docker.internal:5000"}'
```

Then access `http://myapp.localhost:1355` in your browser.

## Testing with the example backend

Start the proxy alongside a test nginx backend:

```bash
docker compose --profile test up -d
```

Register a route pointing to the test API:

```bash
curl -X POST http://localhost:1355/api/v1/routes \
  -H 'Content-Type: application/json' \
  -d '{"hostname":"test.localhost","targetAddress":"http://test-api:80"}'
```

## Image Details

- Multi-stage build based on `mcr.microsoft.com/dotnet/aspnet:10.0`
- Runs as non-root user (`portless`)
- Includes health check on `/health` endpoint
- Supports both HTTP and HTTPS modes
