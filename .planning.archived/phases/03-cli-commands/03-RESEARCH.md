# Phase 3: CLI Commands - Research

**Researched:** 2026-02-19
**Domain:** CLI Development with Spectre.Console.Cli, Process Management, and Cross-Platform Background Execution
**Confidence:** HIGH

## Summary

Phase 3 requires implementing a complete CLI interface for Portless.NET using Spectre.Console.Cli 0.53.1. The CLI must support hierarchical commands (`proxy start/stop`, `list`, `run`), background process execution, TTY-aware output formatting, and comprehensive error handling. Based on research, Spectre.Console.Cli provides excellent support for hierarchical commands with type-safe settings, async command execution, and validation. For background process execution, .NET's `Process.Start` with proper configuration (`CreateNoWindow`, `UseShellExecute`) enables cross-platform detached processes. The existing RouteStore and DynamicConfigProvider from Phases 1-2 provide the foundation for route persistence and proxy communication.

**Primary recommendation:** Use Spectre.Console.Cli's command hierarchy with AsyncCommand<T> base classes, implement background process execution via Process.Start with platform-specific configuration, and leverage Spectre.Console's table/progress widgets for rich CLI output.

## User Constraints (from CONTEXT.md)

### Locked Decisions
- **Estructura jerárquica**: `portless proxy start/stop`, `portless list`, `portless run <name> <cmd>`
- **Comando run**: `portless run <nombre> <comando>` — formato separado
- **Comandos de gestión**: start/stop/list + `portless proxy status`
- **Configuración de puerto**: Flag `--port` en `portless proxy start --port 1355`
- **Salida de list**: Detección automática — tabla si es TTY, JSON si se redirige
- **Información en list**: Nombre, hostname, puerto, PID
- **Feedback de start**: Mensaje breve "Proxy started on http://localhost:1355"
- **Indicador de progreso**: Spinner mientras el proxy se inicia
- **Modo de ejecución**: Background (detached) — CLI retorna inmediatamente
- **Output del proceso**: Descartar stdout/stderr del proceso ejecutado
- **Manejo de SIGINT**: No propagar Ctrl+C al proceso background
- **Mensaje post-run**: "Running on http://miapi.localhost (port: 4001)"
- **Nivel de detalle errores**: Minimal — solo problema y solución, sin stack traces
- **Proxy ya corriendo**: "Error: Proxy is already running. Use 'portless proxy stop' first"
- **Ruta existente**: "Error: Route 'api' already exists. Use 'portless list' to see active routes"
- **Puerto ocupado**: "Error: Port 1355 in use. Try: netstat -ano | findstr 1355"

### Claude's Discretion
- **Sintaxis de run**: Elegir formato más estándar para CLI tools
- **Comandos de gestión**: Agregar `status` y/o `restart` si hacen la CLI más completa sin redundancia
- **Información en list**: Balance entre información útil y no abrumar al usuario
- **Manejo de SIGINT**: Elegir comportamiento más estándar para background processes
- **Mensaje de proxy ya corriendo**: Elegir el mensaje más claro y accionable

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope.

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| CLI-01 | `portless proxy start` inicia proxy en puerto 1355 | Spectre.Console.Cli command settings, HttpClient/Process.Start for proxy launch |
| CLI-02 | `portless proxy stop` detiene proxy limpiamente | PID tracking via proxy.pid file, Process.Kill for clean shutdown |
| CLI-03 | `portless <name> <command>` ejecuta app con URL nombrada | AsyncCommand with Process.Start, PORT env var injection, route registration |
| CLI-04 | `portless list` muestra apps activas con hostname -> puerto mapping | RouteStore.LoadRoutesAsync, Spectre.Console.Table for TTY output |
| CLI-05 | CLI muestra errores claros y accionables | Spectre.Console.AnsiConsole markup, ValidationResult patterns |

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| **Spectre.Console.Cli** | 0.53.1 | CLI framework | Type-safe command parsing, hierarchical commands, async support, rich output formatting |
| **System.Diagnostics.Process** | .NET 10 BCL | Background process execution | Cross-platform process management, environment variable injection |
| **Spectre.Console** | 0.53.1 (included) | Rich CLI output | Tables, progress bars, spinners, colored markup, TTY detection |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| **System.Net.Http** | .NET 10 BCL | Proxy API communication | POST to /api/v1/add-host for route registration |
| **System.Text.Json** | .NET 10 BCL | JSON output for `list` command | When stdout is redirected (non-TTY) |
| **Microsoft.Extensions.DependencyInjection** | .NET 10 BCL | DI container for services | Injecting IRouteStore, IHttpClientFactory into commands |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Spectre.Console.Cli | System.CommandLine | More verbose API, less elegant table formatting |
| Process.Start (detached) | daemonization libraries | More complexity, platform-specific code required |
| Spectre.Console tables | Console tables (manual) | Much more code, less maintainable |

