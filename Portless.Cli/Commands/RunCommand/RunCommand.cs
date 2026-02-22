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

[Description("Run an application with a named URL")]
public class RunCommand : AsyncCommand<RunSettings>
{
    private readonly IPortAllocator _portAllocator;
    private readonly IRouteStore _routeStore;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IProxyProcessManager _proxyManager;
    private readonly IProcessManager _processManager;
    private readonly ILogger<RunCommand> _logger;
    private Process? _spawnedProcess;

    public RunCommand(
        IPortAllocator portAllocator,
        IRouteStore routeStore,
        IHttpClientFactory httpClientFactory,
        IProxyProcessManager proxyManager,
        IProcessManager processManager,
        ILogger<RunCommand> logger)
    {
        _portAllocator = portAllocator;
        _routeStore = routeStore;
        _httpClientFactory = httpClientFactory;
        _proxyManager = proxyManager;
        _processManager = processManager;
        _logger = logger;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, RunSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            // DEBUG: Show what we received
            Console.WriteLine($"[DEBUG] Context Arguments: {string.Join(" | ", context.Arguments)}");

            // Skip first argument (the NAME) and get the rest as the command
            var commandArgs = context.Arguments.Skip(1).ToArray();
            Console.WriteLine($"[DEBUG] Command args: {string.Join(" | ", commandArgs)}");
            Console.WriteLine($"[DEBUG] Command count: {commandArgs.Length}");

            if (commandArgs.Length == 0)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Command is required");
                AnsiConsole.MarkupLine("Usage: [yellow]portless run <name> <command>[/]");
                return 1;
            }

            // Step 0: Check for duplicate routes
            var hostname = $"{settings.Name}.localhost";
            var existingRoutes = await _routeStore.LoadRoutesAsync();
            if (existingRoutes.Any(r => r.Hostname == hostname))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Route '{settings.Name}' already exists.");
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

            // Step 2: Assign free port first (will use temporary PID 0, updated later)
            var port = await _portAllocator.AssignFreePortAsync(0);

            // Step 3: Start process using ProcessManager with PORT injection
            var process = _processManager.StartManagedProcess(
                commandArgs[0],                                             // command
                string.Join(" ", commandArgs.Skip(1)),                     // args
                port,                                                       // allocated port
                Directory.GetCurrentDirectory()                            // working directory
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

            // Step 4: Update port allocation with real PID
            // Note: This is a workaround - we allocated with PID=0, now we need to update
            // The PortPool doesn't have an "UpdatePid" method, so we'll release and re-allocate
            await _portAllocator.ReleasePortAsync(port);
            await _portAllocator.AssignFreePortAsync(process.Id);

            // Step 6: Register route with proxy
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
                AnsiConsole.MarkupLine($"[dim]HResult: {ex.HResult}[/]");
                return 1;
            }

            // Step 7: Persist route to storage
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

            // Step 7: Show success message
            AnsiConsole.MarkupLine($"[green]✓[/] Running on [blue]http://{hostname}[/] (port: {port})");

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
