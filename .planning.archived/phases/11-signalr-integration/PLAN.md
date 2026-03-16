# Phase 11: SignalR Integration - Plan

**Phase:** 11 of 12 (v1.1 Advanced Protocols)
**Created:** 2026-02-22
**Status:** Draft

## Goal Verification

**Goal:** Real-time communication example with SignalR over WebSocket through the proxy

**What "Real-Time Communication" Means:**
- Server can push messages to clients instantly
- Bidirectional messaging (client ↔ server) works through proxy
- Multiple clients can connect simultaneously
- Connections stay alive for extended periods
- Message delivery is reliable through proxy

## Dependency Analysis

**Depends on:** Phase 10 (WebSocket Proxy)
- ✓ WebSocket proxy support is working
- ✓ HTTP/1.1 and HTTP/2 WebSocket scenarios verified
- ✓ Connection timeout configuration in place
- ✓ Echo server demonstrates WebSocket capability

**Provides for Phase 12:**
- Real-time example for documentation
- SignalR configuration best practices
- Troubleshooting scenarios for real-time apps

## Requirements Coverage

From REQUIREMENTS.md:

- [ ] **REAL-01**: SignalR chat example demostrando real-time messaging a través del proxy
- [ ] **REAL-02**: SignalR integration test verificando conexión WebSocket
- [ ] **REAL-03**: Documentation para SignalR troubleshooting

## Success Criteria

**What must be TRUE for this phase to succeed:**

1. ✅ SignalR chat example app connects successfully through proxy
2. ✅ Real-time messages flow bidirectionally between clients through proxy
3. ✅ Multiple clients can connect and receive broadcast messages
4. ✅ Integration test verifies SignalR WebSocket connection
5. ✅ Documentation covers SignalR troubleshooting and configuration

## Research Findings

**SignalR and YARP:**
- SignalR uses WebSocket as primary transport (falls back to Server-Sent Events, Long Polling)
- With Phase 10 WebSocket support, SignalR should work automatically through proxy
- No special YARP configuration needed for SignalR
- SignalR Hub handles connection management, message broadcasting

**SignalR Chat Example Architecture:**
```
Client (Browser/Console) → Proxy (.localhost:1355) → SignalR Server (:4000)
                         ↓ WebSocket connection established
                         ↓ SignalR messages flow bidirectionally
                         ↓ Broadcast messages reach all connected clients
```

**SignalR Dependencies:**
```xml
<PackageReference Include="Microsoft.AspNetCore.SignalR" Version="..." />
<!-- For console clients: -->
<PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="..." />
```

**Testing Strategy:**
- Create simple SignalR chat hub with broadcast functionality
- Test with browser client (JavaScript) and console client (.NET)
- Verify multiple clients can connect and receive messages
- Integration test uses SignalR client to verify connection through proxy

## Plans

### Plan 11-01: Create SignalR Chat Example

**Goal:** Build and test SignalR chat example that demonstrates real-time messaging through proxy

**Success Criteria:**
1. SignalR chat server app created with broadcast hub
2. Browser client can connect and send/receive messages through proxy
3. Console client can connect and send/receive messages through proxy
4. Multiple clients receive broadcast messages simultaneously
5. README documents how to run example through proxy

**Tasks:**
1. **Create SignalR chat server** - ASP.NET Core app with ChatHub and index.html
2. **Create console client example** - .NET console app using SignalR Client
3. **Test chat through proxy** - Verify browser and console clients work through .localhost URL
4. **Document example usage** - README with proxy setup and troubleshooting

**Estimated Tasks:** 4
**Estimated Duration:** 10-12 minutes

**Key Files:**
- Create: `Examples/SignalRChat/` (SignalR chat server)
- Create: `Examples/SignalRChat.Client/` (console client)
- Create: `Examples/SignalRChat/README.md` (documentation)

**Dependencies:**
- Phase 10 WebSocket support must be working

**Acceptance Criteria:**
- [ ] SignalR chat server runs on assigned PORT
- [ ] Browser client connects to `http://chatsignalr.localhost:1355` and sends/receives messages
- [ ] Console client connects and sends/receives messages
- [ ] Multiple clients see broadcast messages
- [ ] README documents proxy setup and common issues

