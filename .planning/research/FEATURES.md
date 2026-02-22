# Feature Research

**Domain:** Local HTTPS Development with Automatic Certificates
**Researched:** 2026-02-22
**Confidence:** MEDIUM

## Feature Landscape

### Table Stakes (Users Expect These)

Features users assume exist. Missing these = product feels incomplete.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| **Automatic certificate generation** | Developers expect HTTPS to "just work" without manual OpenSSL commands | MEDIUM | Use local CA (like mkcert approach) to generate certs on-demand for `*.localhost` domains |
| **Wildcard certificate support** | Multiple services (api.localhost, app.localhost) should share one cert | LOW | Generate single wildcard cert for `*.localhost` (or `*.local.dev`) to cover all subdomains |
| **System trust store installation** | Browsers warn about untrusted certificates - breaks OAuth, cookies, Service Workers | HIGH | Platform-specific: Windows (certmgr), macOS (keychain), Linux (update-ca-certificates) |
| **HTTPS endpoint on separate port** | Standard practice - HTTP on 1355, HTTPS on 1356 | LOW | Kestrel supports multiple endpoints; configure both HTTP and HTTPS listeners |
| **Certificate expiration monitoring** | Expired certs cause browser warnings and service interruptions | MEDIUM | Check expiration on proxy startup; warn user 30 days before expiration |
| **Certificate renewal automation** | Developers shouldn't manually regenerate certificates | MEDIUM | Auto-renew before expiration; reload Kestrel certificate without restart |
| **Mixed HTTP/HTTPS support** | Some services need HTTP, others need HTTPS (testing both) | LOW | YARP routes same hostname to different backend protocols; proxy handles both |
| **SNI (Server Name Indication) support** | Multiple certificates for different domains on same IP | MEDIUM | Kestrel supports SNI natively; required for custom domains beyond `.localhost` |
| **TLS 1.2+ minimum** | Modern security standards; browsers reject TLS 1.0/1.1 | LOW | .NET 10 defaults to TLS 1.2+; Kestrel enforces by default |
| **Private key security** | Certificates contain private keys that must be protected | MEDIUM | Store in `~/.portless/certs/` with proper file permissions (600 on Unix, ACL on Windows) |
| **CLI commands for certificate management** | Developers need visibility and control over certificates | LOW | `portless cert install`, `portless cert trust`, `portless cert status`, `portless cert renew` |
| **Cross-platform certificate paths** | Different OS store certificates in different locations | MEDIUM | Abstract platform differences in `CertificateStore` service |

### Differentiators (Competitive Advantage)

Features that set the product apart. Not required, but valuable.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| **Zero-configuration HTTPS** | Competitors like mkcert require manual steps; Portless.NET automates everything | HIGH | Generate CA, install trust, create certs, configure Kestrel - all automatically on first `--https` use |
| **.NET-native certificate generation** | No external dependencies (mkcert, OpenSSL) - pure .NET implementation | MEDIUM | Use `System.Security.Cryptography.X509Certificates` to create self-signed certs programmatically |
| **Automatic certificate hot-reload** | Other tools require proxy restart to renew certificates | MEDIUM | Kestrel supports certificate reload without connection disruption; monitor cert file changes |
| **Per-environment certificates** | Separate certs for dev, staging, local production | MEDIUM | Support multiple certificate profiles; avoid conflicts between environments |
| **Certificate trust verification** | Detect when CA is not trusted and guide user through installation | LOW | Check trust store on startup; provide platform-specific instructions if trust fails |
| **Certificate export/import** | Share certificates across machines or backup | LOW | Export CA and server certs to password-protected PFX for portability |
| **Custom domain support** | Beyond `.localhost` - support `.local`, `.test`, `.dev` TLDs | MEDIUM | Generate certs for user-specified domains; requires SNI configuration |
| **Certificate pinning support** | Advanced security testing - validate certificates match specific fingerprints | HIGH | Allow pinning certificates for specific routes; helps test pinning failures |
| **OCSP/CRL support simulation** | Test certificate revocation scenarios | HIGH | Mock OCSP responder for testing revocation checking; useful for security testing |
| **HSTS preloading testing** | Validate HSTS headers work correctly with custom domain list | MEDIUM | Support HSTS header configuration; test browser preload behavior locally |
| **Integration with .NET dev-certs** | Reuse existing ASP.NET Core development certificates | MEDIUM | Detect and use `dotnet dev-certs` if available; fallback to Portless.NET CA |
| **Certificate metadata in CLI output** | Show cert expiration, issuer, fingerprint in `portless list` | LOW | Enhanced visibility helps developers understand certificate status |

