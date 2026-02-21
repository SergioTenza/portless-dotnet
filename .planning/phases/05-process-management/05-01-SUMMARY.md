---
phase: 05-process-management
plan: 01
subsystem: Process Management
tags: [process-spawning, health-monitoring, port-injection, pid-tracking, background-service]
wave: 1
requires_provides:
  - requires: ["System.Diagnostics.Process", "Microsoft.Extensions.Hosting.BackgroundService"]
  - provides: ["IProcessManager", "ProcessManager", "ProcessHealthMonitor"]
  - affects: ["Portless.Cli.Commands.RunCommand", "Portless.Core.Services.RouteCleanupService"]
tech_stack:
  added: []
  patterns: ["BackgroundService polling", "Process.Start with environment injection", "PID recycling detection"]
key_files:
  created:
    - "Portless.Core/Services/IProcessManager.cs"
    - "Portless.Core/Services/ProcessManager.cs"
    - "Portless.Core/Services/ProcessHealthMonitor.cs"
  modified:
    - "Portless.Core/Extensions/ServiceCollectionExtensions.cs"
    - "Portless.Cli/Commands/RunCommand/RunCommand.cs"
decisions:
  - "Use System.Diagnostics.Process.Start with UseShellExecute=false for PORT environment variable injection"
  - "Use BackgroundService pattern for process health monitoring with 5-second polling interval per CONTEXT.md"
  - "Detect PID recycling via StartTime comparison with 1-second buffer per RESEARCH.md"
  - "Coordinated cleanup releases ports AND removes routes atomically to prevent state inconsistency"
  - "Working directory defaults to Directory.GetCurrentDirectory() per CONTEXT.md decision"
metrics:
  duration: "3 minutes 56 seconds"
  completed_date: "2026-02-21"
  tasks_completed: 3
  files_created: 3
  files_modified: 2
---

# Phase 5 Plan 01: Process Spawning and Health Monitoring Summary

Process spawning with PORT injection using System.Diagnostics.Process.Start, PID tracking via RouteInfo.Pid, and background health monitoring with 5-second polling for automatic cleanup of terminated processes.

## One-Liner

Implemented process lifecycle management with PORT environment variable injection, BackgroundService-based health polling every 5 seconds, PID recycling detection, and coordinated cleanup of routes and ports for terminated processes.

## Implementation Summary

### Components Created

**1. IProcessManager Interface** (`Portless.Core/Services/IProcessManager.cs`)
- `StartManagedProcess(string command, string args, int port, string workingDirectory)` - Spawns process with PORT injection
- `GetProcessStatusAsync(int pid)` - Returns ProcessStatus with IsRunning, StartTime, ExitTime
- `ProcessStatus` record - Encapsulates process health information

**2. ProcessManager Implementation** (`Portless.Core/Services/ProcessManager.cs`)
- Uses `System.Diagnostics.Process.Start` with configured `ProcessStartInfo`
- `UseShellExecute = false` - Required for environment variable injection
- `RedirectStandardOutput = false` and `RedirectStandardError = false` - Inherits stdout/stderr for real-time visibility
- `CreateNoWindow = true` - Background execution
- Injects PORT via `startInfo.Environment["PORT"] = port.ToString()`
- Throws `InvalidOperationException` if Process.Start returns null

**3. ProcessHealthMonitor BackgroundService** (`Portless.Core/Services/ProcessHealthMonitor.cs`)
- Inherits from `Microsoft.Extensions.Hosting.BackgroundService`
- 5-second polling interval per CONTEXT.md locked decision
- `CheckProcessHealthAsync` - Loads routes, identifies dead processes, performs coordinated cleanup
- `IsProcessAlive` - Validates process via `Process.GetProcessById()`, `HasExited`, and PID recycling detection
- Coordinated cleanup: Releases ports AND removes routes atomically via `IRouteStore.SaveRoutesAsync`

### Integration Points

**DI Registration** (`Portless.Core/Extensions/ServiceCollectionExtensions.cs`)
- `IProcessManager` registered as singleton
- `ProcessHealthMonitor` registered as hosted service (automatic background execution)
- Both registered in `AddPortlessPersistence` method

**RunCommand Integration** (`Portless.Cli/Commands/RunCommand/RunCommand.cs`)
- Constructor injection of `IProcessManager`
- Replaced `Process.Start` logic with `_processManager.StartManagedProcess()`
- Parameters: `commandArgs[0]`, `string.Join(" ", commandArgs.Skip(1))`, `port`, `Directory.GetCurrentDirectory()`
- Maintains existing flow: port allocation, process spawning, route registration, persistence

## Technical Decisions

