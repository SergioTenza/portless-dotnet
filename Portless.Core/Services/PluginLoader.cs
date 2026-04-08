using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using Portless.Plugin.SDK;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Portless.Core.Services;

/// <summary>
/// Runtime plugin loader that discovers, loads, and manages plugin lifecycles
/// using collectible AssemblyLoadContext for hot-reload support.
/// </summary>
public sealed class PluginLoader : IPluginLoader
{
    private readonly ILogger<PluginLoader> _logger;
    private readonly List<LoadedPlugin> _loadedPlugins = [];
    private readonly object _lock = new();

    public PluginLoader(ILogger<PluginLoader> logger)
    {
        _logger = logger;
    }

    // Internal for testing: allows injecting test plugins without reflection
    internal void AddPluginForTesting(PortlessPlugin plugin)
    {
        lock (_lock)
        {
            _loadedPlugins.Add(new LoadedPlugin(plugin, null, new PluginManifest
            {
                Name = plugin.Name,
                Version = plugin.Version
            }));
        }
    }

    public async Task LoadAllAsync(string pluginsPath)
    {
        if (!Directory.Exists(pluginsPath))
        {
            _logger.LogInformation("Plugins directory not found: {Path}. Skipping plugin loading.", pluginsPath);
            return;
        }

        var pluginDirs = Directory.GetDirectories(pluginsPath);
        _logger.LogInformation("Scanning {Count} plugin directories in {Path}", pluginDirs.Length, pluginsPath);

        foreach (var dir in pluginDirs)
        {
            try
            {
                await LoadAsync(dir);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load plugin from {Path}", dir);
            }
        }

        _logger.LogInformation("Loaded {Count} plugins", _loadedPlugins.Count);
    }

    public async Task<PortlessPlugin?> LoadAsync(string pluginPath)
    {
        var manifestPath = Path.Combine(pluginPath, "plugin.yaml");
        if (!File.Exists(manifestPath))
        {
            _logger.LogWarning("No plugin.yaml found in {Path}. Skipping.", pluginPath);
            return null;
        }

        // Parse manifest
        var manifest = ParseManifest(manifestPath);
        if (manifest == null || string.IsNullOrEmpty(manifest.EntryAssembly))
        {
            _logger.LogWarning("Invalid or incomplete plugin.yaml in {Path}", pluginPath);
            return null;
        }

        var assemblyPath = Path.Combine(pluginPath, manifest.EntryAssembly);
        if (!File.Exists(assemblyPath))
        {
            _logger.LogError("Plugin assembly not found: {AssemblyPath}", assemblyPath);
            return null;
        }

        // Load assembly in collectible context for hot-reload
        var context = new PluginAssemblyLoadContext(assemblyPath, isCollectible: true);
        var assembly = context.LoadFromAssemblyPath(assemblyPath);

        // Find PortlessPlugin subclass
        var pluginType = assembly.GetTypes()
            .FirstOrDefault(t => !t.IsAbstract && t.IsSubclassOf(typeof(PortlessPlugin)));

        if (pluginType == null)
        {
            _logger.LogError("No PortlessPlugin subclass found in {Assembly}", assemblyPath);
            context.Unload();
            return null;
        }

        var plugin = (PortlessPlugin)Activator.CreateInstance(pluginType)!;

        // Create plugin context
        var pluginContext = new DefaultPluginContext(manifest.Config, pluginPath);

        // Call OnLoad
        await plugin.OnLoadAsync(pluginContext);

        var loaded = new LoadedPlugin(plugin, context, manifest);
        lock (_lock)
        {
            // Remove existing plugin with same name (hot-reload)
            var existing = _loadedPlugins.FirstOrDefault(p => p.Plugin.Name == plugin.Name);
            if (existing != null)
            {
                _loadedPlugins.Remove(existing);
                existing.Context?.Unload();
            }
            _loadedPlugins.Add(loaded);
        }

        _logger.LogInformation("Plugin loaded: {Name} v{Version} from {Path}", plugin.Name, plugin.Version, pluginPath);
        return plugin;
    }

    public async Task UnloadAsync(string pluginName)
    {
        LoadedPlugin? toRemove;
        lock (_lock)
        {
            toRemove = _loadedPlugins.FirstOrDefault(p =>
                p.Plugin.Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase));
        }

        if (toRemove == null)
        {
            _logger.LogWarning("Plugin not found: {Name}", pluginName);
            return;
        }

