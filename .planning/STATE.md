# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-02-21)

**Core value:** URLs estables y predecibles para desarrollo local
**Current focus:** v1.0 MVP shipped — Planning next milestone

## Current Position

Milestone: v1.0 MVP — SHIPPED 2026-02-21
Status: All phases complete (7 phases, 20 plans)
Phase 07 (Cross-Platform) deferred to future milestone
Last activity: 2026-02-21 - Completed quick task 2: Establecer flujo de trabajo git con rama development
Current branch: development (active development branch)

Progress: [████████████████████] 20/20 plans (100%)

## Git Workflow

Branch structure established to isolate development from production:

- **main**: Production branch (v1.0 MVP stable)
  - Protected from direct commits during development
  - Only updated via merges from development when ready for release
  - Represents stable, released code

- **development**: Active development branch
  - All new feature development happens here
  - Merged into main when ready for release
  - Allows experimentation without affecting production

Workflow rules:
- New development happens in development branch
- Merge development → main when ready for release
- main is protected, only for releases
- Both branches tracked on remote for collaboration

## Performance Metrics

**Velocity:**
- Total plans completed: 16
- Average duration: 7 min
- Total execution time: 1.9 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 01-proxy-core | 4 | 36 min | 9 min |
| 02-route-persistence | 3 | 22 min | 7 min |
| 03-cli-commands | 3 | 21 min | 7 min |
| 08-integration-tests | 3 | 45 min | 15 min |

**Recent Trend:**
- Last 3 plans: 08-01 (15 min), 08-02 (20 min), 08-03 (10 min)
- Trend: Phase 8 complete, 45 tests created for comprehensive validation

*Updated after each plan completion*
| Phase 02-route-persistence P03 | 16min | 3 tasks | 3 files |
| Phase 03-cli-commands P03-03 | 420 | 2 tasks | 4 files |
| Phase 03-cli-commands P03-01 | 11min | 2 tasks | 9 files |
| Phase 05-process-management P01 | 236 | 3 tasks | 5 files |
| Phase 05 P02 | 173 | 3 tasks | 4 files |
| Phase 06 P01 | 5 | 3 tasks | 1 files |
| Phase 06 P02 | 19 | 3 tasks | 11 files |
| Phase 06 P03 | 4024 | 3 tasks | 10 files |

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
- [Phase 05]: Use System.Diagnostics.Process.Start with UseShellExecute=false for PORT environment variable injection
- [Phase 05]: Use BackgroundService pattern with 5-second polling interval per CONTEXT.md locked decision
- [Phase 05]: Detect PID recycling via StartTime comparison with 1-second buffer per RESEARCH.md
- [Phase 05]: Coordinated cleanup releases ports AND removes routes atomically
- [Phase 05]: Working directory defaults to Directory.GetCurrentDirectory() per CONTEXT.md decision
- [Phase 05]: Use CloseMainWindow for SIGTERM forwarding (GUI-friendly, cross-platform)
- [Phase 05]: 10-second timeout before force kill per CONTEXT.md locked decision
- [Phase 05]: Default to false for user prompt to avoid accidental data loss
- [Phase 05]: Store managed PIDs in JSON file for persistence across proxy restarts
- [Phase 08]: Three-tier test architecture: Unit (Portless.Tests), Integration (Portless.IntegrationTests), E2E (Portless.E2ETests)
- [Phase 08]: Integration tests use in-process CommandApp testing via Spectre.Console.Cli TypeRegistrar
- [Phase 08]: E2E tests use real Process.Start with solution root working directory resolution
- [Phase 08]: TestRouteStore implementation for isolated persistence testing with custom directories
- [Phase 08]: WebApplicationFactory<Program> for in-memory YARP routing tests (502 responses acceptable)
- [Phase 08]: Cross-platform validation using Path.GetTempPath(), Path.DirectorySeparatorChar detection
- [Phase 08]: Named mutex "Portless.Routes.Lock" for concurrent file access testing
- [Phase 08]: Atomic write pattern via temp file for corruption-free persistence testing
- [Phase 06]: All example projects use Environment.GetEnvironmentVariable("PORT") pattern for consistency
- [Phase 06]: WebApi and BlazorApp include launchSettings.json with Portless profile (localhost:0) for easy testing
- [Phase 06]: WorkerService demonstrates PORT variable for logging (non-HTTP workload example)
- [Phase 06]: ConsoleApp demonstrates PORT variable for simple display scenarios
- [Phase 06]: README.md provides comprehensive 306-line documentation with troubleshooting section
- [Phase 06]: Installation scripts follow Microsoft dotnet-install pattern with automatic shell detection and persistent PATH configuration
- [Phase 06]: Documentation structured with progressive learning path: migration → new project → microservices → testing
- [Phase 06]: Integration guides separated by configuration method (launchSettings, appsettings, Kestrel) for code vs configuration flexibility
- [Phase quick]: GitHub Actions workflow configured for automated releases with cross-platform Native AOT binaries triggered by version tags (v*.*.*)

### Roadmap Evolution

- Phase 8 added: Integration tests automation for dotnet tool
- Phase 6 added: .NET tool packaging with examples and documentation
- Phase 7 added: Production readiness and polish

### Pending Todos

- Phase 8 complete - ready for Phase 6 (tool packaging) or Phase 7 (production polish)
- Consider CI/CD automation for automated test execution
- macOS validation for cross-platform testing

### Blockers/Concerns

None yet.

### Quick Tasks Completed

| # | Description | Date | Commit | Directory |
|---|-------------|------|--------|-----------|
| 1 | Configurar GitHub Actions para generar artefactos de release descargables | 2026-02-21 | 03f7db4 | [1-configurar-github-actions-para-generar-a](./quick/1-configurar-github-actions-para-generar-a/) |
| 2 | Establecer flujo de trabajo git con rama development | 2026-02-21 | 2cbdb33 | [2-establecer-flujo-de-trabajo-git-con-rama](./quick/2-establecer-flujo-de-trabajo-git-con-rama/) |

## Session Continuity

Last session: 2026-02-21 (Phase 06 complete)
Stopped at: Phase 06 (.NET Integration) complete - all 3 plans executed (tool packaging, examples, documentation)
Resume file: None
