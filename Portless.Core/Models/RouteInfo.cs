namespace Portless.Core.Models;

public class RouteInfo
{
    public string Hostname { get; init; } = string.Empty;
    public int Port { get; init; }
    public int Pid { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? LastSeen { get; set; }
}
