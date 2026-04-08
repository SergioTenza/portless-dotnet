using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Portless.Core.Serialization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Portless.Cli.Commands.TcpCommand;

public class TcpCommand : AsyncCommand<TcpSettings>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TcpCommand> _logger;

    public TcpCommand(IHttpClientFactory httpClientFactory, ILogger<TcpCommand> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, TcpSettings settings, CancellationToken ct)
    {
        if (settings.Remove)
        {
            return await RemoveTcpAsync(settings.Name!, ct);
        }

        if (string.IsNullOrEmpty(settings.Name) || string.IsNullOrEmpty(settings.Target))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Name and target are required");
            AnsiConsole.MarkupLine("[dim]Usage: portless tcp <name> <host:port> --listen <port>[/]");
            return 1;
        }

        if (!settings.ListenPort.HasValue)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] --listen port is required");
            return 1;
        }

        var parts = settings.Target.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[1], out var targetPort))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Target must be in host:port format (e.g. localhost:6379)");
            return 1;
        }

        var targetHost = parts[0];

        try
        {
            var client = _httpClientFactory.CreateClient();
            var payload = new TcpProxyPayload(settings.Name, settings.ListenPort.Value, targetHost, targetPort);
            var json = JsonSerializer.Serialize(payload, PortlessJsonContext.Default.TcpProxyPayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("http://localhost:1355/api/v1/tcp/add", content, ct);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                AnsiConsole.MarkupLine($"[red]Error:[/] Failed to start TCP proxy: {error}");
                return 1;
            }

            AnsiConsole.MarkupLine($"[green]✓[/] TCP proxy [blue]{settings.Name}[/] listening on port {settings.ListenPort} -> {targetHost}:{targetPort}");
            return 0;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Proxy not running: {ex.Message}");
            return 1;
        }
    }

    private async Task<int> RemoveTcpAsync(string name, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(name))
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Name is required for removal");
            return 1;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            await client.DeleteAsync($"http://localhost:1355/api/v1/tcp/remove?name={name}", ct);
            AnsiConsole.MarkupLine($"[green]TCP proxy '{name}' removed[/]");
            return 0;
        }
        catch (HttpRequestException ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] Proxy not running: {ex.Message}");
            return 1;
        }
    }
}
