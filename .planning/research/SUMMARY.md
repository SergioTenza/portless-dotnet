# Project Research Summary

**Project:** Portless.NET v1.2 - HTTPS with Automatic Certificates
**Domain:** Local Development Proxy with HTTPS Support
**Researched:** 2026-02-22
**Confidence:** HIGH

## Executive Summary

Portless.NET v1.2 adds HTTPS support with automatic certificate generation to the local development proxy. This is a **well-understood domain** with established patterns: create a local Certificate Authority (CA), install it in the system trust store, and generate wildcard certificates signed by the CA. The research confirms that .NET 10's built-in `System.Security.Cryptography.X509Certificates` APIs are **sufficient for all certificate operations** - no external dependencies like BouncyCastle or OpenSSL required.

The recommended approach follows the proven **mkcert pattern**: generate a root CA (10-year validity), install it to the OS trust store (platform-specific), then generate wildcard certificates for `*.localhost` domains (1-year validity, auto-renew). Key architectural changes include new certificate services in Portless.Core, dual HTTP/HTTPS endpoints in Portless.Proxy, and platform-specific trust installation (Windows/macOS/Linux). Critical risks are well-documented: missing SAN extensions cause browser warnings, trust installation requires platform-specific code, and certificate expiration must be monitored. All risks have proven mitigation strategies from the research.

## Key Findings

### Recommended Stack

**Zero external NuGet packages required.** .NET 10 includes complete certificate generation and HTTPS support via built-in APIs.

**Core technologies:**
- **System.Security.Cryptography** (.NET 10) — Certificate generation with `CertificateRequest` class, RSA key creation, CA signing
- **System.Security.Cryptography.X509Certificates** (.NET 10) — Certificate manipulation, X509Store integration, PFX/PEM export
- **SubjectAlternativeNameBuilder** (.NET 10) — SAN extension for wildcard certificates, required for modern browser trust
- **Kestrel HTTPS Configuration** (.NET 10) — Dual HTTP/HTTPS endpoints with certificate binding, supports simultaneous listening
- **System.Diagnostics.Process** (Built-in) — Platform-specific trust installation commands for macOS/Linux (Windows uses X509Store API directly)

**Why this stack:** BouncyCastle and OpenSSL.NET are unnecessary dependencies. .NET 10's native APIs provide complete certificate lifecycle management, cross-platform support, and are maintained by Microsoft. The only platform-specific code needed is trust installation, which uses OS-native APIs (X509Store on Windows, `security` CLI on macOS, shell commands on Linux).

### Expected Features

**Must have (table stakes):**
- Automatic certificate generation — Developers expect HTTPS to "just work" without manual OpenSSL commands
- Wildcard certificate support — Single cert for `*.localhost` covers all subdomains (api.localhost, app.localhost, etc.)
- System trust store installation — Browsers warn about untrusted certificates; breaks OAuth, cookies, Service Workers
- HTTPS endpoint on separate port — Standard practice: HTTP on 1355, HTTPS on 1356
- Certificate expiration monitoring — Expired certs cause browser warnings and service interruptions
- Certificate renewal automation — Developers shouldn't manually regenerate certificates
- Mixed HTTP/HTTPS support — Different services need HTTP or HTTPS for testing
- CLI commands for certificate management — `portless cert install`, `portless cert status`, `portless cert renew`
- Private key security — Certificates contain private keys that must be protected with proper file permissions

**Should have (competitive):**
- Zero-configuration HTTPS — Automate CA generation, trust installation, certificate creation; highest complexity but best DX
- .NET-native certificate generation — No external dependencies (mkcert, OpenSSL); pure .NET implementation
- Automatic certificate hot-reload — Other tools require proxy restart to renew certificates
- Certificate trust verification — Detect when CA is not trusted and guide user through installation
- Integration with .NET dev-certs — Reuse existing ASP.NET Core development certificates if available

**Defer (v2+):**
- Let's Encrypt integration for localhost — Let's Encrypt doesn't issue certificates for localhost/private IPs; unnecessary complexity
- Certificate revocation infrastructure — Development environment doesn't need revocation; adds massive complexity
- EV (Extended Validation) certificates — EV requires organization validation; meaningless for development
- OCSP/CRL support simulation — Mock OCSP responder required; conflicts with zero-configuration goal

### Architecture Approach

HTTPS support requires **moderate architecture extensions** to Portless.NET v1.1. The implementation follows a Certificate Authority (CA) hierarchy pattern similar to mkcert.

