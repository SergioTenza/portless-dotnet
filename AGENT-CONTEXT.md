# Agent Context - Portless.NET

> Documento de contexto para agentes AI que continuan el desarrollo de Portless.NET.
> Ultima actualizacion: 2026-03-31 por Hermes Agent (profile: default)
> Memorias respaldadas en Honcho: https://honcho.tnzservicios.es/v3/workspaces/hermes/peers/hermes-agent/card

---

## Estado del Proyecto

**Version actual:** v1.3.0 RELEASED (2026-03-31)
**Branch:** main
**Tag:** v1.3.0 at commit ae5e547
**GitHub:** https://github.com/SergioTenza/portless-dotnet
**GitHub Release:** https://github.com/SergioTenza/portless-dotnet/releases/tag/v1.3.0

### Build & Tests
- Build: 0 errores, 0 code warnings
- Tests: 76 unitarios + 19 integracion = 95 total, estables (verificados en 3 ejecuciones consecutivas)
- SDK: .NET 10.0.201 en /usr/local/dotnet. **SIEMPRE** ejecutar: `export PATH="/usr/local/dotnet:$PATH"`
- Git user: Sergio Tenza <sergio@tnzservicios.es>

### Warnings By-Design (NO TOCAR)
- `IL2xxx/IL3xxx` - Spectre.Console incompatible con AOT/trimming
- `ASP0000` - BuildServiceProvider, patron estandar en .NET DI

### Estructura de la Solucion (Portless.slnx)
```
Portless.Core/          - Class library, logica compartida
Portless.Cli/           - Console app, CLI entry point (dotnet tool)
Portless.Proxy/         - Web app, proxy YARP (Kestrel)
Portless.Tests/         - xUnit test suite (76 tests)
Portless.IntegrationTests/ - Integration tests (19 tests)
Portless.E2ETests/      - End-to-end tests
TestApi/                - Test API para proxy testing
```

### Comandos CLI Actuales (v1.3)
```
portless proxy start/stop/status  - Gestion del reverse proxy
portless run <name> <cmd>         - Ejecutar app con URL nombrada
portless list                     - Ver rutas activas
portless cert check/status/install/uninstall/renew  - Certificados
```

### Variables de Entorno
| Variable | Default | Descripcion |
|----------|---------|-------------|
| `PORTLESS_PORT` | 1355 | Puerto del proxy HTTP |
| `PORTLESS_HTTPS_ENABLED` | false | Habilitar HTTPS |
| `PORTLESS_STATE_DIR` | ~/.portless | Directorio de estado |
| `PORTLESS` | - | `0` o `skip` para bypass |
| `PORTLESS_CERT_WARNING_DAYS` | 30 | Dias antes de alerta |
| `PORTLESS_CERT_CHECK_INTERVAL_HOURS` | 6 | Frecuencia de check |
| `PORTLESS_AUTO_RENEW` | true | Renovacion automatica |
| `PORTLESS_ENABLE_MONITORING` | false | Monitoreo en segundo plano |

### Logros v1.0 - v1.3
- **v1.0 MVP:** Proxy HTTP/1.1 con YARP, gestion de rutas dinamica, CLI basica
- **v1.1:** HTTP/2 + HTTPS, certificados auto-generados, WebSocket proxy
- **v1.2:** Certificados TLS con CA local, trust store Windows, monitoreo, renovacion
- **v1.3 Platform Parity:** Trust store macOS + Linux, ProxyProcessManager cross-platform, validacion de rutas

### Technical Debt Resuelto (sesion 2026-03-31)
- Test flakiness: routes.json state leaking entre tests (ahora filtra hostnames vacios)
- CertificateTrustTests: esperaba Unknown pero Linux con v1.3 devuelve NotTrusted
- Http2IntegrationTests: no contemplaba PermanentRedirect 308 del middleware HTTPS
- 72 warnings corregidos: CA1416, CS8618, CS8602, CS0649, xUnit2002, CS8631/CS8621
- SYSLIB0057: reemplazado X509Certificate2(byte[]) obsoleto por X509CertificateLoader

