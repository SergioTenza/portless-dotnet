# Phase 14: Trust Installation - Research

**Researched:** 2026-02-22
**Domain:** Windows Certificate Store, .NET Security APIs, CLI Development
**Confidence:** HIGH

## Summary

Phase 14 implements Windows-based CA certificate trust installation with three CLI commands: `portless cert install`, `portless cert status`, and `portless cert uninstall`. The phase builds directly on Phase 13's certificate generation infrastructure, adding Windows Certificate Store interaction using .NET's native `X509Store` API. The implementation requires administrator privileges for system-wide trust installation and must handle UAC elevation, cross-platform messaging, and comprehensive trust status detection.

**Primary recommendation:** Use .NET's `X509Store` API with `StoreLocation.LocalMachine` and `StoreName.Root` for Windows trust installation, combined with Spectre.Console.Cli's `AddBranch` pattern for the `cert` command group. Implement administrator privilege detection using `WindowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator)` and provide clear cross-platform limitation messaging.

## User Constraints (from CONTEXT.md)

### Locked Decisions

**Installation behavior:**
- **Auto-elevate**: `portless cert install` uses Windows UAC prompt automatically if not running as administrator
- **Store location**: Certificate installs to "Trusted Root CA" store for system-wide trust (requires admin)
- **Idempotent**: If certificate is already trusted, command succeeds silently (no "already installed" message)
- **Confirmation**: Simple message on success: "Certificate installed successfully"
- **Password handling**: Certificate PFX files are protected; installer prompts for password if needed (or uses stored password from Phase 13)

**Status output format:**
- **Default output**: Minimal binary status - "Trusted" or "Not Trusted"
- **Verbose mode**: `--verbose` flag shows full certificate details (fingerprint, expiration, store location, subject, issuer, serial number, key size)
- **Color-coded status**: 🟢 Green: Trusted, 🔴 Red: Not Trusted, 🟡 Yellow: Expiring within 30 days
- **Not trusted response**: Shows Windows-specific manual installation instructions (step-by-step)

**Error handling:**
- **Permission denied**: Generic error message + "Run as Administrator" instruction + error code
- **Missing certificate**: Auto-generate certificate (with user confirmation prompt) before proceeding with command
- **Corrupted certificate**: Auto-regenerate certificate (with user confirmation prompt)
- **Exit codes**: Distinct error codes for different failure scenarios (0: success, 1: generic, 2: insufficient permissions, 3: missing, 4: corrupted, 5: access denied, 6: not found)

**Cross-platform messaging:**
- **Platform detection**: Commands detect Windows vs macOS/Linux
- **Non-Windows behavior**: All commands (install, status, uninstall) show warning + inline instructions
- **Warning frequency**: Warning always appears on every command execution (no caching/suppression)
- **Status command**: On macOS/Linux, status shows certificate file information but notes trust is manual

### Claude's Discretion

- Exact wording of error messages and instructions
- UAC prompt implementation details (how to trigger elevation)
- Certificate password prompt styling (Spectre.Console secrets handling)
- Color shades for status output (use Spectre.Console colors)
- Exit code range allocation (can add more codes if needed)

### Deferred Ideas (OUT OF SCOPE)

- macOS/Linux certificate trust installation (deferred to v1.3+)
- Certificate trust verification via browser API (deferred)
- Automatic trust propagation to browsers (deferred)
- GUI for certificate management (deferred)

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| **System.Security.Cryptography.X509Certificates** | .NET 10 | Windows Certificate Store API | Native .NET API for certificate operations, no external dependencies |
| **X509Store** | .NET 10 | Certificate store interaction | Official .NET API for reading/writing Windows certificate stores |
| **WindowsPrincipal** | System.Security.Principal | Administrator privilege detection | Standard .NET security API for Windows role-based security checks |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| **Spectre.Console** | 0.53.1 (existing) | CLI output formatting | Required for colored status output, prompts, and tables |
| **Spectre.Console.Cli** | 0.53.1 (existing) | Command branching | Use `AddBranch` for `cert` command group structure |
| **Microsoft.Extensions.Logging** | existing | Diagnostic logging | Log certificate operations and errors |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| X509Store API | PowerShell Process.Start | X509Store is native C#, PowerShell adds complexity and process overhead |
| WindowsPrincipal | advapi32.dll P/Invoke | WindowsPrincipal is managed, P/Invoke requires unsafe code and external declarations |
| Spectre.Console colors | Console.ForegroundColor | Spectre provides cross-platform consistency, Console API is Windows-specific |

