# Portless.NET Roadmap

> Development-first reverse proxy for .NET

This document outlines the planned evolution of Portless.NET across upcoming major releases.

---

## Current Release: v3.0.0 ✅

**Theme: Extensibility & Observability**

| Feature | Status |
|---------|--------|
| NuGet package on nuget.org | ✅ |
| Hot reload for portless.config.yaml | ✅ |
| Professional README + docs | ✅ |
| CI/CD (GitHub Actions) | ✅ |
| CHANGELOG.md | ✅ |
| 262 tests, 65% coverage | ✅ |
| Docker support (240MB) | ✅ |
| Daemon mode (systemd) | ✅ |
| Native AOT CLI | ✅ |
| Shell completion (bash/zsh/fish/ps) | ✅ |
| Plugin System (SDK, Loader, Middleware, CLI) | ✅ |
| Request Inspector (Middleware, API, CLI, WebSocket) | ✅ |
| 20 new tests (plugin + inspector) | ✅ |

---

## v3.0 - Extensibility & Observability ✅

**Theme: Make the proxy hackable and transparent**

### 3.1 Plugin System

Allow developers to extend Portless with custom middleware without modifying the core.

**Architecture:**
```
~/.portless/plugins/
├── my-plugin/
│   ├── plugin.yaml          # Manifest (name, version, hooks)
│   └── MyPlugin.dll         # Compiled plugin
```

**Plugin manifest (plugin.yaml):**
```yaml
name: request-logger
version: 1.0.0
description: Logs all requests to a file
author: developer
hooks:
  - before-proxy      # Before request is forwarded
  - after-proxy       # After response is received
  - on-route-add      # When a route is registered
  - on-route-remove   # When a route is removed
  - on-error          # When proxy returns error (404, 502, 508)
config:
  logPath: ./logs
  maxFileSize: 10MB
```

**Plugin SDK (Portless.Plugin.SDK NuGet package):**
```csharp
public abstract class PortlessPlugin
{
    public string Name { get; }
    public string Version { get; }
    
    // Lifecycle hooks
    virtual Task OnLoadAsync(IPluginContext context) { }
    virtual Task OnUnloadAsync() { }
    
    // Request pipeline hooks
    virtual Task<ProxyResult?> BeforeProxyAsync(ProxyContext context) { }
    virtual Task AfterProxyAsync(ProxyContext context, ProxyResult result) { }
    
    // Route management hooks
    virtual Task OnRouteAddedAsync(RouteInfo route) { }
    virtual Task OnRouteRemovedAsync(RouteInfo route) { }
    
    // Error hooks
    virtual Task<ErrorResponse?> OnErrorAsync(ErrorContext context) { }
}

public class ProxyContext
{
    public string Hostname { get; }
    public string Path { get; }
    public string Method { get; }
    public Dictionary<string, string> Headers { get; }
    public string? Body { get; }
    public CancellationToken CancellationToken { get; }
}

public class ProxyResult
{
    public int StatusCode { get; }
    public long DurationMs { get; }
    public Dictionary<string, string> ResponseHeaders { get; }
}
```

**CLI commands:**
```bash
portless plugin list                    # List installed plugins
portless plugin install <path-or-url>   # Install a plugin
portless plugin uninstall <name>        # Uninstall a plugin
portless plugin enable <name>           # Enable a disabled plugin
portless plugin disable <name>          # Disable without removing
portless plugin create <name>           # Scaffold a new plugin project
```

**Built-in plugins (shipped with Portless):**
- `header-injector` - Add custom headers to requests/responses
- `cors-handler` - Automatic CORS headers per route
- `request-transformer` - Rewrite paths, query params, headers
- `access-log` - Apache/Nginx-style access logging

