# Architecture Research: HTTPS with Automatic Certificates for Portless.NET

**Domain:** Local Development Proxy with HTTPS Support
**Researched:** 2025-02-22
**Confidence:** HIGH

## Executive Summary

HTTPS support with automatic certificates requires **moderate architecture extensions** to Portless.NET v1.1. The implementation follows a Certificate Authority (CA) hierarchy pattern similar to `mkcert`, using .NET's built-in `System.Security.Cryptography.X509Certificates` for certificate generation.

**Key architectural changes:**
- New certificate services layer in Portless.Core (generation, storage, trust installation, renewal)
- Additional HTTPS endpoint in Portless.Proxy (dual HTTP/HTTPS listening)
- New certificate management CLI commands (install, status, renew, uninstall)
- Platform-specific trust installation (Windows/macOS/Linux)
- Background certificate renewal hosted service

**Integration points:**
- Certificate generation triggered on proxy startup if certificates don't exist
- Trust installation is manual CLI command (user action, not automatic)
- Certificate renewal is automatic background process (every 6 hours)
- Kestrel configuration change to add HTTPS endpoint with certificate binding

## Current Architecture (v1.1)

```
┌─────────────────────────────────────────────────────────────┐
│                    Portless.Cli (CLI Tool)                   │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐    │
│  │ Run Cmd  │  │ List Cmd │  │Proxy Cmds│  │ Proxy Mgr│    │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘    │
├───────┴────────────┴────────────┴────────────┴──────────────┤
│                    Portless.Core (Shared)                    │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐    │
│  │RouteStore│  │Port Alloc│  │Proc Mgr  │  │Cleanup   │    │
│  │(JSON w/  │  │(4000-4999│  │(PID track│  │Service   │    │
│  │ locking) │  │  pool)   │  │  -ing)   │  │          │    │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘    │
├───────────────────────────────────────────────────────────────┤
│                  Portless.Proxy (YARP Server)                 │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Kestrel HTTP Endpoint (Port 1355, HTTP/1.1+HTTP/2)  │   │
│  │  ↓                                                    │   │
│  │  YARP Reverse Proxy (DynamicConfigProvider)          │   │
│  │  ↓                                                    │   │
│  │  Backend Apps (localhost:4000-4999)                  │   │
│  └──────────────────────────────────────────────────────┘   │
│  ┌──────────┐  ┌──────────┐                                 │
│  │POST /api │  │DELETE /  │                                 │
│  │/v1/add-  │  │api/v1/   │                                 │
│  │host      │  │remove-   │                                 │
│  └──────────┘  └──────────┘                                 │
└───────────────────────────────────────────────────────────────┘
```

### Component Responsibilities (v1.1)

| Component | Responsibility | Implementation |
|-----------|----------------|----------------|
| **Portless.Cli** | User interaction, process spawning, proxy lifecycle | Spectre.Console.Cli commands |
| **Portless.Core** | Shared business logic, persistence, port allocation | File-based JSON store with mutex locking |
| **Portless.Proxy** | YARP proxy server, route management, HTTP/2 & WebSocket | Kestrel web server with YARP middleware |
| **DynamicConfigProvider** | In-memory YARP configuration hot-reload | Custom IProxyConfigProvider implementation |
| **RouteStore** | Route persistence to ~/.portless/routes.json | JSON file with cross-platform mutex |

## HTTPS Architecture (v1.2)

### System Overview with HTTPS

