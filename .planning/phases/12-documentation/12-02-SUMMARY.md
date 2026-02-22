---
phase: 12-documentation
plan: 02
subsystem: documentation
tags: troubleshooting, http2, websocket, signalr, diagnostics

# Dependency graph
requires:
  - phase: 09-http2-baseline
    provides: HTTP/2 baseline implementation and known issues (silent downgrade)
  - phase: 10-websocket-proxy
    provides: WebSocket proxy support and timeout configuration
  - phase: 11-signalr-integration
    provides: SignalR integration testing and common issues
provides:
  - Comprehensive protocol troubleshooting guide for HTTP/2 and WebSocket issues
  - Diagnostic commands and verification procedures
  - Common error messages and solutions
  - Link from main README to detailed troubleshooting guide
affects: users encountering protocol issues, support workflow, documentation completeness

# Tech tracking
tech-stack:
  added: []
  patterns:
    - Troubleshooting guide structure with symptoms, causes, solutions, verification
    - Diagnostic command reference for curl, browser DevTools, portless CLI
    - Cross-linking between main README and detailed guides

key-files:
  created:
    - docs/protocol-troubleshooting.md
  modified:
    - README.md

key-decisions:
  - "Organized troubleshooting guide by protocol (HTTP/2, WebSocket) then by issue type"
  - "Included both quick fixes and detailed explanations for each issue"
  - "Added verification commands for each troubleshooting section"
  - "Documented silent HTTP/2 downgrade as expected behavior, not a bug"
  - "Provided curl --http2-prior-knowledge workaround for HTTP/2 testing"

patterns-established:
  - "Troubleshooting pattern: Symptom → Cause → Solutions → Verification"
  - "Diagnostic reference: CLI commands, curl commands, browser DevTools"
  - "Cross-linking: Main README provides quick fixes, detailed guide provides in-depth solutions"

requirements-completed: [DOC-01, DOC-02, DOC-03, DOC-04]

# Metrics
duration: 8min
completed: 2026-02-22
---

# Phase 12: Plan 02 - Create Protocol Troubleshooting Guide Summary

**Comprehensive troubleshooting guide for HTTP/2 silent downgrade, WebSocket timeouts, and protocol diagnostics with curl commands and browser DevTools instructions**

## Performance

- **Duration:** 8 min
- **Started:** 2026-02-22T15:15:30Z
- **Completed:** 2026-02-22T15:23:30Z
- **Tasks:** 5
- **Files modified:** 2

## Accomplishments

- Created comprehensive troubleshooting guide covering HTTP/2 and WebSocket issues
- Documented silent HTTP/2 downgrade issue with root cause explanation and workarounds
- Documented WebSocket timeout issues with Kestrel configuration solutions
- Included diagnostic commands for testing protocols (curl, browser DevTools, portless CLI)
- Added troubleshooting section to main README with quick fixes and link to detailed guide

## Task Commits

Each task was committed atomically:

1. **Task 1-5: Create Protocol Troubleshooting Guide** - `6d8e2b5` (docs)

**Plan metadata:** N/A (all tasks combined in single documentation commit)

_Note: Documentation plan - all tasks completed in single comprehensive commit_

## Files Created/Modified

- `docs/protocol-troubleshooting.md` - Comprehensive troubleshooting guide for HTTP/2 and WebSocket issues (344 lines)
  - HTTP/2 Issues: Silent downgrade, negotiation failures
  - WebSocket Issues: Connection timeouts, HTTP/1.1 vs HTTP/2, SignalR connection failures
  - Diagnostic Commands: Proxy status, HTTP/2 testing, WebSocket testing, browser DevTools
  - Common Error Messages: Protocol downgrade, WebSocket failures, timeouts, port conflicts
- `README.md` - Added troubleshooting section with protocol-specific quick fixes and link to detailed guide

## Decisions Made

- **Troubleshooting guide organization:** Structured by protocol (HTTP/2, WebSocket) then by issue type for easy navigation
- **Silent downgrade framing:** Documented as expected behavior of HTTP/2 over plain HTTP, not a bug - requires HTTPS or prior knowledge
- **Diagnostic approach:** Provided multiple diagnostic methods (CLI commands, curl, browser DevTools) to accommodate different user preferences
- **Quick fixes vs detailed solutions:** Main README provides quick fixes, detailed guide provides in-depth explanations and verification steps
- **Code examples:** Included copy-paste ready curl commands and bash scripts for Windows/macOS/Linux

## Deviations from Plan

None - plan executed exactly as written. All 5 tasks completed as specified:

1. Created troubleshooting guide document at `docs/protocol-troubleshooting.md` with full structure
2. Documented silent downgrade issue with cause (HTTP/2 requires HTTPS or prior knowledge) and solution (curl --http2-prior-knowledge)
3. Documented WebSocket timeout issues with KeepAliveTimeout configuration examples
4. Documented diagnostic commands (portless list, portless proxy status, curl --http2, browser DevTools)
5. Added troubleshooting section to README with protocol-specific quick fixes and link to detailed guide

## Issues Encountered

None - documentation plan executed smoothly without technical issues.

## Self-Check: PASSED

All claims verified:
- Created file `docs/protocol-troubleshooting.md` exists (344 lines)
- Created file `.planning/phases/12-documentation/12-02-SUMMARY.md` exists
- Commit `6d8e2b5` exists in git history
- Troubleshooting section exists in README.md (line 407: "## 🔍 Troubleshooting")
- Links to protocol-troubleshooting.md guide present in README

## User Setup Required

None - no external service configuration required. This is a documentation-only plan.

## Next Phase Readiness

- Protocol troubleshooting guide complete and linked from main README
- Users experiencing HTTP/2 or WebSocket issues have comprehensive diagnostic and resolution procedures
- Quick fixes available in main README for common issues (port conflicts, proxy status)
- Ready for remaining documentation plans (12-03: CLI help text, 12-04: Protocol testing guide, 12-05: Migration guide)

**Note:** Plan 12-02 is the second of 5 documentation plans in Phase 12. All protocol troubleshooting content is now complete.

---
*Phase: 12-documentation*
*Plan: 02*
*Completed: 2026-02-22*
