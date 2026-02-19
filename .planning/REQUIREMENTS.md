# Requirements: Portless.NET

**Defined:** 2025-02-19
**Core Value:** URLs estables y predecibles para desarrollo local

## v1 Requirements

Requirements para el lanzamiento inicial enfocado en funcionalidad HTTP básica. Cada requerimiento se mapea a fases del roadmap.

### Proxy Core

- [x] **PROXY-01**: Proxy acepta requests HTTP en puerto configurado (default 1355)
- [x] **PROXY-02**: Proxy routea requests basado en Host header
- [x] **PROXY-03**: Proxy forwarda requests al backend correcto
- [x] **PROXY-04**: Proxy retorna respuestas del backend al cliente

### Gestión de Rutas

- [x] **ROUTE-01**: Sistema persiste rutas en archivo JSON (~/.portless/routes.json)
- [x] **ROUTE-02**: Sistema implementa file locking para concurrencia
- [x] **ROUTE-03**: Sistema limpia rutas muertas (verifica PIDs)
- [x] **ROUTE-04**: Sistema soporta hot-reload de configuración

### CLI Commands

- [ ] **CLI-01**: `portless proxy start` inicia proxy en puerto 1355
- [ ] **CLI-02**: `portless proxy stop` detiene proxy limpiamente
- [x] **CLI-03**: `portless <name> <command>` ejecuta app con URL nombrada
- [ ] **CLI-04**: `portless list` muestra apps activas con hostname -> puerto mapping
- [x] **CLI-05**: CLI muestra errores claros y accionables

### Asignación de Puertos

- [ ] **PORT-01**: Sistema detecta puerto libre en rango 4000-4999
- [ ] **PORT-02**: Sistema asigna puerto automáticamente a app
- [ ] **PORT-03**: Sistema inyecta variable PORT en comando ejecutado
- [ ] **PORT-04**: Sistema libera puerto cuando proceso termina

### Process Management

- [ ] **PROC-01**: Sistema spawnea comando con variable PORT
- [ ] **PROC-02**: Sistema trackea PID de proceso
- [ ] **PROC-03**: Sistema limpia ruta cuando proceso termina
- [ ] **PROC-04**: Sistema forwarda signals (SIGTERM, SIGINT)

### Integración .NET

- [ ] **DOTNET-01**: CLI funciona como `dotnet tool` global
- [ ] **DOTNET-02**: Ejemplos para integrar con launchSettings.json
- [ ] **DOTNET-03**: Ejemplos para integrar con appsettings.json

### Cross-Platform

- [ ] **XPLAT-01**: Proxy funciona idénticamente en Windows 10+
- [ ] **XPLAT-02**: Proxy funciona idénticamente en macOS 12+
- [ ] **XPLAT-03**: Proxy funciona idénticamente en Linux (Ubuntu 20.04+, Debian 11+)

## v2 Requirements

Deferred to future release. Tracked but not in current roadmap.

### HTTPS

- **HTTPS-01**: Sistema genera certificados TLS automáticamente
- **HTTPS-02**: Sistema genera CA local
- **HTTPS-03**: Sistema agrega CA a system trust store (Windows/macOS/Linux)
- **HTTPS-04**: Certificados se renuevan automáticamente

### HTTP/2

- **HTTP2-01**: Proxy soporta HTTP/2 con multiplexing
- **HTTP2-02**: Proxy soporta priorización de streams

### WebSockets

- **WS-01**: Proxy soporta WebSocket upgrades
- **WS-02**: Proxy preserva Connection headers
- **WS-03**: Proxy funciona con SignalR

### Advanced Features

- **ADV-01**: Sistema soporta subdominios (api.v1.miapp.localhost)
- **ADV-02**: `portless trust` command para confiar CA local
- **ADV-03**: Bypass con variable PORTLESS=0|skip

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
|---------|--------|
| Interfaz gráfica | Es una herramienta CLI para desarrolladores |
| Soporte remoto | Solo desarrollo local (localhost/127.0.0.1) |
| Load balancing | Single destination por hostname es suficiente |
| Auth/Z | No expone servicios externamente, solo desarrollo local |
| Mobile app | Web-first, mobile fuera de scope |
| Distributed deployment | Single-machine development tool |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| PROXY-01 | Phase 1 | Complete |
| PROXY-02 | Phase 1 | Complete |
| PROXY-03 | Phase 1 | Complete |
| PROXY-04 | Phase 1 | Complete |
| ROUTE-01 | Phase 2 | Complete |
| ROUTE-02 | Phase 2 | Complete |
| ROUTE-03 | Phase 2 | Complete |
| ROUTE-04 | Phase 2 | Complete |
| CLI-01 | Phase 3 | Pending |
| CLI-02 | Phase 3 | Pending |
| CLI-03 | Phase 3 | Complete |
| CLI-04 | Phase 3 | Pending |
| CLI-05 | Phase 3 | Complete |
| PORT-01 | Phase 4 | Pending |
| PORT-02 | Phase 4 | Pending |
| PORT-03 | Phase 4 | Pending |
| PORT-04 | Phase 4 | Pending |
| PROC-01 | Phase 5 | Pending |
| PROC-02 | Phase 5 | Pending |
| PROC-03 | Phase 5 | Pending |
| PROC-04 | Phase 5 | Pending |
| DOTNET-01 | Phase 6 | Pending |
| DOTNET-02 | Phase 6 | Pending |
| DOTNET-03 | Phase 6 | Pending |
| XPLAT-01 | Phase 7 | Pending |
| XPLAT-02 | Phase 7 | Pending |
| XPLAT-03 | Phase 7 | Pending |

**Coverage:**
- v1 requirements: 24 total
- Mapped to phases: 24
- Unmapped: 0 ✓

---
*Requirements defined: 2025-02-19*
*Last updated: 2025-02-19 after roadmap creation*
