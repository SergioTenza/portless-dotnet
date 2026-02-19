using Spectre.Console;
using Spectre.Console.Cli;
using Portless.Cli.Services;

namespace Portless.Cli.Commands.ProxyCommand;

public class ProxyStopCommand : AsyncCommand<CommandSettings>
{
    private readonly IProxyProcessManager _proxyManager;

    public ProxyStopCommand(IProxyProcessManager proxyManager)
    {
        _proxyManager = proxyManager;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, CommandSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            // Check if proxy is running
            if (!await _proxyManager.IsRunningAsync())
            {
                AnsiConsole.MarkupLine("[yellow]Proxy is not running.[/]");
                return 0;
            }

            // Stop the proxy with status spinner
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Stopping proxy...", async _ =>
                {
                    await _proxyManager.StopAsync();
                });

            AnsiConsole.MarkupLine("[green]✓[/] Proxy stopped.");
            return 0;
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] {0}", ex.Message);
            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Failed to stop proxy: {0}", ex.Message);
            return 1;
        }
    }
}