**Implementation tasks:**
1. Create `Portless.Plugin.SDK` class library
2. Create `Portless.Plugin.Loader` (runtime plugin loading with AssemblyLoadContext)
3. Add plugin hooks to `Portless.Proxy/Program.cs` middleware pipeline
4. Add `portless plugin *` CLI commands
5. Create `header-injector` built-in plugin
6. Create `cors-handler` built-in plugin
7. Unit + integration tests for plugin lifecycle

---

### 3.2 Request Inspector

A built-in request/response inspector similar to Charles Proxy or Fiddler, accessible from the CLI.

**CLI commands:**
```bash
portless inspect                        # Start inspector (live stream)
portless inspect --filter host:api.*    # Filter by hostname pattern
portless inspect --filter method:POST   # Filter by HTTP method
portless inspect --filter status:5xx    # Filter by status code
portless inspect --save output.jsonl    # Save to file for later analysis
portless inspect --replay               # Replay captured requests
```

**Live terminal UI (Spectre.Console):**
```
┌─ Portless Inspector ──────────────────────────────────────────┐
│ Filter: host:api.* │ 23 requests │ 0 errors │ 45ms avg       │
├──────┬─────────┬──────────────────┬────────┬───────┬──────────┤
│ Time │ Method  │ Host + Path      │ Status │ Size  │ Duration │
├──────┼─────────┼──────────────────┼────────┼───────┼──────────┤
│ 14:23│ GET     │ api.localhost/v1 │ 200    │ 4.2KB │ 23ms     │
│ 14:23│ POST    │ api.localhost/v1 │ 201    │ 156B  │ 45ms     │
│ 14:24│ GET     │ web.localhost/   │ 200    │ 28KB  │ 12ms     │
│ 14:24│ GET     │ api.localhost/v1 │ 502    │ 0B    │ 5000ms   │
│ ...  │         │                  │        │       │          │
├──────┴─────────┴──────────────────┴────────┴───────┴──────────┤
│ [Enter] Details │ [F] Filter │ [S] Save │ [Q] Quit            │
└───────────────────────────────────────────────────────────────┘
```

**Request detail view (on Enter):**
```
┌─ Request Detail #23 ──────────────────────────────────────────┐
│ GET api.localhost/v1/users?id=42 → 200 OK (23ms)             │
├─ Request Headers ─────────────────────────────────────────────┤
│ Host: api.localhost                                           │
│ Accept: application/json                                     │
│ Authorization: Bearer ey***                                   │
├─ Response Headers ────────────────────────────────────────────┤
│ Content-Type: application/json                               │
│ X-Request-Id: abc-123                                        │
├─ Response Body (4.2KB) ──────────────────────────────────────┤
│ {"users":[{"id":42,"name":"Sergio","role":"admin"}]}          │
└───────────────────────────────────────────────────────────────┘
```

**Inspector API (for external tools):**
```
GET /api/v1/inspect/sessions       # List captured sessions
GET /api/v1/inspect/sessions/{id}  # Get session detail
GET /api/v1/inspect/stream         # WebSocket stream of live requests
DELETE /api/v1/inspect/sessions    # Clear captured data
```

**Implementation tasks:**
1. Create `Portless.Core/Services/RequestInspectorService` (captures requests in-memory ring buffer)
2. Add inspector middleware to proxy pipeline (non-blocking, async capture)
3. Add inspector API endpoints to `PortlessApiEndpoints`
4. Create `Portless.Cli/Commands/InspectCommand` with Spectre.Console TUI
5. Add WebSocket stream endpoint for real-time updates
6. Add filters (hostname, method, status, path regex)
7. Add `--save` (JSONL export) and `--replay` functionality
8. Unit + E2E tests

---

### 3.3 Bonus (time permitting)

- **Config validation**: `portless config validate` - lint portless.config.yaml before applying
- **Config include**: support `include:` in portless.config.yaml for mono-repos
- **Request rewriting**: path transforms, header manipulation via config
- **Auto-update**: `portless update` self-update from NuGet

---

## v4.0 - Visual & Ecosystem

**Theme: Make it visual, make it everywhere**

