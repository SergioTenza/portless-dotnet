using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Portless.Core.Configuration;
using Portless.Core.Models;
using Portless.Core.Services;
using Yarp.ReverseProxy.Configuration;

namespace Portless.Proxy;

/// <summary>
/// Maps the Portless internal API endpoints for route management.
/// Extracted from Program.cs for better organization.
/// </summary>
public static class PortlessApiEndpoints
{
    /// <summary>
    /// Maps the /api/v1/add-host and /api/v1/remove-host endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapPortlessApi(
        this IEndpointRouteBuilder endpoints,
        DynamicConfigProvider configProvider,
        IRouteStore routeStore,
        IYarpConfigFactory configFactory)
    {
        // POST /api/v1/add-host
        endpoints.MapPost("/api/v1/add-host", async (AddHostRequest request, ILogger<Program> logger) =>
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

                // Get existing configuration to preserve other routes
                var existingConfig = configProvider.GetConfig();
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

                // Create new route and cluster using the factory
                // Build backend URL list: BackendUrls if provided, otherwise single BackendUrl
                var backendUrls = request.BackendUrls is { Length: > 0 }
                    ? request.BackendUrls
                    : new[] { request.BackendUrl };

                var (newRoute, newCluster) = configFactory.CreateRouteClusterPair(
                    request.Hostname, backendUrls, request.Path);

                // Apply load balancing policy if specified
                if (!string.IsNullOrEmpty(request.LoadBalancePolicy) && newCluster.Destinations!.Count > 1)
                {
                    newCluster = newCluster with
                    {
                        LoadBalancingPolicy = request.LoadBalancePolicy
                    };
                }

                // Add to existing configuration
                existingRoutes.Add(newRoute);
                existingClusters.Add(newCluster);

                // Update configuration
                configProvider.Update(existingRoutes, existingClusters);

                // Persist route to file
                logger.LogInformation("Attempting to persist route: {Hostname} => {BackendUrl}", request.Hostname, request.BackendUrl);

                try
                {
                    var allRoutes = await routeStore.LoadRoutesAsync();

                    // Remove any existing route with same hostname to prevent duplicates
                    var filteredRoutes = allRoutes
                        .Where(r => !r.Hostname.Equals(request.Hostname, StringComparison.OrdinalIgnoreCase))
                        .ToArray();

                    // Extract port from backendUrl
                    var uri = new Uri(request.BackendUrl);
                    var port = uri.Port;

                    var newRouteInfo = new RouteInfo
                    {
                        Hostname = request.Hostname,
                        Port = port,
                        Pid = Environment.ProcessId,
                        CreatedAt = DateTime.UtcNow,
                        Path = request.Path,
                        BackendUrls = backendUrls.Length > 1 ? backendUrls : null,
                        LoadBalancingPolicy = ParseLoadBalancingPolicy(request.LoadBalancePolicy),
                    };
                    var updatedRoutes = filteredRoutes.Append(newRouteInfo).ToArray();
                    logger.LogInformation("Saving {Count} routes to file (PID: {Pid})", updatedRoutes.Length, Environment.ProcessId);

                    await routeStore.SaveRoutesAsync(updatedRoutes);

                    logger.LogInformation("Route persisted successfully: {Hostname} => {Port}", request.Hostname, port);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error persisting route: {Hostname}", request.Hostname);
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
                        clusterId = newCluster.ClusterId,
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

        // DELETE /api/v1/remove-host
        endpoints.MapDelete("/api/v1/remove-host", async (string hostname, ILogger<Program> logger) =>
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

                var existingConfig = configProvider.GetConfig();
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
                configProvider.Update(existingRoutes, existingClusters);

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

        // POST /api/v1/tcp/add
        endpoints.MapPost("/api/v1/tcp/add", async (TcpAddRequest request, ILogger<Program> logger) =>
        {
            var tcpForwarding = endpoints.ServiceProvider.GetRequiredService<ITcpForwardingService>();
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name) || request.ListenPort <= 0 || string.IsNullOrWhiteSpace(request.TargetHost) || request.TargetPort <= 0)
                {
                    return Results.Problem("Name, ListenPort, TargetHost, and TargetPort are required", statusCode: 400);
                }

                await tcpForwarding.StartListenerAsync(request.Name, request.ListenPort, request.TargetHost, request.TargetPort);
                return Results.Ok(new { success = true, message = $"TCP listener '{request.Name}' started on port {request.ListenPort}" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error starting TCP listener");
                return Results.Problem(ex.Message, statusCode: 500);
            }
        });

        // DELETE /api/v1/tcp/remove
        endpoints.MapDelete("/api/v1/tcp/remove", (string name, ILogger<Program> logger) =>
        {
            var tcpForwarding = endpoints.ServiceProvider.GetRequiredService<ITcpForwardingService>();
            try
            {
                tcpForwarding.StopListenerAsync(name).GetAwaiter().GetResult();
                return Results.Ok(new { success = true, message = $"TCP listener '{name}' stopped" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error stopping TCP listener");
                return Results.Problem(ex.Message, statusCode: 500);
            }
        });

        return endpoints;
    }

    private static LoadBalancingPolicy ParseLoadBalancingPolicy(string? policy)
    {
        return policy?.ToLowerInvariant() switch
        {
            "roundrobin" => LoadBalancingPolicy.RoundRobin,
            "leastrequests" => LoadBalancingPolicy.LeastRequests,
            "random" => LoadBalancingPolicy.Random,
            "first" => LoadBalancingPolicy.First,
            _ => LoadBalancingPolicy.PowerOfTwoChoices
        };
    }
}

/// <summary>
/// Request model for adding a new host route.
/// </summary>
public record AddHostRequest(
    string Hostname,
    string BackendUrl,
    string? Path = null,
    string[]? BackendUrls = null,
    string? LoadBalancePolicy = null
);

public record TcpAddRequest(string Name, int ListenPort, string TargetHost, int TargetPort);
