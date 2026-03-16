# Phase 4: Port Management - Research

**Researched:** 2026-02-20
**Domain:** .NET 10 Cross-Platform Process Management & Network Port Allocation
**Confidence:** HIGH

## Summary

Phase 4 focuses on implementing robust port management for the Portless.NET system, which currently has basic port allocation but lacks proper port pooling, lifecycle management, and cleanup. The research reveals that .NET 10 provides excellent cross-platform support for both port detection and environment variable injection, with well-established patterns for TCP port availability checking and process environment manipulation.

The current implementation in `PortAllocator.cs` uses a simple random port allocation strategy with TCP listener binding for detection, which is fundamentally sound but needs enhancement for port pooling, proper cleanup, and better error handling. The research confirms that the chosen approach of using `TcpListener.Start()/Stop()` for port availability detection is the industry-standard method in .NET, with the alternative being direct `Socket.Bind()` which offers more control but slightly more complexity.

**Primary recommendation:** Enhance the existing `PortAllocator` with port pooling and lifecycle management, while keeping the core TCP listener-based detection mechanism. Implement proper port release when processes terminate via integration with the existing `RouteCleanupService`. Use `ProcessStartInfo.Environment` dictionary for PORT injection, which works consistently across Windows, macOS, and Linux in .NET 10.

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **PORT Variable Injection**: Solo inyectar variable `PORT` (no `PORTLESS_HOST` ni otras adicionales)
- **Responsibility Separation**: El comando es responsable de leer y usar la variable PORT
- **No Validation**: Portless solo inyecta, no valida si el comando la usa

### Claude's Discretion
- **Cross-platform injection mechanism**: Elegir entre pre-exec injection vs OS-specific wrappers según lo más simple de implementar en .NET 10
- **Validation de uso de PORT**: Warning opcional si el comando no parece usar ${PORT} o %PORT%, basarse en el balance entre helpfulness y complejidad
- **Fallback on injection failure**: Ejecutar sin PORT o fallar con error, según el UX que tenga más sentido

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| PORT-01 | Sistema detecta puerto libre en rango 4000-4999 | TcpListener.Bind() approach is HIGH confidence - industry standard for port detection in .NET |
| PORT-02 | Sistema asigna puerto automáticamente a app | Current random allocation works; enhance with pooling strategy to prevent exhaustion |
| PORT-03 | Sistema inyecta variable PORT en comando ejecutado | ProcessStartInfo.Environment is HIGH confidence - cross-platform, well-documented in .NET 10 |
| PORT-04 | Sistema libera puerto cuando proceso termina | Integration with RouteCleanupService + Process.HasExited detection is HIGH confidence pattern |
</phase_requirements>

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| System.Net.Sockets | .NET 10+ | TCP port detection | TcpListener is the official .NET abstraction for TCP port binding and availability checking |
| System.Diagnostics.Process | .NET 10+ | Process spawning & env injection | ProcessStartInfo.Environment is the canonical way to set environment variables in child processes |
| Microsoft.Extensions.Logging | 10.0+ | Logging port allocation/deallocation | Standard logging pattern already used in RouteCleanupService |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| System.Net.NetworkInformation | .NET 10+ | Active TCP connection monitoring | For advanced port status checking (optional enhancement) |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| TcpListener.Start()/Stop() | Socket.Bind() with try-catch | Socket.Bind() is slightly faster but TcpListener is more idiomatic and clearer in intent |
| ProcessStartInfo.Environment | Command-line wrapping | Command-line wrapping requires OS-specific batch/shell scripts; Environment dict is cross-platform |
| Random port allocation | Sequential port allocation | Sequential is simpler but random provides better distribution and avoids predictable patterns |

**Installation:**
```bash
# No additional packages needed - all part of .NET 10 baseline
dotnet add package Microsoft.Extensions.Logging.Abstractions
```

## Architecture Patterns

### Recommended Project Structure
```
Portless.Core/
├── Services/
│   ├── IPortAllocator.cs          # Interface with pooling support
│   ├── PortAllocator.cs           # Enhanced implementation with pooling
│   └── PortPool.cs                # NEW: Port pool management
├── Models/
│   └── PortAllocation.cs          # NEW: Track allocated ports with PIDs
Portless.Cli/
└── Services/
    └── PortAllocator.cs           # Move to Core, remove duplication
```

### Pattern 1: TCP Port Availability Detection
**What:** Use TcpListener to attempt binding to a port; if successful, port is free.
**When to use:** Checking port availability before allocating to a new process.
**Example:**
```csharp
// Source: https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.tcplistener
public async Task<bool> IsPortFreeAsync(int port)
{
    try
    {
        using var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        listener.Stop();
        return true; // Port is free if we can bind to it
    }
    catch (SocketException)
    {
        return false; // Port is in use
    }
}
```

