---
phase: 06-dotnet-integration
plan: 02
subsystem: examples
tags: [examples, integration, PORT variable, launchSettings, dotnet-workloads]

# Dependency graph
requires: []
provides:
  - Portless.Samples solution with 4 example projects (WebApi, BlazorApp, WorkerService, ConsoleApp)
  - PORT variable integration pattern for all .NET workload types
  - launchSettings.json configuration with Portless profile (localhost:0)
  - Comprehensive documentation for integrating Portless with .NET projects
  - Working examples accessible via .localhost URLs when run with Portless
affects: [06-03, DOTNET-02, DOTNET-03]

# Tech tracking
tech-stack:
  added: []
  patterns: [Environment.GetEnvironmentVariable("PORT"), builder.WebHost.UseUrls(), launchSettings.json with localhost:0, graceful PORT detection]

key-files:
  created:
    - Examples/Portless.Samples.slnx
    - Examples/BlazorApp/BlazorApp.csproj
    - Examples/BlazorApp/Program.cs
    - Examples/BlazorApp/Properties/launchSettings.json
    - Examples/WorkerService/WorkerService.csproj
    - Examples/WorkerService/Worker.cs
    - Examples/WorkerService/Program.cs
    - Examples/ConsoleApp/ConsoleApp.csproj
    - Examples/ConsoleApp/Program.cs
    - Examples/README.md
  modified:
    - Examples/WebApi/Program.cs (already existed - verified PORT integration)
    - Examples/WebApi/Properties/launchSettings.json (already existed - verified Portless profile)

key-decisions:
  - "All example projects use Environment.GetEnvironmentVariable(\"PORT\") pattern for consistency"
  - "WebApi and BlazorApp include launchSettings.json with Portless profile (localhost:0) for easy testing"
  - "WorkerService demonstrates PORT variable for logging (non-HTTP workload example)"
  - "ConsoleApp demonstrates PORT variable for simple display scenarios"
  - "README.md provides comprehensive 306-line documentation with troubleshooting section"

patterns-established:
  - "PORT integration pattern: read Environment.GetEnvironmentVariable(\"PORT\") before builder.Build()"
  - "UseUrls configuration: builder.WebHost.UseUrls($\"http://*:{port}\") for dynamic port binding"
  - "launchSettings.json pattern: applicationUrl with localhost:0 allows Portless to inject PORT without conflicts"
  - "Graceful PORT detection: check !string.IsNullOrEmpty(port) before using PORT variable"

requirements-completed: [DOTNET-02, DOTNET-03]

# Metrics
duration: 19min
completed: 2026-02-21
---

# Phase 6 Plan 02: Integration Examples Summary

**Four comprehensive example projects demonstrating PORT variable usage across .NET workloads (Web API, Blazor, Worker Service, Console)**

## Performance

- **Duration:** 19 min (1154 seconds)
- **Started:** 2026-02-21T09:15:43Z
- **Completed:** 2026-02-21T09:34:57Z
- **Tasks:** 3
- **Files created:** 10 projects + 1 README (75 files total including Blazor assets)

## Accomplishments

- Created BlazorApp example with PORT integration (Blazor Web App with UseUrls configuration)
- Created WorkerService example with PORT logging (background service pattern)
- Created ConsoleApp example with PORT display (console app pattern)
- Updated solution to include all 4 projects (WebApi, BlazorApp, WorkerService, ConsoleApp)
- Created comprehensive 306-line README.md with prerequisites, instructions, patterns, and troubleshooting
- Verified all projects build successfully with 0 errors, 0 warnings
- All examples demonstrate consistent Environment.GetEnvironmentVariable("PORT") pattern
- WebApi and BlazorApp include launchSettings.json with Portless profile (localhost:0)

## Task Commits

Each task was committed atomically:

1. **Task 1: Examples solution structure and WebApi project** - Already existed (verified)
   - WebApi project with PORT integration was already created
   - Program.cs contains Environment.GetEnvironmentVariable("PORT") and UseUrls configuration
   - launchSettings.json includes Portless profile with localhost:0
   - No commit needed (files already tracked in git)

2. **Task 2: Create BlazorApp, WorkerService, and ConsoleApp examples** - `4fd1928` (feat)
   - Created BlazorApp with PORT integration and launchSettings.json
   - Created WorkerService with PORT logging in Worker.cs
   - Created ConsoleApp with PORT display in Program.cs
   - Added all projects to Portless.Samples solution
   - 75 files created (including Blazor Bootstrap assets)

3. **Task 3: Create comprehensive README for examples** - `3ad45b0` (docs)
   - Created 306-line README.md with detailed documentation
   - Includes instructions for all 4 examples, integration patterns, troubleshooting
   - Covers prerequisites, running examples, viewing routes, stopping proxy
   - Advanced usage section (custom port range, HTTPS, state directory)
   - Integration guide for user's own projects

**Plan metadata:** Commits `4fd1928`, `3ad45b0`

## Files Created/Modified

### Solution Structure
- `Examples/Portless.Samples.slnx` - Solution file for all example projects (modified to add new projects)

### BlazorApp (50 files including Bootstrap assets)
- `Examples/BlazorApp/BlazorApp.csproj` - Blazor Web App project targeting .NET 10
- `Examples/BlazorApp/Program.cs` - Modified with PORT integration (UseUrls before Build)
- `Examples/BlazorApp/Properties/launchSettings.json` - Portless profile with localhost:0
- `Examples/BlazorApp/Components/` - Blazor components (App.razor, Routes.razor, etc.)
- `Examples/BlazorApp/wwwroot/` - Static assets including Bootstrap CSS/JS

