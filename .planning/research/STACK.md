# Stack Research: HTTP/2 and WebSockets for Portless.NET

**Domain:** Reverse Proxy with Advanced Protocol Support
**Researched:** 2026-02-22
**Confidence:** HIGH

## Executive Summary

**No new packages required.** YARP 2.3.0 already includes full HTTP/2 and WebSocket support. The existing stack (YARP 2.3.0 + .NET 10) is sufficient. Only configuration changes are needed.

## Recommended Stack

### Core Technologies (No Changes Required)

| Technology | Current Version | Purpose | Why HTTP/2/WebSockets Work |
|------------|----------------|---------|---------------------------|
| **YARP** | 2.3.0 | Reverse proxy engine | **Native support** for HTTP/2 WebSockets since .NET 7/YARP 2.0. Automatically handles protocol negotiation, header adaptation, and connection upgrades. No additional libraries needed. |
| **.NET 10** | net10.0 | Runtime platform | **Built-in HTTP/2 support** in Kestrel with `HttpProtocols.Http1AndHttp2` enum. Supports HTTP/2 Prior Knowledge (non-TLS) and ALPN negotiation (TLS). |
| **Kestrel** | (included in .NET 10) | Web server | **Only ASP.NET Core server** that accepts HTTP/2 WebSocket requests. Automatic enablement - browsers detect advertised support and switch to HTTP/2 automatically. |
| **Spectre.Console.Cli** | 0.53.1 | CLI framework | No changes needed - protocol support is proxy-layer only, CLI layer unchanged. |
| **xUnit** | 2.9.3 | Testing framework | No changes needed for protocol testing. |

### Supporting Libraries

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| **None required** | - | - | YARP includes all necessary functionality for HTTP/2 and WebSocket proxying. No additional dependencies needed. |

### Development Tools

| Tool | Purpose | Notes |
|------|---------|-------|
| **curl with --http2** | Test HTTP/2 connections | Verify HTTP/2 prior knowledge mode: `curl -I --http2-prior-knowledge http://localhost:1355` |
| **Browser DevTools** | Protocol inspection | Chrome/Edge/Firefox (128+) show "h2" in Network tab for HTTP/2 connections |
| **wscat** | WebSocket testing | Test WebSocket connections: `wscat -c ws://hostname.localhost:1355` |
| **netsh/openssl** | TLS verification | For future HTTPS milestone (out of scope for v1.1) |

## Installation

### No New Packages Required

```bash
# Current packages remain unchanged
# Portless.Proxy already has:
dotnet add package Yarp.ReverseProxy --version 2.3.0

# Portless.Core already has:
dotnet add package Yarp.ReverseProxy --version 2.3.0
dotnet add package Microsoft.Extensions.Hosting.Abstractions --version 9.0.0
dotnet add package Microsoft.Extensions.Logging.Abstractions --version 9.0.0

# No additional packages needed for HTTP/2 or WebSockets
```

## Configuration Changes Required

### 1. Kestrel Configuration (HTTP/2 Support)

**Current `Portless.Proxy/Program.cs` (lines 41-44):**
```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
});
```

**Required Change for HTTP/2:**
```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port), listenOptions =>
    {
        // Option A: Support both HTTP/1.1 and HTTP/2 (requires TLS for ALPN)
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;

        // Option B: HTTP/2 only with Prior Knowledge (no TLS, development only)
        // listenOptions.Protocols = HttpProtocols.Http2;
    });
});
```

**Protocol Selection Decision Tree:**
- **Use `Http1AndHttp2` with TLS:** Production, automatic fallback, browser-compatible
- **Use `Http2` without TLS:** Development-only, requires HTTP/2 Prior Knowledge clients
- **Default (no config):** HTTP/1.1 only (current v1.0 behavior)

### 2. YARP Cluster Configuration (Outbound HTTP/2)

**Current cluster creation (line 23-31):**
```csharp
static ClusterConfig CreateCluster(string clusterId, string backendUrl) =>
    new ClusterConfig
    {
        ClusterId = clusterId,
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["backend1"] = new DestinationConfig { Address = backendUrl }
        }
    };
```

**Recommended Enhancement for HTTP/2 backends:**
```csharp
static ClusterConfig CreateCluster(string clusterId, string backendUrl) =>
    new ClusterConfig
    {
        ClusterId = clusterId,
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["backend1"] = new DestinationConfig { Address = backendUrl }
        },
        // Optional: Force HTTP/2 for backend connections
        HttpRequest = new ForwarderRequestConfig
        {
            Version = HttpVersion.Version2,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
        }
    };
```

