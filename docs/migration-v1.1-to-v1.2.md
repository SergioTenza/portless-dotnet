# Migration Guide: v1.1 to v1.2

Guide for upgrading from Portless.NET v1.1 to v1.2.

## Overview

**v1.2 HTTPS with Automatic Certificates** adds HTTPS support with automatic certificate generation while maintaining full backward compatibility with v1.1.

**Release Date:** 2026-03-02
**Milestone:** v1.2 HTTPS with Automatic Certificates
**Compatibility:** Fully backward compatible

---

## What's New in v1.2

### HTTPS Endpoints and Certificates

- **Automatic certificate generation** for `*.localhost` domains
- **5-year certificate validity** - no frequent renewal needed
- **Dual endpoints** - HTTP (1355) and HTTPS (1356) run simultaneously
- **Zero configuration** - certificates generated on first HTTPS start
- **Wildcard certificate** - valid for all `*.localhost` hostnames

**Quick start:**
```bash
portless proxy start --https
# Certificate auto-generated on first use
```

### New Certificate Management Commands

- **`portless cert install`** - Install CA certificate to system trust (Windows automatic)
- **`portless cert status`** - Display certificate trust status with colored output
- **`portless cert check`** - Check certificate expiration status
- **`portless cert renew`** - Renew certificate (automatic or manual)
- **`portless cert uninstall`** - Remove CA certificate from trust store

### Automatic Renewal and Monitoring

- **Background certificate expiration checking** every 6 hours
- **Automatic renewal** within 30 days of expiration
- **Configurable monitoring intervals** via environment variables
- **Non-blocking startup checks** - proxy starts even with expired cert
- **Colored console warnings** for certificate status

### Mixed HTTP/HTTPS Mode

- **Both endpoints run simultaneously** - no need to choose
- **X-Forwarded-Proto header** preserved for backend apps
- **API endpoints excluded** from HTTPS redirect (for CLI add/remove operations)
- **Flexible configuration** - enable HTTPS when ready

---

## Breaking Changes

**None!** v1.2 is fully backward compatible with v1.1.

### What This Means

- All existing v1.1 commands work unchanged
- Existing HTTP-only setups work unchanged
- HTTPS is opt-in via `--https` flag
- No code changes required in your applications
- No configuration file changes needed

### Automatic Upgrades

When you upgrade to v1.2:
- Everything continues working as before (HTTP-only)
- HTTPS is available but not enabled by default
- You can try HTTPS without affecting existing setup
- Rollback is always possible

**Your existing apps:**
- Continue working on HTTP (port 1355)
- Can optionally use HTTPS (port 1356) when you're ready
- No changes required to `launchSettings.json` or `appsettings.json`

---

## Configuration Changes

### No Configuration Required

v1.2 works with your existing v1.1 configuration without any changes.

### Optional: Enable HTTPS

**Basic HTTPS:**
```bash
portless proxy start --https
```

**With monitoring:**
```bash
export PORTLESS_ENABLE_MONITORING=true
portless proxy start --https
```

**That's it!** Certificates are generated automatically on first use.

### Optional: Certificate Management

**Install CA certificate (Windows):**
```bash
# Run as Administrator
portless cert install

# Verify installation
portless cert status
```

**Check certificate status:**
```bash
portless cert check
```

**Renew certificate:**
```bash
portless cert renew
```

### Optional: Environment Variables

Configure certificate monitoring behavior:

```bash
# Warning threshold (default: 30 days)
export PORTLESS_CERT_WARNING_DAYS=60

# Check interval (default: 6 hours)
export PORTLESS_CERT_CHECK_INTERVAL_HOURS=12

# Disable auto-renewal (default: true)
export PORTLESS_AUTO_RENEW=false

# Enable background monitoring (default: false)
export PORTLESS_ENABLE_MONITORING=true
```

---

## CLI Changes

### New Commands

**Certificate management:**
```bash
portless cert install     # Install CA to trust store [NEW in v1.2]
portless cert status      # Display trust status [NEW in v1.2]
portless cert check       # Check expiration status [NEW in v1.2]
portless cert renew       # Renew certificate [NEW in v1.2]
portless cert uninstall   # Remove CA from trust store [NEW in v1.2]
```

### New Options

**`portless proxy start`:**
```bash
--https    # Enable HTTPS endpoint (port 1356) [NEW in v1.2]
```

### Unchanged Commands

All existing commands work as before:
- `portless proxy start` - No changes (HTTP-only by default)
- `portless proxy stop` - No changes
- `portless proxy status` - No changes
- `portless list` - No changes
- `portless <hostname> <command>` - No changes

---

## New Features Guide

