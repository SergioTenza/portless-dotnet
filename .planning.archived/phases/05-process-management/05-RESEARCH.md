# Phase 5: Process Management - Research

**Researched:** 2026-02-21
**Domain:** .NET Process Management, Signal Handling, Background Services
**Confidence:** HIGH

## Summary

Phase 5 focuses on implementing robust process lifecycle management for spawned applications in Portless.NET. The system must spawn background processes with PORT variable injection, track process IDs (PIDs), monitor process health via polling, and perform coordinated cleanup of routes and ports when processes terminate. Based on research of .NET 10 process APIs, cross-platform signal handling, and BackgroundService patterns, the implementation requires careful attention to detached execution semantics, PID recycling detection, and graceful shutdown coordination.

**Primary recommendation:** Use System.Diagnostics.Process with UseShellExecute=false for environment variable injection, implement polling-based health checks via BackgroundService (5-second intervals), detect PID recycling via StartTime comparison, and forward only SIGTERM for graceful shutdown with 10-second timeout before forced termination.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **Process execution mode**: Direct execution (exec) without shell wrapper for performance; stdout/stderr inherited from CLI (visible in real-time); merged stdout/stderr stream; working directory is current directory of `portless run` command
- **Process tracking & lifecycle**: Polling every 5 seconds for process liveness; metadata stored per process (PID + hostname + port + command + start time); zombie process reaping with detection and cleanup
- **Signal forwarding**: Only SIGTERM forwarded (less complex, assumes graceful shutdown); 10-second timeout after SIGTERM before marking as non-responsive; Ctrl+C in CLI forwards SIGTERM to app; proxy stop with active processes prompts user to decide termination
- **Cleanup & error handling**: Route cleanup on next polling cycle (not immediate for fewer proxy calls); crashed processes (exit != 0) cleaned up same as normal exit; proxy restart re-attaches existing processes by reading routes.json; orphaned processes grace period of 5 minutes before cleanup

### Claude's Discretion
- Implementation exact mechanism for polling (timer, thread, background service)
- Handling of race conditions between cleanup and proxy restart
- Exact format for metadata storage in routes.json
- Implementation of zombie reaping cross-platform

### Deferred Ideas (OUT OF SCOPE)
None - discussion stayed within phase scope.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| **PROC-01** | Sistema spawnea comando con variable PORT | Process.Start with ProcessStartInfo.Environment for PORT injection; UseShellExecute=false required for environment variables |
| **PROC-02** | Sistema trackea PID de proceso | Process.Id property provides PID; store in RouteInfo; polling via Process.GetProcessById() + HasExited validation |
| **PROC-03** | Sistema limpia ruta cuando proceso termina | BackgroundService with 5-second polling; PID recycling detection via StartTime comparison; coordinated port+route cleanup |
| **PROC-04** | Sistema forwarda signals (SIGTERM, SIGINT) | SIGTERM only per context; cross-platform signal handling via ConsoleLifetime or PosixSignalRegistration; 10-second graceful timeout |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| **System.Diagnostics.Process** | .NET 10 | Process spawning and management | Official .NET API for process control, cross-platform support |
| **Microsoft.Extensions.Hosting.BackgroundService** | 10.0 | Polling-based health monitoring | Standard pattern for long-running background tasks in .NET |
| **Microsoft.Extensions.Hosting.Abstractions** | 10.0 | IHostApplicationLifetime for shutdown coordination | Provides application lifecycle events for graceful shutdown |
| **System.Threading.Timer** | .NET 10 | Alternative polling mechanism | Built-in timer support, lighter weight than BackgroundService for simple intervals |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| **System.Runtime.InteropServices** | .NET 10 | Platform-specific signal handling (PosixSignalRegistration) | When implementing cross-platform signal forwarding beyond ConsoleLifetime |
| **Microsoft.Extensions.Logging** | 10.0 | Diagnostic logging for process lifecycle | All implementations should include ILogger for observability |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| **BackgroundService** | **System.Threading.Timer** | Timer is simpler but BackgroundService provides better DI integration and lifecycle management |
| **Process.Start()** | **SafeProcessHandle.StartDetached()** (proposed) | StartDetached doesn't exist in .NET 10 yet; Process.Start with proper configuration achieves same goals |
| **Polling** | **Process.Exited event** | Event-driven seems better but requires EnableRaisingEvents and doesn't work reliably for detached processes; polling is more robust |

