using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Model;
using Portless.Core.Extensions;
using Portless.Core.Services;
using Portless.Core.Models;
using Portless.Core.Configuration;

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

// Read port from environment variable or use default
var port = builder.Configuration["PORTLESS_PORT"] ?? "1355";

// Configure Kestrel to listen on all interfaces
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
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
logger.LogInformation("Portless Proxy starting on port {Port}", port);
logger.LogInformation("Proxy URL: http://localhost:{Port}", port);

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
        try
        {
            var allRoutes = await routeStore.LoadRoutesAsync();

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
            await routeStore.SaveRoutesAsync(updatedRoutes);

            logger.LogInformation("Route persisted: {Hostname} => {Port}", request.Hostname, port);
        }
        catch (Exception ex)
        {
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

            _logger.LogInformation("Request: {Method} {Host}{Path} => {StatusCode} ({Duration}ms)",
                method, host, path, statusCode, duration);
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