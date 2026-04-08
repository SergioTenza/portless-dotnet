namespace Portless.Plugin.SDK;

/// <summary>
/// Custom error response returned by <see cref="PortlessPlugin.OnErrorAsync"/>.
/// </summary>
public sealed class ErrorResponse
{
    /// <summary>
    /// HTTP status code to send to the client.
    /// </summary>
    public required int StatusCode { get; init; }

    /// <summary>
    /// Response body to send to the client.
    /// </summary>
    public required string Body { get; init; }

    /// <summary>
    /// Content-Type header value. Defaults to "text/html; charset=utf-8".
    /// </summary>
    public string ContentType { get; init; } = "text/html; charset=utf-8";

    /// <summary>
    /// Additional response headers. Never null; initialised to an empty dictionary.
    /// </summary>
    public Dictionary<string, string> Headers { get; init; } = [];
}