**Installation:**
No additional packages needed - all are part of .NET 10 baseline.

## Architecture Patterns

### Recommended Project Structure
```
Portless.Core/
├── Services/
│   ├── IProcessManager.cs          # Process spawning and tracking interface
│   ├── ProcessManager.cs           # Implementation with Process.Start
│   ├── ProcessHealthMonitor.cs     # BackgroundService for polling
│   └── ProcessMetadata.cs          # Enriched RouteInfo with process details
Portless.Cli/
├── Commands/
│   └── RunCommand/
│       ├── RunCommand.cs           # Enhanced with process lifecycle
│       └── SignalHandler.cs        # Cross-platform signal forwarding
```

### Pattern 1: Process Spawning with Environment Injection
**What:** Spawn background processes with PORT variable and proper detached execution
**When to use:** When executing user commands via `portless run <name> <command>`
**Example:**
```csharp
// Source: Based on System.Diagnostics.Process API and CONTEXT.md decisions
public Process StartManagedProcess(string command, string args, int port, string workingDirectory)
{
    var startInfo = new ProcessStartInfo
    {
        FileName = command,
        Arguments = args,
        UseShellExecute = false,  // Required for environment injection
        RedirectStandardOutput = true,  // Inherit for real-time visibility
        RedirectStandardError = true,   // Merged stream
        CreateNoWindow = true,          // Background execution
        WorkingDirectory = workingDirectory
    };

    // Inject PORT variable (preserves existing environment)
    startInfo.Environment["PORT"] = port.ToString();

    var process = Process.Start(startInfo);
    if (process == null)
        throw new InvalidOperationException("Failed to start process");

    return process;
}
```

### Pattern 2: Polling-Based Health Monitoring
**What:** BackgroundService that polls process health every 5 seconds
**When to use:** Continuous monitoring of spawned processes for cleanup coordination
**Example:**
```csharp
// Source: Microsoft.Extensions.Hosting.BackgroundService pattern
public class ProcessHealthMonitor : BackgroundService
{
    private readonly IRouteStore _routeStore;
    private readonly IPortAllocator _portAllocator;
    private readonly ILogger<ProcessHealthMonitor> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_pollingInterval, stoppingToken);
                await CheckProcessHealthAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during process health check");
            }
        }
    }

    private async Task CheckProcessHealthAsync(CancellationToken cancellationToken)
    {
        var routes = await _routeStore.LoadRoutesAsync(cancellationToken);
        var deadRoutes = routes.Where(r => !IsProcessAlive(r)).ToArray();

        if (deadRoutes.Length > 0)
        {
            // Coordinated cleanup: release ports + remove routes
            foreach (var route in deadRoutes)
            {
                await _portAllocator.ReleasePortAsync(route.Port);
            }

            var aliveRoutes = routes.Except(deadRoutes).ToArray();
            await _routeStore.SaveRoutesAsync(aliveRoutes, cancellationToken);
        }
    }

    private bool IsProcessAlive(RouteInfo route)
    {
        try
        {
            var process = Process.GetProcessById(route.Pid);

            // Check if process has exited
            if (process.HasExited)
                return false;

            // PID recycling detection via StartTime comparison
            if (process.StartTime > route.CreatedAt + TimeSpan.FromSeconds(1))
                return false; // PID was recycled

            route.LastSeen = DateTime.UtcNow;
            return true;
        }
        catch (ArgumentException)
        {
            // PID doesn't exist
            return false;
        }
    }
}
```

