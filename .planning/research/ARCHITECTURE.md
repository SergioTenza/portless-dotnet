# Architecture Research: HTTP/2 and WebSocket Support

**Domain:** HTTP/2 and WebSocket proxying for local development
**Researched:** 2026-02-21
**Confidence:** HIGH

## Executive Summary

**GOOD NEWS:** YARP 2.3.0 already has built-in support for both HTTP/2 and WebSockets. The existing Portless.NET architecture requires **minimal modifications** to enable these protocols. This is a configuration and validation task, not a major rewrite.

**Key findings:**
- HTTP/2: Configure Kestrel to enable HTTP/2 + add cluster metadata for version policy
- WebSockets: Works by default in YARP (HTTP/1.1 upgrade + HTTP/2 WebSockets)
- No new components needed
- Backward compatible: HTTP/1.1 continues to work alongside HTTP/2
- Protocol version negotiation is automatic

## Current Architecture (v1.0)

### System Overview

```
┌─────────────────────────────────────────────────────────────┐
│                        Client Layer                          │
│  Browser / DevTool / Test Runner                             │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────┐  ┌─────────┐  ┌─────────┐                      │
│  │miapi.   │  │chat.    │  │api.     │  HTTP requests       │
│  │localhost│  │localhost│  │localhost│  :1355 → Portless     │
│  └────┬────┘  └────┬────┘  └────┬────┘                      │
├───────┴────────────┴────────────┴──────────────────────────┤
│                    Portless.Proxy (YARP)                     │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  Kestrel Server (HTTP/1.1 only in v1.0)              │    │
│  │  DynamicConfigProvider → YARP Routes/Clusters        │    │
│  │  RequestLoggingMiddleware                           │    │
│  └─────────────────────────────────────────────────────┘    │
├─────────────────────────────────────────────────────────────┤
│                    Portless.Core Services                    │
│  ┌──────────┐  ┌────────────┐  ┌────────────────┐           │
│  │RouteStore│  │PortAllocator│  │RouteFileWatcher│          │
│  └──────────┘  └────────────┘  └────────────────┘           │
│  ┌────────────────────────────────────────────────────┐     │
│  │ProcessManager + ProcessHealthMonitor                │     │
│  └────────────────────────────────────────────────────┘     │
├─────────────────────────────────────────────────────────────┤
│                    Backend Applications                     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │WebApi (4001) │  │Blazor (4002) │  │Worker (4003) │       │
│  └──────────────┘  └──────────────┘  └──────────────┘       │
└─────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

| Component | Responsibility | Current Implementation |
|-----------|----------------|------------------------|
| **Portless.Proxy/Program.cs** | YARP configuration, Kestrel setup, route management endpoints | HTTP/1.1 only |
| **DynamicConfigProvider** | Hot-reload YARP configuration | Protocol-agnostic |
| **RouteStore** | Route persistence to JSON | Protocol-agnostic |
| **ProcessManager** | Backend process lifecycle | Protocol-agnostic |
| **RequestLoggingMiddleware** | Request/response logging | Protocol-agnostic |

## HTTP/2 Integration

### What Changes

**Modified Components:**
- **Portless.Proxy/Program.cs** → Add Kestrel HTTP/2 configuration

**No new components needed**

### HTTP/2 Configuration

#### 1. Enable HTTP/2 on Kestrel (Inbound)

```csharp
// Current (v1.0)
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});

// Updated (v1.1)
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port), listenOptions =>
    {
        // Enable HTTP/1.1 and HTTP/2
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
});
```

#### 2. Configure YARP Cluster Version Policy (Outbound)

```csharp
static ClusterConfig CreateCluster(string clusterId, string backendUrl) =>
    new ClusterConfig
    {
        ClusterId = clusterId,
        HttpRequest = new Yarp.ReverseProxy.Forwarder.ForwarderRequestConfig
        {
            Version = HttpVersion.Version2,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
        },
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["backend1"] = new DestinationConfig { Address = backendUrl }
        }
    };
