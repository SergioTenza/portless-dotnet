# Phase 11: SignalR Integration - Context

**Gathered:** 2026-02-22
**Status:** Ready for planning

## Phase Boundary

Real-time communication example with SignalR over WebSocket through the Portless.NET proxy. Deliver a working chat example that demonstrates SignalR works bidirectionally through the proxy, with integration tests and troubleshooting documentation. Scope is demonstration and documentation — not production SignalR deployment.

## Implementation Decisions

### Chat UX and Features

**Pure broadcast only**
- No usernames, no message history, no persistence
- Everyone sees all messages, stateless demo
- Focus on demonstrating SignalR through proxy, not chat features

**Single HTML file browser client**
- Minimal HTML/JS embedded in server project
- No build step needed, simplest possible demo
- Basic message log (messages appear when sent, no scroll polish)

**No persistence**
- Messages only show while connected
- No history for new users joining
- Stateless demonstration of SignalR connectivity

### Client Examples

**Browser + .NET console client**
- Both client types demonstrate cross-platform SignalR through proxy
- Browser client: single HTML file with basic message log
- Console client: minimal .NET app (connect, send, display received)

### Integration Test Scope

**Basic connection test**
- Connect to hub, send message, verify response
- Happy path only — basic verification that SignalR works through proxy
- No comprehensive edge case or load testing

### Claude's Discretion

**Chat UX**
- Styling level — make it presentable without spending time on design
- Error handling — handle the most common failure modes (connection drops, timeouts)
- UI controls — whatever makes testing SignalR easy

**Client Examples**
- Console client URL configuration — make it easy to run but also easy to adapt
- Can be hardcoded or configurable, balance simplicity and adaptability

**Integration Test Scope**
- HTTP/1.1 vs HTTP/2 transport testing — test what provides value for developers
- Concurrent client scenarios — test what demonstrates SignalR capability through proxy
- May include both protocol tests or multiple clients if valuable

**Documentation Focus**
- Troubleshooting scenarios — prioritize what developers need most
- Production deployment guidance — include if helpful for success with SignalR
- Troubleshooting structure — make issues easy to find and resolve
- Can range from connection issues only to comprehensive coverage including performance and deployment

## Specific Ideas

No specific requirements — open to standard SignalR chat example patterns. The goal is demonstrating that SignalR works through Portless.NET proxy, not creating a production chat application.

## Deferred Ideas

None — discussion stayed within phase scope.

---

*Phase: 11-signalr-integration*
*Context gathered: 2026-02-22*