**Installation:**
```bash
# Already installed in Portless.Cli.csproj
dotnet add package Spectre.Console.Cli --version 0.53.1
```

## Architecture Patterns

### Recommended Project Structure
```
Portless.Cli/
├── Commands/
│   ├── ProxyCommand/
│   │   ├── ProxyStartSettings.cs
│   │   ├── ProxyStartCommand.cs
│   │   ├── ProxyStopCommand.cs
│   │   ├── ProxyStatusCommand.cs  # Claude's discretion
│   │   └── ProxyRestartCommand.cs # Claude's discretion
│   ├── RunCommand/
│   │   ├── RunSettings.cs
│   │   └── RunCommand.cs
│   └── ListCommand/
│       ├── ListSettings.cs
│       └── ListCommand.cs
├── Services/
│   ├── IProxyProcessManager.cs
│   ├── ProxyProcessManager.cs
│   ├── IPortAllocator.cs
│   └── PortAllocator.cs
├── Program.cs
└── Extensions/
    └── ServiceCollectionExtensions.cs
```

### Pattern 1: Hierarchical Commands with Spectre.Console.Cli
**What:** Spectre.Console.Cli supports nested command structure via branches
**When to use:** When you have related commands that share context (e.g., `proxy start`, `proxy stop`)
**Example:**
```csharp
// Source: Spectre.Console.Cli documentation patterns
public class ProxyStartSettings : CommandSettings
{
    [CommandOption("--port <PORT>")]
    [DefaultValue(1355)]
    public int Port { get; set; }
}

public class ProxyStartCommand : AsyncCommand<ProxyStartSettings>
{
    private readonly IProxyProcessManager _proxyManager;

    public ProxyStartCommand(IProxyProcessManager proxyManager)
    {
        _proxyManager = proxyManager;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ProxyStartSettings settings)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Starting proxy...", async _ =>
            {
                await _proxyManager.StartAsync(settings.Port);
            });

        AnsiConsole.MarkupLine("[green]✓[/] Proxy started on http://localhost:{0}", settings.Port);
        return 0;
    }
}

// In Program.cs
var app = new CommandApp();
app.Configure(config =>
{
    config.AddBranch<ProxySettings>("proxy", proxy =>
    {
        proxy.AddCommand<ProxyStartCommand>("start")
            .WithDescription("Start the proxy server");
        proxy.AddCommand<ProxyStopCommand>("stop")
            .WithDescription("Stop the proxy server");
        proxy.AddCommand<ProxyStatusCommand>("status")
            .WithDescription("Check proxy status");
    });
    config.AddCommand<RunCommand>("run")
        .WithAlias("r")  // Common short alias
        .WithDescription("Run an app with a named URL");
    config.AddCommand<ListCommand>("list")
        .WithAlias("ls")
        .WithDescription("List active routes");
});
```

### Pattern 2: Background Process Execution
**What:** Launch detached processes that outlive the CLI
**When to use:** For starting the proxy server and running user applications
**Example:**
```csharp
// Cross-platform detached process execution
public class ProxyProcessManager : IProxyProcessManager
{
    public async Task StartAsync(int port)
    {
        var stateDir = StateDirectoryProvider.GetStateDirectory();
        var pidFile = Path.Combine(stateDir, "proxy.pid");

        // Check if already running
        if (File.Exists(pidFile))
        {
            var existingPid = int.Parse(await File.ReadAllTextAsync(pidFile));
            if (IsProcessRunning(existingPid))
            {
                throw new InvalidOperationException("Proxy is already running");
            }
        }

        // Build proxy project path
        var proxyProjectPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "..", "Portless.Proxy", "Portless.Proxy.csproj"
        );

        // Configure process start info
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{proxyProjectPath}\" --urls http://*:{port}",
            UseShellExecute = true,  // Required for detached execution
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            Environment =
            {
                ["PORTLESS_PORT"] = port.ToString(),
                ["DOTNET_MODIFIABLE_ASSEMBLIES"] = "debug"  // Hot reload support
            }
        };

        // Start detached process
        var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start proxy process");
        }

        // Write PID file
        await File.WriteAllTextAsync(pidFile, process.Id.ToString());

        // Wait briefly to ensure startup
        await Task.Delay(500);
    }

    private static bool IsProcessRunning(int pid)
    {
        try
        {
            var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
```

