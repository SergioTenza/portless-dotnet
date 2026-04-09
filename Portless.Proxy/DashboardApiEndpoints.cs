using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Portless.Core.Models;
using Portless.Core.Services;

namespace Portless.Proxy;

/// <summary>
/// Maps dashboard-specific API endpoints for monitoring and real-time events.
/// </summary>
public static class DashboardApiEndpoints
{
    /// <summary>
    /// Maps the /api/v1/dashboard/* endpoints for the Portless dashboard UI.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode",
        Justification = "Minimal API endpoints use reflection for delegate parameter binding - by design in ASP.NET Core")]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode",
        Justification = "Minimal API endpoints require dynamic code generation - by design in ASP.NET Core")]
    public static IEndpointRouteBuilder MapDashboardApi(
        this IEndpointRouteBuilder endpoints)
    {
        // ── GET /api/v1/dashboard/summary ────────────────────────────────
        endpoints.MapGet("/api/v1/dashboard/summary", async (IRouteStore rs, IRequestInspector? insp) =>
        {
            var routes = await rs.LoadRoutesAsync();
            var uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();

            var stats = InspectorStatsCalculator.Compute(insp);

            return Results.Ok(new
            {
                activeRoutes = routes.Length,
                uptime = uptime.ToString(),
                totalCaptured = stats?.TotalCaptured ?? 0,
                avgDurationMs = stats?.AvgDurationMs ?? 0.0,
                errorRate = stats?.ErrorRate ?? 0.0,
                requestsPerMinute = stats?.RequestsPerMinute ?? 0.0
            });
        });

        // ── GET /api/v1/dashboard/routes ─────────────────────────────────
        endpoints.MapGet("/api/v1/dashboard/routes", async (
            IRouteStore rs,
            IRouteHealthChecker? hc,
            ILogger<Program> logger) =>
        {
            var routes = await rs.LoadRoutesAsync();
            var result = new List<object>();

            foreach (var route in routes)
            {
                string? health = null;
                if (hc != null)
                {
                    try
                    {
                        health = (await hc.CheckHealthAsync(route.Hostname)).ToString().ToLowerInvariant();
                    }
                    catch (Exception ex)
                    {
                        logger.LogDebug(ex, "Health check failed for {Hostname}", route.Hostname);
                        health = "unknown";
                    }
                }

                result.Add(new
                {
                    route.Hostname,
                    route.Port,
                    route.Pid,
                    route.Path,
                    type = route.Type.ToString().ToLowerInvariant(),
                    backends = route.GetBackendUrls(),
                    health,
                    route.CreatedAt,
                    route.LastSeen
                });
            }

            return Results.Ok(result);
        });

        // ── GET /api/v1/dashboard/events (SSE) ───────────────────────────
        endpoints.MapGet("/api/v1/dashboard/events", async (
            HttpContext context,
            IEventBus? bus,
            ILogger<Program> logger) =>
        {
            if (bus == null)
            {
                context.Response.StatusCode = 501;
                await context.Response.WriteAsync("EventBus not available");
                return;
            }

            context.Response.ContentType = "text/event-stream";
            context.Response.Headers.CacheControl = "no-cache";
            context.Response.Headers.Connection = "keep-alive";

            var cts = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);

            try
            {
                await context.Response.WriteAsync(":\n\n", cts.Token); // SSE keep-alive comment
                await context.Response.Body.FlushAsync(cts.Token);

                await foreach (var evt in bus.SubscribeAsync(cts.Token))
                {
                    var json = JsonSerializer.Serialize(evt);
                    await context.Response.WriteAsync($"data: {json}\n\n", cts.Token);
                    await context.Response.Body.FlushAsync(cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Client disconnected — graceful shutdown, no error needed
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "SSE stream ended for dashboard events");
            }
        });

        return endpoints;
    }
}
