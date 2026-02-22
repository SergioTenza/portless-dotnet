# Feature Research

**Domain:** Reverse Proxy - HTTP/2 and WebSocket Support
**Researched:** 2026-02-21
**Confidence:** MEDIUM

## Feature Landscape

### Table Stakes (Users Expect These)

Features users assume exist. Missing these = product feels incomplete.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| **HTTP/2 multiplexing** | Developers expect concurrent requests over single connection | MEDIUM | YARP supports this natively, requires Kestrel HTTP/2 configuration |
| **HTTP/2 header compression (HPACK)** | Standard HTTP/2 feature that reduces overhead | LOW | YARP handles automatically, requires monitoring memory usage |
| **WebSocket upgrade handling** | Required for real-time apps (SignalR, chat, dashboards) | MEDIUM | YARP supports both HTTP/1.1 and HTTP/2 WebSocket automatic |
| **Protocol auto-negotiation** | Browsers expect HTTP/2 when available | LOW | YARP defaults to HTTP/2 with RequestVersionOrLower policy |
| **Long-lived connection support** | WebSockets require connections that don't timeout | MEDIUM | Must disable HTTP timeouts after WebSocket handshake |
| **Connection: Upgrade header passthrough** | WebSocket handshake requires hop-by-hop headers | LOW | YARP handles header transformation between HTTP versions |
| **gRPC support** | Modern .NET microservices use gRPC over HTTP/2 | LOW | YARP supports gRPC natively with HTTP/2 |
| **X-Forwarded-* headers** | Backend services need original client info | LOW | Standard YARP transform for X-Forwarded-For, X-Forwarded-Proto |

### Differentiators (Competitive Advantage)

Features that set the product apart. Not required, but valuable.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| **HTTP/3 (QUIC) support** | Future-proof with TCP head-of-line blocking elimination | HIGH | YARP supports HTTP/3 on .NET 8+, requires QUIC protocol configuration |
| **Per-route protocol configuration** | Flexibility to mix HTTP/1.1, HTTP/2, HTTP/3 per service | MEDIUM | Allows gradual migration and testing of new protocols |
| **Connection pooling metrics** | Visibility into backend connection utilization | MEDIUM | Helps identify when multiplexing benefits saturate |
| **Automatic heartbeat for WebSockets** | Prevents proxy timeout drops without manual ping/pong | HIGH | Requires detecting idle connections and injecting ping frames |
| **Protocol downgrade detection** | Warn developers when HTTP/2 falls back to HTTP/1.1 | LOW | Logging/monitoring when protocol negotiation fails |
| **WebSocket connection lifecycle events** | CLI visibility into WebSocket connections (count, duration) | MEDIUM | Enhances development experience for real-time apps |
| **Integrated 103 Early Hints support** | Modern alternative to HTTP/2 server push for preloading | HIGH | Safari doesn't support, limited browser adoption currently |
| **Custom HTTP client factory** | Fine-tuned HTTP/2 connection settings per cluster | MEDIUM | EnableMultipleHttp2Connections, MaxConnectionsPerServer tuning |

### Anti-Features (Commonly Requested, Often Problematic)

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| **HTTP/2 Server Push** | Seemingly faster resource loading | Being deprecated in favor of 103 Early Hints; poor browser support; complexity for minimal gain | Use 103 Early Hints with Link preload headers |
| **Aggressive timeout values** | Prevent "hung" connections | Breaks WebSocket long-lived connections; causes 60-second drops | Use heartbeat/ping mechanism instead of short timeouts |
| **Forced HTTP/2 for all backend connections** | "Maximize performance" | HTTP/2 provides minimal benefit for low-latency backend connections; adds complexity | HTTP/2 for client-facing, HTTP/1.1 acceptable for backend |
| **Protocol conversion proxying** | Support mixed HTTP versions | Creates security vulnerabilities (request smuggling, DoS attacks); conversion bugs | End-to-end HTTP/2 or HTTP/1.1, avoid conversion |
| **Manual header manipulation for WebSockets** | "Full control" over upgrade headers | YARP handles automatic transformation between HTTP/1.1 and HTTP/2; manual manipulation breaks this | Trust YARP's automatic header handling |
| **Transparent WebSocket proxying** | Zero-config for development tools | Proxies that don't understand WebSocket drop connections after handshake | Explicit WebSocket-aware configuration or WSS (WebSocket Secure) |
| **Unlimited concurrent streams** | "No throttling" | Can overwhelm backend servers; causes request bursts instead of steady traffic; CPU spikes | Implement throttling/queuing to smooth request patterns |

