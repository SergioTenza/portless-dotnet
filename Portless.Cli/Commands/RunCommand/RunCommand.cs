using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
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

    public RunCommand(IPortAllocator portAllocator, IRouteStore routeStore, IHttpClientFactory httpClientFactory)
    {
        _portAllocator = portAllocator;
        _routeStore = routeStore;
        _httpClientFactory = httpClientFactory;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, RunSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            // Step 0: Check for duplicate routes
            var hostname = $"{settings.Name}.localhost";
            var existingRoutes = await _routeStore.LoadRoutesAsync();
            if (existingRoutes.Any(r => r.Hostname == hostname))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Route '{settings.Name}' already exists.");
                AnsiConsole.MarkupLine("Use [yellow]'portless list'[/] to see active routes.");
                return 1;
            }

            // Step 1: Validate proxy is running
            if (!await IsProxyRunningAsync())
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Proxy is not running.");
                AnsiConsole.MarkupLine("Start the proxy first: [yellow]'portless proxy start'[/]");
                return 1;
            }

            // Step 2: Assign free port
            var port = await _portAllocator.AssignFreePortAsync();

            // Step 3: Build process start info
            var startInfo = new ProcessStartInfo
            {
                FileName = settings.Command[0],
                Arguments = string.Join(" ", settings.Command.Skip(1)),
                UseShellExecute = true,  // Detached execution
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                Environment =
                {
                    ["PORT"] = port.ToString()
                }
            };

            // Step 4: Start background process
            var process = Process.Start(startInfo);
            if (process == null)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Failed to start process");
                return 1;
            }

            // Step 5: Register route with proxy
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
                    AnsiConsole.MarkupLine("[red]Error:[/] Failed to register route with proxy");
                    return 1;
                }
            }
            catch (HttpRequestException ex)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Failed to communicate with proxy");
                AnsiConsole.MarkupLine($"[dim]{ex.Message}[/]");
                return 1;
            }

            // Step 6: Persist route to storage
            var routes = await _routeStore.LoadRoutesAsync();
            var newRoute = new RouteInfo
            {
                Hostname = hostname,
                Port = port,
                Pid = process.Id,
                CreatedAt = DateTime.UtcNow
            };

            await _routeStore.SaveRoutesAsync(routes.Append(newRoute).ToArray());

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
}
