# Phase 16: Mixed Protocol Support - Research

**Researched:** 2026-02-23
**Domain:** YARP Reverse Proxy - Mixed HTTP/HTTPS Backend Support
**Confidence:** HIGH

## Summary

Phase 16 requires implementing transparent protocol forwarding for mixed HTTP/HTTPS backend services in Portless.NET. The proxy must preserve the original client protocol (HTTP or HTTPS) in the `X-Forwarded-Proto` header while supporting simultaneous routing to both HTTP and HTTPS backends. Additionally, YARP's HttpClient must be configured to accept self-signed certificates in development mode.

**Primary recommendation:** Use YARP's `HttpClientConfig.DangerousAcceptAnyServerCertificate` property for development SSL validation and leverage YARP's built-in X-Forwarded-* header forwarding via the `UseForwardedHeaders` middleware already present in the codebase. Backend protocol (HTTP vs HTTPS) is automatically determined by the `Address` scheme in the `DestinationConfig`.

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| MIXED-01 | Proxy preserves original protocol in X-Forwarded-Proto header | ASP.NET Core ForwardedHeaders middleware automatically handles this |
| MIXED-02 | Backend HTTP services receive `X-Forwarded-Proto: http` | Automatic when client connects via HTTP to port 1355 |
| MIXED-03 | Backend HTTPS services receive `X-Forwarded-Proto: https` | Automatic when client connects via HTTPS to port 1356 |
| MIXED-04 | Proxy supports mixed routing (some backends HTTP, others HTTPS) | YARP supports mixed protocols via DestinationConfig.Address scheme |
| MIXED-05 | YARP backend SSL validation configured for development mode | HttpClientConfig.DangerousAcceptAnyServerCertificate property |

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| **YARP** | 2.3.0 (existing) | Reverse proxy engine | Already in use, provides built-in X-Forwarded-* support |
| **ASP.NET Core** | 10.0 (existing) | Web framework | ForwardedHeaders middleware built-in |
| **SocketsHttpHandler** | .NET 10 (existing) | HTTP client for YARP backends | Default YARP HttpClient handler |

### Supporting

| Component | Purpose | When to Use |
|-----------|---------|-------------|
| **HttpClientConfig** | YARP HTTP client configuration per cluster | For SSL validation settings (DangerousAcceptAnyServerCertificate) |
| **ForwardedHeadersMiddleware** | ASP.NET Core middleware for X-Forwarded-* headers | Already present in Program.cs, preserves client protocol |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| HttpClientConfig.DangerousAcceptAnyServerCertificate | Custom SslOptions.RemoteCertificateValidationCallback | More complex, code-based configuration instead of declarative |

## Architecture Patterns

### Recommended Project Structure

```
Portless.Proxy/
├── Program.cs                    # Modified: Add HttpClientConfig to clusters
├── DynamicConfigProvider.cs      # Existing: Manages YARP config updates
└── CertificateConfiguration/     # New: Development SSL settings
    └── DevelopmentHttpClientConfig.cs  # Optional: Centralized SSL config

Portless.Core/
├── Models/
│   └── RouteConfigExtensions.cs # New: Backend protocol detection helpers
└── Services/
    └── IBackendProtocolDetector.cs  # Optional: Service for protocol detection
```

### Pattern 1: Mixed Backend Protocol Support

**What:** YARP automatically supports mixed HTTP/HTTPS backends based on the destination address scheme. No special configuration is required beyond setting the correct `Address` protocol.

**When to use:** When you need to proxy to both HTTP and HTTPS backend services simultaneously.

