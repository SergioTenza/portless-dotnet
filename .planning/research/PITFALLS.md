# Pitfalls Research

**Domain:** HTTPS with Automatic Certificates for Local Development Proxy
**Researched:** 2026-02-22
**Confidence:** MEDIUM

## Critical Pitfalls

### Pitfall 1: Certificate Subject Name Mismatch

**What goes wrong:**
Certificate specifies `CN=localhost` but requests use `0.0.0.0` or `127.0.0.1`, causing TLS verification failures. Also occurs when wildcard certificate `*.localhost` is used for `api.sub.localhost` (wildcards only match same-level subdomains).

**Why it happens:**
Developers assume certificates with `CN=localhost` will work for all localhost variations. Modern browsers require Subject Alternative Names (SAN) to match the exact hostname being accessed. IPv4 vs IPv6 differences also cause mismatches.

**How to avoid:**
- Always include both DNS names AND IP addresses in SAN extension
- Include all variations: `localhost`, `127.0.0.1`, `::1` (IPv6 loopback)
- For wildcard certificates, remember `*.example.com` matches `a.example.com` but NOT `a.b.example.com`
- Use `SubjectAlternativeNameBuilder` to add multiple entries:
  ```csharp
  sanBuilder.AddDnsName("localhost");
  sanBuilder.AddDnsName("*.localhost");
  sanBuilder.AddIpAddress(IPAddress.Loopback);
  sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
  ```

**Warning signs:**
- Browser shows `ERR_CERT_COMMON_NAME_INVALID`
- TLS handshake failures when using IP addresses instead of "localhost"
- Certificate works for `localhost` but fails for `127.0.0.1`
- Mobile browsers show certificate warnings while desktop browsers don't

**Phase to address:**
Phase 13-01 (Certificate Generation) - SAN extension must be configured during certificate creation

---

### Pitfall 2: Missing or Untrusted Root CA Certificate

**What goes wrong:**
Self-signed certificates are generated correctly but browsers show "ERR_CERT_AUTHORITY_INVALID" warnings because the local Certificate Authority (CA) is not installed in the system trust store.

**Why it happens:**
Generating certificates is only half the solution. The CA certificate must be explicitly installed and trusted by the operating system. Each platform (Windows, macOS, Linux) has different trust stores and installation procedures.

**How to avoid:**
- Implement explicit trust installation commands for each platform:
  - **Windows**: Import to `Cert:\LocalMachine\Root` with admin privileges
  - **macOS**: Add to System keychain with `security add-trusted-cert`
  - **Linux**: Copy to `/usr/local/share/ca-certificates/` and run `update-ca-certificates`
- Verify trust after installation with browser tests
- Provide `portless cert trust` command that auto-detects platform
- Handle Firefox separately (uses its own certificate store)

**Warning signs:**
- All HTTPS requests show certificate warnings
- Browser error: "ERR_CERT_AUTHORITY_INVALID"
- Certificate validation works in curl but fails in browsers
- Different browsers show different behaviors (Chrome vs Firefox)

**Phase to address:**
Phase 13-02 (Trust Installation) - Platform-specific trust installation must be implemented

---

### Pitfall 3: Kestrel Certificate Not Explicitly Configured

**What goes wrong:**
HTTPS endpoint fails to start or returns "Not secure" because Kestrel doesn't automatically load certificates. Common error: `System.IO.FileNotFoundException: Could not find file '.../cert.pfx'`

**Why it happens:**
ASP.NET Core 7+ removed default HTTPS binding. Kestrel requires explicit certificate configuration with absolute paths, not relative paths. Command-line URL overrides (`--urls`) bypass appsettings.json configuration.

**How to avoid:**
- Always configure HTTPS explicitly in code or appsettings.json:
  ```csharp
  options.ListenAnyIP(httpsPort, listenOptions =>
  {
      listenOptions.UseHttps(httpsOptions =>
      {
          httpsOptions.ServerCertificate = cert;
          httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
      });
  });
  ```
- Use absolute paths for certificate files
- Set `X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet` if re-exporting
- Configure `HttpsRedirectionOptions.HttpsPort` for proper redirects