```
┌────────────────────────────────────────────────────────────────────┐
│                        Portless.Cli                               │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐         │
│  │ Run Cmd  │  │ List Cmd │  │Proxy Cmds│  │Cert Cmds │← NEW    │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────┬─────┘         │
├───────┴────────────┴────────────┴────────────┴───────────────────┤
│                        Portless.Core                              │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐        │
│  │RouteStore│  │Port Alloc│  │Proc Mgr  │  │Cert Store│← NEW    │
│  └──────────┘  └──────────┘  └──────────┘  └────┬─────┘        │
│                                                     │              │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐         │              │
│  │Route Wthr│  │Cleanup   │  │Cert      │← NEW    │              │
│  │          │  │Service   │  │Generator │         │              │
│  └──────────┘  └──────────┘  └────┬─────┘         │              │
│                                     │               │              │
│  ┌─────────────────────────────────┴───────────────┘           │
│  │           Trust Installer Service (NEW)                      │
│  └──────────────────────────────────────────────────────────────┘
├──────────────────────────────────────────────────────────────────┤
│                      Portless.Proxy                              │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │  Kestrel HTTP Endpoint (Port 1355, HTTP/1.1+HTTP/2)     │    │
│  │  Kestrel HTTPS Endpoint (Port 1356, HTTP/1.1+HTTP/2)    │←NEW│
│  │  ↓                                                        │    │
│  │  Certificate Provider (loads from CertStore)             │←NEW│
│  │  ↓                                                        │    │
│  │  YARP Reverse Proxy (works on both HTTP/HTTPS)           │    │
│  │  ↓                                                        │    │
│  │  Backend Apps (localhost:4000-4999)                      │    │
│  └─────────────────────────────────────────────────────────┘    │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐                       │
│  │POST /api │  │DELETE /  │  │GET /api  │← NEW                 │
│  │/v1/add-  │  │api/v1/   │  │/v1/cert  │                       │
│  │host      │  │remove-   │  │status    │                       │
│  └──────────┘  └──────────┘  └──────────┘                       │
└──────────────────────────────────────────────────────────────────┘
```

### Component Responsibilities (v1.2)

| Component | Responsibility | New/Modified | Implementation |
|-----------|----------------|--------------|----------------|
| **CertificateStore** | Certificate and CA persistence (file-based) | NEW | ~/.portless/ca.pfx, cert.pfx, cert-info.json |
| **CertificateGenerator** | CA creation, wildcard certificate generation | NEW | System.Security.Cryptography.X509Certificates |
| **TrustInstaller** | Platform-specific trust installation | NEW | Abstract interface, OS-specific implementations |
| **CertificateProvider** | Loads certificate for Kestrel HTTPS endpoint | NEW | Proxy-specific service |
| **CertificateRenewalService** | Background cert expiry check and renewal | NEW | Hosted service, checks every 6 hours |
| **Cert CLI Commands** | User-facing certificate management | NEW | install, status, renew, uninstall |
| **Portless.Proxy/Program.cs** | Add HTTPS endpoint configuration | MODIFIED | Second ListenAnyIP call with UseHttps() |

## Recommended Project Structure

```
Portless.Core/
├── Services/
│   ├── ICertificateStore.cs          # NEW - Certificate persistence interface
│   ├── CertificateStore.cs            # NEW - Stores CA + cert in ~/.portless/
│   ├── ICertificateGenerator.cs       # NEW - Certificate generation interface
│   ├── CertificateGenerator.cs        # NEW - Creates CA and wildcard certs
│   ├── ITrustInstaller.cs             # NEW - Trust installation interface
│   ├── TrustInstaller.cs              # NEW - Platform-specific trust logic
│   ├── CertificateRenewalService.cs   # NEW - Background cert renewal checker
│   └── ...
├── Models/
│   ├── CertificateInfo.cs             # NEW - Certificate metadata model
│   └── ...
└── Configuration/
    └── CertificateConfiguration.cs    # NEW - Cert config (domain, validity, etc)

Portless.Proxy/
├── Services/
│   ├── CertificateProvider.cs         # NEW - Provides cert to Kestrel
│   └── ...
└── Program.cs                          # MODIFIED - Add HTTPS endpoint

Portless.Cli/
├── Commands/
│   ├── CertificateCommand/            # NEW - Cert management commands
│   │   ├── CertInstallCommand.cs      # Install CA as trusted
│   │   ├── CertStatusCommand.cs       # Show cert status
│   │   ├── CertRenewCommand.cs        # Force certificate renewal
│   │   └── CertUninstallCommand.cs    # Remove CA from trust store
│   └── ...
└── Program.cs                          # MODIFIED - Register cert commands
```

