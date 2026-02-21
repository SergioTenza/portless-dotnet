# Roadmap: Portless.NET

## Overview

Portless.NET es un port de la herramienta Node.js Portless a .NET 10, proporcionando URLs estables y nombradas para desarrollo local. El roadmap comienza con el proxy HTTP core (YARP ya configurado), progresando através de gestión de rutas, CLI commands, asignación automática de puertos, gestión de procesos, integración con el ecosistema .NET, y validación cross-platform. Cada fase entrega una capacidad completa y verificable que acumula sobre las anteriores.

## Phases

**Phase Numbering:**
- Integer phases (1, 2, 3): Planned milestone work
- Decimal phases (2.1, 2.2): Urgent insertions (marked with INSERTED)

Decimal phases appear between their surrounding integers in numeric order.

- [ ] **Phase 1: Proxy Core** - HTTP proxy funcional con routing por hostname
- [ ] **Phase 2: Route Persistence** - Gestión de rutas con persistencia en archivo JSON
- [ ] **Phase 3: CLI Commands** - Comandos básicos para controlar proxy y ejecutar apps
- [ ] **Phase 4: Port Management** - Detección y asignación automática de puertos
- [x] **Phase 5: Process Management** - Spawning, tracking y cleanup de procesos (completed 2026-02-21)
- [ ] **Phase 6: .NET Integration** - Empaquetado como dotnet tool y ejemplos de integración
- [ ] **Phase 7: Cross-Platform** - Validación de compatibilidad Windows/macOS/Linux

## Phase Details

### Phase 1: Proxy Core
**Goal**: Proxy HTTP funcional que acepta requests en puerto 1355 y los routea al backend correcto basado en Host header
**Depends on**: Nothing (YARP ya está configurado en el proyecto)
**Requirements**: PROXY-01, PROXY-02, PROXY-03, PROXY-04
**Success Criteria** (what must be TRUE):
  1. Proxy acepta conexiones HTTP en puerto 1355 (u otro puerto configurado)
  2. Proxy routea requests a diferentes backends basado en Host header
  3. Proxy forwarda requests correctamente al backend destino
  4. Proxy retorna respuestas del backend al cliente sin modificaciones
**Plans**: 4 plans

Plans:
- [x] 01-01: Configure port binding, request logging, and enhanced API validation (Wave 1)
- [x] 01-02: Implement InMemoryConfigProvider, routing helpers, and manual testing verification (Wave 2)
- [x] 01-03: Fix middleware ordering to enable request logging for proxied requests (Wave 1) - GAP CLOSURE
- [ ] 01-04: Create automated integration tests for YARP routing behavior (Wave 1) - GAP CLOSURE

### Phase 2: Route Persistence
**Goal**: Sistema persiste rutas en archivo JSON con file locking para concurrencia y hot-reload
**Depends on**: Phase 1
**Requirements**: ROUTE-01, ROUTE-02, ROUTE-03, ROUTE-04
**Success Criteria** (what must be TRUE):
  1. Rutas se guardan en ~/.portless/routes.json y persisten entre restarts
  2. Múltiples procesos pueden leer/escribir rutas simultáneamente sin corrupción
  3. Rutas de procesos terminados se limpian automáticamente (verificación de PIDs)
  4. Proxy recarga configuración sin restart cuando cambia archivo de rutas
**Plans**: 3 plans

Plans:
- [x] 02-01: Core persistence layer with RouteInfo model, StateDirectoryProvider, and RouteStore with file locking (Wave 1)
- [x] 02-02: Background cleanup service and hot-reload integration with FileSystemWatcher (Wave 1)
- [x] 02-03: Comprehensive testing suite for persistence, cleanup, and hot-reload functionality (Wave 1) - PENDING MANUAL VERIFICATION

### Phase 3: CLI Commands
**Goal**: CLI completa con comandos para iniciar/detener proxy, ejecutar apps, y listar rutas activas
**Depends on**: Phase 2
**Requirements**: CLI-01, CLI-02, CLI-03, CLI-04, CLI-05
**Success Criteria** (what must be TRUE):
  1. Usuario puede iniciar proxy con `portless proxy start` en puerto 1355
  2. Usuario puede detener proxy limpiamente con `portless proxy stop`
  3. Usuario puede ejecutar app con URL nombrada usando `portless <name> <command>`
  4. Usuario puede ver apps activas y mapeo hostname→puerto con `portless list`
  5. CLI muestra errores claros con mensajes accionables (no excepciones crudas)
**Plans**: 3 plans

Plans:
- [ ] 03-01: Proxy lifecycle management commands (start/stop/status) with PID tracking (Wave 1)
- [ ] 03-02: Route listing command with TTY-aware output (table vs JSON) (Wave 1)
- [ ] 03-03: Run command with automatic port allocation and background process execution (Wave 2)