## Feature Dependencies

```
[HTTP/2 Client Support]
    └──requires──> [Kestrel HTTP/2 Configuration]
                    └──requires──> [HTTPS/TLS for production HTTP/2]

[WebSocket over HTTP/2]
    └──requires──> [.NET 7+ and YARP 2.0+]
    └──enhances──> [HTTP/2 multiplexing]

[HTTP/3/QUIC Support]
    └──requires──> [.NET 8+]
    └──requires──> [Kestrel HTTP/3 configuration]

[Long-lived WebSocket Connections]
    └──requires──> [Timeout handling for idle connections]
    └──conflicts──> [Aggressive HTTP timeouts]

[103 Early Hints]
    └──enhances──> [Resource preloading]
    └──conflicts──> [HTTP/2 Server Push]
```

### Dependency Notes

- **HTTP/2 Client Support requires Kestrel HTTP/2 Configuration:** HTTP/2 requires Kestrel to be configured with HTTP/2 enabled. For production (non-localhost), this requires HTTPS/TLS as HTTP/2 without encryption is rarely supported.
- **WebSocket over HTTP/2 requires .NET 7+ and YARP 2.0+:** HTTP/2 WebSocket support was added in .NET 7 and YARP 2.0. This is a newer feature that requires careful testing.
- **HTTP/3/QUIC Support requires .NET 8+:** HTTP/3 support is only available in .NET 8 and later versions. Portless.NET targets .NET 10, so this is feasible but adds significant complexity.
- **Long-lived WebSocket Connections requires Timeout handling:** WebSocket connections are designed to be long-lived, but default HTTP timeouts (often 60 seconds) will terminate them. This requires special timeout configuration after the WebSocket handshake completes.
- **Long-lived WebSocket Connections conflicts with Aggressive HTTP timeouts:** You cannot have both short HTTP timeouts and long-lived WebSocket connections in the same proxy configuration.
- **103 Early Hints enhances Resource preloading:** Early Hints is the modern replacement for HTTP/2 Server Push, allowing browsers to preload resources before the main response arrives.
- **103 Early Hints conflicts with HTTP/2 Server Push:** These are competing approaches to the same problem. Server Push is being deprecated in favor of Early Hints.

## MVP Definition

### Launch With (v1.1)

Minimum viable product — what's needed to validate the concept for HTTP/2 and WebSocket support.

- [ ] **HTTP/2 multiplexing** — Core performance benefit that developers expect from HTTP/2
- [ ] **WebSocket upgrade handling** — Essential for real-time applications (SignalR, chat, dashboards)
- [ ] **Protocol auto-negotiation** — Browsers automatically use HTTP/2 when available
- [ ] **X-Forwarded headers** — Backend services need original client information
- [ ] **Long-lived connection support** — WebSocket connections must not timeout after 60 seconds

### Add After Validation (v1.2+)

Features to add once core is working.

- [ ] **HTTP/3 (QUIC) support** — Requires additional configuration and testing; eliminates TCP head-of-line blocking
- [ ] **Per-route protocol configuration** — Allows mixing protocols; useful for gradual migration
- [ ] **WebSocket connection lifecycle events** — CLI visibility helps debugging real-time apps
- [ ] **Custom HTTP client factory** — Advanced tuning for high-throughput scenarios

### Future Consideration (v2+)

Features to defer until product-market fit is established.