### Structure Rationale

- **CertificateStore:** Mirrors RouteStore pattern - file-based persistence with locking
- **CertificateGenerator:** Isolated service using System.Security.Cryptography (no external deps)
- **TrustInstaller:** Platform abstraction - different implementations for Windows/macOS/Linux
- **CertificateProvider:** Proxy-specific - loads cert and provides to Kestrel at startup
- **CertificateRenewalService:** Hosted service - checks cert expiry periodically in background

## Architectural Patterns

### Pattern 1: Certificate Authority Hierarchy

**What:** Create a local root CA that signs development certificates, similar to mkcert approach.

**When to use:** For local development with multiple `.localhost` domains requiring trusted HTTPS.

**Trade-offs:**
- **Pros:** Single CA trust installation, unlimited wildcard certs, no browser warnings
- **Cons:** Initial trust setup required (one-time), CA private key must be secured

**Example:**
```csharp
// CertificateGenerator creates CA on first run
public class CertificateGenerator
{
    public async Task<CertificateInfo> EnsureCertificateAuthorityAsync()
    {
        var caPath = _stateDirectoryProvider.GetCaCertificatePath();

        if (File.Exists(caPath))
        {
            return LoadExistingAuthority(caPath);
        }

        // Generate new CA with 10-year validity
        var ca = CreateCertificateAuthority(
            subject: "CN=Portless Local Development CA",
            validity: TimeSpan.FromDays(3650)
        );

        await SaveAuthorityAsync(ca, caPath);
        return ca;
    }

    public X509Certificate2 CreateWildcardCertificate(
        X509Certificate2 ca,
        string domain = "*.localhost")
    {
        // Create cert signed by CA with SAN for wildcard domain
        var request = new CertificateRequest(
            $"CN={domain}",
            RSA.Create(2048),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );

        // Add Subject Alternative Name for wildcard
        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName(domain);
        sanBuilder.AddDnsName("localhost"); // Also cover base domain
        request.CertificateExtensions.Add(sanBuilder.Build());

        // Sign with CA
        var cert = request.Create(
            ca,
            notBefore: DateTimeOffset.UtcNow,
            notAfter: DateTimeOffset.UtcNow.AddYears(1),
            serialNumber: BitConverter.GetBytes(DateTime.UtcNow.Ticks)
        );

        return cert;
    }
}
```

### Pattern 2: Dual Endpoint Configuration

**What:** Kestrel listens on both HTTP and HTTPS endpoints simultaneously.

**When to use:** Supporting gradual migration to HTTPS or development scenarios where both protocols needed.

**Trade-offs:**
- **Pros:** Backward compatible, allows gradual HTTPS adoption
- **Cons:** More complex configuration, potential security if HTTP used in production

**Example:**
```csharp
// Portless.Proxy/Program.cs
builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP endpoint (existing)
    var httpPort = builder.Configuration["PORTLESS_PORT"] ?? "1355";
    options.ListenAnyIP(int.Parse(httpPort), listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });

    // HTTPS endpoint (NEW)
    var httpsPort = builder.Configuration["PORTLESS_HTTPS_PORT"] ?? "1356";
    var certProvider = app.Services.GetRequiredService<CertificateProvider>();

    options.ListenAnyIP(int.Parse(httpsPort), listenOptions =>
    {
        listenOptions.UseHttps(certProvider.GetCertificate());
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
});
```

### Pattern 3: Platform-Specific Trust Installation

**What:** Abstract trust installation logic behind interface, implement per-platform.

**When to use:** Cross-platform applications requiring OS-level certificate trust.

**Trade-offs:**
- **Pros:** Clean separation, testable, follows .NET platform conventions
- **Cons:** Requires platform-specific knowledge and testing on each OS