### Anti-Features (Commonly Requested, Often Problematic)

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| **Let's Encrypt integration for localhost** | "Production-like" certificates for local dev | Let's Encrypt doesn't issue certificates for localhost/private IPs; requires DNS validation; rate limits; unnecessary complexity | Use local CA - browsers trust it for development; production uses public certs anyway |
| **Certificate revocation infrastructure** | "Proper" PKI with CRL/OCSP | Development environment doesn't need revocation; adds massive complexity; revocation checking requires network; slows down development | Simple CA regeneration if compromise suspected (rare in local dev) |
| **EV (Extended Validation) certificates** | "Test production certificate types" | EV requires organization validation; expensive; browsers don't show EV indicators for localhost; meaningless for development | Standard DV (Domain Validated) certificates sufficient for local testing |
| **Multiple certificate authorities** | "Separate CAs for different projects" | Managing multiple CAs complicates trust installation; conflicts in trust store; developers confused about which CA to trust | Single CA per machine; use multiple server certs from same CA |
| **Wildcard for `.localhost` TLD** | Cert for `*.localhost` covers everything | Browsers and certificate authorities treat `.localhost` as special; wildcard for TLD may not work; security concerns | Use `.local.dev` or similar second-level domain; or individual certs per subdomain |
| **Certificate chain validation bypass** | "Just trust everything" mode | Defeats purpose of HTTPS testing; masks real certificate issues; bad security practice | Proper CA installation with trust; if trust fails, provide clear guidance |
| **Manual certificate editing** | "I want to tweak certificate fields" | Certificates are cryptographic structures; manual editing breaks signatures; creates security vulnerabilities | Generate new cert with correct parameters; CLI supports common customizations |
| **Certificate sharing across network** | "Access my localhost from other devices" | Exposes private key; violates security model; `.localhost` domains don't resolve network-wide | Use proper reverse proxy with public domain + Let's Encrypt; different use case |
| **Certificate password protection** | "Secure the certificate files" | Private keys already protected by file permissions; password requires entry on every proxy start; friction for local dev | File permissions (ACL/600) sufficient; encrypt if exporting for sharing |
| **HSTS force-enable** | "Always use HTTPS, never HTTP" | HSTS headers persist in browser for weeks; can lock developer out of their own localhost; hard to recover | HSTS optional with warning; provide `portless proxy reset-hsts` command if locked out |
| **Perfect Forward Secrecy (PFS) enforcement** | "Test modern TLS cipher suites" | PFS is default in .NET 10; manual cipher suite configuration is complex and error-prone | Trust .NET defaults; expose cipher suite config only if requested |

## Feature Dependencies

```
[HTTPS Proxy Endpoint]
    └──requires──> [TLS Certificate]
                    └──requires──> [Certificate Authority (CA)]
                                    └──requires──> [System Trust Installation]
                                                    └──requires──> [Platform-Specific Trust API]

[Wildcard Certificate]
    └──requires──> [Subject Alternative Name (SAN) Extension]
    └──enhances──> [Multiple Subdomain Support]

[Certificate Renewal]
    └──requires──> [Certificate Expiration Monitoring]
    └──requires──> [Kestrel Certificate Hot-Reload]
    └──enhances──> [Long-Running Proxy]

[SNI Support]
    └──requires──> [Multiple Certificate Storage]
    └──requires──> [TLS SNI Configuration]
    └──enhances──> [Custom Domain Support]

[Certificate Trust Verification]
    └──requires──> [Platform-Specific Trust Store Access]
    └──enhances──> [Zero-Configuration HTTPS]

[Certificate Export/Import]
    └──requires──> [PFX Password Protection]
    └──enhances──> [Cross-Machine Portability]

[OCSP/CRL Simulation]
    └──requires──> [Mock OCSP Responder]
    └──conflicts──> [Zero-Configuration Goal]

[HSTS Preloading Test]
    └──requires──> [Custom Domain Support]
    └──requires──> [HSTS Header Configuration]
    └──conflicts──> [Localhost-Only Scope]
```

### Dependency Notes

