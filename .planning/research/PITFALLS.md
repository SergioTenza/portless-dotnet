# Pitfalls Research

**Domain:** HTTP/2 and WebSocket support in YARP reverse proxy
**Researched:** 2026-02-21
**Confidence:** MEDIUM

## Critical Pitfalls

### Pitfall 1: HTTP/2 Protocol Negotiation Fallback to HTTP/1.1

**What goes wrong:**
YARP silently downgrades from HTTP/2 to HTTP/1.1 without logging or visibility. Developers believe they're getting HTTP/2 benefits (multiplexing, header compression) but are actually running HTTP/1.1, causing performance to be worse than expected. This is particularly problematic for gRPC services which **require** HTTP/2 and will fail completely.

**Why it happens:**
HTTP/2 over plain HTTP (non-TLS) requires explicit Kestrel configuration. YARP only auto-negotiates HTTP/2 for HTTPS/TLS connections by default. Without TLS, HTTP/2 must be explicitly enabled via `Protocols: "Http2"` in Kestrel endpoint configuration. Many developers assume HTTP/2 "just works" after enabling YARP.

**How to avoid:**
```csharp
// Explicitly configure HTTP/2 for non-TLS endpoints
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(1355, o => o.Protocols = HttpProtocols.Http2);
});
```

Add protocol version logging to verify actual negotiated protocol:
```csharp
app.Use(async (context, next) =>
{
    var protocol = context.Features.Get<IHttpProtocolFeature>()?.HttpProtocol ?? "unknown";
    logger.LogInformation("Request protocol: {Protocol}", protocol);
    await next();
});
```

**Warning signs:**
- gRPC calls fail with "HTTP/2 required" errors
- Browser DevTools Network tab shows "http/1.1" instead of "h2"
- No performance improvement after enabling HTTP/2
- `curl -v --http2` shows protocol downgrade

**Phase to address:**
Phase 1 (HTTP/2 Baseline) - Must verify protocol negotiation before building features that depend on it

---

### Pitfall 2: WebSocket Connection Timeout Mismatch

**What goes wrong:**
WebSocket connections unexpectedly close after 60 seconds (default proxy timeout). Clients think connection is still valid and reuse "zombie connections," triggering errors on next send. This manifests as random connection drops in real-time apps (SignalR, chat, dashboards).

**Why it happens:**
Reverse proxies have default idle timeouts (60s in many proxies) that are shorter than typical WebSocket heartbeat intervals. When no data flows for 60 seconds, the proxy closes the connection. Clients with longer timeouts (e.g., 120s) don't detect the closure until they try to send.

**How to avoid:**
```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    // Keep-alive timeout must be > client heartbeat interval
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);

    // Allow concurrent upgraded connections (HTTP/2 streams)
    options.Limits.MaxConcurrentUpgradedConnections = 100;
});
```

Configure client heartbeats to be 25-30 seconds with timeout at 1.5× interval (40-45s):
```javascript
// Client-side example
const connection = new HubConnectionBuilder()
    .withUrl('/chat')
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

// Keep-alive ping every 30s
connection.serverTimeoutInMilliseconds = 45000; // 45s timeout
connection.keepAliveIntervalInMilliseconds = 30000; // 30s ping
```

**Warning signs:**
- Connections drop after 60 seconds of inactivity
- Browser console shows "Connection closed unexpectedly" errors
- SignalR clients constantly reconnecting
- Load balancer/proxy logs showing timed-out connections

**Phase to address:**
Phase 2 (WebSocket Proxy) - Must configure timeouts before real-time features

---

### Pitfall 3: HTTP/2 WebSocket Method Mismatch (CONNECT vs GET)

**What goes wrong:**
WebSocket handshake fails because routes/controllers expect HTTP/1.1 `GET` requests with `Upgrade: websocket` headers, but HTTP/2 WebSockets use `CONNECT` method with `:protocol: websocket` pseudo-header per RFC 8441. Requests return 404 or 405 errors.

**Why it happens:**
HTTP/2 WebSockets use Extended CONNECT (RFC 8441) which changes the handshake method from `GET` to `CONNECT`. Existing code that checks for `GET` method or traditional WebSocket headers fails to recognize HTTP/2 WebSocket requests. This is a protocol-layer difference that's invisible to developers who only test with HTTP/1.1.

