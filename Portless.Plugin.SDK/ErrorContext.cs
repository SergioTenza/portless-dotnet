namespace Portless.Plugin.SDK;

/// <summary>
/// Describes the context of an error that occurred during proxying.
/// Passed to <see cref="PortlessPlugin.OnErrorAsync"/>.
/// </summary>
public sealed class ErrorContext
{
    /// <summary>
    /// Hostname of the request that caused the error.
    /// </summary>
    public required string Hostname { get; init; }

    /// <summary>
    /// Path of the request that caused the error.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// HTTP status code associated with the error (e.g. 502, 504).
    /// </summary>
    public required int StatusCode { get; init; }

    /// <summary>
    /// Human-readable error message, if available.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
