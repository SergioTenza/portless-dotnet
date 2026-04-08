namespace Portless.Core.Services;

public interface IEventBus
{
    void Publish(string eventType, object? data = null);
    IAsyncEnumerable<EventBusEvent> SubscribeAsync(CancellationToken cancellationToken = default);
}

public record EventBusEvent(string Type, object? Data, DateTime Timestamp)
{
    public static EventBusEvent RequestCompleted(string hostname, string method, int statusCode, double durationMs)
    {
        return new EventBusEvent(
            "RequestCompleted",
            new { Hostname = hostname, Method = method, StatusCode = statusCode, DurationMs = durationMs },
            DateTime.UtcNow);
    }

    public static EventBusEvent RouteAdded(string hostname)
    {
        return new EventBusEvent("RouteAdded", new { Hostname = hostname }, DateTime.UtcNow);
    }

    public static EventBusEvent RouteRemoved(string hostname)
    {
        return new EventBusEvent("RouteRemoved", new { Hostname = hostname }, DateTime.UtcNow);
    }

    public static EventBusEvent HealthChanged(string hostname, string healthStatus)
    {
        return new EventBusEvent("HealthChanged", new { Hostname = hostname, HealthStatus = healthStatus }, DateTime.UtcNow);
    }
}
