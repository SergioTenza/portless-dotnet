# External Integrations

**Analysis Date:** 2026-02-19

## APIs & External Services

**HTTP Reverse Proxy:**
- Generic HTTP/HTTPS services via YARP reverse proxy
- Dynamic route configuration through `/api/v1/add-host` endpoint
- Built-in load balancing and health checks
- HTTP/2 support via ASP.NET Core

**CLI Interface:**
- Spectre.Console for terminal output
- No external API integrations detected in CLI layer

## Data Storage

**Databases:**
- Not detected - No database configuration or ORM packages found
- Configuration stored in memory via InMemoryConfigProvider

**File Storage:**
- Local filesystem only - No cloud storage integrations

**Caching:**
- Not detected - No caching infrastructure configured

## Authentication & Identity

**Auth Provider:**
- None detected - No authentication middleware or packages
- All endpoints publicly accessible by default
- No authentication configuration in appsettings

## Monitoring & Observability

**Error Tracking:**
- Not detected - No external error tracking service integration
- Built-in ASP.NET Core logging framework only

**Logs:**
- ASP.NET Core logging framework
- Console output via Spectre.Console
- Configurable log levels via appsettings.json

## CI/CD & Deployment

**Hosting:**
- Platform not specified - Deployable as .NET 10.0 application
- Cross-platform support via .NET runtime

**CI Pipeline:**
- Not detected - No CI configuration files found (.github/workflows, azure-pipelines.yml, etc.)

## Environment Configuration

**Required env vars:**
- None detected - All configuration via appsettings.json files

**Secrets location:**
- Not configured - No secrets management integration
- Configuration hardcoded or provided via JSON files

## Webhooks & Callbacks

**Incoming:**
- `/api/v1/add-host` - Dynamic route configuration endpoint
- Accepts POST requests with route and cluster configurations

**Outgoing:**
- None detected - No webhook implementation found

---

*Integration audit: 2026-02-19*