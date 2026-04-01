using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Portless.Core.Models;
using Portless.Core.Services;
using Portless.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Portless.Cli.Commands.RunCommand;

public class RunCommand : AsyncCommand<RunSettings>
{
    private readonly IPortAllocator _portAllocator;
    private readonly IRouteStore _routeStore;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IProxyProcessManager _proxyManager;
    private readonly IProcessManager _processManager;
    private readonly IFrameworkDetector _frameworkDetector;
    private readonly IProjectNameDetector _projectNameDetector;
    private readonly ILogger<RunCommand> _logger;
    private Process? _spawnedProcess;

    public RunCommand(
        IPortAllocator portAllocator,
        IRouteStore routeStore,
        IHttpClientFactory httpClientFactory,
        IProxyProcessManager proxyManager,
        IProcessManager processManager,
        IFrameworkDetector frameworkDetector,
        IProjectNameDetector projectNameDetector,
        ILogger<RunCommand> logger)
    {
        _portAllocator = portAllocator;
        _routeStore = routeStore;
        _httpClientFactory = httpClientFactory;
        _proxyManager = proxyManager;
        _processManager = processManager;
        _frameworkDetector = frameworkDetector;
        _projectNameDetector = projectNameDetector;
        _logger = logger;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, RunSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            // Parse command args: skip the NAME and get the rest as the command
            var commandArgs = context.Arguments.Skip(1).ToArray();

            if (commandArgs.Length == 0)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Command is required");
                AnsiConsole.MarkupLine("Usage: [yellow]portless run <name> <command>[/]");
                return 1;
            }

