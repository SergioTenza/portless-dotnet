# SignalR Chat Example

A real-time chat application demonstrating SignalR integration with Portless.NET proxy.

## Overview

This example shows how to build real-time communication applications using SignalR with WebSocket transport through the Portless.NET proxy. Multiple clients can connect and broadcast messages to all participants in real-time.

## Features

- Real-time bidirectional messaging using SignalR
- Browser-based client with modern UI
- WebSocket transport through Portless.NET proxy
- Automatic reconnection on connection loss
- Broadcast to all connected clients
- Connection status indicator

## Prerequisites

- .NET 10 SDK
- Portless.NET installed and proxy running
- Modern web browser with WebSocket support

## Quick Start

### 1. Start the Portless.NET Proxy

```bash
portless proxy start
```

### 2. Register and Run the Chat Server

```bash
portless chatsignalr -- dotnet run --project Examples/SignalRChat/
```

This command:
- Registers the hostname `chatsignalr.localhost`
- Assigns an available port automatically
- Starts the chat server with the PORT environment variable set

### 3. Open the Chat in Your Browser

Navigate to: `http://chatsignalr.localhost:1355`

Open multiple browser windows or tabs to test broadcast messaging.

## Using the Chat

### Browser Client

1. Enter your username (defaults to "User" + random number)
2. Type messages in the input field
3. Click "Send" or press Enter to broadcast
4. All connected clients will receive the message
5. Connection status shows in the header (green = connected)

### Testing Multiple Clients

To test broadcast functionality:

1. Open `http://chatsignalr.localhost:1355` in multiple browser windows
2. Use different usernames in each window
3. Send messages from any window
4. Verify all windows receive the messages

### Connection Features

- **Automatic Reconnection**: If the server restarts, clients automatically reconnect
- **Status Indicator**: Green dot = connected, Red dot = disconnected
- **Message History**: Messages appear in real-time as they're sent
- **Timestamps**: Each message shows when it was sent

## How It Works

### SignalR Hub

The `ChatHub` class defines the server-side hub:

```csharp
public class ChatHub : Hub
{
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }
}
```

### Client Connection

The browser client connects using the SignalR JavaScript client:

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chathub")
    .withAutomaticReconnect()
    .build();
```

### WebSocket Transport

SignalR automatically negotiates the best transport:
1. WebSocket (preferred)
2. Server-Sent Events
3. Long Polling (fallback)

Through Portless.NET, WebSocket connections work transparently.

## Troubleshooting

### Connection Issues

**Problem**: Browser shows "Disconnected - Reconnecting..."

**Solutions**:
- Verify Portless.NET proxy is running: `portless proxy status`
- Check the chat server is running
- Ensure hostname is registered: `portless list`
- Try refreshing the browser page

**Problem**: Messages don't appear

**Solutions**:
- Check browser console for errors (F12)
- Verify server is running without errors
- Try sending a message from another browser window
- Check that WebSocket is being used (browser DevTools Network tab)

### WebSocket Transport

**Verify WebSocket is being used**:

1. Open browser DevTools (F12)
2. Go to Network tab
3. Filter by "WS" (WebSocket)
4. Look for connection to `/chathub` with status "101 Switching Protocols"

If you see "EventSource" instead, SignalR is using Server-Sent Events fallback.

### Port Already in Use

**Problem**: "Port is already in use" error

**Solution**: Portless.NET automatically assigns available ports. If you see this error:
- Check what's using the port: `netstat -ano | findstr :PORT`
- Stop the conflicting process
- Try running the command again

## Technical Details

### Project Structure

```
SignalRChat/
├── ChatHub.cs              # SignalR hub with SendMessage method
├── Program.cs              # Server configuration and startup
├── wwwroot/
│   └── index.html          # Browser client (HTML + CSS + JS)
└── SignalRChat.csproj      # Project file
```

### Dependencies

- .NET 10 SDK
- Microsoft.AspNetCore.SignalR (included in ASP.NET Core)

### Port Configuration

The server reads the port from the `PORT` environment variable, which Portless.NET sets automatically. Default is port 5000 if not specified.

## Next Steps

- Try the console client example: `Examples/SignalRChat.Client/`
- Read the integration tests: `Portless.Tests/SignalRTests.cs`
- Learn about SignalR: [ASP.NET Core SignalR Documentation](https://docs.microsoft.com/aspnet/core/signalr/)

## License

Part of the Portless.NET project. See main repository for license information.