**How to avoid:**
YARP handles protocol translation automatically, but ensure routes don't have method restrictions:
```json
{
  "Clusters": {
    "websocket-cluster": {
      "Destinations": {
        "destination1": {
          "Address": "http://localhost:4000"
        }
      },
      "HttpRequest": {
        // Don't restrict to GET - allow CONNECT for HTTP/2
      }
    }
  }
}
```

Test both protocols explicitly:
```bash
# HTTP/1.1 WebSocket
curl -i -N \
  -H "Connection: Upgrade" \
  -H "Upgrade: websocket" \
  -H "Sec-WebSocket-Version: 13" \
  -H "Sec-WebSocket-Key: SGVsbG8sIHdvcmxkIQ==" \
  http://localhost:1355/ws

# HTTP/2 WebSocket (requires h2c support)
nghttp -n "GET /ws HTTP/2" -H ":protocol: websocket"
```

**Warning signs:**
- WebSocket works in curl but fails in browsers that support HTTP/2 WebSocket
- 405 Method Not Allowed errors for WebSocket endpoints
- Works in Firefox but fails in Chrome (browser-dependent HTTP/2 WebSocket support)
- Connection upgrade never completes

**Phase to address:**
Phase 2 (WebSocket Proxy) - Must test with both HTTP/1.1 and HTTP/2 clients

---

### Pitfall 4: Header Translation Breaking Application Logic

**What goes wrong:**
Application code breaks after adding HTTP/2 because it depends on HTTP/1.1-specific headers that don't exist in HTTP/2 (e.g., `Host` header becomes `:authority` pseudo-header). Code that reads headers directly fails silently or throws exceptions.

**Why it happens:**
HTTP/2 replaces many HTTP/1.1 headers with pseudo-headers:
- `:authority` replaces `Host`
- `:method` replaces request line method
- `:path` replaces request line path
- `:scheme` replaces the scheme
- `:status` replaces status line

YARP translates these between protocols, but application code that inspects headers directly may see pseudo-headers or missing headers depending on protocol. Code like `request.Headers["Host"]` returns empty for HTTP/2 requests.

**How to avoid:**
Use ASP.NET Core abstractions instead of raw headers:
```csharp
// BAD - Breaks with HTTP/2
var host = request.Headers["Host"].ToString();
var scheme = request.Headers["X-Forwarded-Proto"].ToString();

// GOOD - Works with both protocols
var host = request.Host.Value;
var scheme = request.Scheme;
var path = request.Path;
var method = request.Method;
```

For custom headers, verify they're not pseudo-headers:
```csharp
// Check if header is HTTP/2 pseudo-header
bool IsPseudoHeader(string header) => header.StartsWith(':');
```

**Warning signs:**
- Null reference exceptions when accessing headers after HTTP/2 enabled
- Authentication/authorization fails after protocol upgrade
- Logging shows empty header values
- Works in dev (HTTP/1.1) but fails in production (HTTP/2)

**Phase to address:**
Phase 1 (HTTP/2 Baseline) - Audit all header access before enabling HTTP/2

---

### Pitfall 5: HTTP/2 Performance Regression in Proxy Scenarios

**What goes wrong:**
Performance actually **degrades** after enabling HTTP/2 instead of improving. Throughput drops, latency increases, and CPU usage rises. Developers assume "HTTP/2 is faster" without understanding proxy overhead.

**Why it happens:**
HTTP/2 adds CPU overhead from:
- HPACK header compression/decompression
- Multiplexing state management
- Frame parsing and reassembly

In proxy scenarios (YARP), every request is decoded and re-encoded, doubling CPU overhead. HTTP/1.1's simplicity is faster for small payloads and low-latency scenarios. HTTP/2 wins with high-latency networks and many concurrent requests, but local development (Portless.NET's use case) is low-latency with few concurrent requests.

**How to avoid:**
Benchmark with realistic load before committing to HTTP/2:
```csharp
// BenchmarkDotNet test comparing protocols
[MemoryDiagnoser]
public class HttpProtocolBenchmark
{
    private HttpClient _http11Client;
    private HttpClient _http2Client;

    [GlobalSetup]
    public void Setup()
    {
        _http11Client = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(1)
        });

        _http2Client = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(1)
        });
        // HTTP/2 requires version specification
    }

    [Benchmark]
    public async Task<string> Http11()
    {
        return await _http11Client.GetStringAsync("http://localhost:1355/api");
    }

    [Benchmark]
    public async Task<string> Http2()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:1355/api");
        request.Version = HttpVersion.Version20;
        var response = await _http2Client.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }
}
```

