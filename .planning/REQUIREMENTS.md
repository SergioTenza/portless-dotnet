# Requirements: Portless.NET

**Defined:** 2026-02-22
**Core Value:** URLs estables y predecibles para desarrollo local

## v1.2 Requirements

Requirements for HTTPS with Automatic Certificates milestone. Each maps to roadmap phases.

### Certificate Generation (CERT)

- [x] **CERT-01**: User can generate local Certificate Authority (CA) automatically on first proxy start
- [x] **CERT-02**: CA certificate has 5-year validity period (per Phase 13 user context decision)
- [x] **CERT-03**: User can generate wildcard certificate for `*.localhost` domains
- [x] **CERT-04**: Wildcard certificate includes Subject Alternative Names (SAN) for DNS (`localhost`, `*.localhost`) and IP addresses (`127.0.0.1`, `::1`)
- [x] **CERT-05**: Server certificates have 5-year validity period (per Phase 13 user context decision)
- [x] **CERT-06**: Certificates are marked exportable during creation (X509KeyStorageFlags.Exportable)
- [ ] **CERT-07**: Private keys are stored with secure file permissions (600 on Unix, ACL on Windows)
- [ ] **CERT-08**: Certificates persist to `~/.portless/ca.pfx`, `cert.pfx`, `cert-info.json`
- [x] **CERT-09**: Certificate creation uses .NET native APIs only (no BouncyCastle, OpenSSL, or mkcert dependencies)

### Trust Installation (TRUST)

- [ ] **TRUST-01**: User can install CA certificate to Windows Certificate Store via `portless cert install` command
- [ ] **TRUST-02**: Trust installation targets `Cert:\LocalMachine\Root` store using X509Store API
- [ ] **TRUST-03**: User can verify trust status via `portless cert status` command
- [ ] **TRUST-04**: Trust status check detects if CA is not trusted and displays platform-specific installation instructions
- [ ] **TRUST-05**: User can uninstall CA certificate from trust store via `portless cert uninstall` command
- [ ] **TRUST-06**: macOS/Linux trust installation deferred to v1.3+ (documented as known limitation)

### HTTPS Endpoint (HTTPS)

- [ ] **HTTPS-01**: Proxy listens on dual endpoints: HTTP (1355) and HTTPS (1356)
- [ ] **HTTPS-02**: HTTPS port is configurable via `PORTLESS_HTTPS_PORT` environment variable
- [ ] **HTTPS-03**: HTTPS endpoint uses generated wildcard certificate from `~/.portless/cert.pfx`
- [ ] **HTTPS-04**: Kestrel enforces TLS 1.2+ minimum protocol version
- [ ] **HTTPS-05**: HTTP endpoint remains functional for backward compatibility

### Mixed Protocol Support (MIXED)

- [ ] **MIXED-01**: Proxy preserves original protocol in X-Forwarded-Proto header
- [ ] **MIXED-02**: Backend HTTP services receive `X-Forwarded-Proto: http`
- [ ] **MIXED-03**: Backend HTTPS services receive `X-Forwarded-Proto: https`
- [ ] **MIXED-04**: Proxy supports mixed routing (some backends HTTP, others HTTPS)
- [ ] **MIXED-05**: YARP backend SSL validation configured for development mode (accepts self-signed certificates)

### Certificate Lifecycle (LIFECYCLE)

- [ ] **LIFECYCLE-01**: Proxy checks certificate expiration on startup
- [ ] **LIFECYCLE-02**: System displays warning when certificate expires within 30 days
- [ ] **LIFECYCLE-03**: Background hosted service checks certificate expiration every 6 hours
- [ ] **LIFECYCLE-04**: Certificate auto-renews when within 30 days of expiration
- [ ] **LIFECYCLE-05**: User can manually renew certificate via `portless cert renew` command
- [ ] **LIFECYCLE-06**: Certificate renewal requires proxy restart (hot-reload deferred to v1.3+)
- [ ] **LIFECYCLE-07**: Certificate metadata stored in `~/.portless/cert-info.json` (creation timestamp, expiration, fingerprint)

### CLI Integration (CLI)

- [ ] **CLI-01**: `portless cert install` — Install CA certificate to system trust store
- [ ] **CLI-02**: `portless cert status` — Display certificate trust status, expiration date, fingerprint
- [ ] **CLI-03**: `portless cert renew` — Manually trigger certificate renewal
- [ ] **CLI-04**: `portless cert uninstall` — Remove CA certificate from system trust store
- [ ] **CLI-05**: `portless proxy start --https` — Start proxy with HTTPS endpoint enabled
- [ ] **CLI-06**: Certificate commands display colored output with Spectre.Console formatting

### Testing (TEST)

- [ ] **TEST-01**: Integration tests verify certificate generation with correct SAN extensions
- [ ] **TEST-02**: Integration tests verify HTTPS endpoint serves valid TLS certificate
- [ ] **TEST-03**: Integration tests verify X-Forwarded-Proto header preservation
- [ ] **TEST-04**: Integration tests verify certificate renewal before expiration
- [ ] **TEST-05**: Integration tests verify trust status detection on Windows
- [ ] **TEST-06**: Integration tests cover mixed HTTP/HTTPS backend routing scenarios

### Documentation (DOCS)