**Why this is optional:** YARP defaults to HTTP/2 with `RequestVersionOrLower` policy, automatically negotiating the best protocol. Explicit configuration is only needed for specific requirements (e.g., forcing HTTP/2-only).

### 3. WebSocket Support

**No configuration changes required.** YARP automatically:
- Detects WebSocket upgrade requests (HTTP/1.1 `101 Switching Protocols`)
- Proxies HTTP/2 WebSockets transparently
- Adapts headers between HTTP/1.1 and HTTP/2 WebSocket protocols
- Disables HTTP request timeouts after WebSocket handshake (.NET 8+)

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|-------------------------|
| **YARP 2.3.0** | Custom proxy with Kestrel | Use custom only if YARP limitations encountered. YARP is production-ready, Microsoft-maintained, specifically designed for HTTP/2/WebSocket proxying. |
| **Http1AndHttp2 with TLS** | Http2 without TLS (Prior Knowledge) | Use HTTP/2-only without TLS for development testing only. Production requires TLS for security and proper protocol negotiation. |
| **YARP default config** | Explicit ForwarderRequestConfig | Use explicit config only when backend requires specific HTTP version. Default automatic negotiation is sufficient for most cases. |

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| **Additional WebSocket libraries** (e.g., WebSocketSharp, ClientWebSocket) | Unnecessary overhead. YARP handles WebSocket proxying transparently at the transport layer. No custom WebSocket code needed. | Rely on YARP's built-in WebSocket support. |
| **HTTP/2 enforcement without backend support** | Will break connections to backends that don't support HTTP/2. | Use `RequestVersionOrLower` policy (YARP default) for automatic fallback. |
| **TLS configuration in v1.1** | HTTPS is explicitly deferred to v1.2. Focus on HTTP protocols only for this milestone. | Use HTTP/2 Prior Knowledge mode for development HTTP/2 testing. Defer TLS to v1.2. |
| **Manual header manipulation for WebSockets** | YARP automatically adds/removes HTTP/2 WebSocket headers. Manual intervention creates conflicts. | Let YARP handle header adaptation. Use custom transforms only for specific routing requirements. |
| **Ngrok, FRP, or other external proxies** | Defeats purpose of Portless.NET as a native .NET solution. Introduces external dependencies. | YARP is the proxy - it's already in the stack. |
| **SignalR server libraries in Portless.Proxy** | Portless is a proxy, not a SignalR host. SignalR apps should run on backend servers proxied through Portless. | Run SignalR on backend (destination) servers. Portless proxies the connections transparently. |

## Stack Patterns by Variant

**If backend supports HTTP/2:**
- YARP automatically uses HTTP/2 for backend connections
- No configuration needed if using HTTPS (ALPN negotiation)
- For HTTP (non-TLS), backend must support HTTP/2 Prior Knowledge

**If backend only supports HTTP/1.1:**
- YARP automatically falls back to HTTP/1.1 (default `RequestVersionOrLower` policy)
- Incoming HTTP/2 connections are downgraded to HTTP/1.1 for backend
- WebSocket connections work transparently across protocol versions

**If testing WebSockets without TLS:**
- Use HTTP/1.1 WebSocket upgrade (standard)
- Or use HTTP/2 WebSocket with Prior Knowledge if client supports it
- Browsers automatically detect and use advertised HTTP/2 WebSocket support

**If planning for SignalR:**
- SignalR over HTTP/2 works through YARP without special configuration
- SignalR over WebSockets works transparently (YARP handles upgrade)
- For Azure SignalR: Configure `ClientEndpoint` to point to proxy URL
- For self-hosted SignalR: Ensure sticky sessions if using multiple backend instances

## Version Compatibility

| Package | Compatible With | Notes |
|---------|-----------------|-------|
| **Yarp.ReverseProxy 2.3.0** | .NET 8+ | Minimum .NET 8 required. Portless uses .NET 10, fully compatible. |
| **.NET 10** | YARP 2.3.0 | No compatibility issues. .NET 10 includes all HTTP/2 and WebSocket features. |
| **Kestrel (in .NET 10)** | HTTP/2, HTTP/1.1, WebSockets | Full support for all protocols. HTTP/2 Prior Knowledge (non-TLS) supported. |
| **SignalR** | YARP 2.3.0 | No special configuration needed. SignalR connections proxied transparently. |