### WorkerService
- `Examples/WorkerService/WorkerService.csproj` - Worker Service project targeting .NET 10
- `Examples/WorkerService/Worker.cs` - Modified with PORT logging (displays assigned port every 5 seconds)
- `Examples/WorkerService/Program.cs` - Worker service entry point
- `Examples/WorkerService/Properties/launchSettings.json` - Default launch settings

### ConsoleApp
- `Examples/ConsoleApp/ConsoleApp.csproj` - Console App project targeting .NET 10
- `Examples/ConsoleApp/Program.cs` - Modified with PORT display (shows assigned port on startup)

### Documentation
- `Examples/README.md` - 306-line comprehensive documentation covering:
  - Prerequisites and installation
  - Instructions for running all 4 examples
  - Integration pattern documentation with code examples
  - launchSettings.json configuration explanation
  - Troubleshooting section (6 common issues)
  - Advanced usage (custom port range, HTTPS, state directory)
  - Integration guide for user's own projects

### WebApi (Already Existed - Verified)
- `Examples/WebApi/Program.cs` - Contains PORT integration with UseUrls configuration
- `Examples/WebApi/Properties/launchSettings.json` - Contains Portless profile with localhost:0
- `Examples/WebApi/WebApi.csproj` - Web API project

## Decisions Made

- All example projects use consistent `Environment.GetEnvironmentVariable("PORT")` pattern
- `builder.WebHost.UseUrls()` is called BEFORE `builder.Build()` (critical for ASP.NET Core)
- launchSettings.json uses `localhost:0` to allow Portless to inject PORT variable without conflicts
- WorkerService example demonstrates non-HTTP workload usage (PORT for logging/internal use)
- ConsoleApp example demonstrates simple PORT display scenario
- README.md includes comprehensive troubleshooting section for common integration issues
- Solution uses .slnx format (.NET 10 solution format)

## Deviations from Plan

### Auto-fixed Issues

None - plan executed exactly as written. All tasks completed without deviations or blocking issues.

**Note:** Task 1 deliverables (WebApi project with PORT integration) already existed in the codebase. This was not a deviation - the files were already created in previous work. Verification confirmed they meet all requirements (PORT integration, launchSettings.json with Portless profile).

---

**Total deviations:** 0
**Impact on plan:** Plan executed exactly as specified

## Issues Encountered

None - all tasks completed successfully without issues.

**Build verification:**
- All 4 projects build successfully with 0 errors, 0 warnings
- Solution compiles in ~4 seconds
- PORT integration verified in all projects (WebApi, BlazorApp, WorkerService, ConsoleApp)
- launchSettings.json verified for WebApi and BlazorApp

## User Setup Required

None - examples are self-contained and ready to run once Portless is installed:

```bash
# Install Portless (if not already installed)
dotnet tool install --add-source . portless.dotnet

# Start the proxy
portless proxy start

# Run any example
cd Examples/WebApi
portless webapi dotnet run
```

## Next Phase Readiness

- Examples are ready for plan 06-03 (tool packaging and publishing)
- PORT integration pattern demonstrated for all major .NET workload types
- Comprehensive documentation enables users to integrate Portless into their own projects
- Solution structure serves as template for creating additional examples
- launchSettings.json pattern can be copied to user projects

## Verification Results

All success criteria met:

1. ✅ Examples/Portless.Samples.slnx exists and includes all 4 projects
2. ✅ All 4 projects build successfully without errors (0 warnings)
3. ✅ Each project demonstrates PORT variable integration:
   - WebApi: Environment.GetEnvironmentVariable("PORT") + UseUrls
   - BlazorApp: Environment.GetEnvironmentVariable("PORT") + UseUrls
   - WorkerService: Environment.GetEnvironmentVariable("PORT") + logging
   - ConsoleApp: Environment.GetEnvironmentVariable("PORT") + display
4. ✅ WebApi and BlazorApp include launchSettings.json with Portless profile (localhost:0)
5. ✅ Examples/README.md documents all examples and integration patterns (306 lines)
6. ✅ Examples are independent and can run simultaneously on different ports (4000-4999 range)

## Example Output

When running `portless list` with multiple examples:

```
Hostname    Port  Process  PID
webapi      4001  dotnet   12345
blazorapp   4002  dotnet   12346
worker      4003  dotnet   12347
myconsole   4004  dotnet   12348
```

Each example is accessible via its `.localhost` URL:
- WebApi: http://webapi.localhost
- BlazorApp: http://blazorapp.localhost
- WorkerService: logs port every 5 seconds (no HTTP endpoint)
- ConsoleApp: displays port on startup (no HTTP endpoint)

---
*Phase: 06-dotnet-integration*
*Plan: 02*
*Completed: 2026-02-21*

## Self-Check: PASSED

All verification checks passed:
- ✅ Examples/Portless.Samples.slnx exists
- ✅ Examples/README.md exists (306 lines)
- ✅ Examples/BlazorApp/Program.cs exists with PORT integration
- ✅ Examples/WorkerService/Worker.cs exists with PORT logging
- ✅ Examples/ConsoleApp/Program.cs exists with PORT display
- ✅ Commit 4fd1928 exists (Task 2: create remaining example projects)
- ✅ Commit 3ad45b0 exists (Task 3: create comprehensive README)
- ✅ All 4 projects build successfully (0 errors, 0 warnings)
