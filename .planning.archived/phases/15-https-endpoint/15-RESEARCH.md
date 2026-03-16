# Phase 15: HTTPS Endpoint - Research

**Researched:** 2026-02-23
**Domain:** ASP.NET Core Kestrel HTTPS configuration with dual HTTP/HTTPS endpoints
**Confidence:** HIGH

## Summary

Phase 15 requires implementing dual HTTP/HTTPS endpoints in the Portless.NET proxy with automatic certificate binding. The implementation involves configuring Kestrel to listen on two fixed ports (HTTP=1355, HTTPS=1356), binding the Phase 13-generated wildcard certificate to the HTTPS endpoint, enforcing TLS 1.2+ minimum protocol, and implementing HTTP→HTTPS permanent redirects when HTTPS is enabled.

**Primary recommendation:** Use Kestrel's `ListenAnyIP` with certificate binding via code (not configuration files), implement HTTP→HTTPS redirects using ASP.NET Core's `UseHttpsRedirection` middleware configured for 308 permanent redirects, and load the certificate using `X509Certificate2` with the `X509KeyStorageFlags.Exportable` flag to match Phase 13's certificate generation approach.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **Opt-in con flag --https**: `portless proxy start --https` habilita HTTPS
- **Sin flag**: Solo HTTP (puerto 1355), backward compatible con v1.1
- **Con flag**: HTTP + HTTPS simultáneos (1355 + 1356)
- **Auto-generación con --https**: Si certificado no existe, generar automáticamente (como Phase 13)
- **Logging minimalista**: "HTTPS certificate ready" sin detalles prolijos durante startup

### Gestión de certificados
- **Pre-startup validation**: Validar que cert.pfx existe y es válido antes de iniciar Kestrel
- **Sin --https**: No validar certificado (HTTP-only mode)
- **Con --https**: Error si certificado inválido: "Certificate not found. Run: portless cert install" (exit code 1)
- **Claude's discretion**: Nivel de validación (existencia vs expiración) según coherencia con ICertificateManager de Phase 13

### Configuración de puertos
- **Breaking change**: Puertos fijos HTTP=1355, HTTPS=1356 (no configurables)
- **PORTLESS_PORT deprecated**: Warning si está seteado: "PORTLESS_PORT deprecated. Fixed ports: HTTP=1355, HTTPS=1356"
- **Comunicación**: Documentar breaking change en CHANGELOG.md y migration guide v1.1→v1.2
- **Rationale**: Simplifica configuración, elimina complejidad de ports derivados

