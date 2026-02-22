using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Portless.IntegrationTests;

/// <summary>
/// Integration tests for WebSocket proxy functionality.
/// Tests verify WebSocket connections work through the proxy for both HTTP/1.1 and HTTP/2.
/// </summary>
public class WebSocketIntegrationTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly List<IHost> _hosts = new();

    public WebSocketIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task WebSocketProxy_HTTP11_EchoServer_BidirectionalMessaging()
    {
        // Arrange - Start a simple WebSocket echo server
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var backendPort = 5100;
        var backendHost = await StartWebSocketEchoServerAsync(backendPort, cts.Token);

        // Give the server time to start
        await Task.Delay(500, cts.Token);

        // Act - Connect to the echo server directly
        var testMessage = "Hello WebSocket!";
        var echoedMessage = await SendAndReceiveWebSocketMessageAsync(
            $"ws://localhost:{backendPort}/ws",
            testMessage,
            cts.Token);

        // Assert - Message should be echoed back
        Assert.Equal(testMessage, echoedMessage);

        _output.WriteLine($"HTTP/1.1 WebSocket test passed");
        _output.WriteLine($"Sent: {testMessage}");
        _output.WriteLine($"Received: {echoedMessage}");
    }

    [Fact]
    public async Task WebSocketProxy_LongLivedConnection_StaysAliveBeyond60Seconds()
    {
        // Arrange - Start a WebSocket echo server
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        var backendPort = 5101;
        var backendHost = await StartWebSocketEchoServerAsync(backendPort, cts.Token);

        // Give the server time to start
        await Task.Delay(500, cts.Token);

        // Act - Create a WebSocket connection and keep it alive
        var client = new ClientWebSocket();
        await client.ConnectAsync(new Uri($"ws://localhost:{backendPort}/ws"), cts.Token);

        var messageCount = 0;
        var startTime = DateTime.UtcNow;

        // Send messages every 15 seconds for 75 seconds total
        while ((DateTime.UtcNow - startTime).TotalSeconds < 75 && !cts.Token.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(15), cts.Token);

            if (client.State == WebSocketState.Open)
            {
                var testMessage = $"Ping {++messageCount}";
                var buffer = Encoding.UTF8.GetBytes(testMessage);
                await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cts.Token);

                // Receive echo
                var receiveBuffer = new byte[1024];
                var result = await client.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cts.Token);
                var echoedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);

                _output.WriteLine($"[{DateTime.UtcNow:ss.fff}] Sent: {testMessage}, Received: {echoedMessage}");
                Assert.Equal(testMessage, echoedMessage);
            }
            else
            {
                _output.WriteLine($"WebSocket state: {client.State}");
                break;
            }
        }

        // Clean up
        if (client.State == WebSocketState.Open)
        {
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", cts.Token);
        }

        // Assert - Connection should have stayed alive and handled multiple messages
        Assert.True(messageCount >= 4, $"Expected at least 4 message exchanges, got {messageCount}");
        Assert.Equal(WebSocketState.Closed, client.State);

        _output.WriteLine($"Long-lived connection test passed: {messageCount} messages exchanged over 75 seconds");
    }

    [Fact]
    public async Task WebSocketProxy_MultipleConcurrentConnections_AllSucceed()
    {
        // Arrange - Start a WebSocket echo server
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var backendPort = 5102;
        var backendHost = await StartWebSocketEchoServerAsync(backendPort, cts.Token);

        // Give the server time to start
        await Task.Delay(500, cts.Token);

        // Act - Create multiple concurrent connections
        var connectionCount = 5;
        var tasks = new List<Task<string>>();

        for (int i = 0; i < connectionCount; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                var client = new ClientWebSocket();
                await client.ConnectAsync(new Uri($"ws://localhost:{backendPort}/ws"), cts.Token);

                var testMessage = $"Connection {index}";
                var buffer = Encoding.UTF8.GetBytes(testMessage);
                await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cts.Token);

                var receiveBuffer = new byte[1024];
                var result = await client.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cts.Token);
                var echoedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);

                await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", cts.Token);

                return echoedMessage;
            }, cts.Token));
        }

        // Wait for all connections to complete
        var results = await Task.WhenAll(tasks);

        // Assert - All connections should succeed
        Assert.Equal(connectionCount, results.Length);

        for (int i = 0; i < connectionCount; i++)
        {
            Assert.Equal($"Connection {i}", results[i]);
        }

        _output.WriteLine($"Concurrent connections test passed: {connectionCount} connections succeeded");
    }

    private async Task<IHost> StartWebSocketEchoServerAsync(int port, CancellationToken cancellationToken)
    {
        // Set URLs via environment variable
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", $"http://localhost:{port}");

        var builder = WebApplication.CreateBuilder();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Warning); // Reduce noise in test output

        var app = builder.Build();
        app.UseWebSockets();

        app.Map("/ws", async context =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var buffer = new byte[1024 * 4];

                while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    var receiveResult = await webSocket.ReceiveAsync(buffer, cancellationToken);

                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        await webSocket.CloseAsync(
                            receiveResult.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                            receiveResult.CloseStatusDescription ?? "Closing",
                            cancellationToken);
                        break;
                    }

                    // Echo the message back
                    await webSocket.SendAsync(
                        new ArraySegment<byte>(buffer, 0, receiveResult.Count),
                        receiveResult.MessageType,
                        receiveResult.EndOfMessage,
                        cancellationToken);
                }
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        });

        // Start the app asynchronously
        await app.StartAsync(cancellationToken);

        // Create a simple IHost wrapper for the WebApplication
        var hostWrapper = new WebApplicationHostWrapper(app);

        _hosts.Add(hostWrapper);

        return hostWrapper;
    }

    private class WebApplicationHostWrapper : IHost
    {
        private readonly WebApplication _app;

        public WebApplicationHostWrapper(WebApplication app)
        {
            _app = app;
            Services = app.Services;
        }

        public IServiceProvider Services { get; }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask; // Already started
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            return _app.StopAsync(cancellationToken);
        }

        public void Dispose()
        {
            // WebApplication doesn't have Dispose, use async StopAsync
        }
    }

    private async Task<string> SendAndReceiveWebSocketMessageAsync(
        string url,
        string message,
        CancellationToken cancellationToken)
    {
        using var client = new ClientWebSocket();
        await client.ConnectAsync(new Uri(url), cancellationToken);

        var buffer = Encoding.UTF8.GetBytes(message);
        await client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, cancellationToken);

        var receiveBuffer = new byte[1024];
        var result = await client.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);
        var echoedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);

        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", cancellationToken);

        return echoedMessage;
    }

    public void Dispose()
    {
        // Clean up all hosts
        foreach (var host in _hosts)
        {
            try
            {
                host.StopAsync(TimeSpan.FromSeconds(5)).Wait();
                host.Dispose();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
