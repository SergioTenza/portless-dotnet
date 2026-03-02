# Certificate Lifecycle Management

Portless.NET provides automatic certificate lifecycle management for local development HTTPS support. Certificates are generated once and automatically monitored for expiration.

## Overview

> **⚠️ Platform Availability**
>
> - **v1.2 (Current):** Windows — Automatic trust installation
> - **macOS/Linux:** Manual installation required (automatic coming in v1.3)
>
> See [Platform-Specific Installation](#platform-specific-installation) for details.

When you first start the proxy with HTTPS enabled, Portless.NET automatically generates:

When you first start the proxy with HTTPS enabled, Portless.NET automatically generates:
- A Certificate Authority (CA) certificate for `*.localhost` domains
- A wildcard server certificate valid for 5 years
- Metadata stored in `~/.portless/cert-info.json`

## Certificate Status Commands

### Check Certificate Status

```bash
portless cert check
```

Shows:
- Certificate validity status (Valid, Expiring Soon, Expired)
- Days until expiration
- SHA-256 thumbprint
- File information (with `--verbose`)

**Exit codes:**
- `0` - Certificate is valid
- `1` - Error occurred
- `2` - Certificate is expired
- `3` - Certificate not found

**Examples:**

```bash
# Check certificate status
portless cert check

# Verbose output with full details
portless cert check --verbose
```

### Renew Certificate

```bash
portless cert renew
```

Behavior:
- If certificate is valid and not expiring soon: No action taken
- If certificate expires within 30 days: Automatic renewal
- If certificate is expired: Automatic renewal

**Options:**

```bash
# Force renewal regardless of expiration status
portless cert renew --force

# Disable auto-renewal for this invocation
portless cert renew --disable-auto-renew
```

**After renewal:** The proxy must be restarted to use the new certificate:

```bash
portless proxy stop
portless proxy start --https
```

## Certificate Trust

Portless.NET provides commands to manage CA certificate trust installation for secure HTTPS connections.

### Install Certificate Authority

```bash
portless cert install
```

Installs the Portless.NET CA certificate to your system trust store, enabling trusted HTTPS connections for `*.localhost` domains.

**Behavior:**
- **Windows:** Automatic installation to Windows Certificate Store (LocalMachine Root)
- **macOS/Linux:** Displays manual installation instructions (automatic coming in v1.3)
- Idempotent: Safe to run multiple times (succeeds if already installed)

**Exit codes:**
- `0` - Success (or already installed)
- `1` - Platform not supported
- `2` - Permissions error (run as Administrator/root)
- `3` - Certificate file missing
- `5` - Certificate store access error

**Examples:**

```bash
# Windows (run as Administrator)
portless cert install

# Verify installation
portless cert status
```

**Platform-specific notes:**

**Windows:**
- Requires Administrator privileges
- Installs to LocalMachine Root store (system-wide trust)
- Affects all users on the system
- Requires UAC elevation

**macOS/Linux:**
- Manual installation required
- See [Platform-Specific Installation](#platform-specific-installation) for detailed steps
- Automatic installation planned for v1.3

### Check Trust Status

```bash
portless cert status
```

Displays the current trust status of the Portless.NET CA certificate with colored output.

**Exit codes:**
- `0` - Certificate is trusted
- `1` - Certificate is not trusted
- `2` - Error checking status
- `3` - Certificate file not found

**Output examples:**

**Trusted (green):**
```
✓ Portless.NET Development CA is trusted

Certificate Details:
  Subject: CN=Portless.NET Development CA
  Thumbprint: ABC123DEF456...
  Expires: 2030-02-23 (1825 days remaining)

Installation: Windows Certificate Store
```

**Not trusted (yellow):**
```
✗ Portless.NET Development CA is NOT trusted

Certificate Details:
  Subject: CN=Portless.NET Development CA
  Thumbprint: ABC123DEF456...
  Expires: 2030-02-23 (1825 days remaining)

To install: portless cert install
```

**Not found (red):**
```
✗ Certificate not found

Run: portless proxy start --https
```

**Platform-specific behavior:**

**Windows:**
- Checks Windows Certificate Store (LocalMachine Root)
- Displays certificate store location
- Shows exact thumbprint for verification

**macOS/Linux:**
- Checks system keychain/certificate store
- Manual installation may be required
- See platform-specific installation guide

### Uninstall Certificate Authority

```bash
portless cert uninstall
```

Removes the Portless.NET CA certificate from your system trust store.

**Behavior:**
- **Windows:** Automatic removal from Windows Certificate Store
- **macOS/Linux:** Displays manual removal instructions
- Idempotent: Safe to run multiple times (succeeds if not installed)

**Exit codes:**
- `0` - Success (or not installed)
- `1` - Platform not supported
- `2` - Permissions error (run as Administrator/root)
- `3` - Certificate not found in trust store
- `5` - Certificate store access error

**Examples:**

```bash
# Windows (run as Administrator)
portless cert uninstall

# Verify removal
portless cert status
# Expected: "✗ Certificate not found in trust store"
```

**When to uninstall:**
- You're done using Portless.NET HTTPS
- You want to regenerate certificates
- Security audit requires removing development CAs
- Migrating to a different development proxy

**After uninstalling:**
- HTTPS connections to `*.localhost` will show certificate warnings
- You can still use HTTP endpoints (port 1355)
- Reinstall with `portless cert install` if needed

### Security Considerations

When managing certificate trust, be aware of the security implications:

**Installing the CA certificate:**
- ✅ **Safe:** Only affects `*.localhost` domains
- ✅ **Scoped:** Certificate generation is restricted to localhost domains
- ⚠️ **Consider:** Anyone with access to your CA private key can generate trusted certificates
- ⚠️ **Consider:** All `*.localhost` certificates signed by this CA will be trusted

**Not installing the CA certificate:**
- ✅ **Safer:** No system-wide trust changes
- ❌ **Inconvenient:** Browser warnings for all HTTPS connections
- ❌ **Limited:** Some applications may refuse connections

For detailed security information, see [Certificate Security Considerations](certificate-security.md).

### Platform-Specific Installation

**macOS/Linux users:** Automatic trust installation is not yet supported. See [Certificate Trust Installation for macOS/Linux](certificate-troubleshooting-macos-linux.md) for manual installation steps.

**Windows users:** Automatic installation is supported. Just run `portless cert install` as Administrator.

## Automatic Monitoring

Portless.NET provides optional background monitoring for automatic certificate renewal.

### Enable Background Monitoring

```bash
# Enable via environment variable
export PORTLESS_ENABLE_MONITORING=true
portless proxy start --https

# Or enable via configuration flag (coming in v1.3)
# portless proxy start --https --enable-monitoring
```

**When enabled:**
- Checks certificate expiration every 6 hours
- Automatically renews when within 30 days of expiration
- Logs renewal actions to console

### Monitoring Behavior

**Default configuration:**
- Check interval: 6 hours
- Warning threshold: 30 days
- Auto-renewal: Enabled

**What happens:**
1. On proxy startup: Initial certificate check with warning display
2. Every 6 hours: Background check (if monitoring enabled)
3. Within 30 days of expiration: Automatic renewal
4. After renewal: Log warning that proxy restart is required

## Environment Variables

Configure certificate monitoring behavior via environment variables:

| Variable | Default | Description |
|----------|---------|-------------|
| `PORTLESS_CERT_WARNING_DAYS` | `30` | Days before expiration to trigger warning/renewal |
| `PORTLESS_CERT_CHECK_INTERVAL_HOURS` | `6` | Hours between background checks (requires monitoring enabled) |
| `PORTLESS_AUTO_RENEW` | `true` | Automatically renew certificate when expiring |
| `PORTLESS_ENABLE_MONITORING` | `false` | Enable background monitoring service |

**Examples:**

```bash
# Set warning threshold to 60 days
export PORTLESS_CERT_WARNING_DAYS=60

# Check every 12 hours instead of 6
export PORTLESS_CERT_CHECK_INTERVAL_HOURS=12

# Disable auto-renewal (manual renewal only)
export PORTLESS_AUTO_RENEW=false

# Enable background monitoring
export PORTLESS_ENABLE_MONITORING=true
```

## Proxy Startup Integration

When you start the proxy with HTTPS enabled, it automatically checks certificate status:

```bash
portless proxy start --https
```

**Startup messages:**

**Valid certificate (green):**
```
info: Portless.Proxy.Program[0]
      Certificate valid until 2030-02-23 (1825 days remaining)
```

**Certificate expiring soon (yellow):**
```
warn: Portless.Proxy.Program[0]
      Certificate expires in 15 days (2030-02-23). Run: portless cert renew
WARNING: Certificate expires in 15 days (2030-02-23)
Run: portless cert renew
```

**Expired certificate (red):**
```
fail: Portless.Proxy.Program[0]
      Certificate has expired on 2030-02-23. Run: portless cert renew
ERROR: Certificate has expired. HTTPS connections may fail.
Run: portless cert renew
```

**Note:** The proxy will start even if the certificate is expired or expiring. The check is non-blocking to allow manual intervention.

## Certificate Files

Certificates are stored in `~/.portless/`:

```
~/.portless/
├── ca.pfx              # Certificate Authority private key
├── cert.pfx            # Server certificate private key
└── cert-info.json      # Certificate metadata
```

**Security:** Private key files (`.pfx`) are secured with file permissions:
- **Linux/macOS:** `chmod 600` (owner read/write only)
- **Windows:** ACLs restrict access to current user

**Warning:** These files contain private keys. Do not share them or commit to version control.

## Certificate Metadata

The `cert-info.json` file contains:

```json
{
  "Version": "1.0",
  "Sha256Thumbprint": "ABC123...",
  "CreatedAt": "2025-02-23T10:30:00Z",
  "ExpiresAt": "2030-02-23T10:30:00Z",
  "CaThumbprint": "DEF456...",
  "CreatedAtUnix": 1740300600,
  "ExpiresAtUnix": 2077294200
}
```

This metadata is used by the monitoring service to track expiration without loading the certificate file.

## Troubleshooting

This section provides comprehensive troubleshooting for common certificate issues.

### Issue: Browser Shows "Not Trusted" Warning

**Symptom:** HTTPS connections to `*.localhost` work but browser displays security warning "Certificate is not trusted"

**Diagnosis:**
```bash
# Check trust status
portless cert status
# Expected output: "✗ Not Trusted"
```

**Cause:** CA certificate not installed to system trust store

**Solutions:**

**Windows (automatic installation):**
```bash
# Run as Administrator
portless cert install
# Verify installation
portless cert status
```

**macOS/Linux (manual installation):**
See [Platform-Specific Installation](#platform-specific-installation) for manual steps.

**Prevention:** Run `portless cert install` after first proxy start to enable trusted HTTPS connections.

---

### Issue: portless cert status Shows "✗ Not Trusted"

**Symptom:** Command shows certificate is not trusted despite being installed

**Diagnosis:**
```bash
# Check trust status
portless cert status
# Output: "✗ Portless.NET Development CA is NOT trusted"
```

**Cause:** CA certificate not in system trust store or installation failed

**Solutions:**

**Windows:**
```bash
# Run as Administrator
portless cert install

# If that fails, manually check Certificate Manager
certmgr.msc
# Look for "Portless.NET Development CA" in Trusted Root Certification Authorities
```

**macOS/Linux:**
- Manual installation required - see platform-specific guide
- Verify certificate was installed to correct location
- Try reinstalling with manual steps

**Prevention:** Always run `portless cert install` as Administrator/root to ensure system-wide installation.

---

### Issue: Firefox Shows Certificate Warning But Chrome Doesn't

**Symptom:** Chrome/Edge trust the certificate but Firefox shows "Not Trusted" warning

**Diagnosis:**
```bash
# Check system trust status
portless cert status
# May show "Trusted" but Firefox still warns
```

**Cause:** Firefox uses its own NSS certificate database and doesn't read system trust store

**Solutions:**

**Option 1: Install to Firefox (manual):**
```bash
# Find Firefox certificate databases
find ~/.mozilla/firefox -name "cert9.db"

# Install to Firefox (first profile only)
certutil -A -n "Portless.NET Development CA" -t "C,," -i ~/.portless/ca.pfx -d ~/.mozilla/firefox/xxxxx.default-release
```

**Option 2: Use Chrome/Edge for development:**
- Chrome and Edge use system trust store
- No additional configuration needed

**Prevention:** Automatic Firefox installation is planned for v1.3.

---

### Issue: Certificate Expired and Auto-Renewal Not Working

**Symptom:** Certificate has expired but wasn't automatically renewed

**Diagnosis:**
```bash
# Check certificate status
portless cert check
# Output: "✗ Certificate expired on YYYY-MM-DD"

# Check if monitoring is enabled
echo $PORTLESS_ENABLE_MONITORING
# May be empty or "false"
```

**Cause:** Background monitoring is disabled by default

**Solutions:**

**Immediate fix (manual renewal):**
```bash
# Renew certificate
portless cert renew

# Restart proxy
portless proxy stop
portless proxy start --https
```

**Enable auto-renewal for future:**
```bash
# Enable background monitoring
export PORTLESS_ENABLE_MONITORING=true
portless proxy start --https
```

**Prevention:** Enable `PORTLESS_ENABLE_MONITORING=true` for long-running proxies to automatically renew certificates.

---

### Issue: Proxy Started But HTTPS Shows Certificate Expired Warning

**Symptom:** Proxy started successfully but browser shows "Certificate expired" warning

**Diagnosis:**
```bash
# Check certificate status
portless cert check
# Output: "✗ Certificate expired on YYYY-MM-DD"

# Proxy still started despite expired certificate
portless proxy status
# Output: "Running"
```

**Cause:** Proxy startup check is non-blocking (allows manual intervention)

**Solutions:**

**Renew certificate:**
```bash
portless cert renew
portless proxy stop
portless proxy start --https
```

**Verify new certificate:**
```bash
portless cert check
# Expected: "✓ Certificate is valid"
```

**Prevention:** Enable background monitoring to renew certificates before expiration:
```bash
export PORTLESS_ENABLE_MONITORING=true
```

---

### Issue: Certificate Expires Soon But No Renewal Warning Displayed

**Symptom:** Certificate expires within 30 days but proxy doesn't show warning

**Diagnosis:**
```bash
# Check certificate status
portless cert check
# Output: "✓ Certificate is valid (expires in 15 days)"

# Check warning threshold
echo $PORTLESS_CERT_WARNING_DAYS
# Default: 30
```

**Cause:** Warning threshold may be too low or warnings disabled

**Solutions:**

**Increase warning threshold:**
```bash
# Set to 60 days
export PORTLESS_CERT_WARNING_DAYS=60
portless proxy start --https
```

**Manually renew now:**
```bash
portless cert renew
```

**Prevention:** Set `PORTLESS_CERT_WARNING_DAYS=60` for earlier warnings and more time to respond.

---

### Issue: hostname.localhost Doesn't Work But localhost Does

**Symptom:** `https://localhost:1356` works but `https://myapp.localhost:1356` shows certificate error

**Diagnosis:**
```bash
# Check certificate details
portless cert check --verbose
# Look for SANs (Subject Alternative Names)
```

**Cause:** Certificate may not include `*.localhost` in SANs

**Solutions:**

**Regenerate certificate:**
```bash
portless cert renew --force
portless proxy stop
portless proxy start --https
```

**Verify SANs include wildcard:**
```bash
# Check certificate includes *.localhost
openssl x509 -in ~/.portless/cert.pfx -text -noout | grep DNS
# Expected: DNS:*.localhost
```

**Prevention:** Portless.NET generates wildcard certificates by default. If you see this issue, it may indicate a certificate generation bug - please report it.

---

### Issue: Certificate Error: hostname Not in Certificate SANs

**Symptom:** Browser shows "ERR_CERT_COMMON_NAME_INVALID" or "hostname not in certificate SANs"

**Diagnosis:**
```bash
# Check what hostname you're using
echo "Using: $HOSTNAME.localhost"

# Verify certificate SANs
portless cert check --verbose
# Should show *.localhost in SANs
```

**Cause:** Using a hostname outside `*.localhost` pattern

**Solutions:**

**Use correct hostname format:**
```bash
# Correct: myapp.localhost
portless myapp dotnet run

# Incorrect: myapp.local
# Portless.NET certificates only valid for *.localhost
```

**Check certificate SANs:**
```bash
# Verify *.localhost is in SANs
openssl x509 -in ~/.portless/cert.pfx -text -noout | grep -A1 "Subject Alternative Name"
```

**Prevention:** Always use `*.localhost` hostnames with Portless.NET. Other TLDs require different certificates.

---

### Issue: Permission Denied Reading ~/.portless/ca.pfx

**Symptom:** Error "Permission denied" when accessing certificate files

**Diagnosis:**
```bash
# Check file permissions
ls -la ~/.portless/
# Output: -rw-r--r-- (644) or similar
```

**Cause:** Certificate files have incorrect permissions (too permissive)

**Solutions:**

**Fix permissions:**
```bash
# Set correct permissions (owner read/write only)
chmod 600 ~/.portless/*.pfx
chmod 644 ~/.portless/cert-info.json

# Verify
ls -la ~/.portless/
# Expected: -rw------- (600) for .pfx files
```

**Check ownership:**
```bash
# Ensure you own the files
chown $USER:$USER ~/.portless/*.pfx
```

**Prevention:** Portless.NET sets correct permissions automatically. If you see incorrect permissions, check your umask:
```bash
# Set restrictive umask
umask 077
```

---

### Issue: Certificate Files Have Insecure Permissions (Security Warning)

**Symptom:** Security scan warns about certificate file permissions

**Diagnosis:**
```bash
# Check permissions
ls -la ~/.portless/
# Output shows -rw-r--r-- (644) or other permissive permissions
```

**Cause:** Files are readable by other users on the system

**Solutions:**

**Restrict permissions immediately:**
```bash
# Set correct permissions
chmod 600 ~/.portless/*.pfx
chmod 644 ~/.portless/cert-info.json

# Verify
ls -la ~/.portless/
# Expected: -rw------- (600) for .pfx files
```

**Regenerate certificates if compromise suspected:**
```bash
# If others may have accessed your private keys
portless cert renew --force
portless cert uninstall
portless cert install
```

**Prevention:**
- Set restrictive umask: `umask 077`
- Never share certificate files
- On multi-user systems, each user should have their own certificates
- See [Certificate Security Considerations](certificate-security.md) for details

---

### Issue: Certificate Files Are Missing

**Symptom:** Proxy fails to start with "Certificate not found" error

**Diagnosis:**
```bash
# Check if files exist
ls -la ~/.portless/
# Output: No such file or directory
```

**Cause:** First-time setup or certificates were deleted

**Solutions:**

**Generate new certificates:**
```bash
# Start proxy with HTTPS to auto-generate
portless proxy start --https
```

**Verify generation:**
```bash
# Check files were created
ls -la ~/.portless/
# Expected: ca.pfx, cert.pfx, cert-info.json
```

**Prevention:** Once generated, certificates persist indefinitely. Avoid deleting `~/.portless/` directory.

---

### Issue: Certificate Is Corrupted

**Symptom:** `portless cert check` reports "Certificate file is corrupted"

**Diagnosis:**
```bash
# Check certificate status
portless cert check
# Output: "✗ Certificate file is corrupted"
```

**Cause:** File corruption, disk error, or incomplete write

**Solutions:**

**Force regeneration:**
```bash
portless cert renew --force
```

**Verify new certificate:**
```bash
portless cert check
# Expected: "✓ Certificate is valid"
```

**Prevention:** Ensure disk has sufficient space and no I/O errors during certificate generation.

---

### Issue: Background Monitoring Not Working

**Symptom:** Certificate not auto-renewing even though it's expiring

**Diagnosis:**
```bash
# Check if monitoring is enabled
echo $PORTLESS_ENABLE_MONITORING
# Output: empty or "false"
```

**Cause:** Background monitoring is opt-in via environment variable

**Solutions:**

**Enable monitoring:**
```bash
export PORTLESS_ENABLE_MONITORING=true
portless proxy start --https
```

**Verify monitoring is running:**
```bash
# Check proxy logs for monitoring messages
portless proxy logs | grep -i certificate
```

**Prevention:** Set `PORTLESS_ENABLE_MONITORING=true` in your shell profile (`.bashrc`, `.zshrc`) for automatic monitoring on every proxy start.

## Limitations

### Hot Reload (v1.2)

**Current limitation:** Certificate changes require proxy restart.

**Workaround:**
```bash
portless proxy stop
portless proxy start --https
```

**Planned:** Hot-reload support in v1.3+

### Cross-Platform Trust Installation (v1.2)

**Current limitation:** Automatic trust installation is Windows-only.

**Workaround:** Manual installation on macOS/Linux:
```bash
# macOS
sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain ~/.portless/ca.pfx

# Linux
sudo cp ~/.portless/ca.pfx /usr/local/share/ca-certificates/portless-ca.crt
sudo update-ca-certificates
```

**Planned:** Cross-platform trust installation in v1.3+

## Security Considerations

- **Development certificates only:** These certificates are for local development and should not be used in production.
- **Private key protection:** Certificate files contain private keys and are secured with file permissions.
- **Trust implications:** Installing the CA certificate gives trust to all `*.localhost` certificates signed by it.
- **Certificate sharing:** Never share certificate files (`ca.pfx`, `cert.pfx`) with others.
- **Regeneration:** Regenerating certificates creates a new CA. All existing certificates signed by the old CA will become untrusted.

## Best Practices

1. **Check certificate status regularly:**
   ```bash
   portless cert check
   ```

2. **Enable background monitoring for long-running proxies:**
   ```bash
   export PORTLESS_ENABLE_MONITORING=true
   portless proxy start --https
   ```

3. **Renew certificates before expiration:**
   ```bash
   portless cert renew
   ```

4. **Restart proxy after renewal:**
   ```bash
   portless proxy stop && portless proxy start --https
   ```

5. **Use --force only when needed:**
   ```bash
   # Only use --force if you need to regenerate for other reasons
   portless cert renew --force
   ```

## Related Documentation

- [Certificate Security Considerations](certificate-security.md) - Security best practices and implications
- [Platform-Specific Installation](certificate-troubleshooting-macos-linux.md) - macOS/Linux manual installation steps
- [Migration Guide v1.1 to v1.2](migration-v1.1-to-v1.2.md) - Upgrading to HTTPS support
