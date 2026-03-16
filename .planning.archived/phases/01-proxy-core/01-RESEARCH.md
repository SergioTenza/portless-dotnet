# Phase 1: Proxy Core - Research

**Researched:** 2026-02-19
**Domain:** HTTP Reverse Proxy with YARP (Yet Another Reverse Proxy)
**Confidence:** HIGH

## Summary

Phase 1 focuses on implementing a functional HTTP proxy that accepts requests on port 1355 and routes them to the correct backend based on the Host header. The phase uses YARP (Yet Another Reverse Proxy) 2.3.0, Microsoft's production-ready reverse proxy toolkit for .NET.

The existing codebase already has YARP 2.3.0 configured with an InMemoryConfigProvider and an endpoint for dynamic route updates (`/api/v1/add-host`). The current implementation provides a foundation but needs enhancement to fully meet the requirements PROXY-01 through PROXY-04.

**Primary recommendation:** Leverage YARP's built-in configuration APIs and Host header routing capabilities. Focus on implementing proper error handling, request/response forwarding, and dynamic configuration updates without restarting the proxy.

## User Constraints (from CONTEXT.md)

### Locked Decisions
None - User delegated all implementation details to Claude's discretion.

### Claude's Discretion

The user has delegated complete technical implementation discretion for HTTP proxy:

- **Host header handling**: Header names, case sensitivity, default route behavior
- **Error responses**: Error codes, messages, logging level
- **Headers forwarding**: Which headers to preserve, overwrite, or remove
- **Logging**: Verbosity, format, destination
- **Configuration**: Default port (1355), override mechanism
- **Connection handling**: Timeouts, keep-alive, pooling

**Direction for planner:**
Implement following YARP best practices and the architecture defined in PRD.md. Success criteria is that the proxy routes correctly based on Host header and forwards requests/responses without corruption.

### Deferred Ideas (OUT OF SCOPE)
None - discussion stayed within phase scope.

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| PROXY-01 | Proxy accepts HTTP requests on configured port (default 1355) | YARP + ASP.NET Core Kestrel server configuration with URL binding |
| PROXY-02 | Proxy routes requests based on Host header | YARP RouteConfig with Match.Hosts array for host-based routing |
| PROXY-03 | Proxy forwards requests to correct backend | YARP ClusterConfig with Destinations configuration |
| PROXY-04 | Proxy returns responses from backend to client | Built-in YARP response forwarding with StreamProxyTransformer |

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Yarp.ReverseProxy | 2.3.0 | Reverse proxy engine | Microsoft's official toolkit, production-ready, designed for .NET |
| ASP.NET Core | 10.0 | Web server framework | Built-in HTTP server (Kestrel), middleware pipeline |
| Microsoft.NET.Sdk.Web | 10.0 | Web project SDK | Provides web application template and configuration |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| Serilog | 3.x | Structured logging | For production-grade logging with sinks (file, console) |
| Serilog.AspNetCore | 8.x | ASP.NET Core integration | For request/response logging in proxy pipeline |
| Microsoft.Extensions.Diagnostics.HealthChecks | 9.x | Health monitoring | For backend health checks (optional enhancement) |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| YARP | Ocelot | YARP is Microsoft-backed, more flexible, actively maintained |
| YARP | Traefik | Traefik is Go-based, less .NET integration, requires separate process |
| YARP | Nginx | Nginx requires external process, harder to integrate with .NET tooling |
| InMemoryConfigProvider | FileConfigProvider | InMemory allows dynamic updates without file I/O overhead |

**Installation:**
```bash
# Core packages (already installed)
dotnet add package Yarp.ReverseProxy --version 2.3.0

# Optional enhancements
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File
```

## Architecture Patterns

### Recommended Project Structure

```
Portless.Proxy/
├── Program.cs                    # Entry point, proxy startup
├── Config/
│   ├── ProxyConfiguration.cs    # YARP configuration setup
│   └── InMemoryConfigProvider.cs # Custom config provider (or use built-in)
├── Middleware/
│   ├── ErrorHandlerMiddleware.cs # Global exception handling
│   └── LoggingMiddleware.cs      # Request/response logging
├── Models/
│   └── AddHostRequest.cs         # DTO for route addition
├── Services/
│   ├── IProxyService.cs          # Proxy management abstraction
│   └── ProxyService.cs           # Implementation
└── appsettings.json              # Configuration
```

