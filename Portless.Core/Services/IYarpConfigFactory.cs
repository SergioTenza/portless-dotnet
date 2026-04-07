using Yarp.ReverseProxy.Configuration;

namespace Portless.Core.Services;

public interface IYarpConfigFactory
{
    RouteConfig CreateRoute(string hostname, string clusterId, string? path = null);
    ClusterConfig CreateCluster(string clusterId, string[] backendUrls);
    (RouteConfig Route, ClusterConfig Cluster) CreateRouteClusterPair(string hostname, string[] backendUrls, string? path = null);
}