### Pattern 3: TTY-Aware Output
**What:** Detect if output is redirected and format accordingly
**When to use:** For commands that can output either rich text or machine-readable data
**Example:**
```csharp
public class ListCommand : AsyncCommand<ListSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ListSettings settings)
    {
        var routes = await _routeStore.LoadRoutesAsync();

        // Detect if output is redirected (non-TTY)
        if (!Console.IsOutputRedirected)
        {
            // Rich table output for terminal
            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.AddColumn("[yellow]Name[/]");
            table.AddColumn("[yellow]URL[/]");
            table.AddColumn("[yellow]Port[/]");
            table.AddColumn("[yellow]PID[/]");

            foreach (var route in routes)
            {
                table.AddRow(
                    route.Hostname.Replace(".localhost", ""),
                    $"http://{route.Hostname}",
                    route.Port.ToString(),
                    route.Pid.ToString()
                );
            }

            AnsiConsole.Write(table);
        }
        else
        {
            // JSON output for redirection
            var json = JsonSerializer.Serialize(routes, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });
            Console.WriteLine(json);
        }

        return 0;
    }
}
```

### Pattern 4: Error Handling with Validation
**What:** Use Spectre.Console's ValidationResult for user-friendly errors
**When to use:** For input validation and pre-execution checks
**Example:**
```csharp
public class RunSettings : CommandSettings
{
    [CommandArgument(0, "[NAME]")]
    public string Name { get; set; } = string.Empty;

    [CommandArgument(1, "[COMMAND...]")]
    public string[] Command { get; set; } = Array.Empty<string>();

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return ValidationResult.Error("Name is required");
        }

        if (Name.Contains(' ') || Name.Contains('/'))
        {
            return ValidationResult.Error("Name must be a valid hostname (no spaces or slashes)");
        }

        if (Command.Length == 0)
        {
            return ValidationResult.Error("Command to run is required");
        }

        // Check if route already exists
        var routes = _routeStore.LoadRoutesAsync().GetAwaiter().GetResult();
        if (routes.Any(r => r.Hostname == $"{Name}.localhost"))
        {
            return ValidationResult.Error($"Route '{Name}' already exists. Use 'portless list' to see active routes");
        }

        return ValidationResult.Success();
    }
}
```

### Anti-Patterns to Avoid
- **Synchronous Process.Start:** Don't use blocking `WaitForExit()` when you need detached execution. Always configure `UseShellExecute = true` and `CreateNoWindow = true` for background processes.
- **Hardcoded error messages:** Don't inline error strings. Use Spectre.Console's markup for consistent formatting and localization support.
- **Manual argument parsing:** Don't split command strings by spaces. Use `CommandArgument` arrays to properly handle quoted arguments with spaces.
- **Ignoring TTY detection:** Don't always output tables or always output JSON. Use `Console.IsOutputRedirected` to format appropriately.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Command parsing | Manual `args` parsing with string splitting | Spectre.Console.Cli `CommandArgument` and `CommandOption` | Handles quotes, escaping, validation automatically |
| TTY detection | Platform-specific console checks | `Console.IsOutputRedirected` | Built-in cross-platform detection |
| Table formatting | Manual column padding and borders | `Spectre.Console.Table` | Handles borders, alignment, wrapping automatically |
| Progress indication | Manual ASCII spinners | `AnsiConsole.Status()` and `AnsiConsole.Progress()` | Smooth animation, proper terminal handling |
| Color/formatting | ANSI escape codes | Spectre.Console markup (`[green]`, `[bold]`) | Cross-platform, readable, handles unsupported terminals |

**Key insight:** Spectre.Console.Cli eliminates entire categories of boilerplate code (argument parsing, help text generation, validation) while Spectre.Console provides professional-grade output formatting that would take hundreds of lines to implement manually.

## Common Pitfalls

