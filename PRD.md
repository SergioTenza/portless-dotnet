# Product Requirements Document - Portless.NET

## 1. Visión del Producto

**Portless.NET** es una herramienta `dotnet tool` que reemplaza los números de puerto en desarrollo local con URLs estables y nombradas en `.localhost` (ej: `http://miapi.localhost:1355`). Es un port de Portless (Node.js) a .NET 10 usando **YARP** como motor de proxy inverso.

### Problema que Resuelve

- **Conflictos de puerto**: `EADDRINUSE` al ejecutar múltiples servicios
- **Memorización de puertos**: ¿Estaba en 3001, 8080 o 5000?
- **Pestañas equivocadas**: Refrescar la aplicación incorrecta
- **Inconsistencia para agents/AI**: URLs cambian entre sesiones
- **Monorepos complejos**: Múltiples servicios con puertos conflictivos

### Solución

Proporciona URLs estables y predecibles que resuelven a `127.0.0.1` y enrutan automáticamente al puerto correcto.

---

## 2. Objetivos Principales

### MVP (4 semanas)

- Feature parity básica con Portless original
- Proxy HTTP/1.1 funcional
- Gestión de rutas dinámica
- CLI con comandos esenciales

### v1.0 (8 semanas)

- HTTP/2 + HTTPS con certificados auto-generados
- WebSockets
- Soporte de subdominios
- Cross-platform completo (Windows, macOS, Linux)

### v1.1 (12 semanas)

- DX improvements
- Integraciones con herramientas .NET
- Métricas y diagnóstico

---

## 3. Usuario Target

### Personas Principales

1. **Desarrollador .NET Full-Stack**
   - Desarrolla frontends + backends .NET
   - Usa Windows principalmente
   - Trabaja con múltiples servicios simultáneos
   - Necesita URLs consistentes para testing

2. **Desarrollador de Microservicios**
   - Monorepositorios con 5+ servicios
   - Orquesta contenedores y procesos locales
   - Necesita aislamiento entre servicios
   - Valora la reproducibilidad

3. **AI Agent User**
   - Automatiza pruebas E2E
   - Requiere URLs predecibles
   - No puede lidiar con puertos dinámicos

### Diferenciadores vs Portless Node.js

- ✅ **Soporte Windows nativo** (Portless original no lo tiene)
- ✅ **Mejor performance** (.NET 10 vs Node.js)
- ✅ **Single binary deployment** (Native AOT)
- ✅ **Integración nativa con ecosistema .NET**

---

## 4. Historias de Usuario

### HU-1: Iniciar Proxy

**Como** desarrollador
**Quiero** iniciar el proxy con un solo comando
**Para** que todas mis apps usen URLs consistentes

**Criterios de Aceptación:**

- [ ] `portless proxy start` inicia proxy en puerto 1355
- [ ] Proxy corre en background
- [ ] Comando falla si puerto ya está en uso
- [ ] `portless proxy stop` detiene proxy limpiamente

### HU-2: Ejecutar App con URL Nombrada

**Como** desarrollador
**Quiero** ejecutar mi app con un nombre
**Para** accederla siempre en la misma URL

**Criterios de Aceptación:**

- [ ] `portless miapi dotnet run` crea URL `http://miapi.localhost:1355`
- [ ] Puerto se asigna automáticamente (4000-4999)
- [ ] Variable `PORT` se inyecta en el comando
- [ ] Route se limpia cuando proceso termina

### HU-3: Listar Apps Activas

**Como** desarrollador
**Quiero** ver todas las apps corriendo
**Para** saber qué URLs tengo disponibles

**Criterios de Aceptación:**

- [ ] `portless list` muestra hostname → puerto mappings
- [ ] Muestra PID de cada proceso
- [ ] Lista se actualiza en tiempo real
- [ ] Muestra URL completa con protocolo

### HU-4: HTTPS Automático

**Como** desarrollador
**Quiero** HTTPS sin configuración manual
**Para** probar features que requieren TLS

**Criterios de Aceptación:**

- [ ] `portless proxy start --https` genera certificados
- [ ] CA se agrega a system trust store
- [ ] Certificados se renuevan automáticamente
- [ ] No hay warnings de browser

