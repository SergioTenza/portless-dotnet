# Changelog

All notable changes to Portless.NET will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned for TIER 2 (v2.0 stable)
- Production deployment features
- Authentication and authorization
- Advanced monitoring dashboards
- Cluster support
- High availability mode

---

## [2.0.0-beta.1] - 2026-04-01

### 🎉 TIER 1 Features (feature/v2.0-dx branch)

Developer experience overhaul with intelligent project detection, framework awareness, and branded error handling.

### ✨ Added

#### New CLI Commands
- `portless get` - Query route information by name or port
- `portless alias` - Manage route aliases for quick access
- `portless hosts sync` - Synchronize routes with system hosts file
- `portless hosts clean` - Remove stale entries from hosts file

#### Project Name Auto-Detection
- **IProjectNameDetector** interface for pluggable name resolution
- Automatic project name detection from `.csproj`, `package.json`, `Cargo.toml`, and other manifest files
- Fallback heuristics based on directory structure

#### Framework Detection
- **IFrameworkDetector** interface with detection for 11 frameworks:
  - ASP.NET Core, Blazor, Express, Next.js, Nuxt, SvelteKit, Remix, FastAPI, Django, Rails, Go net/http
- Framework-specific environment variable injection during `run` command
- Automatic `ASPNETCORE_URLS`, `PORT`, `HOST`, and other framework-specific vars

#### Placeholder Expansion
- `{PORT}` - Resolves to the allocated port number
- `{HOST}` - Resolves to the local hostname
- `{URL}` - Resolves to the full .localhost URL
- `{NAME}` - Resolves to the detected project name
- Placeholders expanded in command arguments, environment variables, and configuration

#### Branded Error Pages
- **404** - Route not found error page with Portless.NET branding
- **502** - Backend unreachable error page with diagnostic hints
- **508** - Loop detected error page for circular route references

#### RunCommand Integration
- Framework-aware environment variable injection (`ASPNETCORE_URLS`, `PORT`, `NODE_ENV`, etc.)
- `PORTLESS_URL` always injected into child process environment
- Seamless integration with detected framework conventions

### 📊 Metrics
- **98 unit tests** passing
- **0 build errors**

---

## [1.3.0] - 2026-03-31

### ✨ Added

#### Platform Parity for Certificate Trust
- **Linux automated trust installation**: CA certificate installed to system trust store via `update-ca-certificates`
- **macOS automated trust installation**: CA certificate added to system keychain via `security` command
- Full parity across Windows, macOS, and Linux for certificate trust management
- `portless cert install` and `portless cert uninstall` now work natively on all platforms

#### Cross-Platform Proxy Process Management
- Unified proxy process lifecycle management across Windows, macOS, and Linux
- Platform-appropriate process spawning and signal handling
- Consistent graceful shutdown behavior across all platforms

#### Route Validation
- Route configuration validation on startup and during hot-reload
- Validation of port ranges, hostname format, and backend URLs
- Clear error messages for invalid route configurations

### 📊 Metrics
- **95 tests** passing
- **0 build errors**

---

## [1.2.0] - 2026-03-17

### 🎉 Major Feature: HTTPS with Automatic Certificates

Portless.NET v1.2 brings automatic HTTPS certificate generation and management for secure local development. No more certificate warnings when developing with HTTPS!

### ✨ Added

#### Automatic Certificate Management
- **Automatic certificate generation**: Wildcard certificates for `*.localhost` generated on-demand
- **Certificate authority (CA) creation**: Local CA created automatically for certificate signing
- **Certificate lifecycle management**: Background monitoring and automatic renewal
- **Certificate trust installation**: One-click Windows trust store integration
- **5-year certificate validity**: Long-lasting certificates for development
- **Cross-platform certificate storage**: Secure PFX format with platform-specific permissions

#### HTTPS Proxy Support
- **Dual HTTP/HTTPS endpoints**: HTTP on port 1355, HTTPS on port 1356
- **Automatic certificate binding**: Certificates bound to HTTPS endpoint automatically
- **TLS 1.2+ enforcement**: Minimum TLS version enforced for security
- **HTTP→HTTPS redirect**: Automatic redirect (308) with API endpoint exclusion
- **Wildcard certificate support**: Single certificate for all `*.localhost` domains

#### Certificate Monitoring and Renewal
- **Startup expiration checks**: Warnings when certificate expires within 30 days
- **Background monitoring service**: Optional background service checks every 6 hours
- **Automatic renewal**: Certificates auto-renew within 30-day expiration window
- **Manual renewal commands**: `portless cert renew` with `--force` flag
- **Certificate status checks**: `portless cert check` with detailed status information

#### New CLI Commands
- `portless proxy start --https` - Start proxy with HTTPS endpoint enabled
- `portless cert install` - Install CA certificate in system trust store (Windows)
- `portless cert status` - Check certificate trust status with verbose details
- `portless cert check` - Check certificate expiration and validity
- `portless cert renew` - Renew certificate manually with optional force flag
- `portless cert uninstall` - Remove CA certificate from trust store (Windows)

