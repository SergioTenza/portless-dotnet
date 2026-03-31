using Portless.Core.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Text.Json;

namespace Portless.Cli.Commands.GetCommand;

public class GetCommand : AsyncCommand<GetSettings>
{
    private readonly IRouteStore _routeStore;

    public GetCommand(IRouteStore routeStore)
    {
        _routeStore = routeStore;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, GetSettings settings, CancellationToken cancellationToken)
    {
        try
        {
            var routes = await _routeStore.LoadRoutesAsync(cancellationToken);

            // Normalize name: ensure .localhost suffix
            var hostname = settings.Name.EndsWith(".localhost")
                ? settings.Name
                : $"{settings.Name}.localhost";

            var route = routes.FirstOrDefault(r =>
                r.Hostname.Equals(hostname, StringComparison.OrdinalIgnoreCase));

            if (route == null)
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Route '[yellow]{settings.Name}[/]' not found.");
                AnsiConsole.MarkupLine("[dim]Use 'portless list' to see active routes.[/]");
                return 1;
            }

            var url = $"http://{route.Hostname}";

            if (settings.Json || Console.IsOutputRedirected)
            {
                var json = JsonSerializer.Serialize(new
                {
                    name = route.Hostname.Replace(".localhost", ""),
                    hostname = route.Hostname,
                    url,
                    port = route.Port,
                    pid = route.Pid
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                Console.WriteLine(json);
            }
            else
            {
                // Just print the URL for easy copy/paste or scripting
                AnsiConsole.MarkupLine($"[green]{url}[/]");
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            return 1;
        }
    }
}