- [ ] **103 Early Hints support** — Limited browser adoption (Safari doesn't support); still emerging standard
- [ ] **Automatic heartbeat for WebSockets** — Complex to implement correctly; applications can handle ping/pong themselves
- [ ] **Connection pooling metrics** — Nice-to-have for observability; not critical for local development

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| HTTP/2 multiplexing | HIGH | MEDIUM | P1 |
| WebSocket upgrade handling | HIGH | MEDIUM | P1 |
| Protocol auto-negotiation | HIGH | LOW | P1 |
| X-Forwarded headers | HIGH | LOW | P1 |
| Long-lived connection support | HIGH | MEDIUM | P1 |
| HTTP/3 (QUIC) support | MEDIUM | HIGH | P2 |
| Per-route protocol configuration | MEDIUM | MEDIUM | P2 |
| WebSocket connection lifecycle events | MEDIUM | MEDIUM | P2 |
| Custom HTTP client factory | LOW | MEDIUM | P3 |
| Automatic heartbeat for WebSockets | MEDIUM | HIGH | P3 |
| Connection pooling metrics | LOW | MEDIUM | P3 |
| 103 Early Hints support | LOW | HIGH | P3 |

**Priority key:**
- P1: Must have for launch (v1.1)
- P2: Should have, add when possible (v1.2+)
- P3: Nice to have, future consideration (v2+)

## Competitor Feature Analysis

| Feature | Portless (Node.js) | YARP Default | Our Approach |
|---------|-------------------|--------------|--------------|
| HTTP/2 support | Limited (Node.js http proxy) | Full support with Kestrel | Leverage YARP's native HTTP/2 |
| WebSocket proxy | Supported via http-proxy-middleware | Automatic header handling | Use YARP's built-in WebSocket support |
| Protocol negotiation | Manual | Automatic (RequestVersionOrLower) | Trust YARP defaults |
| Timeout handling | Configurable per proxy | Requires manual configuration | Add timeout bypass for WebSocket |
| HTTP/3 support | Not available | Supported on .NET 8+ | Defer to v1.2+, focus on HTTP/2 first |
| Windows support | Limited/Experimental | Full native support | Key differentiator for Portless.NET |

## Sources

### High Confidence (Official Documentation)

- [YARP WebSockets Documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/websockets?view=aspnetcore-9.0) - Official YARP WebSocket support documentation (MEDIUM confidence - official source)
- [YARP HTTP Client Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/http-client-config?view=aspnetcore-9.0) - Official HTTP client configuration guide (MEDIUM confidence - official source)
- [ASP.NET Core WebSocket Support](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/websockets) - ASP.NET Core WebSocket documentation (MEDIUM confidence - official source)
- [YARP Diagnosing Issues](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/diagnosing-yarp-issues) - Official troubleshooting guide (MEDIUM confidence - official source)

### Medium Confidence (WebSearch + Verification)

- [YARP HTTP/2 Configuration - Microsoft AI Team Blog](https://learn.microsoft.com/en-us/shows/on-net/inside-net-8-improvements-for-performance-critical-apps/) - Microsoft AI team uses YARP with HTTP/2/HTTP/3 for 1B+ users (MEDIUM confidence - official Microsoft source)
- [SignalR Reverse Proxy Requirements](https://learn.microsoft.com/en-us/azure/azure-signalr/signalr-howto-work-with-reverse-proxy) - Azure SignalR reverse proxy configuration requirements (MEDIUM confidence - official Microsoft source)

### Low Confidence (WebSearch Only - Needs Validation)

- [HTTP/2 Multiplexing Request Bursts - Lucidchart Case Study](https://engineering.lucidsoftware.com/2016/06/02/http2-at-lucid/) - Real-world performance issues with HTTP/2 multiplexing (LOW confidence - WebSearch only, third-party blog)
- [HTTP/2 Protocol Conversion Vulnerabilities - USENIX Security 2022](https://www.usenix.org/conference/usenixsecurity22/presentation/merget) - Academic paper on HTTP/2 proxy conversion security issues (LOW confidence - WebSearch only, needs verification)
- [WebSocket Reverse Proxy Timeout Issues](https://nginx.org/en/docs/http/websocket.html) - Nginx WebSocket proxy documentation (LOW confidence - WebSearch only, not YARP-specific)
- [103 Early Hints vs HTTP/2 Server Push](https://developer.chrome.com/blog/early-hints/) - Chrome documentation on Early Hints adoption (LOW confidence - WebSearch only, limited browser support)

### Key Gaps Requiring Validation

1. **YARP-specific timeout configuration for WebSocket** - Found general reverse proxy patterns, but need YARP-specific documentation
2. **HTTP/2 stream limits and memory usage in YARP** - Found NGINX-specific data, YARP may differ
3. **Portless (Node.js) current HTTP/2/WebSocket capabilities** - Assumed limited based on Node.js http-proxy limitations, not verified
4. **Windows-specific HTTP/2 behavior** - Most documentation is Linux-focused, Windows may have differences

---
*Feature research for: Portless.NET v1.1 Advanced Protocols*
*Researched: 2026-02-21*
