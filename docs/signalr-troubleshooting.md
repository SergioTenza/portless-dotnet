# SignalR Troubleshooting Guide

This guide helps you diagnose and resolve common issues when using SignalR through Portless.NET proxy.

## Common Issues

### Issue: SignalR Falls Back to Server-Sent Events

**Symptom:** Messages are delayed, connection doesn't use WebSocket

**Diagnosis:**
```bash
# Check browser DevTools Network tab
# Look for "eventsource" instead of "websocket" connection type
```

**Cause:** SignalR client couldn't negotiate WebSocket transport

**Solutions:**

1. Verify WebSocket support is enabled in proxy (Phase 10)
2. Check that no firewall is blocking WebSocket upgrade
3. Configure SignalR client to prefer WebSocket:
   ```javascript
   const connection = new HubConnectionBuilder()
       .withUrl("/chathub", {
           skipNegotiation: false,
           transport: signalR.HttpTransportType.WebSockets
       })
       .build();
   ```

**Prevention:** Ensure proxy WebSocket support is working before adding SignalR

---

### Issue: Connection Drops After 60 Seconds

**Symptom:** SignalR connection disconnects after period of inactivity

**Diagnosis:**
```bash
# Check proxy logs for timeout messages
# Look for connection reset errors
```

**Cause:** Kestrel KeepAliveTimeout is too short (default 2 minutes)

**Solutions:**

1. Increase KeepAliveTimeout in `Program.cs`:
   ```csharp
   builder.WebHost.ConfigureKestrel(options =>
   {
       options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
   });
   ```

2. Implement keep-alive in SignalR client:
   ```javascript
   connection.serverTimeoutInMilliseconds = 300000; // 5 minutes
   connection.keepAliveIntervalInMilliseconds = 15000; // 15 seconds
   ```

**Prevention:** Configure timeouts for long-lived connections from the start

---

### Issue: "Connection not started" Error

**Symptom:** SignalR client throws error when trying to send messages

**Diagnosis:**
```bash
# Check browser console for "Connection not started" error
# Verify connection.State before invoking methods
```

**Cause:** Trying to invoke hub methods before connection is fully established

**Solutions:**

1. Always await connection start:
   ```javascript
   await connection.start();
   console.log("Connected:", connection.state);
   ```

2. Check connection state before invoking:
   ```javascript
   if (connection.state === signalR.HubConnectionState.Connected) {
       connection.invoke("SendMessage", user, message);
   }
   ```

**Prevention:** Use proper async/await pattern, check connection state

---

### Issue: Messages Not Received by Clients

**Symptom:** Server sends message but clients don't receive it

**Diagnosis:**
```bash
# Check server logs for SendAsync calls
# Check browser console for JavaScript errors
# Verify message handler is registered before connection start
```

**Cause:** Message handler not registered or registered after connection started

**Solutions:**

1. Register handlers before starting connection:
   ```javascript
   connection.on("ReceiveMessage", (user, message) => {
       console.log(`${user}: ${message}`);
   });
   await connection.start();
   ```

2. Use correct method name (case-sensitive):
   ```csharp
   // Server
   await Clients.All.SendAsync("ReceiveMessage", user, message);

   // Client
   connection.on("ReceiveMessage", (user, message) => { ... });
   ```

**Prevention:** Always register message handlers before starting connection

---

### Issue: Multiple Clients Don't Receive Broadcasts

**Symptom:** Only one client receives messages, others don't

**Diagnosis:**
```bash
# Verify all clients are connected to same hub URL
# Check server logs for SendAsync to "Clients.All"
# Test with simple browser + console client scenario
```

**Cause:** Clients connected to different hubs or using different hub URLs

**Solutions:**

1. Ensure all clients use same hub URL:
   ```javascript
   // All clients should connect to:
   http://mysignalr.localhost:1355/chathub
   ```

2. Verify server uses `Clients.All` for broadcast:
   ```csharp
   public async Task SendMessage(string user, string message)
   {
       await Clients.All.SendAsync("ReceiveMessage", user, message);
   }
   ```

**Prevention:** Document expected hub URL, test with multiple clients

---

## Diagnostic Commands

### Check Proxy is Running
```bash
portless proxy status
```

### List Registered Hosts
```bash
portless list
```

### Check SignalR Server Logs
```bash
# Server console output should show:
# - Connection started: GET /chathub?negotiateVersion=1
# - WebSocket connection established
# - Connection closed with no error
```