- **HTTPS Proxy Endpoint requires TLS Certificate:** Kestrel cannot serve HTTPS without a certificate. The certificate must be loaded at startup and configured for the HTTPS endpoint.
- **TLS Certificate requires Certificate Authority:** Self-signed certificates must be signed by a CA. For local development, we create a local CA that acts as the root of trust.
- **Certificate Authority requires System Trust Installation:** Browsers only trust certificates signed by CAs in their trust store. The local CA must be installed to the system trust store to avoid browser warnings.
- **System Trust Installation requires Platform-Specific Trust API:** Windows (CertMgr/certutil), macOS (security command), and Linux (update-ca-certificates) all have different trust stores and installation mechanisms.
- **Wildcard Certificate requires SAN Extension:** Modern browsers require Subject Alternative Name extension; wildcard certificates use `*.domain.com` in SAN. Common Name (CN) field is deprecated.
- **Wildcard Certificate enhances Multiple Subdomain Support:** A single wildcard certificate covers `api.localhost`, `app.localhost`, `admin.localhost`, etc., simplifying certificate management.
- **Certificate Renewal requires Expiration Monitoring:** Must check certificate expiration date regularly; renew before expiration (30 days recommended) to avoid service interruption.
- **Certificate Renewal requires Kestrel Hot-Reload:** Kestrel supports reloading certificates without dropping connections. Use `KestrelServerOptions.ConfigureHttpsDefaults()` with certificate reload callback.
- **Certificate Renewal enhances Long-Running Proxy:** Long-running proxy processes (days/weeks) need automatic renewal to avoid downtime.
- **SNI Support requires Multiple Certificate Storage:** SNI allows multiple certificates on same IP/port based on hostname. Need storage mechanism for multiple certs and mapping to hostnames.
- **SNI Support requires TLS SNI Configuration:** Kestrel's SNI configuration requires callback that selects certificate based on SNI hostname.
- **SNI Support enhances Custom Domain Support:** Different domains can have different certificates; necessary for `.localhost`, `.local.dev`, `.test` domains simultaneously.
- **Certificate Trust Verification requires Platform-Specific Trust Store Access:** Need to read trust store to verify CA is installed; differs by platform.
- **Certificate Trust Verification enhances Zero-Configuration HTTPS:** Detect when trust is missing and automatically install or guide user through installation; reduces manual steps.
- **Certificate Export/Import requires PFX Password Protection:** Exported certificates (PFX format) should be password-protected to prevent unauthorized use; .NET APIs support password-based encryption.
- **Certificate Export/Import enhances Cross-Machine Portability:** Share CA and server certs across development machines; backup certificates; onboarding new team members.
- **OCSP/CRL Simulation requires Mock OCSP Responder:** Building a mock OCSP responder is complex; adds infrastructure dependency.
- **OCSP/CRL Simulation conflicts with Zero-Configuration Goal:** Mock OCSP responder requires separate process/service; complicates setup; most developers don't need to test revocation.
- **HSTS Preloading Test requires Custom Domain Support:** HSTS preloading is for public domains, not localhost. Need custom domain support to test effectively.
- **HSTS Preloading Test requires HSTS Header Configuration:** Need to configure HSTS headers (Strict-Transport-Security) in proxy or backend.
- **HSTS Preloading Test conflicts with Localhost-Only Scope:** HSTS preloading is irrelevant for `.localhost` domains; real use case is for public domains; may be out of scope.

## MVP Definition

### Launch With (v1.2)

Minimum viable product — what's needed to validate the concept for HTTPS support with automatic certificates.

- [ ] **Automatic certificate generation** — Core feature; generate CA and server certs programmatically on first `--https` use
- [ ] **Wildcard certificate support** — Single cert for `*.localhost` covers all subdomains; essential for multi-service scenarios
- [ ] **System trust store installation (Windows)** — Platform focus for v1.2; macOS/Linux support added later
- [ ] **HTTPS endpoint on separate port** — Default HTTP on 1355, HTTPS on 1356; configurable via `PORTLESS_HTTPS_PORT`
- [ ] **Certificate expiration monitoring** — Check on proxy startup; warn if expiring within 30 days
- [ ] **Certificate renewal automation** — Auto-renew before expiration; hot-reload in Kestrel without restart
- [ ] **Mixed HTTP/HTTPS support** — Proxy both protocols simultaneously; different backends can use either
- [ ] **CLI commands for certificate management** — `portless cert install`, `portless cert trust`, `portless cert status`
- [ ] **Private key security** — Store in `~/.portless/certs/` with proper file permissions (Windows ACL)
- [ ] **Cross-platform certificate paths** — Abstract platform differences; Windows paths for v1.2, add macOS/Linux in v1.3

### Add After Validation (v1.3+)

Features to add once core HTTPS is working.