**Example:**
```csharp
public interface ITrustInstaller
{
    Task<TrustResult> InstallTrustAsync(X509Certificate2 certificate);
    Task<TrustResult> RemoveTrustAsync(X509Certificate2 certificate);
    Task<TrustStatus> CheckTrustStatusAsync(X509Certificate2 certificate);
}

// Windows implementation
public class WindowsTrustInstaller : ITrustInstaller
{
    public async Task<TrustResult> InstallTrustAsync(X509Certificate2 certificate)
    {
        using var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadWrite);
        store.Add(certificate);
        return TrustResult.Success;
    }
}

// Linux implementation (.NET 9+)
public class LinuxTrustInstaller : ITrustInstaller
{
    public async Task<TrustResult> InstallTrustAsync(X509Certificate2 certificate)
    {
        // Place certificate in ~/.aspnet/dev-certs/trust
        var certDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".aspnet", "dev-certs", "trust"
        );
        Directory.CreateDirectory(certDir);

        var certPath = Path.Combine(certDir, "portless-ca.pem");
        await File.WriteAllBytesAsync(certPath, certificate.Export(X509ContentType.Cert));

        // Set SSL_CERT_DIR environment variable
        // Note: This requires shell profile updates for persistence
        return TrustResult.SuccessWithEnvironmentVariable;
    }
}

// macOS implementation
public class MacOsTrustInstaller : ITrustInstaller
{
    public async Task<TrustResult> InstallTrustAsync(X509Certificate2 certificate)
    {
        // Use security command-line tool
        var certPath = Path.GetTempFileName();
        await File.WriteAllBytesAsync(certPath, certificate.Export(X509ContentType.Pfx));

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "security",
                Arguments = $"add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain {certPath}",
                RedirectStandardOutput = true,
                UseShellExecute = false
            }
        };

        process.Start();
        await process.WaitForExitAsync();

        File.Delete(certPath);
        return process.ExitCode == 0 ? TrustResult.Success : TrustResult.Failed;
    }
}
```

### Pattern 4: Certificate Renewal Background Service

**What:** Hosted service checks certificate expiry periodically and renews before expiration.

**When to use:** Long-running proxy processes to prevent certificate expiry interrupting development.

**Trade-offs:**
- **Pros:** Seamless renewal, no manual intervention
- **Cons:** Background complexity, must handle reload without dropping connections

**Example:**
```csharp
public class CertificateRenewalService : BackgroundService
{
    private readonly ICertificateStore _certificateStore;
    private readonly ICertificateGenerator _certificateGenerator;
    private readonly ILogger<CertificateRenewalService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var certInfo = await _certificateStore.LoadCertificateAsync();

                // Renew if expiring within 30 days
                if (certInfo.NotAfter < DateTime.UtcNow.AddDays(30))
                {
                    _logger.LogInformation("Certificate expiring on {Expiry}, renewing...",
                        certInfo.NotAfter);

                    var caInfo = await _certificateStore.LoadAuthorityAsync();
                    var newCert = _certificateGenerator.CreateWildcardCertificate(
                        caInfo.Certificate,
                        "*.localhost"
                    );

                    await _certificateStore.SaveCertificateAsync(newCert);

                    _logger.LogInformation("Certificate renewed successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking certificate renewal");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}
```

## Data Flow

### HTTPS Proxy Startup Flow

