# Certificate Security Considerations

Portless.NET uses self-signed certificates for local development HTTPS. This document outlines security implications and best practices.

## Certificate Purpose

Portless.NET certificates are designed for **local development only**:
- Valid for `*.localhost` domains
- Self-signed by a local Certificate Authority (CA)
- Not intended for production use
- Not valid for public-facing applications

## Security Model

### Trust Chain

```
Portless Local Development CA (self-signed)
    └── *.localhost Wildcard Certificate (signed by CA)
```

When you install the CA certificate to your system trust store:
- Your system trusts all certificates signed by this CA
- Only `*.localhost` certificates can be signed (by design)
- Certificates are valid for 5 years

### Trust Implications

**Installing the CA certificate:**
- ✅ **Safe:** Only affects `*.localhost` domains
- ✅ **Scoped:** Certificate generation is restricted to localhost domains
- ⚠️ **Consider:** Anyone with access to your CA private key can generate trusted certificates
- ⚠️ **Consider:** All `*.localhost` certificates signed by this CA will be trusted

**Not installing the CA certificate:**
- ✅ **Safer:** No system-wide trust changes
- ❌ **Inconvenient:** Browser warnings for all HTTPS connections
- ❌ **Limited:** Some applications may refuse connections

## Private Key Protection

### Key Files

Portless.NET stores private keys in `~/.portless/`:

```
~/.portless/
├── ca.pfx              # CA private key (4096-bit RSA)
├── cert.pfx            # Server certificate private key (2048-bit RSA)
└── cert-info.json      # Public metadata only
```

### File Permissions

**Linux/macOS:**
- Permissions: `600` (owner read/write only)
- Owner: Current user
- Group: Current user's primary group

**Windows:**
- ACL: Current user (Full Control)
- ACL: Administrators (Full Control)
- ACL: System (Read)
- Inheritance: Disabled

### Verification

Check file permissions on Linux/macOS:

```bash
ls -la ~/.portless/
```

Expected output:
```
-rw------- 1 user group  4096 feb. 23 10:30 ca.pfx
-rw------- 1 user group  2048 feb. 23 10:30 cert.pfx
-rw-r--r-- 1 user group   234 feb. 23 10:30 cert-info.json
```

**Warning:** If permissions are more permissive (e.g., `644`), other users on your system may be able to read your private keys.

## Key Sizes

Portless.NET uses industry-standard key sizes:

| Certificate | Key Size | Algorithm | Notes |
|-------------|----------|-----------|-------|
| CA Certificate | 4096 bits | RSA | Higher security for long-lived CA |
| Server Certificate | 2048 bits | RSA | Standard size for server certificates |

**Security trade-offs:**
- **4096-bit RSA:** More secure but slower certificate generation
- **2048-bit RSA:** Standard security, faster generation

## Validity Period

Certificates are valid for **5 years** from generation:

| Certificate | Validity | Rationale |
|-------------|----------|-----------|
| CA Certificate | 5 years | Long-lived to avoid frequent reinstallation |
| Server Certificate | 5 years | Matches CA validity for convenience |

**Security considerations:**
- Longer validity = less frequent rotation = less user friction
- Longer validity = more risk if private key is compromised
- 5 years is a reasonable balance for local development

## Certificate Regeneration

### When to Regenerate

**Regenerate if:**
- Certificate has expired
- Private key may have been compromised
- You want to invalidate all existing certificates signed by the CA
- Certificate files are corrupted

**Do NOT regenerate if:**
- Certificate is valid and expiring soon (use `portless cert renew` instead)
- You just want to extend validity (renewal preserves the CA)

### Regeneration Security

**Regenerating certificates:**
1. Creates a new CA certificate with new private key
2. Invalidates all certificates signed by the old CA
3. Requires reinstallation of CA certificate to trust store
4. Requires updating all applications that pinned the old certificate

**Command:**
```bash
portless cert renew --force
```

**After regeneration:**
```bash
# Reinstall CA certificate
portless cert install

# Restart proxy to use new certificate
portless proxy stop
portless proxy start --https
```

## Development vs Production

### Development Certificates (Portless.NET)

**Characteristics:**
- Self-signed (not trusted by default)
- Valid for `*.localhost` only
- Stored locally with file permissions
- Managed by user
- No certificate revocation infrastructure

**Use cases:**
- Local development
- Integration testing
- Staging environments (if isolated)

