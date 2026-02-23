using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Model;
using Microsoft.AspNetCore.HttpOverrides;
using Portless.Core.Extensions;
using Portless.Core.Services;
using Portless.Core.Models;
using Portless.Core.Configuration;
using Portless.Proxy;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

// Helper methods for creating YARP routes and clusters
static RouteConfig CreateRoute(string hostname, string clusterId) =>
    new RouteConfig
    {
        RouteId = $"route-{hostname}",
        ClusterId = clusterId,
        Match = new RouteMatch
        {
            Hosts = new[] { hostname },
            Path = "/{**catch-all}"
        }
    };

static ClusterConfig CreateCluster(string clusterId, string backendUrl) =>
    new ClusterConfig
    {
        ClusterId = clusterId,
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["backend1"] = new DestinationConfig { Address = backendUrl }
        }
    };

// Add logging configuration
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add SignalR services for integration testing
builder.Services.AddSignalR();

// Read port and HTTPS configuration from environment variables
var port = builder.Configuration["PORTLESS_PORT"] ?? "1355";
var enableHttps = builder.Configuration["PORTLESS_HTTPS_ENABLED"] == "true";

// Register certificate services for HTTPS support
builder.Services.AddPortlessCertificates();

// Build a temporary service provider to load certificate before Kestrel configuration
X509Certificate2? certificate = null;
if (enableHttps)
{
    var tempServices = builder.Services.BuildServiceProvider();
    var certManager = tempServices.GetRequiredService<ICertificateManager>();
    var status = await certManager.EnsureCertificatesAsync(
        forceRegeneration: false,
        CancellationToken.None
    );

    if (!status.IsValid || status.IsCorrupted)
    {
        Console.Error.WriteLine("Error: Certificate not found or invalid. Run: portless cert install");
        Environment.Exit(1);
        return;
    }

    certificate = await certManager.GetServerCertificateAsync();
}

// Configure Kestrel to listen on all interfaces with HTTP/2 support
builder.WebHost.ConfigureKestrel((context, options) =>
{
    // Enforce TLS 1.2+ globally for HTTPS
    options.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;
    });

    // Configure limits for long-lived WebSocket connections
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10); // Default is 2 minutes
    options.Limits.MaxConcurrentUpgradedConnections = 1000; // Default is 100

    // HTTP endpoint (always active for backward compatibility)
    options.ListenAnyIP(1355, listenOptions =>
    {
        // Enable both HTTP/1.1 and HTTP/2 (Kestrel will negotiate via ALPN)
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
    });

    // HTTPS endpoint (conditional on --https flag)
    if (enableHttps && certificate != null)
    {
        options.ListenAnyIP(1356, listenOptions =>
        {
            listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
            listenOptions.UseHttps(certificate);
        });
    }
});

// Register DynamicConfigProvider as singleton for YARP
builder.Services.AddSingleton<DynamicConfigProvider>();
builder.Services.AddSingleton<IProxyConfigProvider>(sp => sp.GetRequiredService<DynamicConfigProvider>());

// Add persistence layer
builder.Services.AddPortlessPersistence();
builder.Services.AddRouteFileWatcher();

// Add Reverse Proxy with empty initial config (will be managed by DynamicConfigProvider)
builder.Services.AddReverseProxy()
    .LoadFromMemory([],[]);

var app = builder.Build();

// Add startup logging
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Portless Proxy starting on port {Port}", 1355);
logger.LogInformation("Proxy URL: http://localhost:{Port}", 1355);
if (enableHttps)
{
    logger.LogInformation("Proxy URL (HTTPS): https://localhost:{Port}", 1356);
}

// Load existing routes on startup
var routeStore = app.Services.GetRequiredService<IRouteStore>();
var configProvider = app.Services.GetRequiredService<DynamicConfigProvider>();

try
{
    var existingRoutes = await routeStore.LoadRoutesAsync();
    if (existingRoutes.Length > 0)
    {
        var routeConfigs = new List<RouteConfig>();
        var clusterConfigs = new List<ClusterConfig>();

        foreach (var route in existingRoutes)
        {
            routeConfigs.Add(CreateRoute(route.Hostname, $"cluster-{route.Hostname}"));
            clusterConfigs.Add(CreateCluster($"cluster-{route.Hostname}", $"http://localhost:{route.Port}"));
        }

        configProvider.Update(routeConfigs, clusterConfigs);
        logger.LogInformation("Loaded {Count} routes from persistence layer", existingRoutes.Length);
    }
    else
    {
        logger.LogInformation("No existing routes found, starting with empty configuration");
    }
}
catch (Exception ex)
{
    logger.LogError(ex, "Error loading existing routes, starting with empty configuration");
}

