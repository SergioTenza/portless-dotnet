namespace Portless.Plugin.SDK;

/// <summary>
/// Represents the result of a proxied request. Returned by
/// <see cref="PortlessPlugin.BeforeProxyAsync"/> (to short-circuit)
/// and passed to <see cref="PortlessPlugin.AfterProxyAsync"/>.
/// </summary>
public sealed class ProxyResult
{
    /// <summary>
    /// HTTP status code of the response.
    /// </summary>
    public required int StatusCode { get; init; }

    /// <summary>
    /// Total duration of the proxy round-trip in milliseconds.
    /// </summary>
    public required long DurationMs { get; init; }

    /// <summary>
    /// Response headers. Never null; initialised to an empty dictionary.
    /// </summary>
    public Dictionary<string, string> ResponseHeaders { get; init; } = [];

    /// <summary>
    /// Response body, if any.
    /// </summary>
    public string? ResponseBody { get; init; }
}
