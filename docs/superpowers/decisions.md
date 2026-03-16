# Architectural and Technical Decisions

Extracted from GSD framework (`.planning.archived/`) on 2026-03-16.

**Total Decisions:** ~72 (12 architectural from PROJECT.md + ~60 phase-level from STATE.md)

---

## Architectural Decisions (from PROJECT.md)

### Decision A1: YARP vs Custom Proxy
**Status:** ✓ Good | **Date:** 2026-02-19
Use YARP (production-ready from Microsoft) instead of custom proxy
**Outcome:** HTTP/1.1, HTTP/2, WebSocket all working correctly

### Decision A2: Evolutivo vs Feature-Complete v1
**Status:** ✓ Good | **Date:** 2026-02-19
Validate MVP first, add complexity (HTTP/2, HTTPS) in later phases
**Outcome:** v1.0 MVP shipped, v1.1 HTTP/2/WebSocket shipped, v1.2 HTTPS in progress

### Decision A3: .NET 10 with Native AOT
**Status:** ✓ Good | **Date:** 2026-02-19
Single binary, better performance than Node.js
**Outcome:** 761KB NuGet package, excellent startup performance

### Decision A4: Spectre.Console.Cli over System.CommandLine
**Status:** ✓ Good | **Date:** 2026-02-19
Better colored output and formatting
**Outcome:** CLI working with all commands (proxy, list, cert)

### Decision A5: PackAsTool Distribution
**Status:** ✓ Good | **Date:** 2026-02-21
dotnet tool global distribution
**Outcome:** Simple installation, 761KB package size

### Decision A6: Cross-Platform Installation Scripts
**Status:** ✓ Good | **Date:** 2026-02-21
bash/PowerShell scripts for PATH configuration
**Outcome:** Automated PATH setup working

### Decision A7: Integration Examples
**Status:** ✓ Good | **Date:** 2026-02-21
4 example projects (WebApi, Blazor, Worker, Console)
**Outcome:** 3,049 lines of documentation and examples

### Decision A8: Progressive Documentation
**Status:** ✓ Good | **Date:** 2026-02-22
Build documentation with each milestone
**Outcome:** v1.0, v1.1, v1.2 docs all shipped

### Decision A9: ForwardedHeaders vs YARP Transforms
**Status:** ✓ Good | **Date:** 2026-02-22
ASP.NET Core built-in middleware simpler than YARP transforms
**Outcome:** X-Forwarded-* headers working correctly

### Decision A10: Kestrel Timeout (10-min)
**Status:** ✓ Good | **Date:** 2026-02-22
Long-lived WebSocket connections for real-time apps
**Outcome:** SignalR chat working without disconnections

### Decision A11: SignalR Without YARP Special Config
**Status:** ✓ Good | **Date:** 2026-02-22
WebSocket transport works automatically through YARP
**Outcome:** SignalR chat working with no special configuration

### Decision A12: Echo Server vs Full Chat App
**Status:** ✓ Good | **Date:** 2026-02-22
Simple echo server easier to test automatically
**Outcome:** WebSocket protocol coverage good, SignalR added separately

---

## Phase-Level Decisions (from STATE.md)

**Full details:** See `.planning.archived/STATE.md` lines 80-180 (Accumulated Context → Decisions)

**Categories (~60 decisions):**
- Certificate generation (Phase 13): .NET native APIs, PFX storage, secure permissions
- Trust installation (Phase 14): Windows X509Store API, platform guards, idempotent operations
- HTTPS endpoint (Phase 15): Fixed ports (1355/1356), TLS 1.2+ enforcement
- Mixed protocol (Phase 16): X-Forwarded-Proto headers, YARP HttpClient config
- Certificate lifecycle (Phase 17): IHostedService monitoring, auto-renewal
- Integration tests (Phase 18): WebApplicationFactory patterns, test isolation
- Documentation (Phase 19): Migration guide structure, troubleshooting format

**Key Recent Decisions:**
- Fixed ports enforced with PORTLESS_PORT deprecation warning
- 308 Permanent Redirect for HTTP→HTTPS
- /api/v1/* endpoints excluded from HTTPS redirect
- Certificate pre-startup validation exits code 1 if invalid
- TLS 1.2+ minimum protocol enforced
- Background monitoring service with 6-hour check interval
- Integration tests use configuration verification vs actual TLS handshake

---

*See STATE.md for full ~60 phase-level decisions with detailed rationale*