            // Resolve name: use provided name or auto-detect from project
            var name = settings.Name;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = _projectNameDetector.DetectProjectName();
                if (string.IsNullOrWhiteSpace(name))
                {
                    AnsiConsole.MarkupLine("[red]Error:[/] Could not auto-detect project name. Please provide a name.");
                    AnsiConsole.MarkupLine("Usage: [yellow]portless run <name> <command>[/]");
                    return 1;
                }
                AnsiConsole.MarkupLine($"[dim]Auto-detected name: [blue]{name}[/][/]");
            }

            var hostname = $"{name}.localhost";

            // Check for duplicate routes
            var existingRoutes = await _routeStore.LoadRoutesAsync();
            if (existingRoutes.Any(r => r.Hostname == hostname))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Route '{name}' already exists.");
                AnsiConsole.MarkupLine("Use [yellow]'portless list'[/] to see active routes.");
                return 1;
            }

            // Step 1: Check if proxy is running, start it if needed
            if (!await IsProxyRunningAsync())
            {
                AnsiConsole.MarkupLine("[yellow]Proxy not running, starting automatically...[/]");

                try
                {
                    await AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .StartAsync("Starting proxy...", async _ =>
                        {
                            await _proxyManager.StartAsync(1355); // Default port
                        });

                    // Wait a bit for proxy to be ready
                    await Task.Delay(1000);

                    if (!await IsProxyRunningAsync())
                    {
                        AnsiConsole.MarkupLine("[red]Error:[/] Failed to start proxy");
                        return 1;
                    }

                    AnsiConsole.MarkupLine("[green]✓[/] Proxy started");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] Failed to start proxy: {ex.Message}");
                    return 1;
                }
            }

            // Step 2: Assign free port
            var port = await _portAllocator.AssignFreePortAsync(0);

            // Step 3: Detect framework and build environment variables
            var detectedFramework = _frameworkDetector.Detect();
            var envVars = new Dictionary<string, string>();

            if (detectedFramework != null)
            {
                AnsiConsole.MarkupLine($"[dim]Detected framework: [blue]{detectedFramework.DisplayName}[/][/]");
                _logger.LogInformation("Detected framework: {Framework}", detectedFramework.DisplayName);

                // Expand framework env vars with actual port and hostname
                envVars = PlaceholderExpander.ExpandEnvVars(detectedFramework.InjectedEnvVars, port, hostname);
            }
            else
            {
                _logger.LogInformation("No framework detected, using default PORT injection");
            }

            // Always inject PORTLESS_URL
            envVars["PORTLESS_URL"] = $"http://{hostname}";

            // Step 4: Expand placeholders in command args and handle framework flags
            var expandedArgs = PlaceholderExpander.ExpandArgs(commandArgs, port, hostname).ToList();

            // If framework detected and has injected flags, append them to the command
            if (detectedFramework?.InjectedFlags.Length > 0)
            {
                var expandedFlags = PlaceholderExpander.ExpandArgs(detectedFramework.InjectedFlags, port, hostname);
                expandedArgs.AddRange(expandedFlags);
                _logger.LogDebug("Appended framework flags: {Flags}", string.Join(" ", expandedFlags));
            }

            // Step 5: Start process with framework-aware env vars
            var process = _processManager.StartManagedProcess(
                expandedArgs[0],                                              // command
                string.Join(" ", expandedArgs.Skip(1)),                      // args
                port,                                                         // allocated port
                Directory.GetCurrentDirectory(),                              // working directory
                envVars                                                       // framework-specific env vars
            );

            // Store process reference for signal forwarding
            _spawnedProcess = process;

            // Register process for tracking
            await _proxyManager.RegisterManagedProcessAsync(process.Id);

            // Set up signal forwarding for graceful shutdown
            var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            using var _ = cts.Token.Register(() =>
            {
                if (_spawnedProcess != null && !_spawnedProcess.HasExited)
                {
                    AnsiConsole.MarkupLine($"[yellow]Forwarding SIGTERM to process {_spawnedProcess.Id}...[/]");
                    ForwardSignalToProcess(_spawnedProcess);
                }
            });

            // Step 6: Update port allocation with real PID
            await _portAllocator.ReleasePortAsync(port);
            await _portAllocator.AssignFreePortAsync(process.Id);

            // Step 7: Register route with proxy
            var httpClient = _httpClientFactory.CreateClient();
            var payload = new
            {
                hostname = hostname,
                backendUrl = $"http://localhost:{port}"
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await httpClient.PostAsync("http://localhost:1355/api/v1/add-host", content);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    AnsiConsole.MarkupLine("[red]Error:[/] Failed to register route with proxy");
                    AnsiConsole.MarkupLine($"[dim]Status: {response.StatusCode}[/]");
                    AnsiConsole.MarkupLine($"[dim]Response: {errorContent}[/]");
                    return 1;
                }
            }
            catch (HttpRequestException ex)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Failed to communicate with proxy");
                AnsiConsole.MarkupLine($"[dim]{ex.Message}[/]");
                return 1;
            }

            // Step 8: Persist route to storage
            var routes = await _routeStore.LoadRoutesAsync();
            var newRoute = new RouteInfo
            {
                Hostname = hostname,
                Port = port,
                Pid = process.Id,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _routeStore.SaveRoutesAsync(routes.Append(newRoute).ToArray());
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning:[/] Route registered with proxy but failed to persist: {ex.Message}");
                // Don't fail the command - the route is still usable via proxy
            }

            // Step 9: Show success message
            AnsiConsole.MarkupLine($"[green]✓[/] Running on [blue link]http://{hostname}[/] (port: {port})");

            return 0;
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Failed to run command");
            AnsiConsole.MarkupLine($"[dim]{ex.Message}[/]");
            return 1;
        }
    }

    private async Task<bool> IsProxyRunningAsync()
    {
        try
        {
            using var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync("localhost", 1355);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void ForwardSignalToProcess(Process process)
    {
        try
        {
            _logger.LogInformation("Forwarding SIGTERM to process {Pid}", process.Id);

            // Try graceful shutdown first (GUI-friendly, cross-platform)
            // CloseMainWindow sends WM_CLOSE on Windows, SIGTERM on Unix
            process.CloseMainWindow();
            bool exitedGracefully = process.WaitForExit(10000); // 10-second timeout

            if (exitedGracefully)
            {
                _logger.LogInformation("Process {Pid} exited gracefully", process.Id);
                AnsiConsole.MarkupLine($"[green]✓[/] Process {process.Id} exited gracefully");
                return;
            }

            // Force kill if timeout expired
            _logger.LogWarning("Process {Pid} did not exit gracefully, forcing termination", process.Id);
            AnsiConsole.MarkupLine($"[yellow]Process {process.Id} did not exit gracefully, forcing termination...[/]");
            process.Kill(entireProcessTree: true);
            process.WaitForExit(); // Ensure cleanup completes
            _logger.LogInformation("Process {Pid} terminated forcefully", process.Id);
            AnsiConsole.MarkupLine($"[green]✓[/] Process {process.Id} terminated");
        }
        catch (InvalidOperationException ex)
        {
            // Process already terminated during signal forwarding
            _logger.LogWarning(ex, "Process {Pid} already terminated during signal forwarding", process.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forwarding signal to process {Pid}", process.Id);
            AnsiConsole.MarkupLine($"[red]Error:[/] Failed to forward signal to process {process.Id}: {ex.Message}");
        }
    }
}
