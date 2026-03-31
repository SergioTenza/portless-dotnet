# Agent Context - Portless.NET

> Documento de contexto para agentes AI que continuan el desarrollo de Portless.NET.
> Ultima actualizacion: 2026-03-31 por Hermes Agent (profile: portless-dotnet)
> Memorias respaldadas en Honcho: https://honcho.tnzservicios.es/v3/workspaces/hermes/peers/hermes-agent/card

---

## Estado del Proyecto

**Version actual:** v1.3.0 RELEASED + v2.0 TIER 1 IN PROGRESS
**Branch:** feature/v2.0-dx (from development)
**Tag:** v1.3.0 at commit ae5e547
**GitHub:** https://github.com/SergioTenza/portless-dotnet

### Build & Tests
- Build: 0 errores, warnings by-design (IL2xxx/IL3xxx, ASP0000)
- Tests: 76 unitarios + 21 nuevos v2.0 + 19 integracion = 116 total
- SDK: .NET 10.0.201 en /usr/local/dotnet. **SIEMPRE** ejecutar: `export PATH="/usr/local/dotnet:$PATH"`
- Git user: Sergio Tenza <sergio@tnzservicios.es>

### v2.0 TIER 1 Progress (branch: feature/v2.0-dx)

#### Completed (4 commits: dbbd4fe, cf57277, 76182c9, 8f61290)
- [x] `portless get <name>` - URL de servicio para scripts
- [x] `portless alias <name> <port>` - Rutas estaticas (Docker, servicios externos)
- [x] `portless alias --remove <name>` - Eliminar alias
- [x] `portless hosts sync` - Sincronizar /etc/hosts
- [x] `portless hosts clean` - Limpiar entradas portless
- [x] Auto-deteccion de nombre (.csproj/git/cwd) via IProjectNameDetector
- [x] Framework Detection (ASP.NET, Vite, Next.js, Astro, Angular, Expo, React Native, npm, Python, Go, Rust) via IFrameworkDetector
- [x] Placeholder expansion ({PORT}, {HOST}, {URL}, {NAME}) via PlaceholderExpander
- [x] Paginas de error branded (404 con rutas activas, 502 con contexto, 508 loop)
- [x] ProxyPortProvider para resolucion consistente del puerto proxy
- [x] 21 tests unitarios para todos los nuevos features

#### Remaining for TIER 1 completion
- [ ] Integrar framework detection en RunCommand (inyeccion automatica de flags)
- [ ] Integrar placeholder expansion en RunCommand
- [ ] Integrar auto-deteccion de nombre en RunCommand (cuando NAME no se especifica)
- [ ] Tests de integracion para los nuevos comandos CLI

### Warnings By-Design (NO TOCAR)
- `IL2xxx/IL3xxx` - Spectre.Console incompatible con AOT/trimming
- `ASP0000` - BuildServiceProvider, patron estandar en .NET DI

### Estructura de la Solucion (Portless.slnx)
```
Portless.Core/          - Class library, logica compartida
Portless.Cli/           - Console app, CLI entry point (dotnet tool)
Portless.Proxy/         - Web app, proxy YARP (Kestrel)
Portless.Tests/         - xUnit test suite (76 + 21 tests)
Portless.IntegrationTests/ - Integration tests (19 tests)
Portless.E2ETests/      - End-to-end tests
TestApi/                - Test API para proxy testing
```

### Comandos CLI Actuales (v2.0 TIER 1)
```
portless proxy start/stop/status  - Gestion del reverse proxy
portless run <name> <cmd>         - Ejecutar app con URL nombrada
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

### TIER 1: Developer Experience (EN PROGRESO)
- 1.1 CLI completa + auto-deteccion ✅
- 1.2 Framework detection + placeholder expansion ✅
- 1.3 Paginas de error branded ✅
- Integracion en RunCommand (PENDIENTE)

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