- [ ] **System trust store installation (macOS/Linux)** — Complete cross-platform support; keychain on macOS, update-ca-certificates on Linux
- [ ] **SNI support** — Multiple certificates for different domains; enables custom domains beyond `.localhost`
- [ ] **Certificate trust verification** — Detect untrusted CA and guide user; reduce manual troubleshooting
- [ ] **Certificate hot-reload** — Reload certificate without proxy restart; better renewal experience
- [ ] **Integration with .NET dev-certs** — Reuse existing ASP.NET Core development certificates if available
- [ ] **Certificate metadata in CLI output** — Show expiration, issuer, fingerprint in `portless list`
- [ ] **Custom domain support** — Support `.local`, `.test`, `.dev` TLDs; configurable domain suffix
- [ ] **Certificate export/import** — Share CA and server certs; backup and restore

### Future Consideration (v2+)

Features to defer until product-market fit is established.

- [ ] **Zero-configuration HTTPS** — Fully automated CA generation, trust installation, certificate creation; highest complexity but best DX
- [ ] **Per-environment certificates** — Separate certs for dev, staging, local production; environment-aware certificate selection
- [ ] **Certificate pinning support** — Test certificate pinning failures; advanced security testing
- [ ] **OCSP/CRL support simulation** — Mock OCSP responder; test certificate revocation scenarios
- [ ] **HSTS preloading testing** — Validate HSTS headers; test browser preload behavior (requires custom domain)
- [ ] **HSTS force-enable** — Optional HSTS with `portless proxy reset-hsts` escape hatch
- [ ] **Perfect Forward Secrecy enforcement** — Manual cipher suite configuration; advanced TLS tuning

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| Automatic certificate generation | HIGH | MEDIUM | P1 |
| Wildcard certificate support | HIGH | LOW | P1 |
| System trust store installation (Windows) | HIGH | HIGH | P1 |
| HTTPS endpoint on separate port | HIGH | LOW | P1 |
| Certificate expiration monitoring | HIGH | MEDIUM | P1 |
| Certificate renewal automation | HIGH | MEDIUM | P1 |
| Mixed HTTP/HTTPS support | HIGH | LOW | P1 |
| CLI commands for certificate management | HIGH | LOW | P1 |
| Private key security | MEDIUM | MEDIUM | P1 |
| Cross-platform certificate paths (Windows) | MEDIUM | MEDIUM | P1 |
| System trust store installation (macOS/Linux) | HIGH | HIGH | P2 |
| SNI support | MEDIUM | MEDIUM | P2 |
| Certificate trust verification | MEDIUM | MEDIUM | P2 |
| Certificate hot-reload | MEDIUM | MEDIUM | P2 |
| Integration with .NET dev-certs | MEDIUM | MEDIUM | P2 |
| Certificate metadata in CLI output | LOW | LOW | P2 |
| Custom domain support | MEDIUM | MEDIUM | P2 |
| Certificate export/import | LOW | LOW | P2 |
| Zero-configuration HTTPS | HIGH | HIGH | P3 |
| Per-environment certificates | LOW | HIGH | P3 |
| Certificate pinning support | LOW | HIGH | P3 |
| OCSP/CRL support simulation | LOW | HIGH | P3 |
| HSTS preloading testing | LOW | HIGH | P3 |
| HSTS force-enable | LOW | MEDIUM | P3 |
| Perfect Forward Secrecy enforcement | LOW | MEDIUM | P3 |

**Priority key:**
- P1: Must have for launch (v1.2 - Windows focus)
- P2: Should have, add when possible (v1.3 - cross-platform completion)
- P3: Nice to have, future consideration (v2+ - advanced features)

## Competitor Feature Analysis

| Feature | mkcert | dotnet dev-certs | Portless (Node.js) | Our Approach |
|---------|--------|------------------|-------------------|--------------|
| Certificate generation | ✅ Automatic | ✅ Automatic | ❌ Manual (OpenSSL) | ✅ Automatic via .NET APIs |
| Wildcard certificates | ✅ `*.domain.com` | ❌ Per-domain only | ❌ Manual | ✅ `*.localhost` wildcard |
| Windows trust installation | ✅ Automatic | ✅ Automatic | ❌ Manual | ✅ Automatic (v1.2) |
| macOS trust installation | ✅ Automatic | ✅ Automatic | ❌ Manual | ✅ Automatic (v1.3) |
| Linux trust installation | ✅ Automatic | ✅ Automatic (.NET 9+) | ❌ Manual | ✅ Automatic (v1.3) |
| Certificate renewal | ❌ Manual regen | ❌ Manual regen | ❌ Manual | ✅ Auto-renew (P1) |
| HTTPS proxy | ❌ Certs only | ❌ Certs only | ✅ Built-in proxy | ✅ Built-in YARP proxy |
| CLI management | ✅ CLI tool | ✅ dotnet CLI | ❌ No cert commands | ✅ Integrated CLI (P1) |
| Zero-config | ✅ One command | ✅ One command | ❌ Multiple steps | ✅ One command `--https` |
| Cross-platform | ✅ All platforms | ✅ All platforms | ⚠️ Limited Windows | ✅ All platforms (progressive) |
| .NET integration | ❌ External tool | ✅ Native | ❌ Node.js | ✅ Native .NET 10 |
| Certificate hot-reload | ❌ Requires restart | ❌ Requires restart | ❌ N/A | ✅ Hot-reload (P2) |
| Custom domains | ✅ Any domain | ❌ localhost only | ❌ Manual | ✅ Any domain (P2) |

