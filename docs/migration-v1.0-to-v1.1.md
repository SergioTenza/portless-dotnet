# Migration Guide: v1.0 to v1.1

Guide for upgrading from Portless.NET v1.0 to v1.1.

## Overview

**v1.1 Advanced Protocols** adds HTTP/2 and WebSocket support while maintaining full backward compatibility with v1.0.

**Release Date:** 2026-02-22
**Milestone:** v1.1 Advanced Protocols

---

## What's New in v1.1

### HTTP/2 Support

- **Automatic HTTP/2 negotiation** when supported by client
- **Multiplexing** for improved performance with multiple concurrent requests
- **Header compression** (HPACK) for reduced bandwidth
- **Protocol detection** and logging for troubleshooting

**Best for:** Microservices, API gateways, apps with many small requests

### WebSocket Support

- **Transparent WebSocket proxying** for both HTTP/1.1 and HTTP/2
- **Long-lived connections** with configurable timeouts
- **SignalR integration** full support
- **RFC 6455** (HTTP/1.1 WebSocket) and **RFC 8441** (HTTP/2 WebSocket) support

**Best for:** Real-time apps, chat, notifications, live updates

### New Examples

- **HTTP/2 Integration Test** - Verify HTTP/2 negotiation
- **WebSocket Echo Server** - Test WebSocket connections
- **SignalR Chat** - Real-time chat example

### Improved Diagnostics

- **Protocol logging** - See which protocol (HTTP/1.1 or HTTP/2) is used
- **X-Forwarded headers** - Correctly preserve client information
- **Enhanced error messages** - Better troubleshooting guidance

---

## Breaking Changes

**None!** v1.1 is fully backward compatible with v1.0.

### What This Means

- All existing v1.0 commands work unchanged
- Existing apps continue to work without modification
- Configuration files remain compatible
- No code changes required in your applications

### Automatic Upgrades

When you upgrade to v1.1:
- HTTP/2 is automatically enabled (no configuration needed)
- WebSocket support is automatically available
- Protocol negotiation happens transparently

**Your existing apps automatically get:**
- HTTP/2 when clients support it
- WebSocket support if they use it
- Better performance without code changes

---

## New Features Guide

### Using HTTP/2

**No code changes required!** HTTP/2 works automatically when supported by the client.

**Verify HTTP/2 is working:**

```bash
# Terminal 1: Start proxy
portless proxy start

# Terminal 2: Run your app (as before)
portless myapi dotnet run

# Terminal 3: Test HTTP/2
curl -I --http2-prior-knowledge http://myapi.localhost:1355

# Expected: HTTP/2 200
```

**When HTTP/2 is used:**
- Modern browsers (Chrome, Firefox, Edge) automatically use HTTP/2
- `curl --http2` uses HTTP/2
- .NET HttpClient uses HTTP/2 when available

