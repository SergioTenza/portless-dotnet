using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Portless.Core.Models;

namespace Portless.Core.Services;

/// <summary>
/// Background service that periodically checks the health of route backends
/// by making HTTP HEAD requests to their local ports.
/// </summary>
public class RouteHealthChecker : BackgroundService, IRouteHealthChecker
{
    private readonly IRouteStore _routeStore;
    private readonly ILogger<RouteHealthChecker> _logger;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, RouteHealthStatus> _healthStatuses = new();
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(10);
    private readonly TimeSpan _requestTimeout = TimeSpan.FromSeconds(2);

    public RouteHealthChecker(
        IRouteStore routeStore,
        ILogger<RouteHealthChecker> logger)
    {
        _routeStore = routeStore;
        _logger = logger;
        _httpClient = new HttpClient
        {
            Timeout = _requestTimeout
        };
    }

    public Task<RouteHealthStatus> CheckHealthAsync(string hostname)
    {
        _healthStatuses.TryGetValue(hostname, out var status);
        return Task.FromResult(status);
    }

    public IReadOnlyDictionary<string, RouteHealthStatus> GetAllHealthStatuses()
    {
        return _healthStatuses;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Route health checker starting with {Interval}s polling interval",
            _pollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_pollingInterval, stoppingToken);
                await CheckAllRoutesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during route health check");
            }
        }

        _logger.LogInformation("Route health checker stopping");
    }

    private async Task CheckAllRoutesAsync(CancellationToken cancellationToken)
    {
        var routes = await _routeStore.LoadRoutesAsync(cancellationToken);

        foreach (var route in routes)
        {
            // Skip TCP routes - they don't have HTTP backends
            if (route.Type == RouteType.Tcp)
                continue;

            var status = await CheckRouteBackendAsync(route);
            _healthStatuses[route.Hostname] = status;
        }

        _logger.LogDebug("Route health check complete: {Count} HTTP routes checked",
            routes.Count(r => r.Type != RouteType.Tcp));
    }

    private async Task<RouteHealthStatus> CheckRouteBackendAsync(RouteInfo route)
    {
        try
        {
            var healthUrl = $"http://localhost:{route.Port}/";
            using var response = await _httpClient.SendAsync(
                new HttpRequestMessage(HttpMethod.Head, healthUrl),
                HttpCompletionOption.ResponseHeadersRead);

            _logger.LogDebug("Health check for {Hostname} (port {Port}): {StatusCode}",
                route.Hostname, route.Port, response.StatusCode);

            return RouteHealthStatus.Healthy;
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Health check for {Hostname} (port {Port}) failed: {Message}",
                route.Hostname, route.Port, ex.Message);

            return RouteHealthStatus.Unhealthy;
        }
    }

    public override void Dispose()
    {
        _httpClient.Dispose();
        base.Dispose();
    }
}