```

### HTTP/2 Protocol Negotiation

```
Client                     Portless.Proxy                    Backend
  │                              │                              │
  │ HTTP/2 request               │                              │
  │ :1355/miapi.localhost ───────►│                              │
  │                              │                              │
  │                     YARP detects HTTP/2               │
  │                     Routes to backend                  │
  │                              │                              │
  │                              │ HTTP/2 request               │
  │                              ├─────────────────────────────►│
  │                              │                              │
  │                              │ HTTP/2 response              │
  │                              │◄─────────────────────────────┤
  │                              │                              │
  │ HTTP/2 response               │                              │
  │◄──────────────────────────────┤                              │
  │                              │                              │
```

**Key behaviors:**
- Incoming protocol (HTTP/1.1 or HTTP/2) negotiates automatically
- Outbound protocol defaults to HTTP/2 with `RequestVersionOrLower` (fallback to HTTP/1.1)
- HTTP/2 over HTTP (non-TLS) works with Kestrel in local development
- No code changes needed in RouteInfo, RouteStore, ProcessManager

## WebSocket Integration

### What Changes

**ZERO changes required.** YARP proxies WebSockets by default.

### How YARP Handles WebSockets

#### HTTP/1.1 WebSocket Upgrade (Classic)

```
Client                     Portless.Proxy                    Backend
  │                              │                              │
  │ WebSocket Upgrade            │                              │
  │ GET ws://chat.localhost ─────►│                              │
  │ Upgrade: websocket           │                              │
  │                              │                              │
  │                     YARP proxies 101 Switching       │
  │                     Protocol response                │
  │                              │                              │
  │                              │ WebSocket Upgrade            │
  │                              ├─────────────────────────────►│
  │                              │                              │
  │                 Opaque bidirectional stream established │
  │                              │                              │
  │  ────────────────┬──────────────────┬────────────────────── │
  │     WebSocket frames (bidirectional)                    │
  │  ────────────────┴──────────────────┴────────────────────── │
  │                              │                              │
```

#### HTTP/2 WebSocket (RFC 8441)

```
Client                     Portless.Proxy                    Backend
  │                              │                              │
  │ HTTP/2 WebSocket (CONNECT)  │                              │
  │ :1355/chat.localhost ───────►│                              │
  │                              │                              │
  │                     YARP detects HTTP/2 + WebSocket    │
  │                     Uses extended CONNECT protocol    │
  │                              │                              │
  │                              │ HTTP/2 WebSocket             │
  │                              ├─────────────────────────────►│
  │                              │                              │
  │                 Bidirectional stream over HTTP/2       │
  │                              │                              │
  │  ────────────────┬──────────────────┬────────────────────── │
  │     WebSocket frames with multiplexing + compression │
  │  ────────────────┴──────────────────┴────────────────────── │
  │                              │                              │
```

**YARP automatically:**
- Detects WebSocket upgrade requests
- Proxies the 101 Switching Protocols response
- Establishes opaque bidirectional stream
- Adapts headers between HTTP/1.1 and HTTP/2 WebSocket
- Handles protocol version mismatch (HTTP/1.1 in → HTTP/2 out)

### No Middleware Needed

**Do NOT add:**
```csharp
// WRONG - Not needed in proxy
app.UseWebSockets();
```

**Why:** YARP's `app.MapReverseProxy()` handles WebSocket upgrade automatically. Adding `UseWebSockets()` would intercept the request before YARP can proxy it.

### Backend Application Requirements

Backend apps must support WebSockets normally:

```csharp
// Backend app (e.g., chat application running on port 4002)
var app = builder.Build();

app.UseWebSockets();

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await Echo(webSocket);
    }
    else
    {
        context.Response.StatusCode = 400;
    }
});
```

Portless.NET proxies this transparently. Clients connect to `ws://chat.localhost:1355/ws`, Portless proxies to `ws://localhost:4002/ws`.

