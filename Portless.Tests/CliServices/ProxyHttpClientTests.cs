extern alias Cli;
using System.Net;
using Cli::Portless.Cli.Services;
using Moq;
using Xunit;

namespace Portless.Tests.CliServices;

public class ProxyHttpClientTests
{
    [Fact]
    public void CreateClient_ReturnsHttpClientWithTimeout()
    {
        var client = new ProxyHttpClient();
        var http = client.CreateClient();

        Assert.NotNull(http);
        Assert.Equal(TimeSpan.FromSeconds(10), http.Timeout);
    }

    [Fact]
    public async Task NotifyPluginReloadAsync_WhenProxyNotRunning_DoesNotThrow()
    {
        // Proxy is not running, so the HTTP call will fail with HttpRequestException
        // NotifyPluginReloadAsync should catch it silently
        var client = new ProxyHttpClient();

        // This should not throw even though proxy is not running
        await client.NotifyPluginReloadAsync();
    }

    [Fact]
    public void CreateClient_ReturnsNewInstanceEachTime()
    {
        var client = new ProxyHttpClient();
        var http1 = client.CreateClient();
        var http2 = client.CreateClient();

        Assert.NotSame(http1, http2);
    }

    [Fact]
    public void IProxyHttpClient_Interface_HasRequiredMethods()
    {
        // Verify the interface contract
        var interfaceType = typeof(IProxyHttpClient);
        var createClientMethod = interfaceType.GetMethod("CreateClient");
        var notifyReloadMethod = interfaceType.GetMethod("NotifyPluginReloadAsync");

        Assert.NotNull(createClientMethod);
        Assert.NotNull(notifyReloadMethod);
        Assert.Equal(typeof(HttpClient), createClientMethod.ReturnType);
        Assert.Equal(typeof(Task), notifyReloadMethod.ReturnType);
    }
}