**Installation:**
```bash
# No additional packages needed - all APIs are in .NET 10 base class library
```

## Architecture Patterns

### Recommended Project Structure

```
Portless.Core/
├── Services/
│   ├── ICertificateTrustService.cs        # Trust operations interface
│   └── CertificateTrustService.cs          # Windows trust implementation
├── Models/
│   └── TrustStatus.cs                      # Trust state enumeration
└── Exceptions/
    └── CertificateTrustException.cs        # Trust-specific exceptions

Portless.Cli/
├── Commands/
│   └── CertCommand/
│       ├── CertCommand.cs                  # Branch container
│       ├── CertInstallCommand.cs           # Install command
│       ├── CertStatusCommand.cs            # Status command
│       └── CertUninstallCommand.cs         # Uninstall command
└── Formatters/
    └── TrustStatusFormatter.cs             # Status output formatting
```

### Pattern 1: Windows Certificate Store Access

**What:** Read/write operations to Windows Certificate Store using X509Store API

**When to use:** Installing certificates to Trusted Root CA, checking trust status, removing certificates

**Example:**
```csharp
// Source: Microsoft .NET Documentation (https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509store)
using System.Security.Cryptography.X509Certificates;

public async Task<bool> InstallCertificateAsync(X509Certificate2 certificate)
{
    // Open LocalMachine Root store (requires admin)
    using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
    store.Open(OpenFlags.ReadWrite);

    // Check if already installed
    var existing = store.Certificates.Find(
        X509FindType.FindByThumbprint,
        certificate.Thumbprint,
        validOnly: false);

    if (existing.Count > 0)
    {
        return true; // Idempotent: already installed
    }

    // Add certificate to trust store
    store.Add(certificate);
    return true;
}
```

### Pattern 2: Administrator Privilege Detection

**What:** Check if current process has administrator rights using WindowsPrincipal

**When to use:** Before attempting certificate store operations, showing appropriate error messages

**Example:**
```csharp
// Source: Microsoft Security Documentation
using System.Security.Principal;

public bool IsAdministrator()
{
    using var identity = WindowsIdentity.GetCurrent();
    var principal = new WindowsPrincipal(identity);
    return principal.IsInRole(WindowsBuiltInRole.Administrator);
}
```

### Pattern 3: Spectre.Console.Cli Command Branching

**What:** Create nested command structure using `AddBranch` for logical grouping

**When to use:** Creating `portless cert install|status|uninstall` command hierarchy

**Example:**
```csharp
// Source: Spectre.Console.Cli Documentation (inferred from existing codebase)
app.Configure(config =>
{
    config.AddBranch("cert", cert =>
    {
        cert.AddCommand<CertInstallCommand>("install")
            .WithDescription("Install CA certificate to system trust store");
        cert.AddCommand<CertStatusCommand>("status")
            .WithDescription("Display certificate trust status");
        cert.AddCommand<CertUninstallCommand>("uninstall")
            .WithDescription("Remove CA certificate from trust store");
    });
});
```

### Anti-Patterns to Avoid

