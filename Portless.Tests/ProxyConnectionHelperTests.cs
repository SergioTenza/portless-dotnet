using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Moq;
using Portless.Core.Services;
using Xunit;

namespace Portless.Tests;

public class ProxyConnectionHelperTests
{
    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    [Fact]
    public async Task IsProxyRunningAsync_ReturnsFalse_WhenNothingListening()
    {
        // Use a port we know is free (ephemeral)
        var port = GetFreePort();
        Environment.SetEnvironmentVariable("PORTLESS_PORT", port.ToString());
        try
        {
            var helper = new ProxyConnectionHelper();
            var result = await helper.IsProxyRunningAsync();
            Assert.False(result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PORTLESS_PORT", null);
        }
    }

    [Fact]
    public async Task IsProxyRunningAsync_ReturnsTrue_WhenPortIsListening()
    {
        // Use an ephemeral port to avoid conflicts
        var port = GetFreePort();
        Environment.SetEnvironmentVariable("PORTLESS_PORT", port.ToString());
        var listener = new TcpListener(IPAddress.Loopback, port);

        try
        {
            listener.Start();
            var helper = new ProxyConnectionHelper();
            var result = await helper.IsProxyRunningAsync();
            Assert.True(result);
        }
        finally
        {
            listener.Stop();
            Environment.SetEnvironmentVariable("PORTLESS_PORT", null);
        }
    }

    [Fact]
    public async Task IsProxyRunningAsync_ReturnsFalse_WhenConnectionRefused()
    {
        var port = GetFreePort();
        Environment.SetEnvironmentVariable("PORTLESS_PORT", port.ToString());
        try
        {
            var helper = new ProxyConnectionHelper();
            // Port is free (GetFreePort opened/closed it), nothing listening
            var result = await helper.IsProxyRunningAsync();
            Assert.False(result);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PORTLESS_PORT", null);
        }
    }
}
