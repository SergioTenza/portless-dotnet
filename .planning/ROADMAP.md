# Roadmap: Portless.NET

## Overview

Portless.NET delivers stable `.localhost` URLs for Windows .NET development through a reverse proxy with automatic port management. v1.0 established HTTP/1.1 proxying, CLI commands, and route persistence. v1.1 added advanced protocol support (HTTP/2 and WebSockets) to enable real-time applications. v1.2 brings HTTPS support with automatic certificate generation, completing the secure local development experience. The roadmap evolves from MVP protocol support to advanced features, with each phase delivering complete, verifiable capabilities.

## Milestones

- ✅ **v1.0 MVP** - Phases 1-8 (shipped 2026-02-21)
- ✅ **v1.1 Advanced Protocols** - Phases 9-12 (shipped 2026-02-22)
- 🟡 **v1.2 HTTPS with Automatic Certificates** - Phases 13-19 (Phases 13-14 complete, 2/7 phases done, certificate management complete)

## Phases

<details>
<summary>✅ v1.0 MVP (Phases 1-8) - SHIPPED 2026-02-21</summary>

**See:** [Full milestone details](.planning/milestones/v1.0-ROADMAP.md)

- [x] **Phase 1: Proxy Core** (4/4 plans) — HTTP proxy funcional con routing por hostname
- [x] **Phase 2: Route Persistence** (3/3 plans) — Gestión de rutas con persistencia en archivo JSON
- [x] **Phase 3: CLI Commands** (3/3 plans) — Comandos básicos para controlar proxy y ejecutar apps
- [x] **Phase 4: Port Management** (2/2 plans) — Detección y asignación automática de puertos
- [x] **Phase 5: Process Management** (2/2 plans) — Spawning, tracking y cleanup de procesos
- [x] **Phase 6: .NET Integration** (3/3 plans) — Empaquetado como dotnet tool y ejemplos de integración
- [x] **Phase 8: Integration Tests** (3/3 plans) — Suite de pruebas automatizadas

**Delivered:**
- HTTP proxy YARP con routing por hostname
- CLI completa (proxy start/stop/status, list, run)
- Asignación automática de puertos (4000-4999)
- Persistencia de rutas con hot-reload
- Gestión de procesos con PORT injection
- Global tool installation (761KB package)
- 4 ejemplos de integración + documentación (3,049 lines)
- 45 tests de integración

</details>

<details>
<summary>✅ Phase 13: Certificate Generation - COMPLETE 2026-02-22</summary>

**Goal**: Automatic generation of local Certificate Authority and wildcard certificates for `.localhost` domains
**Depends on**: Phase 12
**Requirements**: CERT-01, CERT-02, CERT-03, CERT-04, CERT-05, CERT-06, CERT-07, CERT-08, CERT-09

**Plans:**
- [x] 13-01-PLAN.md — Certificate generation service (CA + wildcard certificates with .NET native APIs)
- [x] 13-02-PLAN.md — Certificate storage and secure file permissions (cross-platform PFX + JSON persistence)
- [x] 13-03-PLAN.md — Certificate manager orchestration (lifecycle, validation, auto-regeneration)

