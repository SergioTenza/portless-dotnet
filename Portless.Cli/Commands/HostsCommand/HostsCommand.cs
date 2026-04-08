using Portless.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Portless.Cli.Commands.HostsCommand;

public class HostsCommand : AsyncCommand<HostsSettings>
{
    private const string HostsMarkerStart = "# >>>> portless >>>>";
    private const string HostsMarkerEnd = "# <<<< portless <<<<";
    private const string DefaultHostsPath = "/etc/hosts";
    private const string WindowsHostsPath = @"C:\Windows\System32\drivers\etc\hosts";

    private readonly IRouteStore _routeStore;

    public HostsCommand(IRouteStore routeStore)
    {
        _routeStore = routeStore;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, HostsSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            var action = settings.Action.ToLowerInvariant();

            if (action == "sync")
            {
                return await SyncAsync(cancellationToken);
            }

            if (action == "clean")
            {
                return await CleanAsync(cancellationToken);
            }

            AnsiConsole.MarkupLine($"[red]Error:[/] Unknown action '[yellow]{settings.Action}[/]'. Use 'sync' or 'clean'.");
            return 1;
        }
        catch (UnauthorizedAccessException)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Permission denied. Run with elevated privileges (sudo/administrator).");
            return 2;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }

    private async Task<int> SyncAsync(CancellationToken cancellationToken)
    {
        var routes = await _routeStore.LoadRoutesAsync(cancellationToken);
        var hostsPath = GetHostsPath();

        if (routes.Length == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No active routes to sync.[/]");
            return 0;
        }

        // Read current hosts file
        var hostsContent = await File.ReadAllTextAsync(hostsPath, cancellationToken);
        var lines = hostsContent.Split('\n').ToList();

        // Remove existing portless block
        RemovePortlessBlock(lines);

        // Build new portless block
        var portlessBlock = new List<string>
        {
            HostsMarkerStart
        };

        foreach (var route in routes.OrderBy(r => r.Hostname))
        {
            portlessBlock.Add($"127.0.0.1 {route.Hostname}");
        }

        portlessBlock.Add(HostsMarkerEnd);

        // Append block
        lines.AddRange(portlessBlock);

        // Write back
        await File.WriteAllTextAsync(hostsPath, string.Join("\n", lines), cancellationToken);

        AnsiConsole.MarkupLine($"[green]Synced[/] {routes.Length} route(s) to [blue]{hostsPath}[/]");
        foreach (var route in routes.OrderBy(r => r.Hostname))
        {
            AnsiConsole.MarkupLine($"  [dim]127.0.0.1[/] {route.Hostname}");
        }

        return 0;
    }

    private async Task<int> CleanAsync(CancellationToken cancellationToken)
    {
        var hostsPath = GetHostsPath();

        if (!File.Exists(hostsPath))
        {
            AnsiConsole.MarkupLine($"[yellow]Hosts file not found:[/] {hostsPath}");
            return 1;
        }

        var hostsContent = await File.ReadAllTextAsync(hostsPath, cancellationToken);
        var lines = hostsContent.Split('\n').ToList();

        if (!lines.Any(l => l.Trim() == HostsMarkerStart))
        {
            AnsiConsole.MarkupLine("[yellow]No portless entries found in hosts file.[/]");
            return 0;
        }

        RemovePortlessBlock(lines);

        await File.WriteAllTextAsync(hostsPath, string.Join("\n", lines), cancellationToken);

        AnsiConsole.MarkupLine($"[green]Cleaned[/] portless entries from [blue]{hostsPath}[/]");
        return 0;
    }

    private static void RemovePortlessBlock(List<string> lines)
    {
        var startIdx = lines.FindIndex(l => l.Trim() == HostsMarkerStart);
        if (startIdx == -1) return;

        var endIdx = lines.FindIndex(startIdx, l => l.Trim() == HostsMarkerEnd);
        if (endIdx == -1) return;

        // Remove lines from startIdx to endIdx (inclusive)
        var count = endIdx - startIdx + 1;
        lines.RemoveRange(startIdx, count);

        // Remove trailing empty lines left behind
        while (startIdx < lines.Count && string.IsNullOrWhiteSpace(lines[startIdx]))
        {
            lines.RemoveAt(startIdx);
        }
    }

    private static string GetHostsPath()
    {
        return OperatingSystem.IsWindows() ? WindowsHostsPath : DefaultHostsPath;
    }
}
