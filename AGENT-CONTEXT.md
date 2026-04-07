# Agent Context - Portless.NET

> Documento de contexto para agentes AI que continuan el desarrollo de Portless.NET.
> Ultima actualizacion: 2026-04-07 por Hermes Agent
> Memorias respaldadas en Honcho: https://honcho.tnzservicios.es/v3/workspaces/hermes/peers/hermes-agent/card

---

## Estado del Proyecto

**Version actual:** v2.0.0-beta.1 (Directory.Build.props)
**Branch:** feature/v2.0-dx (from development)
**GitHub:** https://github.com/SergioTenza/portless-dotnet

### Build & Tests
- Build: 0 errores, 88 warnings by-design
- Tests: 131 passing, 1 skipped, 1 flaky (YarpProxyIntegrationTests - passes in isolation)
- SDK: .NET 10.0.201 en /usr/local/dotnet. **SIEMPRE** ejecutar: `export PATH="/usr/local/dotnet:$PATH"`
- Git user: Sergio Tenza <sergio@tnzservicios.es>
- NuGet pack: Portless.NET.Tool.2.0.0-beta.1.nupkg (965 KB)
- AOT: CLI publishes as Native AOT for linux-x64

### Warnings By-Design (NO TOCAR)
- `IL2xxx/IL3xxx` - Spectre.Console + YamlDotNet incompatible con AOT/trimming
- `ASP0000` - BuildServiceProvider, patron estandar en .NET DI
- `CA1416` - Platform-specific code (macOS cert trust)

---

## TIER 1: Developer Experience ✅ COMPLETE
- CLI completa + auto-deteccion + framework detection
- Paginas de error branded + placeholder expansion

## TIER 2: Routing Avanzado ✅ COMPLETE
- 2.1 Path-based routing (--path flag, YARP Match.Path)
- 2.2 TCP proxying (TcpForwardingService relay)
- 2.3 Multi-backend load balancing (--backend, YARP policies)
- 2.4 Config file (portless.config.yaml, `portless up`)

## TIER 3: Observabilidad y Distribucion ✅ COMPLETE
- Health check: GET /health (ASP.NET Core built-in)
- Metrics: GET /metrics (prometheus-net, request counters/histograms/gauges)
- Status API: GET /api/v1/status, GET /api/v1/routes
- Native AOT: CLI publishes as AOT binary
- NuGet: dotnet pack produces Portless.NET.Tool.nupkg
- Shell completion: bash, zsh, fish, powershell (`portless completion <shell>`)
- Build: Directory.Build.props centralized versioning

---

## Estructura de la Solucion (Portless.slnx)
```
Portless.Core/          - Class library, logica compartida
Portless.Cli/           - Console app, CLI entry point (dotnet tool)
Portless.Proxy/         - Web app, proxy YARP (Kestrel)
Portless.Tests/         - xUnit test suite (131+ tests)
Portless.IntegrationTests/ - Integration tests
Portless.E2ETests/      - End-to-end tests
TestApi/                - Test API para proxy testing
completions/            - Static shell completion scripts
```

## Comandos CLI (v2.0)
```
portless run [name] <cmd> --path /api --backend URL
portless list / get <name>
portless alias <name> <port> --remove --path
portless hosts sync/clean
portless up [-f config.yaml]
portless tcp <name> <host:port> --listen <port> --remove
portless proxy start/stop/status
portless cert install/status/uninstall/check/renew
portless completion bash|zsh|fish|powershell
```

## Proxy API Endpoints
```
GET  /health              - Health check
GET  /metrics             - Prometheus metrics
GET  /api/v1/status       - Proxy status (routes, clusters, TCP listeners)
GET  /api/v1/routes       - List all routes
POST /api/v1/add-host     - Add HTTP route (hostname, backendUrl, path, backendUrls, loadBalancePolicy)
DELETE /api/v1/remove-host - Remove HTTP route
POST /api/v1/tcp/add      - Add TCP listener
DELETE /api/v1/tcp/remove - Remove TCP listener
```

## Config File (portless.config.yaml)
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

## Key Services
| Service | Interface | Project | Description |
|---------|-----------|---------|-------------|
| YarpConfigFactory | IYarpConfigFactory | Core | Creates YARP RouteConfig/ClusterConfig |
| PortlessConfigLoader | IPortlessConfigLoader | Core | Loads portless.config.yaml |
| TcpForwardingService | ITcpForwardingService | Core | TCP port forwarding relay |
| PrometheusMetricsService | IMetricsService | Proxy | Prometheus metrics (counters, histograms, gauges) |

## Key Dependencies
- YARP 2.3.0, Spectre.Console 0.53.1, YamlDotNet 16.3.0
- prometheus-net 8.2.1, xUnit 2.9.3, Moq 4.20.72

## Variables de Entorno
| Variable | Default | Descripcion |
|----------|---------|-------------|
| `PORTLESS_PORT` | 1355 | Puerto del proxy HTTP |
| `PORTLESS_HTTPS_ENABLED` | false | Habilitar HTTPS |
| `PORTLESS_STATE_DIR` | ~/.portless | Directorio de estado |
| `PORTLESS` | - | `0` o `skip` para bypass |

## Infraestructura
- **Server:** 194.164.160.125 (Ubuntu)
- **Honcho:** https://honcho.tnzservicios.es (IP-auth, no API key)
  - Workspace: hermes | Peer: hermes-agent

## Perfil del Usuario
- Sergio Tenza, habla espanol
- Prefiere configs minimas, agrega cosas incrementalmente
- Self-hosted/open-source
- Provider LLM preferido: z.ai (ZhipuAI GLM)