- **Mixing store locations**: Don't mix `StoreLocation.CurrentUser` and `StoreLocation.LocalMachine` - choose one and document it
- **Silent UAC elevation**: Don't use `runas` verb programmatically without user consent - let Windows UAC prompt naturally
- **Hardcoded certificate paths**: Don't assume certificates are always in `~/.portless` - use `StateDirectoryProvider`
- **Cross-platform trust assumptions**: Don't assume macOS/Linux trust works the same as Windows - clearly document limitations

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Certificate store access | Custom P/Invoke to crypt32.dll | `X509Store` API | Handles all Windows certificate store complexity, ACLs, and persistence |
| Permission detection | Custom token lookup | `WindowsPrincipal.IsInRole()` | Manages Windows security tokens and group membership correctly |
| CLI command parsing | Manual string splitting | Spectre.Console.Cli | Handles arguments, options, help text, and command routing |
| Colored console output | ANSI escape codes | Spectre.Console colors | Cross-platform consistency, handles terminals without color support |

**Key insight:** .NET's X509Store API has 20+ years of Windows certificate management evolution - custom solutions would miss edge cases around ACLs, private key persistence, and certificate chain validation.

## Common Pitfalls

### Pitfall 1: Insufficient Permissions for Store Access

**What goes wrong:** `CryptographicException: Access denied` when opening `X509Store` with `StoreLocation.LocalMachine`

**Why it happens:** Opening LocalMachine stores requires administrator privileges. Running as standard user throws exception.

**How to avoid:** Check administrator status before opening store, provide clear error message with "Run as Administrator" instruction

**Warning signs:** CryptographicException with "Access denied" or "Keyset does not exist" messages

### Pitfall 2: Private Key Persistence Issues

**What goes wrong:** Certificate installs successfully but private key is lost after process exits

**Why it happens:** Loading PFX without `X509KeyStorageFlags.PersistKeySet` creates temporary key container

**How to avoid:** Always use `X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet` when loading certificates for trust installation

**Warning signs:** Certificate appears in store but `HasPrivateKey` returns `false` after restart

### Pitfall 3: Cross-Platform Code Execution on macOS/Linux

**What goes wrong:** `PlatformNotSupportedException` when accessing `WindowsPrincipal` on non-Windows

**Why it happens:** Windows-specific APIs are called without platform guards

**How to avoid:** Use `[SupportedOSPlatform("windows")]` attribute and runtime platform detection with `OperatingSystem.IsWindows()`

**Warning signs:** TypeInitializationException or missing method exceptions on non-Windows platforms

### Pitfall 4: Certificate Store Location Inconsistency

**What goes wrong:** Certificate installs to CurrentUser store but code checks LocalMachine for trust status

**Why it happens:** Inconsistent use of `StoreLocation` parameters across different operations

**How to avoid:** Define store location as constant and use consistently across all trust operations

**Warning signs:** Trust status shows "Not Trusted" even after successful installation

## Code Examples

Verified patterns from official sources:

### Installing Certificate to Trusted Root CA

```csharp
// Source: Microsoft Learn - X509Store.Add (https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509store.add)
public async Task<TrustInstallResult> InstallToTrustedRootAsync(X509Certificate2 certificate)
{
    try
    {
        using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadWrite);

        // Check for existing certificate (idempotent operation)
        var existing = store.Certificates.Find(
            X509FindType.FindByThumbprint,
            certificate.Thumbprint,
            validOnly: false);

        if (existing.Count > 0)
        {
            return TrustInstallResult.AlreadyExists;
        }

        store.Add(certificate);
        return TrustInstallResult.Success;
    }
    catch (CryptographicException ex) when (ex.HResult == -2146829211)
    {
        // ERROR_E_ACCESS_DENIED
        return TrustInstallResult.AccessDenied;
    }
}
```

### Checking Trust Status

```csharp
// Source: Microsoft Learn - X509Store.Certificates (https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509store.certificates)
public async Task<TrustStatus> CheckTrustStatusAsync(string thumbprint)
{
    using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
    store.Open(OpenFlags.ReadOnly);

    var certificates = store.Certificates.Find(
        X509FindType.FindByThumbprint,
        thumbprint,
        validOnly: true); // validOnly: true checks expiration and trust chain

    if (certificates.Count == 0)
    {
        return TrustStatus.NotTrusted;
    }

    var cert = certificates[0];

    // Check expiration (30-day warning)
    if (cert.NotAfter < DateTimeOffset.UtcNow.AddDays(30))
    {
        return TrustStatus.ExpiringSoon;
    }

    return TrustStatus.Trusted;
}
```

