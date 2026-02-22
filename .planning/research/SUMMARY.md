# Project Research Summary

**Project:** Portless.NET v1.1 - HTTP/2 and WebSocket Support
**Domain:** Reverse Proxy with Advanced Protocol Support
**Researched:** 2026-02-22
**Confidence:** HIGH

## Executive Summary

Portless.NET v1.1 adds HTTP/2 and WebSocket support to the existing Portless.NET reverse proxy. This is a **configuration and validation task, not a major architectural rewrite**. YARP 2.3.0 already includes full support for both protocols—the existing stack requires no new packages. The research reveals that HTTP/2 multiplexing, header compression (HPACK), and WebSocket proxying work transparently through YARP with minimal code changes. The primary work involves configuring Kestrel to enable HTTP/2 on the proxy endpoint and ensuring proper timeout handling for long-lived WebSocket connections.

The recommended approach is **minimal modification**: enable `HttpProtocols.Http1AndHttp2` in Kestrel configuration, add optional `ForwarderRequestConfig` for outbound HTTP/2 version policy, and rely on YARP's automatic protocol negotiation. WebSocket support requires zero code changes—YARP handles both HTTP/1.1 upgrade and HTTP/2 WebSocket (RFC 8441) transparently. The architecture remains protocol-agnostic in the core (RouteStore, ProcessManager), with protocol configuration isolated to the proxy layer.

Key risks center on **silent protocol downgrades** (HTTP/2 falling back to HTTP/1.1 without logging) and **WebSocket timeout mismatches** (connections dropping after 60 seconds of inactivity). Mitigation involves adding protocol version logging, configuring Kestrel keep-alive timeouts, and testing with both HTTP/1.1 and HTTP/2 clients. Performance may actually degrade with HTTP/2 in local development scenarios due to CPU overhead from HPACK compression—benchmarking is required before declaring HTTP/2 "faster."

## Key Findings

### Recommended Stack

**No new packages required.** YARP 2.3.0 + .NET 10 + Kestrel already include full HTTP/2 and WebSocket support. This is a configuration change, not a dependency addition.

**Core technologies:**
- **YARP 2.3.0** — Reverse proxy engine with native HTTP/2 WebSocket support since .NET 7, automatic protocol negotiation, and transparent header adaptation between HTTP versions
- **.NET 10 Kestrel** — Web server with `HttpProtocols.Http1AndHttp2` configuration, supporting both HTTP/2 Prior Knowledge (non-TLS) and ALPN negotiation (TLS)
- **Existing CLI framework (Spectre.Console.Cli 0.53.1)** — No changes needed; protocol support is proxy-layer only

**Configuration changes only:**
- Add `listenOptions.Protocols = HttpProtocols.Http1AndHttp2` to Kestrel configuration
- Optional: Add `HttpRequest.Version = HttpVersion.Version2` to cluster config for outbound HTTP/2

### Expected Features

**Must have (table stakes):**
- **HTTP/2 multiplexing** — Developers expect concurrent requests over single connection; YARP supports natively with Kestrel HTTP/2 configuration
- **WebSocket upgrade handling** — Required for real-time apps (SignalR, chat, dashboards); YARP supports both HTTP/1.1 and HTTP/2 WebSocket automatically
- **Protocol auto-negotiation** — Browsers automatically use HTTP/2 when available; YARP defaults to `RequestVersionOrLower` policy
- **X-Forwarded headers** — Backend services need original client info; standard YARP transform
- **Long-lived connection support** — WebSocket connections must not timeout; requires Kestrel timeout configuration

**Should have (competitive):**
- **HTTP/3 (QUIC) support** — Future-proof with TCP head-of-line blocking elimination; YARP supports on .NET 8+, defer to v1.2+
- **Per-route protocol configuration** — Flexibility to mix HTTP/1.1, HTTP/2, HTTP/3 per service; useful for gradual migration
- **Protocol downgrade detection** — Warn developers when HTTP/2 falls back to HTTP/1.1; add logging