### Pattern 3: Graceful Shutdown with Signal Forwarding
**What:** Forward SIGTERM to child processes on Ctrl+C with timeout
**When to use:** CLI receives shutdown signal and needs to notify spawned processes
**Example:**
```csharp
// Source: Microsoft.Extensions.Hosting.IHostApplicationLifetime pattern
public class RunCommand : AsyncCommand<RunSettings>
{
    private readonly IHostApplicationLifetime _lifetime;
    private Process _childProcess;

    public override async Task<int> ExecuteAsync(CommandContext context, RunSettings settings, CancellationToken cancellationToken)
    {
        // Register shutdown handler
        _lifetime.ApplicationStopping.Register(OnApplicationStopping);

        // Start process...
        _childProcess = StartManagedProcess(/* ... */);

        // Wait for completion or cancellation
        await WaitForProcessAsync(_childProcess, cancellationToken);

        return 0;
    }

    private void OnApplicationStopping()
    {
        if (_childProcess != null && !_childProcess.HasExited)
        {
            // Forward SIGTERM (graceful shutdown)
            _childProcess.CloseMainWindow(); // GUI-friendly
            if (!_childProcess.WaitForExit(10000)) // 10-second timeout
            {
                _childProcess.Kill(); // Force terminate
            }
        }
    }
}
```

### Anti-Patterns to Avoid
- **Using Shell Execute for Environment Variables**: UseShellExecute=true doesn't support environment variable injection. Must set UseShellExecute=false.
- **Polling Too Frequently**: Intervals < 1 second waste CPU cycles. 5 seconds balances responsiveness and resource usage per context decisions.
- **Ignoring PID Recycling**: Process.GetProcessById() can return a different process if PID was reused. Always validate StartTime.
- **Synchronous Process.WaitForever**: Don't block waiting for process to exit in CLI. Use async/await with cancellation tokens.
- **Not Handling Orphaned Processes**: Proxy restart may find processes without routes.json entries. Implement grace period (5 minutes per context).

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| **Background task scheduling** | Custom Timer/Thread management | **BackgroundService** (Microsoft.Extensions.Hosting) | Provides DI integration, lifecycle management, proper cancellation token handling |
| **Process spawning** | P/Invoke to CreateProcess/fork+exec | **System.Diagnostics.Process.Start()** | Cross-platform abstraction, handles Windows/Unix differences, environment variable injection |
| **Signal handling** | Platform-specific signal handlers | **ConsoleLifetime** (UseConsoleLifetime) | Built-in Ctrl+C/SIGTERM handling, works across platforms, integrates with IHostApplicationLifetime |
| **Polling delays** | Thread.Sleep() loops | **Task.Delay() with CancellationToken** | Async-aware, respects cancellation, more efficient thread usage |
| **PID validation** | Cache PID lookups | **Process.GetProcessById() + StartTime comparison** each poll | Handles PID recycling correctly, process may be replaced between checks |

**Key insight:** .NET 10 provides robust abstractions for process management. Building custom solutions for spawning, monitoring, or signal handling introduces cross-platform bugs and misses edge cases like PID recycling.

## Common Pitfalls

### Pitfall 1: Process Not Actually Detached
**What goes wrong:** Process starts but terminates when CLI exits, or output isn't visible
**Why it happens:** Incorrect ProcessStartInfo configuration (UseShellExecute, RedirectStandardOutput, CreateNoWindow)
**How to avoid:**
```csharp
// CORRECT configuration for detached background process with visible output
var startInfo = new ProcessStartInfo
{
    UseShellExecute = false,        // Required for env injection
    RedirectStandardOutput = false,  // Inherit stdout (visible)
    RedirectStandardError = false,   // Inherit stderr (visible)
    CreateNoWindow = true           // Background but not hidden
};
```
**Warning signs:** Process disappears when CLI exits; no output in terminal; "process exited" messages immediately after start

### Pitfall 2: PID Recycling False Positives
**What goes wrong:** System incorrectly marks a live process as dead because its PID was reused
**Why it happens:** Process.GetProcessById() returns a different process than the original if OS recycled the PID
**How to avoid:** Always compare Process.StartTime with RouteInfo.CreatedAt:
```csharp
if (process.StartTime > route.CreatedAt + TimeSpan.FromSeconds(1))
{
    // PID was recycled, this is a different process
    return false;
}
```
**Warning signs:** Routes disappearing unexpectedly; "process dead" logs for running applications; ports released while app still running

