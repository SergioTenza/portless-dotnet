using Portless.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using Portless.Cli.Services;

namespace Portless.Cli.Commands.ProxyCommand;

public class ProxyStatusCommand : AsyncCommand<ProxyStatusSettings>
{
    private readonly IProxyProcessManager _proxyManager;

    public ProxyStatusCommand(IProxyProcessManager proxyManager)
    {
        _proxyManager = proxyManager;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ProxyStatusSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            var status = await _proxyManager.GetStatusAsync();

            if (!status.isRunning)
            {
                AnsiConsole.MarkupLine("[yellow]Proxy is not running.[/]");
                return 0;
            }

            AnsiConsole.MarkupLine("[green]✓[/] Proxy is [bold]running[/]");
            AnsiConsole.MarkupLine("  URL: [blue]http://localhost:{0}[/]", status.port ?? ProxyConstants.DefaultHttpPort);
            AnsiConsole.MarkupLine("  PID: {0}", status.pid ?? 0);

            // Show protocol information
            if (settings.Protocol)
            {
                AnsiConsole.MarkupLine("\n[bold]Protocol Support:[/]");
                AnsiConsole.MarkupLine("  HTTP/2: [green]Enabled[/]");
                AnsiConsole.MarkupLine("  WebSocket: [green]Supported[/]");
                AnsiConsole.MarkupLine("  HTTP/1.1: [green]Supported[/]");
                AnsiConsole.MarkupLine("\n[dim]Protocol negotiation is automatic.[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("  Protocols: HTTP/2, WebSocket, HTTP/1.1");
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Failed to get proxy status: {0}", ex.Message);
            return 1;
        }
    }
}