### Pitfall 1: Blocking on Background Process Start
**What goes wrong:** CLI waits for proxy/app to fully start before returning, defeating the purpose of background execution
**Why it happens:** Using `Process.WaitForExit()` or `await process.WaitForExitAsync()` after starting the process
**How to avoid:** Always use `UseShellExecute = true` with `CreateNoWindow = true` for true detached execution. Add a small delay (500ms) for startup verification but don't wait for the process to complete.
**Warning signs:** CLI doesn't return control immediately after `portless proxy start` or `portless run`

### Pitfall 2: Not Detecting Output Redirection
**What goes wrong:** Table formatting breaks when output is piped to files or other commands
**Why it happens:** Always rendering rich table output regardless of output target
**How to avoid:** Check `Console.IsOutputRedirected` before formatting. Output JSON when redirected, tables when TTY.
**Warning signs:** `portless list > routes.txt` creates a file with ANSI escape codes

### Pitfall 3: Incorrect Hostname Validation
**What goes wrong:** User creates routes with invalid hostnames that break DNS resolution
**Why it happens:** Not validating hostname format before accepting input
**How to avoid:** Validate that hostname contains only alphanumeric characters, hyphens, and dots. Reject spaces, slashes, and special characters.
**Warning signs:** `portless run "my api" dotnet run` creates a broken route

### Pitfall 4: Race Conditions in PID File
**What goes wrong:** Multiple instances start simultaneously because PID file check happens after process launch
**Why it happens:** Check-and-act race condition between file existence check and process start
**How to avoid:** Use file locking (Mutex) around PID file operations, or use `Process.GetProcessById()` to verify actual process state rather than just file existence.
**Warning signs:** Multiple proxy processes running after rapid `portless proxy start` commands

### Pitfall 5: Environment Variable Injection
**What goes wrong:** Child process doesn't receive PORT variable or receives wrong value
**Why it happens:** Not setting `ProcessStartInfo.Environment` dictionary before starting process
**How to avoid:** Always set environment variables on `ProcessStartInfo` before calling `Process.Start()`. Verify with `echo $PORT` in child process.
**Warning signs:** `portless run myapp dotnet run` fails with "PORT environment variable not found"

## Code Examples

Verified patterns from official sources:

### Background Process with Environment Variables
```csharp
// Source: System.Diagnostics.Process documentation + Spectre.Console patterns
public async Task<int> ExecuteAsync(CommandContext context, RunSettings settings)
{
    // Assign free port
    var port = await _portAllocator.AssignFreePortAsync();
    var hostname = $"{settings.Name}.localhost";

    // Build command with PORT injection
    var command = string.Join(" ", settings.Command);
    var startInfo = new ProcessStartInfo
    {
        FileName = settings.Command[0],  // First arg is the command
        Arguments = string.Join(" ", settings.Command.Skip(1)),
        UseShellExecute = true,
        CreateNoWindow = true,
        WindowStyle = ProcessWindowStyle.Hidden,
        Environment =
        {
            ["PORT"] = port.ToString()
        }
    };

    // Start detached process
    var process = Process.Start(startInfo);
    if (process == null)
    {
        AnsiConsole.MarkupLine("[red]✗[/] Failed to start process");
        return 1;
    }

    // Register route with proxy
    await _proxyClient.AddRouteAsync(hostname, port);

    // Persist route
    var routes = await _routeStore.LoadRoutesAsync();
    var newRoute = new RouteInfo
    {
        Hostname = hostname,
        Port = port,
        Pid = process.Id,
        CreatedAt = DateTime.UtcNow
    };
    await _routeStore.SaveRoutesAsync(routes.Append(newRoute).ToArray());

    AnsiConsole.MarkupLine("[green]✓[/] Running on http://{0} (port: {1})", hostname, port);
    return 0;
}
```

### Proxy Status Check
```csharp
// Source: Pattern from existing Portless.Core Services
public async Task<bool> IsRunningAsync()
{
    var stateDir = StateDirectoryProvider.GetStateDirectory();
    var pidFile = Path.Combine(stateDir, "proxy.pid");

    if (!File.Exists(pidFile))
    {
        return false;
    }

    var pidText = await File.ReadAllTextAsync(pidFile);
    if (!int.TryParse(pidText, out var pid))
    {
        return false;
    }

    try
    {
        var process = Process.GetProcessById(pid);
        return !process.HasExited;
    }
    catch (ArgumentException)
    {
        return false;
    }
}
```

