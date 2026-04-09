using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Model;
using Microsoft.AspNetCore.HttpOverrides;
using Prometheus;
using Portless.Core.Extensions;
using Portless.Core.Services;
using Portless.Core.Models;
using Portless.Core.Configuration;
using Portless.Proxy;
using Portless.Proxy.ErrorPages;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add logging configuration
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Add SignalR services for integration testing
builder.Services.AddSignalR();

// Read port and HTTPS configuration from environment variables
var port = builder.Configuration[ProxyConstants.PortEnvVar] ?? ProxyConstants.DefaultHttpPort.ToString();
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
    options.ListenAnyIP(ProxyConstants.DefaultHttpPort, listenOptions =>
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
builder.Services.AddConfigFileWatcher();

// Prometheus metrics
builder.Services.AddSingleton<IMetricsService, PrometheusMetricsService>();

// Plugin system
builder.Services.AddPluginSystem();

// Request inspector (ring buffer capacity: 1000)
builder.Services.AddRequestInspector();

// Dashboard services
builder.Services.AddSingleton<IEventBus, EventBus>();
builder.Services.AddSingleton<IRouteHealthChecker, RouteHealthChecker>();
builder.Services.AddHostedService(sp => (RouteHealthChecker)sp.GetRequiredService<IRouteHealthChecker>());

// Health checks
builder.Services.AddHealthChecks();

// Add Reverse Proxy with empty initial config (will be managed by DynamicConfigProvider)
builder.Services.AddReverseProxy()
    .LoadFromMemory([],[]);

var app = builder.Build();

// Add startup logging
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Portless Proxy starting on port {Port}", ProxyConstants.DefaultHttpPort);
logger.LogInformation("Proxy URL: http://localhost:{Port}", ProxyConstants.DefaultHttpPort);
if (enableHttps)
{
    logger.LogInformation("Proxy URL (HTTPS): https://localhost:{Port}", 1356);
}

// Certificate expiration check (non-blocking warning)
if (enableHttps)
{
    try
    {
        var certManager = app.Services.GetRequiredService<ICertificateManager>();
        var cert = await certManager.GetServerCertificateAsync();

        if (cert != null)
        {
            var now = DateTimeOffset.UtcNow;
            var daysUntilExpiration = (cert.NotAfter - now).Days;
            var isExpired = now > cert.NotAfter;
            var isExpiringSoon = now > cert.NotAfter.AddDays(-30);

            if (isExpired)
            {
                // Red error for expired certificate
                logger.LogError("Certificate has expired on {ExpirationDate}. Run: portless cert renew",
                    cert.NotAfter.ToString("yyyy-MM-dd"));
                Console.Error.WriteLine("ERROR: Certificate has expired. HTTPS connections may fail.");
                Console.Error.WriteLine("Run: portless cert renew");
            }
            else if (isExpiringSoon)
            {
                // Yellow warning for certificate expiring soon
                logger.LogWarning("Certificate expires in {Days} days ({ExpirationDate}). Run: portless cert renew",
                    daysUntilExpiration, cert.NotAfter.ToString("yyyy-MM-dd"));
                Console.WriteLine($"WARNING: Certificate expires in {daysUntilExpiration} days ({cert.NotAfter:yyyy-MM-dd})");
                Console.WriteLine("Run: portless cert renew");
            }
            else
            {
                // Info message for valid certificate
                logger.LogInformation("Certificate valid until {ExpirationDate} ({Days} days remaining)",
                    cert.NotAfter.ToString("yyyy-MM-dd"), daysUntilExpiration);
            }
        }
    }
    catch (Exception ex)
    {
        // Non-blocking - log error but continue startup
        logger.LogWarning(ex, "Failed to check certificate expiration status. Proxy will start anyway.");
    }
}

// Load existing routes on startup
var routeStore = app.Services.GetRequiredService<IRouteStore>();
var configProvider = app.Services.GetRequiredService<DynamicConfigProvider>();
var configFactory = app.Services.GetRequiredService<IYarpConfigFactory>();

try
{
    var existingRoutes = await routeStore.LoadRoutesAsync();
    if (existingRoutes.Length > 0)
    {
        // Deduplicate routes by hostname (keep last occurrence)
        var deduplicatedRoutes = existingRoutes
            .GroupBy(r => r.Hostname, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Last())
            .ToArray();

        var routeConfigs = new List<RouteConfig>();
        var clusterConfigs = new List<ClusterConfig>();

        foreach (var route in deduplicatedRoutes)
        {
            var urls = route.GetBackendUrls();
            var (routeConfig, clusterConfig) = configFactory.CreateRouteClusterPair(
                route.Hostname, urls, route.Path);

            // Apply load balancing policy for multi-backend clusters
            if (urls.Length > 1)
            {
                clusterConfig = clusterConfig with
                {
                    LoadBalancingPolicy = route.LoadBalancingPolicy switch
                    {
                        LoadBalancingPolicy.RoundRobin => "RoundRobin",
                        LoadBalancingPolicy.LeastRequests => "LeastRequests",
                        LoadBalancingPolicy.Random => "Random",
                        LoadBalancingPolicy.First => "First",
                        _ => "PowerOfTwoChoices"
                    }
                };
            }

            routeConfigs.Add(routeConfig);
            clusterConfigs.Add(clusterConfig);
        }

        configProvider.Update(routeConfigs, clusterConfigs);
        var metrics = app.Services.GetRequiredService<IMetricsService>();
        metrics.UpdateActiveRoutes(routeConfigs.Count);
        logger.LogInformation("Loaded {Count} routes from persistence layer ({Duplicates} duplicates removed)",
            deduplicatedRoutes.Length, existingRoutes.Length - deduplicatedRoutes.Length);
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

// Also load routes from portless.config.yaml if present
var configLoader = app.Services.GetRequiredService<IPortlessConfigLoader>();
var fileConfig = configLoader.Load();
if (fileConfig.Routes.Count > 0)
{
    var configRouteInfos = configLoader.ToRouteInfos(fileConfig);
    var httpRoutes = configRouteInfos.Where(r => r.Type == RouteType.Http).ToArray();
    
    if (httpRoutes.Length > 0)
    {
        // Get current config to append (not replace)
        var currentConfig = configProvider.GetConfig();
        var allRoutes = currentConfig.Routes.ToList();
        var allClusters = currentConfig.Clusters.ToList();
        
        foreach (var route in httpRoutes)
        {
            var urls = route.GetBackendUrls();
            var (routeConfig, clusterConfig) = configFactory.CreateRouteClusterPair(
                route.Hostname, urls, route.Path);

            // Prefix with "config-" to identify config-file routes
            var configRouteId = $"config-{routeConfig.RouteId}";
            var configClusterId = $"config-{clusterConfig.ClusterId}";
            routeConfig = routeConfig with { RouteId = configRouteId, ClusterId = configClusterId };
            clusterConfig = clusterConfig with { ClusterId = configClusterId };

            // Apply load balancing policy for multi-backend clusters
            if (urls.Length > 1)
            {
                clusterConfig = clusterConfig with
                {
                    LoadBalancingPolicy = route.LoadBalancingPolicy switch
                    {
                        LoadBalancingPolicy.RoundRobin => "RoundRobin",
                        LoadBalancingPolicy.LeastRequests => "LeastRequests",
                        LoadBalancingPolicy.Random => "Random",
                        LoadBalancingPolicy.First => "First",
                        _ => "PowerOfTwoChoices"
                    }
                };
            }

            allRoutes.Add(routeConfig);
            allClusters.Add(clusterConfig);
        }
        
        configProvider.Update(allRoutes, allClusters);
        logger.LogInformation("Loaded {Count} HTTP routes from config file", httpRoutes.Length);
    }

    // Start TCP listeners from config file
    var tcpRoutes = configRouteInfos?.Where(r => r.Type == RouteType.Tcp).ToArray()
        ?? Array.Empty<RouteInfo>();
    if (tcpRoutes.Length > 0 && configRouteInfos != null)
    {
        var tcpForwarding = app.Services.GetRequiredService<ITcpForwardingService>();
        foreach (var route in tcpRoutes)
        {
            if (route.TcpListenPort.HasValue && route.Port > 0)
            {
                await tcpForwarding.StartListenerAsync(
                    $"config-{route.Hostname}",
                    route.TcpListenPort.Value,
                    "localhost",
                    route.Port);
            }
        }
        logger.LogInformation("Started {Count} TCP proxy listeners", tcpRoutes.Length);
    }
}

// Enable WebSockets for inspector live stream (must be before middleware that uses WS)
app.UseWebSockets();

// Serve dashboard static files (must be before proxy middleware)
app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true,
    DefaultContentType = "text/html"
});

app.UseMiddleware<RequestLoggingMiddleware>();

// Inspector middleware: captures all proxied traffic into ring buffer
app.UseMiddleware<InspectorMiddleware>();

// Plugin middleware: fires BeforeProxy/AfterProxy hooks
app.UseMiddleware<PluginMiddleware>();

// Load plugins on startup
var pluginLoader = app.Services.GetRequiredService<IPluginLoader>();
var stateDir = builder.Configuration["PORTLESS_STATE_DIR"] ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".portless");
var pluginsPath = Path.Combine(stateDir, "plugins");
if (Directory.Exists(pluginsPath))
{
    await pluginLoader.LoadAllAsync(pluginsPath);
    logger.LogInformation("Loaded {Count} plugins", pluginLoader.GetLoadedPlugins().Count);
}
else
{
    logger.LogInformation("No plugins directory found at {Path}, skipping plugin loading", pluginsPath);
}

