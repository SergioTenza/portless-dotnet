using Portless.Core.Models;
using Portless.Core.Services;
using Portless.Cli.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http.Json;

namespace Portless.Cli.Commands.AliasCommand;

public class AliasCommand : AsyncCommand<AliasSettings>
{
    private readonly IRouteStore _routeStore;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IProxyProcessManager _proxyProcessManager;

    public AliasCommand(
        IRouteStore routeStore,
        IHttpClientFactory httpClientFactory,
        IProxyProcessManager proxyProcessManager)
    {
        _routeStore = routeStore;
        _httpClientFactory = httpClientFactory;
        _proxyProcessManager = proxyProcessManager;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, AliasSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            var hostname = settings.Name.EndsWith(".localhost")
                ? settings.Name
                : $"{settings.Name}.localhost";

            if (settings.Remove)
            {
                return await RemoveAliasAsync(hostname, cancellationToken);
            }

            if (!settings.Port.HasValue)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Port is required when adding an alias.");
                AnsiConsole.MarkupLine("[dim]Usage: portless alias <name> <port>[/]");
                return 1;
            }

            return await AddAliasAsync(hostname, settings.Port.Value, settings.Host, settings.Protocol, cancellationToken);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    private async Task<int> AddAliasAsync(string hostname, int port, string host, string protocol, CancellationToken cancellationToken)
    {
        var routes = await _routeStore.LoadRoutesAsync(cancellationToken);

        // Check for duplicate
        if (routes.Any(r => r.Hostname.Equals(hostname, StringComparison.OrdinalIgnoreCase)))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Route '[yellow]{hostname}[/]' already exists.");
            AnsiConsole.MarkupLine("[dim]Use 'portless alias --remove <name>' to remove it first.[/]");
            return 1;
        }

        var backendUrl = $"{protocol}://{host}:{port}";

        // Register with proxy if it's running
        try
        {
            var proxyPort = ProxyPortProvider.GetProxyPort();
            using var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri($"http://localhost:{proxyPort}");

            var response = await client.PostAsJsonAsync("/api/v1/add-host", new
            {
                hostname,
                backendUrl
            }, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning:[/] Proxy returned {response.StatusCode}. Route saved but proxy may need restart.");
            }
        }
        catch (HttpRequestException)
        {
            AnsiConsole.MarkupLine("[yellow]Warning:[/] Proxy is not running. Route will be available after proxy starts.");
        }

        // Save to route store (alias = PID 0, no managed process)
        var routeInfo = new RouteInfo
        {
            Hostname = hostname,
            Port = port,
            Pid = 0, // No managed process - this is a static alias
            BackendProtocol = protocol,
            CreatedAt = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        };

        var routeList = routes.ToList();
        routeList.Add(routeInfo);
        await _routeStore.SaveRoutesAsync(routeList.ToArray(), cancellationToken);

        var url = $"http://{hostname}";
        AnsiConsole.MarkupLine($"[green]Alias created:[/] [blue]{url}[/] -> {backendUrl}");
        return 0;
    }

    private async Task<int> RemoveAliasAsync(string hostname, CancellationToken cancellationToken)
    {
        var routes = await _routeStore.LoadRoutesAsync(cancellationToken);
        var route = routes.FirstOrDefault(r => r.Hostname.Equals(hostname, StringComparison.OrdinalIgnoreCase));

        if (route == null)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Alias '[yellow]{hostname}[/]' not found.");
            return 1;
        }

        // Remove from proxy if it's running
        try
        {
            var proxyPort = ProxyPortProvider.GetProxyPort();
            using var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri($"http://localhost:{proxyPort}");
            await client.DeleteAsync($"/api/v1/remove-host?hostname={hostname}", cancellationToken);
        }
        catch (HttpRequestException)
        {
            // Proxy not running, that's fine
        }

        var routeList = routes.Where(r => !r.Hostname.Equals(hostname, StringComparison.OrdinalIgnoreCase)).ToArray();
        await _routeStore.SaveRoutesAsync(routeList, cancellationToken);

        AnsiConsole.MarkupLine($"[green]Alias removed:[/] {hostname}");
        return 0;
    }
}