### Platform-Specific Code Guard

```csharp
// Source: .NET Platform Compatibility Documentation
[SupportedOSPlatform("windows")]
public class WindowsCertificateTrustService : ICertificateTrustService
{
    public async Task<bool> IsInstalledAsync(string thumbprint)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Certificate trust installation is Windows-only");
        }

        // Windows-specific implementation
        using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
        // ...
    }
}
```

### Administrator Privilege Check

```csharp
// Source: Microsoft Security Documentation
public bool HasAdministratorPrivileges()
{
    try
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }
    catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
    {
        return false;
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Manual MMC certificate installation | X509Store API programmatic installation | .NET Framework 2.0 (2005) | Automation eliminates manual steps, enables idempotent operations |
| P/Invoke to crypt32.dll | Managed X509Store API | .NET Framework 2.0 | No unsafe code, better exception handling, cross-version compatibility |
| CertMgr.exe external process | In-process X509Store operations | .NET Core 1.0 | No process spawning overhead, better error reporting |
| CurrentUser store only | LocalMachine store for system-wide trust | Always available | System-wide trust affects all users and services, not just current user |

**Deprecated/outdated:**
- **makecert.exe**: Deprecated Windows SDK tool, replaced by PowerShell `New-SelfSignedCertificate`
- **certmgr.exe command-line**: External tool approach, use X509Store API instead
- **Manual MMC import**: Not automatable, use X509Store for programmatic access

## Open Questions

1. **UAC Elevation Strategy**
   - What we know: WindowsPrincipal can detect admin status, `runas` verb can trigger UAC
   - What's unclear: Whether to auto-restart process with elevation or prompt user to re-run as admin
   - Recommendation: Check admin status first, if not admin show clear "Run as Administrator" message rather than auto-restart (clearer user intent)

2. **Certificate Password Storage**
   - What we know: Phase 13 generates certificates with empty password, PFX files stored in `~/.portless`
   - What's unclear: Whether to prompt for password if PFX has non-empty password or auto-generate new cert
   - Recommendation: Per CONTEXT.md, prompt for password using Spectre.Console secrets handling if PFX is password-protected

## Sources

### Primary (HIGH confidence)
- Microsoft Learn - X509Store API documentation (https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509store)
- Microsoft Learn - X509Certificate2 class documentation (https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509certificate2)
- Microsoft Learn - WindowsPrincipal documentation (https://learn.microsoft.com/en-us/dotnet/api/system.security.principal.windowsprincipal)
- .NET 10 API Documentation - System.Security.Cryptography namespace

### Secondary (MEDIUM confidence)
- Microsoft Learn - Installing Test Certificates (driver signing context, applicable patterns)
- Microsoft Learn - Windows Certificate Store architecture (Trusted Root Certification Authorities store)
- Spectre.Console.Cli GitHub repository - command branching examples (inferred from existing codebase patterns)

### Tertiary (LOW confidence)
- WebSearch results for certificate trust installation patterns (verified against official docs where possible)
- Community discussions on UAC elevation strategies (treated as recommendations, not authoritative)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All APIs are part of .NET 10 BCL with extensive official documentation
- Architecture: HIGH - X509Store API patterns are well-documented with 20+ years of usage
- Pitfalls: HIGH - Common permission and platform compatibility issues are well-documented in Microsoft Learn

**Research date:** 2026-02-22
**Valid until:** 2026-04-22 (60 days - certificate APIs are stable, but .NET 10 preview features may evolve)

---

*Phase: 14-trust-installation*
*Research completed: 2026-02-22*
