# Agent Context - Portless.NET

> Documento de contexto para agentes AI que continuan el desarrollo de Portless.NET.
> Ultima actualizacion: 2026-04-07 por Hermes Agent (profile: portless-dotnet)
> Memorias respaldadas en Honcho: https://honcho.tnzservicios.es/v3/workspaces/hermes/peers/hermes-agent/card

---

## Estado del Proyecto

**Version actual:** v1.3.0 RELEASED + v2.0 TIER 2 COMPLETE
**Branch:** feature/v2.0-dx (from development)
**Tag:** v1.3.0 at commit ae5e547
**GitHub:** https://github.com/SergioTenza/portless-dotnet

### Build & Tests
- Build: 0 errores, warnings by-design (IL2xxx/IL3xxx, ASP0000, CA1416)
- Tests: 126 passing, 1 skipped, 1 flaky (YarpProxyIntegrationTests.MultipleHostnames_RouteToDifferentBackends - passes in isolation)
- SDK: .NET 10.0.201 en /usr/local/dotnet. **SIEMPRE** ejecutar: `export PATH="/usr/local/dotnet:$PATH"`
- Git user: Sergio Tenza <sergio@tnzservicios.es>

### v2.0 TIER 2 Progress (branch: feature/v2.0-dx)

#### Phase 0: Refactor Base (3 commits)
- [x] IYarpConfigFactory - consolidated CreateRoute/CreateCluster from Program.cs + PortlessApiEndpoints
- [x] RouteInfo expanded: Path, BackendUrls[], LoadBalancingPolicy, RouteType (Http/Tcp), TcpListenPort
- [x] CLI uses IProxyRouteRegistrar, removed double-persistence in RunCommand

#### Feature 2.4: Config File (4 commits)
- [x] YamlDotNet 16.3.0 added to Portless.Core
- [x] PortlessConfig / PortlessRouteConfig YAML models
- [x] PortlessConfigLoader service (find, load, convert to RouteInfo[])
- [x] `portless up` CLI command (reads portless.config.yaml, registers routes)
- [x] Proxy loads config file routes on startup

#### Feature 2.1: Path-Based Routing (2 commits)
- [x] YarpConfigFactory supports path parameter in CreateRoute
- [x] AddHostRequest expanded with Path field
- [x] Route persistence includes Path
- [x] `--path` flag on `portless run` and `portless alias` commands

#### Feature 2.3: Load Balancing (2 commits)
- [x] YARP multi-backend clusters with multiple DestinationConfig
- [x] AddHostRequest expanded with BackendUrls[] and LoadBalancePolicy
- [x] LoadBalancingPolicy enum mapped to YARP policy strings
- [x] `--backend` flag on `portless run` command
- [x] IProxyRouteRegistrar multi-backend overload

#### Feature 2.2: TCP Proxying (1 commit)
- [x] TcpForwardingService (background service, TcpListener relay)
- [x] TCP management API endpoints: POST /api/v1/tcp/add, DELETE /api/v1/tcp/remove
- [x] `portless tcp` CLI command (add/remove TCP proxies)
- [x] Proxy starts TCP listeners from config file on startup

### Warnings By-Design (NO TOCAR)
- `IL2xxx/IL3xxx` - Spectre.Console incompatible con AOT/trimming
- `ASP0000` - BuildServiceProvider, patron estandar en .NET DI
- `CA1416` - Platform-specific code (macOS cert trust)

### Estructura de la Solucion (Portless.slnx)
```
Portless.Core/          - Class library, logica compartida
Portless.Cli/           - Console app, CLI entry point (dotnet tool)
Portless.Proxy/         - Web app, proxy YARP (Kestrel)
Portless.Tests/         - xUnit test suite (126+ tests)
Portless.IntegrationTests/ - Integration tests
Portless.E2ETests/      - End-to-end tests
TestApi/                - Test API para proxy testing
```

### Comandos CLI Actuales (v2.0 TIER 2)
```
portless proxy start/stop/status  - Gestion del reverse proxy
portless run [name] <cmd>         - Ejecutar app con URL nombrada
portless run --path /api          - Path-based routing
portless run --backend URL        - Load-balanced multi-backend
portless list                     - Ver rutas activas
portless get <name>               - Obtener URL de un servicio
portless alias <name> <port>      - Rutas estaticas (Docker, etc.)
portless alias --remove <name>    - Eliminar alias
portless hosts sync/clean         - Gestion de /etc/hosts
portless up [-f config.yaml]      - Levantar rutas desde config file
portless tcp <name> <host:port> --listen <port>  - TCP proxy
portless tcp <name> --remove      - Eliminar TCP proxy
portless cert check/status/install/uninstall/renew  - Certificados
```

### Config File (portless.config.yaml)
```yaml
routes:
  - host: api.localhost
    backends:
      - http://localhost:5000
  - host: web.localhost
    path: /app
    backends:
      - http://localhost:3000
      - http://localhost:3001
    loadBalance: RoundRobin
  - host: redis.localhost
    type: tcp
    listenPort: 16379
    backends:
      - localhost:6379
```

### Variables de Entorno
| Variable | Default | Descripcion |
|----------|---------|-------------|
| `PORTLESS_PORT` | 1355 | Puerto del proxy HTTP |
| `PORTLESS_HTTPS_ENABLED` | false | Habilitar HTTPS |
| `PORTLESS_STATE_DIR` | ~/.portless | Directorio de estado |
| `PORTLESS` | - | `0` o `skip` para bypass |
| `PORTLESS_CERT_WARNING_DAYS` | 30 | Dias antes de expiracion para advertencia |
| `PORTLESS_CERT_CHECK_INTERVAL_HOURS` | 6 | Horas entre verificaciones de certificado |
| `PORTLESS_AUTO_RENEW` | true | Renovar certificado automaticamente |
| `PORTLESS_ENABLE_MONITORING` | false | Habilitar monitoreo en segundo plano |

---

## Roadmap v2.0 Completo

### TIER 1: Developer Experience ✅ COMPLETE
- 1.1 CLI completa + auto-deteccion
- 1.2 Framework detection + placeholder expansion
- 1.3 Paginas de error branded
- Integracion completa en RunCommand

### TIER 2: Routing Avanzado ✅ COMPLETE
- 2.1 Path-based routing (YARP RouteConfig.Match.Path)
- 2.2 TCP proxying (TcpForwardingService)
- 2.3 Multi-backend load balancing (YARP round-robin, etc.)
- 2.4 Configuration file (portless.config.yaml)

### TIER 3: Observabilidad y Distribucion (SIGUIENTE)
- Metrics Prometheus, Health checks, Native AOT, NuGet, Shell completion

---

## New Tier 2 Services

| Service | Interface | Description |
|---------|-----------|-------------|
| YarpConfigFactory | IYarpConfigFactory | Creates YARP RouteConfig/ClusterConfig |
| PortlessConfigLoader | IPortlessConfigLoader | Loads portless.config.yaml |
| TcpForwardingService | ITcpForwardingService | TCP port forwarding relay |

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