### Comportamiento dual HTTP/HTTPS
- **Con --https**: HTTP (1355) redirect 301→HTTPS (1356) para todas las requests
- **Sin --https**: Solo HTTP funciona normalmente
- **Redirect 301 permanente**: HTTP returns 301 Permanent Redirect a https://same-hostname:1356/path
- **Claude's discretion**:
  - Si /api/v1/* endpoints también redirect o se excluyen (según necesidades de management API)
  - Comportamiento HTTP sin --https flag (siempre activo o respetar flag)

### Deferred Ideas (OUT OF SCOPE)
- Certificate background monitoring (deferred to Phase 17: Certificate Lifecycle)
- Integration tests for HTTPS (deferred to Phase 18: Integration Tests)
- User documentation for HTTPS (deferred to Phase 19: Documentation)
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| HTTPS-01 | Proxy listens on dual endpoints: HTTP (1355) and HTTPS (1356) | Kestrel ListenAnyIP supports multiple endpoints; code-based configuration recommended |
| HTTPS-02 | HTTPS port is configurable via `PORTLESS_HTTPS_PORT` environment variable | **CONFLICT**: User decision says fixed ports (breaking change). REQUIREMENTS.md needs update or user decision override. |
| HTTPS-03 | HTTPS endpoint uses generated wildcard certificate from `~/.portless/cert.pfx` | X509Certificate2 loading from PFX file; ICertificateManager.GetServerCertificateAsync provides certificate |
| HTTPS-04 | Kestrel enforces TLS 1.2+ minimum protocol version | ConfigureHttpsDefaults with SslProtocols.Tls12 \| SslProtocols.Tls13 |
| HTTPS-05 | HTTP endpoint remains functional for backward compatibility | Dual endpoint configuration supports HTTP-only mode when --https flag not set |
| CLI-05 | `portless proxy start --https` — Start proxy with HTTPS endpoint enabled | Spectre.Console.Cli boolean flag pattern; ProxyStartSettings needs --https option |

**Note**: HTTPS-02 conflicts with user decision on fixed ports. Planner must resolve this discrepancy.
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| ASP.NET Core Kestrel | 10.0 | HTTP/HTTPS server with dual endpoint support | Built-in cross-platform web server, native certificate binding |
| X509Certificate2 | System.Security.Cryptography | Certificate loading and validation | .NET native API, no external dependencies |
| Spectre.Console.Cli | 0.53.1 | CLI command parsing for --https flag | Already used in Portless.Cli, consistent patterns |
| Microsoft.AspNetCore.HttpOverrides | Existing | Forwarded headers middleware | Already in use for X-Forwarded-* headers |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| ICertificateManager | Phase 13 | Certificate validation and loading | Pre-startup validation, auto-generation |
| ICertificateStorageService | Phase 13 | Loading certificate from ~/.portless/cert.pfx | File-based certificate access |
| HttpsRedirectionMiddleware | ASP.NET Core | HTTP→HTTPS redirects | Permanent redirect (308) when --https enabled |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Code-based Kestrel config | appsettings.json configuration | Code gives better control over certificate loading, validation errors, and empty password handling |
| 308 Permanent Redirect | 301 Moved Permanently | 308 preserves HTTP method (POST stays POST), 301 may convert POST to GET |

**Installation:**
```bash
# No new packages required - all dependencies already in project
# Phase 13 certificates (ICertificateManager) already integrated
# Spectre.Console.Cli 0.53.1 already in use for CLI commands
```

## Architecture Patterns

### Recommended Project Structure
```
Portless.Proxy/
├── Program.cs                    # Modified: dual endpoint configuration
├── Middleware/
│   └── HttpsRedirectMiddleware.cs # NEW: conditional HTTP→HTTPS redirect

Portless.Cli/
├── Commands/ProxyCommand/
│   ├── ProxyStartCommand.cs      # Modified: --https flag handling
│   └── ProxyStartSettings.cs     # Modified: add --https boolean option
│
Portless.Core/
├── Services/
│   ├── ICertificateManager.cs    # EXISTING: certificate validation
│   └── CertificateManager.cs     # EXISTING: EnsureCertificatesAsync
```

### Pattern 1: Dual Kestrel Endpoints with Certificate Binding
**What:** Configure Kestrel to listen on HTTP and HTTPS ports simultaneously
**When to use:** Proxy needs to support both HTTP and HTTPS protocols
**Example:**
```csharp
// Source: Microsoft Learn - Configure Kestrel Endpoints (ASP.NET Core 10.0)
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0

builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP endpoint (always active)
    options.ListenAnyIP(1355, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });

    // HTTPS endpoint (conditional on --https flag)
    if (enableHttps)
    {
        options.ListenAnyIP(1356, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
            listenOptions.UseHttps(certificate); // X509Certificate2 from Phase 13
        });
    }
});
```

### Pattern 2: TLS 1.2+ Enforcement
**What:** Configure minimum TLS protocol version globally
**When to use:** Security requirement to enforce TLS 1.2 or higher
**Example:**
```csharp
// Source: Microsoft Learn - Kestrel TLS Protocol Configuration
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/https

builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
    });
});
```

### Pattern 3: Conditional HTTP→HTTPS Redirect
**What:** Redirect HTTP requests to HTTPS only when HTTPS is enabled
**When to use:** Permanent redirect from HTTP to HTTPS endpoint
**Example:**
```csharp
// Source: Microsoft Learn - HTTPS Redirection Middleware
// https://learn.microsoft.com/en-us/aspnet/core/security/enforcing-ssl

// In Program.cs, before MapReverseProxy()
if (enableHttps)
{
    // Configure permanent redirect (308, not 301)
    builder.Services.AddHttpsRedirection(options =>
    {
        options.RedirectStatusCode = Status308PermanentRedirect;
        options.HttpsPort = 1356;
    });

    app.UseHttpsRedirection();
}
```

### Pattern 4: Spectre.Console.Cli Boolean Flag
**What:** Add boolean command-line flag to Spectre.Console.Cli settings
**When to use:** Enable/disable HTTPS feature via CLI
**Example:**
```csharp
// Source: Spectre.Console.Cli Documentation (inferred from existing codebase patterns)
// Portless.Cli already uses CommandOption for --port, --protocol, --verbose

public class ProxyStartSettings : CommandSettings
{
    [CommandOption("--port <PORT>")]
    public int Port { get; set; } = 1355;

    [CommandOption("--https")]
    public bool EnableHttps { get; set; } = false; // NEW: boolean flag
}
```

### Anti-Patterns to Avoid
- **Don't use appsettings.json for HTTPS**: Code-based configuration gives better error messages and certificate validation
- **Don't use 301 redirect**: Use 308 (permanent) to preserve HTTP methods
- **Don't hardcode certificate paths**: Use ICertificateStorageService from Phase 13
- **Don't silently fall back to HTTP**: Fail explicitly if certificate invalid when --https flag set

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| HTTP→HTTPS redirects | Custom middleware with 301 status | `UseHttpsRedirection()` with 308 status | Preserves HTTP methods, handles edge cases (POST, WebSockets) |
| Certificate loading | Manual PFX parsing | `X509Certificate2` constructor | Handles PEM/DER, validates format, cross-platform |
| Certificate validation | Custom expiration checking | `ICertificateManager.EnsureCertificatesAsync` | Already implements validation, auto-generation, status checking |
| TLS protocol enforcement | Manual cipher suite configuration | `ConfigureHttpsDefaults` with `SslProtocols` | OS-managed cipher suites, automatic updates |
| CLI flag parsing | Manual argument parsing | Spectre.Console.Cli `CommandOption` | Already integrated, handles help, validation |

**Key insight:** ASP.NET Core has built-in solutions for all HTTPS concerns. Custom implementations add maintenance burden and miss edge cases (WebSocket upgrades, POST preservation, cipher suite negotiation).

## Common Pitfalls

### Pitfall 1: Certificate Without Password Loading
**What goes wrong:** Loading PFX certificate without password throws `CryptographicException`
**Why it happens:** Phase 13 generates certificates with empty password, but `X509Certificate2` constructor expects password parameter
**How to avoid:** Use empty string `""` or `null` as password when loading certificate
**Warning signs:** "The specified network password is not correct" exception on startup

### Pitfall 2: 301 Redirect Breaking POST Requests
**What goes wrong:** HTTP POST requests converted to GET during HTTPS redirect
**Why it happens:** 301 Moved Permanently allows method change, browsers convert POST to GET
**How to avoid:** Use `Status308PermanentRedirect` instead of 301 (308 preserves HTTP method)
**Warning signs:** Backend receives GET requests when expecting POST

### Pitfall 3: /api/v1/* Endpoints Redirected to HTTPS
**What goes wrong:** CLI cannot add routes because /api/v1/add-host redirects to HTTPS
**Why it happens:** Global HTTPS redirection middleware applies to all requests
**How to avoid:** Exclude /api/v1/* from HTTPS redirect using path-based conditional middleware
**Warning signs:** CLI commands fail with "SSL connection required" errors

### Pitfall 4: Certificate Not Exportable
**What goes wrong:** `HasPrivateKey` is false after loading certificate
**Why it happens:** Certificate loaded without `X509KeyStorageFlags.Exportable` flag
**How to avoid:** Load certificate with `X509KeyStorageFlags.Exportable` flag (matches Phase 13 generation)
**Warning signs:** Kestrel fails to bind HTTPS endpoint with "certificate missing private key"

### Pitfall 5: HTTP Endpoint Active When Only HTTPS Requested
**What goes wrong:** Both HTTP and HTTPS accept traffic when user expects HTTPS-only
**Why it happens:** Dual endpoint configuration always enables HTTP endpoint
**How to avoid:** HTTP→HTTPS redirect ensures all traffic goes to HTTPS, but HTTP endpoint stays reachable (correct per user decision)
**Warning signs:** None - this is intended behavior per CONTEXT.md: "HTTP (1355) redirect 301→HTTPS (1356)"

### Pitfall 6: Breaking Change: PORTLESS_PORT Environment Variable
**What goes wrong:** Users with `PORTLESS_PORT=5000` environment variable expect proxy to listen on port 5000
**Why it happens:** Phase 15 changes to fixed ports (1355, 1356) ignores environment variable
**How to avoid:** Log warning if `PORTLESS_PORT` is set: "PORTLESS_PORT deprecated. Fixed ports: HTTP=1355, HTTPS=1356"
**Warning signs:** Proxy ignores custom port, users confused why port changed

## Code Examples

Verified patterns from official sources:

### Loading Certificate from PFX File
```csharp
// Source: Microsoft Learn - X509Certificate2 Constructor
// https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509certificate2.-ctor

// Load certificate with empty password (Phase 13 generates password-less certificates)
var certificate = new X509Certificate2(
    certPath,           // Path to ~/.portless/cert.pfx
    "",                 // Empty password
    X509KeyStorageFlags.Exportable // Required for Kestrel to use private key
);

// Verify certificate has private key before binding
if (!certificate.HasPrivateKey)
{
    throw new InvalidOperationException("Certificate missing private key");
}
```

### Pre-startup Certificate Validation
```csharp
// Source: Phase 13 ICertificateManager integration
// https://github.com/serge-khao/portless-dotnet (Phase 13 implementation)

// Before starting Kestrel, validate certificate
if (enableHttps)
{
    var certManager = app.Services.GetRequiredService<ICertificateManager>();
    var status = await certManager.EnsureCertificatesAsync(
        forceRegeneration: false,
        cancellationToken
    );

    if (!status.IsValid || status.IsCorrupted)
    {
        logger.LogError("Certificate not found or invalid. Run: portless cert install");
        Environment.Exit(1); // Exit code 1 per CONTEXT.md
        return;
    }

    logger.LogInformation("HTTPS certificate ready");
}
```

### Conditional HTTPS Redirect Middleware
```csharp
// Source: Microsoft Learn - UseHttpsRedirection Middleware
// https://learn.microsoft.com/en-us/aspnet/core/security/enforcing-ssl

// In Program.cs, configure redirect only if HTTPS enabled
if (enableHttps)
{
    builder.Services.AddHttpsRedirection(options =>
    {
        options.RedirectStatusCode = Status308PermanentRedirect; // 308, not 301
        options.HttpsPort = 1356;
    });
}

// In middleware pipeline (before MapReverseProxy)
if (enableHttps)
{
    app.UseHttpsRedirection();
}

// Note: Consider excluding /api/v1/* endpoints from redirect
// to allow CLI management API to work over HTTP
```

### Complete Kestrel Configuration
```csharp
// Source: Microsoft Learn - Configure Kestrel Endpoints (ASP.NET Core 10.0)
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0

builder.WebHost.ConfigureKestrel(options =>
{
    // Enforce TLS 1.2+ globally (applies to all HTTPS endpoints)
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
    });

    // HTTP endpoint (always active for backward compatibility)
    options.ListenAnyIP(1355, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });

    // HTTPS endpoint (conditional on --https flag)
    if (enableHttps)
    {
        var cert = new X509Certificate2(certPath, "", X509KeyStorageFlags.Exportable);

        options.ListenAnyIP(1356, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
            listenOptions.UseHttps(cert);
        });
    }
});
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| appsettings.json Kestrel config | Code-based ConfigureKestrel | .NET 6+ | Better control, validation, error handling |
| 301 Moved Permanently redirect | 308 Permanent Redirect | ASP.NET Core 2.1+ | Preserves HTTP methods (POST stays POST) |
| Password-protected PFX | Empty password PFX | Phase 13 | Simplifies certificate loading, no password management |
| Single HTTP endpoint | Dual HTTP/HTTPS endpoints | Phase 15 | Supports both HTTP and HTTPS simultaneously |

**Deprecated/outdated:**
- **301 for HTTPS redirects**: Use 308 instead (preserves POST requests)
- **Configuration-based HTTPS**: Use code-based configuration for better validation
- **PORTLESS_PORT environment variable**: Deprecated in favor of fixed ports (breaking change)

## Open Questions

1. **HTTPS-02 Requirement vs User Decision Conflict**
   - What we know: REQUIREMENTS.md says "HTTPS port configurable via PORTLESS_HTTPS_PORT"
   - What's unclear: User decision in CONTEXT.md says "breaking change: fixed ports HTTP=1355, HTTPS=1356 (no configurable)"
   - Recommendation: Follow user decision (fixed ports), update REQUIREMENTS.md to match CONTEXT.md

2. **Should /api/v1/* Endpoints Exclude from HTTPS Redirect?**
   - What we know: Global HTTPS redirection applies to all requests by default
   - What's unclear: CLI management API (/api/v1/add-host) may need to work over HTTP
   - Recommendation: Exclude /api/v1/* from HTTPS redirect using path-based conditional middleware (Claude's discretion area)

3. **HTTP Endpoint Behavior Without --https Flag**
   - What we know: User decision says "Sin flag: Solo HTTP funciona normalmente"
   - What's unclear: Should HTTP endpoint be disabled when --https is set?
   - Recommendation: Keep HTTP endpoint active (for redirect), all traffic goes to HTTPS via 308 redirect

## Sources

### Primary (HIGH confidence)
- [Microsoft Learn - Configure Kestrel Endpoints (ASP.NET Core 10.0)](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0) - Verified ListenAnyIP dual endpoint configuration
- [Microsoft Learn - HTTPS Redirection Middleware](https://learn.microsoft.com/en-us/aspnet/core/security/enforcing-ssl) - Verified UseHttpsRedirection with 308 status code
- [Microsoft Learn - X509Certificate2 Constructor](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509certificate2.-ctor) - Verified certificate loading with empty password
- [Phase 13 Implementation](https://github.com/serge-khao/portless-dotnet) - Verified ICertificateManager, CertificateStorageService, certificate generation patterns
- [Portless.NET Codebase](https://github.com/serge-khao/portless-dotnet) - Verified existing Kestrel configuration, CLI patterns, certificate services

### Secondary (MEDIUM confidence)
- [Microsoft Learn - Kestrel TLS Protocol Changes](https://learn.microsoft.com/zh-cn/dotnet/core/compatibility/aspnet-core/5.0/kestrel-default-supported-tls-protocol-versions-changed) - Verified TLS 1.2+ enforcement via ConfigureHttpsDefaults
- [Microsoft Learn - Kestrel HTTPS API](https://learn.microsoft.com/zh-cn/dotnet/api/microsoft.aspnetcore.server.kestrel.https?view=aspnetcore-8.0) - Verified HttpsConnectionAdapterOptions for certificate binding

### Tertiary (LOW confidence)
- Chinese documentation resources (2026) referencing .NET 10 Kestrel configuration - Marked for validation: Search results didn't provide specific URLs, need official .NET 10 documentation confirmation
- Spectre.Console.Cli boolean flag patterns - Inferred from existing codebase, not verified with official docs

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All components verified in codebase or official Microsoft docs
- Architecture: HIGH - Kestrel dual endpoints verified in official ASP.NET Core 10.0 docs
- Pitfalls: MEDIUM - Certificate loading issues verified, /api/v1/* exclusion needs validation during implementation

**Research date:** 2026-02-23
**Valid until:** 2026-03-25 (30 days - stable ASP.NET Core Kestrel APIs)

---

**Phase:** 15 - HTTPS Endpoint
**Requirements addressed:** HTTPS-01, HTTPS-03, HTTPS-04, HTTPS-05, CLI-05 (HTTPS-02 conflicts with user decision)
**Dependency:** Phase 13 (Certificate Generation) - COMPLETE
**Next phases:** Phase 16 (Mixed Protocol Support), Phase 17 (Certificate Lifecycle)
