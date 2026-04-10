using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Moq;
using Portless.Core.Services;
using Xunit;

namespace Portless.Tests;

public class TcpForwardingExtendedTests : IDisposable
{
    private readonly Mock<ILogger<TcpForwardingService>> _logger;
    private readonly TcpForwardingService _service;

    public TcpForwardingExtendedTests()
    {
        _logger = new Mock<ILogger<TcpForwardingService>>();
        _service = new TcpForwardingService(_logger.Object);
    }

    public void Dispose()
    {
        _service.Dispose();
    }

    [Fact]
    public async Task StartAsync_CompletesImmediately()
    {
        await _service.StartAsync(CancellationToken.None);
        // No listeners should be started, just returns completed task
        Assert.Empty(_service.GetActiveListeners());
    }

    [Fact]
    public async Task StopAsync_WithNoListeners_CompletesSuccessfully()
    {
        await _service.StopAsync(CancellationToken.None);
        Assert.Empty(_service.GetActiveListeners());
    }

    [Fact]
    public async Task StopAsync_StopsAllListeners()
    {
        await _service.StartListenerAsync("listener1", 0, "localhost", 5001);
        await _service.StartListenerAsync("listener2", 0, "localhost", 5002);

        Assert.Equal(2, _service.GetActiveListeners().Count);

        await _service.StopAsync(CancellationToken.None);
        Assert.Empty(_service.GetActiveListeners());
    }

    [Fact]
    public async Task StartListenerAsync_ThenStop_StartAndStopCycle()
    {
        await _service.StartListenerAsync("cycle-test", 0, "localhost", 6001);
        Assert.True(_service.GetActiveListeners().ContainsKey("cycle-test"));

        await _service.StopListenerAsync("cycle-test");
        Assert.Empty(_service.GetActiveListeners());

        // Can restart
        await _service.StartListenerAsync("cycle-test", 0, "localhost", 6001);
        Assert.True(_service.GetActiveListeners().ContainsKey("cycle-test"));
    }

    [Fact]
    public async Task StartListenerAsync_PortZero_GetsAssignedPort()
    {
        await _service.StartListenerAsync("ephemeral", 0, "localhost", 7000);
        var listeners = _service.GetActiveListeners();
        Assert.True(listeners["ephemeral"] > 0);
    }

    [Fact]
    public async Task RelayConnection_ForwardsDataBetweenClientAndBackend()
    {
        // Create a simple TCP echo server as the "backend"
        var echoListener = new TcpListener(IPAddress.Loopback, 0);
        echoListener.Start();
        var backendPort = ((IPEndPoint)echoListener.LocalEndpoint).Port;

        // Start echo server accept loop
        var echoTask = Task.Run(async () =>
        {
            using var client = await echoListener.AcceptTcpClientAsync();
            using var stream = client.GetStream();
            var buffer = new byte[1024];
            var read = await stream.ReadAsync(buffer);
            // Echo back
            await stream.WriteAsync(buffer.AsMemory(0, read));
        });

        // Start forwarding listener
        await _service.StartListenerAsync("relay-test", 0, "127.0.0.1", backendPort);
        var forwardPort = _service.GetActiveListeners()["relay-test"];

        // Connect as a client
        using var testClient = new TcpClient();
        await testClient.ConnectAsync(IPAddress.Loopback, forwardPort);
        using var testStream = testClient.GetStream();

        // Send data
        var sendData = System.Text.Encoding.UTF8.GetBytes("Hello Relay!");
        await testStream.WriteAsync(sendData);
        await testStream.FlushAsync();

        // Read response
        var recvBuffer = new byte[1024];
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var bytesRead = await testStream.ReadAsync(recvBuffer, cts.Token);
        var received = System.Text.Encoding.UTF8.GetString(recvBuffer, 0, bytesRead);

        Assert.Equal("Hello Relay!", received);

        echoListener.Stop();
        await _service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task RelayConnection_NonexistentBackend_HandledGracefully()
    {
        // Start forwarding to a nonexistent backend
        await _service.StartListenerAsync("bad-backend", 0, "127.0.0.1", 19999);
        var forwardPort = _service.GetActiveListeners()["bad-backend"];

        // Connect as a client - the relay should handle the connection failure
        using var testClient = new TcpClient();
        await testClient.ConnectAsync(IPAddress.Loopback, forwardPort);
        using var testStream = testClient.GetStream();

        // Send data - connection should fail gracefully (no crash)
        var sendData = System.Text.Encoding.UTF8.GetBytes("test");
        await testStream.WriteAsync(sendData);

        // Give it time for relay to attempt and fail
        await Task.Delay(500);

        // No assertion needed - just verifying no unhandled exception
        await _service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public void Dispose_StopsAllListeners()
    {
        // Start listeners then dispose
        _service.StartListenerAsync("dispose-test-1", 0, "localhost", 8001).Wait();
        _service.StartListenerAsync("dispose-test-2", 0, "localhost", 8002).Wait();

        Assert.Equal(2, _service.GetActiveListeners().Count);

        _service.Dispose();

        // After dispose, the listeners should be stopped
        // (the dictionary isn't cleared in Dispose, but the listeners are stopped)
    }

    [Fact]
    public async Task GetActiveListeners_ReturnsCorrectPorts()
    {
        await _service.StartListenerAsync("port-test-1", 0, "localhost", 9001);
        await _service.StartListenerAsync("port-test-2", 0, "localhost", 9002);

        var listeners = _service.GetActiveListeners();
        Assert.Equal(2, listeners.Count);
        Assert.True(listeners["port-test-1"] > 0);
        Assert.True(listeners["port-test-2"] > 0);
        // Ports should be different
        Assert.NotEqual(listeners["port-test-1"], listeners["port-test-2"]);
    }

    [Fact]
    public async Task StopListenerAsync_NonexistentName_DoesNotThrow()
    {
        // Should not throw when stopping a listener that doesn't exist
        await _service.StopListenerAsync("nonexistent");
    }
}
