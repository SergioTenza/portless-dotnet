using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;

namespace Portless.Core.Configuration;

public class DynamicConfigProvider : IProxyConfigProvider
{
    private volatile DynamicConfig _config;

    public DynamicConfigProvider()
    {
        _config = new DynamicConfig(Array.Empty<RouteConfig>(), Array.Empty<ClusterConfig>());
    }

    public IProxyConfig GetConfig() => _config;

    public void Update(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
    {
        var oldConfig = _config;
        _config = new DynamicConfig(routes, clusters);
        oldConfig.SignalChange();
    }
}

public class DynamicConfig : IProxyConfig
{
    private readonly IReadOnlyList<RouteConfig> _routes;
    private readonly IReadOnlyList<ClusterConfig> _clusters;
    private readonly CancellationChangeToken _changeToken;
    private CancellationTokenSource _cts;

    public DynamicConfig(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
    {
        _routes = routes;
        _clusters = clusters;
        _cts = new CancellationTokenSource();
        _changeToken = new CancellationChangeToken(_cts.Token);
    }

    public IReadOnlyList<RouteConfig> Routes => _routes;
    public IReadOnlyList<ClusterConfig> Clusters => _clusters;
    public IChangeToken ChangeToken => _changeToken;

    public void SignalChange()
    {
        _cts.Cancel();
        _cts = new CancellationTokenSource();
    }
}
