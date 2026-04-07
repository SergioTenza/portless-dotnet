using Portless.Core.Models;
using Portless.Core.Serialization;
using Portless.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Text.Json;

namespace Portless.Cli.Commands.ListCommand;

public class ListCommand : AsyncCommand<ListSettings>
{
    private readonly IRouteStore _routeStore;
    private readonly IProcessLivenessChecker _processLivenessChecker;

    public ListCommand(IRouteStore routeStore, IProcessLivenessChecker processLivenessChecker)
    {
        _routeStore = routeStore;
        _processLivenessChecker = processLivenessChecker;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, ListSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            // Load routes from the route store
            var routes = await _routeStore.LoadRoutesAsync();

            // Handle empty state
            if (routes.Length == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No active routes.[/]");
                AnsiConsole.MarkupLine("Use [yellow]'portless run <name> <command>'[/] to start an app.");
                return 0;
            }

            // Detect output redirection
            if (!Console.IsOutputRedirected)
            {
                // TTY output - render table
                RenderTable(routes);
            }
            else
            {
                // Redirected output - render JSON
                RenderJson(routes);
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Failed to load routes.");
            AnsiConsole.MarkupLine($"[dim]{ex.Message}[/]");
            return 1;
        }
    }

    private void RenderTable(RouteInfo[] routes)
    {
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.Expand(); // Use full width

        // Add columns
        table.AddColumn(new TableColumn("[yellow]Name[/]").Width(20));
        table.AddColumn(new TableColumn("[yellow]URL[/]").Width(35));
        table.AddColumn(new TableColumn("[yellow]Port[/]").Centered());
        table.AddColumn(new TableColumn("[yellow]PID[/]").Centered());

        // Add rows
        foreach (var route in routes.OrderBy(r => r.Hostname))
        {
            var name = route.Hostname.Replace(".localhost", "");
            var url = $"[blue]http://{route.Hostname}[/]";
            var port = route.Port.ToString();
            var pid = route.Pid.ToString();

            // Add status indicator based on PID liveness
            var isAlive = _processLivenessChecker.IsAlive(route);
            var status = isAlive ? "[green]●[/]" : "[red]○[/]";

            table.AddRow($"{status} {name}", url, port, pid);
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("\n[dim]All routes support HTTP/2 and WebSocket protocols.[/]");
    }

    private void RenderJson(RouteInfo[] routes)
    {
        var jsonRoutes = routes.Select(r => new RouteListEntry(
            Name: r.Hostname.Replace(".localhost", ""),
            Hostname: r.Hostname,
            Url: $"http://{r.Hostname}",
            Port: r.Port,
            Pid: r.Pid,
            CreatedAt: r.CreatedAt.ToString("o"),
            LastSeen: r.LastSeen?.ToString("o")
        )).ToArray();

        // Use source-generated context with indented option
        var options = new JsonSerializerOptions(PortlessJsonContext.Default.Options)
        {
            WriteIndented = true
        };
        var context = new PortlessJsonContext(options);

        var json = JsonSerializer.Serialize(jsonRoutes, context.RouteListEntryArray);
        Console.WriteLine(json);
    }

}