---

## Analisis del Original: vercel-labs/portless v0.9.0

### Datos Generales
- **Repositorio:** https://github.com/vercel-labs/portless
- **Version:** 0.9.0, Apache-2.0, por Vercel Labs (@ctate)
- **Lenguaje:** TypeScript (Node.js 20+), monorepo con pnpm + Turborepo
- **Popularidad:** 6,500 estrellas, 207 forks, 40 issues abiertos
- **Dependencias:** Cero runtime dependencies (solo built-ins de Node.js)

### Arquitectura del Original
- Proxy HTTPS en puerto 443 con HTTP/2 + fallback HTTP/1.1
- TLS/HTTP demuxing en un solo puerto (byte-peek: 0x16 = TLS ClientHello)
- Certificados per-hostname via SNI con cache en memoria + disco
- File-based state (routes.json) con directory locking
- Daemon mode via fork+detach
- Generacion de certs via **openssl CLI** (no crypto nativo)

### Estructura de Codigo del Original (~4,700 lineas)
| Archivo | Lineas | Proposito |
|---------|--------|-----------|
| cli.ts | 1757 | CLI principal, parsing de comandos, lifecycle del proxy |
| proxy.ts | 439 | Servidor reverse proxy HTTP/HTTPS |
| routes.ts | 250 | Route store con file locking |
| certs.ts | 771 | Gestion de certificados TLS (CA + per-hostname) |
| auto.ts | 318 | Inferencia de nombre de proyecto + git worktree |
| hosts.ts | 127 | Gestion de /etc/hosts |
| cli-utils.ts | 706 | Port discovery, state management, process spawning |
| pages.ts | 209 | Paginas de error HTML branded |

### Comandos CLI del Original
```
proxy start/stop      - Iniciar/detener proxy (daemon mode, auto-sudo)
run <cmd>             - Inferir nombre y ejecutar via proxy
<name> <cmd>          - Ejecutar con nombre explicito
get <name>            - Obtener URL de un servicio
alias <name> <port>   - Rutas estaticas (Docker, servicios externos)
list                  - Ver rutas activas
trust                 - Agregar CA al trust store del sistema
hosts sync/clean      - Gestion de /etc/hosts
```

### Features del Original que NOS FALTAN
1. **`portless get <name>`** - Retorna URL para scripts (`BACKEND_URL=$(portless get api)`)
2. **`portless alias <name> <port>`** - Rutas estaticas para Docker/servicios externos
3. **`portless hosts sync/clean`** - Gestion de /etc/hosts con bloques marcados
4. **Auto-deteccion de nombre** - Desde package.json/git root/cwd basename
5. **Git worktree detection** - Prefix de branch para linked worktrees
6. **Framework flag injection** - Detecta Vite/Angular/Astro/Next/Expo/React Router e inyecta flags
7. **`PORTLESS_URL` env var** - URL publica inyectada en el proceso hijo
8. **Auto-start del proxy** - Si no corre, lo inicia automaticamente
9. **Custom TLD** - `--tld` para usar `.dev`, `.test`, etc.
10. **Wildcard subdomains** - `--wildcard` para catch-all
11. **Paginas de error branded** - 404 (lista de rutas), 502 (contextual), 508 (loop)
12. **HTTP-to-HTTPS redirect** - Puerto 80 redirige a HTTPS
13. **TLS/HTTP demuxing** - Un solo puerto atiende TLS y plain HTTP
14. **Loop detection** - Header `X-Portless-Hops`, max 5, retorna 508
15. **Daemon mode** - Fork+detach con log file
16. **Custom certificates** - `--cert/--key` para certs propios
17. **Placeholder expansion** - `{PORT}`, `{HOST}`, `{PORTLESS_URL}` en comandos
18. **Process supervision** - Auto-restart de apps caidas