```
[User runs: portless proxy start]
    ↓
[CLI starts Portless.Proxy process]
    ↓
[Proxy Program.cs starts]
    ↓
[Load services from DI container]
    ├─→ [CertificateStore.LoadCertificateAsync()]
    │   └─→ [Read ~/.portless/cert.pfx]
    │       ├─→ Found? → Load certificate
    │       └─→ Not found? → Trigger generation flow
    ├─→ [TrustInstaller.CheckTrustStatusAsync()]
    │   └─→ [Check if CA is trusted]
    │       ├─→ Trusted? → Continue
    │       └─→ Not trusted? → Log warning, show CLI instructions
    └─→ [CertificateProvider.GetCertificate()]
        └─→ [Return cert to Kestrel]
    ↓
[Kestrel.Configure() called]
    ├─→ [ListenAnyIP(1355) - HTTP endpoint]
    └─→ [ListenAnyIP(1356) - HTTPS with cert]
    ↓
[Proxy starts listening]
    ↓
[Ready to proxy HTTPS requests]
```

### Certificate Generation Flow (First Run)

```
[Proxy starts, no certificate found]
    ↓
[CertificateGenerator.EnsureCertificateAuthorityAsync()]
    ├─→ [Generate RSA 4096-bit key pair]
    ├─→ [Create CA certificate: "Portless Local Development CA"]
    │   ├─→ Validity: 10 years
    │   ├─→ Key Usage: DigitalSignature, CertSign, CRLSign
    │   └─→ Basic Constraints: SubjectType=CA, PathLenConstraint=None
    └─→ [Save to ~/.portless/ca.pfx with password]
    ↓
[CertificateGenerator.CreateWildcardCertificateAsync()]
    ├─→ [Generate RSA 2048-bit key pair]
    ├─→ [Create certificate request: "*.localhost"]
    ├─→ [Add SAN: *.localhost, localhost]
    ├─→ [Sign with CA private key]
    │   ├─→ Validity: 1 year
    │   ├─→ Key Usage: DigitalSignature, KeyEncipherment
    │   ├─→ Extended Key Usage: ServerAuth
    │   └─→ Subject Alternative Name: DNS:*.localhost, DNS:localhost
    └─→ [Save to ~/.portless/cert.pfx]
    ↓
[CertificateStore.SaveCertificateInfoAsync()]
    └─→ [Save metadata to ~/.portless/cert-info.json]
    ↓
[TrustInstaller.InstallTrustAsync() - Manual step required]
    ├─→ [Windows: Add to CurrentUser\Root store]
    ├─→ [macOS: Run security add-trusted-cert]
    └─→ [Linux: Add to ~/.aspnet/dev-certs/trust + NSS DB]
    ↓
[User instructed to run: portless cert install]
    └─→ [CLI executes trust installation with elevated privileges if needed]
```

### HTTPS Request Flow

```
[Browser: https://api.localhost:1356]
    ↓
[Kestrel HTTPS endpoint receives request]
    ├─→ [TLS handshake with certificate]
    │   ├─→ [Browser validates certificate]
    │   ├─→ [Browser checks SAN: *.localhost matches api.localhost]
    │   └─→ [Browser verifies CA trust (if installed)]
    └─→ [Request decrypted, passed to middleware pipeline]
    ↓
[ForwardedHeaders middleware]
    └─→ [Add X-Forwarded-Proto: https]
    ↓
[YARP ReverseProxy middleware]
    ├─→ [Match route: api.localhost → cluster-api.localhost]
    ├─→ [Forward to backend: http://localhost:4001]
    └─→ [Add X-Forwarded-* headers]
    ↓
[Backend app receives request]
    ├─→ [Sees X-Forwarded-Proto: https]
    └─→ [Can generate secure URLs, cookies, etc.]
```

### Certificate Management CLI Flow

```
[portless cert status]
    ↓
[CLI → CertificateStore.LoadCertificateInfoAsync()]
    ├─→ [Read ~/.portless/cert-info.json]
    └─→ [Display: Subject, SAN, NotBefore, NotAfter, Trust status]
    ↓
[Output table showing certificate details]
```

```
[portless cert install]
    ↓
[CLI → CertificateStore.LoadAuthorityAsync()]
    └─→ [Load CA certificate from ~/.portless/ca.pfx]
    ↓
[CLI → TrustInstaller.InstallTrustAsync(caCertificate)]
    ├─→ [Windows: Add to Root store, may require admin]
    ├─→ [macOS: Execute security command with sudo]
    └─→ [Linux: Add to NSS DB, may require sudo]
    ↓
[Display success with trust status check]
```

