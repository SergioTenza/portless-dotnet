using Microsoft.AspNetCore.SignalR;

namespace Portless.Proxy;

/// <summary>
/// Test SignalR hub for integration testing.
/// This hub is used exclusively by integration tests to verify SignalR connectivity through the proxy.
/// </summary>
public class TestChatHub : Hub
{
    /// <summary>
    /// Sends a message to all connected clients.
    /// Used by integration tests to verify bidirectional messaging.
    /// </summary>
    /// <param name="user">The username of the sender.</param>
    /// <param name="message">The message content.</param>
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    /// <summary>
    /// Echoes a message back to the caller.
    /// Used by integration tests to verify request-response pattern.
    /// </summary>
    /// <param name="message">The message to echo.</param>
    /// <returns>The same message that was sent.</returns>
    public async Task<string> EchoMessage(string message)
    {
        await Clients.Caller.SendAsync("ReceiveEcho", message);
        return message;
    }
}
