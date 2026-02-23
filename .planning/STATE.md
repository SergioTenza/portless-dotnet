# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-22)

**Core value:** URLs estables y predecibles para desarrollo local
**Current focus:** Milestone v1.2 HTTPS - Certificate Management Complete (Phases 13-14), awaiting next milestone decision

## Current Position

Phase: 15 of 19 (HTTPS Endpoint)
Plan: 1 of 1 in current phase (COMPLETE)
Status: Phase 15 HTTPS endpoint complete - dual HTTP/HTTPS endpoints with automatic certificate binding
Last activity: 2026-02-23 — Phase 15-01 complete: dual HTTP/HTTPS endpoints configured
Current branch: development (active development branch)
Resume file: .planning/phases/15-https-endpoint/15-01-SUMMARY.md (plan complete)

Progress: [█████████░░░░░░░░░] 45% (Phases 13-15 complete, Phases 16-19 remaining)

**Milestone completion:**
- v1.0 MVP: Complete (2026-02-21) — 20 plans
- v1.1 Advanced Protocols: Complete (2026-02-22) — 14 plans
- v1.2 HTTPS with Automatic Certificates: In Progress (2026-02-23) — 3/7 phases complete (Phases 13-15: Certificate Management + HTTPS Endpoint)

## Performance Metrics

**Velocity:**
- Total plans completed: 73 plans (v1.0 + v1.1 + Phase 13 + Phase 14 + Phase 15-01)
- Average duration: ~10.5 min per plan
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

Last session: 2026-02-23 (Phase 15 HTTPS Endpoint Complete)
Stopped at: Phase 15-01 complete, dual HTTP/HTTPS endpoints configured with automatic certificate binding
Resume file: .planning/phases/15-https-endpoint/15-01-SUMMARY.md (plan complete)
