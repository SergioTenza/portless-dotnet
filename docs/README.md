# Portless.NET Documentation

Welcome to the Portless.NET documentation. Portless.NET provides stable .localhost URLs for local .NET development.

## What's New in v1.2

### HTTPS Support with Automatic Certificates

Portless.NET v1.2 adds HTTPS support with automatic certificate generation:

- **Automatic certificates** for `*.localhost` domains (5-year validity)
- **Dual endpoints**: HTTP (1355) and HTTPS (1356)
- **Certificate management**: install, status, uninstall commands
- **Automatic renewal** within 30 days of expiration
- **Zero configuration** - certificates generated on first HTTPS start

**Quick start:**
```bash
# Start proxy with HTTPS
portless proxy start --https

# Install CA certificate (Windows - automatic)
portless cert install

# Verify trust status
portless cert status
```

See [Certificate Management](#certificate-management) for complete documentation.

---

## Quick Start

1. [Install Portless.NET](../../scripts/install.sh) (Linux/macOS) or [install.ps1](../../scripts/install.ps1) (Windows)
2. Start the proxy: `portless proxy start`
3. Run your app: `portless myapp dotnet run`
4. Access via: `http://myapp.localhost`

## Tutorials

Step-by-step guides for common scenarios:

- [Tutorial 1: Migrating an Existing Project](tutorials/01-migration.md)
  - Add PORT integration to existing projects
  - Update launchSettings.json
  - Common migration issues

- [Tutorial 2: Creating a New Project](tutorials/02-from-scratch.md)
  - Start with Portless from day one
  - Configure new projects correctly
  - Best practices

- [Tutorial 3: Microservices Scenario](tutorials/03-microservices.md)
  - Run multiple services with Portless
  - Service-to-service communication
  - Managing complex architectures

- [Tutorial 4: E2E Testing](tutorials/04-e2e-testing.md)
  - Stable URLs for reliable tests
  - Test automation integration
  - CI/CD setup

## Integration Guides

Reference documentation for specific configuration options:

- [launchSettings.json Integration](integration/launch-settings.md)
  - Profile configuration
  - Dynamic port assignment
  - Environment variables

- [appsettings.json Integration](integration/appsettings.md)
  - Configuration-based setup
  - Kestrel endpoints
  - Environment-specific settings

- [Kestrel Configuration](integration/kestrel-configuration.md)
  - Code-based configuration
  - UseUrls vs ConfigureKestrel
  - HTTPS setup

## Examples

See the [Examples](../../Examples/) directory for complete working examples:
- **WebApi** - ASP.NET Core Web API
- **BlazorApp** - Blazor Web App
- **WorkerService** - Background service
- **ConsoleApp** - Console application

## Certificate Management

Portless.NET provides automatic HTTPS certificate generation and lifecycle management for local development.

### Quick Start

```bash
# Enable HTTPS (automatic certificate generation)
portless proxy start --https

# Install CA certificate to trust store (Windows)
portless cert install

# Check certificate status
portless cert check

# Verify trust status
portless cert status
```

### Documentation

- [Certificate Lifecycle Management](certificate-lifecycle.md)
  - Certificate status commands (check, renew)
  - **Certificate trust commands (install, status, uninstall)**
  - Automatic monitoring configuration
  - Environment variables
  - **Comprehensive troubleshooting guide**

- [Migration Guide v1.1 to v1.2](migration-v1.1-to-v1.2.md)
  - What's new in v1.2
  - Breaking changes (none!)
  - Upgrading from HTTP-only to HTTPS
  - New certificate management features

- [Certificate Security Considerations](certificate-security.md)
  - Private key protection
  - Trust implications
  - Development vs production certificates
  - Security best practices

- [Platform-Specific Installation](certificate-troubleshooting-macos-linux.md)
  - macOS manual installation steps
  - Linux manual installation steps (Ubuntu, Fedora, Arch)
  - Firefox NSS database configuration

### Platform Support

> **⚠️ Platform Availability**
>
> - **v1.2 (Current):** Windows — Automatic trust installation
> - **macOS/Linux:** Manual installation required (automatic coming in v1.3)
>
> See [Platform-Specific Installation](certificate-troubleshooting-macos-linux.md) for manual steps.

### Certificate Commands

- `portless cert install` - Install CA certificate to system trust
- `portless cert status` - Display certificate trust status
- `portless cert check` - Check certificate expiration status
- `portless cert renew` - Renew certificate (automatic or manual)
- `portless cert uninstall` - Remove CA certificate from trust store

---

## CLI Reference

Common commands:
- `portless proxy start` - Start the proxy
- `portless proxy start --https` - Start proxy with HTTPS enabled **[NEW in v1.2]**
- `portless proxy stop` - Stop the proxy
- `portless proxy status` - Check proxy status
- `portless list` - List active routes
- `portless <hostname> <command>` - Run app with URL

**Certificate commands:**
- `portless cert install` - Install CA certificate to system trust **[NEW in v1.2]**
- `portless cert status` - Display certificate trust status **[NEW in v1.2]**
- `portless cert check` - Check certificate expiration status **[NEW in v1.2]**
- `portless cert renew` - Renew certificate **[NEW in v1.2]**
- `portless cert uninstall` - Remove CA certificate from trust store **[NEW in v1.2]**

See `portless --help` for full command reference.

## Troubleshooting

**Proxy not running?**
- Run: `portless proxy status`
- Start: `portless proxy start`

**App not using assigned port?**
- Check Program.cs: Ensure `UseUrls()` is called before `Build()`
- See: [Integration Guides](#integration-guides)

**hostname.localhost not resolving?**
- Ensure proxy is running: `portless proxy status`
- Check route exists: `portless list`
- See: [Tutorial 1 Troubleshooting](tutorials/01-migration.md#troubleshooting)

**HTTPS certificate warnings?**
- Install CA certificate: `portless cert install` (Windows)
- Check trust status: `portless cert status`
- See: [Certificate Lifecycle Management](certificate-lifecycle.md#troubleshooting)

## Migration Guides

- [Migration Guide v1.1 to v1.2](migration-v1.1-to-v1.2.md) - Upgrading to HTTPS support
- [Migration Guide v1.0 to v1.1](migration-v1.0-to-v1.1.md) - Upgrading to HTTP/2 and WebSocket

## Contributing

Found an issue or have a suggestion? Please open an issue on GitHub.

## License

Portless.NET is licensed under the MIT License.
