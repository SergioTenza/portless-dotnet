# Certificate Lifecycle Management

Portless.NET provides automatic certificate lifecycle management for local development HTTPS support. Certificates are generated once and automatically monitored for expiration.

## Overview

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

### Certificate files are missing

**Problem:** Proxy fails to start with "Certificate not found" error.

**Solution:** Start the proxy with HTTPS enabled to generate new certificates:

```bash
portless proxy start --https
```

### Certificate is corrupted

**Problem:** `portless cert check` reports "Certificate file is corrupted".

**Solution:** Force regeneration:

```bash
portless cert renew --force
```

### Certificate expired but proxy still started

**Problem:** Proxy started with expired certificate, HTTPS shows warnings.

**Solution:** Renew the certificate and restart the proxy:

```bash
portless cert renew
portless proxy stop
portless proxy start --https
```

### Background monitoring not working

**Problem:** Certificate not auto-renewing even though it's expiring.

**Solution:** Ensure monitoring is enabled:

```bash
# Check if monitoring is enabled
echo $PORTLESS_ENABLE_MONITORING

# Enable monitoring
export PORTLESS_ENABLE_MONITORING=true
portless proxy start --https
```

### Browser shows certificate warnings

**Problem:** HTTPS works but browser shows "Not Trusted" warning.

**Solution:** Install the CA certificate to your system trust store:

```bash
# Windows (run as Administrator)
portless cert install

# Verify installation
portless cert status
```

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

## Related Commands

- `portless cert install` - Install CA certificate to system trust
- `portless cert status` - Display certificate trust status
- `portless cert uninstall` - Remove CA certificate from trust store
- `portless proxy start --https` - Start proxy with HTTPS enabled
