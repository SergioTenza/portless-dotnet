# Phase 10: WebSocket Proxy - Plan

**Phase:** 10 of 12 (v1.1 Advanced Protocols)
**Created:** 2026-02-22
**Status:** Draft

## Goal Verification

**Goal:** Transparent WebSocket proxy support for both HTTP/1.1 and HTTP/2

**What "Transparent" Means:**
- Client → Proxy → Backend WebSocket flow works without client awareness
- No special configuration required on client or backend
- Proxy handles protocol upgrade automatically
- Connection stays alive for extended periods

## Dependency Analysis

**Depends on:** Phase 9 (HTTP/2 Baseline)
- ✓ HTTP/2 is enabled in Kestrel
- ✓ Protocol logging helps diagnose WebSocket issues
- ✓ X-Forwarded headers preserve client information
- ✓ Integration test infrastructure available

**Provides for Phase 11:**
- WebSocket proxy capability for SignalR connections
- Stable WebSocket connections for real-time messaging
- Connection timeout configuration for long-lived connections

## Requirements Coverage

From REQUIREMENTS.md:

- [ ] **WS-01**: WebSocket transparent proxy para HTTP/1.1 upgrade (101 Switching Protocols)
- [ ] **WS-02**: WebSocket transparent proxy para HTTP/2 WebSocket (RFC 8441 Extended CONNECT)
- [ ] **WS-03**: Kestrel timeout configuration (`KeepAliveTimeout`, `MaxConcurrentUpgradedConnections`)
- [ ] **WS-04**: Integration test para WebSocket bidirectional messaging
- [ ] **WS-05**: WebSocket echo server example para testing

## Success Criteria

**What must be TRUE for this phase to succeed:**

1. ✅ YARP supports WebSocket proxy out of the box (no additional configuration needed for HTTP/1.1)
2. ✅ HTTP/2 WebSocket (RFC 8441 Extended CONNECT) works through YARP with HTTP/2 enabled
3. ✅ WebSocket connections remain stable beyond 60 seconds (timeout configuration)
4. ✅ Bidirectional messaging works end-to-end through proxy
5. ✅ Echo server example demonstrates WebSocket functionality
6. ✅ Integration test verifies both HTTP/1.1 and HTTP/2 WebSocket scenarios

## Research Findings

**YARP WebSocket Support:**
- YARP supports WebSocket proxy by default with `WebSocketsTransport`
- For HTTP/1.1: YARP handles `101 Switching Protocols` automatically
- For HTTP/2: YARP supports RFC 8441 Extended CONNECT when HTTP/2 is enabled
- No special configuration needed - YARP detects WebSocket upgrade requests

**Kestrel Configuration for WebSockets:**
```csharp
// For long-lived WebSocket connections
options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10); // Default is 2 minutes
options.Limits.MaxConcurrentUpgradedConnections = 100; // Default is 100, increase if needed
```

**HTTP/2 WebSocket (RFC 8441):**
- Uses `CONNECT` method with `protocol: "websocket"` header
- Requires HTTP/2 to be enabled (already done in Phase 9)
- YARP handles Extended CONNECT automatically when HTTP/2 is enabled

**Testing Strategy:**
- Create echo server example that accepts WebSocket connections
- Test both HTTP/1.1 upgrade (ws://) and HTTP/2 WebSocket (h2c prior knowledge)
- Verify bidirectional messaging through proxy
- Test connection stability beyond default timeouts

## Plans

### Plan 10-01: Enable WebSocket Support in YARP and Configure Timeouts

**Goal:** Verify YARP WebSocket proxy works and configure Kestrel for long-lived connections

**Success Criteria:**
1. YARP accepts WebSocket upgrade requests (HTTP/1.1)
2. YARP accepts HTTP/2 WebSocket Extended CONNECT requests
3. Kestrel configured with extended KeepAliveTimeout (10 minutes)
4. Kestrel MaxConcurrentUpgradedConnections set appropriately
5. Integration test verifies WebSocket proxy works end-to-end

**Tasks:**
1. **Verify YARP WebSocket support** - Confirm YARP proxies WebSocket connections without additional configuration
2. **Configure Kestrel timeouts** - Set KeepAliveTimeout and MaxConcurrentUpgradedConnections in Program.cs
3. **Create WebSocket echo server** - Simple example app for testing WebSocket connections
4. **Create WebSocket integration test** - Test both HTTP/1.1 and HTTP/2 WebSocket scenarios

**Estimated Tasks:** 4
**Estimated Duration:** 12-15 minutes

**Key Files:**
- Modify: `Portless.Proxy/Program.cs` (Kestrel configuration)
- Create: `Examples/WebSocketEchoServer/` (example WebSocket server)
- Create: `Portless.Tests/WebSocketIntegrationTests.cs` (integration tests)

**Dependencies:**
- None beyond Phase 9 completion

**Acceptance Criteria:**
- [ ] WebSocket connections successfully proxy through HTTP/1.1 upgrade (101 Switching Protocols)
- [ ] WebSocket connections successfully proxy through HTTP/2 WebSocket (RFC 8441 Extended CONNECT)
- [ ] Connections remain stable beyond 60 seconds of inactivity
- [ ] Integration test verifies bidirectional messaging
- [ ] Echo server example demonstrates functionality

## Phase Completion Checklist

**When all plans are complete:**
- [ ] All requirements (WS-01 through WS-05) are satisfied
- [ ] Integration tests pass (WebSocket HTTP/1.1 and HTTP/2)
- [ ] Echo server example is documented and working
- [ ] Phase 10 summary documents WebSocket capability
- [ ] Phase 11 has verified WebSocket proxy is ready for SignalR

## Known Risks and Mitigations

**Risk 1: WebSocket connections timeout after 60 seconds**
- **Mitigation:** Configure Kestrel KeepAliveTimeout to 10 minutes (WS-03)
- **Verification:** Integration test with 90-second idle connection

**Risk 2: HTTP/2 WebSocket doesn't work without HTTPS**
- **Mitigation:** Test with curl --http2-prior-knowledge for h2c scenario
- **Verification:** Integration test covers both HTTP/1.1 and HTTP/2 scenarios

**Risk 3: MaxConcurrentUpgradedConnections limit reached**
- **Mitigation:** Increase limit from 100 to 1000 for development scenarios
- **Verification:** Document limit in troubleshooting guide

## Notes

- YARP handles WebSocket proxy automatically - no special routing configuration needed
- HTTP/2 WebSocket requires prior knowledge without HTTPS (curl --http2-prior-knowledge)
- Echo server example should be simple enough to run locally without dependencies
- Integration tests should use WebApplicationFactory from Phase 8

---
*Phase: 10-websocket-proxy*
*Plan: 01*
*Status: Draft*