```
[portless cert renew]
    ↓
[CLI → CertificateGenerator.CreateWildcardCertificateAsync(ca)]
    └─→ [Generate new certificate, serial number = timestamp]
    ↓
[CLI → CertificateStore.SaveCertificateAsync(newCert)]
    └─→ [Overwrite ~/.portless/cert.pfx]
    ↓
[Display: Certificate renewed, proxy restart recommended]
    └─→ [Note: Kestrel needs restart to reload certificate]
```

## Scaling Considerations

| Scale | Architecture Adjustments |
|-------|--------------------------|
| 1-10 concurrent HTTPS requests | Default Kestrel configuration is sufficient |
| 10-100 concurrent HTTPS requests | Consider TLS session resumption for performance |
| 100+ concurrent HTTPS requests | Monitor TLS handshake overhead, consider hardware acceleration |

### Scaling Priorities

1. **First bottleneck:** TLS handshake CPU cost - Mitigate with session resumption and HTTP/2
2. **Second bottleneck:** Certificate reload requires proxy restart - Implement certificate hot-reload in future version

## Anti-Patterns

### Anti-Pattern 1: Using dotnet dev-certs Directly

**What people do:** Rely on `dotnet dev-certs https --trust` for Portless certificates.

**Why it's wrong:** dotnet dev-certs creates certificates with `localhost` CN but not `*.localhost` SAN. Wildcard certificates for top-level domains are invalid per RFC. Also, dotnet dev-certs doesn't create a CA for signing multiple certificates.

**Do this instead:** Create a dedicated CA and generate custom wildcard certificates with proper SAN for `*.localhost` and `localhost`.

### Anti-Pattern 2: Hardcoding Certificate Passwords

**What people do:** Store certificate passwords in code or configuration files.

**Why it's wrong:** Private keys are exposed, breaking the security model. Even for development, this is bad practice.

**Do this instead:** Use secure storage (Windows DPAPI, macOS Keychain, Linux secret service) or generate random passwords stored in protected user config. For development, can use empty password but document the security implication.

### Anti-Pattern 3: Ignoring Certificate Expiry

**What people do:** Generate certificates with 100-year validity to avoid renewal.

**Why it's wrong:** Violates certificate best practices, may not be accepted by browsers/security libraries, makes key rotation impossible.

**Do this instead:** Use 1-year validity for certificates, 10-year validity for CA, implement automatic renewal background service.

### Anti-Pattern 4: Mixing HTTP and HTTPS Routes

**What people do:** Proxy HTTP requests to HTTPS backends or vice versa.

**Why it's wrong:** Breaks secure context requirements (Service Workers, WebRTC, secure cookies), causes mixed content warnings, loses HTTPS benefits.

**Do this instead:** Keep protocol separation clear - HTTP endpoint proxies to HTTP backends, HTTPS endpoint proxies to HTTP backends (development backends typically use HTTP). Use X-Forwarded-Proto header so backends know original protocol.

### Anti-Pattern 5: Trusting CA in Machine Store (Windows)

**What people do:** Install development CA in LocalMachine\Root store requiring admin privileges.

**Why it's wrong:** Security risk (affects all users), requires elevation for every install/uninstall, UAC prompts disrupt developer workflow.

**Do this instead:** Install in CurrentUser\Root store - trusted only for current user, no elevation required, better security isolation.

## Integration Points

### External Services

| Service | Integration Pattern | Notes |
|---------|---------------------|-------|
| OS Certificate Store | Platform-specific ITrustInstaller implementations | Windows: X509Store, macOS: security CLI, Linux: certutil/NSS |
| dotnet dev-certs | None (avoid - see Anti-Patterns) | Could reference for .NET 9+ Linux trust implementation pattern |
| mkcert | Optional reference | Similar CA hierarchy pattern, good for validation testing |

