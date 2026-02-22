# SignalR Console Client

A .NET console application demonstrating SignalR client usage for real-time messaging.

## Overview

This console client connects to the SignalR chat server and allows you to send and receive messages from the command line. It demonstrates how to use the Microsoft.AspNetCore.SignalR.Client package to integrate SignalR into .NET applications.

## Prerequisites

- .NET 10 SDK
- SignalR chat server running (see `../SignalRChat/`)
- Portless.NET proxy running (if using proxy)

## Quick Start

### Run with Default URL

```bash
dotnet run --project Examples/SignalRChat.Client/
```

This connects to `http://localhost:5000/chathub` (direct connection to server).

### Run with Custom URL (Through Portless.NET Proxy)

```bash
dotnet run --project Examples/SignalRChat.Client/ -- http://chatsignalr.localhost:1355/chathub
```

This connects through the Portless.NET proxy.

### Run as Compiled Executable

```bash
cd Examples/SignalRChat.Client/
dotnet build
dotnet run -- http://chatsignalr.localhost:1355/chathub
```

## Usage

1. **Enter your username** when prompted
2. **Type messages** and press Enter to send
3. **Messages from other clients** will appear automatically
4. **Press Enter on empty line** to quit

## Example Session

```
SignalR Chat Console Client
============================
Connecting to: http://chatsignalr.localhost:1355/chathub

Connecting... Connected!

Enter your username: Alice

Connected as Alice
Type messages and press Enter to send.
Press Enter with empty line to quit.
--------------------------------------------------

Hello everyone!
[14:23:45] Bob: Hi Alice!
[14:23:50] Charlie: Welcome to the chat!
How's it going?
[14:24:02] Bob: Great, thanks!
```

## Features

- **Real-time messaging**: Receive messages instantly from other clients
- **Automatic reconnection**: Reconnects automatically if connection is lost
- **Connection events**: Displays connection status changes
- **Error handling**: Gracefully handles connection failures
- **Flexible URL**: Accept hub URL as command-line argument

## How It Works

### Connection

The client uses `HubConnectionBuilder` to establish a connection:

```csharp
var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect()
    .Build();
```

### Receiving Messages

Register a handler for the `ReceiveMessage` event:

```csharp
connection.On<string, string>("ReceiveMessage", (user, message) =>
{
    Console.WriteLine($"{user}: {message}");
});
```

### Sending Messages

Invoke server methods using `InvokeAsync`:

```csharp
await connection.InvokeAsync("SendMessage", username, message);
```

### Connection Events

Handle connection lifecycle events:

```csharp
connection.Reconnecting += error => { /* ... */ };
connection.Reconnected += connectionId => { /* ... */ };
connection.Closed += error => { /* ... */ };
```

## Troubleshooting

### Connection Failed

**Problem**: "Error: Cannot connect to server"

**Solutions**:
- Verify the chat server is running
- Check the hub URL is correct
- If using Portless.NET proxy:
  - Ensure proxy is running: `portless proxy status`
  - Check hostname is registered: `portless list`
  - Verify the URL includes the correct port (default: 1355)

### Messages Not Appearing

**Problem**: You can send but don't receive messages

**Solutions**:
- Check other clients are connected
- Verify server console for errors
- Ensure the `ReceiveMessage` handler is registered before connecting

### Reconnection Issues

**Problem**: Client doesn't reconnect after server restart

**Solutions**:
- This is normal behavior - client will attempt reconnection
- Check console for reconnection messages
- If reconnection fails after many attempts, restart the client

## Technical Details

### Project Structure

```
SignalRChat.Client/
├── Program.cs                    # Console client implementation
└── SignalRChat.Client.csproj     # Project file
```

### Dependencies

- .NET 10 SDK
- Microsoft.AspNetCore.SignalR.Client 8.0.0

### Transport

SignalR automatically negotiates the best transport:
1. WebSocket (preferred)
2. Server-Sent Events
3. Long Polling (fallback)

The client works with any transport supported by the server.

## Integration with Your Applications

To use SignalR in your .NET applications:

1. Add the package:
   ```xml
   <PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.0" />
   ```

2. Build a connection:
   ```csharp
   var connection = new HubConnectionBuilder()
       .WithUrl("http://your-server/hub")
       .Build();
   ```

3. Register handlers:
   ```csharp
   connection.On<EventType>("EventName", data => { /* handle */ });
   ```

4. Start the connection:
   ```csharp
   await connection.StartAsync();
   ```

5. Invoke server methods:
   ```csharp
   await connection.InvokeAsync("MethodName", args);
   ```

## Next Steps

- Try the browser client: `../SignalRChat/`
- Read the SignalR documentation: [ASP.NET Core SignalR](https://docs.microsoft.com/aspnet/core/signalr/)
- Learn about advanced features: Groups, Streams, Authentication

## License

Part of the Portless.NET project. See main repository for license information.
