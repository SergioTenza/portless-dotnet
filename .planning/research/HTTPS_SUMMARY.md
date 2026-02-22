# Research Summary: Portless.NET v1.2 HTTPS with Automatic Certificates

**Domain:** Local HTTPS Development with Automatic Certificates
**Researched:** 2026-02-22
**Overall confidence:** MEDIUM

## Executive Summary

Research into HTTPS development tools reveals a clear standard for local HTTPS: **mkcert** has emerged as the de facto industry standard (49.3k+ GitHub stars) for zero-configuration local development certificates. The tool's success demonstrates that developers prioritize:

1. **Automatic certificate generation** - No manual OpenSSL commands
2. **System trust store installation** - No browser warnings
3. **Wildcard certificate support** - One cert for all subdomains
4. **Cross-platform support** - Windows, macOS, Linux

For Portless.NET v1.2, the key opportunity is to **integrate mkcert-like functionality natively** into the .NET tool, eliminating the external dependency while maintaining the same developer experience. .NET 10's `System.Security.Cryptography.X509Certificates` APIs provide everything needed to generate certificates programmatically without external tools.

The research identified **12 table-stakes features** that users expect from any HTTPS development tool, **12 differentiator features** that could set Portless.NET apart, and **11 anti-features** that seem attractive but create unnecessary complexity.

## Key Findings

**Stack:** Use .NET 10's native cryptography APIs (`X509Certificate2`, `CertificateRequest`) for certificate generation; Kestrel's built-in HTTPS endpoint configuration; YARP's TLS proxy capabilities. No external dependencies like mkcert or OpenSSL required.

**Architecture:** Three-layer architecture: (1) CertificateGenerator service creates CA and server certs, (2) CertificateTruster service handles platform-specific trust store installation, (3) ProxyManager configures Kestrel HTTPS endpoints with certificate hot-reload.

**Critical pitfall:** **Certificate trust store fragmentation** - Different platforms (Windows/macOS/Linux) have completely different trust stores and installation mechanisms. Windows uses Certificate Manager API, macOS uses Keychain, Linux uses `update-ca-certificates`. This is the #1 source of cross-platform HTTPS tool failures.

## Implications for Roadmap

Based on research, suggested phase structure for v1.2:

1. **Phase 1: Certificate Generation & Storage** - Core certificate creation
   - Addresses: Automatic certificate generation, wildcard certificates, private key security
   - Avoids: Platform-specific trust installation complexity initially
   - Dependencies: None (can develop in isolation)

2. **Phase 2: Windows Trust Installation** - Platform focus for v1.2
   - Addresses: System trust store installation (Windows), certificate trust verification
   - Avoids: Cross-platform complexity until v1.3
   - Dependencies: Requires Phase 1 (need certs before installing)

3. **Phase 3: HTTPS Proxy Endpoint** - Kestrel configuration
   - Addresses: HTTPS endpoint on separate port, mixed HTTP/HTTPS support
   - Avoids: SNI complexity initially
   - Dependencies: Requires Phase 1 (need certificates for Kestrel)

4. **Phase 4: Certificate Lifecycle Management** - Renewal and monitoring
   - Addresses: Certificate expiration monitoring, renewal automation, CLI commands
   - Avoids: Advanced features like hot-reload initially
   - Dependencies: Requires Phase 3 (need running proxy to monitor)

**Phase ordering rationale:**
- Phase 1 can be developed and tested independently (unit tests)
- Phase 2 is Windows-specific, aligns with v1.2 platform focus
- Phase 3 integrates certificates into existing proxy (least disruption to current code)
- Phase 4 adds polish and management capabilities after core works

**Research flags for phases:**
- Phase 2 (Windows Trust Installation): **Needs deeper research** - Windows certificate store API performance, UAC prompts, permission issues
- Phase 3 (HTTPS Proxy): **Standard patterns** - Kestrel HTTPS configuration well-documented
- Phase 4 (Lifecycle Management): **May need research** - Certificate hot-reload in YARP not well-documented

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack (Certificate APIs) | HIGH | .NET 10 APIs well-documented; multiple official sources |
| Features (Table Stakes) | HIGH | mkcert provides proven reference implementation |
| Architecture (Trust Store) | MEDIUM | Platform-specific differences well-understood; Windows path clear |
| Pitfalls (Trust Fragmentation) | MEDIUM | Identified from research; validated by competitor analysis |

