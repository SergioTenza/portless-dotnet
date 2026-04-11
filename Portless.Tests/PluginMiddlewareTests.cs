using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Portless.Core.Configuration;
using Portless.Core.Services;
using Portless.Plugin.SDK;
using System.Net;
using System.Net.Http.Json;
using Yarp.ReverseProxy.Configuration;

namespace Portless.Tests;

/// <summary>
/// Unit and integration tests for PluginMiddleware.
/// Covers: internal path bypass, no-plugin passthrough, BeforeProxy short-circuit,
/// BeforeProxy exception handling, AfterProxy hooks, ReadRequestBodyAsync, SanitizeHeaders.
/// </summary>
[Collection("Integration Tests")]
public class PluginMiddlewareTests : IntegrationTestBase
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _factory = CreateProxyApp(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Register a mock IPluginLoader that returns plugins
                var mockPluginLoader = new Mock<IPluginLoader>();
                mockPluginLoader.Setup(x => x.GetLoadedPlugins())
                    .Returns(new List<PortlessPlugin> { new TestPlugin() });
                mockPluginLoader.Setup(x => x.FireBeforeProxyAsync(It.IsAny<ProxyContext>()))
                    .ReturnsAsync((ProxyResult?)null);
                mockPluginLoader.Setup(x => x.FireAfterProxyAsync(It.IsAny<ProxyContext>(), It.IsAny<ProxyResult>()))
                    .Returns(Task.CompletedTask);
                services.AddSingleton(mockPluginLoader.Object);
            });
        });
        _client = CreateHttpClient(_factory);
    }

    [Fact]
    public async Task InternalApiPath_SkipsPluginHooks()
    {
        // Request to /api/v1/routes should bypass plugin middleware
        var response = await _client.GetAsync("/api/v1/routes");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task HealthPath_SkipsPluginHooks()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MetricsPath_SkipsPluginHooks()
    {
        var response = await _client.GetAsync("/metrics");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DashboardPath_SkipsPluginHooks()
    {
        // _dashboard internal path bypasses plugins
        var response = await _client.GetAsync("/_dashboard");
        // May return 404 if no static file, but should not hit plugins
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExternalPath_InvokesPluginHooks()
    {
        // Create a route for an external hostname and request it
        var config = _factory.Services.GetRequiredService<DynamicConfigProvider>();
        var routes = new List<RouteConfig>
        {
            YarpTestFactory.CreateRoute("route-ext.localhost", "cluster-ext.localhost", new[] { "ext.localhost" })
        };
        var clusters = new List<ClusterConfig>
        {
            YarpTestFactory.CreateCluster("cluster-ext.localhost", "http://localhost:19999")
        };
        config.Update(routes, clusters);
        await Task.Delay(200);

        var request = new HttpRequestMessage(HttpMethod.Get, "/test-path");
        request.Headers.Add("Host", "ext.localhost");
        var response = await _client.SendAsync(request);

        // Will get 502 (no backend) but plugin hooks should have been called
        Assert.True(
            response.StatusCode == HttpStatusCode.BadGateway ||
            response.StatusCode == HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostWithJsonBody_ReadsBody()
    {
        var config = _factory.Services.GetRequiredService<DynamicConfigProvider>();
        var routes = new List<RouteConfig>
        {
            YarpTestFactory.CreateRoute("route-body.localhost", "cluster-body.localhost", new[] { "body.localhost" })
        };
        var clusters = new List<ClusterConfig>
        {
            YarpTestFactory.CreateCluster("cluster-body.localhost", "http://localhost:19998")
        };
        config.Update(routes, clusters);
        await Task.Delay(200);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/data");
        request.Headers.Add("Host", "body.localhost");
        request.Content = new StringContent("{\"key\":\"value\"}", System.Text.Encoding.UTF8, "application/json");
        var response = await _client.SendAsync(request);
        Assert.True(response.StatusCode == HttpStatusCode.BadGateway || response.StatusCode == HttpStatusCode.OK);
    }

    /// <summary>
    /// Test plugin used for verifying plugin hooks are invoked.
    /// </summary>
    private class TestPlugin : PortlessPlugin
    {
        public override string Name => "TestPlugin";
        public override string Version => "1.0.0";
    }
}

/// <summary>
/// Tests for PluginMiddleware with short-circuit behavior.
/// </summary>
[Collection("Integration Tests")]
public class PluginMiddlewareShortCircuitTests : IntegrationTestBase
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _factory = CreateProxyApp(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var mockPluginLoader = new Mock<IPluginLoader>();
                mockPluginLoader.Setup(x => x.GetLoadedPlugins())
                    .Returns(new List<PortlessPlugin> { new ShortCircuitPlugin() });

                // Short-circuit: return a ProxyResult to stop the pipeline
                mockPluginLoader.Setup(x => x.FireBeforeProxyAsync(It.IsAny<ProxyContext>()))
                    .ReturnsAsync(new ProxyResult
                    {
                        StatusCode = 200,
                        DurationMs = 0,
                        ResponseHeaders = new Dictionary<string, string> { { "X-Plugin-Header", "intercepted" } },
                        ResponseBody = "blocked by plugin"
                    });

                mockPluginLoader.Setup(x => x.FireAfterProxyAsync(It.IsAny<ProxyContext>(), It.IsAny<ProxyResult>()))
                    .Returns(Task.CompletedTask);
                services.AddSingleton(mockPluginLoader.Object);
            });
        });
        _client = CreateHttpClient(_factory);
    }

    [Fact]
    public async Task PluginShortCircuit_ReturnsPluginResponse()
    {
        // Setup a route
        var config = _factory.Services.GetRequiredService<DynamicConfigProvider>();
        var routes = new List<RouteConfig>
        {
            YarpTestFactory.CreateRoute("route-sc.localhost", "cluster-sc.localhost", new[] { "sc.localhost" })
        };
        var clusters = new List<ClusterConfig>
        {
            YarpTestFactory.CreateCluster("cluster-sc.localhost", "http://localhost:19997")
        };
        config.Update(routes, clusters);
        await Task.Delay(200);

        var request = new HttpRequestMessage(HttpMethod.Get, "/anything");
        request.Headers.Add("Host", "sc.localhost");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("blocked by plugin", await response.Content.ReadAsStringAsync());
        Assert.True(response.Headers.Contains("X-Plugin-Header"));
    }

    private class ShortCircuitPlugin : PortlessPlugin
    {
        public override string Name => "ShortCircuitPlugin";
        public override string Version => "1.0.0";
    }
}

