using System.Security.Authentication;
using Yarp.ReverseProxy.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Portless.Core.Configuration;
using Portless.Core.Models;
using Portless.Core.Services;

namespace Portless.Proxy;

/// <summary>
/// Maps the Portless internal API endpoints for route management.
/// Extracted from Program.cs for better organization.
/// </summary>
public static class PortlessApiEndpoints
{
    // Helper methods for creating YARP routes and clusters
    private static RouteConfig CreateRoute(string hostname, string clusterId) =>
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

    private static ClusterConfig CreateCluster(string clusterId, string backendUrl) =>
        new ClusterConfig
        {
            ClusterId = clusterId,
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["backend1"] = new DestinationConfig { Address = backendUrl }
            },
            // Add HttpClient configuration for SSL validation
            HttpClient = new Yarp.ReverseProxy.Configuration.HttpClientConfig
            {
                DangerousAcceptAnyServerCertificate = true, // Development mode only
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
            }
        };

    /// <summary>
    /// Maps the /api/v1/add-host and /api/v1/remove-host endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapPortlessApi(
        this IEndpointRouteBuilder endpoints,
        DynamicConfigProvider configProvider,
        IRouteStore routeStore)
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

                // Create new route and cluster
                var clusterId = $"cluster-{request.Hostname}";
                var newRoute = CreateRoute(request.Hostname, clusterId);
                var newCluster = CreateCluster(clusterId, request.BackendUrl);

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
                        CreatedAt = DateTime.UtcNow
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

        return endpoints;
    }
}

/// <summary>
/// Request model for adding a new host route.
/// </summary>
public record AddHostRequest(
    string Hostname,
    string BackendUrl
);