### HTTP/2 Support Matrix

| Incoming → Outgoing | HTTP/1.1 Backend | HTTP/2 Backend (TLS) | HTTP/2 Backend (non-TLS) |
|---------------------|------------------|----------------------|--------------------------|
| **HTTP/1.1 Client** | ✅ Works | ✅ Works (YARP upgrades) | ⚠️ Needs backend Prior Knowledge |
| **HTTP/2 Client (TLS)** | ✅ Works (YARP downgrades) | ✅ Works | ⚠️ TLS mismatch |
| **HTTP/2 Client (non-TLS)** | ✅ Works (YARP downgrades) | ⚠️ TLS mismatch | ✅ Works (both Prior Knowledge) |

### WebSocket Support Matrix

| Protocol | Works Through YARP | Configuration Needed |
|----------|-------------------|---------------------|
| **HTTP/1.1 WebSocket** | ✅ Yes (automatic) | None |
| **HTTP/2 WebSocket** | ✅ Yes (automatic) | None |
| **SignalR over HTTP/2** | ✅ Yes (automatic) | None |
| **SignalR over WebSockets** | ✅ Yes (automatic) | None (sticky sessions if multiple backends) |

## Integration with Existing Architecture

### DynamicConfigProvider Integration

**Current Implementation:** `DynamicConfigProvider` updates routes and clusters in memory.

**HTTP/2/WebSocket Impact:** Zero. The `IProxyConfigProvider` abstraction handles all protocols transparently. No changes needed to:
- `DynamicConfigProvider.Update()` method
- Route configuration structure
- Cluster configuration structure

**Recommendation:** Add optional `HttpRequest` configuration to `CreateCluster()` method if specific HTTP version control is needed. Otherwise, rely on YARP defaults.

### RouteStore Integration

**Current Implementation:** `RouteStore` persists hostname → port mappings to JSON.

**HTTP/2/WebSocket Impact:** Zero. Route storage is protocol-agnostic. No changes needed to:
- `RouteInfo` model (hostname, port, PID, timestamp)
- File persistence format
- Hot-reload mechanism

### ProcessManager Integration

**Current Implementation:** `ProcessManager` spawns commands with `PORT` environment variable.

**HTTP/2/WebSocket Impact:** Zero. Backend processes receive their assigned port and can use any HTTP protocol. No changes needed to:
- PORT variable injection
- Process spawning logic
- PID tracking

### CLI Commands Integration

**Current Implementation:** CLI commands (`proxy start`, `list`, `run`) manage proxy and routes.

**HTTP/2/WebSocket Impact:** Minimal. Only changes needed:
1. **`proxy start`**: Optional flag `--http2` or `--protocols Http1AndHttp2` for Kestrel configuration
2. **`list` output**: Optionally show protocol version (HTTP/1.1 vs HTTP/2) if detectable
3. **Help text**: Mention HTTP/2 and WebSocket support are enabled

**Recommended CLI Additions:**
```bash
# Optional: Explicit protocol selection
portless proxy start --protocols Http1AndHttp2

# Or: HTTP/2 only (development)
portless proxy start --protocols Http2

# Default: HTTP/1.1 only (current behavior)
portless proxy start
```

## Testing Strategy

### Unit Tests (xUnit)

**No changes required** to existing test infrastructure. Protocol handling is YARP's responsibility.

**Add tests for:**
- Kestrel configuration with different `HttpProtocols` values
- Cluster configuration with `ForwarderRequestConfig`
- Protocol version detection (if adding CLI flags)

### Integration Tests

**New test scenarios needed:**

1. **HTTP/2 Connection Test**
   - Start proxy with HTTP/2 enabled
   - Create route to HTTP/2 backend
   - Verify HTTP/2 response (check `:authority` header, binary protocol)

2. **WebSocket Test**
   - Start proxy
   - Create route to WebSocket echo server
   - Open WebSocket connection through proxy
   - Verify bidirectional messaging

3. **Protocol Downgrade Test**
   - Configure proxy for HTTP/2
   - Route to HTTP/1.1-only backend
   - Verify successful connection (YARP downgrades automatically)

4. **SignalR Test**
   - Start SignalR backend on assigned port
   - Connect through Portless proxy
   - Verify real-time messaging works

### Test Dependencies

```bash
# No new test packages needed
# Existing packages sufficient:
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package xunit.runner.visualstudio
```

**Test utilities:**
- Use `ClientWebSocket` for WebSocket testing
- Use `HttpClient` with HTTP/2 enabled for protocol testing
- Use SignalR client for SignalR integration tests

