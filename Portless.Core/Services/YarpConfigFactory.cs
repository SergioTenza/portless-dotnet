using System.Security.Authentication;
using Yarp.ReverseProxy.Configuration;

namespace Portless.Core.Services;

public class YarpConfigFactory : IYarpConfigFactory
{
    public RouteConfig CreateRoute(string hostname, string clusterId, string? path = null)
    {
        return new RouteConfig
        {
            RouteId = $"route-{hostname}",
            ClusterId = clusterId,
            Match = new RouteMatch
            {
                Hosts = new[] { hostname },
                Path = path ?? "/{**catch-all}"
            }
        };
    }

    public ClusterConfig CreateCluster(string clusterId, string[] backendUrls)
    {
        var destinations = new Dictionary<string, DestinationConfig>();
        for (int i = 0; i < backendUrls.Length; i++)
        {
            destinations[$"backend{i + 1}"] = new DestinationConfig { Address = backendUrls[i] };
        }

        return new ClusterConfig
        {
            ClusterId = clusterId,
            Destinations = destinations,
            HttpClient = new HttpClientConfig
            {
                DangerousAcceptAnyServerCertificate = true,
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
            }
        };
    }

    public (RouteConfig Route, ClusterConfig Cluster) CreateRouteClusterPair(
        string hostname, string[] backendUrls, string? path = null)
    {
        var clusterId = $"cluster-{hostname}";
        return (CreateRoute(hostname, clusterId, path), CreateCluster(clusterId, backendUrls));
    }
}
