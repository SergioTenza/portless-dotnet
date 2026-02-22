# Portless.NET

## What This Is

Una herramienta `dotnet tool` que elimina los conflictos de puerto en desarrollo local proporcionando URLs estables y nombradas en `.localhost`. En lugar de recordar si tu app está en `localhost:3001`, `localhost:8080` o `localhost:5000`, simplemente usas `http://miapi.localhost:1355`.

Es un port de [Portless](https://github.com/portless/portless) (Node.js) a .NET 10 usando **YARP** como motor de proxy inverso, diseñado específicamente para desarrolladores .NET que necesitan soporte Windows nativo.

## Core Value

**URLs estables y predecibles para desarrollo local** - Si esta única cosa falla, el proyecto no tiene propósito.

## Current State

**Shipped:** v1.1 Advanced Protocols — 2026-02-22

Portless.NET v1.1 adds advanced protocol support (HTTP/2 and WebSockets) to enable real-time applications and improved performance. Building on v1.0 MVP, v1.1 includes:

**v1.0 Features (Foundation):**
- HTTP proxy with YARP routing by hostname
- CLI commands (proxy start/stop/status, list, run)
- Automatic port allocation (4000-4999 range)
- Route persistence with hot-reload
- Process management with PID tracking
- Global tool installation (761KB package)
- 4 integration examples (WebApi, Blazor, Worker, Console)
- Comprehensive documentation (3,049 lines)
- Integration test suite (45 tests)

**v1.1 Features (Advanced Protocols):**
- HTTP/2 support con Kestrel (ALPN negotiation, protocol logging)
- WebSocket transparent proxy (HTTP/1.1 upgrade + HTTP/2 Extended CONNECT)
- SignalR chat example con browser y console clients
- Integration tests para HTTP/2, WebSocket y SignalR (8 new tests)
- X-Forwarded headers para backward compatibility
- Protocol logging con silent downgrade detection
- Documentación completa (troubleshooting, migration, protocol testing)
- 3 nuevos ejemplos (WebSocketEchoServer, SignalRChat, HTTP/2 tests)

**Platform:** Windows 10+ (macOS/Linux validation deferred to v1.2+)

**Codebase:** ~6,500 LOC C# across 4 projects (Core, Cli, Proxy, Tests)

## Current Milestone: v1.2 HTTPS with Automatic Certificates

**Goal:** Implement HTTPS support con certificados TLS automáticos generados on-the-fly para desarrollo local seguro sin configuración manual.

**Target features:**
- Certificate authority (CA) local para generar certificados automáticamente
- Wildcard certificates para `*.localhost` (o `*.local.dev` según configuración)
- Trust certificate installation en sistema operativo (Windows/macOS/Linux)
- HTTPS endpoint en proxy (puerto 1356 por defecto, configurable)
- Mixed HTTP/HTTPS support (mismo proxy, ambos protocolos)
- Certificate renewal automática antes de expiración
- CLI commands para gestión de certificados (`portless cert install`, `portless cert trust`, `portless cert status`)

## Next Milestone Goals

**Future work for v1.2 or later:**
- HTTPS support con certificados automáticos
- Cross-platform validation (macOS, Linux)
- Performance optimization
- Advanced CLI features (profiles, configuration files)
- HTTP/3 (QUIC) support para TCP head-of-line blocking elimination

## Next Milestone Goals

**Future work for v1.2 or later:**
- HTTPS support con certificados automáticos
- Cross-platform validation (macOS, Linux)
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

### Validated (v1.1)

- ✓ HTTP/2 support con Kestrel ALPN negotiation — Phase 09
- ✓ Protocol logging con silent downgrade detection — Phase 09
- ✓ X-Forwarded headers para backward compatibility — Phase 09
- ✓ WebSocket transparent proxy (HTTP/1.1 + HTTP/2) — Phase 10
- ✓ Long-lived WebSocket connections (10-min timeout) — Phase 10
- ✓ SignalR integration con chat example — Phase 11
- ✓ Integration tests para HTTP/2, WebSocket, SignalR — Phase 09-11
- ✓ Documentación completa de protocolos avanzados — Phase 12

### Active

- [ ] HTTPS proxy con certificate authority local — v1.2
- [ ] Automatic certificate generation para `*.localhost` — v1.2
- [ ] Trust certificate installation (Windows/macOS/Linux) — v1.2
- [ ] Certificate renewal automática — v1.2
- [ ] HTTPS endpoint (puerto 1356) — v1.2
- [ ] Mixed HTTP/HTTPS support — v1.2
- [ ] CLI certificate management commands — v1.2
- [ ] Integration tests para HTTPS — v1.2
- [ ] Cross-platform validation (macOS, Linux) — v1.3+ (deferido para foco en HTTPS)
- [ ] Performance optimization — v1.3+

### Out of Scope

- **Interfaz gráfica** — Es una herramienta CLI para desarrolladores
- **Soporte remoto** — Solo desarrollo local (localhost/127.0.0.1)
- **Load balancing** — Single destination por hostname
- **Auth/Z** — No expone servicios externamente, solo desarrollo local
- **Cross-platform (macOS/Linux)** — Validación completa deferida a v1.3+ — Windows focus mantenido en v1.2, HTTPS trust implementation incluirá macOS/Linux
- **HTTP/3 (QUIC)** — Deferred to v1.3+ o future — HTTP/2 con HTTPS es prioritario
- **Certificate revocation** — Desarrollo local no requiere revocación compleja
- **Multiple certificate authorities** — Single CA integrado es suficiente
- **EV certificates / Organization validation** — Desarrollo local usa self-signed certs

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
| ForwardedHeaders vs YARP transforms | ASP.NET Core built-in middleware más simple que custom YARP transforms | ✓ Good - Phase 09 implementado exitosamente |
| Kestrel timeout configuration (10-min) | Soporta long-lived WebSocket connections para real-time apps | ✓ Good - Phase 10 validado con integration tests |
| SignalR sin YARP special config | SignalR WebSocket funciona automáticamente a través de proxy | ✓ Good - Phase 11 confirmó sin configuración adicional |
| Echo server vs full chat app | Server simple más fácil de testear que chat completo con estado | ✓ Good - Phase 10, mejor para pruebas unitarias |

---
*Last updated: 2026-02-22 after starting v1.2 HTTPS with Automatic Certificates milestone*