### Framework Detection del Original
| Framework | Deteccion | Flags inyectadas |
|-----------|-----------|-----------------|
| Vite | vite.config.* | `--port {PORT} --strictPort --host` |
| React Router | react-router config | `--port {PORT} --strictPort --host` |
| Astro | astro.config.* | `--port {PORT} --host` |
| Angular | angular.json | `--port {PORT} --host` |
| React Native | metro.config.* | `--port {PORT} --host localhost` |
| Expo | app.json (expo) | `--port {PORT} --host localhost` |

### Top Issues del Original (validacion para nuestro roadmap)
| Issue | Titulo | Relevancia |
|-------|--------|------------|
| #180 | Path-based routing | Feature mas pedido |
| #181 | TCP proxy for databases | Segundo mas pedido |
| #177 | LAN sharing mode | Compartir URLs en red local |
| #174 | Lock contention errors | Paralelismo roto |
| #167 | Tunnel support (ngrok/CF) | Exponer localhost |
| #163 | Docker Compose integration | Orquestacion |
| #178 | Custom env var names + placeholders | Flexibilidad |
| #166 | Shell completion | DX |
| #176 | NX Monorepo support | Monorepos grandes |

---

## Plan v2.0 - Portless.NET

**Documento completo:** `docs/superpowers/plans/v2.0-vision.md`

### Filosofia
No solo alcanzar paridad con el original, sino **superarlo** aprovechando .NET 10 + YARP + Kestrel para features que el original no puede implementar facilmente.

### TIER 1: Developer Experience Completa (4 semanas) - PARIDAD

#### 1.1 CLI Completa (~1 semana)
Nuevos comandos:
- `portless get <name>` - URL de servicio para scripts
- `portless alias <name> <port>` - Rutas estaticas (Docker, servicios externos)
- `portless alias --remove <name>` - Eliminar alias
- `portless hosts sync` - Sincronizar /etc/hosts
- `portless hosts clean` - Limpiar entradas portless

Mejoras:
- Auto-deteccion de nombre desde: .csproj AssemblyName/PackageId > git root > cwd basename
- `--name` override en `portless run`
- Auto-start del proxy si no esta corriendo

#### 1.2 Framework Detection (~1 semana)
Extension del concept del original, adaptado a .NET + multi-lenguaje:

| Framework | Deteccion | Inyeccion |
|-----------|-----------|-----------|
| ASP.NET | *.csproj con Microsoft.AspNetCore | `ASPNETCORE_URLS=http://0.0.0.0:{PORT}` |
| Vite | vite.config.* | `--port {PORT} --strictPort --host` |
| Next.js | next.config.* | `PORT={PORT}` |
| npm/node | package.json con start | `PORT={PORT}` |
| Python | requirements.txt | `PORT={PORT}` |
| Go | go.mod | `PORT={PORT}` |
| Rust | Cargo.toml | `PORT={PORT}` |

Ademas: placeholder expansion en comandos: `{PORT}`, `{HOST}`, `{URL}`

#### 1.3 Paginas de Error Branded (~3 dias)
- 404: Lista de rutas activas con links
- 502: Error contextual (proceso muerto, puerto cerrado)
- 508: Loop detection con instrucciones
- Dark/light mode, design tipo Vercel

### TIER 2: Routing Avanzado (4 semanas) - DIFERENCIADORES