### Internal Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| Proxy ↔ CertificateStore | Direct in-process | Singleton service, file-based persistence |
| CLI ↔ CertificateStore | Via API endpoints or direct | CLI uses Portless.Core services directly |
| Proxy ↔ TrustInstaller | CLI-initiated, not proxy | Trust installation is user action, not proxy startup concern |
| CertificateRenewalService ↔ CertificateStore | Polling-based | Background service checks cert info file periodically |

## Build Order Dependencies

### Phase 13: Certificate Authority and Generation
**Dependencies:** None (new service layer)
**Components:**
- ICertificateStore, CertificateStore (Portless.Core)
- ICertificateGenerator, CertificateGenerator (Portless.Core)
- CertificateInfo model (Portless.Core)
- CertificateConfiguration (Portless.Core)

**Outcome:** CA and wildcard certificate files in ~/.portless/

### Phase 14: HTTPS Endpoint Configuration
**Dependencies:** Phase 13 (needs CertificateStore)
**Components:**
- CertificateProvider (Portless.Proxy)
- Modify Portless.Proxy/Program.cs (add HTTPS ListenAnyIP)
- Environment variable: PORTLESS_HTTPS_PORT (default: 1356)

**Outcome:** Proxy listens on both HTTP (1355) and HTTPS (1356)

### Phase 15: Trust Installation
**Dependencies:** Phase 13 (needs CA certificate)
**Components:**
- ITrustInstaller interface (Portless.Core)
- WindowsTrustInstaller (Portless.Core)
- MacOsTrustInstaller (Portless.Core)
- LinuxTrustInstaller (Portless.Core)
- Platform detection logic

**Outcome:** CA installed as trusted in OS certificate store

### Phase 16: Certificate Management CLI Commands
**Dependencies:** Phase 13, 14, 15 (needs full cert stack)
**Components:**
- CertStatusCommand (Portless.Cli)
- CertInstallCommand (Portless.Cli)
- CertRenewCommand (Portless.Cli)
- CertUninstallCommand (Portless.Cli)

**Outcome:** Users can manage certificates via CLI

### Phase 17: Certificate Renewal Service
**Dependencies:** Phase 13, 14 (needs CertificateStore + HTTPS endpoint)
**Components:**
- CertificateRenewalService hosted service (Portless.Core)
- Integration with proxy startup

**Outcome:** Automatic certificate renewal before expiry

### Phase 18: HTTPS Testing and Documentation
**Dependencies:** All previous phases
**Components:**
- Integration tests for HTTPS proxy
- Integration tests for certificate generation
- Integration tests for trust installation
- Documentation: HTTPS setup guide
- Documentation: Certificate troubleshooting

**Outcome:** Tested and documented HTTPS feature

## Key Technical Decisions

### Decision 1: Use System.Security.Cryptography.X509Certificates (Native .NET)

**Rationale:** Built-in .NET API for certificate generation, no external dependencies like BouncyCastle required.

**Trade-offs:**
- **Pros:** No package dependency, maintained by Microsoft, cross-platform
- **Cons:** Less flexible than BouncyCastle for advanced scenarios, but sufficient for development certs

**Confidence:** HIGH - Microsoft documentation shows CertificateRequest supports all required features (SAN, CA signing, wildcard DNS).

### Decision 2: Use *.localhost Domain (Not *.dev.localhost)

**Rationale:** Portless already uses `.localhost` suffix for hostnames. Using `*.dev.localhost` would require changing all existing hostname patterns and user documentation.

**Trade-offs:**
- **Pros:** Consistent with existing Portless patterns, simpler user experience
- **Cons:** Wildcard certificates for top-level domains are technically invalid per RFC, but `.localhost` is reserved for special use (RFC 6761), browsers accept it

**Confidence:** MEDIUM - Need to validate browser acceptance of `*.localhost` wildcard certificates. Safari on macOS may have issues (see .NET 10 docs recommending `*.dev.localhost`).