### Pitfall 3: Zombie Process Accumulation
**What goes wrong:** Defunct processes accumulate, consuming PIDs and system resources
**Why it happens:** Parent process doesn't reap child exit status; on Unix, child becomes zombie when parent doesn't wait()
**How to avoid:** Ensure proper process disposal:
```csharp
using (var process = Process.Start(startInfo))
{
    // Process will be disposed automatically
}
// Or explicitly call process.Close() when done tracking
```
**Warning signs:** High zombie process count (`ps aux | grep defunct`); PID exhaustion warnings; system slowdown

### Pitfall 4: Race Condition During Cleanup
**What goes wrong:** Routes cleaned up while proxy restart is reading them, or vice versa
**Why it happens:** No synchronization between RouteCleanupService polling and proxy startup reading routes.json
**How to avoid:** Use file locking (already implemented in RouteStore) and atomic writes:
```csharp
// Coordinated cleanup with proper locking
using (var mutex = new Mutex(false, "Portless.Routes.Lock"))
{
    if (mutex.WaitOne(TimeSpan.FromSeconds(5)))
    {
        var routes = await _routeStore.LoadRoutesAsync();
        // Modify routes...
        await _routeStore.SaveRoutesAsync(updatedRoutes);
    }
}
```
**Warning signs:** File access exceptions during cleanup; lost routes; inconsistent state between proxy and CLI

### Pitfall 5: Signal Handling Not Cross-Platform
**What goes wrong:** Signal forwarding works on Windows but fails on Unix, or vice versa
**Why it happens:** Platform-specific signal APIs (Windows console events vs Unix signals)
**How to avoid:** Use ConsoleLifetime for built-in handling or PosixSignalRegistration for custom signals:
```csharp
// Cross-platform graceful shutdown
builder.UseConsoleLifetime(); // Handles Ctrl+C and SIGTERM automatically

// Or custom signal handling (.NET 10+)
if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
{
    PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
    {
        // Handle SIGTERM
        Environment.Exit(0);
    });
}
```
**Warning signs:** Signals ignored on one platform; different behavior between Windows and Unix; test failures

## Code Examples

Verified patterns from official sources:

### Process Spawning with Environment Variables
```csharp
// Source: https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.environment
var startInfo = new ProcessStartInfo
{
    FileName = "dotnet",
    Arguments = "run --project MyApi.csproj",
    UseShellExecute = false  // Required for Environment to work
};

// Environment variables are inherited from parent process
startInfo.Environment["PORT"] = "4000";
startInfo.Environment["ASPNETCORE_URLS"] = "http://0.0.0.0:${PORT}";

var process = Process.Start(startInfo);
```

### BackgroundService Polling Pattern
```csharp
// Source: https://learn.microsoft.com/en-us/dotnet/core/extensions/hosted-services
public class TimedBackgroundService : BackgroundService
{
    private readonly ILogger<TimedBackgroundService> _logger;

    public TimedBackgroundService(ILogger<TimedBackgroundService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Timed Background Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Do work here
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        _logger.LogInformation("Timed Background Service is stopping.");
    }
}
```

### PID Recycling Detection
```csharp
// Source: https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.starttime
private static bool IsSameProcess(RouteInfo route)
{
    try
    {
        var process = Process.GetProcessById(route.Pid);

        // If process started AFTER route creation, PID was recycled
        // Add 1-second buffer for clock skew
        return process.StartTime <= route.CreatedAt + TimeSpan.FromSeconds(1);
    }
    catch (ArgumentException)
    {
        return false; // Process doesn't exist
    }
}
```

### Graceful Shutdown with Timeout
```csharp
// Source: https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.kill
public static bool TryGracefulShutdown(Process process, int timeoutMs = 10000)
{
    // Try graceful shutdown first
    process.CloseMainWindow(); // Sends WM_CLOSE to GUI apps

    // Wait for process to exit gracefully
    if (process.WaitForExit(timeoutMs))
    {
        return true; // Process exited gracefully
    }

    // Force kill if timeout expired
    process.Kill(entireProcessTree: true);
    return false;
}
```

