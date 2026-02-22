using Microsoft.AspNetCore.SignalR;

namespace SignalRChat;

public class ChatHub : Hub
{
    /// <summary>
    /// Sends a message to all connected clients.
    /// </summary>
    /// <param name="user">The username of the sender.</param>
    /// <param name="message">The message content.</param>
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }
}
