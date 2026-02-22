# Protocol Troubleshooting Guide

Troubleshooting HTTP/2 and WebSocket issues with Portless.NET.

## Table of Contents

1. [HTTP/2 Issues](#http2-issues)
2. [WebSocket Issues](#websocket-issues)
3. [Diagnostic Commands](#diagnostic-commands)
4. [Common Error Messages](#common-error-messages)

---

## HTTP/2 Issues

### Silent HTTP/2 Downgrade

**Symptom:**
- Request succeeds but uses HTTP/1.1 instead of HTTP/2
- No error message visible
- Protocol logging shows "HTTP/1.1" instead of "HTTP/2"

**Cause:**
HTTP/2 requires prior knowledge or HTTPS. Over plain HTTP, browsers and clients may silently downgrade to HTTP/1.1.

**Solutions:**

1. **Verify proxy supports HTTP/2:**
   ```bash
   # Check proxy logs for HTTP/2 support
   # Look for: "HTTP/2" in protocol logs
   ```

2. **Force HTTP/2 with curl:**
   ```bash
   curl -I --http2-prior-knowledge http://miapp.localhost:1355
   ```

3. **Use HTTPS (v1.2 milestone):**
   - HTTPS will enable true HTTP/2 negotiation
   - Planned for v1.2 Platform Expansion

**Verification:**
```bash
# Terminal 1: Check proxy logs
portless proxy logs
# Look for: "Using protocol: HTTP/2"

# Terminal 2: Test with prior knowledge
curl -I --http2-prior-knowledge http://miapp.localhost:1355
# Should see: "HTTP/2 200"
```

### HTTP/2 Negotiation Fails

**Symptom:**
- Connection fails or uses HTTP/1.1
- Client reports "HTTP/2 not supported"
- Error in application logs

**Cause:**
- Backend application doesn't support HTTP/2
- YARP configuration issue
- Protocol version mismatch

**Solutions:**

1. **Verify backend supports HTTP/2:**
   ```bash
   # Test backend directly
   curl -I --http2 http://localhost:4000  # Use actual backend port
   ```

2. **Check Kestrel configuration:**
   ```json
   {
     "Kestrel": {
       "Endpoints": {
         "Http": {
           "Url": "http://0.0.0.0:${PORT}",
           "Protocols": "Http1AndHttp2"  // Must include Http2
         }
       }
     }
   }
   ```

3. **Verify proxy configuration:**
   - Proxy should pass through HTTP/2 without modification
   - Check YARP logs for protocol translation

---

## WebSocket Issues

### WebSocket Connection Timeout

**Symptom:**
- WebSocket connects but disconnects after ~60 seconds
- Long-lived connections fail intermittently
- SignalR connection drops unexpectedly

**Cause:**
Default Kestrel timeouts are too short for long-lived WebSocket connections.

**Solutions:**

1. **Configure KeepAliveTimeout (recommended):**
   ```csharp
   // In Program.cs of Portless.Proxy
   builder.WebHost.ConfigureKestrel(options =>
   {
       options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
       options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
   });
   ```

2. **Implement WebSocket keep-alive:**
   ```csharp
   // In your WebSocket handler
   await webSocket.SendAsync(keepAliveMessage, cancellationToken);
   ```

**Verification:**
```bash
# Terminal 1: Start proxy
portless proxy start

# Terminal 2: Run echo server
portless echo dotnet run --project Examples/WebSocketEcho

# Terminal 3: Test long-lived connection
# Connect and keep connection open for > 60 seconds
# Should remain stable
```

### HTTP/1.1 vs HTTP/2 WebSocket

**Symptom:**
- WebSocket fails to connect
- "Unsupported protocol" error
- Connection upgrade fails

**Cause:**
Mismatch between client WebSocket protocol and server support.

**Solutions:**

1. **Verify backend supports both protocols:**
   ```csharp
   // In backend Program.cs
   builder.WebHost.ConfigureKestrel(options =>
   {
       options.Limits.Http2.MaxStreamsPerConnection = 100;
       options.ConfigureEndpointsDefaults(o =>
       {
           o.Protocols = HttpProtocols.Http1AndHttp2;
       });
   });
   ```

2. **Check browser DevTools:**
   - Open Network tab
   - Look for WebSocket connection
   - Check "Protocol" column (should be "websocket")

3. **Test with both HTTP/1.1 and HTTP/2:**
   ```bash
   # Force HTTP/1.1 WebSocket
   curl --http1.1 --include \
     -H "Connection: Upgrade" \
     -H "Upgrade: websocket" \
     -H "Sec-WebSocket-Version: 13" \
     -H "Sec-WebSocket-Key: test" \
     http://echo.localhost:1355/ws

   # Force HTTP/2 WebSocket (RFC 8441)
   curl --http2 --include \
     -H "Connection: Upgrade, HTTP2-Settings" \
     -H "Upgrade: h2c" \
     http://echo.localhost:1355/ws
   ```

### SignalR Connection Fails Through Proxy

**Symptom:**
- SignalR negotiation fails
- "Connection disconnected" error
- Transports fail to connect

**Cause:**
SignalR transport negotiation failing through proxy.

**Solutions:**

1. **Verify WebSocket transport works:**
   ```bash
   # Test WebSocket endpoint directly
   curl -I http://chat.localhost:1355/hubs/chat
   ```

2. **Check SignalR configuration:**
   ```csharp
   // In SignalR startup
   builder.Services.AddSignalR(options =>
   {
       options.EnableDetailedErrors = true;
       options.KeepAliveInterval = TimeSpan.FromSeconds(10);
   });
   ```

3. **Test all transports:**
   ```javascript
   // In browser console
   const connection = new signalR.HubConnectionBuilder()
       .withUrl("http://chat.localhost:1355/hubs/chat", {
           skipNegotiation: false,
           transport: signalR.HttpTransportType.WebSockets
       })
       .build();

   connection.start().catch(err => console.error(err));
   ```

**Verification:**
- Open browser DevTools → Console
- Check for SignalR connection errors
- Verify WebSocket connection in Network tab

---

## Diagnostic Commands

### Check Proxy Status

```bash
# List active routes and protocols
portless list

# Check proxy is running
portless proxy status
```

### Test HTTP/2

```bash
# Test with HTTP/2 prior knowledge
curl -I --http2-prior-knowledge http://miapp.localhost:1355

# Test with HTTP/2 negotiation
curl -I --http2 http://miapp.localhost:1355

# Verbose output (shows protocol)
curl -v --http2 http://miapp.localhost:1355
```

### Test WebSocket

```bash
# Using websocat (if installed)
websocat ws://echo.localhost:1355/ws

# Using Python
python -m websockets ws://echo.localhost:1355/ws

# Using wscat
wscat -c ws://echo.localhost:1355/ws
```

### Browser DevTools

1. **Open DevTools:** F12 or Ctrl+Shift+I
2. **Go to Network tab**
3. **Filter by "WS" for WebSockets**
4. **Check columns:**
   - **Name**: Endpoint path
   - **Status**: 101 Switching Protocols (success)
   - **Type**: websocket
   - **Protocol**: h2 (HTTP/2) or http/1.1

---

## Common Error Messages

### "Protocol downgrade detected"

**Meaning:** HTTP/2 was requested but HTTP/1.1 was used

**Solution:** See [Silent HTTP/2 Downgrade](#silent-http2-downgrade)

### "WebSocket connection failed"

**Meaning:** WebSocket upgrade or connection failed

**Solution:** See [WebSocket Connection Timeout](#websocket-connection-timeout)

### "Connection timed out"

**Meaning:** Request exceeded timeout threshold

**Solution:** Check backend is running, verify port is correct

### "Port already in use"

**Meaning:** Assigned port is occupied by another process

**Solution:**
```bash
# Windows
netstat -ano | findstr :PORT
taskkill /PID <pid> /F

# macOS/Linux
lsof -ti:PORT | xargs kill -9

# Then restart the app
portless run miapp dotnet run
```

---

## Still Having Issues?

1. **Check the logs:**
   ```bash
   portless proxy logs
   ```

2. **Verify examples work:**
   ```bash
   cd Examples
   # Try Http2Test, WebSocketEcho, SignalRChat
   ```

3. **Report an issue:**
   - Include: OS, .NET version, error messages
   - Include: Output of `portless list`
   - Include: Relevant logs

---

*Guide: Protocol Troubleshooting*
*Version: 1.1*
*Updated: 2026-02-22*