### Cross-Platform Signal Handling
```csharp
// Source: https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/10.0/sigterm-signal-handler
public static void SetupSignalHandlers()
{
    // .NET 10 no longer provides default SIGTERM handling
    // Use ConsoleLifetime for automatic handling:
    // builder.UseConsoleLifetime();

    // Or manual registration:
    if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
    {
        PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
        {
            Console.WriteLine("SIGTERM received, shutting down...");
            Environment.Exit(0);
        });
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| **Process.Start() with shell** | **Direct Process.Start() with UseShellExecute=false** | .NET Core 1.0+ | Better performance, cross-platform consistency, environment variable support |
| **Thread-based polling** | **BackgroundService with Task.Delay** | .NET Core 3.0 | Async-aware, proper cancellation, DI integration |
| **SIGTERM auto-handled by runtime** | **Must register SIGTERM handler explicitly** | .NET 10 Preview 5 | Breaking change - need ConsoleLifetime or PosixSignalRegistration |
| **Process.Exited event for monitoring** | **Polling with GetProcessById()** | Stable pattern | Events don't work reliably for detached processes; polling is more robust |
| **Manual PID validation** | **StartTime comparison for recycling detection** | Established pattern | Prevents false positives when OS reuses PIDs |

**Deprecated/outdated:**
- **UseShellExecute=true for environment variables**: Never worked; must use false
- **Relying on .NET Framework's default SIGTERM handling**: Removed in .NET 10, must explicitly register
- **Process.Kill() without graceful shutdown attempt**: Consider CloseMainWindow() first for GUI apps
- **Synchronous Process.WaitForExit() in async code**: Use WaitForExitAsync() or await with cancellation

## Open Questions

1. **Polling Implementation Choice**
   - What we know: BackgroundService vs System.Threading.Timer both viable; context allows discretion
   - What's unclear: Which provides better cancellation token integration for the proxy restart scenario
   - Recommendation: **BackgroundService** - better DI integration, more common in .NET ecosystem, easier to test

2. **Orphaned Process Grace Period Implementation**
   - What we know: 5-minute grace period required; need to detect processes without routes.json entries
   - What's unclear: Where to store orphan timestamps - in routes.json or separate state file?
   - Recommendation: **Add optional OrphanedAt field to RouteInfo** - keeps metadata in one place, atomic updates

3. **Signal Forwarding Scope**
   - What we know: Only SIGTERM required per context; 10-second timeout before forced kill
   - What's unclear: Should we implement custom PosixSignalRegistration or rely on ConsoleLifetime?
   - Recommendation: **Start with ConsoleLifetime** - simpler, built-in, handles most cases; add custom registration only if needed

## Sources

### Primary (HIGH confidence)
- **System.Diagnostics.Process API** - Official .NET documentation for process spawning and management
- **Microsoft.Extensions.Hosting.BackgroundService** - Official pattern for background tasks in .NET
- **Microsoft.Extensions.Hosting.Abstractions** - IHostApplicationLifetime for shutdown coordination
- **.NET 10 SIGTERM Breaking Change** - Official compatibility documentation for signal handling changes

### Secondary (MEDIUM confidence)
- **C# Process Class Comprehensive Guide** (2026-02-12) - Verified process usage patterns and common properties
- **Cross-Platform Process Execution** (2025-12-09) - Platform differences in detached process execution
- **BackgroundService Implementation Patterns** (2026-01-09) - Real-world examples and best practices

### Tertiary (LOW confidence)
- **GitHub Issue: Start Detached Process** (2026-02-12) - Proposed SafeProcessHandle.StartDetached API (not yet available)
- **Process.StartInfo.UseShellExecute Documentation** - Verified but needs cross-platform testing

## Metadata

**Confidence breakdown:**
- Standard stack: **HIGH** - All APIs are part of .NET 10 baseline, well-documented
- Architecture: **HIGH** - BackgroundService and Process.Start are established patterns; PID recycling detection is proven
- Pitfalls: **HIGH** - All pitfalls documented in official sources or established .NET practices
- Signal handling: **MEDIUM** - .NET 10 SIGTERM changes verified, but cross-platform testing needed

**Research date:** 2026-02-21
**Valid until:** 2026-03-23 (30 days - stable .NET APIs, no expected changes)
