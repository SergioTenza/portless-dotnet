using Spectre.Console;
using Spectre.Console.Cli;
using Portless.Cli.Services;

namespace Portless.Cli.Commands.DaemonCommand;

public class DaemonInstallCommand : AsyncCommand<DaemonInstallSettings>
{
    private readonly IDaemonService _daemonService;

    public DaemonInstallCommand(IDaemonService daemonService)
    {
        _daemonService = daemonService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, DaemonInstallSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Installing daemon service...", async _ =>
                {
                    await _daemonService.InstallAsync(settings.EnableHttps, settings.EnableNow);
                });

            AnsiConsole.MarkupLine("[green]✓[/] Daemon service installed.");
            if (settings.EnableHttps)
            {
                AnsiConsole.MarkupLine("      HTTPS: [green]enabled[/]");
            }
            if (settings.EnableNow)
            {
                AnsiConsole.MarkupLine("      Auto-start: [green]enabled[/]");
            }
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] {0}", ex.Message);
            return 1;
        }
    }
}

public class DaemonUninstallCommand : AsyncCommand<DaemonUninstallSettings>
{
    private readonly IDaemonService _daemonService;

    public DaemonUninstallCommand(IDaemonService daemonService)
    {
        _daemonService = daemonService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, DaemonUninstallSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Uninstalling daemon service...", async _ =>
                {
                    await _daemonService.UninstallAsync();
                });

            AnsiConsole.MarkupLine("[green]✓[/] Daemon service uninstalled.");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] {0}", ex.Message);
            return 1;
        }
    }
}

public class DaemonStatusCommand : AsyncCommand<DaemonStatusSettings>
{
    private readonly IDaemonService _daemonService;

    public DaemonStatusCommand(IDaemonService daemonService)
    {
        _daemonService = daemonService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, DaemonStatusSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            var (isInstalled, isEnabled, isRunning, pid) = await _daemonService.GetStatusAsync();

            if (!isInstalled)
            {
                AnsiConsole.MarkupLine("[yellow]Daemon service is not installed.[/]");
                AnsiConsole.MarkupLine("Run [blue]portless daemon install[/] to install it.");
                return 0;
            }

            var table = new Table()
                .Border(TableBorder.Simple)
                .AddColumn("Property")
                .AddColumn("Value");

            table.AddRow("Installed", isInstalled ? "[green]Yes[/]" : "[red]No[/]");
            table.AddRow("Enabled", isEnabled ? "[green]Yes[/]" : "[yellow]No[/]");
            table.AddRow("Running", isRunning ? "[green]Yes[/]" : "[red]No[/]");
            table.AddRow("PID", pid?.ToString() ?? "N/A");

            AnsiConsole.Write(table);
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] {0}", ex.Message);
            return 1;
        }
    }
}

public class DaemonEnableCommand : AsyncCommand<DaemonEnableSettings>
{
    private readonly IDaemonService _daemonService;

    public DaemonEnableCommand(IDaemonService daemonService)
    {
        _daemonService = daemonService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, DaemonEnableSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            await _daemonService.EnableAsync();
            AnsiConsole.MarkupLine("[green]✓[/] Daemon service enabled (auto-start on boot).");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] {0}", ex.Message);
            return 1;
        }
    }
}

public class DaemonDisableCommand : AsyncCommand<DaemonDisableSettings>
{
    private readonly IDaemonService _daemonService;

    public DaemonDisableCommand(IDaemonService daemonService)
    {
        _daemonService = daemonService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, DaemonDisableSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            await _daemonService.DisableAsync();
            AnsiConsole.MarkupLine("[green]✓[/] Daemon service disabled (no auto-start on boot).");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] {0}", ex.Message);
            return 1;
        }
    }
}