Consider making HTTP/2 opt-in for routes that benefit (gRPC, high-latency backends) while keeping HTTP/1.1 default for others.

**Warning signs:**
- CPU usage increases after HTTP/2 enabled
- Requests per second decreases
- p95/p99 latency increases
- Profiler shows time spent in compression/ framing code

**Phase to address:**
Phase 3 (Testing & Validation) - Must benchmark before declaring "complete"

---

### Pitfall 6: Silent Protocol Downgrade Breaking Features

**What goes wrong:**
Features that require HTTP/2 (like gRPC) silently fail or behave incorrectly when protocol downgrades to HTTP/1.1. Error messages are misleading because the root cause (protocol mismatch) is hidden.

**Why it happens:**
YARP's `RequestVersionOrLower` policy automatically downgrades when HTTP/2 negotiation fails. This is intended for graceful fallback, but for protocols like gRPC that **require** HTTP/2, the fallback should fail fast with a clear error instead of attempting HTTP/1.1.

**How to avoid:**
For gRPC or other HTTP/2-only services, use strict version policy:
```json
{
  "Clusters": {
    "grpc-service": {
      "HttpRequest": {
        "Version": "2",
        "VersionPolicy": "Exact"  // Don't downgrade
      }
    }
  }
}
```

Or in code:
```csharp
var requestConfig = new ForwarderRequestConfig
{
    Version = HttpVersion.Version20,
    VersionPolicy = HttpVersionPolicy.ExactVersion // Fail if HTTP/2 not available
};
```

Add explicit validation:
```csharp
if (requestContext.Request.Protocol != "HTTP/2")
{
    logger.LogWarning("gRPC request on non-HTTP/2 protocol: {Protocol}", requestContext.Request.Protocol);
    // Return 505 HTTP Version Not Supported
}
```

**Warning signs:**
- gRPC calls fail with unclear errors
- Features work intermittently
- Browser DevTools shows "http/1.1" for gRPC endpoints
- Logs show version mismatches

**Phase to address:**
Phase 3 (Testing & Validation) - Must add validation for HTTP/2-only features

---

### Pitfall 7: Connection Pooling Exhaustion with HTTP/2

**What goes wrong:**
HTTP/2's multiplexing (single connection for many requests) causes connection pool exhaustion when backends don't properly support HTTP/2 concurrent streams. Requests queue up and timeout.

