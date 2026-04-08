namespace Portless.Plugin.SDK;

/// <summary>
/// Describes a proxy route. Passed to <see cref="PortlessPlugin.OnRouteAddedAsync"/>
/// and <see cref="PortlessPlugin.OnRouteRemovedAsync"/>.
/// </summary>
public sealed class RouteInfo
{
    /// <summary>
    /// The hostname this route matches.
    /// </summary>
    public required string Hostname { get; init; }

    /// <summary>
    /// Optional port the route listens on.
    /// </summary>
    public int? Port { get; init; }

    /// <summary>
    /// Optional path prefix the route matches.
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Optional type label for the route (e.g. "api", "static").
    /// </summary>
    public string? Type { get; init; }

    /// <summary>
    /// Backend addresses this route forwards to. Never null.
    /// </summary>
    public IReadOnlyList<string> Backends { get; init; } = [];

    /// <summary>
    /// Optional load-balancing policy name (e.g. "RoundRobin", "LeastRequests").
    /// </summary>
    public string? LoadBalancingPolicy { get; init; }
}