## Sources

### High Confidence (Official Documentation)

- [mkcert GitHub Repository](https://github.com/FiloSottile/mkcert) - Official mkcert project (49.3k+ stars); de facto standard for local HTTPS development (HIGH confidence - industry standard)
- [ASP.NET Core Kestrel HTTPS Endpoint Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints) - Official Kestrel endpoint configuration documentation (HIGH confidence - official Microsoft source)
- [ASP.NET Core HTTPS Development Certificates](https://learn.microsoft.com/en-us/aspnet/core/security/enforcing-ssl) - Official .NET dev-certs documentation (HIGH confidence - official Microsoft source)
- [.NET 9 Linux Certificate Trust](https://learn.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/9.0/linux-dev-certs) - .NET 9 Linux trust improvements (HIGH confidence - official Microsoft source)

### Medium Confidence (WebSearch + Verification)

- [Local HTTPS Certificate Generation Tutorial (CSDN, Jan 2026)](https://blog.csdn.net/weixin_46244623/article/details/156943289) - Comprehensive mkcert tutorial covering Windows, macOS, Linux (MEDIUM confidence - recent tutorial with practical examples)
- [SSL Certificate Renewal Practical Guide](https://m.blog.csdn.net/gitblog_01192/article/details/157240340) - Certificate renewal best practices; 30-day renewal warning recommendation (MEDIUM confidence - aligns with industry standards)
- [YARP HTTPS Configuration with Let's Encrypt (CSDN)](https://blog.csdn.net/2501_93329146/article/details/151364447) - Shows YARP HTTPS certificate configuration with Kestrel (MEDIUM confidence - practical example, not official docs)
- [Cross-Platform Certificate Trust Installation](https://docs.redhat.com/zh-cn/documentation/red_hat_enterprise_linux/8/html/securing_networks/managing-trusted-system-certificates_using-shared-system-certificates) - Linux `trust` command reference (MEDIUM confidence - official Red Hat documentation)

### Low Confidence (WebSearch Only - Needs Validation)

- [Free SSL Certificate Updates (3-month validity period)](https://m.blog.csdn.net/gitblog_01192/article/details/157240340) - Mentions certificate validity reduction to 3 months (LOW confidence - single source, needs verification)
- [Wildcard Certificate for .localhost TLD](https://blog.csdn.net/weixin_46244623/article/details/156943289) - Safari limitations with `*.localhost` (LOW confidence - needs browser-specific testing)
- [Nginx WebSocket Proxy Timeouts](https://nginx.org/en/docs/http/websocket.html) - WebSocket proxy timeout patterns (LOW confidence - NGINX-specific, may not apply to YARP)
- [Certificate Revocation Process](https://m.blog.csdn.net/gitblog_01192/article/details/157240340) - Certificate renewal vs revocation confusion (LOW confidence - conflates production and development scenarios)

### Key Gaps Requiring Validation

1. **YARP-specific certificate hot-reload** - Found Kestrel documentation for certificate reload, but need YARP-specific confirmation
2. **SNI configuration in YARP** - Found Kestrel SNI docs, but YARP may have additional requirements or limitations
3. **Wildcard certificate browser compatibility** - Need to test `*.localhost` vs `*.local.dev` across browsers (Chrome, Firefox, Safari, Edge)
4. **Windows certificate store API performance** - Reading/writing to Windows certificate store may be slow; need performance testing
5. **Certificate file permission best practices** - Found general guidance, need .NET-specific file permission recommendations
6. **.NET 10 certificate generation API changes** - .NET 10 may have new certificate APIs not covered in current documentation

---
*Feature research for: Portless.NET v1.2 HTTPS with Automatic Certificates*
*Researched: 2026-02-22*
