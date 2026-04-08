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
    private readonly ILogger<PluginMiddleware> _logger;

    public PluginMiddleware(RequestDelegate next, ILogger<PluginMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip internal endpoints that should not be intercepted by plugins
        if (IsInternalPath(path))
        {
            await _next(context);
            return;
        }

        var pluginLoader = context.RequestServices.GetService<IPluginLoader>();
        if (pluginLoader == null || pluginLoader.GetLoadedPlugins().Count == 0)
        {
            await _next(context);
            return;
        }

        // Build the ProxyContext for plugins
        var proxyContext = new ProxyContext
        {
            Hostname = context.Request.Host.Host,
            Path = context.Request.Path + context.Request.QueryString,
            Method = context.Request.Method,
            Scheme = context.Request.Scheme,
            RouteId = context.GetYarpRouteId(),
            Headers = SanitizeHeaders(context.Request.Headers),
            Body = await ReadRequestBodyAsync(context),
            CancellationToken = context.RequestAborted
        };

        // Fire BeforeProxy hooks
        try
        {
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

                _logger.LogDebug(
                    "Plugin short-circuited: {Method} {Host}{Path} => {Status}",
                    context.Request.Method, context.Request.Host, context.Request.Path,
                    shortCircuit.StatusCode);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plugin before-proxy hook failed for {Method} {Host}{Path}",
                context.Request.Method, context.Request.Host, context.Request.Path);
            // Continue with normal proxy pipeline on plugin error
        }

        var sw = Stopwatch.StartNew();
        await _next(context);
        sw.Stop();

        // Fire AfterProxy hooks
        try
        {
            var result = new ProxyResult
            {
                StatusCode = context.Response.StatusCode,
                DurationMs = sw.ElapsedMilliseconds,
                ResponseHeaders = context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
                ResponseBody = null // Don't capture body in plugin hooks to avoid buffering overhead
            };

            await pluginLoader.FireAfterProxyAsync(proxyContext, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plugin after-proxy hook failed for {Method} {Host}{Path}",
                context.Request.Method, context.Request.Host, context.Request.Path);
            // Don't rethrow – the response has already been sent to the client
        }
    }

    /// <summary>
    /// Determines whether the request path targets an internal endpoint
    /// that should bypass plugin hooks.
    /// </summary>
    private static bool IsInternalPath(string path)
    {
        return path.StartsWith("/api/v1", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/testhub", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/_dashboard", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Copies request headers into a dictionary, masking sensitive values
    /// such as the Authorization header.
    /// </summary>
    private static Dictionary<string, string> SanitizeHeaders(IHeaderDictionary headers)
    {
        var dict = new Dictionary<string, string>(headers.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var header in headers)
        {
            dict[header.Key] = header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
                ? "***masked***"
                : header.Value.ToString();
        }
        return dict;
    }

    /// <summary>
    /// Reads the request body when it is a small text-based payload so plugins
    /// can inspect it.  The stream is rewound afterwards so downstream middleware
    /// still has access.
    /// </summary>
    private static async Task<string?> ReadRequestBodyAsync(HttpContext context)
    {
        try
        {
            if (context.Request.ContentLength is > 0 and < 1_000_000)
            {
                var contentType = context.Request.ContentType ?? string.Empty;
                if (contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
                    || contentType.Contains("text", StringComparison.OrdinalIgnoreCase)
                    || contentType.Contains("xml", StringComparison.OrdinalIgnoreCase)
                    || contentType.Contains("form", StringComparison.OrdinalIgnoreCase))
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