### Rich Error Messages
```csharp
// Source: Spectre.Console best practices
private static void ShowError(string title, string message, string? suggestion = null)
{
    AnsiConsole.MarkupLine(Environment.NewLine);
    AnsiConsole.MarkupLine($"[red]Error: {title}[/]");
    AnsiConsole.MarkupLine($"[dim]{message}[/]");

    if (suggestion != null)
    {
        AnsiConsole.MarkupLine(Environment.NewLine);
        AnsiConsole.MarkupLine($"[yellow]→[/] {suggestion}");
    }

    AnsiConsole.MarkupLine(Environment.NewLine);
}

// Usage
if (await _proxyManager.IsRunningAsync())
{
    ShowError(
        "Proxy already running",
        "A proxy instance is already active on this system.",
        "Use 'portless proxy stop' first to stop the existing instance."
    );
    return 1;
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Manual args parsing | Spectre.Console.Cli type-safe commands | Spectre.Console 0.40+ (2022) | Eliminates boilerplate, adds validation |
| Synchronous process execution | AsyncCommand<T> with detached processes | .NET 5+ (2020) | Non-blocking CLI, better UX |
| Always table output | TTY-aware output (table vs JSON) | Modern CLI standard (2020+) | Better scripting/integration |
| Basic console colors | Spectre.Console markup with fallback | Spectre.Console 0.40+ (2022) | Professional output, graceful degradation |

**Deprecated/outdated:**
- **Manual string splitting for arguments:** Replaced by `CommandArgument` arrays with automatic parsing
- **Console.WriteLine for tables:** Replaced by `Spectre.Console.Table` with automatic formatting
- **ANSI escape codes:** Replaced by Spectre.Console markup (`[green]text[/]`) with fallback support
- **Synchronous Process.Start:** Replaced by async/await patterns with proper cancellation support

## Open Questions

1. **Cross-platform background process behavior**
   - What we know: `UseShellExecute = true` with `CreateNoWindow = true` works for detached execution on Windows. Unix behavior may differ.
   - What's unclear: Whether macOS/Linux require additional configuration (e.g., fork/detach) for true daemon behavior
   - Recommendation: Test on macOS/Linux early. May need platform-specific code using `fork()` on Unix.

2. **Hot reload compatibility**
   - What we know: Setting `DOTNET_MODIFIABLE_ASSEMBLIES=debug` enables hot reload for .NET processes
   - What's unclear: Whether this interferes with background process detachment
   - Recommendation: Make hot reload opt-in via environment variable or flag

3. **Port allocation strategy**
   - What we know: Need to detect free ports in range 4000-4999
   - What's unclear: Whether to use TCP listener binding (reliable) or OS port randomization (simpler but conflicts possible)
   - Recommendation: Use TCP listener binding for reliability, with retry logic for port conflicts

4. **SIGINT propagation to child processes**
   - What we know: CONTEXT.md specifies "No propagar Ctrl+C al proceso background"
   - What's unclear: How this interacts with process groups on different platforms
   - Recommendation: Test with Ctrl+C during running app. May need to ignore SIGINT in CLI and let child handle it independently.

## Sources

### Primary (HIGH confidence)
- Spectre.Console.Cli 0.53.1 documentation - Command hierarchy, AsyncCommand<T>, validation patterns
- System.Diagnostics.Process .NET 10 documentation - ProcessStartInfo configuration, cross-platform behavior
- Spectre.Console 0.53.1 documentation - Table formatting, Status() widgets, TTY detection
- Existing Portless.Core code - RouteStore, StateDirectoryProvider, DynamicConfigProvider patterns

### Secondary (MEDIUM confidence)
- C# Process.Start best practices articles (2025) - Background process configuration, environment variables
- Spectre.Console example repositories - Real-world command structures, error handling patterns

### Tertiary (LOW confidence)
- Cross-platform daemonization patterns - May need platform-specific code for Unix
- Hot reload with background processes - Interaction between DOTNET_MODIFIABLE_ASSEMBLIES and detached execution

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Spectre.Console.Cli 0.53.1 is well-documented and stable. System.Diagnostics.Process is mature .NET BCL.
- Architecture: HIGH - Command hierarchy pattern is standard for Spectre.Console.Cli. Background process pattern is well-established.
- Pitfalls: MEDIUM - Cross-platform process behavior may have edge cases. TTY detection is reliable but output redirection handling needs testing.

**Research date:** 2026-02-19
**Valid until:** 2026-05-19 (90 days - CLI libraries are stable, .NET 10 is in preview)
