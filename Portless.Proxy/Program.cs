using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Model;
using Portless.Proxy;

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

// Add Reverse Proxy with empty initial config (will be managed by DynamicConfigProvider)
builder.Services.AddReverseProxy()
    .LoadFromMemory([],[]);

var app = builder.Build();

// Add startup logging
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Portless Proxy starting on port {Port}", port);
logger.LogInformation("Proxy URL: http://localhost:{Port}", port);

app.UseMiddleware<RequestLoggingMiddleware>();

app.MapPost("/api/v1/add-host", (AddHostRequest request, ILogger<Program> logger) =>
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