# Portless.NET Documentation

Welcome to the Portless.NET documentation. Portless.NET provides stable .localhost URLs for local .NET development.

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

## CLI Reference

Common commands:
- `portless proxy start` - Start the proxy
- `portless proxy stop` - Stop the proxy
- `portless proxy status` - Check proxy status
- `portless list` - List active routes
- `portless <hostname> <command>` - Run app with URL

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

## Contributing

Found an issue or have a suggestion? Please open an issue on GitHub.

## License

Portless.NET is licensed under the MIT License.