**Defer (v2+):**
- **103 Early Hints support** — Limited browser adoption (Safari doesn't support); still emerging standard
- **Automatic heartbeat for WebSockets** — Complex to implement correctly; applications can handle ping/pong themselves
- **Connection pooling metrics** — Nice-to-have for observability; not critical for local development

### Architecture Approach

**Minimal architectural changes required.** The existing v1.0 architecture already separates protocol handling (proxy layer) from business logic (core layer). HTTP/2 and WebSocket support are configuration-only changes in `Portless.Proxy/Program.cs`. Core components (RouteStore, PortAllocator, ProcessManager) remain protocol-agnostic—no code changes needed.

**Major components:**
1. **Portless.Proxy/Program.cs** — Add Kestrel HTTP/2 configuration (`HttpProtocols.Http1AndHttp2`), optional cluster `HttpRequest` config for outbound HTTP/2 version policy
2. **YARP Reverse Proxy** — Handles HTTP/2/WebSocket proxying transparently: protocol negotiation, header adaptation, WebSocket upgrade (both HTTP/1.1 and HTTP/2)
3. **Protocol-agnostic Core** — RouteStore, ProcessManager, RouteInfo remain unchanged; storage and process management don't care about HTTP version

**YARP handles automatically:**
- HTTP/2 ↔ HTTP/1.1 protocol adaptation
- HTTP/1.1 WebSocket upgrade (101 Switching Protocols)
- HTTP/2 WebSocket (RFC 8441 Extended CONNECT)
- Header translation between pseudo-headers (`:authority`) and HTTP/1.1 headers (`Host`)

### Critical Pitfalls

1. **HTTP/2 Protocol Negotiation Fallback** — YARP silently downgrades from HTTP/2 to HTTP/1.1 without logging when HTTP/2 negotiation fails. Developers believe they're getting HTTP/2 benefits but are actually running HTTP/1.1. **Avoid:** Add protocol version logging in middleware, verify with `curl -v --http2` and browser DevTools (look for "h2").

2. **WebSocket Connection Timeout Mismatch** — WebSocket connections unexpectedly close after 60 seconds (default proxy timeout) when no data flows. Clients with longer timeouts don't detect closure until next send. **Avoid:** Configure `Kestrel.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2)`, set client heartbeat to 25-30 seconds with timeout at 1.5× interval.

3. **HTTP/2 WebSocket Method Mismatch** — WebSocket handshake fails because routes expect HTTP/1.1 `GET` with `Upgrade: websocket`, but HTTP/2 WebSockets use `CONNECT` method with `:protocol: websocket` pseudo-header (RFC 8441). **Avoid:** Don't restrict route methods (allow both GET and CONNECT), test with both HTTP/1.1 and HTTP/2 clients.

4. **Header Translation Breaking Application Logic** — Application code breaks after adding HTTP/2 because it depends on HTTP/1.1-specific headers that don't exist in HTTP/2 (e.g., `Host` → `:authority`). **Avoid:** Use ASP.NET Core abstractions (`request.Host`, `request.Scheme`) instead of raw header access (`request.Headers["Host"]`).

5. **HTTP/2 Performance Regression** — Performance degrades after enabling HTTP/2 instead of improving. HTTP/2 adds CPU overhead from HPACK compression and multiplexing state management. In local dev (low latency, few concurrent requests), HTTP/1.1's simplicity is faster. **Avoid:** Benchmark with realistic load before committing to HTTP/2, consider making HTTP/2 opt-in per route.

## Implications for Roadmap

Based on research, suggested phase structure:

### Phase 1: HTTP/2 Baseline
**Rationale:** HTTP/2 is foundational infrastructure. Must verify protocol negotiation works correctly before building features that depend on it (e.g., gRPC, SignalR over HTTP/2). This phase focuses on Kestrel configuration and protocol verification.

**Delivers:**
- Kestrel configured with `HttpProtocols.Http1AndHttp2`
- HTTP/2 integration test verifying protocol negotiation
- Protocol version logging middleware
- Documentation for HTTP/2 testing (`curl --http2`, browser DevTools)

**Addresses:**
- HTTP/2 multiplexing (table stakes feature)
- Protocol auto-negotiation (table stakes feature)

**Avoids:**
- Silent protocol downgrade (Pitfall 1) — adds logging
- Header translation breaks (Pitfall 4) — audits header access before enabling HTTP/2

**Uses:**
- YARP 2.3.0 HTTP/2 support (no new packages)
- Kestrel HTTP/2 configuration

**Implements:**
- Modified `Portless.Proxy/Program.cs` with Kestrel HTTP/2 config
- Optional cluster `ForwarderRequestConfig` for outbound HTTP/2

### Phase 2: WebSocket Proxy
**Rationale:** WebSocket support is critical for real-time apps (SignalR, chat, dashboards). YARP handles WebSocket proxying automatically, but requires timeout configuration to prevent 60-second connection drops. This phase validates both HTTP/1.1 and HTTP/2 WebSocket scenarios.

**Delivers:**
- WebSocket echo server example
- WebSocket integration test (bidirectional messaging)
- Kestrel timeout configuration (`KeepAliveTimeout`, `MaxConcurrentUpgradedConnections`)
- Documentation for WebSocket testing and troubleshooting

**Addresses:**
- WebSocket upgrade handling (table stakes feature)
- Long-lived connection support (table stakes feature)

**Avoids:**
- WebSocket timeout mismatch (Pitfall 2) — configures keep-alive timeouts
- HTTP/2 WebSocket method mismatch (Pitfall 3) — tests both protocols

**Uses:**
- YARP's built-in WebSocket support (zero code changes)
- Kestrel timeout configuration

**Implements:**
- Timeout configuration in `Program.cs`
- Example WebSocket backend for testing
- Integration test for WebSocket proxying

### Phase 3: Testing & Validation
**Rationale:** HTTP/2 is not universally "faster"—local dev scenarios may see performance regression from HPACK compression overhead. This phase benchmarks actual performance and validates that HTTP/2 benefits outweigh costs. Also tests edge cases: protocol downgrades, concurrent connections, mixed HTTP/1.1 and HTTP/2 backends.

**Delivers:**
- BenchmarkDotNet tests comparing HTTP/1.1 vs HTTP/2 performance
- Load test with 50+ concurrent requests
- SignalR chat example demonstrating real-world WebSocket usage
- Protocol validation middleware (fail-fast for HTTP/2-only features like gRPC)

**Addresses:**
- Performance validation (ensure HTTP/2 doesn't degrade performance)
- Protocol downgrade detection (should-have feature)

**Avoids:**
- HTTP/2 performance regression (Pitfall 5) — benchmarks before declaring complete
- Silent protocol downgrade breaking gRPC (Pitfall 6) — adds strict version policy for HTTP/2-only features
- Connection pooling exhaustion (Pitfall 7) — load tests concurrent requests

**Uses:**
- BenchmarkDotNet for performance testing
- SignalR client for integration testing

**Implements:**
- Performance benchmarks
- Load test scenarios
- SignalR example backend

### Phase 4: Documentation & Examples
**Rationale:** Developers need clear guidance on when to use HTTP/2, how to test protocol negotiation, and how to troubleshoot WebSocket issues. Examples demonstrate working HTTP/2 backend, WebSocket echo server, and SignalR chat.

**Delivers:**
- Updated README with HTTP/2 and WebSocket support documentation
- Troubleshooting guide for protocol issues
- Example projects: HTTP/2 backend, WebSocket echo, SignalR chat
- CLI help text updates (`portless proxy start --protocols Http1AndHttp2`)

**Addresses:**
- Documentation gaps
- User onboarding for new features

**Uses:**
- Existing Spectre.Console.Cli framework
- Example backends for demonstration

### Phase Ordering Rationale

- **Phase 1 first** because HTTP/2 is infrastructure—WebSocket testing (Phase 2) needs HTTP/2 working correctly. Protocol negotiation must be verified before building features that depend on it.
- **Phase 2 second** because WebSocket support is the primary user-facing feature. Timeouts must be configured before real-time apps work reliably.
- **Phase 3 third** because performance validation prevents premature optimization. Must benchmark before assuming HTTP/2 is "faster."
- **Phase 4 last** because documentation depends on working implementations. Examples only work after phases 1-3 are complete.

This ordering avoids critical pitfalls:
- Silent protocol downgrades (Phase 1 adds logging)
- WebSocket timeout drops (Phase 2 configures timeouts)
- Performance regression (Phase 3 benchmarks before release)

### Research Flags

**Phases likely needing deeper research during planning:**
- **Phase 2:** WebSocket timeout configuration is YARP-specific. Found general reverse proxy patterns, but may need YARP-specific documentation during implementation. Consider `/gsd:research-phase` if timeouts prove tricky.
- **Phase 3:** Performance characteristics of HTTP/2 in local dev scenarios are poorly documented. Most benchmarks focus on high-latency networks. May need real-world testing during implementation.

**Phases with standard patterns (skip research-phase):**
- **Phase 1:** HTTP/2 configuration is well-documented in Microsoft Learn docs. High-confidence patterns exist.
- **Phase 4:** Documentation and examples follow standard patterns. No research needed.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | All claims verified with official Microsoft documentation. YARP 2.3.0 confirmed to include HTTP/2 and WebSocket support. Current codebase inspected to verify upgrade path. |
| Features | MEDIUM | Table stakes features verified via official docs. Differentiators based on competitive analysis—Portless (Node.js) capabilities inferred but not directly verified. Some LOW confidence sources (third-party blogs). |
| Architecture | HIGH | Architecture based on YARP's documented behavior for HTTP/2 and WebSocket proxying. Verified current codebase structure. Minimal changes required—low risk. |
| Pitfalls | MEDIUM | All pitfalls verified with official Microsoft docs or YARP GitHub issues. Prevention strategies based on documented patterns. Performance regression (Pitfall 5) inferred from HTTP/2 characteristics—needs benchmark validation. |

**Overall confidence:** HIGH

### Gaps to Address

- **YARP-specific timeout configuration for WebSocket:** Found general reverse proxy patterns (Nginx, Envoy), but YARP-specific documentation is sparse. May need to test timeout values empirically during Phase 2 implementation.
- **Performance characteristics of HTTP/2 in local dev:** Research indicates HTTP/2 may be slower than HTTP/1.1 for low-latency, low-concurrency scenarios (typical local dev). This needs benchmark validation in Phase 3 before declaring HTTP/2 "production-ready."
- **Portless (Node.js) current capabilities:** Assumed limited HTTP/2/WebSocket support based on Node.js http-proxy limitations, but not directly verified. This gap doesn't affect implementation—only competitive positioning.
- **Windows-specific HTTP/2 behavior:** Most documentation is Linux-focused. Portless.NET targets Windows as a differentiator, so Windows-specific testing is required during Phase 1.

## Sources

### Primary (HIGH confidence)
- [YARP Proxying WebSockets and SPDY](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/websockets?view=aspnetcore-10.0) — HTTP/2 WebSocket support, Kestrel as only server supporting HTTP/2 WebSocket, automatic header handling (verified 2026-01-23)
- [YARP Proxying gRPC](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/yarp/grpc?view=aspnetcore-10.0) — HTTP/2 configuration requirements, protocol negotiation, TLS vs Prior Knowledge (verified 2026-01-23)
- [Configure Kestrel Endpoints](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0) — `HttpProtocols.Http1AndHttp2` configuration, ALPN negotiation (verified 2025)
- [Microsoft YARP Diagnostics Guide](https://learn.microsoft.com/zh-cn/aspnet/core/fundamentals/servers/yarp/diagnosing-yarp-issues) — Published June 20, 2025
- [Microsoft YARP Timeouts Documentation](https://learn.microsoft.com/zh-cn/aspnet/core/fundamentals/servers/yarp/timeouts) — Published January 22, 2026
- [RFC 8441: Bootstrapping WebSockets with HTTP/2](https://datatracker.ietf.org/doc/html/rfc8441) — Extended CONNECT protocol specification

### Secondary (MEDIUM confidence)
- [YARP转发请求配置：ForwarderRequestConfig参数设置](https://m.blog.csdn.net/gitblog_01197/article/details/151086578) — `ForwarderRequestConfig` controls outbound HTTP version, defaults to HTTP/2 with `RequestVersionOrLower` (published 2025-09-01)
- [Azure SignalR with Reverse Proxies](https://learn.microsoft.com/en-us/azure/azure-signalr/signalr-howto-work-with-reverse-proxy) — HOST header rewriting, `ClientEndpoint` configuration (updated 2025-05)
- [ASP.NET Core Kestrel HTTP/2 Documentation](https://learn.microsoft.com/zh-cn/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0) — Protocol configuration, TLS requirements
- [.NET HTTP/2 WebSocket Support Announcement](https://devblogs.microsoft.com/aspnet/asp-net-core-updates-in-net-7/) — .NET 7 HTTP/2 WebSocket support details

### Tertiary (LOW confidence)
- [HTTP/2 Multiplexing Request Bursts - Lucidchart Case Study](https://engineering.lucidsoftware.com/2016/06/02/http2-at-lucid/) — Real-world performance issues with HTTP/2 multiplexing (third-party blog, needs validation)
- [HTTP/2 Protocol Conversion Vulnerabilities - USENIX Security 2022](https://www.usenix.org/conference/usenixsecurity22/presentation/merget) — Academic paper on HTTP/2 proxy conversion security issues (needs verification)
- [103 Early Hints vs HTTP/2 Server Push](https://developer.chrome.com/blog/early-hints/) — Chrome documentation on Early Hints adoption (limited browser support)
- [Envoy HTTP Upgrades Documentation](https://www.envoyproxy.io/docs/envoy/latest/intro/arch_overview/http/upgrades) — WebSocket over HTTP/2 transformation patterns (not YARP-specific)
- [Nginx WebSocket Proxying Guide](https://nginx.org/en/docs/http/websocket.html) — HTTP/1.1 WebSocket proxy configuration (not YARP-specific)

### Verification Methodology
- Cross-referenced Microsoft Learn documentation (official, up-to-date)
- Verified current codebase has YARP 2.3.0 installed
- Confirmed .NET 10 includes all required HTTP/2 features
- Checked existing Kestrel configuration for upgrade path
- No conflicting information found across primary sources

---
*Research completed: 2026-02-22*
*Ready for roadmap: yes*
