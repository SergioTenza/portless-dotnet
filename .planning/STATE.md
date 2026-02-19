# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2025-02-19)

**Core value:** URLs estables y predecibles para desarrollo local
**Current focus:** Phase 2 - Route Persistence

## Current Position

Phase: 2 of 7 (Route Persistence)
Plan: 2 of 3 in current phase
Status: Plan 02-02 completed, background cleanup and hot-reload integration
Last activity: 2026-02-19 — Plan 02-02 executed successfully (RouteCleanupService, RouteFileWatcher, persistence integration)

Progress: [██░░░░░░░░░] 22%

## Performance Metrics

**Velocity:**
- Total plans completed: 6
- Average duration: 8 min
- Total execution time: 0.8 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01-proxy-core | 4 | 36 min | 9 min |
| 02-route-persistence | 2 | 6 min | 3 min |

**Recent Trend:**
- Last 5 plans: 01-02 (5 min), 01-03 (1 min), 01-04 (15 min), 02-01 (1 min), 02-02 (5 min)
- Trend: Consistent implementation, phase 2 progressing

*Updated after each plan completion*
| Phase 02-route-persistence P02 | 5min | 3 tasks | 9 files |

## Accumulated Context

### Decisions

Decisions are logged in PROJECT.md Key Decisions table.
Recent decisions affecting current work:

- [Initialization]: YARP seleccionado como motor de proxy inverso (production-ready de Microsoft)
- [Initialization]: .NET 10 con Native AOT para single binary deployment
- [Initialization]: Spectre.Console.Cli para experiencia CLI mejorada
- [Plan 01-02]: Renamed InMemoryConfigProvider to DynamicConfigProvider to avoid YARP naming conflict
- [Plan 01-02]: Used CancellationChangeToken for simplified change token implementation
- [Plan 01-02]: API endpoint preserves existing routes when adding new hosts
- [Plan 01-03]: RequestLoggingMiddleware must execute BEFORE MapReverseProxy() because MapReverseProxy() is terminal middleware
- [Plan 01-04]: Used WebApplicationFactory<Program> for in-memory integration testing of YARP routing
- [Plan 01-04]: Fixed DynamicConfigProvider registration to properly connect with YARP DI container
- [Phase 01-proxy-core]: RequestLoggingMiddleware must execute BEFORE MapReverseProxy() because MapReverseProxy() is terminal middleware
- [Plan 02-01]: Named mutex "Portless.Routes.Lock" for cross-process file locking with 5-second timeout
- [Plan 02-01]: Atomic writes via temp file in same directory as target to prevent corruption
- [Plan 02-01]: Platform-specific state directories: Windows (%APPDATA%/portless) vs Unix (~/.portless)
- [Plan 02-01]: Graceful degradation pattern - returns empty array if routes.json missing
- [Plan 02-02]: RouteCleanupService runs every 30 seconds with Process.GetProcessById() + HasExited validation
- [Plan 02-02]: PID recycling detection via StartTime comparison to prevent false positives
- [Plan 02-02]: RouteFileWatcher with 500ms debounce monitors routes.json for hot-reload
- [Plan 02-02]: DynamicConfigProvider moved from Portless.Proxy to Portless.Core.Configuration to resolve circular dependency
- [Plan 02-02]: Proxy startup loads existing routes from persistence layer before YARP initialization
- [Plan 02-02]: /api/v1/add-host endpoint persists new routes with extracted port and current PID
- [Phase 02-route-persistence]: Named mutex "Portless.Routes.Lock" for cross-process file locking with 5-second timeout
- [Phase 02-route-persistence]: Atomic writes via temp file in same directory as target to prevent corruption
- [Phase 02-route-persistence]: Platform-specific state directories: Windows (%APPDATA%/portless) vs Unix (~/.portless)
- [Phase 02-route-persistence]: Graceful degradation pattern - returns empty array if routes.json missing

### Pending Todos

None yet.

### Blockers/Concerns

None yet.

## Session Continuity

Last session: 2026-02-19 (Plan 02-02 execution)
Stopped at: Completed Plan 02-02 - background cleanup service and hot-reload integration
Resume file: None
