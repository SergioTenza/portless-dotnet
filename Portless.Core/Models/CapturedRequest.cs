namespace Portless.Core.Models;

/// <summary>A captured HTTP request/response pair for inspection.</summary>
public sealed class CapturedRequest
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string Method { get; init; } = string.Empty;
    public string Hostname { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public string Scheme { get; init; } = string.Empty;
    public Dictionary<string, string> RequestHeaders { get; init; } = [];
    public string? RequestBody { get; init; }
    public int StatusCode { get; init; }
    public Dictionary<string, string> ResponseHeaders { get; init; } = [];
    public string? ResponseBody { get; init; }
    public long DurationMs { get; init; }
    public string RouteId { get; init; } = string.Empty;
}
