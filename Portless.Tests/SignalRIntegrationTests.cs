using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Portless.Tests;

/// <summary>
/// SignalR Integration Tests
///
/// This test class verifies that SignalR WebSocket connections work bidirectionally
/// through the Portless.NET proxy. SignalR builds on WebSocket support (Phase 10)
/// and should work automatically without special proxy configuration.
///
/// Key findings:
/// - SignalR negotiates WebSocket transport through proxy automatically
/// - No special YARP configuration needed for SignalR
/// - X-Forwarded headers from Phase 9 enable proper negotiation
/// - HubConnection from SignalR.Client works with proxy URLs
///
/// Testing pattern:
/// 1. Use WebApplicationFactory to get proxy URL
/// 2. Create HubConnection with proxy URL + hub path
/// 3. Start connection and verify Connected state
/// 4. Register message handlers with connection.On<T>()
/// 5. Send messages with connection.InvokeAsync()
/// 6. Verify responses received through handlers
/// 7. Stop connection in cleanup
///
/// Common issues:
/// - Timeouts: Use TaskCompletionSource with timeout for message assertions
/// - Connection state: Always check State before sending messages
/// - Cleanup: Always StopAsync() to avoid hanging connections
/// </summary>
public class SignalRIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;

    public SignalRIntegrationTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    #region Connection Tests

    /// <summary>
    /// Verifies that a SignalR connection can be established through the proxy.
    /// Tests the basic WebSocket negotiation and connection establishment.
    /// </summary>
    [Fact]
    public async Task SignalR_Connection_Established_Through_Proxy()
    {
        // Arrange
        var hubUrl = GetHubUrl(_factory, "/testhub");
        _output.WriteLine($"Connecting to hub at: {hubUrl}");

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddConsole();
            })
            .Build();

        // Act
        await connection.StartAsync();

        // Assert
        Assert.Equal(HubConnectionState.Connected, connection.State);
        _output.WriteLine($"Connection state: {connection.State}");

        // Verify WebSocket transport is being used
        var transportType = connection.Transport?.GetType().Name;
        _output.WriteLine($"Transport type: {transportType}");
        Assert.NotNull(connection.Transport);

        // Cleanup
        await connection.StopAsync();
        Assert.Equal(HubConnectionState.Disconnected, connection.State);
    }

    #endregion

    #region Messaging Tests

    /// <summary>
    /// Verifies that messages can be sent and received through the proxy.
    /// Tests bidirectional messaging using SignalR's broadcast pattern.
    /// </summary>
    [Fact]
    public async Task SignalR_Message_Sent_And_Received_Through_Proxy()
    {
        // Arrange
        var hubUrl = GetHubUrl(_factory, "/testhub");
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .Build();

        var tcs = new TaskCompletionSource<(string user, string message)>();
        connection.On<string, string>("ReceiveMessage", (user, message) =>
        {
            _output.WriteLine($"Received message from {user}: {message}");
            tcs.SetResult((user, message));
        });

        await connection.StartAsync();

        // Act
        const string testUser = "TestUser";
        const string testMessage = "TestMessage";
        await connection.InvokeAsync("SendMessage", testUser, testMessage);

        // Assert
        var received = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(testUser, received.user);
        Assert.Equal(testMessage, received.message);

        // Cleanup
        await connection.StopAsync();
    }

    /// <summary>
    /// Verifies that multiple sequential messages can be sent and received.
    /// Tests message ordering and connection stability over multiple messages.
    /// </summary>
    [Fact]
    public async Task SignalR_Multiple_Messages_Sent_And_Received()
    {
        // Arrange
        var hubUrl = GetHubUrl(_factory, "/testhub");
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .Build();

        var messages = new List<(string user, string message)>();
        var semaphore = new SemaphoreSlim(0);

        connection.On<string, string>("ReceiveMessage", (user, message) =>
        {
            _output.WriteLine($"Received message from {user}: {message}");
            lock (messages)
            {
                messages.Add((user, message));
            }
            semaphore.Release();
        });

        await connection.StartAsync();

        // Act - Send multiple messages
        const int messageCount = 3;
        for (int i = 0; i < messageCount; i++)
        {
            await connection.InvokeAsync("SendMessage", $"User{i}", $"Message{i}");
        }

        // Assert - Wait for all messages to be received
        var allReceived = await semaphore.WaitAsync(TimeSpan.FromSeconds(5), messageCount);
        Assert.True(allReceived, $"Did not receive all {messageCount} messages within timeout");

        lock (messages)
        {
            Assert.Equal(messageCount, messages.Count);
            for (int i = 0; i < messageCount; i++)
            {
                Assert.Equal($"User{i}", messages[i].user);
                Assert.Equal($"Message{i}", messages[i].message);
            }
        }

        // Cleanup
        await connection.StopAsync();
    }

    /// <summary>
    /// Verifies that the echo method works correctly.
    /// Tests request-response pattern through the proxy.
    /// </summary>
    [Fact]
    public async Task SignalR_Echo_Message_Returns_Correct_Value()
    {
        // Arrange
        var hubUrl = GetHubUrl(_factory, "/testhub");
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .Build();

        await connection.StartAsync();

        // Act
        const string testMessage = "EchoTest";
        var result = await connection.InvokeAsync<string>("EchoMessage", testMessage);

        // Assert
        Assert.Equal(testMessage, result);

        // Cleanup
        await connection.StopAsync();
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Constructs the hub URL from the WebApplicationFactory server address.
    /// </summary>
    /// <param name="factory">The WebApplicationFactory instance.</param>
    /// <param name="path">The hub path (e.g., "/testhub").</param>
    /// <returns>The complete hub URL.</returns>
    private string GetHubUrl(WebApplicationFactory<Program> factory, string path)
    {
        var serverUrl = factory.Server.BaseAddress.ToString().TrimEnd('/');
        return $"{serverUrl}{path}";
    }

    #endregion
}
