using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Portless.Core.Models;

namespace Portless.Core.Services;

public class RouteCleanupService : BackgroundService
{
    private readonly IRouteStore _routeStore;
    private readonly ILogger<RouteCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromSeconds(30);

    public RouteCleanupService(
        IRouteStore routeStore,
        ILogger<RouteCleanupService> logger)
    {
        _routeStore = routeStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Route cleanup service starting with {Interval}s interval",
            _cleanupInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);

                var routes = await _routeStore.LoadRoutesAsync(stoppingToken);
                var aliveRoutes = routes.Where(IsProcessAlive).ToArray();

                if (aliveRoutes.Length != routes.Length)
                {
                    var deadCount = routes.Length - aliveRoutes.Length;
                    _logger.LogInformation("Cleaning up {DeadCount} dead routes (total: {Total})",
                        deadCount, routes.Length);

                    await _routeStore.SaveRoutesAsync(aliveRoutes, stoppingToken);

                    // Trigger YARP reload if config updater is available
                    // (This will be connected in Task 3)
                }
                else
                {
                    _logger.LogDebug("Route cleanup check complete: {Count} routes alive",
                        routes.Length);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during route cleanup cycle");
            }
        }

        _logger.LogInformation("Route cleanup service stopping");
    }

    private static bool IsProcessAlive(RouteInfo route)
    {
        try
        {
            // Process.GetProcessById throws ArgumentException if PID doesn't exist
            var process = Process.GetProcessById(route.Pid);

            // Check if process has exited
            if (process.HasExited)
                return false;

            // Validate PID hasn't been recycled by checking StartTime
            // If process started after route creation, PID was reused
            if (process.StartTime > route.CreatedAt + TimeSpan.FromSeconds(1))
            {
                return false; // PID recycled
            }

            // Update LastSeen for future validation
            route.LastSeen = DateTime.UtcNow;
            return true;
        }
        catch (ArgumentException)
        {
            // PID doesn't exist
            return false;
        }
    }
}