### 4.1 Web Dashboard

A single-page dashboard served by the proxy itself for real-time monitoring.

**Access:** `http://proxy.localhost:1355/_dashboard/`

**Features:**
- Real-time route table with health indicators
- Live request stream (filtered, pausable)
- Response time charts (last 100 requests per route)
- Prometheus metrics rendered as graphs
- Certificate status and expiration warnings
- Config file editor with syntax highlighting and validation
- Plugin management UI (enable/disable/configure)

**Tech stack:**
- Server: Minimal API endpoints on the proxy (already serving /api/v1/*)
- Client: Vanilla HTML + CSS + JS (no Node.js build step, embedded as static files)
- Real-time: Server-Sent Events or WebSocket for live updates
- Charts: Chart.js (CDN, no build)

**Implementation tasks:**
1. Create `Portless.Proxy/wwwroot/dashboard/` with static HTML/CSS/JS
2. Add `/_dashboard/*` endpoint to proxy (excluded from routing)
3. Create dashboard API endpoints: routes, metrics, inspector stream, config editor
4. Build route table view with live health indicators
5. Build request stream view with filters
6. Build metrics charts view
7. Build cert status panel
8. Build config editor with validation
9. E2E tests for dashboard endpoints

---

### 4.2 VS Code Extension

Native integration with VS Code for the best developer experience.

**Features:**
- Auto-detect `portless.config.yaml` in workspace → show Portless sidebar
- Route tree view in sidebar (with health status icons)
- Start/stop proxy from status bar button
- Click route → open in browser
- Config file validation with inline errors
- Config file autocomplete (hostnames, backends, policies)
- Request inspector panel (integrated terminal or webview)
- Auto-register running debug targets as routes

**Tech stack:**
- TypeScript + VS Code Extension API
- Bundled with esbuild
- Communicates with proxy via /api/v1/* HTTP endpoints

**Implementation tasks:**
1. Initialize VS Code extension project (yo code / npm init)
2. Implement portless.config.yaml detection and parsing
3. Build sidebar tree view for routes
4. Add status bar proxy start/stop button
5. Add config file validation (JSON Schema)
6. Add autocomplete for config file
7. Add request inspector webview panel
8. Publish to VS Code Marketplace

---

### 4.3 Ecosystem Integrations

- **Docker Compose generation**: `portless compose` generates docker-compose.yml from config
- **Kubernetes sidecar**: Minimal Dockerfile for injecting Portless as sidecar in pods
- **Traefik labels**: Generate Traefik-compatible labels from portless.config.yaml
- **Migration tools**: `portless migrate nginx|traefik|caddy` converts foreign configs

---

## Timeline (estimacion orientativa)

| Version | Theme | Key Deliverables | ETA |
|---------|-------|-----------------|-----|
| v2.1 ✅ | Publish & Polish | NuGet, hot reload, CI/CD, README | Done |
| v3.0 ✅ | Extensibility | Plugin system, request inspector | Done |
| v3.x | Polish | Config validation, includes, rewriting | 2-3 semanas |
| v4.0 | Visual | Web dashboard, VS Code extension | 6-8 semanas |
| v4.x | Ecosystem | Docker Compose gen, K8s, migrations | 3-4 semanas |

---

## Principios de Diseño

1. **Dev experience first** - Cada feature debe mejorar el flujo del developer
2. **Zero config by default** - Funciona sin configuracion, se configura cuando se necesita
3. **Incremental complexity** - Features simples primero, avanzadas opt-in
4. **AOT compatible** - Todo el core y proxy deben funcionar con Native AOT
5. **Cross-platform** - Windows, macOS, Linux desde dia uno
6. **Minimal dependencies** - Preferir .NET BCL sobre paquetes externos
7. **Observable by default** - Todo es visible: logs, metrics, inspector
8. **Extensible by design** - Plugins antes que features hardcoded

---

*Last updated: 2026-04-08 by Hermes Agent*