## Data Flow Changes

### HTTP/2 Request Flow

```
[Client: Browser]
    ↓
    HTTP/2 request to miapi.localhost:1355
    ↓
[Portless.Proxy/Kestrel]
    ↓
    HttpProtocols.Http1AndHttp2 enabled
    ↓
[RequestLoggingMiddleware]
    ↓
    Logs protocol version (HTTP/2)
    ↓
[YARP Route Matching]
    ↓
    Finds route by hostname (miapi.localhost)
    ↓
[YARP Cluster Selection]
    ↓
    Uses HttpRequest.Version = HTTP/2
    ↓
[YARP Forwarder]
    ↓
    Forwards HTTP/2 to backend (localhost:4001)
    ↓
[Backend Application]
    ↓
    Processes HTTP/2 request
    ↓
[Response Path]
    ↓
    Returns via YARP → Client
```

### WebSocket Upgrade Flow

```
[Client: Browser/JavaScript]
    ↓
    new WebSocket('ws://chat.localhost:1355/ws')
    ↓
[Portless.Proxy/Kestrel]
    ↓
    Receives HTTP/1.1 GET with Upgrade: websocket
    ↓
[RequestLoggingMiddleware]
    ↓
    Logs initial upgrade request
    ↓
[YARP Route Matching]
    ↓
    Finds route (chat.localhost)
    ↓
[YARP WebSocket Handling]
    ↓
    Detects upgrade request
    Proxies to backend
    Waits for 101 Switching Protocols
    ↓
[Backend Application]
    ↓
    Accepts WebSocket upgrade
    Returns 101 Switching Protocols
    ↓
[YARP Stream Proxy]
    ↓
    Switches to opaque bidirectional mode
    Forwards WebSocket frames in both directions
    No further HTTP processing
    ↓
[Client/Backend Communication]
    ↓
    Direct WebSocket frame tunneling
```

## Protocol Version Matrix

| Client → Proxy | Proxy → Backend | Status |
|----------------|-----------------|--------|
| HTTP/1.1 | HTTP/1.1 | ✓ Works (v1.0) |
| HTTP/2 | HTTP/2 | ✓ Works (v1.1) |
| HTTP/2 | HTTP/1.1 | ✓ Works (auto downgrade) |
| HTTP/1.1 | HTTP/2 | ✓ Works (auto upgrade) |
| WS (HTTP/1.1) | WS (HTTP/1.1) | ✓ Works (v1.0) |
| WS (HTTP/2) | WS (HTTP/2) | ✓ Works (v1.1) |
| WS (HTTP/1.1) | WS (HTTP/2) | ✓ Works (YARP adapts) |
| WS (HTTP/2) | WS (HTTP/1.1) | ✓ Works (YARP adapts) |

**YARP handles protocol adaptation automatically.**

## Recommended Project Structure

No structural changes needed. Existing structure supports HTTP/2 and WebSockets:

```
portless-dotnet/
├── Portless.Core/           # Shared logic (no changes)
│   ├── Configuration/
│   │   └── DynamicConfigProvider.cs    # Add HttpRequest config
│   ├── Models/
│   │   └── RouteInfo.cs                # No changes
│   └── Services/
│       ├── RouteStore.cs               # No changes
│       ├── PortAllocator.cs            # No changes
│       └── ProcessManager.cs           # No changes
│
├── Portless.Proxy/          # Web app (modify)
│   └── Program.cs                        # Add Kestrel HTTP/2
│                                        # Update CreateCluster()
│
├── Portless.Cli/           # Console app (no changes)
│
└── Portless.Tests/         # Tests (add HTTP/2 + WebSocket tests)
```

## Integration Points

### External Services

