# Roadmap: Portless.NET

## Overview

Portless.NET delivers stable `.localhost` URLs for Windows .NET development through a reverse proxy with automatic port management. v1.0 established HTTP/1.1 proxying, CLI commands, and route persistence. v1.1 adds advanced protocol support (HTTP/2 and WebSockets) to enable real-time applications and improved performance. The roadmap evolves from MVP protocol support to advanced features, with each phase delivering complete, verifiable capabilities.

## Milestones

- ✅ **v1.0 MVP** - Phases 1-8 (shipped 2026-02-21)
- ✅ **v1.1 Advanced Protocols** - Phases 9-12 (shipped 2026-02-22)
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

<details>
<summary>✅ v1.1 Advanced Protocols (Phases 9-12) - SHIPPED 2026-02-22</summary>

**See:** [Full milestone details](.planning/milestones/v1.1-ROADMAP.md)

- [x] **Phase 9: HTTP/2 Baseline** (1/1 plans) — Soporte HTTP/2 con Kestrel y detección de protocolo
- [x] **Phase 10: WebSocket Proxy** (1/1 plans) — Proxy transparente para conexiones WebSocket HTTP/1.1 y HTTP/2
- [x] **Phase 11: SignalR Integration** (3/3 plans) — Ejemplo de chat SignalR y pruebas de integración
- [x] **Phase 12: Documentation** (5/5 plans) — Documentación completa de protocolos avanzados

**Delivered:**
- HTTP/2 support con Kestrel (ALPN negotiation, protocol logging)
- WebSocket transparent proxy (HTTP/1.1 upgrade + HTTP/2 Extended CONNECT)
- SignalR chat example con browser y console clients
- Integration tests para HTTP/2, WebSocket y SignalR
- Documentación completa (troubleshooting guides, migration guide, protocol testing)
- 3 nuevos ejemplos (WebSocketEchoServer, SignalRChat, HTTP/2 tests)
- 8 nuevos tests de integración

</details>

### 📋 v1.2 Platform Expansion (Planned)

**Milestone Goal:** HTTPS support con certificados automáticos y validación cross-platform

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
| 9. HTTP/2 Baseline | v1.1 | 1/1 | Complete | 2026-02-22 |
| 10. WebSocket Proxy | v1.1 | 1/1 | Complete | 2026-02-22 |
| 11. SignalR Integration | v1.1 | 3/3 | Complete | 2026-02-22 |
| 12. Documentation | 6/6 | Complete    | 2026-02-22 | 2026-02-22 |

**For detailed milestone information, see:**
- [milestones/v1.0-ROADMAP.md](.planning/milestones/v1.0-ROADMAP.md) - MVP (Phases 1-8)
- [milestones/v1.1-ROADMAP.md](.planning/milestones/v1.1-ROADMAP.md) - Advanced Protocols (Phases 9-12)
