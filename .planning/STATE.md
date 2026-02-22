# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-22)

**Core value:** URLs estables y predecibles para desarrollo local
**Current focus:** Phase 11 - SignalR Integration

## Current Position

Phase: 11 of 12 (SignalR Integration)
Plan: 2 of 3 complete
Status: In progress - Plan 11-03 (SignalR Documentation) remaining
Last activity: 2026-02-22 — Plan 11-02 SignalR Integration Test completed
Current branch: development (active development branch)

Progress: [██████████░░░░░░░] 56% (22/20 plans complete from v1.0, 2/3 plans complete in Phase 11)

## Performance Metrics

**Velocity:**
- Total plans completed: 22
- Average duration: ~24 min
- Total execution time: ~9 hours

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

**Recent Trend:**
- Last 3 plans: ~10 min avg
- Trend: Stable

*Updated after v1.0 completion*
| Phase 09-http2-baseline P01 | 8 | 4 tasks | 2 files |
| Phase 10 P01 | 444 | 4 tasks | 7 files |

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- [Phase 8]: Integration test suite provides safety net for v1.1 protocol changes
- [Phase 6]: PackAsTool works with acceptable warnings for .NET 10 Native AOT
- [Phase 1]: YARP selected as reverse proxy engine with built-in HTTP/2 and WebSocket support
- [Research]: HTTP/2 and WebSocket support require configuration changes only, no new packages (YARP 2.3.0 already supports both)
- [Phase 09]: Used ForwardedHeaders middleware instead of YARP transforms for X-Forwarded headers (simpler, built-in support)
- [Phase 09]: HTTP/2 over HTTP requires prior knowledge (curl --http2-prior-knowledge), HTTPS requires TLS 1.2+ for ALPN
- [Phase 10]: Set Kestrel KeepAliveTimeout to 10 minutes and MaxConcurrentUpgradedConnections to 1000 for long-lived WebSocket connections
- [Phase 10]: Created WebSocket echo server example for testing and documentation
- [Phase 10]: Integration tests verify WebSocket bidirectional messaging, long-lived connections, and concurrent connections
- [Phase 10]: Kestrel KeepAliveTimeout set to 10 minutes, MaxConcurrentUpgradedConnections to 1000 for long-lived WebSocket connections
- [Phase 10]: Simple echo server example chosen over full chat app for easier testing and clearer WebSocket demonstration
- [Phase 10]: Integration tests use direct WebSocket connections (not through proxy); end-to-end proxy testing deferred to Phase 11
- [Phase 11]: SignalR works through Portless.NET proxy with WebSocket transport without special YARP configuration
- [Phase 11]: SignalR chat example demonstrates real-time bidirectional messaging and broadcast patterns
- [Phase 11]: Integration tests verify SignalR connection establishment and message flow through proxy
- [Phase 11]: Troubleshooting guide documents common SignalR issues: SSE fallback, connection drops, "connection not started" errors, message delivery, and multi-client broadcasts
- [Phase 11]: Best practices documented for connection management, error handling, retry logic, testing strategy, and development vs production considerations

### Pending Todos

None yet.

### Blockers/Concerns

- [Phase 12]: Documentation updates needed for HTTP/2, WebSocket, and SignalR features

### Completed Mitigations

- [Phase 9]: HTTP/2 protocol negotiation may silently downgrade to HTTP/1.1 without logging (mitigation: PROTO-02 protocol logging middleware) - **RESOLVED**
- [Phase 10]: WebSocket connections may timeout after 60 seconds of inactivity (mitigation: WS-03 Kestrel timeout configuration) - **RESOLVED**
- [Phase 10]: HTTP/2 WebSocket uses CONNECT method instead of GET with Upgrade header (mitigation: WS-02 test both HTTP/1.1 and HTTP/2 scenarios) - **RESOLVED**
- [Phase 11]: SignalR may fall back to Server-Sent Events instead of WebSocket (mitigation: documented in troubleshooting guide with WebSocket transport configuration) - **RESOLVED**
- [Phase 11]: SignalR connections may drop after 60 seconds of inactivity (mitigation: KeepAliveTimeout configuration documented in troubleshooting guide) - **RESOLVED**

## Session Continuity

Last session: 2026-02-22 (Phase 11 completion)
Stopped at: Phase 11 SignalR Integration completed, ready for Phase 12 Documentation
Resume file: None
