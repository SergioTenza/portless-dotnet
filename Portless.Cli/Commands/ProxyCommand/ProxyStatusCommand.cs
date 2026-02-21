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
            AnsiConsole.MarkupLine("  URL: [blue]http://localhost:{0}[/]", status.port ?? 1355);
            AnsiConsole.MarkupLine("  PID: {0}", status.pid ?? 0);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Failed to get proxy status: {0}", ex.Message);
            return 1;
        }
    }
}