### Production Certificates

**Characteristics:**
- Signed by trusted public CA (e.g., Let's Encrypt)
- Valid for public domains
- Managed by certificate authority
- Certificate revocation supported
- Short validity (90 days for Let's Encrypt)

**Use cases:**
- Production deployments
- Public-facing applications
- Any environment accessible from the internet

**Never use Portless.NET certificates in production.**

## Shared Systems

### Multi-User Systems

**Risk:** If multiple users have access to the same system:
- Each user should generate their own certificates
- Never share `ca.pfx` or `cert.pfx` files
- Set file permissions to `600` (owner only)

**Isolation:**
```bash
# Each user generates their own certificates
sudo -u user1 portless proxy start --https
sudo -u user2 portless proxy start --https
```

### CI/CD Systems

**Recommendations:**
- Generate certificates in CI pipeline before tests
- Do NOT commit certificate files to version control
- Use `~/.portless/` with appropriate permissions
- Run tests as non-privileged user

**Example CI configuration:**
```bash
# Generate certificates
portless proxy start --https --daemon

# Run tests
dotnet test

# Cleanup (optional)
rm -rf ~/.portless/
```

## Certificate Pinning

### What is Certificate Pinning?

Certificate pinning is when an application hardcodes the expected certificate thumbprint and refuses connections if the certificate changes.

### Risks with Pinning

**Problem:** If you pin the Portless.NET certificate thumbprint:
- Certificate regeneration will break your application
- You'll need to update the pinned thumbprint after regeneration
- Manual intervention required for each regeneration

**Alternatives:**
- Pin the CA certificate thumbprint (changes less frequently)
- Use certificate trust validation instead of pinning
- Disable pinning for local development

### Checking Thumbprint

```bash
# Get current certificate thumbprint
portless cert status --verbose
```

Output:
```
Certificate Details:
  SHA-256: ABC123DEF456...
```

## Audit and Compliance

### Security Audits

**What auditors check:**
- Private key file permissions
- Certificate validity period
- Trust installation scope
- Certificate generation process
- Key sizes and algorithms

**Demonstrating security:**
```bash
# Show file permissions
ls -la ~/.portless/

# Show certificate details
portless cert status --verbose

# Show trust status
portless cert status
```

### Compliance Notes

Portless.NET certificates are **not compliant** with:
- SOC 2 (production certificate requirements)
- PCI DSS (production certificate requirements)
- HIPAA (production certificate requirements)

**Reason:** Self-signed certificates are not appropriate for production systems handling sensitive data.

## Incident Response

### Private Key Compromise

If you suspect private key compromise:

1. **Immediate actions:**
   ```bash
   # Regenerate certificates
   portless cert renew --force

   # Remove old CA from trust store
   portless cert uninstall

   # Install new CA
   portless cert install
   ```

2. **Verify:**
   ```bash
   # Check new certificate thumbprint
   portless cert status --verbose

   # Verify trust status
   portless cert status
   ```

3. **Document:**
   - Record the incident
   - Note the old thumbprint (for auditing)
   - Update any pinned references

### Unauthorized Access

If someone had access to your system:

1. **Assume compromise:**
   - Regenerate all certificates
   - Change all passwords
   - Review audit logs

2. **Prevent future access:**
   - Review user accounts
   - Check file permissions
   - Enable system auditing

## Best Practices

1. **Never share private keys:**
   - Do not commit `ca.pfx` or `cert.pfx` to version control
   - Do not share via email or chat
   - Do not copy between systems

2. **Use appropriate file permissions:**
   ```bash
   chmod 600 ~/.portless/*.pfx
   ```

3. **Regenerate after security incidents:**
   ```bash
   portless cert renew --force
   ```

4. **Monitor certificate expiration:**
   ```bash
   portless cert check
   ```

5. **Enable background monitoring for long-running proxies:**
   ```bash
   export PORTLESS_ENABLE_MONITORING=true
   ```

6. **Use only for local development:**
   - Never use in production
   - Never expose to the internet
   - Never share with external users

## Additional Resources

- [Certificate Lifecycle Management](certificate-lifecycle.md)
- [Installation Guide](../README.md)
- [Troubleshooting](../README.md#troubleshooting)

## Questions?

See [Security FAQ](../README.md#security-faq) or [report a security issue](https://github.com/yourusername/portless-dotnet/security).
