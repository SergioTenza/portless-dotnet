using Portless.Plugin.SDK;

namespace Portless.Core.Services;

/// <summary>Manages plugin discovery, loading, and lifecycle.</summary>
public interface IPluginLoader
{
    /// <summary>Discovers and loads all plugins from the plugins directory.</summary>
    Task LoadAllAsync(string pluginsPath);

    /// <summary>Loads a single plugin from a directory path.</summary>
    Task<PortlessPlugin?> LoadAsync(string pluginPath);

    /// <summary>Unloads a plugin by name.</summary>
    Task UnloadAsync(string pluginName);

    /// <summary>Gets all loaded plugins.</summary>
    IReadOnlyList<PortlessPlugin> GetLoadedPlugins();

    /// <summary>Fires BeforeProxy hooks. Returns first non-null result (short-circuit).</summary>
    Task<ProxyResult?> FireBeforeProxyAsync(ProxyContext context);

    /// <summary>Fires AfterProxy hooks.</summary>
    Task FireAfterProxyAsync(ProxyContext context, ProxyResult result);

    /// <summary>Fires OnRouteAdded hooks.</summary>
    Task FireRouteAddedAsync(RouteInfo route);

    /// <summary>Fires OnRouteRemoved hooks.</summary>
    Task FireRouteRemovedAsync(RouteInfo route);

    /// <summary>Fires OnError hooks. Returns first non-null result.</summary>
    Task<ErrorResponse?> FireErrorAsync(ErrorContext context);
}
