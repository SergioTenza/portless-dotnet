namespace Portless.Plugin.SDK;

/// <summary>
/// Describes an incoming proxy request. Passed to <see cref="PortlessPlugin.BeforeProxyAsync"/>
/// and <see cref="PortlessPlugin.AfterProxyAsync"/>.
/// </summary>
public sealed class ProxyContext
{
    /// <summary>
    /// The hostname of the incoming request (e.g. "myapp.localhost").
    /// </summary>
    public required string Hostname { get; init; }

    /// <summary>
    /// The request path (e.g. "/api/users").
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// The HTTP method (e.g. "GET", "POST").
    /// </summary>
    public required string Method { get; init; }

    /// <summary>
    /// The request scheme ("http" or "https").
    /// </summary>
    public required string Scheme { get; init; }

    /// <summary>
    /// Identifier of the matched route, if any.
    /// </summary>
    public required string RouteId { get; init; }

    /// <summary>
    /// Request headers. Never null; initialised to an empty dictionary.
    /// </summary>
    public Dictionary<string, string> Headers { get; init; } = [];

    /// <summary>
    /// Request body, if present.
    /// </summary>
    public string? Body { get; init; }

    /// <summary>
    /// Cancellation token forwarded from the host.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }
}
