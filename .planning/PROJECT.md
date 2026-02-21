# Portless.NET

## What This Is

Una herramienta `dotnet tool` que elimina los conflictos de puerto en desarrollo local proporcionando URLs estables y nombradas en `.localhost`. En lugar de recordar si tu app está en `localhost:3001`, `localhost:8080` o `localhost:5000`, simplemente usas `http://miapi.localhost:1355`.

Es un port de [Portless](https://github.com/portless/portless) (Node.js) a .NET 10 usando **YARP** como motor de proxy inverso, diseñado específicamente para desarrolladores .NET que necesitan soporte Windows nativo.

## Core Value

**URLs estables y predecibles para desarrollo local** - Si esta única cosa falla, el proyecto no tiene propósito.

## Current State

**Shipped:** v1.0 MVP — 2026-02-21

Portless.NET v1.0 is a fully functional `dotnet tool` for Windows development that provides stable `.localhost` URLs. The tool includes:

- HTTP proxy with YARP routing by hostname
- CLI commands (proxy start/stop/status, list, run)
- Automatic port allocation (4000-4999 range)
- Route persistence with hot-reload
- Process management with PID tracking
- Global tool installation (761KB package)
- 4 integration examples (WebApi, Blazor, Worker, Console)
- Comprehensive documentation (3,049 lines)
- Integration test suite (45 tests)

**Platform:** Windows 10+ (macOS/Linux validation deferred)

**Codebase:** ~5,000 LOC C# across 4 projects (Core, Cli, Proxy, Tests)

## Next Milestone Goals

**Future work for v1.1 or later:**
- Cross-platform validation (macOS, Linux)
- HTTPS support
- HTTP/2 and WebSocket support
- Performance optimization
- Advanced CLI features (profiles, configuration files)

## Requirements

### Validated (v1.0)

- ✓ Proxy HTTP funcional con routing por hostname — Phase 01
- ✓ CLI commands básicos: `proxy start`, `proxy stop`, `list`, ejecución de apps — Phase 03
- ✓ Asignación automática de puertos (4000-4999) — Phase 03
- ✓ Gestión de rutas dinámicas con persistencia — Phase 02
- ✓ Soporte Windows nativo — Phase 01-05
- ✓ Integración con ecosistema .NET (dotnet tool, launchSettings.json) — Phase 06
- ✓ Integration test automation — Phase 08

### Active

(No active requirements — all v1.0 requirements validated)

### Out of Scope

- **Interfaz gráfica** — Es una herramienta CLI para desarrolladores
- **Soporte remoto** — Solo desarrollo local (localhost/127.0.0.1)
- **Load balancing** — Single destination por hostname
- **Auth/Z** — No expone servicios externamente, solo desarrollo local
- **Cross-platform (macOS/Linux)** — Deferred to v1.1+, Windows prioritized

### Out of Scope

- **Interfaz gráfica** - Es una herramienta CLI para desarrolladores
- **Soporte remoto** - Solo desarrollo local (localhost/127.0.0.1)
- **Load balancing** - Single destination por hostname
- **Auth/Z** - No expone servicios externamente, solo desarrollo local

## Context

**Por qué existe:**
- Portless original no tiene buen soporte Windows
- Los desarrolladores .NET necesitan una alternativa nativa
- Los monorepos y microservicios necesitan URLs consistentes
- Los AI agents y tests automatizados requieren URLs predecibles

**Casos de uso identificados:**
- Monorepos .NET con múltiples servicios
- Desarrollo full-stack (frontend + backend)
- Orquestación de microservicios localmente
- Testing E2E con URLs estables

**Enfoque de desarrollo:**
Evolutivo - empezar con MVP HTTP básico, agregar HTTP/2, HTTPS y WebSockets en fases posteriores según necesidad.

## Constraints

- **Tech Stack**: .NET 10, C# 14, YARP 2.3.0 - Es un port de Portless a .NET, no un rewrite con stack diferente
- **Platform**: Soporte nativo Windows 10+, macOS 12+, Linux (Ubuntu 20.04+, Debian 11+) - Prioridad Windows por la brecha de Portless original
- **Distribution**: `dotnet tool` global con Native AOT para single binary deployment
- **Performance**: <5ms overhead por request, >10,000 req/sec throughput
- **Compatibility**: Debe comportarse idénticamente en Windows, macOS y Linux

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| YARP en lugar de proxy custom | YARP es production-ready de Microsoft, soporta HTTP/2/WebSockets nativamente | ✓ Good |
| Evolutivo vs feature-complete v1 | Validar MVP primero, agregar complejidad gradualmente | ✓ Good - Phase 01-05 completado |
| .NET 10 con Native AOT | Single binary, mejor performance que Node.js, startup rápido | ✓ Good - PackAsTool funciona con advertencias aceptables |
| Spectre.Console.Cli sobre System.CommandLine | Mejor experiencia CLI con output coloreado y formateado | ✓ Good - CLI funciona correctamente |
| PackAsTool para distribución | dotnet tool install global para fácil instalación | ✓ Good - 761KB package con todas las dependencias |
| Instalación cross-platform | Scripts bash/PowerShell con PATH automático | ✓ Good - Scripts siguiendo patrón Microsoft |
| Ejemplos de integración | 4 proyectos ejemplares (WebApi, Blazor, Worker, Console) | ✓ Good - Patrones consistentes PORT variable |
| Documentación progresiva | Tutorials (migration, new, microservices, E2E) + guías integration | ✓ Good - 3,049 líneas de documentación |

---
*Last updated: 2026-02-21 after v1.0 MVP milestone*
