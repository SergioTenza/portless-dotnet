# Portless.NET Examples

Collection of example applications demonstrating Portless.NET features, including HTTP/2, WebSocket, and SignalR support in v1.1.

## Quick Start

All examples require the proxy to be running:

```bash
# Install Portless.NET (if not already installed)
dotnet tool install --add-source . portless.dotnet

# Start the proxy
portless proxy start
```

---

## v1.1 Examples

### WebSocketEchoServer Example

**Location:** `Examples/WebSocketEchoServer`

**Demonstrates:** WebSocket proxy support and long-lived connections through Portless.NET

**Quick Start:**

```bash
# Terminal 1: Start proxy
portless proxy start

# Terminal 2: Run WebSocket echo server
portless echoserver dotnet run --project Examples/WebSocketEchoServer

# Terminal 3: Test in browser
# Open http://echoserver.localhost:1355
# Send messages - they should echo back
```

**What This Example Shows:**
- WebSocket connection through proxy (RFC 6455)
- Bidirectional messaging with echo functionality
- Browser-based WebSocket testing interface
- Long-lived connection stability (> 60 seconds)

**Browser Testing:**
1. Open http://echoserver.localhost:1355
2. Enter message in text box
3. Click "Send"
4. Message appears in "Received" box

**Command-Line Testing:**
```bash
# Using websocat
websocat ws://echoserver.localhost:1355/ws

# Using wscat
wscat -c ws://echoserver.localhost:1355/ws
```

**Learn More:**
- [WebSocketEchoServer/README.md](WebSocketEchoServer/README.md)
- [Migration Guide v1.0 to v1.1](../docs/migration-v1.0-to-v1.1.md)

---

### SignalRChat Example (Real-Time Communication)

**Location:** `Examples/SignalRChat`

**Demonstrates:** Real-time communication with SignalR over WebSocket through Portless.NET

**Quick Start:**

```bash
# Terminal 1: Start proxy
portless proxy start

# Terminal 2: Run SignalR chat server
portless chatsignalr dotnet run --project Examples/SignalRChat

# Terminal 3: Open in browser
# Open http://chatsignalr.localhost:1355 in multiple tabs
# Enter username in each tab
# Send messages - they appear in all tabs
```

**What This Example Shows:**
- SignalR WebSocket connection through proxy
- Real-time bidirectional messaging
- Multi-client synchronization
- ASP.NET Core SignalR integration

**Features:**
- Real-time chat between multiple clients
- User join/leave notifications
- Message history
- Connection status indicator

**Browser Testing:**
1. Open http://chatsignalr.localhost:1355 in Tab A
2. Open http://chatsignalr.localhost:1355 in Tab B
3. Enter username in both tabs
4. Send message from Tab A
5. Message appears in both tabs in real-time

**Console Client:**
```bash
cd Examples/SignalRChat.Client
dotnet run -- http://chatsignalr.localhost:1355/chathub
```

**Learn More:**
- [SignalRChat/README.md](SignalRChat/README.md)
- [SignalR Troubleshooting Guide](../docs/signalr-troubleshooting.md)
- [Migration Guide v1.0 to v1.1](../docs/migration-v1.0-to-v1.1.md)

---

### HTTP/2 Integration Tests

**Location:** `Portless.Tests/Http2IntegrationTests.cs`

**Demonstrates:** HTTP/2 protocol support and automatic negotiation

**Quick Start:**

```bash
# Run the HTTP/2 integration tests
dotnet test Portless.Tests/Portless.Tests.csproj --filter "FullyQualifiedName~Http2IntegrationTests"
```

**What This Example Shows:**
- HTTP/2 protocol negotiation
- Automatic HTTP/2 support in Kestrel
- Protocol detection and logging
- X-Forwarded headers configuration

**Manual Testing:**
```bash
# Terminal 1: Start proxy
portless proxy start

# Terminal 2: Run any app
portless myapi dotnet run --project Examples/WebApi

# Terminal 3: Test HTTP/2
curl -I --http2-prior-knowledge http://webapi.localhost:1355

# Expected: HTTP/2 200
```