#### Mixed Protocol Routing
- **X-Forwarded-Proto header preservation**: Original protocol preserved for backends
- **Simultaneous HTTP/HTTPS backends**: Support for mixed protocol routing
- **YARP SSL validation**: Self-signed certificate acceptance in development mode
- **Forwarded headers middleware**: Complete forwarded headers configuration

#### Documentation
- **Certificate lifecycle guide** (907 lines): Complete certificate management documentation
- **Migration guide** (387 lines): v1.1 to v1.2 upgrade instructions
- **Platform-specific troubleshooting** (345 lines): macOS/Linux manual installation
- **Security considerations** (365 lines): Private key protection and trust implications
- **13 FAQ troubleshooting items**: Common certificate issues and solutions

#### Testing
- **25 new integration tests**: Comprehensive HTTPS feature validation
- **1,838 lines of test code**: Certificate generation, renewal, endpoint, routing tests
- **Platform-specific test guards**: Windows trust status tests with platform detection
- **Test isolation**: IAsyncLifetime for proper cleanup

### 📝 Documentation

- Added comprehensive certificate management documentation
- Added platform-specific installation guides (Windows, macOS, Linux)
- Added security considerations for development certificates
- Added troubleshooting guide for common certificate issues
- Added migration guide from v1.1 HTTP-only to v1.2 HTTPS

### 🔧 Configuration

#### New Environment Variables
- `PORTLESS_HTTPS_ENABLED` - Enable HTTPS endpoint (default: false)
- `PORTLESS_CERT_WARNING_DAYS` - Days before expiration to show warning (default: 30)
- `PORTLESS_CERT_CHECK_INTERVAL_HOURS` - Hours between certificate checks (default: 6)
- `PORTLESS_AUTO_RENEW` - Automatically renew expiring certificates (default: true)
- `PORTLESS_ENABLE_MONITORING` - Enable background monitoring service (default: false)

### ⚠️ Changed

#### Breaking Changes
- **None** - All v1.1 features remain fully functional

#### Deprecated
- `PORTLESS_PORT` environment variable - Fixed ports enforced (HTTP=1355, HTTPS=1356)
- Deprecation warning implemented when `PORTLESS_PORT` is set

### 🐛 Fixed

- Certificate namespace compilation errors in monitoring service
- Missing using directives in certificate services
- Platform guard warnings for Windows-specific features

### 🔒 Security

- Secure PFX certificate storage with platform-specific permissions
- Windows Certificate Store integration requires administrator privileges
- TLS 1.2+ minimum protocol enforcement
- Private key protection documentation
- Development vs production certificate guidance

### 🌐 Platform Support

- **Windows 10+**: Full automated trust installation
- **macOS 12+**: Manual trust installation (documented)
- **Linux**: Manual trust installation (documented for Ubuntu/Debian, Fedora/RHEL, Arch)

### 📊 Metrics

- **33/33 requirements verified** (100%)
- **5/5 phases complete** (100%)
- **25 integration tests** created and passing
- **1,845+ lines of documentation** added
- **~3,500 lines of code** added across services, CLI, and tests

### 🙏 Acknowledgments

This milestone completes the HTTPS vision for Portless.NET, bringing secure local development with automatic certificate management inspired by the original Portless project.

---

## [1.1.0] - 2026-02-20

### ✨ Added

- **HTTP/2 support**: Automatic HTTP/2 with multiplexing and header compression
- **WebSocket support**: Transparent WebSocket proxying for real-time apps
- **SignalR integration**: Full support for ASP.NET Core SignalR
- **Protocol testing guides**: HTTP/2 and WebSocket troubleshooting documentation
- **SignalR chat example**: Working demonstration of real-time chat through proxy

### 🔧 Configuration

- HTTP/2 enabled automatically with ALPN negotiation
- WebSocket connections supported on both HTTP/1.1 and HTTP/2
- Configurable connection timeouts for long-lived WebSocket connections

### 📊 Metrics

- **15/15 requirements verified** (100%)
- **3/3 phases complete** (100%)
- **WebSocket echo server example**
- **SignalR chat example**
- **Protocol troubleshooting guides**

---

## [1.0.0] - 2026-02-15

### 🎉 Initial Release

#### Core Features
- **Stable .localhost URLs**: Replace port numbers with named URLs
- **Automatic port allocation**: Ports 4000-4999 assigned automatically
- **Route persistence**: JSON-based route storage with file locking
- **Process management**: Automatic cleanup of dead routes
- **CLI commands**: `proxy start`, `proxy stop`, `run`, `list`
- **Hot reload**: Routes update without proxy restart
- **Cross-platform**: Windows, macOS, Linux support

#### Architecture
- **.NET 10** with C# 14
- **YARP 2.3.0** reverse proxy
- **Spectre.Console.Cli** CLI framework
- **xUnit** testing framework

#### Testing
- **Unit tests**: YARP routing configuration
- **Integration tests**: CLI commands, process management, port allocation
- **E2E tests**: Tool installation and workflows

### 📊 Metrics

- **18/18 requirements verified** (100%)
- **12/12 phases complete** (100%)
- **Initial stable release** for .NET local development

---

**For more information, see:**
- [Product Requirements Document](PRD.md)
- [Milestone Completion Reports](.planning.archived/milestones/)
- [Migration Guides](docs/migration-v1.1-to-v1.2.md)