**Major components:**
1. **CertificateStore** (Portless.Core, NEW) — Certificate and CA persistence to ~/.portless/ca.pfx, cert.pfx, cert-info.json; mirrors RouteStore pattern with file-based storage
2. **CertificateGenerator** (Portless.Core, NEW) — CA creation (10-year validity) and wildcard certificate generation (1-year validity) using System.Security.Cryptography
3. **TrustInstaller** (Portless.Core, NEW) — Platform-specific trust installation with abstract interface; Windows (X509Store API), macOS (security CLI), Linux (shell commands)
4. **CertificateProvider** (Portless.Proxy, NEW) — Loads certificate for Kestrel HTTPS endpoint; provides cert at startup
5. **CertificateRenewalService** (Portless.Core, NEW) — Background hosted service checking cert expiry every 6 hours; auto-renews before 30-day expiration
6. **Certificate CLI Commands** (Portless.Cli, NEW) — User-facing management: install, status, renew, uninstall
7. **Portless.Proxy/Program.cs** (MODIFIED) — Add HTTPS endpoint configuration with dual ListenAnyIP calls (HTTP + HTTPS)

**Integration points:**
- Certificate generation triggered on proxy startup if certificates don't exist
- Trust installation is manual CLI command (user action, not automatic)
- Certificate renewal is automatic background process
- YARP proxy works transparently on both HTTP and HTTPS (no route/cluster changes needed)

### Critical Pitfalls

1. **Certificate Subject Name Mismatch** — Modern browsers require Subject Alternative Names (SAN) to match exact hostname. Certificate with `CN=localhost` fails for `127.0.0.1` or IPv6 loopback. **Prevention:** Always include both DNS names AND IP addresses in SAN extension using `SubjectAlternativeNameBuilder`: `localhost`, `*.localhost`, `127.0.0.1`, `::1`.

2. **Missing or Untrusted Root CA Certificate** — Self-signed certificates show "ERR_CERT_AUTHORITY_INVALID" warnings because CA isn't in system trust store. **Prevention:** Implement explicit trust installation for each platform: Windows (Import to `Cert:\LocalMachine\Root`), macOS (`security add-trusted-cert`), Linux (copy to `/usr/local/share/ca-certificates/` + run `update-ca-certificates`).

3. **Kestrel Certificate Not Explicitly Configured** — ASP.NET Core 7+ removed default HTTPS binding. Kestrel requires explicit certificate configuration with absolute paths. **Prevention:** Configure HTTPS explicitly in code: `options.ListenAnyIP(httpsPort, listenOptions => { listenOptions.UseHttps(httpsOptions => { httpsOptions.ServerCertificate = cert; }); });`

4. **Private Key Export Not Marked Exportable** — Certificate can be created but cannot be exported to PFX file. Error: `CryptographicException: The specified network password is not correct` (misleading). **Prevention:** Always use `X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet` when creating certificates.

5. **Certificate Expiration Without Renewal** — Certificates expire after 1-3 years, causing sudden HTTPS failures. Chrome limits certificates to 398 days max. **Prevention:** Implement expiration checking on proxy startup; renew automatically when <30 days from expiration; store creation timestamp in state directory.

## Implications for Roadmap

Based on research, suggested phase structure:

### Phase 13-01: Certificate Generation Infrastructure
**Rationale:** Foundation for all HTTPS features. Certificate generation must exist before trust installation or HTTPS endpoint configuration. No dependencies on existing code.
**Delivers:** CertificateStore, CertificateGenerator, CertificateInfo model, CA and wildcard certificate files in ~/.portless/
**Addresses:** Automatic certificate generation, Wildcard certificate support, Private key security, Cross-platform certificate paths
**Avoids:** Pitfall 1 (SAN mismatch), Pitfall 4 (export not marked exportable), Pitfall 8 (file permission issues)

### Phase 13-02: Trust Installation (Windows Focus)
**Rationale:** Windows is v1.2 priority platform (per project focus). Trust installation is user-initiated manual step, independent of HTTPS endpoint. Must complete before HTTPS testing.
**Delivers:** ITrustInstaller interface, WindowsTrustInstaller (X509Store API), `portless cert install` command, trust status verification
**Addresses:** System trust store installation (Windows), Certificate trust verification, CLI commands for certificate management
**Avoids:** Pitfall 2 (untrusted CA), Pitfall 5 (Windows security update breaking changes)
**Research Flags:** Well-documented Windows X509Store API patterns (HIGH confidence - skip research-phase)

### Phase 13-03: HTTPS Endpoint Configuration
**Rationale:** Requires certificate from Phase 13-01. Enables actual HTTPS traffic. Platform-independent (same Kestrel configuration works on all OS).
**Delivers:** CertificateProvider (Portless.Proxy), Modify Program.cs for dual HTTP/HTTPS endpoints, `PORTLESS_HTTPS_PORT` environment variable
**Addresses:** HTTPS endpoint on separate port, Mixed HTTP/HTTPS support, TLS 1.2+ minimum
**Avoids:** Pitfall 3 (Kestrel certificate not explicitly configured), Pitfall 9 (.localhost DNS resolution issues)
**Research Flags:** Standard Kestrel HTTPS patterns (HIGH confidence - skip research-phase)