**Example:**
```csharp
// Source: YARP documentation and existing Program.cs patterns

// HTTP backend cluster
var httpCluster = new ClusterConfig
{
    ClusterId = "cluster-http-backend",
    Destinations = new Dictionary<string, DestinationConfig>
    {
        ["backend1"] = new DestinationConfig
        {
            Address = "http://localhost:4000"  // HTTP scheme
        }
    },
    // Optional: Disable SSL validation for development
    HttpClient = new HttpClientConfig
    {
        DangerousAcceptAnyServerCertificate = true  // For development only
    }
};

// HTTPS backend cluster
var httpsCluster = new ClusterConfig
{
    ClusterId = "cluster-https-backend",
    Destinations = new Dictionary<string, DestinationConfig>
    {
        ["backend1"] = new DestinationConfig
        {
            Address = "https://localhost:5001"  // HTTPS scheme
        }
    },
    HttpClient = new HttpClientConfig
    {
        DangerousAcceptAnyServerCertificate = true,  // Required for self-signed certs
        SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
    }
};
```

### Pattern 2: X-Forwarded-Proto Header Preservation

**What:** ASP.NET Core's `ForwardedHeadersMiddleware` automatically adds `X-Forwarded-Proto`, `X-Forwarded-For`, and `X-Forwarded-Host` headers based on the incoming request. YARP forwards these headers to backends by default.

**When to use:** Always - this is the standard way to preserve client protocol information in reverse proxy scenarios.

**Example:**
```csharp
// Source: Portless.Proxy/Program.cs lines 334-340 (existing implementation)

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.All,
    // Only trust local proxies
    KnownProxies = { IPAddress.Loopback }
});

// YARP automatically forwards these headers to backends
// - X-Forwarded-Proto: http (for HTTP client connections on port 1355)
// - X-Forwarded-Proto: https (for HTTPS client connections on port 1356)
```

### Pattern 3: Development SSL Validation Configuration

**What:** Use YARP's `HttpClientConfig.DangerousAcceptAnyServerCertificate` to disable SSL certificate validation for development environments. This allows YARP to connect to HTTPS backends with self-signed certificates.

**When to use:** Development environments only. Never use in production.

**Example:**
```csharp
// Source: YARP HTTP Client Configuration documentation
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/http-client-config

var clusters = new[]
{
    new ClusterConfig
    {
        ClusterId = "cluster1",
        Destinations = {
            { "destination1", new DestinationConfig { Address = "https://localhost:5001" } }
        },
        HttpClient = new HttpClientConfig
        {
            DangerousAcceptAnyServerCertificate = true,  // Development only!
            SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            MaxConnectionsPerServer = 100
        }
    }
};
```

### Anti-Patterns to Avoid

- **Manual X-Forwarded-Proto header injection:** Don't manually add `X-Forwarded-Proto` headers in custom middleware. Use the built-in `ForwardedHeadersMiddleware` instead, which is already correctly configured in the codebase.
- **Globally disabling SSL validation:** Don't use `ConfigureHttpClient` with a global callback that disables SSL for all clusters. Use per-cluster `HttpClientConfig` instead to control SSL validation granularly.
- **Hardcoding backend protocols:** Don't assume all backends use the same protocol. Support mixed protocols by allowing the `Address` scheme to determine the backend protocol.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| X-Forwarded-* header generation | Custom middleware to add X-Forwarded-* headers | ASP.NET Core `UseForwardedHeaders` middleware | Already implemented in Program.cs, handles all edge cases correctly |
| Backend protocol detection | Custom logic to parse backend URLs and detect scheme | YARP's automatic protocol detection from `Address` URI | YARP automatically uses HTTP or HTTPS based on the address scheme |
| SSL certificate validation bypass | Custom `RemoteCertificateValidationCallback` delegates | `HttpClientConfig.DangerousAcceptAnyServerCertificate` | Declarative configuration, easier to disable in production |

**Key insight:** YARP and ASP.NET Core already provide all the necessary functionality for mixed protocol support. The implementation requires configuration, not custom code.

## Common Pitfalls

### Pitfall 1: Missing HttpClientConfig for HTTPS Backends

**What goes wrong:** HTTPS backends with self-signed certificates fail with "The SSL connection could not be established" error.

**Why it happens:** YARP's default `HttpClientConfig` has `DangerousAcceptAnyServerCertificate = false`, which enforces strict certificate validation.

**How to avoid:** Always add `HttpClient = new HttpClientConfig { DangerousAcceptAnyServerCertificate = true }` to clusters that connect to HTTPS backends in development mode.

