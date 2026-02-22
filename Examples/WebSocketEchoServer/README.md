# WebSocket Echo Server Example

A simple WebSocket echo server that demonstrates bidirectional WebSocket communication through Portless.NET.

## Purpose

This example demonstrates:
- WebSocket connection acceptance and handling
- Bidirectional messaging (echo server)
- Connection lifecycle management
- Both HTTP/1.1 and HTTP/2 WebSocket support

## Running with Portless.NET

### Step 1: Start the Portless.NET proxy

```bash
portless proxy start
```

### Step 2: Start the echo server with Portless

```bash
portless wsecho dotnet run --project Examples/WebSocketEchoServer/WebSocketEchoServer.csproj
```

This will:
1. Assign an available port (e.g., 4001)
2. Set the `PORT` environment variable
3. Start the echo server on that port
4. Register the route `wsecho.localhost` -> `localhost:4001`

### Step 3: Test the WebSocket connection

#### Using a WebSocket client (JavaScript)

```javascript
const ws = new WebSocket('ws://wsecho.localhost/ws');

ws.onopen = () => {
    console.log('Connected to echo server');
    ws.send('Hello, WebSocket!');
};

ws.onmessage = (event) => {
    console.log('Received:', event.data);
};

ws.onclose = () => {
    console.log('Connection closed');
};
```

#### Using websocat (command-line tool)

```bash
# Install websocat: cargo install websocat
echo "Hello via WebSocket" | websocat ws://wsecho.localhost/ws
```

#### Using Python

```python
import asyncio
import websockets

async def test_echo():
    uri = "ws://wsecho.localhost/ws"
    async with websockets.connect(uri) as websocket:
        await websocket.send("Hello from Python!")
        response = await websocket.recv()
        print(f"Received: {response}")

asyncio.run(test_echo())
```

## Features

- **Echo functionality**: All received messages are echoed back
- **Connection logging**: Logs connection establishment, messages, and closure
- **Error handling**: Gracefully handles WebSocket errors and disconnections
- **Protocol support**: Works with both HTTP/1.1 and HTTP/2 WebSocket connections

## Testing Connection Stability

To test long-lived connections (beyond 60 seconds):

```javascript
const ws = new WebSocket('ws://wsecho.localhost/ws');
let messageCount = 0;

const interval = setInterval(() => {
    if (ws.readyState === WebSocket.OPEN) {
        ws.send(`Ping ${++messageCount}`);
    }
}, 10000); // Send message every 10 seconds

ws.onmessage = (event) => {
    console.log('Echo:', event.data);
};

// Stop after 2 minutes
setTimeout(() => {
    clearInterval(interval);
    ws.close();
}, 120000);
```

## Health Check

The server provides a health check endpoint:

```bash
curl http://wsecho.localhost/health
```

Response:
```json
{
  "status": "healthy",
  "timestamp": "2026-02-22T12:00:00Z",
  "websocketEndpoint": "/ws"
}
```

## Troubleshooting

### Connection refused

- Ensure Portless.NET proxy is running (`portless proxy start`)
- Check that the route is registered (`portless list`)

### Connection timeout

- Verify the proxy is configured for long-lived WebSocket connections
- Check firewall settings
- Ensure the backend server is running

### Messages not being echoed

- Check browser console for WebSocket errors
- Verify the WebSocket URL is correct (ws://wsecho.localhost/ws)
- Check server logs for connection errors

## HTTP/2 WebSocket Testing

To test HTTP/2 WebSocket (RFC 8441 Extended CONNECT), use a client that supports HTTP/2:

```bash
# Using curl with HTTP/2 prior knowledge
curl --http2-prior-knowledge -H "Upgrade: websocket" \
  -H "Connection: Upgrade" \
  -H "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==" \
  -H "Sec-WebSocket-Version: 13" \
  http://wsecho.localhost/ws
```

Note: HTTP/2 WebSocket requires prior knowledge without HTTPS (h2c).

## Portless.NET Integration

This example is designed to work seamlessly with Portless.NET:

- Uses `${PORT}` environment variable for dynamic port assignment
- Listens on `0.0.0.0` for compatibility
- Supports both HTTP/1.1 and HTTP/2 protocols
- Provides health check for monitoring

For more information, see the main [Portless.NET README](../../README.md).
