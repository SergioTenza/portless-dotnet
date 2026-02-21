using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Portless.Core.Models;

namespace Portless.Core.Services;

/// <summary>
/// Background service that monitors process health every 5 seconds and performs coordinated cleanup.
/// </summary>
public class ProcessHealthMonitor : BackgroundService
{
    private readonly IRouteStore _routeStore;
    private readonly IPortAllocator _portAllocator;
    private readonly ILogger<ProcessHealthMonitor> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);

    public ProcessHealthMonitor(
        IRouteStore routeStore,
        IPortAllocator portAllocator,
        ILogger<ProcessHealthMonitor> logger)
    {
        _routeStore = routeStore;
        _portAllocator = portAllocator;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Process health monitor starting with {Interval}s polling interval",
            _pollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_pollingInterval, stoppingToken);
                await CheckProcessHealthAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during process health check");
            }
        }

        _logger.LogInformation("Process health monitor stopping");
    }

    private async Task CheckProcessHealthAsync(CancellationToken cancellationToken)
    {
        var routes = await _routeStore.LoadRoutesAsync(cancellationToken);
        var deadRoutes = routes.Where(r => !IsProcessAlive(r)).ToArray();

        if (deadRoutes.Any())
        {
            _logger.LogInformation("Detected {DeadCount} dead processes out of {Total} total routes",
                deadRoutes.Length, routes.Length);

            // Coordinated cleanup: release ports AND remove routes atomically
            foreach (var route in deadRoutes)
            {
                await _portAllocator.ReleasePortAsync(route.Port);
                _logger.LogDebug("Released port {Port} from dead process {Pid} ({Hostname})",
                    route.Port, route.Pid, route.Hostname);
            }

            var aliveRoutes = routes.Except(deadRoutes).ToArray();
            await _routeStore.SaveRoutesAsync(aliveRoutes, cancellationToken);

            _logger.LogInformation("Cleanup complete: {AliveCount} routes remaining",
                aliveRoutes.Length);
        }
        else
        {
            _logger.LogDebug("Process health check complete: {Count} processes alive",
                routes.Length);
        }
    }

    private static bool IsProcessAlive(RouteInfo route)
    {
        try
        {
            var process = Process.GetProcessById(route.Pid);

            // Check if process has exited
            if (process.HasExited)
            {
                return false;
            }

            // PID recycling detection: if process started after route creation + 1s buffer,
            // the PID was reused by a different process
            if (process.StartTime > route.CreatedAt + TimeSpan.FromSeconds(1))
            {
                return false; // PID was recycled
            }

            // Update LastSeen timestamp for future validation
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