#### 2.1 Path-Based Routing (~3 dias)
El feature mas pedido en el original (#180) que nunca implementaron:
```bash
portless run api dotnet run --path /api
portless run web npm start --path /
```
Implementacion: YARP soporta path-based routing nativamente con `RouteConfig.Match.Path`.

#### 2.2 TCP Proxying (~1 semana)
Segundo feature mas pedido (#181). Acceso a databases/Redis via URLs amigables:
```yaml
services:
  postgres:
    type: tcp
    target: localhost:5432
    proxy: postgres.myapp.localhost:15432
```
Implementacion: Kestrel TCP streams con YARP.

#### 2.3 Multi-Backend Load Balancing (~4 dias)
```bash
portless run api dotnet run --replicas 3
```
YARP round-robin entre instancias. Para testear load balancing real en local.

#### 2.4 Configuration File (~1.5 semanas)
El original solo tiene env vars. Nosotros: declarative config.
```yaml
# portless.config.yaml
proxy:
  port: 443
  https: true
tld: localhost
services:
  api:
    command: dotnet run --project src/Api
    path: /api
  web:
    command: npm start
    path: /
  postgres:
    type: tcp
    port: 5432
    alias: true
```
Comandos: `portless up` / `portless down` / `portless config validate`

### TIER 3: Observabilidad y Distribucion (3 semanas)

#### 2.5 Metrics & Observabilidad (~1 semana)
- Endpoint `/metrics` formato Prometheus
- Contadores: requests/sec, latencia, errores, bytes (por ruta)
- `portless status` con dashboard en terminal (Spectre.Console)
- Structured logging a archivo

#### 2.6 Health Checks (~3 dias)
YARP native health checks:
- Marcar backends como unhealthy
- 502 con info util cuando backend cae
- Auto-retry periodico
- `portless status` muestra salud de cada servicio

#### 3.1 Native AOT Single Binary (~3 dias)
```bash
dotnet publish Portless.Cli -c Release -r win-x64 -p:PublishAot=true
# Output: portless.exe (~15MB, self-contained, no runtime)
```
Windows: portless.exe | Linux: portless | macOS: portless (Universal)

#### 3.2 NuGet Package (~2 dias)
```bash
dotnet tool install --global Portless.DotNet
```

#### 3.3 Shell Completion (~3 dias)
bash, zsh, fish, PowerShell via Spectre.Console extensions.

---

## Ventajas Competitivas vs Portless Node.js

| Aspecto | Portless (Node.js) | Portless.NET |
|---------|-------------------|--------------|
| Runtime | Requiere Node.js 20+ | Single binary (AOT) |
| Crypto | Depende de openssl CLI | Crypto .NET integrado |
| Proxy | Custom 439 lineas | YARP (production-grade) |
| Performance | Single-threaded | Multi-threaded Kestrel |
| HTTP/3 | No | Si (QUIC nativo) |
| gRPC | No | Si (YARP nativo) |
| Path routing | No (feature request) | Si (v2.0) |
| TCP proxy | No (feature request) | Si (v2.0) |
| Load balance | No | Si (YARP, v2.0) |
| Config file | No (solo env vars) | Si (YAML, v2.0) |
| Metrics | No | Si (Prometheus, v2.0) |
| Windows | Parcial (requiere OpenSSL) | Nativo |

---

## Open Questions para v2.0
1. Nombre del binary: `portless` o `portless-dotnet`? (conflicto con original)
2. Config file format: YAML o JSON?
3. LAN mode: v2.0 o v2.1?
4. Tunnel support: built-in o plugin?
5. Git worktree: relevante para publico .NET?

---

## Infraestructura
- **Server:** 194.164.160.125 (Ubuntu)
- **Reverse proxy:** Pangolin (Docker network: pangolin)
- **Dominio:** tnzservicios.es con wildcard subdomains
- **Honcho:** https://honcho.tnzservicios.es (IP-auth, no API key)
  - Workspace ID: `hermes` | Peer ID: `hermes-agent`
  - Leer memorias: `GET /v3/workspaces/hermes/peers/hermes-agent/card`
  - Escribir memorias: `PUT /v3/workspaces/hermes/peers/hermes-agent/card` con body `{"peer_card": ["linea1", ...]}`

## Perfil del Usuario
- Sergio Tenza, habla espanol
- Prefiere configs minimas, agrega cosas incrementalmente
- Prefiere self-hosted/open-source
- Prefiere z.ai (ZhipuAI GLM) como provider LLM
- Gestiona su propia infraestructura VPS con Pangolin