### Phase 13-04: Mixed HTTP/HTTPS Support
**Rationale:** Ensures proxy correctly forwards protocol information to backends. Required for OAuth, secure cookies, Service Worker testing.
**Delivers:** X-Forwarded-Proto header configuration, ForwardedHeaders middleware, backend SSL validation for HTTPS backends (development mode)
**Addresses:** Mixed HTTP/HTTPS support, Backend SSL/TLS verification with self-signed certificates
**Avoids:** Pitfall 7 (HTTP/HTTPS protocol confusion), Pitfall 10 (backend SSL verification failures)
**Research Flags:** Standard YARP forwarded headers patterns (MEDIUM confidence - may need quick research for YARP-specific details)

### Phase 13-05: Certificate Renewal Automation
**Rationale:** Prevents certificate expiration interrupting development work. Can be implemented incrementally after HTTPS is working.
**Delivers:** CertificateRenewalService hosted service, expiration monitoring on startup, auto-renewal before 30-day expiry, `portless cert renew` command
**Addresses:** Certificate expiration monitoring, Certificate renewal automation
**Avoids:** Pitfall 6 (certificate expiration without renewal)
**Research Flags:** Standard ASP.NET Core BackgroundService pattern (HIGH confidence - skip research-phase)

### Phase 13-06: Cross-Platform Trust Installation (macOS/Linux)
**Rationale:** Complete v1.3 cross-platform support. Platform-specific implementations are independent. Can be developed in parallel if resources allow.
**Delivers:** MacOsTrustInstaller (security CLI), LinuxTrustInstaller (distribution-specific commands), platform detection logic
**Addresses:** System trust store installation (macOS/Linux), Cross-platform certificate paths
**Avoids:** Platform-specific integration gotchas, Firefox NSS database configuration
**Research Flags:** Platform-specific shell commands (MEDIUM confidence - need verification on each OS)

### Phase Ordering Rationale

**Why this order based on dependencies:**
- Phase 13-01 (Certificate Generation) is foundation — all other phases depend on certificates existing
- Phase 13-02 (Trust Installation) comes before 13-03 so HTTPS endpoint starts with trusted certificate from day one
- Phase 13-03 (HTTPS Endpoint) requires certificates from 13-01 but is independent of trust installation (HTTPS works with warnings, trust eliminates warnings)
- Phase 13-04 (Mixed HTTP/HTTPS) requires HTTPS endpoint from 13-03 to test protocol forwarding
- Phase 13-05 (Renewal) requires working HTTPS proxy from 13-03 to validate renewal logic
- Phase 13-06 (Cross-Platform) is independent of 13-02/13-03/13-04/13-05 but kept separate to focus on Windows quality first

**Why this grouping based on architecture patterns:**
- Phases 13-01/13-02/13-06 are certificate services layer (Portless.Core) — can be developed in parallel by different developers
- Phases 13-03/13-04 are proxy layer (Portless.Proxy) — sequential dependencies (endpoint before mixed protocol support)
- Phase 13-05 is background service layer — independent of proxy implementation, only needs CertificateStore

**How this avoids pitfalls from research:**
- Phase 13-01 addresses Pitfall 1 (SAN mismatch) and Pitfall 4 (export flags) during certificate creation — impossible to fix later without regeneration
- Phase 13-02 addresses Pitfall 2 (untrusted CA) explicitly with platform-specific verification
- Phase 13-03 addresses Pitfall 3 (Kestrel configuration) with explicit HTTPS endpoint code
- Phase 13-04 addresses Pitfall 7 (protocol confusion) and Pitfall 10 (backend SSL) with forwarded headers
- Phase 13-05 addresses Pitfall 6 (expiration) before users hit expiry issues

### Research Flags

**Phases likely needing deeper research during planning:**
- **Phase 13-04:** YARP-specific X-Forwarded-Proto configuration may need verification — standard forwarded headers but YARP might have specific requirements
- **Phase 13-06:** macOS `security` command syntax and Linux distribution differences (Debian/Ubuntu vs RHEL/Fedora) need platform testing

