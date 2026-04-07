using Prometheus;
using Portless.Core.Services;

namespace Portless.Proxy;

public class PrometheusMetricsService : IMetricsService
{
    private readonly Counter _requestTotal = Metrics.CreateCounter(
        "portless_proxy_requests_total",
        "Total number of proxied requests",
        new CounterConfiguration { LabelNames = new[] { "hostname", "method", "status_code" } });

    private readonly Histogram _requestDuration = Metrics.CreateHistogram(
        "portless_proxy_request_duration_seconds",
        "Request duration in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "hostname" },
            Buckets = Histogram.ExponentialBuckets(0.001, 2, 12)
        });

    private readonly Gauge _activeRoutes = Metrics.CreateGauge(
        "portless_active_routes",
        "Number of active HTTP proxy routes");

    private readonly Gauge _activeTcpListeners = Metrics.CreateGauge(
        "portless_active_tcp_listeners",
        "Number of active TCP proxy listeners");

    public void RecordProxyRequest(string hostname, string method, int statusCode, double durationMs)
    {
        _requestTotal.WithLabels(hostname, method, statusCode.ToString()).Inc();
        _requestDuration.WithLabels(hostname).Observe(durationMs / 1000.0);
    }

    public void UpdateActiveRoutes(int count) => _activeRoutes.Set(count);
    public void UpdateActiveTcpListeners(int count) => _activeTcpListeners.Set(count);
}