**Warning signs:**
- Backend connection errors with "SSL connection" messages
- Error code `ForwarderError.Upgrade` or `ForwarderError.Request` in YARP logs

### Pitfall 2: Incorrect Backend Address Scheme

**What goes wrong:** YARP attempts HTTP connection to an HTTPS-only backend (or vice versa), resulting in connection refused or EOF errors.

**Why it happens:** The `DestinationConfig.Address` uses the wrong scheme (http:// instead of https:// or vice versa).

**How to avoid:** When creating `DestinationConfig`, ensure the `Address` scheme matches the backend's actual protocol:
- HTTP backends: `"http://localhost:{port}"`
- HTTPS backends: `"https://localhost:{port}"`

**Warning signs:**
- Connection refused errors
- Unexpected EOF errors
- Protocol mismatch in YARP logs

### Pitfall 3: X-Forwarded-Proto Not Reaching Backends

**What goes wrong:** Backend services report incorrect protocol (always http or always https) despite client using different protocol.

**Why it happens:** `ForwardedHeadersMiddleware` is not configured or is placed after YARP's `MapReverseProxy()` in the middleware pipeline.

**How to avoid:** Ensure `app.UseForwardedHeaders()` is called **before** `app.MapReverseProxy()` and use the `ForwardedHeaders.All` setting. The current implementation in Program.cs is correct.

**Warning signs:**
- Backend `HttpContext.Request.Scheme` is always "http" or always "https"
- Backend can't detect original client protocol

### Pitfall 4: Production SSL Validation Disabled

**What goes wrong:** Deployment to production with `DangerousAcceptAnyServerCertificate = true`, creating a security vulnerability.

**Why it happens:** Development configuration is accidentally deployed to production.

**How to avoid:** Use environment-specific configuration or explicitly check for development environment before applying `DangerousAcceptAnyServerCertificate = true`.

**Warning signs:**
- Certificate validation disabled in production logs
- Security scan findings related to SSL validation

### Pitfall 5: Mixed Protocols in Same Cluster

**What goes wrong:** Different destinations in the same cluster use different protocols, causing YARP to use inconsistent SSL settings.

**Why it happens:** Adding both HTTP and HTTPS addresses to the same `ClusterConfig.Destinations` dictionary.

**How to avoid:** Use separate clusters for HTTP and HTTPS backends, or ensure all destinations in a cluster use the same protocol scheme.

**Warning signs:**
- Intermittent SSL errors
- Some backends work while others fail

## Code Examples

Verified patterns from official sources:

### Adding HttpClientConfig to Existing Cluster Creation

```csharp
// Source: Based on YARP HTTP Client Configuration documentation
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/http-client-config

static ClusterConfig CreateCluster(string clusterId, string backendUrl) =>
    new ClusterConfig
    {
        ClusterId = clusterId,
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["backend1"] = new DestinationConfig { Address = backendUrl }
        },
        // NEW: Add HttpClient configuration for SSL validation
        HttpClient = new HttpClientConfig
        {
            DangerousAcceptAnyServerCertificate = true,  // Development mode only
            SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            MaxConnectionsPerServer = 100
        }
    };
```

### Detecting Backend Protocol from Address

```csharp
// Helper method to detect backend protocol from address string
static bool IsHttpsBackend(string backendUrl)
{
    return Uri.TryCreate(backendUrl, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps;
}

// Usage:
var isHttps = IsHttpsBackend(request.BackendUrl);
logger.LogInformation("Backend protocol: {Protocol}", isHttps ? "HTTPS" : "HTTP");
```

### Environment-Specific SSL Validation

```csharp
// Source: Best practice for environment-specific configuration
static ClusterConfig CreateCluster(string clusterId, string backendUrl, bool isDevelopment)
{
    var cluster = new ClusterConfig
    {
        ClusterId = clusterId,
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["backend1"] = new DestinationConfig { Address = backendUrl }
        }
    };

    // Only disable SSL validation in development for HTTPS backends
    if (isDevelopment && IsHttpsBackend(backendUrl))
    {
        cluster.HttpClient = new HttpClientConfig
        {
            DangerousAcceptAnyServerCertificate = true,
            SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
        };
    }

    return cluster;
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Manual X-Forwarded-* header construction | Built-in `ForwardedHeadersMiddleware` | ASP.NET Core 1.0 | Automatic handling of all forwarded headers, fewer edge cases |
| Custom SSL validation callbacks | `HttpClientConfig.DangerousAcceptAnyServerCertificate` | YARP 1.0 | Declarative configuration, easier to manage, per-cluster control |
| Global HTTP client configuration | Per-cluster `HttpClientConfig` | YARP 1.0 | Granular control over SSL settings per backend |

**Deprecated/outdated:**
- **Manual forwarded header injection:** Using custom middleware to add `X-Forwarded-Proto` headers is unnecessary and error-prone. Use `UseForwardedHeaders()` instead.
- **Global SSL validation bypass:** Using `ConfigureHttpClient` with a global callback to disable SSL validation is less flexible than per-cluster configuration.

## Open Questions

1. **Environment detection strategy**
   - What we know: Need to disable SSL validation only in development
   - What's unclear: Should we use `builder.Environment.IsDevelopment()` or a dedicated configuration flag?
   - Recommendation: Use `builder.Environment.IsDevelopment()` for simplicity, as it aligns with ASP.NET Core conventions

2. **Per-backend vs global SSL validation**
   - What we know: Some backends may have valid certificates even in development
   - What's unclear: Should SSL validation be configurable per backend or globally for all HTTPS backends?
   - Recommendation: Apply `DangerousAcceptAnyServerCertificate = true` to all HTTPS backends in development for consistency, as this is a development-only tool

3. **CLI support for backend protocol specification**
   - What we know: Current API accepts full backend URLs (including scheme)
   - What's unclear: Should the CLI add-host command support protocol flags (e.g., `--https`)?
   - Recommendation: No - the full URL scheme already specifies the protocol. Adding flags would be redundant.

## Sources

### Primary (HIGH confidence)

- **YARP HTTP Client Configuration** - [Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/http-client-config?view=aspnetcore-10.0) - Comprehensive documentation of `HttpClientConfig`, including `DangerousAcceptAnyServerCertificate`, `SslProtocols`, and per-cluster configuration patterns
- **YARP Direct Forwarding** - [Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/direct-forwarding?view=aspnetcore-8.0) - Official examples of IHttpForwarder usage with custom HttpMessageInvoker and SocketsHttpHandler configuration
- **ASP.NET Core Forwarded Headers Middleware** - [Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0) - Documentation on using `UseForwardedHeaders` to preserve X-Forwarded-* headers in reverse proxy scenarios

### Secondary (MEDIUM confidence)

- **ASP.NET Core Kestrel HTTPS Configuration** - [Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/http-3?view=aspnetcore-10.0) - Kestrel configuration for HTTPS endpoints with TLS protocol enforcement (referenced in Phase 15 implementation)
- **Portless.NET Phase 15 Plan** - [Internal Documentation](.planning/phases/15-https-endpoint/15-01-PLAN.md) - Existing implementation of dual HTTP/HTTPS endpoints and certificate binding
- **Portless.NET Source Code** - Portless.Proxy/Program.cs - Current implementation with ForwardedHeaders middleware and YARP configuration

### Tertiary (LOW confidence)

- **YARP Reverse Proxy GitHub Repository** - [microsoft/reverse-proxy](https://github.com/microsoft/reverse-proxy) - Source code for YARP implementation details (not directly consulted, but referenced in documentation)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Based on existing YARP 2.3.0 usage and official Microsoft documentation
- Architecture: HIGH - Patterns verified against official YARP and ASP.NET Core documentation
- Pitfalls: HIGH - Based on common reverse proxy issues documented in YARP troubleshooting guides

**Research date:** 2026-02-23
**Valid until:** 2026-05-23 (90 days - YARP configuration is stable, but .NET 10 is in preview)
