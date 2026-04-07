using Portless.Core.Models;
using YamlDotNet.Serialization;
using Xunit;

namespace Portless.Tests;

public class PortlessConfigTests
{
    [Fact]
    public void Deserialize_SimpleConfig()
    {
        var yaml = @"
routes:
  - host: api.localhost
    backends:
      - http://localhost:5000
  - host: web.localhost
    path: /app
    backends:
      - http://localhost:3000
      - http://localhost:3001
    loadBalance: RoundRobin
";
        var deserializer = new DeserializerBuilder().Build();
        var config = deserializer.Deserialize<PortlessConfig>(yaml);

        Assert.Equal(2, config.Routes.Count);
        Assert.Equal("api.localhost", config.Routes[0].Host);
        Assert.Single(config.Routes[0].Backends);
        Assert.Equal("http://localhost:5000", config.Routes[0].Backends[0]);
        Assert.Null(config.Routes[0].Path);
        Assert.Equal("/app", config.Routes[1].Path);
        Assert.Equal(2, config.Routes[1].Backends.Count);
        Assert.Equal("RoundRobin", config.Routes[1].LoadBalancePolicy);
    }

    [Fact]
    public void Deserialize_EmptyConfig()
    {
        var yaml = "";
        var deserializer = new DeserializerBuilder().Build();
        var config = deserializer.Deserialize<PortlessConfig>(yaml);

        // Empty YAML returns null
        Assert.Null(config);
    }

    [Fact]
    public void Deserialize_ConfigWithNoRoutes()
    {
        var yaml = "routes: []";
        var deserializer = new DeserializerBuilder().Build();
        var config = deserializer.Deserialize<PortlessConfig>(yaml);

        Assert.NotNull(config);
        Assert.Empty(config.Routes);
    }

    [Fact]
    public void Deserialize_TcpRoute()
    {
        var yaml = @"
routes:
  - host: redis.localhost
    type: tcp
    listenPort: 6379
    backends:
      - localhost:6379
";
        var deserializer = new DeserializerBuilder().Build();
        var config = deserializer.Deserialize<PortlessConfig>(yaml);

        Assert.Single(config.Routes);
        Assert.Equal("tcp", config.Routes[0].Type);
        Assert.Equal(6379, config.Routes[0].ListenPort);
    }
}