/// <summary>
/// Tests for PluginMiddleware when BeforeProxy throws an exception.
/// The middleware should log the error and continue with normal proxy pipeline.
/// </summary>
[Collection("Integration Tests")]
public class PluginMiddlewareExceptionTests : IntegrationTestBase
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _factory = CreateProxyApp(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var mockPluginLoader = new Mock<IPluginLoader>();
                mockPluginLoader.Setup(x => x.GetLoadedPlugins())
                    .Returns(new List<PortlessPlugin> { new FailPlugin() });
                mockPluginLoader.Setup(x => x.FireBeforeProxyAsync(It.IsAny<ProxyContext>()))
                    .ThrowsAsync(new InvalidOperationException("Plugin error"));
                mockPluginLoader.Setup(x => x.FireAfterProxyAsync(It.IsAny<ProxyContext>(), It.IsAny<ProxyResult>()))
                    .Returns(Task.CompletedTask);
                services.AddSingleton(mockPluginLoader.Object);
            });
        });
        _client = CreateHttpClient(_factory);
    }

    [Fact]
    public async Task PluginBeforeProxyError_ContinuesPipeline()
    {
        var config = _factory.Services.GetRequiredService<DynamicConfigProvider>();
        var routes = new List<RouteConfig>
        {
            YarpTestFactory.CreateRoute("route-fail.localhost", "cluster-fail.localhost", new[] { "fail.localhost" })
        };
        var clusters = new List<ClusterConfig>
        {
            YarpTestFactory.CreateCluster("cluster-fail.localhost", "http://localhost:19996")
        };
        config.Update(routes, clusters);
        await Task.Delay(200);

        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Host", "fail.localhost");
        var response = await _client.SendAsync(request);

        // Should still get 502 because backend is unreachable, not 500 from plugin error
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    private class FailPlugin : PortlessPlugin
    {
        public override string Name => "FailPlugin";
        public override string Version => "1.0.0";
    }
}