// Prometheus metrics endpoint (excluded from proxy routing)
app.UseMetricServer("/metrics");

// Health check endpoint (excluded from proxy routing)
app.MapHealthChecks("/health");

app.MapPortlessApi(configProvider, routeStore, configFactory);

// Dashboard API endpoints
app.MapDashboardApi();

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

// Branded error pages middleware
app.Use(async (context, next) =>
{
    var originalBodyStream = context.Response.Body;
    using var responseBody = new MemoryStream();
    context.Response.Body = responseBody;

    await next();

    var hostname = context.Request.Host.Host;
    var statusCode = context.Response.StatusCode;

    // Only intercept HTML requests (not API, not WebSocket)
    var acceptHeader = context.Request.Headers["Accept"].ToString();
    var isHtmlRequest = acceptHeader.Contains("text/html") || string.IsNullOrEmpty(acceptHeader);

    if (isHtmlRequest && (statusCode == 404 || statusCode == 502))
    {
        // Reset and read the response
        responseBody.Seek(0, SeekOrigin.Begin);

        context.Response.Body = originalBodyStream;

        var routeStoreLocal = context.RequestServices.GetRequiredService<IRouteStore>();
        var routes = await routeStoreLocal.LoadRoutesAsync();
        var activeHostnames = routes
            .Where(r => !string.IsNullOrWhiteSpace(r.Hostname))
            .Select(r => r.Hostname)
            .ToList();

        string html;

        if (statusCode == 404)
        {
            html = ErrorPageGenerator.Generate404(hostname, activeHostnames);
        }
        else
        {
            // Try to find the backend port for more context
            var route = routes.FirstOrDefault(r => r.Hostname.Equals(hostname, StringComparison.OrdinalIgnoreCase));
            int? backendPort = route?.Port;
            html = ErrorPageGenerator.Generate502(hostname, backendPort, null);
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(html);
    }
    else
    {
        // Copy the response back to the original stream
        responseBody.Seek(0, SeekOrigin.Begin);
        context.Response.Body = originalBodyStream;
        await responseBody.CopyToAsync(originalBodyStream);
    }
});

app.MapReverseProxy();

app.Run();

// Request logging middleware
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly IMetricsService? _metricsService;
    private readonly IEventBus? _eventBus;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger, IMetricsService? metricsService = null, IEventBus? eventBus = null)
    {
        _next = next;
        _logger = logger;
        _metricsService = metricsService;
        _eventBus = eventBus;
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

            _metricsService?.RecordProxyRequest(host.Split(':')[0], method, statusCode, duration);

            // Publish event for dashboard live stream
            _eventBus?.Publish("request.completed", new { hostname = host.Split(':')[0], method, statusCode, durationMs = duration });

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