### Using HTTPS

**Step 1: Start proxy with HTTPS**
```bash
portless proxy start --https
```

**Step 2: Access your apps via HTTPS**
```bash
# Your app URLs now work with HTTPS:
https://myapp.localhost:1356
https://chat.localhost:1356
https://api.localhost:1356
```

**Step 3: (Optional) Install CA certificate for trusted connections**
```bash
# Windows users
portless cert install

# Verify
portless cert status
```

**That's it!** Your apps work on both HTTP and HTTPS simultaneously.

### Managing Certificates

**Check certificate status:**
```bash
portless cert check
```

Output:
```
✓ Certificate is valid
Expires: 2030-02-23 (1825 days remaining)
SHA-256: ABC123DEF456...
```

**Renew certificate:**
```bash
# Automatic renewal (if expiring within 30 days)
portless cert renew

# Force renewal (regenerate even if valid)
portless cert renew --force
```

**Check trust status:**
```bash
portless cert status
```

Output:
```
✓ Portless.NET Development CA is trusted

Certificate Details:
  Subject: CN=Portless.NET Development CA
  Thumbprint: ABC123DEF456...
  Expires: 2030-02-23 (1825 days remaining)

Installation: Windows Certificate Store
```

### Mixed HTTP/HTTPS Mode

Both endpoints run simultaneously when you enable HTTPS:

```bash
portless proxy start --https
```

**HTTP endpoint (port 1355):**
- http://myapp.localhost:1355
- Used by CLI for add/remove operations
- Available for legacy apps

**HTTPS endpoint (port 1356):**
- https://myapp.localhost:1356
- Encrypted connections
- Modern web standards

**X-Forwarded-Proto header:**
Your backend apps receive the original protocol:
```csharp
// In your ASP.NET Core app
var protocol = Request.Headers["X-Forwarded-Proto"];
// Value: "https" or "http"
```

---

## Troubleshooting

### Browser shows certificate warnings

**Issue:** HTTPS works but browser shows "Not Trusted" warning

**Cause:** CA certificate not installed to system trust store

**Solution:**

**Windows:**
```bash
# Run as Administrator
portless cert install

# Verify
portless cert status
```

**macOS/Linux:**
See [Platform-Specific Installation](certificate-troubleshooting-macos-linux.md) for manual installation steps.

---

### Certificate expired but proxy started

**Issue:** Proxy started with expired certificate, HTTPS shows warnings

**Cause:** Proxy startup check is non-blocking (allows manual intervention)

**Solution:**
```bash
# Renew certificate
portless cert renew

# Restart proxy
portless proxy stop
portless proxy start --https
```

---

### macOS/Linux manual installation

**Issue:** Automatic trust installation not supported on macOS/Linux in v1.2

**Solution:** See [Certificate Trust Installation for macOS/Linux](certificate-troubleshooting-macos-linux.md) for manual installation steps.

**Note:** Automatic installation for macOS/Linux is planned for v1.3.

---

### Certificate files are missing

**Issue:** Proxy fails to start with "Certificate not found" error

**Solution:**
```bash
# Generate new certificates
portless proxy start --https
```

Certificates are auto-generated on first HTTPS start.

---

## Rollback Plan

If you need to rollback to v1.1:

```bash
# Uninstall v1.2
dotnet tool uninstall -g portless.dotnet

# Install v1.1
dotnet tool install -g portless.dotnet --version 1.1.0
```

**Note:** Your apps and configuration will work with v1.1 (just without HTTPS support).

**If you generated certificates in v1.2:**
- Certificate files remain in `~/.portless/`
- HTTP-only operation works fine
- Certificates don't affect HTTP operation
- You can delete `~/.portless/` if desired

---

## Summary

**Upgrade difficulty:** Easy
**Breaking changes:** None
**Required actions:** None (just upgrade)
**Recommended actions:** Try HTTPS with `--https` flag, install CA certificate (Windows)

**Next steps:**
1. Upgrade to v1.2
2. Try HTTPS: `portless proxy start --https`
3. Install CA certificate: `portless cert install` (Windows)
4. Read [Certificate Lifecycle Management](certificate-lifecycle.md)
5. Read [Certificate Security Considerations](certificate-security.md)

---

## Need Help?

- [Certificate Lifecycle Management](certificate-lifecycle.md) - Complete certificate documentation
- [Certificate Security Considerations](certificate-security.md) - Security best practices
- [Platform-Specific Installation](certificate-troubleshooting-macos-linux.md) - macOS/Linux manual installation
- [Main README](../README.md) - General documentation

---

*Migration Guide*
*Version: 1.1 -> 1.2*
*Updated: 2026-03-02*
