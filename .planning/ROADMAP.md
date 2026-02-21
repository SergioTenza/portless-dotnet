# Roadmap: Portless.NET

## Overview

Portless.NET es un port de la herramienta Node.js Portless a .NET 10, proporcionando URLs estables y nombradas para desarrollo local. El roadmap comienza con el proxy HTTP core (YARP ya configurado), progresando através de gestión de rutas, CLI commands, asignación automática de puertos, gestión de procesos, integración con el ecosistema .NET, y validación cross-platform. Cada fase entrega una capacidad completa y verificable que acumula sobre las anteriores.

## Milestones

- ✅ **v1.0 MVP** — Phases 1-6, 8 (shipped 2026-02-21)
- 📋 **v1.1 Cross-Platform** — Phase 7 (planned, deferred)

## Phases

<details>
<summary>✅ v1.0 MVP (Phases 1-6, 8) — SHIPPED 2026-02-21</summary>

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

### 📋 v1.1 Cross-Platform (Planned - Deferred)

- [ ] **Phase 7: Cross-Platform** — Validación de compatibilidad Windows/macOS/Linux

**Note:** Phase 7 deferred to prioritize Windows delivery. Cross-platform validation will be addressed when macOS/Linux deployment is needed.

## Progress

| Phase                  | Plans Complete | Status        | Completed    |
| ---------------------- | -------------: | ------------ | ------------ |
| 1. Proxy Core          | 4/4            | Complete      | 2026-02-19   |
| 2. Route Persistence   | 3/3            | Complete      | 2026-02-19   |
| 3. CLI Commands        | 3/3            | Complete      | 2026-02-19   |
| 4. Port Management     | 2/2            | Complete      | 2026-02-20   |
| 5. Process Management  | 2/2            | Complete      | 2026-02-21   |
| 6. .NET Integration    | 3/3            | Complete      | 2026-02-21   |
| 7. Cross-Platform      | 0/2            | Deferred      | Future       |
| 8. Integration Tests   | 3/3            | Complete      | 2026-02-21   |

**For detailed phase information, see:** [milestones/v1.0-ROADMAP.md](.planning/milestones/v1.0-ROADMAP.md)
