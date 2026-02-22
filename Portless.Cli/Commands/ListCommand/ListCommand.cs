using Portless.Core.Models;
using Portless.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics;
using System.Text.Json;

namespace Portless.Cli.Commands.ListCommand;

[Description("List all active routes with their hostnames, ports, and process IDs")]
public class ListCommand : AsyncCommand<ListSettings>
{
    private readonly IRouteStore _routeStore;

    public ListCommand(IRouteStore routeStore)
    {
        _routeStore = routeStore;
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
            var isAlive = IsProcessAlive(route.Pid);
            var status = isAlive ? "[green]●[/]" : "[red]○[/]";

            table.AddRow($"{status} {name}", url, port, pid);
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine("\n[dim]All routes support HTTP/2 and WebSocket protocols.[/]");
    }

    private void RenderJson(RouteInfo[] routes)
    {
        var jsonRoutes = routes.Select(r => new
        {
            name = r.Hostname.Replace(".localhost", ""),
            hostname = r.Hostname,
            url = $"http://{r.Hostname}",
            port = r.Port,
            pid = r.Pid,
            created_at = r.CreatedAt.ToString("o"), // ISO 8601
            last_seen = r.LastSeen?.ToString("o")
        });

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(jsonRoutes, options);
        Console.WriteLine(json);
    }

    private static bool IsProcessAlive(int pid)
    {
        try
        {
            var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