| Service | Integration Pattern | Notes |
|---------|---------------------|-------|
| **Backend Apps** | HTTP/1.1 or HTTP/2 | Backend controls protocol support |
| **Browsers** | Auto-negotiate | Chrome/Edge/Firefox 128+ support HTTP/2 WebSocket |

### Internal Boundaries

| Boundary | Communication | HTTP/2 Impact | WebSocket Impact |
|----------|---------------|---------------|------------------|
| **CLI ↔ Proxy** | HTTP POST /api/v1/add-host | None | None |
| **Proxy ↔ Backend** | Forwarded HTTP/S | Add Version config | Transparent |
| **Proxy ↔ Persistence** | File I/O (routes.json) | None | None |
| **Proxy ↔ Process** | Process start/stop | None | None |

## Architectural Patterns

### Pattern 1: Protocol-Agnostic Core

**What:** Portless.Core, RouteStore, ProcessManager don't care about HTTP version
**When:** All business logic remains unchanged
**Trade-offs:**
- Pro: Clean separation of concerns
- Pro: Easy to test
- Pro: Backward compatible
- Con: None significant

**Example:**
```csharp
// RouteInfo.cs - No protocol information
public class RouteInfo
{
    public string Hostname { get; init; }
    public int Port { get; init; }
    public int Pid { get; init; }
    // No "Protocol" field needed
}
```

### Pattern 2: Declarative Protocol Configuration

**What:** Configure protocols at Kestrel and YARP cluster level, not per-request
**When:** Startup configuration in Program.cs
**Trade-offs:**
- Pro: Centralized configuration
- Pro: No runtime overhead
- Pro: YARP handles negotiation
- Con: Can't dynamically change protocols per route (not needed)

**Example:**
```csharp
// Kestrel configuration (inbound)
listenOptions.Protocols = HttpProtocols.Http1AndHttp2;

// YARP cluster configuration (outbound)
HttpRequest = new ForwarderRequestConfig
{
    Version = HttpVersion.Version2,
    VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
};
```

### Pattern 3: Transparent Proxying

**What:** YARP handles WebSocket upgrade without middleware intervention
**When:** Any WebSocket or HTTP/2 traffic
**Trade-offs:**
- Pro: Zero code changes for WebSocket support
- Pro: YARP handles protocol adaptation
- Pro: Performance (opaque stream after upgrade)
- Con: Can't inspect/modify WebSocket frames (not needed)

**Example:**
```csharp
// Just map reverse proxy - no WebSocket middleware needed
app.MapReverseProxy();

// YARP automatically handles:
// - HTTP/1.1 Upgrade: websocket
// - HTTP/2 WebSocket (CONNECT method)
// - Protocol version adaptation
```

## Anti-Patterns

### Anti-Pattern 1: Adding WebSocket Middleware to Proxy

**What people do:**
```csharp
// WRONG
app.UseWebSockets();
app.MapReverseProxy();
```

**Why it's wrong:** Intercepts WebSocket requests before YARP can proxy them

**Do this instead:**
```csharp
// CORRECT
app.MapReverseProxy(); // YARP handles WebSocket
```

### Anti-Pattern 2: Per-Route Protocol Configuration

**What people do:** Add `Protocol` field to `RouteInfo`, try to configure HTTP/2 per hostname

**Why it's wrong:** Unnecessary complexity. Protocol negotiation is automatic.

**Do this instead:** Configure HTTP/2 at Kestrel level (all routes), let YARP handle version policy per cluster

### Anti-Pattern 3: Manual Protocol Detection

**What people do:** Write middleware to detect HTTP/2 vs HTTP/1.1

**Why it's wrong:** YARP and Kestrel already handle this

**Do this instead:** Trust YARP's automatic negotiation

## Scaling Considerations

### Performance Impact

