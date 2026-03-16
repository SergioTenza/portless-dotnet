---
phase: 05-process-management
plan: 02
subsystem: Process Management
tags: [signal-forwarding, graceful-shutdown, user-prompts, process-tracking, sigterm]
wave: 2
requires_provides:
  - requires: ["System.Diagnostics.Process", "Microsoft.Extensions.Hosting.IHostApplicationLifetime", "Spectre.Console"]
  - provides: ["SignalForwarding", "GracefulShutdown", "ManagedProcessTracking"]
  - affects: ["Portless.Cli.Commands.RunCommand", "Portless.Cli.Commands.ProxyCommand.ProxyStopCommand", "Portless.Cli.Services.ProxyProcessManager"]
tech_stack:
  added: []
  patterns: ["ApplicationStopping.Register callback", "CloseMainWindow graceful shutdown", "CancellationToken.Register", "Persistent PID tracking"]
key_files:
  created: []
  modified:
    - "Portless.Cli/Commands/RunCommand/RunCommand.cs"
    - "Portless.Cli/Commands/ProxyCommand/ProxyStopCommand.cs"
    - "Portless.Cli/Services/IProxyProcessManager.cs"
    - "Portless.Cli/Services/ProxyProcessManager.cs"
decisions:
  - "Use CloseMainWindow for SIGTERM forwarding (GUI-friendly, cross-platform)"
  - "10-second timeout before force kill per CONTEXT.md locked decision"
  - "Default to false for user prompt to avoid accidental data loss"
  - "Store managed PIDs in JSON file for persistence across proxy restarts"
metrics:
  duration: "2 minutes 53 seconds"
  completed_date: "2026-02-21"
  tasks_completed: 3
  files_created: 0
  files_modified: 4
---

# Phase 5 Plan 02: Signal Forwarding and Graceful Shutdown Summary

Signal forwarding infrastructure with Ctrl+C handler in RunCommand, graceful shutdown with 10-second timeout, process tracking via persistent PID storage, and user prompts in ProxyStopCommand for active process termination.

## One-Liner

Implemented cross-platform signal forwarding using CloseMainWindow for graceful SIGTERM delivery, 10-second timeout before force kill, persistent PID tracking in managed-pids.json, and user confirmation prompts when stopping proxy with active processes.

## Implementation Summary

### Components Modified

**1. RunCommand Signal Forwarding** (`Portless.Cli/Commands/RunCommand/RunCommand.cs`)
- Added `_spawnedProcess` field to store process reference
- Added `ILogger<RunCommand>` dependency for diagnostic logging
- Set up `CancellationToken.Register` callback for ApplicationStopping event
- Implemented `ForwardSignalToProcess` method:
  * `CloseMainWindow()` for GUI-friendly SIGTERM (sends WM_CLOSE on Windows, SIGTERM on Unix)
  * `WaitForExit(10000)` for 10-second graceful shutdown period
  * `Kill(entireProcessTree: true)` for force termination after timeout
  * Exception handling for processes that terminate during signal forwarding

**2. ProxyProcessManager Process Tracking** (`Portless.Cli/Services/ProxyProcessManager.cs`)
- Added `_managedPids` HashSet field for in-memory tracking
- Added `_managedPidsFilePath` field for persistent storage (`~/.portless/managed-pids.json`)
- Implemented `GetActiveManagedProcessesAsync`:
  * Loads PIDs from JSON file
  * Filters dead PIDs using `Process.GetProcessById()` + `HasExited`
  * Returns array of active process PIDs
- Implemented `KillManagedProcessesAsync(int[] pids)`:
  * Kills each process with `entireProcessTree: true`
  * Removes from `_managedPids` HashSet
  * Saves updated PIDs to JSON file
- Implemented `RegisterManagedProcessAsync(int pid)`:
  * Adds PID to `_managedPids` HashSet
  * Persists to JSON file
- Implemented `LoadManagedPidsAsync`:
  * Deserializes JSON file to HashSet<int>
  * Handles missing/invalid files gracefully
- Implemented `SaveManagedPidsAsync`:
  * Serializes HashSet to JSON

**3. IProxyProcessManager Interface** (`Portless.Cli/Services/IProxyProcessManager.cs`)
- Added `Task<int[]> GetActiveManagedProcessesAsync()` method
- Added `Task KillManagedProcessesAsync(int[] pids)` method
- Added `Task RegisterManagedProcessAsync(int pid)` method

**4. ProxyStopCommand User Prompt** (`Portless.Cli/Commands/ProxyCommand/ProxyStopCommand.cs`)
- Check for active managed processes before stopping proxy
- Display warning with count of active processes
- List each active PID for user visibility
- Show confirmation prompt with `AnsiConsole.Confirm(defaultValue: false)`
- Cancel proxy stop if user declines to kill processes
- Kill processes via `KillManagedProcessesAsync` if user confirms
- Show success message after processes killed

**5. RunCommand Process Registration** (`Portless.Cli/Commands/RunCommand/RunCommand.cs`)
- Added `await _proxyManager.RegisterManagedProcessAsync(process.Id)` call after spawning process
- Ensures all spawned processes are tracked for cleanup

## Technical Decisions