### Browser DevTools
```bash
# Network tab: Look for WebSocket connection (101 Switching Protocols)
# Console tab: Check for SignalR client errors
# Application tab: Check WebSocket frames for message flow
```

### Test with curl (for negotiation)
```bash
curl -v http://mysignalr.localhost:1355/chathub?negotiateVersion=1
# Should return 200 with connectionId and availableTransports
```

---

## Performance Considerations

### Connection Limits
- Kestrel default: 100 concurrent upgraded connections
- Increase if needed: `options.Limits.MaxConcurrentUpgradedConnections = 1000;`
- Monitor with: `portless proxy status` (when implemented)

### Message Frequency
- SignalR handles high-frequency messages well through WebSocket
- Consider message batching for very high frequency (100+ msg/sec)
- Monitor proxy logs for performance indicators

### Scale Considerations
- **Single server:** SignalR works great through Portless.NET proxy
- **Multiple servers:** Requires Redis backplane for SignalR (out of scope for v1.1)
- **Development:** Portless.NET proxy is sufficient for local development

---

## When to Ask for Help

If you've tried the solutions above and still have issues:

1. **Check the logs:**
   - Portless.NET proxy logs
   - SignalR server console output
   - Browser DevTools console

2. **Verify prerequisites:**
   - Portless.NET proxy is running
   - WebSocket support is enabled (Phase 10)
   - HTTP/2 support is enabled (Phase 9)

3. **Create a minimal reproduction:**
   - Use the SignalR chat example as baseline
   - Verify it works through proxy
   - Gradually add your custom code

4. **Report the issue:**
   - Include error messages
   - Share your proxy and server configuration
   - Mention what you've already tried

---

## Best Practices

### Connection Management

**DO:**
- Register message handlers before starting connection
- Always await `connection.start()` before invoking methods
- Check `connection.state` before sending messages
- Implement proper cleanup with `connection.stop()`
- Use try-catch around connection operations

**DON'T:**
- Invoke hub methods before connection is started
- Forget to stop connections (causes connection leaks)
- Assume connection stays connected forever (handle reconnection)

```javascript
// Good pattern
async function connectWithRetry() {
    let retryCount = 0;
    while (retryCount < 5) {
        try {
            await connection.start();
            console.log("Connected");
            return;
        } catch (err) {
            retryCount++;
            console.log(`Retry ${retryCount} in 5s...`);
            await new Promise(r => setTimeout(r, 5000));
        }
    }
}
```

### Error Handling

**Always handle connection errors:**
```javascript
connection.start().catch(err => {
    console.error("Connection failed:", err);
    // Implement retry logic or user notification
});

connection.onclose(err => {
    if (err) {
        console.error("Connection closed with error:", err);
        // Attempt reconnection
    } else {
        console.log("Connection closed");
    }
});
```

### Retry Logic

**Implement exponential backoff for reconnection:**
```javascript
async function reconnect() {
    let delay = 1000; // Start with 1 second
    while (true) {
        try {
            await connection.start();
            console.log("Reconnected");
            return;
        } catch (err) {
            delay *= 2; // Double delay each time
            if (delay > 60000) delay = 60000; // Max 1 minute
            console.log(`Reconnecting in ${delay/1000}s...`);
            await new Promise(r => setTimeout(r, delay));
        }
    }
}
```

### Testing Strategy

**Test in this order:**
1. Test SignalR server directly (without proxy)
2. Test through Portless.NET proxy
3. Test with multiple clients
4. Test connection recovery (stop/start server)
5. Test with your actual application logic

**Use the SignalR chat example as baseline:**
- If chat example works through proxy, your app should too
- If chat example doesn't work, proxy/site has issue (not your app)

### Development vs Production

**Development (Portless.NET):**
- Single server, no backplane needed
- Direct WebSocket connections through proxy
- Great for local development and testing

**Production (considerations):**
- Multiple servers require Redis backplane for SignalR
- Consider sticky sessions if using WebSocket
- Monitor connection count and message throughput
- Implement health checks for SignalR connectivity
- Consider Azure SignalR Service or AWS equivalent for scale

---

## Related Documentation

- [SignalR Chat Example](../Examples/SignalRChat/README.md)
- [WebSocket Support](./websocket-support.md)
- [HTTP/2 Support](./http2-support.md)
- [General Troubleshooting](./troubleshooting.md)
