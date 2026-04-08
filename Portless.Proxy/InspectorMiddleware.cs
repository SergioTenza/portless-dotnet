using System.Diagnostics;
using System.Text;
using Portless.Core.Models;
using Portless.Core.Services;

namespace Portless.Proxy;

/// <summary>
/// Middleware that captures proxied requests/responses into the inspector ring buffer.
/// Must be registered after PluginMiddleware but before MapReverseProxy.
/// Non-blocking: captures are fire-and-forget into the buffer.
/// </summary>
public sealed class InspectorMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly HashSet<string> CaptureContentTypes =
    [
        "json", "text", "html", "xml", "javascript", "form-urlencoded"
    ];

    public InspectorMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var inspector = context.RequestServices.GetService<IRequestInspector>();
        if (inspector == null)
        {
            await _next(context);
            return;
        }

        // Don't capture internal API/dashboard calls
        var path = context.Request.Path.Value;
        if (path != null &&
            (path.StartsWith("/api/v1", StringComparison.OrdinalIgnoreCase) ||
             path.StartsWith("/_dashboard", StringComparison.OrdinalIgnoreCase) ||
             path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase) ||
             path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
             path.StartsWith("/testhub", StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var sw = Stopwatch.StartNew();

        // Capture request body if text-based and small
        string? requestBody = null;
        try
        {
            if (context.Request.ContentLength is > 0 and < 1_000_000)
            {
                var ct = context.Request.ContentType ?? "";
                if (ShouldCaptureBody(ct))
                {
                    context.Request.EnableBuffering();
                    using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                    requestBody = await reader.ReadToEndAsync(context.RequestAborted);
                    context.Request.Body.Position = 0;
                }
            }
        }
        catch
        {
            // Non-critical: skip body capture on failure
        }

        // Buffer response body to capture it
        var originalBody = context.Response.Body;
        using var captureStream = new MemoryStream();
        context.Response.Body = captureStream;

        await _next(context);
        sw.Stop();

        // Capture response body if text-based and small
        string? responseBody = null;
        try
        {
            var responseCt = context.Response.ContentType ?? "";
            captureStream.Seek(0, SeekOrigin.Begin);

            if (captureStream.Length < 1_000_000 && ShouldCaptureBody(responseCt))
            {
                using var reader = new StreamReader(captureStream, leaveOpen: true);
                responseBody = await reader.ReadToEndAsync(context.RequestAborted);
            }
        }
        catch
        {
            // Non-critical: skip body capture on failure
        }

        // Copy response back to original stream
        captureStream.Seek(0, SeekOrigin.Begin);
        context.Response.Body = originalBody;
        await captureStream.CopyToAsync(originalBody, context.RequestAborted);

        // Record into inspector (fire-and-forget, no awaiting)
        var captured = new CapturedRequest
        {
            Method = context.Request.Method,
            Hostname = context.Request.Host.Host,
            Path = (context.Request.Path + context.Request.QueryString).ToString(),
            Scheme = context.Request.Scheme,
            RequestHeaders = context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            RequestBody = requestBody,
            StatusCode = context.Response.StatusCode,
            ResponseHeaders = context.Response.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()),
            ResponseBody = responseBody,
            DurationMs = sw.ElapsedMilliseconds,
            RouteId = context.GetYarpRouteId()
        };

        inspector.Capture(captured);
    }

    private static bool ShouldCaptureBody(string contentType)
    {
        if (string.IsNullOrEmpty(contentType)) return false;
        foreach (var keyword in CaptureContentTypes)
        {
            if (contentType.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