- [ ] **DOCS-01**: User guide for certificate management (install, verify, renew, uninstall)
- [ ] **DOCS-02**: Troubleshooting guide for common certificate issues (untrusted CA, expired cert, SAN mismatch)
- [ ] **DOCS-03**: Migration guide from v1.1 HTTP-only to v1.2 HTTPS
- [ ] **DOCS-04**: Platform-specific notes (Windows Certificate Store, macOS/Linux deferred to v1.3)
- [ ] **DOCS-05**: Security considerations for development certificates (private key protection, trust implications)

## v1.3+ Requirements

Deferred to future release. Tracked but not in current roadmap.

### Cross-Platform Trust Installation

- **TRUST-macOS-01**: macOS Keychain trust installation via `security add-trusted-cert` command
- **TRUST-Linux-01**: Linux trust installation via distribution-specific commands (`update-ca-certificates`, `update-ca-trust`)
- **TRUST-Firefox-01**: Firefox NSS database trust installation via `certutil` commands

### Advanced Features

- **HTTPS-HotReload-01**: Certificate hot-reload without proxy restart (Kestrel certificate reload)
- **HTTPS-SNI-01**: Server Name Indication (SNI) support for multiple certificates per IP
- **HTTPS-CustomDomain-01**: Custom domain certificates beyond `.localhost` (e.g., `.local`, `.test`)
- **HTTPS-DevCerts-01**: Integration with `dotnet dev-certs` to reuse existing ASP.NET Core certificates

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Let's Encrypt integration | Let's Encrypt doesn't issue certificates for localhost/private IPs; unnecessary complexity for local development |
| Certificate revocation infrastructure | Development environment doesn't need revocation; adds massive complexity |
| EV (Extended Validation) certificates | EV requires organization validation; meaningless for localhost development |
| Multiple certificate authorities | Managing multiple CAs complicates trust installation; single CA sufficient |
| OCSP/CRL support simulation | Mock OCSP responder required; conflicts with zero-configuration goal |
| Certificate sharing across network | Exposes private key; violates security model; `.localhost` doesn't resolve network-wide |
| Certificate password protection | Private keys protected by file permissions; password adds friction for local dev |
| HSTS force-enable | HSTS headers persist for weeks; can lock developer out of localhost |
| Perfect Forward Secrecy (PFS) enforcement | PFS is default in .NET 10; manual cipher suite configuration is complex and error-prone |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| CERT-01 | Phase 13 | Complete |
| CERT-02 | Phase 13 | Complete |
| CERT-03 | Phase 13 | Complete |
| CERT-04 | Phase 13 | Complete |
| CERT-05 | Phase 13 | Complete |
| CERT-06 | Phase 13 | Complete |
| CERT-07 | Phase 13 | Pending |
| CERT-08 | Phase 13 | Pending |
| CERT-09 | Phase 13 | Complete |
| TRUST-01 | Phase 14 | Pending |
| TRUST-02 | Phase 14 | Pending |
| TRUST-03 | Phase 14 | Pending |
| TRUST-04 | Phase 14 | Pending |
| TRUST-05 | Phase 14 | Pending |
| TRUST-06 | Phase 14 | Pending |
| HTTPS-01 | Phase 15 | Pending |
| HTTPS-02 | Phase 15 | Pending |
| HTTPS-03 | Phase 15 | Pending |
| HTTPS-04 | Phase 15 | Pending |
| HTTPS-05 | Phase 15 | Pending |
| MIXED-01 | Phase 16 | Pending |
| MIXED-02 | Phase 16 | Pending |
| MIXED-03 | Phase 16 | Pending |
| MIXED-04 | Phase 16 | Pending |
| MIXED-05 | Phase 16 | Pending |
| LIFECYCLE-01 | Phase 17 | Pending |
| LIFECYCLE-02 | Phase 17 | Pending |
| LIFECYCLE-03 | Phase 17 | Pending |
| LIFECYCLE-04 | Phase 17 | Pending |
| LIFECYCLE-05 | Phase 17 | Pending |
| LIFECYCLE-06 | Phase 17 | Pending |
| LIFECYCLE-07 | Phase 17 | Pending |
| CLI-01 | Phase 14 | Pending |
| CLI-02 | Phase 14 | Pending |
| CLI-03 | Phase 17 | Pending |
| CLI-04 | Phase 14 | Pending |
| CLI-05 | Phase 15 | Pending |
| CLI-06 | Phase 17 | Pending |
| TEST-01 | Phase 18 | Pending |
| TEST-02 | Phase 18 | Pending |
| TEST-03 | Phase 18 | Pending |
| TEST-04 | Phase 18 | Pending |
| TEST-05 | Phase 18 | Pending |
| TEST-06 | Phase 18 | Pending |
| DOCS-01 | Phase 19 | Pending |
| DOCS-02 | Phase 19 | Pending |
| DOCS-03 | Phase 19 | Pending |
| DOCS-04 | Phase 19 | Pending |
| DOCS-05 | Phase 19 | Pending |

**Coverage:**
- v1.2 requirements: 36 total
- Mapped to phases: 36 (7 phases)
- Unmapped: 0 ✓

---
*Requirements defined: 2026-02-22*
*Last updated: 2026-02-22 after roadmap creation*