## Gaps to Address

1. **YARP certificate hot-reload** - Kestrel supports hot-reload, but YARP integration needs validation
2. **Wildcard certificate browser compatibility** - Need to test `*.localhost` vs `*.local.dev` across browsers
3. **Windows certificate store performance** - API calls may be slow; needs performance testing
4. **Certificate file permissions** - .NET-specific best practices for securing private key files
5. **macOS/Linux trust installation** - Deferred to v1.3, but needs research before implementation

## Feature Dependencies

```
Certificate Generation (Phase 1)
    └──required by──> Windows Trust Installation (Phase 2)
    └──required by──> HTTPS Proxy Endpoint (Phase 3)
                      └──required by──> Lifecycle Management (Phase 4)

Windows Trust Installation (Phase 2)
    └──enhances──> HTTPS Proxy Endpoint (Phase 3)
                   (trusted certificates = no browser warnings)
```

## Competitor Analysis

| Tool | Strength | Weakness |
|------|----------|----------|
| **mkcert** | Industry standard; zero-config; cross-platform | External dependency; no proxy integration |
| **dotnet dev-certs** | Native .NET integration | No wildcard certs; per-domain only |
| **Portless (Node.js)** | Built-in proxy | Manual OpenSSL commands; limited Windows support |
| **Portless.NET v1.2** | Native .NET; built-in proxy; wildcard certs; auto-renewal | Still in development; v1.2 Windows-only |

**Differentiation opportunity:** Portless.NET can combine the best of all tools - mkcert's zero-config experience, dotnet dev-certs' native integration, and Portless's built-in proxy - while adding unique features like automatic renewal and hot-reload.

## Technical Risks

1. **Windows Certificate Store UAC Prompts** - Installing to LocalMachine store may require elevation; user experience impact unknown
2. **Certificate Hot-Reload Stability** - Kestrel hot-reload documented, but YARP integration untested
3. **Wildcard Certificate Compatibility** - Some browsers may reject `*.localhost`; may need `*.local.dev` fallback
4. **Cross-Platform Trust Installation** - Windows path clear, but macOS/Linux deferred to v1.3; complexity may be underestimated

## Recommended MVP Scope for v1.2

**Must Have (P1):**
- Automatic certificate generation (CA + server certs)
- Wildcard certificate for `*.localhost`
- Windows trust store installation
- HTTPS endpoint on port 1366 (configurable)
- Certificate expiration monitoring
- Basic renewal (manual CLI command)
- Mixed HTTP/HTTPS proxy support
- CLI commands: `cert install`, `cert trust`, `cert status`

**Should Have (P2 - v1.3):**
- Automatic renewal with hot-reload
- macOS/Linux trust installation
- Certificate trust verification
- Integration with dotnet dev-certs
- Certificate metadata in CLI output

**Nice to Have (P3 - v2+):**
- Zero-configuration HTTPS (fully automated)
- Custom domain support (SNI)
- Certificate export/import
- Per-environment certificates
- Advanced security testing features (pinning, OCSP simulation)

## Success Criteria

v1.2 HTTPS milestone is successful when:
1. Developer runs `portless proxy start --https` → HTTPS works without browser warnings
2. Multiple services (`api.localhost`, `app.localhost`) share single wildcard certificate
3. Certificate auto-renews before expiration (30-day warning)
4. CLI provides clear visibility into certificate status (`portless cert status`)
5. Mixed HTTP/HTTPS works simultaneously (different backends, same proxy)

## Sources

- [mkcert GitHub Repository](https://github.com/FiloSottile/mkcert) - Industry standard for local HTTPS
- [ASP.NET Core Kestrel Endpoints](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints) - Official HTTPS configuration
- [.NET 9 Linux Certificate Trust](https://learn.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/9.0/linux-dev-certs) - Cross-platform trust improvements
- [YARP HTTPS Configuration](https://blog.csdn.net/2501_93329146/article/details/151364447) - Practical YARP HTTPS example
- [Certificate Renewal Best Practices](https://m.blog.csdn.net/gitblog_01192/article/details/157240340) - Renewal timing recommendations

---
*Research summary for: Portless.NET v1.2 HTTPS with Automatic Certificates*
*Researched: 2026-02-22*
*Confidence: MEDIUM*