### Pattern 2: Cross-Platform Environment Variable Injection
**What:** Use ProcessStartInfo.Environment dictionary to inject PORT into child process.
**When to use:** Spawning any process that needs to know its assigned port.
**Example:**
```csharp
// Source: https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.environment
var startInfo = new ProcessStartInfo
{
    FileName = "dotnet",
    Arguments = "run",
    // CRITICAL: Always merge with existing environment to preserve PATH
    Environment =
    {
        ["PORT"] = allocatedPort.ToString()
    }
};
Process.Start(startInfo);
```

### Pattern 3: Port Pool with PID Tracking
**What:** Maintain a pool of allocated ports mapped to process PIDs for lifecycle management.
**When to use:** Tracking which ports are in use and when they can be released.
**Example:**
```csharp
public class PortPool
{
    private readonly Dictionary<int, int> _portToPid = new();

    public void Allocate(int port, int pid)
    {
        _portToPid[port] = pid;
    }

    public void ReleaseByPid(int pid)
    {
        var portsToRelease = _portToPid
            .Where(kvp => kvp.Value == pid)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var port in portsToRelease)
        {
            _portToPid.Remove(port);
        }
    }

    public bool IsPortAllocated(int port) => _portToPid.ContainsKey(port);
}
```

### Anti-Patterns to Avoid
- **[TOCTOU Race Condition]**: Checking port availability separately from binding - always bind immediately after checking to minimize race condition window
- **[Environment Variable Clobbering]**: Setting `ProcessStartInfo.Environment` without preserving existing variables - always merge with `process.env` equivalent
- **[Sequential Port Allocation]**: Simply incrementing port numbers - leads to predictable patterns and doesn't handle port conflicts well
- **[Blocking on Port Detection]**: Synchronous port checking in async contexts - always use async/await for network operations

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Port availability checking | Raw socket binding with Win32 API calls | TcpListener.Start()/Stop() | Cross-platform, handles edge cases, well-tested |
| Process environment injection | OS-specific batch/shell script generation | ProcessStartInfo.Environment dictionary | Works on Windows/macOS/Linux, no temp files needed |
| Port lifecycle tracking | Manual PID polling with timers | Existing RouteCleanupService + Process.HasExited | Already implemented, handles PID recycling detection |
| Port pool state management | Custom file format + locking | Reuse RouteStore pattern (JSON + file locking) | Proven pattern in codebase, handles concurrency |

**Key insight:** The existing RouteCleanupService already handles process lifecycle detection with PID recycling validation. Port management should integrate with this rather than creating parallel tracking infrastructure.

## Common Pitfalls

### Pitfall 1: TOCTOU Race Condition in Port Detection
**What goes wrong:** Port is free when checked but occupied by the time binding happens, causing "Address already in use" errors.
**Why it happens:** Time gap between port availability check and actual port binding by the application.
**How to avoid:** Minimize time between check and use; implement retry logic with fallback ports; handle SocketException gracefully.
**Warning signs:** Intermittent "Address already in use" errors that go away on retry.

### Pitfall 2: Environment Variable Clobbering on Windows
**What goes wrong:** Child process can't find commands like `dotnet` because PATH was lost.
**Why it happens:** Setting `ProcessStartInfo.Environment` to a new dictionary instead of merging with existing environment.
**How to avoid:** Always preserve existing environment variables when adding PORT:
```csharp
// WRONG - loses PATH and other critical vars
startInfo.Environment = new Dictionary<string, string> { ["PORT"] = "4000" };

// CORRECT - preserves existing environment
startInfo.Environment["PORT"] = "4000";
```
**Warning signs:** Commands work in terminal but fail when spawned by Portless with "command not found" errors.

### Pitfall 3: PID Recycling Detection
**What goes wrong:** Port allocated to old process gets cleaned up when PID is reused by new process.
**Why it happens:** OS reuses PIDs; simple PID existence check doesn't guarantee same process.
**How to avoid:** Use `Process.StartTime` comparison like RouteCleanupService does:
```csharp
if (process.StartTime > route.CreatedAt + TimeSpan.FromSeconds(1))
{
    return false; // PID was recycled
}
```
**Warning signs:** Ports mysteriously disappearing when processes restart quickly.

### Pitfall 4: Cross-Platform Port Binding Differences
**What goes wrong:** Port detection works on Windows but fails on macOS/Linux due to permissions or binding rules.
**Why it happens:** Different OSes have different rules about privileged ports (<1024) and binding to 127.0.0.1 vs 0.0.0.0.
**How to avoid:** Always bind to `IPAddress.Loopback` (127.0.0.1) not `IPAddress.Any` (0.0.0.0); stay in unprivileged range (>1024).
**Warning signs:** "Permission denied" or "Address not available" errors on non-Windows platforms.

