namespace Portless.Core.Models;

/// <summary>
/// Type of route: HTTP proxy or raw TCP forwarding.
/// </summary>
public enum RouteType
{
    Http = 0,
    Tcp = 1
}

/// <summary>
/// Load balancing policy for multi-backend clusters.
/// </summary>
public enum LoadBalancingPolicy
{
    First = 0,
    RoundRobin = 1,
    LeastRequests = 2,
    Random = 3,
    PowerOfTwoChoices = 4
}

public class RouteInfo
{
    // === Existing fields (backward compatible) ===
    public string Hostname { get; init; } = string.Empty;
    public int Port { get; init; }
    public int Pid { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? LastSeen { get; set; }
    public string BackendProtocol { get; init; } = "http";

    // === Tier 2 fields (nullable for backward compat) ===

    /// <summary>
    /// Optional path prefix for path-based routing (e.g. "/api").
    /// Null means catch-all "/{**catch-all}".
    /// </summary>
    public string? Path { get; init; }

    /// <summary>
    /// Multiple backend URLs for load balancing.
    /// If null or empty, falls back to single backend derived from Port + BackendProtocol.
    /// </summary>
    public string[]? BackendUrls { get; init; }

    /// <summary>
    /// Load balancing policy for multi-backend clusters.
    /// </summary>
    public LoadBalancingPolicy LoadBalancingPolicy { get; init; } = LoadBalancingPolicy.PowerOfTwoChoices;

    /// <summary>
    /// Route type: HTTP (default) or TCP.
    /// </summary>
    public RouteType Type { get; init; } = RouteType.Http;

    /// <summary>
    /// TCP listen port (only for RouteType.Tcp).
    /// </summary>
    public int? TcpListenPort { get; init; }

    /// <summary>
    /// Helper: resolves effective backend URLs.
    /// </summary>
    public string[] GetBackendUrls()
    {
        if (BackendUrls is { Length: > 0 })
            return BackendUrls;

        return [$"{BackendProtocol}://localhost:{Port}"];
    }
}