### Plan 11-02: Create SignalR Integration Test

**Goal:** Integration test that verifies SignalR WebSocket connection through proxy

**Success Criteria:**
1. Integration test uses SignalR Client to connect through proxy
2. Test verifies connection establishment
3. Test sends message and receives echo/broadcast
4. Test covers both HTTP/1.1 and HTTP/2 scenarios
5. Test documents SignalR integration pattern

**Tasks:**
1. **Create SignalR integration test** - Use SignalR Client with WebApplicationFactory
2. **Test connection through proxy** - Verify WebSocket negotiation and message flow
3. **Test bidirectional messaging** - Send message, verify response
4. **Document test findings** - Add comments explaining SignalR integration

**Estimated Tasks:** 4
**Estimated Duration:** 8-10 minutes

**Key Files:**
- Create: `Portless.Tests/SignalRIntegrationTests.cs`
- Modify: `Portless.Tests/Portless.Tests.csproj` (add SignalR Client package)

**Dependencies:**
- Plan 11-01 must be complete (SignalR chat server exists)

**Acceptance Criteria:**
- [ ] Integration test connects to SignalR hub through proxy
- [ ] Test verifies WebSocket connection established
- [ ] Test sends message and receives response
- [ ] Test documents SignalR integration pattern
- [ ] All tests pass

### Plan 11-03: Create SignalR Troubleshooting Documentation

**Goal:** Documentation covering SignalR setup, configuration, and common issues through proxy

**Success Criteria:**
1. README section documents SignalR support in Portless.NET
2. Troubleshooting guide covers common SignalR issues through proxy
3. Example usage shows proxy configuration for SignalR apps
4. Performance tips documented for real-time scenarios

**Tasks:**
1. **Add SignalR section to main README** - Document SignalR support with examples
2. **Create troubleshooting guide** - Common issues (connection drops, timeouts, negotiation failures)
3. **Document best practices** - Connection management, retry logic, scale considerations
4. **Update examples README** - Link SignalR chat example from main documentation

**Estimated Tasks:** 4
**Estimated Duration:** 8-10 minutes

**Key Files:**
- Modify: `README.md` (add SignalR section)
- Create: `docs/signalr-troubleshooting.md`
- Modify: `Examples/README.md` (add SignalR chat example)

**Dependencies:**
- Plan 11-01 and 11-02 must be complete (examples and tests working)

**Acceptance Criteria:**
- [ ] README documents SignalR support
- [ ] Troubleshooting guide covers common issues
- [ ] Examples README links SignalR chat
- [ ] Documentation includes curl/commands for testing SignalR
- [ ] Performance tips documented

## Phase Completion Checklist

**When all plans are complete:**
- [ ] All requirements (REAL-01 through REAL-03) are satisfied
- [ ] SignalR chat example works through proxy
- [ ] Integration tests pass (SignalR connection verified)
- [ ] Documentation covers SignalR setup and troubleshooting
- [ ] Phase 11 summary documents real-time capability
- [ ] Phase 12 has verified SignalR example is ready for documentation

## Known Risks and Mitigations

**Risk 1: SignalR falls back to Server-Sent Events instead of WebSocket**
- **Mitigation:** Configure SignalR to prefer WebSocket transport
- **Verification:** Test logs show WebSocket connection established

**Risk 2: Multiple clients connection limit reached**
- **Mitigation:** Document MaxConcurrentUpgradedConnections limit from Phase 10
- **Verification:** Test with 5+ concurrent clients

**Risk 3: SignalR negotiation fails through proxy**
- **Mitigation:** Ensure X-Forwarded headers are set (Phase 9)
- **Verification:** Integration test verifies negotiation succeeds

## Notes

- SignalR should work automatically through proxy once WebSocket support is in place
- No special YARP configuration needed for SignalR
- Example should be simple (broadcast chat) to demonstrate core functionality
- Documentation should focus on development scenarios (production SignalR requires Redis/Service Bus backplane)

---
*Phase: 11-signalr-integration*
*Plan: 01, 02, 03*
*Status: Draft*