### HU-5: Subdominios

**Como** desarrollador
**Quiero** usar subdominios para organizar servicios
**Para** tener URLs jerárquicas

**Criterios de Aceptación:**

- [ ] `portless api.v1 miapp dotnet run` crea `http://api.v1.miapp.localhost:1355`
- [ ] Soporta múltiples niveles de profundidad
- [ ] Wildcard routes funcionan correctamente

### HU-6: WebSockets

**Como** desarrollador
**Quiero** que WebSockets funcionen transparentemente
**Para** apps tiempo real

**Criterios de Aceptación:**

- [ ] WebSocket upgrades se proxean correctamente
- [ ] Connection headers se preservan
- [ ] Funciona con SignalR

### HU-7: Bypass Opcional

**Como** desarrollador
**Quiero** desactivar portless temporalmente
**Para** debuggear directamente

**Criterios de Aceptación:**

- [ ] `PORTLESS=0 portless miapp dotnet run` ejecuta directamente
- [ ] `PORTLESS=skip` también funciona
- [ ] Variable se documenta en help

---

## 5. Requisitos Funcionales

### Proxy Core

- [ ] Reverse proxy con routing por Host header
- [ ] HTTP/1.1 completo
- [ ] HTTP/2 con multiplexing
- [ ] WebSocket upgrades
- [ ] Headers X-Forwarded-* correctos
- [ ] Connection pooling

### Gestión de Rutas

- [ ] Persistencia en JSON
- [ ] File locking para concurrencia
- [ ] Limpieza de rutas muertas (PID verification)
- [ ] Hot reload de configuración

### Asignación de Puertos

- [ ] Detección de puerto libre (4000-4999)
- [ ] Verificación de disponibilidad
- [ ] Pool de puertos reutilizables
- [ ] Configurable via env var

### CLI

- [ ] `portless proxy start [--https] [-p <port>]`
- [ ] `portless proxy stop`
- [ ] `portless <name> <command...>`
- [ ] `portless list`
- [ ] `portless trust`
- [ ] Colored output con chalk-like formatting

### Certificados

- [ ] Generación de CA local
- [ ] Generación de certificados server
- [ ] Trust store integration (Windows/macOS/Linux)
- [ ] Auto-renewal

### Process Management

- [ ] Spawn command con PORT env var
- [ ] PID tracking
- [ ] Cleanup on exit
- [ ] Signal forwarding (SIGTERM, SIGINT)

### Configuración

- [ ] Variables de entorno:
  - `PORTLESS_PORT` (default: 1355)
  - `PORTLESS_HTTPS` (auto-enable HTTPS)
  - `PORTLESS_STATE_DIR` (custom state dir)
  - `PORTLESS=0|skip` (bypass)

### Integración ASP.NET Core

- [ ] Documentación para integrar con `launchSettings.json`
- [ ] Ejemplos para diferentes frameworks

---

## 6. Requisitos No Funcionales

### Rendimiento

- [ ] < 5ms overhead por request proxy
- [ ] > 10,000 req/sec throughput
- [ ] < 100ms cold start (sin AOT) / < 20ms (con AOT)
- [ ] < 100MB memory base

### Cross-Platform

- [ ] Soporte Windows 10+
- [ ] Soporte macOS 12+
- [ ] Soporte Ubuntu 20.04+, Debian 11+
- [ ] Comportamiento idéntico en todas las plataformas

### Confiabilidad

- [ ] No memory leaks
- [ ] Graceful shutdown
- [ ] Recovery de crash
- [ ] Idempotencia de comandos

### Seguridad

- [ ] No expone servicios externamente
- [ ] Validación de hostnames
- [ ] Sanitización de input
- [ ] Safe default permissions

### Developer Experience

- [ ] < 5 segundos desde instalación hasta primer uso
- [ ] Mensajes de error claros y accionables
- [ ] Help text completo
- [ ] Auto-completion donde sea posible

### Testing

- [ ] > 80% code coverage
- [ ] Tests unitarios y integración
- [ ] Tests E2E cross-platform
- [ ] Performance benchmarks

### Documentación