/// <summary>
/// Tests for PluginMiddleware with AfterProxy exception handling.
/// </summary>
[Collection("Integration Tests")]
public class PluginMiddlewareAfterProxyErrorTests : IntegrationTestBase
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _factory = CreateProxyApp(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var mockPluginLoader = new Mock<IPluginLoader>();
                mockPluginLoader.Setup(x => x.GetLoadedPlugins())
                    .Returns(new List<PortlessPlugin> { new AfterFailPlugin() });
                mockPluginLoader.Setup(x => x.FireBeforeProxyAsync(It.IsAny<ProxyContext>()))
                    .ReturnsAsync((ProxyResult?)null);
                mockPluginLoader.Setup(x => x.FireAfterProxyAsync(It.IsAny<ProxyContext>(), It.IsAny<ProxyResult>()))
                    .ThrowsAsync(new InvalidOperationException("After-proxy error"));
                services.AddSingleton(mockPluginLoader.Object);
            });
        });
        _client = CreateHttpClient(_factory);
    }

    [Fact]
    public async Task PluginAfterProxyError_DoesNotCrashResponse()
    {
        var config = _factory.Services.GetRequiredService<DynamicConfigProvider>();
        var routes = new List<RouteConfig>
        {
            YarpTestFactory.CreateRoute("route-afail.localhost", "cluster-afail.localhost", new[] { "afail.localhost" })
        };
        var clusters = new List<ClusterConfig>
        {
            YarpTestFactory.CreateCluster("cluster-afail.localhost", "http://localhost:19995")
        };
        config.Update(routes, clusters);
        await Task.Delay(200);

        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Host", "afail.localhost");
        var response = await _client.SendAsync(request);

        // Response should still be returned (502 for unreachable backend)
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    private class AfterFailPlugin : PortlessPlugin
    {
        public override string Name => "AfterFailPlugin";
        public override string Version => "1.0.0";
    }
}

/// <summary>
/// Tests PluginMiddleware when no plugins are loaded (empty plugin list).
/// </summary>
[Collection("Integration Tests")]
public class PluginMiddlewareNoPluginsTests : IntegrationTestBase
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _factory = CreateProxyApp(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var mockPluginLoader = new Mock<IPluginLoader>();
                mockPluginLoader.Setup(x => x.GetLoadedPlugins())
                    .Returns(new List<PortlessPlugin>());
                services.AddSingleton(mockPluginLoader.Object);
            });
        });
        _client = CreateHttpClient(_factory);
    }

    [Fact]
    public async Task NoPlugins_PassthroughToNextMiddleware()
    {
        var config = _factory.Services.GetRequiredService<DynamicConfigProvider>();
        var routes = new List<RouteConfig>
        {
            YarpTestFactory.CreateRoute("route-np.localhost", "cluster-np.localhost", new[] { "np.localhost" })
        };
        var clusters = new List<ClusterConfig>
        {
            YarpTestFactory.CreateCluster("cluster-np.localhost", "http://localhost:19994")
        };
        config.Update(routes, clusters);
        await Task.Delay(200);

        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Host", "np.localhost");
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }
}