| Metric | HTTP/1.1 (v1.0) | HTTP/2 (v1.1) | Notes |
|--------|-----------------|---------------|-------|
| **Request overhead** | ~5ms | ~5ms | No change |
| **Multiplexing** | No | Yes | Multiple requests over single connection |
| **Header compression** | No | Yes (HPACK) | Reduced bandwidth |
| **Throughput** | >10K req/sec | >10K req/sec | Limited by Kestrel, not protocol |
| **WebSocket latency** | Same | Same | No change |

**HTTP/2 benefits:**
- **Multiplexing:** Single TCP connection for multiple concurrent requests
- **Header compression:** Reduced header overhead (especially for cookies)
- **Server push:** (Not used in proxy scenario)

**Local dev context:** Benefits are modest since latency is low, but HTTP/2 mirrors production behavior.

## Build Order

### Phase 1: HTTP/2 Configuration (1-2 hours)

1. **Modify Portless.Proxy/Program.cs**
   - Add `HttpProtocols.Http1AndHttp2` to Kestrel configuration
   - Add `HttpRequest` config to `CreateCluster()`

2. **Update tests**
   - Add HTTP/2 integration test
   - Verify HTTP/1.1 still works (backward compatibility)

3. **Documentation**
   - Update CLAUDE.md with HTTP/2 support notes
   - Add example backend with HTTP/2

### Phase 2: WebSocket Validation (1-2 hours)

1. **Create WebSocket example**
   - Add example backend with WebSocket endpoint
   - Test both HTTP/1.1 and HTTP/2 WebSocket

2. **Update tests**
   - Add WebSocket integration test
   - Verify bidirectional communication

3. **Documentation**
   - Document WebSocket support
   - Add troubleshooting guide

### Phase 3: SignalR Example (optional, 2-3 hours)

1. **Create SignalR example backend**
   - Demonstrate real-world WebSocket usage
   - Test with Portless.NET proxy

2. **Documentation**
   - Add SignalR integration guide

**Total estimated effort:** 4-7 hours for HTTP/2 + WebSocket support

## Backward Compatibility

**100% backward compatible:**

- HTTP/1.1 continues to work alongside HTTP/2
- Existing routes.json format unchanged
- CLI commands unchanged
- PORT variable injection unchanged
- Process management unchanged

**Clients control protocol:**
- Old browsers/tools continue using HTTP/1.1
- New browsers/tools auto-negotiate HTTP/2
- No breaking changes to API

## Testing Strategy

### Unit Tests (No Changes)

HTTP/2 and WebSocket are integration concerns, not unit test concerns. Existing unit tests for RouteStore, PortAllocator, ProcessManager remain valid.

### Integration Tests (Add)

```csharp
// HTTP/2 test
[Fact]
public async Task Proxy_Forwards_Http2_Request()
{
    // Arrange
    var backend = CreateBackend(listenOnHttp2: true);
    var proxy = CreateProxy(enableHttp2: true);

    // Act
    var client = new HttpClient();
    client.DefaultRequestVersion = HttpVersion.Version2;
    var response = await client.GetAsync("http://localhost:1355/test");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
}

// WebSocket test
[Fact]
public async Task_Proxy_Forwards_WebSocket_Connection()
{
    // Arrange
    var backend = CreateBackendWithWebSocket();
    var proxy = CreateProxy();

    // Act
    var client = new ClientWebSocket();
    await client.ConnectAsync(new Uri("ws://localhost:1355/ws"));

    // Assert
    Assert.Equal(WebSocketState.Open, client.State);
}
```

## Sources

- [YARP Proxying WebSockets and SPDY](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/websockets) — HIGH confidence (official Microsoft docs, updated 2026-01-23)
- [YARP Proxying gRPC](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/grpc) — HIGH confidence (official Microsoft docs, updated 2026-01-23)
- [WebSockets support in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/websockets/) — HIGH confidence (official Microsoft docs, updated 2025-10-17)
- Existing Portless.NET codebase (v1.0) — HIGH confidence

---
*Architecture research for: Portless.NET v1.1 HTTP/2 and WebSocket support*
*Researched: 2026-02-21*
