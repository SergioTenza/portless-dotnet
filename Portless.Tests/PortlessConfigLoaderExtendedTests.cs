using Portless.Core.Models;
using Portless.Core.Services;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;

namespace Portless.Tests;

public class PortlessConfigLoaderExtendedTests
{
    private readonly PortlessConfigLoader _loader;
    private readonly string _tempDir;

    public PortlessConfigLoaderExtendedTests()
    {
        var logger = new Mock<ILogger<PortlessConfigLoader>>().Object;
        _loader = new PortlessConfigLoader(logger);
        _tempDir = Path.Combine(Path.GetTempPath(), $"portless-test-cfg-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void FindConfigFile_ReturnsNull_WhenNoConfigExists()
    {
        var result = _loader.FindConfigFile(_tempDir);
        Assert.Null(result);
    }

    [Fact]
    public void FindConfigFile_ReturnsPath_WhenConfigExists()
    {
        var configPath = Path.Combine(_tempDir, "portless.config.yaml");
        File.WriteAllText(configPath, "routes: []");

        var result = _loader.FindConfigFile(_tempDir);
        Assert.Equal(configPath, result);
    }

    [Fact]
    public void FindConfigFile_SearchesParentDirectories()
    {
        var subDir = Path.Combine(_tempDir, "sub", "deep");
        Directory.CreateDirectory(subDir);
        var configPath = Path.Combine(_tempDir, "portless.config.yaml");
        File.WriteAllText(configPath, "routes: []");

        var result = _loader.FindConfigFile(subDir);
        Assert.Equal(configPath, result);
    }

    [Fact]
    public void Load_WithNullPathAndNoConfig_ReturnsEmptyConfig()
    {
        // Load with null path when no config file exists in temp dir
        var origDir = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(_tempDir);
            var result = _loader.Load();
            Assert.Empty(result.Routes);
        }
        finally
        {
            Directory.SetCurrentDirectory(origDir);
        }
    }

    [Fact]
    public void Load_WithValidYaml_ReturnsConfig()
    {
        var configPath = Path.Combine(_tempDir, "portless.config.yaml");
        File.WriteAllText(configPath, @"
routes:
  - host: api.test.local
    backends:
      - http://localhost:5000
");
        var result = _loader.Load(configPath);
        Assert.Single(result.Routes);
        Assert.Equal("api.test.local", result.Routes[0].Host);
    }

    [Fact]
    public void Load_WithEmptyYaml_ReturnsEmptyConfig()
    {
        var configPath = Path.Combine(_tempDir, "portless.config.yaml");
        File.WriteAllText(configPath, "");
        var result = _loader.Load(configPath);
        Assert.Empty(result.Routes);
    }

    [Fact]
    public void ToRouteInfos_MultipleRoutes_AllConverted()
    {
        var config = new PortlessConfig
        {
            Routes = new List<PortlessRouteConfig>
            {
                new() { Host = "api.localhost", Backends = new List<string> { "http://localhost:5000" } },
                new() { Host = "web.localhost", Backends = new List<string> { "http://localhost:3000" } },
                new() { Host = "admin.localhost", Backends = new List<string> { "http://localhost:8080" } }
            }
        };

        var routes = _loader.ToRouteInfos(config);
        Assert.Equal(3, routes.Length);
        Assert.Equal("api.localhost", routes[0].Hostname);
        Assert.Equal("web.localhost", routes[1].Hostname);
        Assert.Equal("admin.localhost", routes[2].Hostname);
    }

    [Fact]
    public void ToRouteInfos_LoadBalancingPolicies_AllParsed()
    {
        var policies = new[] { "roundrobin", "leastrequests", "random", "first", "poweroftwochoices", null, "unknown" };
        var expected = new[] { LoadBalancingPolicy.RoundRobin, LoadBalancingPolicy.LeastRequests, LoadBalancingPolicy.Random, LoadBalancingPolicy.First, LoadBalancingPolicy.PowerOfTwoChoices, LoadBalancingPolicy.PowerOfTwoChoices, LoadBalancingPolicy.PowerOfTwoChoices };

        for (int i = 0; i < policies.Length; i++)
        {
            var config = new PortlessConfig
            {
                Routes = new List<PortlessRouteConfig>
                {
                    new()
                    {
                        Host = "test.localhost",
                        Backends = new List<string> { "http://localhost:5000" },
                        LoadBalancePolicy = policies[i]
                    }
                }
            };

            var routes = _loader.ToRouteInfos(config);
            Assert.Equal(expected[i], routes[0].LoadBalancingPolicy);
        }
    }

    [Fact]
    public void ToRouteInfos_InvalidBackendUrl_PortIsZero()
    {
        var config = new PortlessConfig
        {
            Routes = new List<PortlessRouteConfig>
            {
                new() { Host = "test.localhost", Backends = new List<string> { "not-a-url" } }
            }
        };

        var routes = _loader.ToRouteInfos(config);
        Assert.Single(routes);
        Assert.Equal(0, routes[0].Port);
    }

    [Fact]
    public void ToRouteInfos_EmptyBackend_PortIsZero()
    {
        var config = new PortlessConfig
        {
            Routes = new List<PortlessRouteConfig>
            {
                new() { Host = "test.localhost", Backends = new List<string>() }
            }
        };

        var routes = _loader.ToRouteInfos(config);
        Assert.Single(routes);
        Assert.Equal(0, routes[0].Port);
        Assert.Equal("http", routes[0].BackendProtocol);
    }

    [Fact]
    public void ToRouteInfos_HttpsBackend_SetsProtocol()
    {
        var config = new PortlessConfig
        {
            Routes = new List<PortlessRouteConfig>
            {
                new() { Host = "secure.localhost", Backends = new List<string> { "https://localhost:8443" } }
            }
        };

        var routes = _loader.ToRouteInfos(config);
        Assert.Single(routes);
        Assert.Equal(8443, routes[0].Port);
        Assert.Equal("https", routes[0].BackendProtocol);
    }

    [Fact]
    public void Load_WithMultipleRoutesInYaml_ReturnsAll()
    {
        var configPath = Path.Combine(_tempDir, "portless.config.yaml");
        File.WriteAllText(configPath, @"
routes:
  - host: api.test.local
    backends:
      - http://localhost:5000
  - host: web.test.local
    backends:
      - http://localhost:3000
      - http://localhost:3001
    loadBalance: RoundRobin
  - host: redis.test.local
    type: tcp
    listenPort: 6379
    backends:
      - localhost:6379
");
        var result = _loader.Load(configPath);
        Assert.Equal(3, result.Routes.Count);

        var routeInfos = _loader.ToRouteInfos(result);
        Assert.Equal(3, routeInfos.Length);
        Assert.Equal(RouteType.Http, routeInfos[0].Type);
        Assert.Equal(RouteType.Http, routeInfos[1].Type);
        Assert.Equal(RouteType.Tcp, routeInfos[2].Type);
        Assert.Equal(6379, routeInfos[2].TcpListenPort);
        Assert.Equal(LoadBalancingPolicy.RoundRobin, routeInfos[1].LoadBalancingPolicy);
    }
}
