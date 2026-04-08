# Changelog

All notable changes to Portless.NET will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.0.0] - 2026-04-08

### Added - Plugin System
- **Portless.Plugin.SDK**: NuGet package for building custom plugins (dependency-free, AOT-compatible)
- **PluginLoader**: Runtime plugin loading with AssemblyLoadContext (collectible for hot-reload)
- **PluginMiddleware**: Before/after proxy hooks, route management hooks, error hooks
- **Plugin CLI**: `portless plugin list/install/uninstall/create/enable/disable`
- **Plugin API**: GET /api/v1/plugins, POST /api/v1/plugins/reload
- **Built-in plugin**: header-injector as reference implementation
- Plugin manifests via plugin.yaml (name, version, hooks, config, entry assembly)
- Plugin sandboxing: one bad plugin doesn't crash the proxy

### Added - Request Inspector
- **InspectorMiddleware**: Captures every proxied request/response into in-memory ring buffer
- **RequestInspectorService**: Thread-safe ring buffer (default 1000 requests, configurable)
- **Inspector API**: GET/DELETE /api/v1/inspect/sessions, GET /api/v1/inspect/stats
- **Inspector WebSocket**: GET /api/v1/inspect/stream for real-time request streaming
- **Inspector CLI**: `portless inspect` with Spectre.Console TUI table, filtering, JSONL export
- Body capture for text-based content types (JSON, HTML, XML, < 1MB)
- Header sanitization (Authorization header masked in plugin context)
- Status color coding (2xx green, 3xx blue, 4xx yellow, 5xx red)

### Changed
- Updated middleware pipeline order: WebSockets → Inspector → Plugin → Logging → Proxy
- Plugin types moved to dedicated Portless.Plugin.SDK package
- 20 new unit tests (10 plugin + 10 inspector)

### Project Structure
- New project: `Portless.Plugin.SDK/` (plugin author SDK)
- New project: `Portless.BuiltinPlugins/HeaderInjectorPlugin/` (reference plugin)
- New services: PluginLoader, RequestInspectorService
- New middleware: PluginMiddleware, InspectorMiddleware
- New CLI commands: plugin, inspect

## [2.1.0] - 2026-04-08

### Added
- **Hot reload for portless.config.yaml**: `ConfigFileWatcher` automatically detects config file changes and reloads YARP routes at runtime with 500ms debounce
- **GitHub Actions CI/CD**: `ci.yml` (build + test + coverage on push) and `release.yml` (auto-release on tag with NuGet package)
- **Professional README**: Badges, quickstart, full command reference, config examples, architecture diagram, development guide
- 9 new unit tests for config file hot reload

### Changed
- Config routes now use `"config-"` prefix for route IDs to distinguish from dynamic CLI routes
- Improved startup config loading to use consistent route ID naming

## [2.0.0] - 2026-04-08

### Added - TIER 1: Developer Experience
- Smart CLI with auto-detection, framework detection (ASP.NET, Vite, Next.js, Astro, Angular, Expo, React Native, npm, Python, Go, Rust)
- Placeholder expansion (`{PORT}`, `{HOST}`)
- Auto project naming from `.csproj`, git, or directory
- Branded error pages (404, 502, 508)
- Shell completion for bash, zsh, fish, PowerShell (`portless completion <shell>`)

### Added - TIER 2: Advanced Routing
- Path-based routing with `--path` flag
- TCP proxying via `TcpForwardingService` relay
- Multi-backend load balancing (round-robin, least requests, random, power-of-two-choices, first)
- Configuration file support (`portless.config.yaml` + `portless up`)

### Added - TIER 3: Observability & Distribution
- Health check endpoint: `GET /health`
- Prometheus metrics endpoint: `GET /metrics` (request counters, histograms, gauges)
- Status API: `GET /api/v1/status`, `GET /api/v1/routes`
- Native AOT support (CLI publishes as AOT binary)
- NuGet package: `Portless.NET.Tool`
- Centralized version management via `Directory.Build.props`

### Added - Quality & Infrastructure
- E2E test suite (14 real workflow tests)
- Unit tests for all 16 CLI commands (262 total tests, 0 failures)
- Code coverage pipeline (65.6% line, 50.4% branch, 77.4% method)
- Daemon mode: systemd user service (`portless daemon install/uninstall/status/enable/disable`)
- Docker support: multi-stage Dockerfile (240MB), docker-compose.yml
- Certificate management: generate, install, renew, trust (Linux, macOS, Windows)
- All IL/AOT trimming warnings eliminated in production code (Core, Cli, Proxy)

### Changed
- Migrated to .NET 10 with C# 14
- YARP 2.3.0 as reverse proxy engine
- Spectre.Console.Cli 0.53.1 for CLI framework
- Source-generated JSON serialization (`PortlessJsonContext`)

## [1.3.0] - 2026-03-31

### Added
- Certificate trust management for Linux and macOS
- Cross-platform certificate trust service factory
- Certificate permission service for Linux
- CLI cert commands: check, install, renew, status, uninstall

### Fixed
- Certificate generation and renewal
- Certificate trust installation on Linux

## [1.0.0] - 2026-03-30

### Added
- Initial release
- Basic reverse proxy with YARP
- CLI with proxy start/stop/status
- Route management via API
- Dynamic configuration with hot-reload
- .localhost domain support
