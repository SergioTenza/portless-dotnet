using Portless.Cli.Services;
using Portless.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Sockets;

namespace Portless.Cli.Commands.UpCommand;

public class UpCommand : AsyncCommand<UpSettings>
{
    private readonly IPortlessConfigLoader _configLoader;
    private readonly IProxyRouteRegistrar _registrar;
    private readonly IProxyProcessManager _proxyManager;

    public UpCommand(
        IPortlessConfigLoader configLoader,
        IProxyRouteRegistrar registrar,
        IProxyProcessManager proxyManager)
    {
        _configLoader = configLoader;
        _registrar = registrar;
        _proxyManager = proxyManager;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, UpSettings settings, CancellationToken ct)
    {
        var config = _configLoader.Load(settings.ConfigFile);

        if (config.Routes.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No routes found in config file.[/]");
            return 0;
        }

        // Ensure proxy is running
        if (!await IsProxyRunningAsync())
        {
            AnsiConsole.MarkupLine("[yellow]Proxy not running, starting automatically...[/]");
            try
            {
                await _proxyManager.StartAsync(1355);
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

        int registered = 0, failed = 0;
        foreach (var route in config.Routes)
        {
            if (string.IsNullOrEmpty(route.Host) || route.Backends.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]Skipping route with missing host or backends[/]");
                failed++;
                continue;
            }

            var backend = route.Backends[0];
            var success = await _registrar.RegisterRouteAsync(route.Host, backend);
            if (success)
            {
                var pathInfo = !string.IsNullOrEmpty(route.Path) ? $" (path: {route.Path})" : "";
                var backendInfo = route.Backends.Count > 1
                    ? $"{route.Backends.Count} backends"
                    : route.Backends[0];
                AnsiConsole.MarkupLine($"[green]✓[/] {route.Host}{pathInfo} -> {backendInfo}");
                registered++;
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Failed to register {route.Host}");
                failed++;
            }
        }

        AnsiConsole.MarkupLine($"[blue]{registered}[/] routes registered, [red]{failed}[/] failed");
        return failed > 0 ? 1 : 0;
    }

    private static async Task<bool> IsProxyRunningAsync()
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
