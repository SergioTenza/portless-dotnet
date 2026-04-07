namespace Portless.Core.Services;

public interface IMetricsService
{
    void RecordProxyRequest(string hostname, string method, int statusCode, double durationMs);
    void UpdateActiveRoutes(int count);
    void UpdateActiveTcpListeners(int count);
}