**Delivered:**
- ICertificateService with CA and wildcard certificate generation using .NET 10 native APIs
- CertificateAuthority (4096-bit RSA, 5-year validity, CA extensions)
- Wildcard certificate (2048-bit RSA, SAN for localhost/*.localhost/127.0.0.1/::1, 5-year validity)
- Three-file storage strategy (ca.pfx, cert.pfx, cert-info.json) with secure permissions
- Cross-platform file permission service (chmod 600 on Unix, ACL on Windows)
- ICertificateManager orchestration with automatic lifecycle management
- First-time generation with user notification (logger prompt)
- Existing certificate reuse without prompting
- Automatic regeneration for corrupted certificates
- 30-day expiration warning window
- File permission verification with security warnings
- DI registration via AddPortlessCertificates extension

</details>

<details>
<summary>✅ v1.1 Advanced Protocols (Phases 9-12) - SHIPPED 2026-02-22</summary>

**See:** [Full milestone details](.planning/milestones/v1.1-ROADMAP.md)

- [x] **Phase 9: HTTP/2 Baseline** (1/1 plans) — Soporte HTTP/2 con Kestrel y detección de protocolo
- [x] **Phase 10: WebSocket Proxy** (1/1 plans) — Proxy transparente para conexiones WebSocket HTTP/1.1 y HTTP/2
- [x] **Phase 11: SignalR Integration** (3/3 plans) — Ejemplo de chat SignalR y pruebas de integración
- [x] **Phase 12: Documentation** (5/5 plans) — Documentación completa de protocolos avanzados

**Delivered:**
- HTTP/2 support con Kestrel (ALPN negotiation, protocol logging)
- WebSocket transparent proxy (HTTP/1.1 upgrade + HTTP/2 Extended CONNECT)
- SignalR chat example con browser y console clients
- Integration tests para HTTP/2, WebSocket y SignalR
- Documentación completa (troubleshooting guides, migration guide, protocol testing)
- 3 nuevos ejemplos (WebSocketEchoServer, SignalRChat, HTTP/2 tests)
- 8 nuevos tests de integración

</details>

### 🟡 v1.2 HTTPS with Automatic Certificates (Partial - Certificate Management Complete)

**Milestone Status:** Phases 13-14 complete (certificate generation & trust management). Phases 15-19 deferred (HTTPS proxy integration, lifecycle, tests, docs).

**Milestone Goal:** HTTPS support con certificados TLS automáticos generados on-the-fly para desarrollo local seguro sin configuración manual

**See:** [Full milestone details](.planning/milestones/v1.2-ROADMAP.md)

#### Phase 14: Trust Installation
**Goal**: Windows-based CA certificate trust installation with status verification
**Depends on**: Phase 13
**Requirements**: TRUST-01, TRUST-02, TRUST-03, TRUST-04, TRUST-05, TRUST-06, CLI-01, CLI-02, CLI-04
**Success Criteria** (what must be TRUE):
  1. User can install CA certificate to Windows Certificate Store via `portless cert install` command
  2. User can verify trust status via `portless cert status` command (displays fingerprint, expiration, trust state)
  3. Trust status check detects if CA is not trusted and displays platform-specific installation instructions
  4. User can uninstall CA certificate from trust store via `portless cert uninstall` command
  5. macOS/Linux trust installation is documented as known limitation (deferred to v1.3+)
**Plans**: 3 plans

Plans:
- [ ] 14-01-PLAN.md — Certificate trust service with Windows X509Store API (TRUST-01, TRUST-02, TRUST-05)
- [ ] 14-02-PLAN.md — CLI commands for certificate trust management (CLI-01, CLI-02, CLI-04, TRUST-03, TRUST-04)
- [ ] 14-03-PLAN.md — Cross-platform messaging and error handling (TRUST-04, TRUST-06)

#### Phase 15: HTTPS Endpoint
**Goal**: Dual HTTP/HTTPS proxy endpoints with automatic certificate binding
**Depends on**: Phase 13
**Requirements**: HTTPS-01, HTTPS-02, HTTPS-03, HTTPS-04, HTTPS-05, CLI-05
**Success Criteria** (what must be TRUE):
  1. Proxy listens on dual endpoints: HTTP (1355) and HTTPS (1356, fixed ports - breaking change)
  2. HTTPS endpoint serves valid wildcard certificate matching `*.localhost` domains
  3. Browsers accept HTTPS connection without certificate warnings (after trust installation)
  4. Kestrel enforces TLS 1.2+ minimum protocol version
  5. User can start proxy with HTTPS enabled via `portless proxy start --https` command
**Plans**: 1 plan

Plans:
- [x] 15-01-PLAN.md — Dual HTTP/HTTPS endpoints with certificate binding and CLI --https flag

#### Phase 16: Mixed Protocol Support
**Goal**: Transparent protocol forwarding for mixed HTTP/HTTPS backend services
**Depends on**: Phase 15
**Requirements**: MIXED-01, MIXED-02, MIXED-03, MIXED-04, MIXED-05
**Success Criteria** (what must be TRUE):
  1. Backend HTTP services receive `X-Forwarded-Proto: http` header
  2. Backend HTTPS services receive `X-Forwarded-Proto: https` header
  3. Proxy supports mixed routing (some backends HTTP, others HTTPS) simultaneously
  4. YARP backend SSL validation accepts self-signed certificates in development mode
  5. Backend services can detect original protocol from forwarded headers
**Plans**: 1 plan

Plans:
- [ ] 16-01-PLAN.md — Add YARP HttpClient configuration for mixed HTTP/HTTPS backend support with development SSL validation (MIXED-01, MIXED-02, MIXED-03, MIXED-04, MIXED-05)

#### Phase 17: Certificate Lifecycle
**Goal**: Automatic certificate expiration monitoring and renewal
**Depends on**: Phase 13
**Requirements**: LIFECYCLE-01, LIFECYCLE-02, LIFECYCLE-03, LIFECYCLE-04, LIFECYCLE-05, LIFECYCLE-06, LIFECYCLE-07, CLI-03, CLI-06
**Success Criteria** (what must be TRUE):
  1. Proxy checks certificate expiration on startup and displays warning within 30 days of expiry
  2. Background hosted service checks certificate expiration every 6 hours
  3. Certificate auto-renews when within 30 days of expiration
  4. Certificate metadata stored in `~/.portless/cert-info.json` (creation timestamp, expiration, fingerprint)
  5. User can manually renew certificate via `portless cert renew` command with colored Spectre.Console output
  6. Certificate renewal requires proxy restart (documented limitation, hot-reload deferred to v1.3+)
**Plans**: 5 plans (4 implementation + 1 gap closure)

Plans:
- [x] 17-01-PLAN.md — Background certificate monitoring service with IHostedService (LIFECYCLE-01, LIFECYCLE-02, LIFECYCLE-03, LIFECYCLE-04) ✅ Complete 2026-02-23
- [x] 17-02-PLAN.md — CLI certificate renewal and check commands (CLI-03, CLI-06, LIFECYCLE-05) ✅ Complete 2026-02-23
- [x] 17-03-PLAN.md — Proxy startup certificate check integration (LIFECYCLE-01, LIFECYCLE-02) ✅ Complete 2026-02-23
- [x] 17-04-PLAN.md — Environment variable configuration and documentation (LIFECYCLE-06, DOCS-01, DOCS-05) ✅ Complete 2026-02-23
- [ ] 17-05-PLAN.md — Fix namespace compilation errors in certificate monitoring service (gap closure) 🔄 Planning 2026-02-23

#### Phase 18: Integration Tests
**Goal**: Comprehensive test coverage for HTTPS features
**Depends on**: Phase 15, Phase 16, Phase 17
**Requirements**: TEST-01, TEST-02, TEST-03, TEST-04, TEST-05, TEST-06
**Success Criteria** (what must be TRUE):
  1. Integration tests verify certificate generation with correct SAN extensions
  2. Integration tests verify HTTPS endpoint serves valid TLS certificate
  3. Integration tests verify X-Forwarded-Proto header preservation (HTTP vs HTTPS backends)
  4. Integration tests verify certificate renewal before expiration
  5. Integration tests verify trust status detection on Windows
  6. Integration tests cover mixed HTTP/HTTPS backend routing scenarios
**Plans**: TBD

Plans:
- [ ] 18-01: [TBD during planning]

#### Phase 19: Documentation
**Goal**: Complete user-facing documentation for HTTPS certificate management
**Depends on**: Phase 14, Phase 15, Phase 17
**Requirements**: DOCS-01, DOCS-02, DOCS-03, DOCS-04, DOCS-05
**Success Criteria** (what must be TRUE):
  1. User guide for certificate management (install, verify, renew, uninstall)
  2. Troubleshooting guide for common certificate issues (untrusted CA, expired cert, SAN mismatch)
  3. Migration guide from v1.1 HTTP-only to v1.2 HTTPS
  4. Platform-specific notes (Windows Certificate Store, macOS/Linux deferred to v1.3)
  5. Security considerations for development certificates (private key protection, trust implications)
**Plans**: TBD

Plans:
- [ ] 19-01: [TBD during planning]

## Progress

**Execution Order:**
Phases execute in numeric order: 13 → 14 → 15 → 16 → 17 → 18 → 19

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 1. Proxy Core | v1.0 | 4/4 | Complete | 2026-02-19 |
| 2. Route Persistence | v1.0 | 3/3 | Complete | 2026-02-19 |
| 3. CLI Commands | v1.0 | 3/3 | Complete | 2026-02-19 |
| 4. Port Management | v1.0 | 2/2 | Complete | 2026-02-20 |
| 5. Process Management | v1.0 | 2/2 | Complete | 2026-02-21 |
| 6. .NET Integration | v1.0 | 3/3 | Complete | 2026-02-21 |
| 8. Integration Tests | v1.0 | 3/3 | Complete | 2026-02-21 |
| 9. HTTP/2 Baseline | v1.1 | 1/1 | Complete | 2026-02-22 |
| 10. WebSocket Proxy | v1.1 | 1/1 | Complete | 2026-02-22 |
| 11. SignalR Integration | v1.1 | 3/3 | Complete | 2026-02-22 |
| 12. Documentation | v1.1 | 5/5 | Complete | 2026-02-22 |
| 13. Certificate Generation | v1.2 | 3/3 | Complete | 2026-02-22 |
| 14. Trust Installation | v1.2 | 3/3 | Complete | 2026-02-23 |
| 15. HTTPS Endpoint | v1.2 | 1/1 | Complete | 2026-02-23 |
| 16. Mixed Protocol Support | v1.2 | 1/1 | Not started | - |
| 17. Certificate Lifecycle | v1.2 | 1/1 | Complete | 2026-02-23 |
| 18. Integration Tests | v1.2 | 0/0 | Not started | - |
| 19. Documentation | v1.2 | 0/0 | Not started | - |

**For detailed milestone information, see:**
- [milestones/v1.0-ROADMAP.md](.planning/milestones/v1.0-ROADMAP.md) - MVP (Phases 1-8)
- [milestones/v1.1-ROADMAP.md](.planning/milestones/v1.1-ROADMAP.md) - Advanced Protocols (Phases 9-12)
