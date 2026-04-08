namespace Portless.Plugin.SDK;

/// <summary>
/// Abstract base class for all Portless plugins.
/// Override the lifecycle methods you need; all defaults are no-ops.
/// </summary>
public abstract class PortlessPlugin
{
    /// <summary>
    /// Human-readable name of the plugin.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Semantic version of the plugin (e.g. "1.0.0").
    /// </summary>
    public abstract string Version { get; }

    /// <summary>
    /// Called once when the plugin is loaded into the host.
    /// Use this to initialise state, read configuration, etc.
    /// </summary>
    /// <param name="context">Provides access to plugin configuration and directory.</param>
    /// <param name="cancellationToken">Token signalled when the host is shutting down.</param>
    public virtual Task OnLoadAsync(IPluginContext context) => Task.CompletedTask;

    /// <summary>
    /// Called once when the plugin is being unloaded (host shutdown or plugin hot-reload).
    /// Release any resources here.
    /// </summary>
    public virtual Task OnUnloadAsync() => Task.CompletedTask;

    /// <summary>
    /// Called before a proxy request is forwarded to the backend.
    /// Return a non-null <see cref="ProxyResult"/> to short-circuit the request
    /// (the backend will NOT be called); return null to continue normal proxying.
    /// </summary>
    /// <param name="context">Describes the incoming request.</param>
    /// <returns>A <see cref="ProxyResult"/> to short-circuit, or null to continue.</returns>
    public virtual Task<ProxyResult?> BeforeProxyAsync(ProxyContext context) => Task.FromResult<ProxyResult?>(null);

    /// <summary>
    /// Called after the backend has responded (or after <see cref="BeforeProxyAsync"/> short-circuited).
    /// Use this for logging, metrics, or post-processing.
    /// </summary>
    /// <param name="context">Describes the original request.</param>
    /// <param name="result">The result returned to the client.</param>
    public virtual Task AfterProxyAsync(ProxyContext context, ProxyResult result) => Task.CompletedTask;

    /// <summary>
    /// Called when a new route is added to the proxy configuration at runtime.
    /// </summary>
    /// <param name="route">The route that was added.</param>
    public virtual Task OnRouteAddedAsync(RouteInfo route) => Task.CompletedTask;

    /// <summary>
    /// Called when a route is removed from the proxy configuration at runtime.
    /// </summary>
    /// <param name="route">The route that was removed.</param>
    public virtual Task OnRouteRemovedAsync(RouteInfo route) => Task.CompletedTask;

    /// <summary>
    /// Called when an error occurs during proxying.
    /// Return a non-null <see cref="ErrorResponse"/> to customise the error sent to the client;
    /// return null to let the host use its default error handling.
    /// </summary>
    /// <param name="context">Describes the error context.</param>
    /// <returns>A <see cref="ErrorResponse"/> to send to the client, or null for default handling.</returns>
    public virtual Task<ErrorResponse?> OnErrorAsync(ErrorContext context) => Task.FromResult<ErrorResponse?>(null);
}
