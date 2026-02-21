# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2025-02-19)

**Core value:** URLs estables y predecibles para desarrollo local
**Current focus:** Phase 5 - Process Management

## Current Position

Phase: 5 of 7 (Process Management)
Plan: 0 of 2 in current phase (not planned yet)
Status: Phase 5 context gathered, ready for planning
Last activity: 2026-02-21 — Phase 5 process management context captured via discuss-phase

Progress: [██████░░░░] 71%

## Performance Metrics

**Velocity:**
- Total plans completed: 10
- Average duration: 7 min
- Total execution time: 1.2 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01-proxy-core | 4 | 36 min | 9 min |
| 02-route-persistence | 3 | 22 min | 7 min |
| 03-cli-commands | 3 | 21 min | 7 min |

**Recent Trend:**
- Last 5 plans: 02-03 (16 min), 03-01 (5 min), 03-02 (9 min), 03-03 (7 min)
- Trend: Phase 3 complete, all CLI commands implemented

*Updated after each plan completion*
| Phase 02-route-persistence P03 | 16min | 3 tasks | 3 files |
| Phase 03-cli-commands P03-03 | 420 | 2 tasks | 4 files |
| Phase 03-cli-commands P03-01 | 11min | 2 tasks | 9 files |

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
- [Plan 02-01]: Atomic writes via temp file in same directory as target prevents corruption
- [Plan 02-02]: Moved DynamicConfigProvider from Portless.Proxy to Portless.Core.Configuration to resolve circular dependency
- [Plan 02-02]: RouteCleanupService validates PID recycling via Process.StartTime comparison
- [Plan 02-02]: RouteFileWatcher uses 500ms debounce timer to prevent multiple rapid YARP reloads
- [Plan 02-03]: Comprehensive test suite created with manual verification checkpoint for hot-reload and cleanup
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
- [Plan 03-01]: ProxyProcessManager with BackgroundChildProcessTracker for Windows process management
- [Plan 03-01]: Proxy lifecycle managed via dotnet run with --project flag for in-process execution
- [Plan 03-02]: List command with TTY detection - table format for terminals, JSON for pipes
- [Plan 03-02]: PID liveness detection using Process.GetProcessById() + HasExited
- [Plan 03-03]: TCP listener binding for port detection (reliable, prevents conflicts)
- [Plan 03-03]: Random port allocation in 4000-4999 range with 50 attempt limit
- [Plan 03-03]: Background process execution with UseShellExecute=true for detached execution
- [Plan 03-03]: PORT environment variable injection via ProcessStartInfo.Environment
- [Plan 03-03]: Duplicate route detection in ExecuteAsync (not Validate due to Spectre.Console.Cli DI limitation)
- [Plan 03-03]: Explicit Spectre.Console package reference for .NET 10 compatibility (transitive dependency issue)
- [Phase 03-cli-commands]: Commands organized hierarchically: proxy start/stop/status, list, run
- [Phase 03-cli-commands]: Spectre.Console.Cli with custom TypeRegistrar for dependency injection
- [Phase 03-cli-commands]: UseShellExecute=true for Windows detached process execution
- [Phase 03-cli-commands]: PID file tracking in state directory for process lifecycle
- [Phase 03-cli-commands]: Spectre.Console.Cli DI via TypeRegistrar/TypeResolver bridge pattern

### Pending Todos

None yet.

### Blockers/Concerns

None yet.

## Session Continuity

Last session: 2026-02-21 (Phase 5 discuss-phase)
Stopped at: Phase 5 context gathered
Resume file: .planning/phases/05-process-management/05-CONTEXT.md