app.UseMiddleware<RequestLoggingMiddleware>();

app.MapPost("/api/v1/add-host", async (AddHostRequest request, ILogger<Program> logger) =>
{
    try
    {
        // Validate request
        if (string.IsNullOrWhiteSpace(request.Hostname))
        {
            logger.LogWarning("Invalid add-host request: Hostname is null or empty");
            return Results.Problem(
                detail: "Hostname cannot be null or empty",
                statusCode: 400,
                title: "Validation Error"
            );
        }

        if (string.IsNullOrWhiteSpace(request.BackendUrl))
        {
            logger.LogWarning("Invalid add-host request: BackendUrl is null or empty");
            return Results.Problem(
                detail: "BackendUrl cannot be null or empty",
                statusCode: 400,
                title: "Validation Error"
            );
        }

        var config = app.Services.GetRequiredService<DynamicConfigProvider>();
        var routeStore = app.Services.GetRequiredService<IRouteStore>();

        // Get existing configuration to preserve other routes
        var existingConfig = config.GetConfig();
        var existingRoutes = existingConfig.Routes.ToList();
        var existingClusters = existingConfig.Clusters.ToList();

        // Check if hostname already exists
        if (existingRoutes.Any(r => r.Match?.Hosts?.Contains(request.Hostname) == true))
        {
            logger.LogWarning("Hostname {Hostname} already exists", request.Hostname);
            return Results.Problem(
                detail: $"Hostname '{request.Hostname}' is already configured",
                statusCode: 409,
                title: "Conflict"
            );
        }

        // Create new route and cluster
        var clusterId = $"cluster-{request.Hostname}";
        var newRoute = CreateRoute(request.Hostname, clusterId);
        var newCluster = CreateCluster(clusterId, request.BackendUrl);

        // Add to existing configuration
        existingRoutes.Add(newRoute);
        existingClusters.Add(newCluster);

        // Update configuration
        config.Update(existingRoutes, existingClusters);

        // Persist route to file
        Console.WriteLine($"[DEBUG] → Attempting to persist route: {request.Hostname} => {request.BackendUrl}");

        try
        {
            var allRoutes = await routeStore.LoadRoutesAsync();
            Console.WriteLine($"[DEBUG] → Loaded {allRoutes.Length} existing routes from file");

            // Extract port from backendUrl
            var uri = new Uri(request.BackendUrl);
            var port = uri.Port;

            var newRouteInfo = new RouteInfo
            {
                Hostname = request.Hostname,
                Port = port,
                Pid = Environment.ProcessId,
                CreatedAt = DateTime.UtcNow
            };
            var updatedRoutes = allRoutes.Append(newRouteInfo).ToArray();
            Console.WriteLine($"[DEBUG] → Saving {updatedRoutes.Length} routes to file (PID: {Environment.ProcessId})");
            Console.WriteLine($"[DEBUG] → Route to save: {newRouteInfo.Hostname} => Port {newRouteInfo.Port}");

            await routeStore.SaveRoutesAsync(updatedRoutes);

            Console.WriteLine($"[DEBUG] ✓ Route persisted successfully: {request.Hostname} => {port}");
            logger.LogInformation("Route persisted: {Hostname} => {Port}", request.Hostname, port);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] ✗ Error persisting route: {ex.Message}");
            Console.WriteLine($"[DEBUG] ✗ Stack trace: {ex.StackTrace}");
            logger.LogError(ex, "Error persisting route to file");
        }

        logger.LogInformation("Host added successfully: {Hostname} => {BackendUrl}",
            request.Hostname, request.BackendUrl);

        return Results.Ok(new
        {
            success = true,
            message = $"Host '{request.Hostname}' added successfully",
            data = new
            {
                hostname = request.Hostname,
                backendUrl = request.BackendUrl,
                clusterId = clusterId,
                routeId = newRoute.RouteId
            }
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error adding host");
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Add Host Error"
        );
    }
});

