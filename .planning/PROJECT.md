# Portless.NET

## What This Is

Una herramienta `dotnet tool` que elimina los conflictos de puerto en desarrollo local proporcionando URLs estables y nombradas en `.localhost`. En lugar de recordar si tu app está en `localhost:3001`, `localhost:8080` o `localhost:5000`, simplemente usas `http://miapi.localhost:1355`.

Es un port de [Portless](https://github.com/portless/portless) (Node.js) a .NET 10 usando **YARP** como motor de proxy inverso, diseñado específicamente para desarrolladores .NET que necesitan soporte Windows nativo.

## Core Value

**URLs estables y predecibles para desarrollo local** - Si esta única cosa falla, el proyecto no tiene propósito.

## Requirements

### Validated

(Ninguno aún - es un proyecto greenfield)

### Active

- [ ] Proxy HTTP funcional con routing por hostname
- [ ] CLI commands básicos: `proxy start`, `proxy stop`, `list`, ejecución de apps
- [ ] Asignación automática de puertos (4000-4999)
- [ ] Gestión de rutas dinámicas con persistencia
- [ ] Soporte Windows nativo (diferenciador clave vs Portless original)
- [ ] Integración con ecosistema .NET (dotnet tool, launchSettings.json)

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
| Evolutivo vs feature-complete v1 | Validar MVP primero, agregar complejidad gradualmente | — Pending |
| .NET 10 con Native AOT | Single binary, mejor performance que Node.js, startup rápido | — Pending |
| Spectre.Console.Cli sobre System.CommandLine | Mejor experiencia CLI con output coloreado y formateado | — Pending |
| Archivo PRD.md extenso | Documentación detallada guía roadmap y arquitectura | ✓ Good |

---
*Last updated: 2025-02-19 after initialization*