**Learn More:**
- [Migration Guide v1.0 to v1.1](../docs/migration-v1.0-to-v1.1.md#using-http2)

---

## v1.0 Examples

### WebApi Example

**Location:** `Examples/WebApi`

**Demonstrates:** Basic ASP.NET Core Web API with PORT integration

**Quick Start:**

```bash
portless proxy start
portless webapi dotnet run --project Examples/WebApi
curl http://webapi.localhost:1355/
```

**Expected Response:**
```json
{
  "message": "WebApi example is running with Portless!",
  "timestamp": "2026-02-21T10:30:00Z",
  "port": "4001"
}
```

---

### BlazorApp Example

**Location:** `Examples/BlazorApp`

**Demonstrates:** Blazor Web App with PORT integration

**Quick Start:**

```bash
portless proxy start
portless blazor dotnet run --project Examples/BlazorApp
# Open http://blazorapp.localhost:1355
```

---

### WorkerService Example

**Location:** `Examples/WorkerService`

**Demonstrates:** Background service with PORT integration

**Quick Start:**

```bash
portless proxy start
portless worker dotnet run --project Examples/WorkerService
```

**Expected Output:**
```
info: WorkerService.Worker[0]
      Worker running at: http://localhost:4003 (assigned by Portless)
```

**Note:** Worker services don't typically expose HTTP endpoints, but this example demonstrates how to access the PORT variable for logging or other purposes.

---

### ConsoleApp Example

**Location:** `Examples/ConsoleApp`

**Demonstrates:** Console application with PORT integration

**Quick Start:**

```bash
portless proxy start
portless myconsole dotnet run --project Examples/ConsoleApp
```

**Expected Output:**
```
Portless Console App Example
Running on port: 4004 (assigned by Portless)
URL: http://localhost:4004
```

## Running the Examples

### WebApi Example

Start the WebApi with Portless:
```bash
cd Examples/WebApi
portless webapi dotnet run
```

Access the API at: `http://webapi.localhost`

The API will respond with:
```json
{
  "message": "WebApi example is running with Portless!",
  "timestamp": "2026-02-21T10:30:00Z",
  "port": "4001"
}
```

### BlazorApp Example

Start the Blazor app with Portless:
```bash
cd Examples/BlazorApp
portless blazor dotnet run
```

Access the app at: `http://blazorapp.localhost`

### WorkerService Example

Start the worker service with Portless:
```bash
cd Examples/WorkerService
portless worker dotnet run
```

The worker will log its assigned port every 5 seconds:
```
info: WorkerService.Worker[0]
      Worker running at: http://localhost:4003 (assigned by Portless)
```

Note: Worker services don't typically expose HTTP endpoints, but this example demonstrates how to access the PORT variable for any purpose (logging, connecting to other services, etc.).

### ConsoleApp Example

Start the console app with Portless:
```bash
cd Examples/ConsoleApp
portless myconsole dotnet run
```

The console will display:
```
Portless Console App Example
Running on port: 4004 (assigned by Portless)
URL: http://localhost:4004
```

### SignalRChat Example (Real-Time Communication)

Start the SignalR chat server with Portless:
```bash
cd Examples/SignalRChat
portless chatsignalr dotnet run
```

Access the chat at: `http://chatsignalr.localhost:1355`

**Features:**
- Real-time bidirectional messaging using SignalR
- Browser-based client with modern UI
- WebSocket transport through Portless.NET proxy
- Automatic reconnection on connection loss
- Broadcast to all connected clients

**Testing:**
1. Open the URL in multiple browser windows
2. Send messages from any window
3. All clients receive the broadcast messages

**Console Client:**
```bash
cd Examples/SignalRChat.Client
dotnet run -- http://chatsignalr.localhost:1355/chathub
```

See [SignalRChat/README.md](SignalRChat/README.md) for detailed documentation.

### WebSocketEchoServer Example

Start the WebSocket echo server with Portless:
```bash
cd Examples/WebSocketEchoServer
portless echoserver dotnet run
```

Access the echo server at: `http://echoserver.localhost:1355`

**Features:**
- WebSocket echo server for testing proxy support
- Bidirectional messaging through Portless.NET
- Connection status indicator
- Message logging

**Testing:**
Open browser DevTools Console and run:
```javascript
const ws = new WebSocket('ws://echoserver.localhost:1355/ws');
ws.onmessage = (event) => console.log('Received:', event.data);
ws.send('Hello WebSocket!');
```

See [WebSocketEchoServer/README.md](WebSocketEchoServer/README.md) for detailed documentation.

## Running Multiple Examples

You can run multiple examples simultaneously - Portless will assign unique ports to each:

```bash
# Terminal 1
cd Examples/WebApi
portless webapi dotnet run

# Terminal 2
cd Examples/BlazorApp
portless blazor dotnet run

# Terminal 3
cd Examples/ConsoleApp
portless myconsole dotnet run

# Terminal 4
cd Examples/SignalRChat
portless chatsignalr dotnet run
```

Each example will receive a unique port in the 4000-4999 range and be accessible via its `.localhost` URL.

## Viewing Active Routes

List all active Portless routes:
```bash
portless list
```

Example output:
```
Hostname    Port  Process  PID
webapi      4001  dotnet   12345
blazorapp   4002  dotnet   12346
worker      4003  dotnet   12347
myconsole   4004  dotnet   12348
chatsignalr 4005  dotnet   12349
```

## Integration Pattern

All examples use the same PORT integration pattern:

### Program.cs (for ASP.NET Core projects)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Portless integration: Read PORT from environment
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}