// Configure reverse proxy (must be after logging middleware)
app.MapDelete("/api/v1/remove-host", async (string hostname, ILogger<Program> logger, DynamicConfigProvider config, IRouteStore routeStore) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(hostname))
        {
            return Results.Problem(
                detail: "Hostname cannot be null or empty",
                statusCode: 400,
                title: "Validation Error"
            );
        }

        var existingConfig = config.GetConfig();
        var existingRoutes = existingConfig.Routes.ToList();
        var existingClusters = existingConfig.Clusters.ToList();

        // Remove route and cluster
        var routeToRemove = existingRoutes.FirstOrDefault(r => r.Match?.Hosts?.Contains(hostname) == true);
        if (routeToRemove != null)
        {
            existingRoutes.Remove(routeToRemove);
        }

        var clusterToRemove = existingClusters.FirstOrDefault(c => c.ClusterId == $"cluster-{hostname}");
        if (clusterToRemove != null)
        {
            existingClusters.Remove(clusterToRemove);
        }

        // Update configuration
        config.Update(existingRoutes, existingClusters);

        // Remove from file storage
        var allRoutes = await routeStore.LoadRoutesAsync();
        var updatedRoutes = allRoutes.Where(r => r.Hostname != hostname).ToArray();
        await routeStore.SaveRoutesAsync(updatedRoutes);

        logger.LogInformation("Host removed: {Hostname}", hostname);

        return Results.Ok(new
        {
            success = true,
            message = $"Host '{hostname}' removed successfully"
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error removing host");
        return Results.Problem(
            detail: ex.Message,
            statusCode: 500,
            title: "Remove Host Error"
        );
    }
});

// Use ForwardedHeaders middleware to add X-Forwarded-* headers
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.All,
    // Only trust local proxies
    KnownProxies = { System.Net.IPAddress.Loopback }
});

// Configure HTTP→HTTPS redirect only when HTTPS is enabled
// Exclude /api/v1/* endpoints from HTTPS redirect (CLI needs HTTP access)
if (enableHttps)
{
    app.Use(async (context, next) =>
    {
        // Exclude /api/v1/* endpoints from HTTPS redirect
        if (context.Request.Path.StartsWithSegments("/api/v1"))
        {
            await next();
        }
        else if (context.Request.Protocol == "HTTP/1.1" || context.Request.Protocol == "HTTP/2")
        {
            // Apply HTTPS redirect for all other HTTP requests
            var httpsPort = 1356;
            var host = context.Request.Host.Host;
            var originalPath = context.Request.Path.Value ?? "/";
            var queryString = context.Request.QueryString.Value ?? "";

            var redirectUrl = $"https://{host}:{httpsPort}{originalPath}{queryString}";
            context.Response.StatusCode = 308; // Permanent Redirect
            context.Response.Headers["Location"] = redirectUrl;
            return;
        }
        else
        {
            await next();
        }
    });
}

// Map test SignalR hub for integration testing (must be before reverse proxy)
app.MapHub<TestChatHub>("/testhub");

// Custom middleware to add X-Forwarded-Protocol header
app.Use(async (context, next) =>
{
    var protocol = context.Request.Protocol;
    context.Request.Headers["X-Forwarded-Protocol"] = protocol;
    await next();
});

app.MapReverseProxy();

app.Run();


public record AddHostRequest(
    string Hostname,
    string BackendUrl
);

// Request logging middleware
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = DateTime.UtcNow;
        var method = context.Request.Method;
        var host = context.Request.Headers["Host"].ToString();
        var path = context.Request.Path;

        try
        {
            
            await _next(context);

            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            var statusCode = context.Response.StatusCode;
            var protocol = context.Request.Protocol;

            _logger.LogInformation("Request: {Method} {Host}{Path} => {StatusCode} ({Duration}ms) [{Protocol}]",
                method, host, path, statusCode, duration, protocol);

            // Detect silent protocol downgrades (HTTP/2 requested but HTTP/1.1 used)
            var http2Requested = context.Request.Headers["Upgrade-Insecure-Requests"].Count > 0 ||
                                context.Request.Headers.ContainsKey("HTTP2-Settings");
            if (protocol == "HTTP/1.1" && http2Requested)
            {
                _logger.LogWarning("Possible silent HTTP/2 downgrade detected: Client may have requested HTTP/2 but HTTP/1.1 was used. Check TLS/ALPN configuration.");
            }
        }
        catch (Exception ex)
        {
            var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _logger.LogError(ex, "Request: {Method} {Host}{Path} => Error ({Duration}ms)",
                method, host, path, duration);
            throw;
        }
    }
}

// Expose Program class for WebApplicationFactory testing
public partial class Program { }