        await toRemove.Plugin.OnUnloadAsync();

        lock (_lock)
        {
            _loadedPlugins.Remove(toRemove);
        }

        toRemove.Context?.Unload();
        _logger.LogInformation("Plugin unloaded: {Name}", pluginName);
    }

    public IReadOnlyList<PortlessPlugin> GetLoadedPlugins()
    {
        lock (_lock)
        {
            return _loadedPlugins.Select(p => p.Plugin).ToList().AsReadOnly();
        }
    }

    public async Task<ProxyResult?> FireBeforeProxyAsync(ProxyContext context)
    {
        List<PortlessPlugin> plugins;
        lock (_lock) { plugins = [.. _loadedPlugins.Select(p => p.Plugin)]; }

        foreach (var plugin in plugins)
        {
            try
            {
                var result = await plugin.BeforeProxyAsync(context);
                if (result != null)
                {
                    _logger.LogDebug("Plugin {Name} short-circuited request to {Host}{Path}",
                        plugin.Name, context.Hostname, context.Path);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in plugin {Name}.BeforeProxyAsync", plugin.Name);
            }
        }

        return null;
    }

    public async Task FireAfterProxyAsync(ProxyContext context, ProxyResult result)
    {
        List<PortlessPlugin> plugins;
        lock (_lock) { plugins = [.. _loadedPlugins.Select(p => p.Plugin)]; }

        foreach (var plugin in plugins)
        {
            try
            {
                await plugin.AfterProxyAsync(context, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in plugin {Name}.AfterProxyAsync", plugin.Name);
            }
        }
    }

    public async Task FireRouteAddedAsync(RouteInfo route)
    {
        List<PortlessPlugin> plugins;
        lock (_lock) { plugins = [.. _loadedPlugins.Select(p => p.Plugin)]; }

        foreach (var plugin in plugins)
        {
            try
            {
                await plugin.OnRouteAddedAsync(route);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in plugin {Name}.OnRouteAddedAsync", plugin.Name);
            }
        }
    }

    public async Task FireRouteRemovedAsync(RouteInfo route)
    {
        List<PortlessPlugin> plugins;
        lock (_lock) { plugins = [.. _loadedPlugins.Select(p => p.Plugin)]; }

        foreach (var plugin in plugins)
        {
            try
            {
                await plugin.OnRouteRemovedAsync(route);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in plugin {Name}.OnRouteRemovedAsync", plugin.Name);
            }
        }
    }

    public async Task<ErrorResponse?> FireErrorAsync(ErrorContext context)
    {
        List<PortlessPlugin> plugins;
        lock (_lock) { plugins = [.. _loadedPlugins.Select(p => p.Plugin)]; }

        foreach (var plugin in plugins)
        {
            try
            {
                var response = await plugin.OnErrorAsync(context);
                if (response != null)
                {
                    _logger.LogDebug("Plugin {Name} handled error for {Host}{Path}",
                        plugin.Name, context.Hostname, context.Path);
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in plugin {Name}.OnErrorAsync", plugin.Name);
            }
        }

        return null;
    }

    private PluginManifest? ParseManifest(string yamlPath)
    {
        try
        {
            var yaml = File.ReadAllText(yamlPath);
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            return deserializer.Deserialize<PluginManifest>(yaml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse plugin manifest: {Path}", yamlPath);
            return null;
        }
    }

    private sealed record LoadedPlugin(PortlessPlugin Plugin, PluginAssemblyLoadContext? Context, PluginManifest Manifest);
}

/// <summary>
/// Collectible AssemblyLoadContext for plugin hot-reload.
/// </summary>
internal sealed class PluginAssemblyLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginAssemblyLoadContext(string pluginPath, bool isCollectible)
        : base(isCollectible: isCollectible)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Try to resolve from plugin dependencies first
        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        if (path != null)
        {
            return LoadFromAssemblyPath(path);
        }

        // Fall back to default context (shared dependencies like SDK)
        return null;
    }
}

/// <summary>Default implementation of IPluginContext.</summary>
internal sealed class DefaultPluginContext : IPluginContext
{
    public IReadOnlyDictionary<string, string> Config { get; }
    public string PluginDirectory { get; }

    public DefaultPluginContext(Dictionary<string, string> config, string pluginDirectory)
    {
        Config = config.AsReadOnly();
        PluginDirectory = pluginDirectory;
    }
}