- [ ] README con quick start
- [ ] Changelog mantenido
- [ ] API docs para extensiones
- [ ] Troubleshooting guide

---

## 7. Arquitectura Técnica

### Stack Tecnológico

- **.NET 10** con C# 14
- **YARP 2.x** (Yet Another Reverse Proxy)
- **System.CommandLine** para CLI
- **Serilog** para logging
- **xUnit + Moq + Alba** para testing
- **Native AOT** para distribución

### Estructura del Repositorio

```
portless-dotnet/
├── src/
│   ├── Portless.Core/              # Class library
│   │   ├── Models/
│   │   │   ├── RouteInfo.cs
│   │   │   ├── ProxyConfig.cs
│   │   │   └── CertificateInfo.cs
│   │   ├── Services/
│   │   │   ├── YarpProxyProvider.cs
│   │   │   ├── RouteStore.cs
│   │   │   ├── PortAssigner.cs
│   │   │   ├── CertificateGenerator.cs
│   │   │   ├── CertificateTruster.cs
│   │   │   ├── DaemonManager.cs
│   │   │   └── ProcessManager.cs
│   │   └── Extensions/
│   │       └── ServiceCollectionExtensions.cs
│   │
│   ├── Portless.Cli/               # Console app (dotnet tool)
│   │   ├── Commands/
│   │   │   ├── ProxyCommand.cs
│   │   │   ├── RunCommand.cs
│   │   │   ├── ListCommand.cs
│   │   │   └── TrustCommand.cs
│   │   └── Program.cs
│   │
│   └── Portless.Tests/             # Test suite
│       ├── Unit/
│       ├── Integration/
│       └── E2E/
│
├── docs/
│   ├── architecture.md
│   ├── contributing.md
│   └── troubleshooting.md
├── README.md
├── CONTRIBUTING.md
├── CHANGELOG.md
└── .gitignore
```

### Componentes Principales

#### YarpProxyProvider

```csharp
public interface IYarpProxyProvider
{
    Task StartAsync(int port, CancellationToken ct);
    Task StopAsync();
    Task ReloadRoutesAsync(RouteInfo[] routes);
}
```

#### RouteStore

```csharp
public interface IRouteStore
{
    RouteInfo[] LoadRoutes();
    void AddRoute(string hostname, int port, int pid);
    void RemoveRoute(string hostname);
    void CleanupStaleRoutes();
}
```

#### PortAssigner

```csharp
public interface IPortAssigner
{
    Task<int> AssignFreePortAsync(int minPort = 4000, int maxPort = 4999);
    bool IsPortFree(int port);
}
```

### Diagrama de Arquitectura

```
┌─────────────────────────────────────────────────────────┐
│                     CLI Layer                           │
│  (portless proxy start | portless <name> <cmd>)         │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│                   Core Services                         │
│  ┌──────────┐ ┌───────────┐ ┌────────────┐             │
│  │ Route    │ │  Port     │ │ Process    │             │
│  │ Store    │ │ Assigner  │ │ Manager    │             │
│  └──────────┘ └───────────┘ └────────────┘             │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│              YARP Proxy Layer                           │
│  ┌──────────────────────────────────────────┐           │
│  │  Reverse Proxy with Dynamic Routes       │           │
│  │  (HTTP/1.1, HTTP/2, WebSockets)          │           │
│  └──────────────────────────────────────────┘           │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│              File System                                │
│  ~/.portless/routes.json (or /tmp/portless)             │
│  ~/.portless/proxy.pid                                  │
│  ~/.portless/certs/                                     │
└─────────────────────────────────────────────────────────┘
```

---

## 8. Roadmap

### Sprint 1-2: Fundamentos

**Objetivo:** Core infrastructure

- [ ] Estructura de solución
- [ ] RouteStore con file locking
- [ ] PortAssigner con detección de puertos
- [ ] Models básicos
- [ ] Unit tests

### Sprint 3-4: Proxy Core

**Objetivo:** Proxy funcional con YARP

- [ ] YarpProxyProvider
- [ ] Configuración dinámica de rutas
- [ ] HTTP/1.1 completo
- [ ] ProcessManager con PORT env var
- [ ] CLI commands: `proxy start`, `proxy stop`