var app = builder.Build();
// ... rest of application
```

**Important**: Call `builder.WebHost.UseUrls()` BEFORE `builder.Build()` to ensure Kestrel binds to the correct port.

### Worker.cs (for Background Services)

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    var port = Environment.GetEnvironmentVariable("PORT");
    while (!stoppingToken.IsCancellationRequested)
    {
        if (port != null)
        {
            _logger.LogInformation("Worker running at: http://localhost:{port} (assigned by Portless)", port);
        }
        await Task.Delay(5000, stoppingToken);
    }
}
```

### Program.cs (for Console Apps)

```csharp
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    Console.WriteLine($"Running on port: {port} (assigned by Portless)");
    Console.WriteLine($"URL: http://localhost:{port}");
}
```

### launchSettings.json

The "Portless" profile uses `http://localhost:0` to allow dynamic port assignment:

```json
{
  "profiles": {
    "Portless": {
      "commandName": "Project",
      "applicationUrl": "http://localhost:0",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

Using `localhost:0` tells ASP.NET Core to accept any port, allowing Portless to inject the PORT variable without conflicts.

## Alternative: appsettings.json Integration

You can also configure PORT binding via appsettings.json:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:${PORT}",
        "Protocols": "Http1AndHttp2"
      }
    }
  }
}
```

Then in Program.cs:
```csharp
builder.Configuration.AddEnvironmentVariables();
```

**Note**: The `${PORT}` syntax requires the configuration builder to support environment variable substitution. In .NET 10, this may require additional configuration.

## Stopping Examples

Press `Ctrl+C` in each terminal to stop the individual applications.

Stop the Portless proxy:
```bash
portless proxy stop
```

## Troubleshooting

### Issue: Application doesn't use the PORT assigned by Portless

**Solution**: Ensure `builder.WebHost.UseUrls()` is called BEFORE `builder.Build()` in Program.cs. The order matters in ASP.NET Core application initialization.

### Issue: Port is not accessible

**Solution**:
1. Check that Portless proxy is running: `portless proxy status`
2. Verify the route exists: `portless list`
3. Ensure your application actually started and is listening on the port

### Issue: Multiple apps on same port

**Solution**: Portless automatically assigns unique ports in the 4000-4999 range. If you see conflicts:
1. Each app must be started with a unique hostname: `portless hostname1 dotnet run`, `portless hostname2 dotnet run`
2. Check for orphaned processes with `portless list` and stop them if needed

### Issue: launchSettings.json not being used

**Solution**:
1. Ensure you're using the correct profile: `dotnet run --launch-profile "Portless"`
2. Or set `DOTNET_LAUNCH_PROFILE` environment variable: `export DOTNET_LAUNCH_PROFILE=Portless`

### Issue: Blazor app shows "Connection failed"

