using Portless.Core.Services;
using Xunit;

namespace Portless.Tests;

public class LoadBalancingTests
{
    [Fact]
    public void CreateCluster_MultipleBackends_CreatesAllDestinations()
    {
        var factory = new YarpConfigFactory();
        var backends = new[] { "http://localhost:5000", "http://localhost:5001", "http://localhost:5002" };
        var (_, cluster) = factory.CreateRouteClusterPair("api.localhost", backends);
        
        Assert.Equal(3, cluster.Destinations!.Count);
        Assert.Equal("http://localhost:5000", cluster.Destinations["backend1"].Address);
        Assert.Equal("http://localhost:5001", cluster.Destinations["backend2"].Address);
        Assert.Equal("http://localhost:5002", cluster.Destinations["backend3"].Address);
    }

    [Fact]
    public void CreateCluster_SingleBackend_OneDestination()
    {
        var factory = new YarpConfigFactory();
        var (_, cluster) = factory.CreateRouteClusterPair("api.localhost", new[] { "http://localhost:5000" });
        
        Assert.Single(cluster.Destinations!);
        Assert.Equal("http://localhost:5000", cluster.Destinations["backend1"].Address);
    }
}