### BackgroundService Pattern
Chose BackgroundService over System.Threading.Timer for:
- Better DI integration (constructor injection of IRouteStore, IPortAllocator, ILogger)
- Proper lifecycle management (stoppingToken handling)
- Standard .NET pattern for long-running background tasks

### 5-Second Polling Interval
Locked decision from CONTEXT.md - balances responsiveness and resource usage:
- Fast enough to detect crashes quickly
- Slow enough to avoid excessive CPU usage
- Consistent with RouteCleanupService's 30-second interval for different concerns

### PID Recycling Detection
Implementation per RESEARCH.md Pitfall #2:
- Compare `process.StartTime` with `route.CreatedAt + TimeSpan.FromSeconds(1)`
- 1-second buffer accounts for clock skew
- Prevents false positives when OS reuses PIDs

### Coordinated Cleanup
Atomic cleanup sequence prevents state inconsistency:
1. Release ports for all dead routes
2. Save alive routes back to store
3. YARP reload triggered by file watcher (separate service)

### UseShellExecute=false
Required for environment variable injection per .NET documentation:
- `UseShellExecute=true` prevents Environment dictionary modifications
- Tradeoff: Cannot use shell features (pipes, redirects) - acceptable for our use case

## Deviations from Plan

None - plan executed exactly as written. All tasks completed without deviations or auto-fixes.

## Requirements Traceability

| Requirement | Status | Implementation |
|-------------|--------|----------------|
| **PROC-01** | Complete | `ProcessManager.StartManagedProcess` injects PORT via `ProcessStartInfo.Environment["PORT"]` |
| **PROC-02** | Complete | `Process.Id` stored in `RouteInfo.Pid`, tracked by `ProcessHealthMonitor.CheckProcessHealthAsync` |
| **PROC-03** | Complete | `ProcessHealthMonitor` polls every 5s, detects dead processes, removes routes via `IRouteStore.SaveRoutesAsync` |
| **PROC-04** | Deferred | Signal forwarding deferred to Plan 05-02 per phase structure |

All requirements satisfied except PROC-04 (signal forwarding), which is intentionally deferred to Plan 05-02 "Signal Forwarding and Graceful Shutdown".

## Verification Results

### Build Verification
```
dotnet build Portless.slnx
Compilación correcta.
0 Errores
```

### Integration Verification
- `IProcessManager` resolves in `RunCommand` constructor
- `ProcessHealthMonitor` registered as `AddHostedService`
- `RunCommand.ExecuteAsync` calls `_processManager.StartManagedProcess`

### Process Spawning Verification
- `ProcessManager.StartManagedProcess` uses `ProcessStartInfo` with:
  - `UseShellExecute = false` ✓
  - `RedirectStandardOutput = false` (inherits stdout) ✓
  - `RedirectStandardError = false` (inherits stderr) ✓
  - `CreateNoWindow = true` ✓
  - `Environment["PORT"]` injection ✓

### Health Monitoring Verification
- 5-second polling interval implemented ✓
- `IsProcessAlive` checks `HasExited` ✓
- PID recycling detection via `StartTime` comparison ✓
- Coordinated cleanup releases ports AND removes routes ✓

## Commits

| Commit | Message | Files |
|--------|---------|-------|
| `5ea2426` | feat(05-01): create IProcessManager interface and ProcessManager implementation | IProcessManager.cs, ProcessManager.cs |
| `3bf8347` | feat(05-01): create ProcessHealthMonitor BackgroundService for polling | ProcessHealthMonitor.cs |
| `01152ca` | feat(05-01): integrate ProcessManager and ProcessHealthMonitor with DI and RunCommand | ServiceCollectionExtensions.cs, RunCommand.cs |

## Next Steps

Plan 05-02 will implement signal forwarding (SIGTERM) and graceful shutdown coordination:
- Ctrl+C in CLI forwards SIGTERM to spawned process
- 10-second timeout before forced termination
- Proxy stop prompts user to decide on spawned process termination
- Integration with `IHostApplicationLifetime` for shutdown coordination

## Files Summary

**Created (3 files, 225 lines):**
- `Portless.Core/Services/IProcessManager.cs` - Process management interface
- `Portless.Core/Services/ProcessManager.cs` - Process spawning implementation
- `Portless.Core/Services/ProcessHealthMonitor.cs` - Background health monitoring service

**Modified (2 files, 96 insertions, 32 deletions):**
- `Portless.Core/Extensions/ServiceCollectionExtensions.cs` - DI registration
- `Portless.Cli/Commands/RunCommand/RunCommand.cs` - ProcessManager integration

---

*Plan completed: 2026-02-21*
*Duration: 3 minutes 56 seconds*
*Tasks: 3/3 completed*
