namespace Portless.Core.Services;

public interface IRouteHealthChecker
{
    Task<RouteHealthStatus> CheckHealthAsync(string hostname);
    IReadOnlyDictionary<string, RouteHealthStatus> GetAllHealthStatuses();
}

public enum RouteHealthStatus
{
    Unknown,
    Healthy,
    Unhealthy
}