**Phases with standard patterns (skip research-phase):**
- **Phase 13-01:** Certificate generation is well-documented Microsoft API — CertificateRequest, SubjectAlternativeNameBuilder, X509Certificate2
- **Phase 13-02:** Windows X509Store API is mature and stable — StoreName.Root, StoreLocation.CurrentUser patterns
- **Phase 13-03:** Kestrel HTTPS endpoint configuration is standard ASP.NET Core pattern — ListenAnyIP with UseHttps
- **Phase 13-05:** ASP.NET Core BackgroundService is established pattern for hosted services

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | All certificate generation APIs verified with official Microsoft documentation. System.Security.Cryptography.X509Certificates is complete and mature. |
| Features | MEDIUM | Table stakes features are well-documented (mkcert, dotnet dev-certs reference). Differentiator features (hot-reload, zero-config) need implementation validation. |
| Architecture | HIGH | Certificate Authority hierarchy pattern is proven (mkcert). Dual endpoint configuration is standard Kestrel pattern. Platform-specific trust installation is well-understood. |
| Pitfalls | HIGH | All pitfalls verified with multiple sources (official docs, community issues, stack overflow). Mitigation strategies are proven. |

**Overall confidence:** HIGH

**Gaps to Address:**

1. **Wildcard .localhost Certificate Browser Acceptance:** Research indicates Safari on macOS may not support `*.localhost` wildcard certificates. **Mitigation:** Test in Phase 13-03; add `*.dev.localhost` as additional SAN if needed (per .NET 10 docs).

2. **YARP Certificate Hot-Reload:** Research shows Kestrel supports certificate reload but YARP-specific configuration is unclear. **Mitigation:** Defer hot-reload to v1.3; v1.2 uses proxy restart for renewal (acceptable for development tool).

3. **Linux Firefox Certificate Trust:** Firefox uses NSS database, not system store. **Mitigation:** Document Firefox-specific trust installation steps in Phase 13-06; this is known limitation, not implementation gap.

4. **Certificate Password Storage:** Research warns against hardcoding passwords but doesn't specify secure storage pattern for development tools. **Mitigation:** Use empty password for development certificates (documented security implication) or user-provided password via CLI.

## Sources

### Primary (HIGH confidence)

- [CertificateRequest.CreateSelfSigned Method](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.certificaterequest.createselfsigned?view=net-10.0) — Self-signed certificate creation in .NET 10, verified 2025-03-01
- [CertificateRequest.Create Method](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.certificaterequest.create?view=net-10.0) — CA-signed certificate creation, verified 2025-06-11
- [SubjectAlternativeNameBuilder Class](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.x509certificates.subjectalternativenamebuilder?view=net-9.0) — SAN extension generation, verified 2025-09-15
- [dotnet dev-certs HTTPS Tool](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-dev-certs) — Reference implementation for certificate management, verified 2025-09-28
- [Configure Kestrel HTTPS Endpoints](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0) — Kestrel HTTPS configuration, verified 2025-08-15
- [ASP.NET Core 10.0 Release Notes](https://learn.microsoft.com/en-us/aspnet/core/release-notes/aspnetcore-10.0) — `.localhost` domain support in .NET 10 Preview 7+, verified 2025-11-01
- [mkcert GitHub Repository](https://github.com/FiloSottile/mkcert) — Industry-standard local HTTPS tool (49.3k stars), verified 2026-02-22

### Secondary (MEDIUM confidence)

- [Local HTTPS Certificate Generation Tutorial (CSDN, Jan 2026)](https://blog.csdn.net/weixin_46244623/article/details/156943289) — Cross-platform mkcert tutorial with practical examples
- [SSL Certificate Renewal Practical Guide](https://m.blog.csdn.net/gitblog_01192/article/details/157240340) — Certificate renewal best practices, 30-day warning recommendation
- [YARP HTTPS Configuration with Let's Encrypt (CSDN)](https://blog.csdn.net/2501_93329146/article/details/151364447) — YARP HTTPS certificate configuration example
- [Cross-Platform Certificate Trust Installation](https://docs.redhat.com/zh-cn/documentation/red_hat_enterprise_linux/8/html/securing_networks/managing-trusted-system-certificates_using_shared-system-certificates) — Linux `trust` command reference
- [macOS security Command Reference](https://ss64.com/osx/security.html) — macOS trust installation commands

### Tertiary (LOW confidence)

- [Free SSL Certificate Updates (3-month validity period)](https://m.blog.csdn.net/gitblog_01192/article/details/157240340) — Certificate validity reduction to 3 months (needs verification)
- [Wildcard Certificate for .localhost TLD](https://blog.csdn.net/weixin_46244623/article/details/156943289) — Safari limitations with `*.localhost` (needs browser-specific testing)
- [Certificate Revocation Process](https://m.blog.csdn.net/gitblog_01192/article/details/157240340) — Confuses renewal vs revocation (conflates production and development scenarios)

---
*Research completed: 2026-02-22*
*Ready for roadmap: yes*