### Sprint 5-6: HTTPS + HTTP/2

**Objetivo:** TLS y HTTP/2

- [ ] CertificateGenerator
- [ ] CertificateTruster cross-platform
- [ ] HTTP/2 con YARP
- [ ] WebSocket upgrades
- [ ] CLI command: `trust`

### Sprint 7-8: Advanced Features

**Objetivo:** Features avanzadas

- [ ] Subdominios
- [ ] CLI command: `list`
- [ ] Daemon management
- [ ] Bypass with PORTLESS env var
- [ ] E2E tests cross-platform

---

## 9. Criterios de Éxito

### Métricas de Adopción

- [ ] 1000+ descargas NuGet en 3 meses
- [ ] 500+ GitHub stars en 6 meses
- [ ] 50+ contributors en 1 año

### Métricas Técnicas

- [ ] < 5ms overhead por request
- [ ] > 10,000 req/sec throughput
- [ ] < 100MB memory footprint
- [ ] > 80% code coverage
- [ ] 0 critical bugs en producción

### Métricas de Calidad

- [ ] Tests pasan en Windows, macOS, Linux
- [ ] 0 regresiones en versiones mayores
- [ ] Response time < 24h en issues críticos
- [ ] Documentación completa

---

## 10. Riesgos y Mitigaciones

### Riesgo 1: YARP Limitations

**Probabilidad:** 20% | **Impacto:** Alto

**Descripción:** YARP podría no soportar ciertos features necesarios

**Mitigación:**

- PoC temprano de rutas dinámicas
- Tests exhaustivos de WebSockets
- Plan B: Custom proxy con Kestrel

### Riesgo 2: Certificados Cross-Platform

**Probabilidad:** 40% | **Impacto:** Alto

**Descripción:** Trust store management varía mucho entre plataformas

**Mitigación:**

- Abstracción de platform-specific code
- Tests multi-OS en CI
- Documentación de troubleshooting

### Riesgo 3: Scope Creep

**Probabilidad:** 60% | **Impacto:** Medio

**Descripción:** Agregar features fuera del scope original

**Mitigación:**

- Feature gate agresivo
- PRD como referencia
- Sprint planning disciplinado

### Riesgo 4: Performance Degradation

**Probabilidad:** 30% | **Impacto:** Alto

**Descripción:** Overhead excesivo en el proxy

**Mitigación:**

- Benchmarks desde Sprint 1
- Performance budgets
- Profiling regular

### Riesgo 5: Windows Support Issues

**Probabilidad:** 25% | **Impacto:** Alto

**Descripción:** Comportamientos diferentes en Windows

**Mitigación:**

- Tests en Windows desde día 1
- VM para testing
- Beta testers Windows

### Riesgo 6: Daemon Management Complexity

**Probabilidad:** 50% | **Impacto:** Medio

**Descripción:** Background process management es complejo cross-platform

**Mitigación:**

- Considerar foreground-only inicialmente
- Library existente para daemonization
- Simplificar: servicio Windows + systemd

### Riesgo 7: Low Adoption

**Probabilidad:** 35% | **Impacto:** Alto

**Descripción:** Portless original ya tiene mercado

**Mitigación:**

- Diferenciadores claros (Windows, .NET)
- Marketing a comunidad .NET
- Integración con herramientas populares

---

## 11. Timeline Estimado

| Fase | Duración | Entregable |
|------|----------|------------|
| MVP | 4 semanas | Proxy funcional HTTP/1.1 |
| v1.0 Beta | 8 semanas | HTTP/2 + HTTPS completo |
| v1.0 Stable | 12 semanas | Cross-platform probado |
| v1.1 | 16 semanas | DX improvements |

---

## 12. Próximos Pasos Inmediatos

1. ✅ Crear repositorio con estructura inicial
2. ✅ Configurar tooling: linting, formatting, CI
3. ✅ Implementar RouteStore con tests
4. ✅ Implementar PortAssigner con tests
5. ✅ Crear CLI skeleton con System.CommandLine
6. ✅ PoC de YARP con rutas dinámicas

---

**Documento Version:** 1.0
**Last Updated:** 2025-02-19
**Status:** Ready for Development
