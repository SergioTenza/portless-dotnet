using System.Diagnostics;
using Portless.Core.Services;
using Portless.Plugin.SDK;

namespace Portless.Proxy;

/// <summary>
/// Middleware that invokes plugin hooks before and after proxy forwarding.
/// Registered early in the pipeline so plugins can intercept and modify requests.
/// </summary>
public sealed class PluginMiddleware
{
    private readonly RequestDelegate _next;

    public PluginMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var pluginLoader = context.RequestServices.GetRequiredService<IPluginLoader>();
        var logger = context.RequestServices.GetRequiredService<ILogger<PluginMiddleware>>();

        // Build the ProxyContext for plugins
        var proxyContext = new ProxyContext
        {
            Hostname = context.Request.Host.Host,
            Path = context.Request.Path + context.Request.QueryString,
            Method = context.Request.Method,
            Scheme = context.Request.Scheme,
            RouteId = context.GetYarpRouteId(),
            Headers = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            Body = await ReadRequestBodyAsync(context),
            CancellationToken = context.RequestAborted
        };

        // Fire BeforeProxy hooks
        var shortCircuit = await pluginLoader.FireBeforeProxyAsync(proxyContext);
        if (shortCircuit != null)
        {
            // Plugin short-circuited the request
            context.Response.StatusCode = shortCircuit.StatusCode;
            foreach (var header in shortCircuit.ResponseHeaders)
            {
                context.Response.Headers[header.Key] = header.Value;
            }
            if (shortCircuit.ResponseBody != null)
            {
                await context.Response.WriteAsync(shortCircuit.ResponseBody, context.RequestAborted);
            }

            logger.LogDebug("Plugin short-circuited: {Method} {Host}{Path} => {Status}",
                context.Request.Method, context.Request.Host, context.Request.Path,
                shortCircuit.StatusCode);
            return;
        }

        var sw = Stopwatch.StartNew();
        await _next(context);
        sw.Stop();

        // Fire AfterProxy hooks
        var result = new ProxyResult
        {
            StatusCode = context.Response.StatusCode,
            DurationMs = sw.ElapsedMilliseconds,
            ResponseHeaders = context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            ResponseBody = null // Don't capture body in plugin hooks to avoid buffering overhead
        };

        try
        {
            await pluginLoader.FireAfterProxyAsync(proxyContext, result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error firing AfterProxy hooks");
        }
    }

    private static async Task<string?> ReadRequestBodyAsync(HttpContext context)
    {
        try
        {
            if (context.Request.ContentLength is > 0 and < 1_000_000)
            {
                var contentType = context.Request.ContentType ?? "";
                if (contentType.Contains("json") || contentType.Contains("text") ||
                    contentType.Contains("xml") || contentType.Contains("form"))
                {
                    context.Request.EnableBuffering();
                    using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                    var body = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;
                    return body;
                }
            }
        }
        catch
        {
            // Non-critical: if body read fails, continue without it
        }
        return null;
    }
}

/// <summary>Extension to get YARP route ID from the HttpContext.</summary>
internal static class YarpContextExtensions
{
    public static string GetYarpRouteId(this HttpContext context)
    {
        // Derive route/cluster ID from hostname (same convention used by YarpConfigFactory)
        var hostname = context.Request.Host.Host;
        return $"cluster-{hostname}";
    }
}
