namespace Portless.Core.Services;

/// <summary>
/// Registers and removes routes with the proxy server.
/// Centralizes the HTTP communication between CLI and Proxy.
/// </summary>
public interface IProxyRouteRegistrar
{
    /// <summary>
    /// Registers a route with the proxy server.
    /// </summary>
    /// <param name="hostname">The hostname to register (e.g. "myapi.localhost").</param>
    /// <param name="backendUrl">The backend URL to forward to (e.g. "http://localhost:4042").</param>
    /// <returns>True if registration succeeded; false otherwise.</returns>
    Task<bool> RegisterRouteAsync(string hostname, string backendUrl);

    /// <summary>
    /// Removes a route from the proxy server.
    /// </summary>
    /// <param name="hostname">The hostname to remove.</param>
    /// <returns>True if removal succeeded; false otherwise.</returns>
    Task<bool> RemoveRouteAsync(string hostname);
}
