using Spectre.Console;
using Spectre.Console.Cli;
using System.Net.Http.Json;

namespace Portless.Cli.Commands.InspectCommand;

public sealed class InspectCommand : AsyncCommand<InspectSettings>
{
    private static readonly string ProxyBaseUrl = $"http://localhost:{Environment.GetEnvironmentVariable("PORTLESS_PORT") ?? "1355"}";

    public override async Task<int> ExecuteAsync(CommandContext context, InspectSettings settings, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(settings.SavePath))
        {
            return await SaveSessions(settings);
        }

        if (settings.Live)
        {
            return await LiveStream(settings);
        }

        return await ShowRecent(settings);
    }

    private async Task<int> ShowRecent(InspectSettings settings)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await http.GetAsync($"{ProxyBaseUrl}/api/v1/inspect/sessions?count={settings.Count}");
            response.EnsureSuccessStatusCode();

            var sessions = await response.Content.ReadFromJsonAsync<List<SessionSummary>>();
            if (sessions == null || sessions.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No captured requests. Make sure the proxy is running and handling traffic.[/]");
                return 0;
            }

            // Apply filters
            var filtered = ApplyFilters(sessions, settings.Filter);

            // Get stats
            var statsResponse = await http.GetAsync($"{ProxyBaseUrl}/api/v1/inspect/stats");
            var stats = statsResponse.IsSuccessStatusCode
                ? await statsResponse.Content.ReadFromJsonAsync<InspectStats>()
                : null;

            var table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold]Portless Inspector[/]")
                .AddColumn("Time")
                .AddColumn("Method")
                .AddColumn("Host + Path")
                .AddColumn("Status")
                .AddColumn("Duration");

            foreach (var s in filtered)
            {
                var statusColor = s.StatusCode switch
                {
                    >= 200 and < 300 => "green",
                    >= 300 and < 400 => "blue",
                    >= 400 and < 500 => "yellow",
                    _ => "red"
                };

                table.AddRow(
                    Markup.Escape(s.Timestamp.ToString("HH:mm:ss")),
                    Markup.Escape(s.Method),
                    Markup.Escape(Truncate($"{s.Hostname}{s.Path}", 40)),
                    $"[{statusColor}]{s.StatusCode}[/]",
                    $"{s.DurationMs}ms"
                );
            }

            AnsiConsole.Write(table);

            if (stats != null)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[dim]Total: {stats.TotalCaptured} captured | Avg: {stats.AvgDurationMs}ms | Error rate: {stats.ErrorRate:P1}[/]");
            }

            return 0;
        }
        catch (HttpRequestException)
        {
            AnsiConsole.MarkupLine("[red]Error: Cannot connect to proxy. Is it running?[/]");
            return 1;
        }
    }

    private async Task<int> SaveSessions(InspectSettings settings)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await http.GetAsync($"{ProxyBaseUrl}/api/v1/inspect/sessions?count={settings.Count}");
            response.EnsureSuccessStatusCode();

            var sessions = await response.Content.ReadFromJsonAsync<List<SessionSummary>>();
            if (sessions == null || sessions.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No captured requests to save.[/]");
                return 0;
            }

            var filtered = ApplyFilters(sessions, settings.Filter);
            using var writer = new StreamWriter(settings.SavePath!, append: false);
            foreach (var s in filtered)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(s);
                await writer.WriteLineAsync(json);
            }

            AnsiConsole.MarkupLine($"[green]Saved {filtered.Count} sessions to {Markup.Escape(settings.SavePath!)}[/]");
            return 0;
        }
        catch (HttpRequestException)
        {
            AnsiConsole.MarkupLine("[red]Error: Cannot connect to proxy.[/]");
            return 1;
        }
    }

    private async Task<int> LiveStream(InspectSettings settings)
    {
        AnsiConsole.MarkupLine("[bold]Portless Inspector - Live Mode[/]");
        AnsiConsole.MarkupLine("[dim]Press Ctrl+C to stop[/]");
        AnsiConsole.WriteLine();

        try
        {
            // Use polling for live mode (simpler than WebSocket in CLI)
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var seen = new HashSet<Guid>();
            var running = true;
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; running = false; };

            // Initial load
            var response = await http.GetAsync($"{ProxyBaseUrl}/api/v1/inspect/sessions?count={settings.Count}");
            var sessions = await response.Content.ReadFromJsonAsync<List<SessionSummary>>() ?? [];
            foreach (var s in sessions) seen.Add(s.Id);

            var table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Time")
                .AddColumn("Method")
                .AddColumn("Host + Path")
                .AddColumn("Status")
                .AddColumn("Duration");

            var entries = new List<string[]>();
            var maxRows = settings.Count;

            await AnsiConsole.Live(table)
                .StartAsync(async ctx =>
                {
                    while (running)
                    {
                        await Task.Delay(1000);

                        try
                        {
                            var resp = await http.GetAsync($"{ProxyBaseUrl}/api/v1/inspect/sessions?count={settings.Count}");
                            if (!resp.IsSuccessStatusCode) continue;

                            var all = await resp.Content.ReadFromJsonAsync<List<SessionSummary>>() ?? [];
                            var newSessions = all.Where(s => !seen.Contains(s.Id)).ToList();

                            foreach (var s in newSessions)
                            {
                                if (!MatchesFilter(s, settings.Filter)) continue;
                                seen.Add(s.Id);

                                var statusColor = s.StatusCode switch
                                {
                                    >= 200 and < 300 => "green",
                                    >= 300 and < 400 => "blue",
                                    >= 400 and < 500 => "yellow",
                                    _ => "red"
                                };

                                entries.Add([
                                    s.Timestamp.ToString("HH:mm:ss"),
                                    s.Method,
                                    Truncate($"{s.Hostname}{s.Path}", 40),
                                    $"{s.StatusCode}",
                                    $"{s.DurationMs}ms"
                                ]);

                                if (entries.Count > maxRows)
                                    entries.RemoveAt(0);
                            }

                            // Rebuild table
                            table.Rows.Clear();
                            foreach (var row in entries)
                            {
                                table.AddRow(row.Select(Markup.Escape).ToArray());
                            }

                            ctx.Refresh();
                        }
                        catch
                        {
                            // Continue on transient errors
                        }
                    }
                });

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(ex.Message)}[/]");
            return 1;
        }
    }

    private static List<SessionSummary> ApplyFilters(List<SessionSummary> sessions, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) return sessions;
        return sessions.Where(s => MatchesFilter(s, filter)).ToList();
    }

    private static bool MatchesFilter(SessionSummary s, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) return true;

        // Support comma-separated filters: host:api.*,method:POST,status:5xx
        var parts = filter.Split(',');
        foreach (var part in parts)
        {
            var kv = part.Split(':', 2);
            if (kv.Length != 2) continue;

            var key = kv[0].Trim().ToLowerInvariant();
            var value = kv[1].Trim();

            switch (key)
            {
                case "host":
                    if (!s.Hostname.Contains(value, StringComparison.OrdinalIgnoreCase) &&
                        !LikeMatch(s.Hostname, value)) return false;
                    break;
                case "method":
                    if (!s.Method.Equals(value, StringComparison.OrdinalIgnoreCase)) return false;
                    break;
                case "status":
                    if (!StatusMatches(s.StatusCode, value)) return false;
                    break;
                case "path":
                    if (!s.Path.Contains(value, StringComparison.OrdinalIgnoreCase)) return false;
                    break;
            }
        }
        return true;
    }

    private static bool StatusMatches(int statusCode, string pattern) => pattern.ToLowerInvariant() switch
    {
        "2xx" => statusCode is >= 200 and < 300,
        "3xx" => statusCode is >= 300 and < 400,
        "4xx" => statusCode is >= 400 and < 500,
        "5xx" => statusCode is >= 500 and < 600,
        _ => int.TryParse(pattern, out var code) && statusCode == code
    };

    private static bool LikeMatch(string input, string pattern)
    {
        if (!pattern.Contains('*')) return false;
        var parts = pattern.Split('*');
        if (parts.Length == 2)
        {
            return input.StartsWith(parts[0], StringComparison.OrdinalIgnoreCase) &&
                   input.EndsWith(parts[1], StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    private static string Truncate(string s, int maxLen) =>
        s.Length <= maxLen ? s : s[..maxLen] + "...";
}

internal record SessionSummary(
    Guid Id,
    DateTime Timestamp,
    string Method,
    string Hostname,
    string Path,
    string Scheme,
    int StatusCode,
    long DurationMs,
    string RouteId,
    int? RequestBodySize,
    int? ResponseBodySize
);

internal record InspectStats(int TotalCaptured, double AvgDurationMs, double ErrorRate);