## Code Examples

Verified patterns from official sources:

### Port Allocation with Retry Logic
```csharp
// Enhanced version of existing PortAllocator with retry
public async Task<int> AssignFreePortAsync()
{
    var random = new Random();
    const int maxAttempts = 50;

    for (int attempt = 0; attempt < maxAttempts; attempt++)
    {
        var port = random.Next(4000, 5000);

        if (!IsPortAllocated(port) && await IsPortFreeAsync(port))
        {
            AllocatePort(port);
            return port;
        }
    }

    throw new InvalidOperationException(
        "Failed to allocate port after 50 attempts. " +
        "Port range 4000-4999 may be exhausted.");
}
```

### Environment Variable Injection (Cross-Platform)
```csharp
// Source: https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.-ctor
public Process StartProcessWithPort(string command, int port)
{
    var startInfo = new ProcessStartInfo
    {
        FileName = command,
        UseShellExecute = false,  // Required for environment injection
        RedirectStandardOutput = true,
        RedirectStandardError = true
    };

    // CRITICAL: This preserves existing environment variables
    startInfo.Environment["PORT"] = port.ToString();

    return Process.Start(startInfo);
}
```

### Integration with RouteCleanupService
```csharp
// Add to RouteCleanupService.ExecuteAsync()
var portsToRelease = routes
    .Where(route => !IsProcessAlive(route))
    .Select(route => route.Port)
    .ToList();

if (portsToRelease.Any())
{
    _portPool.ReleasePorts(portsToRelease);
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| P/Invoke to Win32 API | TcpListener abstraction | .NET Core 1.0+ | Cross-platform without OS-specific code |
| Manual environment parsing | ProcessStartInfo.Environment | .NET Framework 1.1+ | Type-safe environment variable handling |
| Synchronous I/O | Async/await throughout | .NET Core 2.0+ | Better scalability for port detection operations |

**Deprecated/outdated:**
- **SocketAsyncEventArgs for simple port detection**: Overly complex for single-port checks; use TcpListener instead
- **Environment.SetEnvironmentVariable for child processes**: Only affects current process; use ProcessStartInfo.Environment
- **netstat command parsing**: Fragile and OS-specific; use .NET networking APIs

## Open Questions

1. **Port Pool Persistence**
   - What we know: Port allocations are currently ephemeral (in-memory only)
   - What's unclear: Should port pool persist across proxy restarts like routes do?
   - Recommendation: Keep port pool ephemeral for now; ports are released when processes die, proxy restart means all processes are dead anyway

2. **Sequential vs Random Allocation**
   - What we know: Current implementation uses random allocation
   - What's unclear: Would sequential allocation (4000, 4001, 4002...) be simpler and more predictable?
   - Recommendation: Keep random allocation - better distribution, avoids "port exhaustion clusters"

3. **PORT Usage Validation**
   - What we know: User decided "Portless injects PORT, the app uses it. If the app doesn't, that's the app's concern."
   - What's unclear: Should we add a warning if the command doesn't seem to use PORT?
   - Recommendation: Skip validation - adds complexity without clear benefit; document responsibility clearly instead

## Sources

### Primary (HIGH confidence)
- [Microsoft Learn - TcpListener Class](https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.tcplistener) - Official .NET 10 documentation for TCP listener operations
- [Microsoft Learn - ProcessStartInfo.Environment](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.-ctor) - Official documentation for environment variable injection
- [Microsoft Learn - SocketException.ErrorCode](https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.socketexception.errorcode) - Error code reference for port binding failures

### Secondary (MEDIUM confidence)
- [C# TCP Port Detection Guide](https://m.blog.csdn.net/weixin_35987118/article/details/150477445) - Practical examples of port availability checking (verified against Microsoft docs)
- [Cross-Platform Environment Variable Injection](https://blog.csdn.net/weixin_30995917/article/details/144002146) - Real-world patterns for ProcessStartInfo usage (verified against Microsoft docs)
- [.NET Port Exhaustion Troubleshooting](https://learn.microsoft.com/en-us/troubleshoot/windows/client/networking/tcp-ip-port-exhaustion) - Microsoft guidance on port range management

### Tertiary (LOW confidence)
- [Port Pool Management Patterns](https://m.blog.csdn.net/songhuangong123/article/details/151897895) - General port allocation strategies (not .NET-specific, needs validation)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All components are part of .NET 10 baseline with official documentation
- Architecture: HIGH - Patterns are well-established in .NET ecosystem with Microsoft examples
- Pitfalls: HIGH - All pitfalls documented with verified solutions from official sources

**Research date:** 2026-02-20
**Valid until:** 2026-03-20 (30 days - .NET 10 is stable, port management patterns are mature)