**Solution**: This is normal for the Blazor template's counter component. The main app should still work. If you see connection issues:
1. Check that the Blazor app is running on the PORT assigned by Portless
2. Verify the proxy is routing correctly to the blazorapp.localhost hostname

### Issue: SignalR chat shows "Disconnected - Reconnecting..."

**Solution**:
1. Verify Portless proxy is running: `portless proxy status`
2. Check that the SignalR chat server is running
3. Ensure the hostname is registered: `portless list`
4. Check browser DevTools Console for WebSocket connection errors
5. Verify the chat server is listening on the assigned PORT

### Issue: WebSocket connection fails

**Solution**:
1. Ensure Portless proxy is running with WebSocket support enabled (default)
2. Check browser DevTools Network tab for WebSocket connection (status 101)
3. Verify the WebSocket server is running on the assigned PORT
4. Check that your WebSocket client uses the correct URL (ws://hostname.localhost:1355/path)

### Issue: SignalR messages don't appear in browser

**Solution**:
1. Open browser DevTools Console (F12) and check for JavaScript errors
2. Verify the SignalR hub URL is correct (/chathub)
3. Check that multiple clients can receive broadcast messages
4. Try refreshing the browser page to re-establish the connection

### Issue: HTTP/2 not working

**Cause:** HTTP/2 over plain HTTP may silently downgrade to HTTP/1.1

**Solution:**
```bash
# Use HTTP/2 prior knowledge for testing
curl -I --http2-prior-knowledge http://webapi.localhost:1355
```

**Note:** This is expected behavior. Your apps still work with HTTP/1.1. HTTP/2 automatic negotiation works best with HTTPS (planned for v1.2).

See [Migration Guide: HTTP/2 Troubleshooting](../docs/migration-v1.0-to-v1.1.md#http2-is-not-working) for details.

### Issue: Need help with v1.1 features

**Solution:**
- Read the [Migration Guide v1.0 to v1.1](../docs/migration-v1.0-to-v1.1.md) for HTTP/2 and WebSocket features
- Check the [SignalR Troubleshooting Guide](../docs/signalr-troubleshooting.md) for SignalR-specific issues
- Review the example-specific README files:
  - [SignalRChat README](SignalRChat/README.md)
  - [WebSocketEchoServer README](WebSocketEchoServer/README.md)

## Integration with Your Projects

To integrate Portless into your own .NET projects:

1. Add PORT environment variable reading in your Program.cs (before `builder.Build()`)
2. Use `builder.WebHost.UseUrls($"http://*:{port}")` for web projects
3. Optionally create a "Portless" profile in launchSettings.json with `applicationUrl: "http://localhost:0"`
4. Run your app with: `portless yourhostname dotnet run`

Your app will be accessible at `http://yourhostname.localhost`

## Advanced Usage

### Custom Port Range

By default, Portless assigns ports in the 4000-4999 range. To configure a different range:

```bash
export PORTLESS_PORT_RANGE=5000-5999
portless proxy start
```

### HTTPS Support

To enable HTTPS for your applications:

```bash
export PORTLESS_HTTPS=1
portless proxy start
```

Then access your apps at `https://yourapp.localhost` (you may need to trust the self-signed certificate).

### State Directory

Portless stores routes and state in `~/.portless` (Unix) or `%APPDATA%\portless` (Windows). To customize:

```bash
export PORTLESS_STATE_DIR=/custom/path
portless proxy start
```

## Additional Resources

### Documentation

- [Main README](../README.md) - Portless.NET overview and getting started
- [Migration Guide v1.0 to v1.1](../docs/migration-v1.0-to-v1.1.md) - Upgrading to HTTP/2 and WebSocket support
- [SignalR Troubleshooting Guide](../docs/signalr-troubleshooting.md) - SignalR-specific issues and solutions

### External Resources

- [YARP Reverse Proxy Documentation](https://microsoft.github.io/reverse-proxy/)
- [ASP.NET Core Kestrel Configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel)
- [ASP.NET Core SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr/)
- [WebSocket API Documentation](https://developer.mozilla.org/en-US/docs/Web/API/WebSocket)

---

*Examples*
*Portless.NET v1.1*
*Updated: 2026-02-22*