**Mitigation:** If testing reveals browser rejection, add `*.dev.localhost` as additional SAN and support both patterns.

### Decision 3: Store Certificates in ~/.portless/ Directory

**Rationale:** Consistent with existing route storage, follows Portless patterns, user-owned location (no admin required).

**Trade-offs:**
- **Pros:** Simple, cross-platform, no elevation required
- **Cons:** Less secure than OS keychain for private keys

**Confidence:** HIGH - Development certificates, security risk is acceptable. Future enhancement could integrate with OS keychain.

### Decision 4: Separate HTTP and HTTPS Ports (1355, 1356)

**Rationale:** Allows both protocols to coexist, backward compatible, easier debugging.

**Trade-offs:**
- **Pros:** Simple configuration, can test HTTP vs HTTPS easily
- **Cons:** Two ports to remember, potential confusion

**Confidence:** HIGH - Matches development tool conventions (many tools use separate ports for HTTP/HTTPS).

### Decision 5: Certificate Renewal Requires Proxy Restart

**Rationale:** Kestrel doesn't support certificate hot-reload without complex configuration.

**Trade-offs:**
- **Pros:** Simpler implementation, explicit control over when cert changes take effect
- **Cons:** Brief downtime during renewal

**Confidence:** HIGH - Acceptable for development tool. Future enhancement could implement graceful reload.

## Sources

### High Confidence (Official Documentation)

- [Microsoft Learn - Kestrel HTTPS endpoint configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints) - HIGH confidence, official .NET 10 documentation
- [Microsoft Learn - CertificateRequest.CreateSelfSigned method](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.certificaterequest.createselfsigned) - HIGH confidence, official .NET API reference
- [Microsoft Learn - SubjectAlternativeNameBuilder class](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.subjectalternativenamebuilder) - HIGH confidence, official .NET API reference
- [Microsoft Learn - .NET 9 Linux certificate trust improvements](https://learn.microsoft.com/en-us/dotnet/core/compatibility/deployment/9.0/linux-dev-certs-trust) - HIGH confidence, official .NET 9 release notes
- [Microsoft Learn - .NET 10 .localhost domain support](https://learn.microsoft.com/en-us/dotnet/core/compatibility/deployment/10.0/localhost-domain) - HIGH confidence, official .NET 10 documentation
- [YARP Documentation - HTTPS Configuration](https://microsoft.github.io/reverse-proxy/) - HIGH confidence, official YARP documentation

### Medium Confidence (Web Search Verified)

- [mkcert GitHub Repository](https://github.com/FiloSottile/mkcert) - MEDIUM confidence, reference implementation for local CA pattern
- [CodeProject - BouncyCastle C# Certificate Generation](https://www.codeproject.com/Articles/1349071/Generating-a-Certificate-using-a-Csharp-Bouncy-Cas) - LOW confidence, using native .NET APIs instead

### Low Confidence (Web Search Only)

- ASP.NET Core HTTPS development certificate patterns - Need verification with official docs
- Browser wildcard certificate validation behavior - Need testing to verify

## Open Questions for Phase-Specific Research

1. **Wildcard .localhost Certificate Browser Acceptance:** Do all major browsers accept `*.localhost` in SAN, or is `*.dev.localhost` required? (Requires testing in Phase 13)

2. **Safari macOS Behavior:** Does Safari on macOS resolve `*.localhost` correctly, or does it require special handling? (Requires testing in Phase 15)

3. **Certificate Hot-Reload:** Can Kestrel reload certificate without restart using custom configuration? (Requires research in Phase 17 if needed)

4. **Linux Trust Persistence:** Does SSL_CERT_DIR approach persist across shell sessions, or requires profile modification? (Requires testing in Phase 15)

---
*Architecture research for: Portless.NET HTTPS with Automatic Certificates*
*Researched: 2025-02-22*