**Warning signs:**
- HTTPS endpoint doesn't bind (no error, just doesn't start)
- Certificate file not found errors
- Works with `dotnet run` but fails as deployed executable
- HTTP redirects to HTTPS fail with "Unable to determine https port"

**Phase to address:**
Phase 13-03 (HTTPS Endpoint) - Kestrel configuration must include explicit certificate binding

---

### Pitfall 4: Private Key Export Not Marked Exportable

**What goes wrong:**
Certificate can be created and used, but cannot be exported to PFX file for backup or sharing across processes. Error: `CryptographicException: The specified network password is not correct` (misleading error message).

**Why it happens:**
X509Certificate2 requires `X509KeyStorageFlags.Exportable` flag during import/creation. Without this flag, the private key is stored in a temporary container that cannot be exported.

**How to avoid:**
- Always use `X509KeyStorageFlags.Exportable` when creating certificates:
  ```csharp
  var cert = new X509Certificate2(
      certData,
      password,
      X509KeyStorageFlags.MachineKeySet |
      X509KeyStorageFlags.PersistKeySet |
      X509KeyStorageFlags.Exportable
  );
  ```
- Check `cert.HasPrivateKey` before attempting export
- For CA certificates that need to be exported to files, ensure exportable flag is set

**Warning signs:**
- Export operations throw `CryptographicException`
- `cert.HasPrivateKey` returns `true` but export fails
- Windows Certificate MMC shows "Export private key" option grayed out
- Certificate works in current process but cannot be serialized

**Phase to address:**
Phase 13-01 (Certificate Generation) - Certificate creation must use exportable key storage flags

---

### Pitfall 5: Certificate Security Update Breaking Changes (June 2023)

**What goes wrong:**
Certificate export operations fail after Windows security updates KB5025823/KB5028608. Exports with null passwords or high iteration counts (>600,000) are blocked.

**Why it happens:**
Microsoft hardened PKCS12 export security. Null passwords, high iteration counts, and SID-protected certificates are no longer exportable.

