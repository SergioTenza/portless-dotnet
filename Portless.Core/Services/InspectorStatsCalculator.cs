using Portless.Core.Models;

namespace Portless.Core.Services;

/// <summary>
/// Calculates inspector statistics from recent captured requests.
/// Eliminates duplicated stats computation between DashboardApiEndpoints and PortlessApiEndpoints.
/// </summary>
public static class InspectorStatsCalculator
{
    public sealed class Stats
    {
        public int TotalCaptured { get; init; }
        public double AvgDurationMs { get; init; }
        public double ErrorRate { get; init; }
        public double RequestsPerMinute { get; init; }
    }

    /// <summary>
    /// Computes aggregate statistics from the inspector's recent requests.
    /// Returns null if the inspector is null.
    /// </summary>
    public static Stats? Compute(IRequestInspector? inspector)
    {
        if (inspector == null) return null;

        var recent = inspector.GetRecent(100);
        var avgDurationMs = recent.Count > 0 ? Math.Round(recent.Average(r => r.DurationMs), 2) : 0.0;
        var errorRate = recent.Count > 0 ? Math.Round(recent.Count(r => r.StatusCode >= 400) / (double)recent.Count, 4) : 0.0;

        double requestsPerMinute = 0.0;
        if (recent.Count >= 2)
        {
            var oldest = recent.Min(r => r.Timestamp);
            var newest = recent.Max(r => r.Timestamp);
            var span = newest - oldest;
            if (span.TotalMinutes > 0)
            {
                requestsPerMinute = Math.Round(recent.Count / span.TotalMinutes, 2);
            }
        }

        return new Stats
        {
            TotalCaptured = inspector.Count,
            AvgDurationMs = avgDurationMs,
            ErrorRate = errorRate,
            RequestsPerMinute = requestsPerMinute
        };
    }
}