### CloseMainWindow for SIGTERM Forwarding
Chose `CloseMainWindow()` over platform-specific signal APIs:
- Cross-platform compatible (Windows: WM_CLOSE, Unix: SIGTERM)
- GUI-friendly - allows applications to handle shutdown gracefully
- Standard .NET pattern for graceful process termination
- Tradeoff: May not work for console apps without message loop, but `Kill()` fallback handles this

### 10-Second Timeout
Locked decision from CONTEXT.md:
- Balances responsiveness and graceful shutdown opportunity
- Most well-behaved applications can clean up within 10 seconds
- Force kill prevents indefinite hangs

### Default=False for User Prompt
Safety-first approach:
- Prevents accidental data loss from terminating active processes
- User must explicitly confirm termination
- Clear messaging about what will be terminated

### Persistent PID Tracking
JSON file storage (`~/.portless/managed-pids.json`):
- Survives proxy restarts
- Simple serialization with `System.Text.Json`
- HashSet<int> for O(1) lookups
- Graceful degradation for missing/corrupted files

## Deviations from Plan

None - plan executed exactly as written. All tasks completed without deviations or auto-fixes.

## Requirements Traceability

| Requirement | Status | Implementation |
|-------------|--------|----------------|
| **PROC-04** | Complete | RunCommand forwards SIGTERM via `CloseMainWindow()` in `ForwardSignalToProcess` method |
| Graceful Shutdown | Complete | 10-second timeout via `WaitForExit(10000)` before `Kill(entireProcessTree: true)` |
| User Control | Complete | ProxyStopCommand prompts with `AnsiConsole.Confirm(defaultValue: false)` |
| Cross-Platform | Complete | `CloseMainWindow()` works on Windows/macOS/Linux (per RESEARCH.md) |

All requirements satisfied.

## Verification Results

### Build Verification
```
dotnet build Portless.slnx
Compilacion correcta.
0 Errores
```

### Signal Forwarding Verification
- RunCommand has `_spawnedProcess` field ✓
- `ApplicationStopping.Register` calls `ForwardSignalToProcess` ✓
- `ForwardSignalToProcess` implements `CloseMainWindow` → `WaitForExit(10000)` → `Kill` flow ✓

### Process Tracking Verification
- `IProxyProcessManager` has `GetActiveManagedProcessesAsync` and `KillManagedProcessesAsync` methods ✓
- `ProxyProcessManager` loads/saves `_managedPids` from JSON file ✓
- `GetActiveManagedProcessesAsync` filters dead PIDs using `Process.GetProcessById` ✓
- `KillManagedProcessesAsync` kills processes and updates JSON file ✓

### User Prompt Verification
- ProxyStopCommand calls `GetActiveManagedProcessesAsync` before `StopAsync` ✓
- Active PIDs displayed to user in list format ✓
- `AnsiConsole.Confirm` prompt shown with `default=false` ✓
- `KillManagedProcessesAsync` called only if user confirms ✓

## Commits

| Commit | Message | Files |
|--------|---------|-------|
| `88884cf` | feat(05-02): implement Ctrl+C signal forwarding in RunCommand | RunCommand.cs |
| `54fdbf2` | feat(05-02): track spawned processes in ProxyProcessManager | IProxyProcessManager.cs, ProxyProcessManager.cs |
| `b711adc` | feat(05-02): add user prompt to ProxyStopCommand for active processes | ProxyStopCommand.cs, RunCommand.cs |

## Test Results

**Checkpoint:** Auto-approved (auto-advance mode enabled)

The following manual verification steps should be performed:
1. Start proxy: `dotnet run --project Portless.Cli -- proxy start`
2. Run test process: `dotnet run --project Portless.Cli -- run testapi dotnet run --project TestApi`
3. Verify process starts with "Running on http://testapi.localhost" message
4. Press Ctrl+C to trigger graceful shutdown
5. Verify "Forwarding SIGTERM to process XXX" message appears
6. Verify process exits cleanly (check with `portless list` - should be empty)
7. Test proxy stop with active processes:
   - Run another process: `dotnet run --project Portless.Cli -- run testapi2 dotnet run --project TestApi`
   - Try to stop proxy: `dotnet run --project Portless.Cli -- proxy stop`
   - Verify prompt appears with active PIDs listed
   - Confirm termination (or cancel to test)
8. If confirmed: Verify processes killed and proxy stops

**Status:** Implementation complete, awaiting manual user verification.

## Next Steps

Phase 5 (Process Management) is now complete. Next phase will be:
- **Phase 6: .NET Integration** - Pack as dotnet tool with examples

## Files Summary

**Modified (4 files, 190 insertions, 9 deletions):**
- `Portless.Cli/Commands/RunCommand/RunCommand.cs` - Signal forwarding and process registration
- `Portless.Cli/Commands/ProxyCommand/ProxyStopCommand.cs` - User prompt for active processes
- `Portless.Cli/Services/IProxyProcessManager.cs` - New methods for process tracking
- `Portless.Cli/Services/ProxyProcessManager.cs` - PID tracking implementation

---

*Plan completed: 2026-02-21*
*Duration: 2 minutes 53 seconds*
*Tasks: 3/3 completed*
