# Requirements: Portless.NET

**Defined:** 2026-02-22
**Core Value:** URLs estables y predecibles para desarrollo local

## v1.1 Requirements

Requirements for HTTP/2 and WebSocket support. Each maps to roadmap phases.

### Protocol Foundation

- [x] **PROTO-01**: Kestrel configurado con `HttpProtocols.Http1AndHttp2` para habilitar HTTP/2
- [x] **PROTO-02**: Protocol logging middleware para detectar silent downgrades (HTTP/2 → HTTP/1.1)
- [x] **PROTO-03**: Per-route protocol configuration (HTTP/1.1, HTTP/2, HTTP/3) en cluster metadata
- [x] **PROTO-04**: Integration test que verifique HTTP/2 negotiation con `curl -v --http2`
- [x] **PROTO-05**: X-Forwarded headers transform configurado para backward compatibility

### WebSocket Support

- [x] **WS-01**: WebSocket transparent proxy para HTTP/1.1 upgrade (101 Switching Protocols)
- [x] **WS-02**: WebSocket transparent proxy para HTTP/2 WebSocket (RFC 8441 Extended CONNECT)
- [x] **WS-03**: Kestrel timeout configuration (`KeepAliveTimeout`, `MaxConcurrentUpgradedConnections`)
- [x] **WS-04**: Integration test para WebSocket bidirectional messaging
- [x] **WS-05**: WebSocket echo server example para testing

### Real-Time Examples

- [ ] **REAL-01**: SignalR chat example demostrando real-time messaging a través del proxy
- [ ] **REAL-02**: SignalR integration test verificando conexión WebSocket
- [ ] **REAL-03**: Documentation para SignalR troubleshooting

### Documentation

- [x] **DOC-01**: README actualizado con HTTP/2 y WebSocket support section
- [ ] **DOC-02**: Troubleshooting guide para protocol issues (silent downgrade, timeouts)
- [ ] **DOC-03**: CLI help text updates (`--protocols` flag documentation)
- [ ] **DOC-04**: Protocol testing guide (curl commands, browser DevTools)

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### Advanced Features

- **ADV-01**: HTTP/3 (QUIC) support para TCP head-of-line blocking elimination
- **ADV-02**: 103 Early Hints support para preloading anticipado
- **ADV-03**: Automatic heartbeat para WebSocket keep-alive
- **ADV-04**: Connection pooling metrics para observability

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| HTTPS support | Requiere certificados SSL y configuración TLS — defer to v1.2+ |
| Cross-platform macOS/Linux | Validación prioritaria pero deferida — Windows focus mantenido |
| HTTP/2 Server Push | Siendo deprecado en favor de 103 Early Hints — poor browser support |
| gRPC support | Depende de HTTP/2 pero requiere configuración adicional — defer to v1.2+ |
| Load balancing | Single destination por hostname es suficiente para local dev |
| Rate limiting | No necesario para desarrollo local |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| PROTO-01 | Phase 9 | Complete |
| PROTO-02 | Phase 9 | Complete |
| PROTO-03 | Phase 9 | Complete |
| PROTO-04 | Phase 9 | Complete |
| PROTO-05 | Phase 9 | Complete |
| WS-01 | Phase 10 | Complete |
| WS-02 | Phase 10 | Complete |
| WS-03 | Phase 10 | Complete |
| WS-04 | Phase 10 | Complete |
| WS-05 | Phase 10 | Complete |
| REAL-01 | Phase 11 | Pending |
| REAL-02 | Phase 11 | Pending |
| REAL-03 | Phase 11 | Pending |
| DOC-01 | Phase 12 | Complete |
| DOC-02 | Phase 12 | Pending |
| DOC-03 | Phase 12 | Pending |
| DOC-04 | Phase 12 | Pending |

**Coverage:**
- v1.1 requirements: 16 total
- Mapped to phases: 16
- Unmapped: 0 ✓

---
*Requirements defined: 2026-02-22*
*Last updated: 2026-02-22 after roadmap creation*
