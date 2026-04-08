using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Yarp.ReverseProxy.Configuration;
using Portless.Core.Configuration;
using Portless.Core.Models;

namespace Portless.Core.Services;

public class ConfigFileWatcher : IHostedService, IDisposable
{
    private readonly IPortlessConfigLoader _configLoader;
    private readonly DynamicConfigProvider _configProvider;
    private readonly IRouteStore _routeStore;
    private readonly ITcpForwardingService _tcpForwardingService;
    private readonly IYarpConfigFactory _configFactory;
    private readonly IMetricsService _metricsService;
    private readonly ILogger<ConfigFileWatcher> _logger;

    private FileSystemWatcher? _watcher;
    private Timer? _debounceTimer;
    private const int DebounceMs = 500;
    private string? _configFilePath;
    private string? _configDirectory;
    private readonly ConcurrentDictionary<string, int> _activeTcpListeners = new();

    public ConfigFileWatcher(
        IPortlessConfigLoader configLoader,
        DynamicConfigProvider configProvider,
        IRouteStore routeStore,
        ITcpForwardingService tcpForwardingService,
        IYarpConfigFactory configFactory,
        IMetricsService metricsService,
        ILogger<ConfigFileWatcher> logger)
    {
        _configLoader = configLoader;
        _configProvider = configProvider;
        _routeStore = routeStore;
        _tcpForwardingService = tcpForwardingService;
        _configFactory = configFactory;
        _metricsService = metricsService;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _configFilePath = _configLoader.FindConfigFile();

        if (_configFilePath != null)
        {
            _configDirectory = Path.GetDirectoryName(_configFilePath);
            _logger.LogInformation("Config file watcher started: {Path}", _configFilePath);
        }
        else
        {
            // No config file found yet; watch the current working directory for creation
            _configDirectory = Directory.GetCurrentDirectory();
            _logger.LogInformation("No config file found; watching directory for creation: {Path}", _configDirectory);
        }

        Directory.CreateDirectory(_configDirectory!);

        _watcher = new FileSystemWatcher(_configDirectory)
        {
            Filter = "portless.config.yaml",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.CreationTime,
            IncludeSubdirectories = false
        };

        _watcher.Created += OnFileChanged;
        _watcher.Changed += OnFileChanged;
        _watcher.Deleted += OnFileDeleted;
        _watcher.Renamed += OnFileRenamed;
        _watcher.Error += OnWatcherError;
        _watcher.EnableRaisingEvents = true;

        _debounceTimer = new Timer(OnDebounceElapsed, null, Timeout.Infinite, Timeout.Infinite);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _watcher?.EnableRaisingEvents = false;
        _watcher?.Dispose();
        _debounceTimer?.Dispose();

        _logger.LogInformation("Config file watcher stopped");

        return Task.CompletedTask;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogDebug("Config file changed: {Name}, {ChangeType}", e.Name, e.ChangeType);
        _configFilePath = e.FullPath;
        _debounceTimer?.Change(DebounceMs, Timeout.Infinite);
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        _logger.LogInformation("Config file deleted: {Path}", e.FullPath);
        _configFilePath = null;
        _debounceTimer?.Change(DebounceMs, Timeout.Infinite);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (e.OldFullPath.EndsWith("portless.config.yaml", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Config file renamed away: {OldPath}", e.OldFullPath);
            _configFilePath = null;
            _debounceTimer?.Change(DebounceMs, Timeout.Infinite);
        }

        if (e.FullPath.EndsWith("portless.config.yaml", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Config file renamed to: {NewPath}", e.FullPath);
            _configFilePath = e.FullPath;
            _debounceTimer?.Change(DebounceMs, Timeout.Infinite);
        }
    }

    private async void OnDebounceElapsed(object? state)
    {
        try
        {
            await ReloadConfigAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading config file");
        }
    }

    public async Task ReloadConfigAsync()
    {
        _logger.LogInformation("Reloading config from portless.config.yaml");

        // Load the config file
        var fileConfig = _configFilePath != null
            ? _configLoader.Load(_configFilePath)
            : _configLoader.Load();

        // Stop all previously started TCP listeners from config
        foreach (var kvp in _activeTcpListeners)
        {
            await _tcpForwardingService.StopListenerAsync(kvp.Key);
            _logger.LogDebug("Stopped TCP listener: {Name}", kvp.Key);
        }
        _activeTcpListeners.Clear();

        // Get current YARP config (routes from route store, etc.)
        var currentConfig = _configProvider.GetConfig();
        var allRoutes = currentConfig.Routes.ToList();
        var allClusters = currentConfig.Clusters.ToList();

        // Remove previously loaded config routes (they have "config-" prefix) before re-adding
        allRoutes.RemoveAll(r => r.RouteId.StartsWith("config-"));
        allClusters.RemoveAll(c => c.ClusterId.StartsWith("config-"));

        var configRouteInfos = _configLoader.ToRouteInfos(fileConfig);

        // Process HTTP routes
        var httpRoutes = configRouteInfos.Where(r => r.Type == RouteType.Http).ToArray();
        foreach (var route in httpRoutes)
        {
            var urls = route.GetBackendUrls();
            var (routeConfig, clusterConfig) = _configFactory.CreateRouteClusterPair(
                route.Hostname, urls, route.Path);

            // Prefix with "config-" to identify config-file routes
            var configRouteId = $"config-{routeConfig.RouteId}";
            var configClusterId = $"config-{clusterConfig.ClusterId}";

            routeConfig = routeConfig with { RouteId = configRouteId, ClusterId = configClusterId };
            clusterConfig = clusterConfig with { ClusterId = configClusterId };

            // Apply load balancing policy for multi-backend clusters
            if (urls.Length > 1)
            {
                clusterConfig = clusterConfig with
                {
                    LoadBalancingPolicy = route.LoadBalancingPolicy switch
                    {
                        LoadBalancingPolicy.RoundRobin => "RoundRobin",
                        LoadBalancingPolicy.LeastRequests => "LeastRequests",
                        LoadBalancingPolicy.Random => "Random",
                        LoadBalancingPolicy.First => "First",
                        _ => "PowerOfTwoChoices"
                    }
                };
            }

            allRoutes.Add(routeConfig);
            allClusters.Add(clusterConfig);
        }

        // Update YARP configuration
        _configProvider.Update(allRoutes, allClusters);
        _logger.LogInformation("Config reload: {HttpCount} HTTP routes from config file", httpRoutes.Length);

        // Start TCP listeners from config file
        var tcpRoutes = configRouteInfos.Where(r => r.Type == RouteType.Tcp).ToArray();
        foreach (var route in tcpRoutes)
        {
            if (route.TcpListenPort.HasValue && route.Port > 0)
            {
                var listenerName = $"config-{route.Hostname}";
                await _tcpForwardingService.StartListenerAsync(
                    listenerName,
                    route.TcpListenPort.Value,
                    "localhost",
                    route.Port);
                _activeTcpListeners[listenerName] = route.TcpListenPort.Value;
            }
        }

        if (tcpRoutes.Length > 0)
        {
            _logger.LogInformation("Config reload: {TcpCount} TCP proxy listeners started", tcpRoutes.Length);
        }

        // Update metrics
        var totalRoutes = allRoutes.Count;
        _metricsService.UpdateActiveRoutes(totalRoutes);

        var activeTcp = _tcpForwardingService.GetActiveListeners().Count;
        _metricsService.UpdateActiveTcpListeners(activeTcp);
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        _logger.LogWarning(e.GetException(), "Config file watcher error");
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _debounceTimer?.Dispose();
    }
}
