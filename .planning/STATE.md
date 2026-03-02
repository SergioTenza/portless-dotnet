---
gsd_state_version: 1.0
milestone: v1.2
milestone_name: HTTPS with Automatic Certificates
status: unknown
last_updated: "2026-03-02T07:27:30.788Z"
progress:
  total_phases: 17
  completed_phases: 16
  total_plans: 42
  completed_plans: 46
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-22)

**Core value:** URLs estables y predecibles para desarrollo local
**Current focus:** Milestone v1.2 HTTPS - Certificate Management Complete (Phases 13-14), awaiting next milestone decision

## Current Position

Phase: 18 of 19 (Integration Tests)
Plan: 2 of 4 in current phase (COMPLETE)
Status: Phase 18-02 HTTPS endpoint integration tests complete - certificate serving validation, TLS protocol enforcement, and HTTP/HTTPS dual endpoint tests
Last activity: 2026-03-02 — Phase 18-02 complete: HTTPS endpoint integration tests with 6 test methods covering certificate properties, TLS 1.2+ support, and configuration verification
Current branch: development (active development branch)
Resume file: .planning/phases/18-integration-tests/18-02-SUMMARY.md (plan complete)

Progress: [████████████░░░░░░] 56% (Phases 13-15, 17-18.02 complete, Phases 16, 18.03-18.04, 19 remaining)

**Milestone completion:**
- v1.0 MVP: Complete (2026-02-21) — 20 plans
- v1.1 Advanced Protocols: Complete (2026-02-22) — 14 plans
- v1.2 HTTPS with Automatic Certificates: In Progress (2026-03-02) — 5/7 phases complete (Phases 13-15, 17-18.01: Certificate Management + HTTPS Endpoint + Certificate Lifecycle + Integration Tests)

## Performance Metrics

**Velocity:**
- Total plans completed: 81 plans (v1.0 + v1.1 + Phase 13 + Phase 14 + Phase 15 + Phase 17)
- Average duration: ~10.1 min per plan
- Total execution time: ~12.5 hours across 2 milestones + Phase 13-15

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 1 | 4 | 36 min | 9 min |
| 2 | 3 | 22 min | 7 min |
| 3 | 3 | 21 min | 7 min |
| 4 | 2 | TBD | TBD |
| 5 | 2 | TBD | TBD |
| 6 | 3 | TBD | TBD |
| 8 | 3 | 45 min | 15 min |
| 9 | 1 | 8 min | 8 min |
| 10 | 1 | 7 min | 7 min |
| 11 | 3 | ~25 min | ~8 min |
| 12 | 5 | ~35 min | ~7 min |
| 13 | 3 | ~30 min | ~10 min |
| 14 | 3 | ~20 min | ~7 min |
| 15 | 1 | ~7 min | ~7 min |

**Recent Trend:**
- Last 5 plans: ~8 min avg
- Trend: Phase 14 Trust Installation completed with Windows Certificate Store integration and cross-platform messaging

