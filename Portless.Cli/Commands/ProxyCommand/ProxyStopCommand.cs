using Spectre.Console;
using Spectre.Console.Cli;
using Portless.Cli.Services;

namespace Portless.Cli.Commands.ProxyCommand;

public class ProxyStopCommand : AsyncCommand<ProxyStopSettings>
{
    private readonly IProxyProcessManager _proxyManager;

    public ProxyStopCommand(IProxyProcessManager proxyManager)
    {
        _proxyManager = proxyManager;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ProxyStopSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            // Check if proxy is running
            if (!await _proxyManager.IsRunningAsync())
            {
                AnsiConsole.MarkupLine("[yellow]Proxy is not running.[/]");
                return 0;
            }

            // Check for active managed processes
            var activePids = await _proxyManager.GetActiveManagedProcessesAsync();
            if (activePids.Length > 0)
            {
                AnsiConsole.MarkupLine($"[yellow]Warning:[/] {activePids.Length} active process(es) spawned by Portless:");
                foreach (var pid in activePids)
                {
                    AnsiConsole.MarkupLine($"  - PID {pid}");
                }

                var confirm = AnsiConsole.Confirm("Stop these processes along with the proxy?", defaultValue: false);
                if (!confirm)
                {
                    AnsiConsole.MarkupLine("[yellow]Proxy stop cancelled.[/]");
                    AnsiConsole.MarkupLine("Processes will continue running. Use [yellow]'portless list'[/] to see active routes.");
                    return 0;
                }

                AnsiConsole.MarkupLine("[yellow]Stopping processes...[/]");
                try
                {
                    await _proxyManager.KillManagedProcessesAsync(activePids);
                    AnsiConsole.MarkupLine("[green]✓[/] Processes stopped");
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[yellow]Warning:[/] Failed to stop some processes: {ex.Message}");
                    // Continue stopping proxy anyway
                }
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
