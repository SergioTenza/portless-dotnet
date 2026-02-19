using System.IO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Yarp.ReverseProxy.Configuration;
using Portless.Core.Models;
using Portless.Proxy;

namespace Portless.Core.Services;

public class RouteFileWatcher : IHostedService, IDisposable
{
    private readonly IRouteStore _routeStore;
    private readonly DynamicConfigProvider _configProvider;
    private readonly ILogger<RouteFileWatcher> _logger;
    private readonly string _stateDirectory;
    private FileSystemWatcher? _watcher;
    private Timer? _debounceTimer;
    private const int DebounceMs = 500;

    public RouteFileWatcher(
        IRouteStore routeStore,
        DynamicConfigProvider configProvider,
        ILogger<RouteFileWatcher> logger)
    {
        _routeStore = routeStore;
        _configProvider = configProvider;
        _logger = logger;
        _stateDirectory = StateDirectoryProvider.GetStateDirectory();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Ensure state directory exists
        Directory.CreateDirectory(_stateDirectory);

        _watcher = new FileSystemWatcher(_stateDirectory)
        {
            Filter = "routes.json",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            IncludeSubdirectories = false
        };

        _watcher.Changed += OnFileChanged;
        _watcher.Error += OnWatcherError;
        _watcher.EnableRaisingEvents = true;

        _debounceTimer = new Timer(OnDebounceElapsed, null, Timeout.Infinite, Timeout.Infinite);

        _logger.LogInformation("Route file watcher started: {Path}", _stateDirectory);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _watcher?.EnableRaisingEvents = false;
        _watcher?.Dispose();
        _debounceTimer?.Dispose();

        _logger.LogInformation("Route file watcher stopped");

        return Task.CompletedTask;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogDebug("File changed event: {Name}, {ChangeType}", e.Name, e.ChangeType);

        // Reset debounce timer on each change
        _debounceTimer?.Change(DebounceMs, Timeout.Infinite);
    }

    private async void OnDebounceElapsed(object? state)
    {
        try
        {
            _logger.LogInformation("Reloading routes from file after debounce");

            var routes = await _routeStore.LoadRoutesAsync();

            // Convert RouteInfo[] to YARP RouteConfig[] and ClusterConfig[]
            var routeConfigs = new List<RouteConfig>();
            var clusterConfigs = new List<ClusterConfig>();

            foreach (var route in routes)
            {
                var routeConfig = new RouteConfig
                {
                    RouteId = $"route-{route.Hostname}",
                    ClusterId = $"cluster-{route.Hostname}",
                    Match = new RouteMatch
                    {
                        Hosts = new[] { route.Hostname },
                        Path = "/{**catch-all}"
                    }
                };

                var clusterConfig = new ClusterConfig
                {
                    ClusterId = $"cluster-{route.Hostname}",
                    Destinations = new Dictionary<string, DestinationConfig>
                    {
                        ["backend1"] = new DestinationConfig
                        {
                            Address = $"http://localhost:{route.Port}"
                        }
                    }
                };

                routeConfigs.Add(routeConfig);
                clusterConfigs.Add(clusterConfig);
            }

            // Update YARP configuration (triggers hot-reload)
            _configProvider.Update(routeConfigs, clusterConfigs);

            _logger.LogInformation("YARP configuration reloaded: {Count} routes", routes.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading routes from file");
        }
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        _logger.LogWarning(e.GetException(), "FileSystemWatcher error, may need to restart proxy");
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _debounceTimer?.Dispose();
    }
}