**Why it happens:**
HTTP/2 uses a single TCP connection with multiple streams instead of multiple connections. If the backend has a low limit on concurrent streams (or doesn't support HTTP/2 at all), all requests queue on that single connection instead of distributing across multiple HTTP/1.1 connections.

YARP's default connection limits assume HTTP/1.1 behavior (many connections). With HTTP/2, you need higher stream limits per connection.

**How to avoid:**
Configure HTTP/2 stream limits explicitly:
```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(1355, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
        // Increase stream limit for backends that support it
        listenOptions.UseHttps(); // TLS enables proper HTTP/2
    });

    // Limits for outgoing connections
    options.Limits.MaxConcurrentConnections = 100;
    options.Limits.MaxConcurrentUpgradedConnections = 100;
});
```

Monitor connection pool metrics:
```csharp
// Add diagnostics
var diagnostics = app.Services.GetRequiredService<ForwarderHttpClientDiagnostics>();
diagnostics.ConnectionPoolFailed += (sender, args) =>
{
    logger.LogError("Connection pool failure: {Reason}", args.Reason);
};
```

**Warning signs:**
- Requests timeout under load
- "Connection pool exhausted" errors
- Performance degrades with concurrent requests
- Works with single request but fails with parallel requests

**Phase to address:**
Phase 3 (Testing & Validation) - Must load test with concurrent requests

---

## Technical Debt Patterns

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Default HTTP/2 for all routes | Simple configuration, "future-proof" | Performance regression for HTTP/1.1-optimized backends, debugging complexity | Never - should be opt-in per route |
| Assume YARP handles all translation | Less code to write | Subtle bugs when headers are inspected directly, hard to debug | Only in MVP, must add validation before v1.1 |
| Test only with HTTP/1.1 | Faster initial development, simpler tests | Missed HTTP/2 WebSocket handshake issues, browser compatibility problems | Never - must test both protocols from Phase 1 |
| Ignore protocol version in logs | Simpler logging | Can't detect silent downgrades, impossible to debug performance issues | Never - protocol logging is essential |
| Use default timeout values | Works for initial testing | WebSocket connections drop, poor UX in real-time apps | Never - timeouts must match client heartbeat |

## Integration Gotchas

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| SignalR | Assuming HTTP/1.1 WebSocket upgrade headers work with HTTP/2 | Use SignalR's built-in HTTP/2 WebSocket support (requires .NET 7+), test with both protocols |
| gRPC services | Not setting `VersionPolicy: Exact` for gRPC backends | Always use strict HTTP/2 requirement for gRPC, fail fast if unavailable |
| Blazor WebAssembly | Not configuring HTTP/2 for Blazor Server endpoint | Blazor uses HTTP/2 automatically, ensure proxy doesn't downgrade |
| Kestrel backends | Assuming HTTP/2 works over plain HTTP | Configure explicit `Protocols: Http2` or use HTTPS for auto-negotiation |
| Console apps (HttpClient) | Not setting `HttpRequestMessage.Version = HttpVersion.Version20` | Explicitly set version when testing HTTP/2, or use `HttpVersionPolicy.RequestVersionOrLower` |

## Performance Traps

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Premature HTTP/2 optimization | CPU usage ↑, latency ↑, throughput ↓ | Benchmark with realistic local dev load (1-10 concurrent requests) | Immediately in local dev scenario |
| Missing connection pool limits | Timeouts under load, "pool exhausted" errors | Configure `MaxConcurrentConnections` and `MaxConcurrentUpgradedConnections` | At 10+ concurrent backend services |
| HTTP/2 on every route | Complexity ↑, debugging hard, mixed protocols | Make HTTP/2 opt-in, document which routes need it | When adding 3+ HTTP/2-only backends |
| Ignoring HPACK compression cost | High CPU on header-heavy requests | Monitor CPU during load test, profile compression overhead | At 1000+ req/s with large headers |
| Single HTTP/2 connection bottleneck | Requests queue, timeouts under load | Configure appropriate stream limits, monitor pool metrics | When backend has <100 stream limit |

## Security Mistakes

| Mistake | Risk | Prevention |
|---------|------|------------|
| HTTP/2 without TLS | Protocol downgrade attacks, middlebox tampering | Use HTTPS/TLS for production, HTTP/2 over HTTP only for local dev | Always use TLS in production |
| Confusing `:authority` with `Host` for auth | Authorization bypass, privilege escalation | Use `HttpContext.Request.Host` abstraction, never raw headers | Validate all routing/auth with abstraction layer |
| WebSocket without Origin check | CSRF attacks on WebSocket endpoints | Verify `Origin` header matches expected frontend domain | Add Origin validation in Phase 2 |
| HTTP/2 fingerprinting exposure | Information disclosure about server stack | Minimize `Server` header, consider standard responses | Hardening phase (post-MVP) |

## UX Pitfalls

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| Silent protocol downgrade | "It's slow" but no error message | Log protocol version, show warning in CLI status output | Add protocol status to `portless proxy status` |
| WebSocket drops without notification | Chat/dashboard stops updating, user thinks app broken | Graceful reconnection with user notification (SignalR auto-reconnect) | Implement reconnection UI in examples |
| HTTP/2-only routes not documented | Developers try HTTP/1.1, get cryptic errors | Clear documentation: "This feature requires HTTP/2" | Document in Phase 3, add validation errors |
| No indication of active protocol | Can't debug issues, doesn't know if HTTP/2 working | CLI command `portless proxy protocols` shows active protocol per route | Add diagnostic command in Phase 3 |

## "Looks Done But Isn't" Checklist

- [ ] **HTTP/2 enabled:** Often missing protocol verification — verify with `curl -v --http2` and browser DevTools
- [ ] **WebSocket working:** Often missing timeout configuration — test connection stays alive >60s with no activity
- [ ] **Both protocols tested:** Often missing HTTP/2 WebSocket tests — test with Chrome (has HTTP/2 WS) and older browsers
- [ ] **Header compatibility:** Often breaks apps reading raw headers — audit all `Headers["..."]` access
- [ ] **Performance validated:** Often assumed HTTP/2 = faster — benchmark with realistic load
- [ ] **Error messages clear:** Often silent protocol downgrade — add explicit logging and fail-fast for HTTP/2-only features
- [ ] **Documentation complete:** Often missing protocol requirements — document which features need HTTP/2
- [ ] **Examples work:** Often only tested with HTTP/1.1 — verify all examples work with HTTP/2 enabled

## Recovery Strategies

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| HTTP/2 silent downgrade | LOW | Add protocol logging, identify affected routes, either fix backend or force HTTP/1.1 |
| WebSocket timeout drops | LOW | Configure keep-alive timeout, add client heartbeat, test with 90s idle |
| Header translation breaks apps | MEDIUM | Audit all header access, replace with ASP.NET Core abstractions (`request.Host`, `request.Scheme`) |
| HTTP/2 performance regression | MEDIUM | Benchmark to confirm, make HTTP/2 opt-in per route, document which routes need it |
| Protocol-specific routing issues | MEDIUM | Add route metadata for protocol requirements, implement version policy checks |
| Connection pool exhaustion | HIGH | Reconfigure pool limits, possibly separate HTTP/1.1 and HTTP/2 endpoints, redesign routing |

## Pitfall-to-Phase Mapping

How roadmap phases should address these pitfalls.

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| HTTP/2 protocol negotiation fallback | Phase 1 (HTTP/2 Baseline) | Log protocol version, test with `curl --http2`, verify h2 in browser DevTools |
| WebSocket timeout mismatch | Phase 2 (WebSocket Proxy) | Test idle connection >90s, verify heartbeat configuration, add timeout logging |
| HTTP/2 WebSocket method mismatch | Phase 2 (WebSocket Proxy) | Test with both HTTP/1.1 and HTTP/2 clients, verify CONNECT method handling |
| Header translation breaking logic | Phase 1 (HTTP/2 Baseline) | Audit all header access, add unit tests for both protocols, use abstractions |
| HTTP/2 performance regression | Phase 3 (Testing & Validation) | BenchmarkDotNet tests, measure req/s and latency, compare HTTP/1.1 vs HTTP/2 |
| Silent protocol downgrade | Phase 3 (Testing & Validation) | Add strict version policy for gRPC, validate protocol in middleware, fail fast |
| Connection pooling exhaustion | Phase 3 (Testing & Validation) | Load test with 50+ concurrent requests, monitor pool metrics, configure limits |

## Sources

- [Microsoft YARP gRPC Documentation](https://learn.microsoft.com/zh-cn/aspnet/core/fundamentals/servers/yarp/grpc?view=aspnetcore-8.0) - HTTP/2 configuration requirements, protocol negotiation details
- [Microsoft YARP Diagnostics Guide](https://learn.microsoft.com/zh-cn/aspnet/core/fundamentals/servers/yarp/diagnosing-yarp-issues) - Published June 20, 2025
- [Microsoft YARP Timeouts Documentation](https://learn.microsoft.com/zh-cn/aspnet/core/fundamentals/servers/yarp/timeouts) - Published January 22, 2026
- [ASP.NET Core Kestrel HTTP/2 Documentation](https://learn.microsoft.com/zh-cn/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0) - Protocol configuration, TLS requirements
- [RFC 8441: Bootstrapping WebSockets with HTTP/2](https://datatracker.ietf.org/doc/html/rfc8441) - Extended CONNECT protocol specification
- [Envoy HTTP Upgrades Documentation](https://www.envoyproxy.io/docs/envoy/latest/intro/arch_overview/http/upgrades) - WebSocket over HTTP/2 transformation patterns
- [Nginx WebSocket Proxying Guide](https://nginx.org/en/docs/http/websocket.html) - HTTP/1.1 WebSocket proxy configuration
- [.NET HTTP/2 WebSocket Support Announcement](https://devblogs.microsoft.com/aspnet/asp-net-core-updates-in-net-7/) - .NET 7 HTTP/2 WebSocket support details
- [YARP GitHub Repository](https://github.com/microsoft/reverse-proxy) - Issue tracking for known WebSocket/HTTP/2 problems
- [libwebsockets Testing Suite](https://libwebsockets.org/) - Reference for HTTP/2 WebSocket testing patterns (h2spec, h2load)
- [ASP.NET Core Performance Tuning Guide](https://learn.microsoft.com/en-us/aspnet/core/performance/performance-best-practices) - HTTP/2 CPU overhead considerations
- [Kestrel Configuration Limits Documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/options) - Connection pool and timeout configuration

---
*Pitfalls research for: Portless.NET HTTP/2 and WebSocket support*
*Researched: 2026-02-21*