**How to avoid:**
- Never export with null or empty passwords
- Keep password iteration counts between 2,000-10,000
- Use strong passwords (don't hardcode in source)
- Avoid `PKCS12_PROTECT_TO_DOMAIN_SIDS` flag
- Test exports on updated Windows systems

**Warning signs:**
- Certificate exports work in development but fail in production
- `CryptographicException` with message about password or export protection
- Code worked before June 2023 but suddenly fails
- Export works on some machines but not others (depending on update status)

**Phase to address:**
Phase 13-01 (Certificate Generation) - Export logic must use secure password practices

---

### Pitfall 6: Certificate Expiration Without Renewal

**What goes wrong:**
Certificates expire after 1-3 years, causing sudden HTTPS failures. Users see "ERR_CERT_DATE_INVALID" and the proxy becomes unusable.

**Why it happens:**
Development certificates typically have short validity periods. Automatic renewal logic is missing or not triggered before expiration. Chrome limits certificates to maximum 398 days (as of 2020).

**How to avoid:**
- Implement expiration checking on proxy startup
- Renew certificates automatically when <30 days from expiration
- Store certificate creation timestamp in state directory
- Provide `portless cert status` command to check expiration
- Use 1-3 year validity for development certificates (balance between security and maintenance)
- Log warnings when certificate is approaching expiration

**Warning signs:**
- HTTPS works today but suddenly fails after deployment
- Browser shows "ERR_CERT_DATE_INVALID"
- Certificate validity period is >398 days (Chrome will reject)
- No expiration warnings in logs before failure

**Phase to address:**
Phase 13-05 (Certificate Renewal) - Automatic renewal must be implemented before initial release

---

### Pitfall 7: HTTP/HTTPS Protocol Confusion in Proxy

**What goes wrong:**
Client sends HTTPS request to HTTP endpoint, or proxy doesn't preserve original protocol when forwarding to backend. Backend receives HTTP when expecting HTTPS (or vice versa).

**Why it happens:**
Mixed HTTP/HTTPS support requires proper `X-Forwarded-Proto` header handling. Backends may generate HTTPS URLs when accessed via HTTP proxy, or vice versa.

**How to avoid:**
- Set `X-Forwarded-Proto` header to match original protocol
- Ensure `X-Forwarded-Protocol` header is also set (some backends check this)
- Configure `ForwardedHeaders` middleware in ASP.NET Core backends
- Test with backends that enforce HTTPS (redirect HTTP to HTTPS)
- Log protocol mismatches during development

**Warning signs:**
- Backend redirects HTTP to HTTPS when accessed through proxy
- Backend generates HTTPS URLs when accessed via HTTP proxy
- Mixed content warnings in browser console
- `X-Forwarded-*` headers missing or incorrect in backend logs

**Phase to address:**
Phase 13-04 (Mixed HTTP/HTTPS) - Protocol forwarding must preserve original scheme

---

### Pitfall 8: Certificate File Permission Issues

**What goes wrong:**
Certificate files are readable by all users, creating security risk. Or certificate files cannot be read due to restrictive permissions.

**Why it happens:**
Default file creation permissions may be too permissive. Windows and Unix systems handle permissions differently. Private keys should have restricted access (owner-only).

**How to avoid:**
- Set restrictive permissions on certificate files:
  - **Unix/Linux**: `chmod 600` (read/write for owner only)
  - **Windows**: Use ACLs to restrict to current user
- Store certificates in secure directory:
  - **Unix/Linux**: `~/.portless/certs/` with `700` permissions
  - **Windows**: `%APPDATA%\portless\certs\` with proper ACLs
- Never store certificates in world-readable locations
- Validate permissions on certificate file operations

**Warning signs:**
- Certificate files are readable by all users (`ls -l` shows `-rw-r--r--`)
- Security scanner flags certificate files as accessible
- "Access denied" errors when reading certificates
- Certificates work when run as admin but fail as regular user

**Phase to address:**
Phase 13-01 (Certificate Generation) - File permissions must be set during certificate storage

---

### Pitfall 9: .localhost Domain DNS Resolution Issues

**What goes wrong:**
Requests to `*.localhost` domains fail with DNS resolution errors. Safari on macOS doesn't support `*.localhost` domains. Some applications treat `*.localhost` as regular domain names.

**Why it happens:**
Most modern browsers auto-resolve `*.localhost` to loopback, but Safari and non-browser applications don't. No DNS records exist for `*.localhost` domains.

**How to avoid:**
- Test with Safari on macOS (use `localhost` instead of `*.localhost`)
- For non-browser HTTP clients, add entries to `/etc/hosts`:
  ```
  127.0.0.1 myapp.localhost
  ```
- Consider using `*.dev.localhost` (ASP.NET Core has built-in support)
- Document Safari limitations for users
- Provide fallback to `localhost` with port numbers for Safari users

**Warning signs:**
- curl/Postman can reach `myapp.localhost` but Safari cannot
- DNS resolution failures in non-browser applications
- "Name or service not known" errors
- Works on Windows/Chrome but fails on macOS/Safari

**Phase to address:**
Phase 13-03 (HTTPS Endpoint) - DNS resolution must be tested across browsers and platforms

---

### Pitfall 10: Backend SSL/TLS Verification with Self-Signed Certificates

**What goes wrong:**
Proxy successfully terminates HTTPS but backend rejects forwarded requests due to certificate validation failures. Or proxy refuses to connect to HTTPS backends with self-signed certificates.

**Why it happens:**
When proxy connects to HTTPS backends, it performs certificate validation by default. Self-signed backend certificates (common in development) fail validation.

**How to avoid:**
- For development: Allow self-signed certificates for localhost backends
- Configure YARP to disable certificate validation for localhost destinations:
  ```csharp
  new ForwarderHttpClientConfig
  {
      SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
      DangerousAcceptAnyServerCertificate = true, // Only for localhost
      WebProxy = null
  }
  ```
- Document this as development-only setting
- Provide option to enable strict validation for production testing
- Log when certificate validation is disabled

**Warning signs:**
- Proxy cannot connect to HTTPS backends
- "The remote certificate is invalid" errors in proxy logs
- Backend works when accessed directly but fails through proxy
- Works with HTTP backends but fails with HTTPS backends

**Phase to address:**
Phase 13-04 (Mixed HTTP/HTTPS) - Backend SSL validation must be configurable for development

---

## Technical Debt Patterns

Shortcuts that seem reasonable but create long-term problems.

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Hardcoding certificate password | Quick prototype, no password management | Password can be extracted via IL disassembly; cannot rotate certificates | Never - not even for development |
| Using null password for certificate export | Simpler code, no user prompts | Fails after June 2023 security updates; security risk | Never |
| Generating certificates with 10+ year validity | Avoid expiration issues | Chrome rejects (>398 days); security exposure if CA key compromised | Never |
| Trusting all certificates (DangerousAcceptAnyServerCertificate globally) | Works with any backend | Security risk; masks real certificate issues | Only for localhost backends in development mode |
| Storing certificates in project directory | Simple file paths | Certificates checked into source control; private keys exposed | Never - use state directory |
| Skipping certificate validation "for now" | Faster development | Forgetting to re-enable leads to production security issues | Only with compile-time guards (`#if DEBUG`) |
| Using system certificate store for CA | No file management needed | Requires admin privileges; harder to uninstall; cross-platform issues | Only as fallback if file-based fails |

## Integration Gotchas

Common mistakes when connecting to external services.

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| **Windows Certificate Store** | Installing to CurrentUser instead of LocalMachine; not requesting admin elevation | Install to `Cert:\LocalMachine\Root` with admin privileges; check elevation before install |
| **macOS Keychain** | Adding to user keychain instead of System keychain; not setting trust settings | Use `sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain` |
| **Linux (Debian/Ubuntu)** | Copying certificate to `/etc/ssl/certs/` directly instead of ca-certificates location | Copy to `/usr/local/share/ca-certificates/` and run `sudo update-ca-certificates` |
| **Linux (RHEL/Fedora)** | Using Debian-style ca-certificates commands | Copy to `/etc/pki/ca-trust/source/anchors/` and run `sudo update-ca-trust extract` |
| **Firefox** | Assuming Firefox uses OS certificate store | Import CA certificate manually through Firefox Settings > Certificates |
| **Docker containers** | Assuming host certificates are available inside container | Mount certificate volume or install inside container |
| **YARP Reverse Proxy** | Not setting X-Forwarded-Proto header | Use ForwardedHeaders middleware or set header manually |
| **ASP.NET Core Backends** | Not configuring ForwardedHeaders middleware | Add `app.UseForwardedHeaders()` with proper options |
| **Node.js backends** | Not trusting self-signed certificates | Set `NODE_TLS_REJECT_UNAUTHORIZED=0` (development only) |
| **curl/Postman** | Not providing CA certificate for validation | Use `--cacert` flag or disable verification with `-k` (development only) |

## Performance Traps

Patterns that work at small scale but fail as usage grows.

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| **Generating new certificate on every startup** | Slow startup (2-5 seconds); high CPU on startup | Generate once, reuse until expiration; check file existence first | Immediately noticeable in CLI tools |
| **Reading certificate file on every request** | Increased latency; file I/O contention | Load certificate once at startup; reuse in Kestrel configuration | At 100+ concurrent requests |
| **Not caching certificate validation results** | Repeated validation overhead | Trust the local CA after first validation; cache in memory | At 1000+ requests/second |
| **Synchronous certificate generation** | UI freezes during certificate creation | Use async/await for certificate operations | Immediately noticeable in CLI |
| **Large certificate chains** | Increased handshake latency | Keep CA chain short (1-2 intermediates max) | At 10,000+ requests/second |
| **Not using TLS session resumption** | Full handshake on every connection | Enable TLS session tickets in Kestrel | At 1000+ connections/second |

**Note:** For local development proxy, most performance traps are not critical (<100 concurrent requests is typical). Focus on startup performance (certificate generation) rather than request throughput.

## Security Mistakes

Domain-specific security issues beyond general web security.

| Mistake | Risk | Prevention |
|---------|------|------------|
| **Hardcoding certificate password in source** | Password extracted via IL disassembly; CA key compromised | Store password in secure configuration or use user prompt |
| **Using weak RSA key size (<2048 bits)** | Certificate can be forged; private key can be cracked | Always use RSA-2048 or RSA-4096 |
| **Setting certificate validity >398 days** | Chrome rejects certificate; unexpected failures | Use 1-3 year validity; implement auto-renewal |
| **Exporting private key without password** | Private key exposed if file compromised | Always use strong password for PFX export |
| **Storing certificates in source control** | Private keys exposed in git history | Add certificate directory to `.gitignore` |
| **Using same CA for production and development** | Development CA trust compromises production | Use separate CAs; label clearly in certificate subject |
| **Allowing any certificate (DangerousAcceptAnyServerCertificate) globally** | Man-in-the-middle attacks possible | Restrict to localhost backends only; document as development-only |
| **Not validating certificate subject on backend** | Impersonation attacks | Verify certificate subject matches expected hostname |
| **Using SHA-1 for certificate signing** | Certificate forging attacks | Always use SHA-256 or higher |
| **Not checking certificate expiration** | Sudden failures when expired | Implement expiration monitoring; auto-renew before expiry |

## UX Pitfalls

Common user experience mistakes in this domain.

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| **No warning before certificate expires** | Sudden HTTPS failures; user confusion | Log warnings 30/7/1 days before expiration; provide `portless cert status` command |
| **Requiring browser restart after trust installation** | User tries HTTPS, still sees warnings, gives up | Detect when browser needs restart and provide clear instructions |
| **Unclear error messages for certificate failures** | User doesn't know how to fix "ERR_CERT_" errors | Provide actionable error messages with troubleshooting links |
| **No visual feedback during certificate generation** | User thinks CLI is frozen (2-5 seconds) | Show progress indicator: "Generating CA certificate..." |
| **Requiring admin privileges without explanation** | User confused why UAC prompt appears | Explain: "Admin privileges required to install trusted certificate" |
| **Silent certificate trust failures** | HTTPS doesn't work, no error logged | Log trust installation success/failure clearly |
| **No Safari documentation for *.localhost** | Safari users see DNS errors, no workaround documented | Document Safari limitation; provide alternative (localhost:port) |
| **Firefox certificate import is manual** | Firefox users see warnings even after system trust | Document Firefox-specific trust installation steps |
| **Certificate files in confusing location** | User can't find certificates to backup/import | Use consistent location: `~/.portless/certs/` or `%APPDATA%\portless\certs\` |
| **No command to verify certificate trust** | User unsure if HTTPS will work before testing | Provide `portless cert verify` command that tests browser trust |

## "Looks Done But Isn't" Checklist

Things that appear complete but are missing critical pieces.

- [ ] **HTTPS endpoint binds but shows "Not secure"**: Often missing trust installation — verify CA certificate is installed in system trust store AND browser accepts it
- [ ] **Certificate generates successfully but can't be exported**: Often missing `X509KeyStorageFlags.Exportable` flag — verify certificate was created with exportable private key
- [ ] **Works on Windows but fails on macOS**: Often missing platform-specific trust installation — verify macOS keychain installation and Safari compatibility
- [ ] **Backend receives HTTP when expecting HTTPS**: Often missing `X-Forwarded-Proto` header — verify forwarded headers middleware is configured
- [ ] **Certificate works for localhost but fails for 127.0.0.1**: Often missing IP addresses in SAN extension — verify certificate includes both DNS and IP SAN entries
- [ ] **Proxy can reach HTTPS backend but shows certificate errors**: Often missing backend SSL validation configuration — verify YARP `DangerousAcceptAnyServerCertificate` for localhost
- [ ] **Works in Chrome but fails in Firefox**: Often Firefox doesn't use OS certificate store — verify Firefox certificate import separately
- [ ] **Certificate generates but expires unexpectedly soon**: Often using default short validity — verify certificate validity period is 1-3 years
- [ ] **HTTPS works but HTTP stops working**: Often HTTP endpoint not configured when adding HTTPS — verify both HTTP and HTTPS endpoints bind
- [ ] **Certificate file permissions are world-readable**: Often default file creation permissions — verify certificate files have restrictive permissions (600 on Unix)
- [ ] **Works today but fails after Windows updates**: Often using null password or high iteration count — verify export uses password and compatible iteration count
- [ ] **No browser warnings but curl shows certificate errors**: Often curl doesn't use system certificate store — verify curl uses `--cacert` flag or disable verification for testing

## Recovery Strategies

When pitfalls occur despite prevention, how to recover.

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| **Certificate expired** | LOW | Run `portless cert renew` to generate new certificate; restart proxy |
| **CA not trusted** | LOW | Run `portless cert trust --force` to reinstall CA; restart browser |
| **Certificate file corrupted** | MEDIUM | Delete certificate files; run `portless cert generate` to recreate; update all backends |
| **Private key not exportable** | HIGH | Regenerate certificate with exportable flag; cannot recover existing private key |
| **Wrong SAN in certificate** | HIGH | Regenerate certificate with correct SAN entries; cannot modify existing certificate |
| **CA private key compromised** | HIGH | Generate new CA; reissue all certificates; reinstall trust on all machines |
| **Certificate password lost** | HIGH | Regenerate certificate with new password; cannot recover existing certificate |
| **Windows update breaks exports** | MEDIUM | Update export code to use password and compatible iteration count; regenerate certificates |
| **macOS keychain trust broken** | LOW | Remove certificate from keychain; run `portless cert trust` again |
| **Linux ca-certificates cache stale** | LOW | Run `sudo update-ca-certificates --fresh` to refresh cache |
| **Firefox doesn't trust certificate** | LOW | Manual import: Firefox > Settings > Certificates > View Certificates > Authorities > Import |
| **Safari can't resolve *.localhost** | LOW | Use `localhost` instead of `*.localhost` or add `/etc/hosts` entry |
| **Backend rejects proxy certificates** | MEDIUM | Configure backend to accept self-signed certificates or disable validation for localhost |

## Pitfall-to-Phase Mapping

How roadmap phases should address these pitfalls.

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| Certificate Subject Name Mismatch | Phase 13-01 (Certificate Generation) | Test with `localhost`, `127.0.0.1`, `::1`, and `*.localhost` domains |
| Missing/Untrusted Root CA | Phase 13-02 (Trust Installation) | Verify no browser warnings across Chrome, Firefox, Safari |
| Kestrel Certificate Not Explicitly Configured | Phase 13-03 (HTTPS Endpoint) | Test HTTPS endpoint binds without errors; check certificate is loaded |
| Private Key Export Not Marked Exportable | Phase 13-01 (Certificate Generation) | Export certificate to PFX and verify import succeeds |
| Certificate Security Update Breaking Changes | Phase 13-01 (Certificate Generation) | Test export on updated Windows 10/11 systems |
| Certificate Expiration Without Renewal | Phase 13-05 (Certificate Renewal) | Test auto-renewal triggers before 30-day expiration |
| HTTP/HTTPS Protocol Confusion | Phase 13-04 (Mixed HTTP/HTTPS) | Verify `X-Forwarded-Proto` header is set correctly in backend logs |
| Certificate File Permission Issues | Phase 13-01 (Certificate Generation) | Verify file permissions: `ls -l` on Unix, ACLs on Windows |
| .localhost Domain DNS Resolution Issues | Phase 13-03 (HTTPS Endpoint) | Test with Safari on macOS and non-browser HTTP clients |
| Backend SSL/TLS Verification | Phase 13-04 (Mixed HTTP/HTTPS) | Test proxy connecting to HTTPS backend with self-signed cert |
| Hardcoded Passwords | Phase 13-01 (Certificate Generation) | Code review: verify no passwords in source code |
| Weak RSA Key Size | Phase 13-01 (Certificate Generation) | Verify RSA-2048 or RSA-4096 in certificate generation code |
| Certificate Validity >398 Days | Phase 13-01 (Certificate Generation) | Check certificate validity period is 1-3 years |
| Exporting Without Password | Phase 13-01 (Certificate Generation) | Verify all exports use strong passwords |
| Certificates in Source Control | Phase 13-01 (Certificate Generation) | Verify `.gitignore` excludes certificate directory |
| Same CA for Production and Development | Phase 13-01 (Certificate Generation) | Verify CA certificate subject includes "Development" label |
| No Expiration Warnings | Phase 13-05 (Certificate Renewal) | Test warnings appear at 30/7/1 days before expiration |
| Unclear Error Messages | Phase 13-02 (Trust Installation) | User testing: verify error messages are actionable |
| No Firefox Documentation | Phase 13-02 (Trust Installation) | Verify Firefox trust steps are documented |
| Works on Windows, Fails on macOS | Phase 13-02 (Trust Installation) | Test trust installation on all three platforms |

## Platform-Specific Issues

### Windows Certificate Store

**Issues:**
- Requires administrator privileges for LocalMachine store installation
- Certificate MMC "Export" option grayed out if key not marked exportable
- June 2023 security updates block null password exports
- Windows Services may require `X509KeyStorageFlags.MachineKeySet`

**Mitigation:**
- Detect admin elevation before install; prompt UAC if needed
- Always use `X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet`
- Use strong passwords for all exports
- Test with both user and system contexts

### macOS Keychain

**Issues:**
- Safari doesn't support `*.localhost` domains
- Certificate trust requires System keychain, not user keychain
- macOS doesn't support offline CRL utilization
- Some certificate stores throw exceptions on macOS (e.g., Disallowed store)

**Mitigation:**
- Use `sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain`
- Document Safari `*.localhost` limitation
- Test with Safari explicitly
- Avoid using Disallowed store

### Linux Certificate Stores

**Issues:**
- Different locations for different distributions:
  - Debian/Ubuntu: `/usr/local/share/ca-certificates/`
  - RHEL/Fedora: `/etc/pki/ca-trust/source/anchors/`
- Requires `sudo update-ca-certificates` (Debian) or `sudo update-ca-trust extract` (RHEL)
- Certificate files need proper naming (`.crt` extension)
- File permissions matter (600 for private keys, 644 for certificates)

**Mitigation:**
- Detect distribution and use appropriate paths
- Always run update command after installing certificate
- Use `.crt` extension for certificate files
- Set proper permissions with `chmod`

## Sources

- [Kestrel HTTPS Certificate Pitfalls](https://github.com/dotnet/AspNetCore.Docs/issues/25284) - Certificate subject name mismatch, missing default HTTPS binding in .NET 7+
- [Microsoft Dev Proxy - CI/CD Certificate Trust](https://learn.microsoft.com/en-us/microsoft-cloud/dev/dev-proxy/how-to/use-dev-proxy-in-ci-cd-overview) - Linux certificate trust commands and cross-platform certificate store differences
- [Azure DevOps - Self-Signed Certificates](https://learn.microsoft.com/en-us/azure/devops/pipelines/agents/certificate) - Platform-specific certificate stores (Windows Certificate Manager, macOS Keychain, Linux OpenSSL)
- [mkcert Tutorial - Cross-Platform HTTPS](https://blog.csdn.net/weixin_46244623/article/details/156943289) - Local certificate authority setup and trust installation across platforms
- [Certificate Verification Failed Guide](https://www.websitepulse.com/blog/ssl-certificate-verification-failed-fix) - Common SSL certificate errors and troubleshooting
- [OpenSSL Certificate Generation](https://www.openssl.org/docs/) - SAN extension configuration and certificate generation best practices
- [ASP.NET Core Localhost Certificate Support](https://learn.microsoft.com/en-us/aspnet/core/security/enforcing-ssl) - `.localhost` TLD support and `*.dev.localhost` domains
- [X509Certificate2 Export Best Practices](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.x509certificate2.export) - Exportable key storage flags and password requirements
- [Localhost DNS Resolution Issues](https://developer.mozilla.org/en-US/docs/Web/HTTP/Server-Side_Access_Control) - IPv4 vs IPv6 loopback resolution and Safari limitations
- [Portless (Vercel) HTTPS Implementation](https://github.com/vercel-labs/portless) - Reference implementation for HTTPS in local development proxy
- [YARP Reverse Proxy Documentation](https://microsoft.github.io/reverse-proxy/) - Forwarded headers configuration and SSL validation options
- [Chrome Certificate Validity Policy](https://developer.chrome.com/blog/enforcing-trustworthy-ev-certs/) - 398-day certificate validity limit
- [Windows Security Updates KB5025823/KB5028608](https://support.microsoft.com/kb/5025823) - PKCS12 export security hardening changes

---
*Pitfalls research for: HTTPS with Automatic Certificates for Local Development Proxy*
*Researched: 2026-02-22*