### Phase 4: Port Management
**Goal**: Sistema detecta puertos libres automáticamente y los asigna a procesos
**Depends on**: Phase 3
**Requirements**: PORT-01, PORT-02, PORT-03, PORT-04
**Success Criteria** (what must be TRUE):
  1. Sistema detecta automáticamente puerto libre en rango 4000-4999
  2. Sistema asigna puerto único a cada app sin conflictos
  3. Sistema inyecta variable PORT en el comando ejecutado
  4. Sistema libera puerto cuando proceso termina (para reutilización)
**Plans**: 2 plans

Plans:
- [ ] 04-01: Port pooling and lifecycle management with PID tracking (Wave 1)
- [ ] 04-02: Cross-platform PORT injection and automatic port release (Wave 2)

### Phase 5: Process Management
**Goal**: Sistema spawnea procesos, trackea PIDs, y limpia rutas cuando terminan
**Depends on**: Phase 4
**Requirements**: PROC-01, PROC-02, PROC-03, PROC-04
**Success Criteria** (what must be TRUE):
  1. Sistema spawnea el comando especificado con variable PORT inyectada
  2. Sistema trackea PID del proceso para monitorear su estado
  3. Sistema limpia automáticamente la ruta cuando proceso termina
  4. Sistema forwarda signals (SIGTERM, SIGINT) al proceso para shutdown limpio
**Plans**: 2 plans

Plans:
- [ ] 05-01: Process spawning and health monitoring with PORT injection (Wave 1)
- [ ] 05-02: Signal forwarding and graceful shutdown coordination (Wave 2)

### Phase 6: .NET Integration
**Goal**: Empaquetado como dotnet tool global con ejemplos de integración para proyectos .NET
**Depends on**: Phase 5
**Requirements**: DOTNET-01, DOTNET-02, DOTNET-03
**Success Criteria** (what must be TRUE):
  1. Usuario puede instalar como dotnet tool global con `dotnet tool install`
  2. Ejemplos muestran cómo integrar con launchSettings.json de proyectos .NET
  3. Ejemplos muestran cómo integrar con appsettings.json para configuración
**Plans**: 3 plans

Plans:
- [ ] 06-01: Configure PackAsTool settings and validate tool packaging with Native AOT (Wave 1)
- [ ] 06-02: Create integration examples (WebApi, BlazorApp, WorkerService, ConsoleApp) with PORT variable usage (Wave 1)
- [ ] 06-03: Create installation scripts and comprehensive documentation (tutorials + integration guides) (Wave 2)

### Phase 7: Cross-Platform
**Goal**: Validación de que Portless.NET funciona idénticamente en Windows, macOS y Linux
**Depends on**: Phase 6
**Requirements**: XPLAT-01, XPLAT-02, XPLAT-03
**Success Criteria** (what must be TRUE):
  1. Proxy funciona correctamente en Windows 10+ con comportamiento idéntico
  2. Proxy funciona correctamente en macOS 12+ con comportamiento idéntico
  3. Proxy funciona correctamente en Linux (Ubuntu 20.04+, Debian 11+) con comportamiento idéntico
**Plans**: TBD

Plans:
- [ ] 07-01: [Brief description]
- [ ] 07-02: [Brief description]

## Progress

**Execution Order:**
Phases execute in numeric order: 1 → 2 → 3 → 4 → 5 → 6 → 7

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Proxy Core | 4/4 | Complete | 2026-02-19 |
| 2. Route Persistence | 3/3 | Complete | 2026-02-19 |
| 3. CLI Commands | 3/3 | Complete | 2026-02-19 |
| 4. Port Management | 0/2 | Not started | - |
| 5. Process Management | 2/2 | Complete   | 2026-02-21 |
| 6. .NET Integration | 1/3 | In Progress|  |
| 7. Cross-Platform | 0/2 | Not started | - |

### Phase 8: Integration Tests Automation for .NET Tool

**Goal:** Sistema de pruebas de integración automatizadas para validar que Portless.NET funcione correctamente como dotnet tool instalado globalmente, incluyendo validación de comandos CLI, proxy YARP, gestión de procesos, y comportamiento cross-platform.
**Depends on:** Phase 5 (Process Management completed)
**Requirements:** TEST-01, TEST-02, TEST-03, TEST-04, TEST-05
**Success Criteria** (what must be TRUE):
  1. Tests de integración validan comandos CLI (proxy start/stop, list, run)
  2. Tests E2E validan instalación de tool (dotnet tool install/uninstall)
  3. Tests de integración validan routing YARP con múltiples hostnames
  4. Tests de persistencia validan file locking, cleanup, y hot-reload
  5. Tests corren en Windows y Linux sin modificaciones
**Plans:** 1/3 plans executed

Plans:
- [ ] 08-01: Integration test project for CLI commands, proxy process management, and port allocation (Wave 1)
- [ ] 08-02: E2E test project for full tool installation, CLI invocation, and cross-platform validation (Wave 1)
- [ ] 08-03: Enhanced YARP proxy and route persistence integration tests with README documentation (Wave 2)
