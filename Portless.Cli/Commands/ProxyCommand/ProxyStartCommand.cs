using Spectre.Console;
using Spectre.Console.Cli;
using Portless.Cli.Services;

namespace Portless.Cli.Commands.ProxyCommand;

public class ProxyStartCommand : AsyncCommand<ProxyStartSettings>
{
    private readonly IProxyProcessManager _proxyManager;

    public ProxyStartCommand(IProxyProcessManager proxyManager)
    {
        _proxyManager = proxyManager;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ProxyStartSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            // Check if proxy is already running
            if (await _proxyManager.IsRunningAsync())
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Proxy is already running.");
                AnsiConsole.MarkupLine("Use [yellow]'portless proxy stop'[/] first to stop the existing instance.");
                return 1;
            }

            // Start the proxy with status spinner
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Starting proxy...", async _ =>
                {
                    await _proxyManager.StartAsync(settings.Port);
                });

            AnsiConsole.MarkupLine("[green]✓[/] Proxy started on http://localhost:{0}", settings.Port);
            return 0;
        }
        catch (InvalidOperationException ex)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] {0}", ex.Message);
            return 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Failed to start proxy: {0}", ex.Message);
            return 1;
        }
    }
}
