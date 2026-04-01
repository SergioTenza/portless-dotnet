# Agent Context - Portless.NET

> Documento de contexto para agentes AI que continuan el desarrollo de Portless.NET.
> Ultima actualizacion: 2026-04-01 por Hermes Agent (profile: portless-dotnet)
> Memorias respaldadas en Honcho: https://honcho.tnzservicios.es/v3/workspaces/hermes/peers/hermes-agent/card

---

## Estado del Proyecto

**Version actual:** v1.3.0 RELEASED + v2.0 TIER 1 COMPLETE
**Branch:** feature/v2.0-dx (from development)
**Tag:** v1.3.0 at commit ae5e547
**GitHub:** https://github.com/SergioTenza/portless-dotnet

### Build & Tests
- Build: 0 errores, warnings by-design (IL2xxx/IL3xxx, ASP0000)
- Tests: 98 unitarios (86 base + 14 nuevos integracion RunCommand) + 19 integracion = 117+
- SDK: .NET 10.0.201 en /usr/local/dotnet. **SIEMPRE** ejecutar: `export PATH="/usr/local/dotnet:$PATH"`
- Git user: Sergio Tenza <sergio@tnzservicios.es>

### v2.0 TIER 1 Progress (branch: feature/v2.0-dx)

#### All TIER 1 Features Complete (6 commits)
- [x] `portless get <name>` - URL de servicio para scripts
- [x] `portless alias <name> <port>` - Rutas estaticas (Docker, servicios externos)
- [x] `portless alias --remove <name>` - Eliminar alias
- [x] `portless hosts sync` - Sincronizar /etc/hosts
- [x] `portless hosts clean` - Limpiar entradas portless
- [x] Auto-deteccion de nombre (.csproj/git/cwd) via IProjectNameDetector
- [x] Framework Detection (11 frameworks) via IFrameworkDetector
- [x] Placeholder expansion ({PORT}, {HOST}, {URL}, {NAME}) via PlaceholderExpander
- [x] Paginas de error branded (404, 502, 508) con dark theme
- [x] Integracion completa en RunCommand:
  - Auto-deteccion de nombre cuando NAME no se especifica
  - Framework detection con inyeccion automatica de env vars (ASPNETCORE_URLS, etc.)
  - Placeholder expansion en command args + framework flags
  - PORTLESS_URL siempre inyectada
  - IProcessManager extendido con overload para additional env vars
- [x] 35+ tests (21 unitarios features + 14 integracion RunCommand/ProcessManager)
- [x] DEBUG lines removidos de RunCommand

### Warnings By-Design (NO TOCAR)
- `IL2xxx/IL3xxx` - Spectre.Console incompatible con AOT/trimming
- `ASP0000` - BuildServiceProvider, patron estandar en .NET DI

### Estructura de la Solucion (Portless.slnx)
```
Portless.Core/          - Class library, logica compartida
Portless.Cli/           - Console app, CLI entry point (dotnet tool)
Portless.Proxy/         - Web app, proxy YARP (Kestrel)
Portless.Tests/         - xUnit test suite (98 tests)
Portless.IntegrationTests/ - Integration tests (19 tests)
Portless.E2ETests/      - End-to-end tests
TestApi/                - Test API para proxy testing
```

### Comandos CLI Actuales (v2.0 TIER 1)
```
portless proxy start/stop/status  - Gestion del reverse proxy
portless run [name] <cmd>         - Ejecutar app con URL nombrada (auto-naming)
portless list                     - Ver rutas activas
portless get <name>               - Obtener URL de un servicio
portless alias <name> <port>      - Rutas estaticas (Docker, etc.)
portless alias --remove <name>    - Eliminar alias
portless hosts sync/clean         - Gestion de /etc/hosts
portless cert check/status/install/uninstall/renew  - Certificados
```

### Variables de Entorno
| Variable | Default | Descripcion |
|----------|---------|-------------|
| `PORTLESS_PORT` | 1355 | Puerto del proxy HTTP |
| `PORTLESS_HTTPS_ENABLED` | false | Habilitar HTTPS |
| `PORTLESS_STATE_DIR` | ~/.portless | Directorio de estado |
| `PORTLESS` | - | `0` o `skip` para bypass |

---

## Roadmap v2.0 Completo

### TIER 1: Developer Experience ✅ COMPLETE
- 1.1 CLI completa + auto-deteccion
- 1.2 Framework detection + placeholder expansion
- 1.3 Paginas de error branded
- Integracion completa en RunCommand

### TIER 2: Routing Avanzado (SIGUIENTE)
- 2.1 Path-based routing (YARP RouteConfig.Match.Path)
- 2.2 TCP proxying (databases/Redis)
- 2.3 Multi-backend load balancing (YARP round-robin)
- 2.4 Configuration file (portless.config.yaml)

### TIER 3: Observabilidad y Distribucion
- Metrics Prometheus, Health checks, Native AOT, NuGet, Shell completion

---

## Infraestructura
- **Server:** 194.164.160.125 (Ubuntu)
- **Honcho:** https://honcho.tnzservicios.es (IP-auth, no API key)
  - Workspace: hermes | Peer: hermes-agent
  - Leer: GET /v3/workspaces/hermes/peers/hermes-agent/card
  - Escribir: PUT /v3/workspaces/hermes/peers/hermes-agent/card

## Perfil del Usuario
- Sergio Tenza, habla espanol
- Prefiere configs minimas, agrega cosas incrementalmente
- Self-hosted/open-source
- Provider LLM preferido: z.ai (ZhipuAI GLM)
