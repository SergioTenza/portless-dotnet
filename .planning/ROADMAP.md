# Roadmap: Portless.NET

## Overview

Portless.NET delivers stable `.localhost` URLs for Windows .NET development through a reverse proxy with automatic port management. v1.0 established HTTP/1.1 proxying, CLI commands, and route persistence. v1.1 adds advanced protocol support (HTTP/2 and WebSockets) to enable real-time applications and improved performance. The roadmap evolves from MVP protocol support to advanced features, with each phase delivering complete, verifiable capabilities.

## Milestones

- ✅ **v1.0 MVP** - Phases 1-8 (shipped 2026-02-21)
- 🚧 **v1.1 Advanced Protocols** - Phases 9-12 (in progress)
- 📋 **v1.2 Platform Expansion** - Planned (HTTPS, cross-platform validation)

## Phases

<details>
<summary>✅ v1.0 MVP (Phases 1-8) - SHIPPED 2026-02-21</summary>

**See:** [Full milestone details](.planning/milestones/v1.0-ROADMAP.md)

- [x] **Phase 1: Proxy Core** (4/4 plans) — HTTP proxy funcional con routing por hostname
- [x] **Phase 2: Route Persistence** (3/3 plans) — Gestión de rutas con persistencia en archivo JSON
- [x] **Phase 3: CLI Commands** (3/3 plans) — Comandos básicos para controlar proxy y ejecutar apps
- [x] **Phase 4: Port Management** (2/2 plans) — Detección y asignación automática de puertos
- [x] **Phase 5: Process Management** (2/2 plans) — Spawning, tracking y cleanup de procesos
- [x] **Phase 6: .NET Integration** (3/3 plans) — Empaquetado como dotnet tool y ejemplos de integración
- [x] **Phase 8: Integration Tests** (3/3 plans) — Suite de pruebas automatizadas

**Delivered:**
- HTTP proxy YARP con routing por hostname
- CLI completa (proxy start/stop/status, list, run)
- Asignación automática de puertos (4000-4999)
- Persistencia de rutas con hot-reload
- Gestión de procesos con PORT injection
- Global tool installation (761KB package)
- 4 ejemplos de integración + documentación (3,049 lines)
- 45 tests de integración

</details>

### 🚧 v1.1 Advanced Protocols (In Progress)

**Milestone Goal:** Enable HTTP/2 and WebSocket support for improved performance and real-time communication

#### Phase 9: HTTP/2 Baseline
**Goal**: HTTP/2 protocol support with automatic negotiation and protocol detection
**Depends on**: Phase 8
**Requirements**: PROTO-01, PROTO-02, PROTO-03, PROTO-04, PROTO-05
**Success Criteria** (what must be TRUE):
  1. Proxy accepts HTTP/2 connections when client requests HTTP/2 protocol
  2. Protocol version is logged for each request (HTTP/1.1 or HTTP/2)
  3. Silent protocol downgrades are detected and logged with warnings
  4. X-Forwarded headers correctly preserve original client information
  5. Integration test verifies HTTP/2 negotiation with curl --http2
**Plans**: TBD

#### Phase 10: WebSocket Proxy
**Goal**: Transparent WebSocket proxy support for both HTTP/1.1 and HTTP/2
**Depends on**: Phase 9
**Requirements**: WS-01, WS-02, WS-03, WS-04, WS-05
**Success Criteria** (what must be TRUE):
  1. WebSocket connections successfully proxy through HTTP/1.1 upgrade (101 Switching Protocols)
  2. WebSocket connections successfully proxy through HTTP/2 WebSocket (RFC 8441 Extended CONNECT)
  3. Long-lived WebSocket connections remain stable beyond 60 seconds
  4. Bidirectional messaging works end-to-end through the proxy
  5. Echo server example demonstrates WebSocket functionality
**Plans**: TBD

#### Phase 11: SignalR Integration
**Goal**: Real-time communication example with SignalR over WebSocket
**Depends on**: Phase 10
**Requirements**: REAL-01, REAL-02, REAL-03
**Success Criteria** (what must be TRUE):
  1. SignalR chat example successfully connects through the proxy
  2. Real-time messages flow bidirectionally between clients through the proxy
  3. Integration test verifies SignalR WebSocket connection
  4. Documentation covers SignalR troubleshooting and configuration
**Plans**: TBD

#### Phase 12: Documentation
**Goal**: Complete documentation for HTTP/2 and WebSocket features
**Depends on**: Phase 11
**Requirements**: DOC-01, DOC-02, DOC-03, DOC-04
**Success Criteria** (what must be TRUE):
  1. README documents HTTP/2 and WebSocket support with examples
  2. Troubleshooting guide covers protocol issues (silent downgrade, timeouts)
  3. CLI help text includes --protocols flag documentation
  4. Protocol testing guide provides curl commands and browser DevTools instructions
**Plans**: TBD

## Progress

**Execution Order:**
Phases execute in numeric order: 9 → 10 → 11 → 12

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 1. Proxy Core | v1.0 | 4/4 | Complete | 2026-02-19 |
| 2. Route Persistence | v1.0 | 3/3 | Complete | 2026-02-19 |
| 3. CLI Commands | v1.0 | 3/3 | Complete | 2026-02-19 |
| 4. Port Management | v1.0 | 2/2 | Complete | 2026-02-20 |
| 5. Process Management | v1.0 | 2/2 | Complete | 2026-02-21 |
| 6. .NET Integration | v1.0 | 3/3 | Complete | 2026-02-21 |
| 8. Integration Tests | v1.0 | 3/3 | Complete | 2026-02-21 |
| 9. HTTP/2 Baseline | v1.1 | 0/0 | Not started | - |
| 10. WebSocket Proxy | v1.1 | 0/0 | Not started | - |
| 11. SignalR Integration | v1.1 | 0/0 | Not started | - |
| 12. Documentation | v1.1 | 0/0 | Not started | - |

**For detailed v1.0 phase information, see:** [milestones/v1.0-ROADMAP.md](.planning/milestones/v1.0-ROADMAP.md)
