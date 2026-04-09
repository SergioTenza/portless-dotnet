using Yarp.ReverseProxy.Configuration;

namespace Portless.Tests;

/// <summary>
/// Factory helpers for creating YARP RouteConfig and ClusterConfig in tests.
/// Eliminates the repeated boilerplate of constructing these objects across
/// ProxyRoutingTests, XForwardedProtoTests, YarpProxyIntegrationTests,
/// Http2IntegrationTests, MixedProtocolRoutingTests, and ConfigFileWatcherTests.
/// </summary>
public static class YarpTestFactory
{
    /// <summary>
    /// Creates a RouteConfig with the given route ID, cluster ID, and optional path pattern.
    /// </summary>
    public static RouteConfig CreateRoute(
        string routeId,
        string clusterId,
        string path = "/{**catch-all}")
    {
        return new RouteConfig
        {
            RouteId = routeId,
            ClusterId = clusterId,
            Match = new RouteMatch { Path = path }
        };
    }

    /// <summary>
    /// Creates a RouteConfig with host-based matching.
    /// </summary>
    public static RouteConfig CreateRoute(
        string routeId,
        string clusterId,
        string[] hosts,
        string path = "/{**catch-all}")
    {
        return new RouteConfig
        {
            RouteId = routeId,
            ClusterId = clusterId,
            Match = new RouteMatch { Hosts = hosts, Path = path }
        };
    }

    /// <summary>
    /// Creates a ClusterConfig with a single backend destination.
    /// </summary>
    public static ClusterConfig CreateCluster(
        string clusterId,
        string backendAddress,
        string destinationKey = "backend1")
    {
        return new ClusterConfig
        {
            ClusterId = clusterId,
            Destinations = new Dictionary<string, DestinationConfig>
            {
                [destinationKey] = new DestinationConfig { Address = backendAddress }
            }
        };
    }

    /// <summary>
    /// Creates a ClusterConfig with multiple backend destinations (for load balancing tests).
    /// </summary>
    public static ClusterConfig CreateCluster(
        string clusterId,
        params (string key, string address)[] destinations)
    {
        return new ClusterConfig
        {
            ClusterId = clusterId,
            Destinations = destinations.ToDictionary(
                d => d.key,
                d => new DestinationConfig { Address = d.address })
        };
    }

    /// <summary>
    /// Creates a matched pair of RouteConfig + ClusterConfig for a single host.
    /// The routeId and clusterId are derived from the hostname.
    /// </summary>
    public static (RouteConfig route, ClusterConfig cluster) CreateHostRoute(
        string hostname,
        string backendAddress,
        string path = "/{**catch-all}")
    {
        var clusterId = $"{hostname}-cluster";
        return (
            CreateRoute($"{hostname}-route", clusterId, new[] { hostname }, path),
            CreateCluster(clusterId, backendAddress)
        );
    }
}