**When HTTP/1.1 is used:**
- Older clients that don't support HTTP/2
- Plain HTTP connections (silent downgrade - see [Protocol Troubleshooting Guide](#silent-http2-downgrade))

**Note:** Your apps work with both HTTP/1.1 and HTTP/2. No changes needed.

---

### Using WebSocket

**No proxy configuration required!** WebSocket connections work transparently.

**Your WebSocket apps work unchanged:**

```bash
# Terminal 1: Start proxy
portless proxy start

# Terminal 2: Run your WebSocket app (as before)
portless chat dotnet run --project MyChatApp

# Terminal 3: Connect (as before)
# Browser: http://chat.localhost:1355
# WebSocket connection works through proxy automatically
```

**SignalR apps work without changes:**

```csharp
// In your ASP.NET Core app
builder.Services.AddSignalR();

app.MapHub<ChatHub>("/hubs/chat");

// No configuration needed for proxy!
```

**Long-lived connections:**

If you have WebSocket connections that stay open for extended periods (> 60 seconds), they're now supported out of the box.

---

### Testing the New Features

**1. Try the HTTP/2 integration test:**

```bash
# Run the integration tests
dotnet test Portless.Tests/Portless.Tests.csproj --filter "FullyQualifiedName~Http2IntegrationTests"
```

**2. Try the WebSocket echo server:**

```bash
portless proxy start
portless echoserver dotnet run --project Examples/WebSocketEchoServer
# Open http://echoserver.localhost:1355 in browser
```

**3. Try the SignalR chat:**

```bash
portless proxy start
portless chatsignalr dotnet run --project Examples/SignalRChat
# Open http://chatsignalr.localhost:1355 in multiple browser tabs
```

See [Examples README](../Examples/README.md) for details.

---

## Configuration Changes

### No Configuration Required

v1.1 works with your existing v1.0 configuration without changes.

### Optional: Protocol Logging

If you want to see which protocol is used for each request, check the proxy logs:

```bash
portless proxy logs
```

You'll see log entries like:
```
[12:00:00] Request to myapi.localhost using HTTP/2
[12:00:01] Request to frontend.localhost using HTTP/1.1
```

### Optional: X-Forwarded Headers

The proxy now correctly sets X-Forwarded-* headers. If your app relies on these:

```csharp
// In your ASP.NET Core app
builder.Services.AddForwardedHeaders(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                               ForwardedHeaders.XForwardedProto |
                               ForwardedHeaders.XForwardedHost;
});
```

This is optional - most apps don't need it.

---

## CLI Changes

### New Command Options

**`portless proxy status`** now shows protocol information:

```bash
$ portless proxy status
Proxy Status: Running
Protocols: HTTP/2, WebSocket, HTTP/1.1
Active routes: 2
```

**`portless proxy status --protocol`** shows detailed protocol info:

```bash
$ portless proxy status --protocol
Proxy Status: Running

Protocol Support:
  HTTP/2: Enabled
  WebSocket: Supported
  HTTP/1.1: Supported
```

### Unchanged Commands

All existing commands work as before:
- `portless proxy start` - No changes
- `portless proxy stop` - No changes
- `portless run <name> <command>` - No changes
- `portless list` - No changes (just shows protocol support note)

---

## Performance Improvements

### HTTP/2 Benefits

If your clients use HTTP/2, you get:
- **Multiplexing**: Multiple requests over one connection
- **Header compression**: Reduced bandwidth overhead
- **Better performance**: Especially for many small requests

**No code changes required** - clients that support HTTP/2 automatically benefit.

### Measuring the Improvement

```bash
# Test with HTTP/1.1
time curl -I --http1.1 http://myapi.localhost:1355

# Test with HTTP/2
time curl -I --http2 http://myapi.localhost:1355

# For many small requests, HTTP/2 should be faster
```

---

## Troubleshooting

### "HTTP/2 is not working"

**Issue:** Requests use HTTP/1.1 instead of HTTP/2

**Cause:** HTTP/2 requires HTTPS or prior knowledge. Over plain HTTP, clients may silently downgrade to HTTP/1.1.

**Solution:**
```bash
# Use HTTP/2 prior knowledge for testing
curl -I --http2-prior-knowledge http://myapi.localhost:1355

# Or wait for v1.2 (HTTPS support)
```

**Note:** This is expected behavior for HTTP/2 over plain HTTP. Your apps still work - they just use HTTP/1.1.

### "WebSocket connection drops"

**Issue:** WebSocket connections disconnect after ~60 seconds

**Cause:** Default timeout settings (shouldn't happen in v1.1)

**Solution:**
```bash
# Check you're running v1.1
portless --version

# If still having issues, check the proxy logs
portless proxy logs
```

**Note:** v1.1 configures Kestrel with extended timeouts (10 minutes) for long-lived WebSocket connections.

---

## Rollback Plan

If you need to rollback to v1.0:

```bash
# Uninstall v1.1
dotnet tool uninstall -g portless.dotnet

# Install v1.0
dotnet tool install -g portless.dotnet --version 1.0.0
```

**Note:** Your apps and configuration will work with v1.0 (just without HTTP/2 and WebSocket support).

---

## Summary

**Upgrade difficulty:** Easy
**Breaking changes:** None
**Required actions:** None (just upgrade)
**Recommended actions:** Try the new examples, verify HTTP/2 works

**Next steps:**
1. Upgrade to v1.1
2. Try the [WebSocket Echo Server example](../Examples/README.md#websocketechoserver-example)
3. Try the [SignalR Chat example](../Examples/README.md#signalrchat-example-real-time-communication)
4. Run the [HTTP/2 integration tests](../Portless.Tests/Http2IntegrationTests.cs)
5. Read the [SignalR Troubleshooting Guide](signalr-troubleshooting.md)

---

## Need Help?

- [SignalR Troubleshooting Guide](signalr-troubleshooting.md)
- [Examples README](../Examples/README.md)
- [Main README](../README.md)

---

*Migration Guide*
*Version: 1.0 -> 1.1*
*Updated: 2026-02-22*