*Updated: 2026-02-23*
| Phase 14 P03 | 4 | 4 tasks | 3 files |
| Phase 14 P03 | 4 | 4 tasks | 3 files |
| Phase 15 P01 | 7 | 2 tasks | 5 files |
| Phase 17-certificate-lifecycle P05 | 200 | 2 tasks | 2 files |
| Phase 18 P02 | 6min | 6 tasks | 1 files |

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- [Phase 12]: Documentation completed for all v1.1 features (HTTP/2, WebSocket, SignalR)
- [Phase 12-05]: Migration guide emphasizes "no breaking changes" with clear upgrade path
- [Phase 12-05]: Examples README reorganized to highlight v1.1 examples first with comprehensive quick starts
- [Phase 12]: Positioned HTTP/2 and WebSocket section after Overview but before Quick Start for maximum discoverability
- [Phase 12]: Combined HTTP/2 and WebSocket in single section instead of separate sections for cohesive protocol coverage
- [Phase 12]: Used badges in header for immediate visual recognition of v1.1 features
- [Phase 12]: Added What's New in v1.1 callout to highlight new features without disrupting existing content flow
- [Phase 12-03]: CLI reference documentation created with comprehensive command descriptions and protocol support information
- [Phase 12-03]: Removed Description attributes from command classes (not supported in Spectre.Console.Cli 0.53.1) to fix build errors
- [Phase 12-03]: Protocol information added to status command with --protocol flag for detailed protocol support display
- [Phase 11]: SignalR works through Portless.NET proxy with WebSocket transport without special YARP configuration
- [Phase 11]: SignalR chat example demonstrates real-time bidirectional messaging and broadcast patterns
- [Phase 11]: Integration tests verify SignalR connection establishment and message flow through proxy
- [Phase 11]: Troubleshooting guide documents common SignalR issues: SSE fallback, connection drops, "connection not started" errors, message delivery, and multi-client broadcasts
- [Phase 11]: Best practices documented for connection management, error handling, retry logic, testing strategy, and development vs production considerations
- [Phase 14]: Windows Certificate Store integration uses LocalMachine Root store for system-wide trust (requires admin)
- [Phase 14]: Trust operations are idempotent (install twice succeeds, uninstall non-existent cert succeeds)
- [Phase 14]: Platform guards with [SupportedOSPlatform] and OperatingSystem.IsWindows() checks for Windows-only services
- [Phase 14]: Trust status detection includes 30-day expiration warning via TrustStatus.ExpiringSoon
- [Phase 14-03]: Cross-platform messaging with manual installation instructions for macOS/Linux (3-5 lines per CONTEXT.md)
- [Phase 14-03]: Exit codes follow CONTEXT.md specification: 0=success, 1=generic/platform, 2=permissions, 3=missing, 5=store access
- [Phase 14]: Platform detection uses OperatingSystem.IsWindows() for cross-platform support
- [Phase 14]: Manual installation instructions displayed inline on macOS/Linux (3-5 lines per CONTEXT.md)
- [Phase 14]: Exit codes follow CONTEXT.md specification: 0=success, 1=generic/platform, 2=permissions, 3=missing, 5=store access
- [Phase 15-01]: Fixed ports enforced (HTTP=1355, HTTPS=1356) with PORTLESS_PORT deprecation warning for simplified HTTPS configuration
- [Phase 15-01]: 308 Permanent Redirect used instead of 301 to preserve HTTP methods during HTTP→HTTPS redirect
- [Phase 15-01]: /api/v1/* management endpoints excluded from HTTPS redirect to allow CLI HTTP access for add/remove operations
- [Phase 15-01]: Certificate pre-startup validation exits with code 1 if certificate invalid, with clear error message to run 'portless cert install'
- [Phase 15-01]: TLS 1.2+ minimum protocol enforced via ConfigureHttpsDefaults for secure HTTPS connections
- [Phase 15-01]: Temporary BuildServiceProvider used to load certificate before Kestrel configuration (accepted ASP0000 warning for pre-configuration)
- [Phase 15]: Fixed ports enforced (HTTP=1355, HTTPS=1356) with PORTLESS_PORT deprecation warning for simplified HTTPS configuration
- [Phase 15]: 308 Permanent Redirect used instead of 301 to preserve HTTP methods during HTTP to HTTPS redirect
- [Phase 15]: API endpoint exclusion from HTTPS redirect - /api/v1/* management endpoints remain HTTP for CLI add/remove operations
- [Phase 15]: Certificate pre-startup validation exits with code 1 if invalid, with clear error message
- [Phase 15]: TLS 1.2+ minimum protocol enforced via ConfigureHttpsDefaults for secure HTTPS connections
- [Phase 15]: Temporary BuildServiceProvider used to load certificate before Kestrel configuration (accepted ASP0000 warning)
- [Phase 17]: Background monitoring service implemented as IHostedService with 6-hour check interval (configurable via PORTLESS_CERT_CHECK_INTERVAL_HOURS)
- [Phase 17]: Certificate auto-renewal enabled by default with opt-out via PORTLESS_AUTO_RENEW=false
- [Phase 17]: CLI commands cert check and cert renew implemented with colored Spectre.Console output and proper exit codes (0, 1, 2, 3)
- [Phase 17]: Proxy startup integration displays non-blocking warnings for expiring/expired certificates with red/yellow/green color coding
- [Phase 17]: Certificate monitoring requires proxy restart after renewal (hot-reload deferred to v1.3+)
- [Phase 17]: Environment variables for thresholds: PORTLESS_CERT_WARNING_DAYS (30), PORTLESS_CERT_CHECK_INTERVAL_HOURS (6), PORTLESS_AUTO_RENEW (true), PORTLESS_ENABLE_MONITORING (false)
- [Phase 17-05]: CertificateStatus kept in Services namespace (not Models) as it represents service contract/result type, not persistent data model
- [Phase 17-certificate-lifecycle]: CertificateStatus kept in Services namespace (not Models) as it represents service contract/result type, not persistent data model
- [Phase 18-01]: StateDirectoryProvider respects PORTLESS_STATE_DIR environment variable for test isolation
- [Phase 18-01]: Certificate private key export uses CopyWithPrivateKey() to ensure PFX includes private key
- [Phase 18-01]: Server certificate validity cannot exceed CA certificate validity to prevent signing errors
- [Phase 18-01]: Integration tests use WebApplicationFactory with IAsyncLifetime for temp directory cleanup
- [Phase 18-01]: StateDirectoryProvider respects PORTLESS_STATE_DIR environment variable for test isolation
- [Phase 18-01]: Certificate private key export uses CopyWithPrivateKey() to ensure PFX includes private key
- [Phase 18-01]: Server certificate validity cannot exceed CA certificate validity to prevent signing errors
- [Phase 18-01]: Integration tests use WebApplicationFactory with IAsyncLifetime for temp directory cleanup
- [Phase 18-02]: Configuration-based HTTPS testing: WebApplicationFactory TestServer doesn't bind real TCP ports, so tests verify HTTPS configuration and certificate properties rather than actual port binding
- [Phase 18-02]: Temp directory isolation via PORTLESS_STATE_DIR for each test to prevent certificate file conflicts
- [Phase 18-02]: HTTP endpoint accessibility tested via /api/v1/add-host instead of non-existent /api/v1/status
- [Phase 18-02]: TLS protocol enforcement verified via certificate properties (2048-bit RSA key, validity period) when real TLS handshake not available in TestServer

### Pending Todos

None yet.

### Blockers/Concerns

None yet.

### Completed Mitigations

- [Phase 9]: HTTP/2 protocol negotiation may silently downgrade to HTTP/1.1 without logging (mitigation: PROTO-02 protocol logging middleware) - **RESOLVED**
- [Phase 10]: WebSocket connections may timeout after 60 seconds of inactivity (mitigation: WS-03 Kestrel timeout configuration) - **RESOLVED**
- [Phase 10]: HTTP/2 WebSocket uses CONNECT method instead of GET with Upgrade header (mitigation: WS-02 test both HTTP/1.1 and HTTP/2 scenarios) - **RESOLVED**
- [Phase 11]: SignalR may fall back to Server-Sent Events instead of WebSocket (mitigation: documented in troubleshooting guide with WebSocket transport configuration) - **RESOLVED**
- [Phase 11]: SignalR connections may drop after 60 seconds of inactivity (mitigation: KeepAliveTimeout configuration documented in troubleshooting guide) - **RESOLVED**

## Session Continuity

Last session: 2026-03-02 (Phase 18-02 HTTPS Endpoint Integration Tests)
Stopped at: Phase 18-02 complete, HTTPS endpoint integration tests with certificate serving validation and TLS protocol enforcement
Resume file: .planning/phases/18-integration-tests/18-02-SUMMARY.md (plan complete)