## Performance Considerations

| Aspect | HTTP/1.1 | HTTP/2 | WebSocket (HTTP/1.1) | WebSocket (HTTP/2) |
|--------|----------|--------|---------------------|-------------------|
| **Connection overhead** | High (1 req per connection) | Low (multiplexing) | Medium (persistent) | Low (multiplexed) |
| **Header overhead** | High (text headers) | Low (HPACK compression) | N/A (post-handshake) | N/A (post-handshake) |
| **Memory per connection** | Low | Medium | Medium | Medium |
| **YARP processing overhead** | Minimal | Minimal | Minimal | Minimal |
| **Backend compatibility** | Universal | Modern servers | Universal | Modern servers |

**Recommendation:** Use default `Http1AndHttp2` for maximum compatibility with automatic HTTP/2 opt-in.

## Migration Path from v1.0

### Phase 1: Basic HTTP/2 Support (1-2 days)
1. Add `--protocols` CLI flag to `proxy start` command
2. Configure Kestrel with selected `HttpProtocols` value
3. Test with HTTP/2-capable backend (e.g., ASP.NET Core 6+ with HTTP/2 enabled)
4. Verify backward compatibility with HTTP/1.1 backends

### Phase 2: WebSocket Verification (1 day)
1. Create WebSocket echo server example
2. Test WebSocket connections through proxy
3. Verify both HTTP/1.1 and HTTP/2 WebSocket work
4. Document WebSocket testing procedure

### Phase 3: SignalR Integration (1-2 days)
1. Create SignalR chat example
2. Test SignalR through proxy
3. Document SignalR configuration (if any needed)
4. Add integration test for SignalR scenario

### Phase 4: Documentation (1 day)
1. Update README with HTTP/2 support
2. Document WebSocket capabilities
3. Add troubleshooting section for protocol issues
4. Create examples: HTTP/2 backend, WebSocket echo, SignalR chat

**Total Estimated Effort:** 4-6 days

## Sources

### Official Documentation (HIGH Confidence)
- **[YARP Proxying WebSockets and SPDY](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/websockets?view=aspnetcore-10.0)** — Verified YARP 2.0+ supports HTTP/2 WebSockets automatically, Kestrel is only server supporting HTTP/2 WebSocket, browsers auto-detect support. Last updated: 2026-01-23
- **[YARP Proxying gRPC](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/grpc?view=aspnetcore-10.0)** — Verified HTTP/2 requires TLS for ALPN negotiation, HTTP/2 without TLS requires Prior Knowledge mode, outgoing protocols independent of incoming. Last updated: 2026-01-23
- **[Configure Kestrel Endpoints](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0)** — Verified `HttpProtocols.Http1AndHttp2` is default for TLS endpoints, protocol negotiation via ALPN. Last updated: 2025

### Community Resources (MEDIUM Confidence)
- **[YARP转发请求配置：ForwarderRequestConfig参数设置](https://m.blog.csdn.net/gitblog_01197/article/details/151086578)** — Verified `ForwarderRequestConfig` controls outbound HTTP version, defaults to HTTP/2 with `RequestVersionOrLower`. Published: 2025-09-01
- **[Azure SignalR with Reverse Proxies](https://learn.microsoft.com/en-us/azure/azure-signalr/signalr-howto-work-with-reverse-proxy)** — Verified HOST header rewriting requirement for Azure SignalR, `ClientEndpoint` configuration. Last updated: 2025-05

### Verified Implementation Details (HIGH Confidence)
- **YARP 2.3.0 Package** — Verified .NET 8+ requirement, includes HTTP/2 and WebSocket support, no additional dependencies needed. Source: NuGet package metadata and Microsoft documentation
- **Current Portless.NET Implementation** — Verified YARP 2.3.0 already installed, Kestrel configuration in `Program.cs` lines 41-44, cluster creation in lines 23-31. Source: Code review

### Verification Methodology
- Cross-referenced Microsoft Learn documentation (official, up-to-date)
- Verified current codebase has YARP 2.3.0 installed
- Confirmed .NET 10 includes all required HTTP/2 features
- Checked existing Kestrel configuration for upgrade path
- No conflicting information found across sources

---
*Stack research for: Portless.NET v1.1 - HTTP/2 and WebSocket Support*
*Researched: 2026-02-22*
*Confidence: HIGH - All claims verified with official Microsoft documentation or code inspection*