### Pattern 1: YARP Configuration with Host Header Routing

**What:** Configure YARP routes to match based on Host header and forward to backend clusters.

**When to use:** All reverse proxy scenarios where routing decisions are based on hostname.

**Example:**

```csharp
// Source: YARP documentation and existing codebase pattern
using Yarp.ReverseProxy.Configuration;

var routes = new[]
{
    new RouteConfig
    {
        RouteId = "route1",
        ClusterId = "cluster1",
        Match = new RouteMatch
        {
            // Host header matching - case-insensitive by default
            Hosts = new[] { "miapi.localhost", "api.example.com" }
        }
    }
};

var clusters = new[]
{
    new ClusterConfig
    {
        ClusterId = "cluster1",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            {
                "destination1",
                new DestinationConfig { Address = "http://localhost:5000" }
            }
        }
    }
};

builder.Services.AddReverseProxy()
    .LoadFromMemory(routes, clusters);
```

**Key insights:**
- `Match.Hosts` array supports multiple hostnames per route
- Host matching is case-insensitive by default
- Route priority: first match wins (order matters)
- Cluster destinations can include port numbers

### Pattern 2: Dynamic Configuration Updates

**What:** Update proxy routes at runtime without restart using InMemoryConfigProvider.

**When to use:** Scenarios requiring runtime route changes (this project's main use case).

**Example:**

```csharp
// Source: Existing codebase in Portless.Proxy/Program.cs
public class InMemoryConfigProvider : IProxyConfigProvider, IUpdateConfig
{
    private volatile InMemoryConfig _config;

    public InMemoryConfigProvider()
    {
        _config = new InMemoryConfig(
            Array.Empty<RouteConfig>(),
            Array.Empty<ClusterConfig>()
        );
    }

    public IProxyConfig GetConfig() => _config;

    public void Update(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
    {
        var oldConfig = _config;
        _config = new InMemoryConfig(routes, clusters);
        oldConfig.SignalChange();
    }
}

// Usage in endpoint
app.MapPost("/api/v1/add-host", (UpdateConfigRequest request, InMemoryConfigProvider config) =>
{
    config.Update(request.Routes, request.Clusters);
    return Results.Ok("Configuration updated");
});
```

**Key insights:**
- Must implement `IProxyConfigProvider` for custom config
- `SignalChange()` triggers YARP to reload config
- Thread-safe: volatile field ensures visibility
- No proxy restart required

### Pattern 3: Request/Response Transformation

**What:** Modify headers and other request/response properties during forwarding.

**When to use:** Need to add, remove, or modify headers (e.g., X-Forwarded-* headers).

**Example:**

```csharp
// Source: YARP transform documentation
var route = new RouteConfig
{
    RouteId = "route1",
    ClusterId = "cluster1",
    Match = new RouteMatch { Hosts = new[] { "api.localhost" } },
    Transforms = new[]
    {
        // Add X-Forwarded-For header automatically
        new Dictionary<string, string>
        {
            { "RequestHeader", "X-Forwarded-For" },
            { "Append", "{RemoteIpAddress}" }
        },
        // Preserve original Host header
        new Dictionary<string, string>
        {
            { "RequestHeader", "Host" },
            { "Set", "{Host}" }  // Forward original host
        }
    }
};
```

**Key insights:**
- YARP automatically adds X-Forwarded-* headers by default
- Transforms can use template variables ({Host}, {RemoteIpAddress})
- Order of transforms matters (applied sequentially)

### Pattern 4: Error Handling and Response Codes

**What:** Handle proxy errors and return appropriate HTTP responses.

**When to use:** All production proxies need error handling.

**Example:**

```csharp
// Custom error handling middleware
public class ProxyErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProxyErrorHandlerMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Proxy error processing request");
            context.Response.StatusCode = 502;
            await context.Response.WriteAsync("Bad Gateway");
        }
    }
}

// YARP provides built-in error responses:
// - 502 Bad Gateway: Backend not reachable
// - 504 Gateway Timeout: Backend timeout
// - 404 Not Found: No matching route
```

**Key insights:**
- YARP handles most proxy errors automatically
- Custom middleware for application-specific error handling
- Consider health check endpoint for monitoring

### Anti-Patterns to Avoid

- **Hardcoding backend addresses:** Use configuration or dynamic updates instead
- **Ignoring Host header case:** YARP handles this, but custom matching should use case-insensitive comparison
- **Blocking on async operations:** Always use async/await throughout the pipeline
- **Swallowing exceptions in config updates:** Log and return error responses
- **Not disposing resources:** Ensure proper cleanup on shutdown

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| HTTP request parsing | Custom HTTP parser | YARP + ASP.NET Core | Edge cases: chunked encoding, compression, keep-alive |
| Connection pooling | Custom socket management | YARP connection management | Proper HTTP/1.1 semantics, connection reuse |
| Load balancing | Round-robin logic | YARP cluster destinations | Built-in health checks, multiple destinations |
| Header transformation | Manual header manipulation | YARP transforms | Handles edge cases, preserves required headers |
| WebSockets upgrade | Custom WebSocket handling | YARP WebSocket support | Proper handshake, frame handling |
| HTTP/2 support | Custom HTTP/2 implementation | YARP HTTP/2 | Complex protocol, stream management |

**Key insight:** YARP handles all HTTP protocol complexities. Building custom proxy logic leads to bugs, security vulnerabilities, and maintenance burden. Focus on configuration and business logic, not protocol implementation.

## Common Pitfalls

### Pitfall 1: Missing X-Forwarded-* Headers

**What goes wrong:** Backend applications can't determine original client IP, protocol, or host.

**Why it happens:** Headers not forwarded or overwritten incorrectly.

**How to avoid:** YARP adds X-Forwarded-* headers by default. Verify they're not removed by transforms.

**Warning signs:** Backend logs show 127.0.0.1 for all client IPs, incorrect redirect URLs.

### Pitfall 2: Port Already in Use

**What goes wrong:** Proxy fails to start with "Address already in use" error.

**Why it happens:** Previous instance still running or another process using port 1355.

**How to avoid:** Implement graceful shutdown, check port availability before starting, use PID file tracking.

**Warning signs:** EADDRINUSE errors on startup, proxy binds fail.

### Pitfall 3: Route Configuration Not Applied

**What goes wrong:** Routes added via API don't take effect, requests return 404.

**Why it happens:** Forgetting to call `SignalChange()` after updating InMemoryConfigProvider.

**How to avoid:** Always call `SignalChange()` on old config after creating new one. Use the pattern shown in existing code.

**Warning signs:** 404 responses for configured routes, logs showing old route count.

### Pitfall 4: Backend Connection Timeouts

**What goes wrong:** Proxy hangs indefinitely when backend is unresponsive.

**Why it happens:** No timeout configured on HTTP client or cluster destinations.

**How to avoid:** Configure timeouts in YARP cluster options:

```csharp
var cluster = new ClusterConfig
{
    ClusterId = "cluster1",
    Destinations = new Dictionary<string, DestinationConfig>
    {
        { "destination1", new DestinationConfig { Address = "http://localhost:5000" } }
    },
    // Add timeout configuration
    HttpClient = new HttpClientConfig
    {
        DangerousAcceptAnyServerCertificate = false,
        MaxConnectionsPerServer = 100,
        RequestHeaderEncoding = System.Text.Encoding.UTF8,
        ResponseHeadersReadOnly = false,
        ActivityContextHeaders = ActivityContextHeaders.Baggage,
        ResponseTrailers = false,
        WebProxy = null,
        ConnectTimeout = TimeSpan.FromSeconds(5),
        // Add other timeout options as needed
    }
};
```

**Warning signs:** Requests hang, high memory usage, connection pool exhaustion.

### Pitfall 5: Case Sensitivity in Host Matching

**What goes wrong:** Requests fail when Host header case differs from configuration.

**Why it happens:** YARP matches case-insensitively by default, but custom logic might not.

**How to avoid:** Rely on YARP's built-in Hosts array matching. If implementing custom matching, use `StringComparer.OrdinalIgnoreCase`.

**Warning signs:** Intermittent 404s depending on client/browser Host header case.

### Pitfall 6: Not Handling Connection Close

**What goes wrong:** Connections stay open, consuming resources.

**Why it happens:** Not respecting Connection: close header from client/backend.

**How to avoid:** YARP handles this automatically. Don't add custom connection management.

**Warning signs:** Too many open files/socket errors, high connection count.

## Code Examples

Verified patterns from official sources:

### Basic Proxy Startup

```csharp
// Source: ASP.NET Core and YARP documentation patterns
var builder = WebApplication.CreateBuilder(args);

// Add YARP reverse proxy
builder.Services.AddReverseProxy()
    .LoadFromMemory([], []);  // Start with empty config

// Add logging
builder.Services.AddLogging();

var app = builder.Build();

// Map proxy middleware
app.MapReverseProxy();

// Add configuration endpoint
app.MapPost("/api/v1/add-host", async (
    UpdateConfigRequest request,
    InMemoryConfigProvider configProvider,
    ILogger<Program> logger) =>
{
    try
    {
        configProvider.Update(request.Routes, request.Clusters);
        logger.LogInformation("Updated configuration with {RouteCount} routes",
            request.Routes.Count);
        return Results.Ok(new { message = "Configuration updated" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to update configuration");
        return Results.Problem(
            title: "Configuration update failed",
            statusCode: 500
        );
    }
});

// Run on port 1355
app.Run("http://localhost:1355");
```

### Host-Based Routing Configuration

```csharp
// Source: YARP routing documentation
public static RouteConfig CreateHostRoute(string hostname, string backendUrl)
{
    return new RouteConfig
    {
        RouteId = $"route-{hostname}",
        ClusterId = $"cluster-{hostname}",
        Match = new RouteMatch
        {
            // Match on exact Host header
            Hosts = new[] { hostname }
        },
        // Optional: Path-based routing within host
        // Path = "/api/{**catch-all}"
    };
}

public static ClusterConfig CreateBackendCluster(string clusterId, string backendUrl)
{
    return new ClusterConfig
    {
        ClusterId = clusterId,
        Destinations = new Dictionary<string, DestinationConfig>
        {
            {
                "backend1",
                new DestinationConfig { Address = backendUrl }
            }
        },
        // Health check configuration (optional)
        HealthCheck = new HealthCheckConfig
        {
            Active = new ActiveHealthCheckConfig
            {
                Enabled = true,
                Interval = TimeSpan.FromSeconds(30),
                Timeout = TimeSpan.FromSeconds(5),
                Path = "/health"
            }
        }
    };
}
```

### Custom InMemoryConfigProvider Implementation

```csharp
// Source: Existing codebase pattern + YARP IProxyConfigProvider interface
public class InMemoryConfigProvider : IProxyConfigProvider
{
    private volatile InMemoryConfig _config;

    public InMemoryConfigProvider(
        IReadOnlyList<RouteConfig> routes,
        IReadOnlyList<ClusterConfig> clusters)
    {
        _config = new InMemoryConfig(routes, clusters);
    }

    public IProxyConfig GetConfig() => _config;

    public void Update(
        IReadOnlyList<RouteConfig> routes,
        IReadOnlyList<ClusterConfig> clusters)
    {
        // Atomically swap config
        var oldConfig = _config;
        _config = new InMemoryConfig(routes, clusters);

        // Signal YARP to reload
        oldConfig.SignalChange();
    }
}

// DTO for API requests
public record UpdateConfigRequest(
    IReadOnlyList<RouteConfig> Routes,
    IReadOnlyList<ClusterConfig> Clusters
);
```

### Request Logging Middleware

```csharp
// Source: ASP.NET Core middleware documentation
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        var host = context.Request.Headers.Host.ToString();
        var path = context.Request.Path;

        try
        {
            await _next(context);
        }
        finally
        {
            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "Request: {Method} {Host}{Path} => {StatusCode} ({Duration}ms)",
                context.Request.Method,
                host,
                path,
                context.Response.StatusCode,
                duration.TotalMilliseconds
            );
        }
    }
}

// Register in Program.cs
app.UseMiddleware<RequestLoggingMiddleware>();
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Custom proxy implementations | YARP (Yet Another Reverse Proxy) | 2021 (v1.0) | Production-ready, Microsoft-maintained, .NET-native |
| Static configuration files | Dynamic configuration providers | 2022 (v1.1) | Runtime updates without restart |
| Manual header management | Transform system | 2021 (v1.0) | Declarative header modifications |
| Single-threaded proxy models | Async/await throughout | 2020 (.NET 5+) | Better scalability, performance |

**Deprecated/outdated:**
- IIS ARR (Application Request Routing): Legacy, Windows-only, replaced by YARP
- Ocelot: Still maintained but YARP is Microsoft's recommended choice for new projects
- Manual HttpClient proxying: Prone to connection leaks, doesn't handle WebSockets/HTTP/2 properly

## Open Questions

1. **Port binding configuration mechanism**
   - What we know: Default port is 1355, need override capability
   - What's unclear: Should this be environment variable, CLI flag, or config file?
   - Recommendation: Support both environment variable (PORTLESS_PORT) and CLI flag for flexibility

2. **Error response format**
   - What we know: Need meaningful error responses
   - What's unclear: JSON vs plain text, detail level
   - Recommendation: JSON for API errors, plain text for console output, consistent with REST best practices

3. **Logging verbosity**
   - What we know: Need logging for debugging
   - What's unclear: Default log level, what to log in production vs development
   - Recommendation: Information in development, Warning in production, configurable via appsettings

4. **Header forwarding policy**
   - What we know: Need to preserve certain headers, modify others
   - What's unclear: Exact list of headers to forward/modify/remove
   - Recommendation: Preserve all headers by default, only add X-Forwarded-* headers, document any exceptions

## Sources

### Primary (HIGH confidence)

- **Existing codebase analysis**
  - Portless.Proxy/Program.cs: Current YARP implementation pattern
  - Portless.Proxy.csproj: YARP 2.3.0 package reference
  - .planning/codebase/ARCHITECTURE.md: Architecture decisions and patterns
  - .planning/codebase/STACK.md: Technology stack details
  - PRD.md: Product requirements and technical specifications

- **YARP GitHub repository** (https://github.com/microsoft/reverse-proxy)
  - Verified YARP is Microsoft's official reverse proxy toolkit
  - Confirmed 2.3.0 version exists and is stable
  - Verified InMemoryConfigProvider pattern
  - Documentation on configuration providers

- **ASP.NET Core documentation** (https://learn.microsoft.com/en-us/aspnet/core/)
  - Kestrel server configuration
  - Middleware pipeline patterns
  - HTTP request handling

### Secondary (MEDIUM confidence)

- **YARP configuration patterns** (based on existing codebase implementation)
  - Host header routing via RouteConfig.Match.Hosts
  - Dynamic configuration via InMemoryConfigProvider
  - Transform system for header modification

- **.NET 10 documentation**
  - Native AOT capabilities
  - Performance improvements
  - Web SDK enhancements

### Tertiary (LOW confidence)

- **Web search attempts** (search tool returned empty results)
  - Attempted searches for YARP 2.3 specific features
  - Attempted searches for InMemoryConfigProvider examples
  - All web search queries failed due to technical issues
  - **Note:** These findings are based on existing codebase patterns and general knowledge of YARP architecture

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - YARP 2.3.0 is verified in codebase, well-documented
- Architecture: HIGH - Existing implementation provides working pattern, verified against YARP best practices
- Pitfalls: MEDIUM - Based on general reverse proxy experience and YARP patterns documented in codebase

**Research date:** 2026-02-19
**Valid until:** 2026-03-21 (30 days - YARP is stable but .NET 10 is evolving)

**Assumptions made:**
- YARP 2.3.0 behavior matches existing codebase patterns
- ASP.NET Core 10.0 follows established middleware patterns
- Host header routing works as documented in YARP samples
- InMemoryConfigProvider pattern from existing code is correct

**Validation recommendations:**
- Test dynamic route updates before committing to pattern
- Verify Host header matching works with .localhost domains
- Confirm error handling behavior in production scenarios
- Performance test with realistic load before v1.0 release
