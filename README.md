<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet" alt=".NET 10" />
</p>

<h1 align="center">Portless.NET</h1>

<p align="center">
  <strong>Stable <code>.localhost</code> URLs for local development — no more port numbers.</strong>
</p>

<p align="center">
  <a href="#quick-start">Quick Start</a> •
  <a href="#commands">Commands</a> •
  <a href="#configuration">Configuration</a> •
  <a href="#docker">Docker</a> •
  <a href="#development">Development</a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet" alt=".NET 10" />
</p>

[![Build](https://github.com/SergioTenza/portless-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/SergioTenza/portless-dotnet/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Portless.NET.Tool.svg)](https://www.nuget.org/packages/Portless.NET.Tool/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey.svg)](#cross-platform)

---

## What is Portless.NET?

**Portless.NET** is a `dotnet tool` that eliminates port conflicts in local development by giving every service a stable, named URL. Instead of remembering whether your API runs on `localhost:3001`, `localhost:8080`, or `localhost:5000`, you just use:

```
http://myapi.localhost
```

It runs a lightweight reverse proxy (powered by Microsoft YARP) on your machine, auto-assigns free ports to your apps, and routes named `.localhost` domains to them. Works with any backend — .NET, Node.js, Python, Go, Docker containers, and more.

Inspired by [Portless](https://github.com/portless/portless) (Node.js), rebuilt with .NET 10 + YARP for native Windows support, better performance, and first-class .NET integration.

---

## Quick Start

```bash
# 1. Install the tool
dotnet tool install -g Portless.NET.Tool

# 2. Run your app with a name
portless run myapi dotnet run

# 3. Open in browser
# http://myapi.localhost
```

That's it. The proxy starts automatically on first use.

---

## Features

- **Named `.localhost` URLs** — `http://myapi.localhost` instead of `http://localhost:5000`
- **Automatic proxy management** — proxy starts on demand, no manual setup
- **Auto port allocation** — free ports assigned automatically, no conflicts
- **Multiple backends & load balancing** — RoundRobin, FirstAlphabetical, PowerOfTwoStrategies
- **Path-based routing** — route `/api` to one service, `/app` to another on the same host
- **TCP proxying** — proxy databases and non-HTTP services (Redis, PostgreSQL, etc.)
- **Static aliases** — map names to external services and Docker containers
- **HTTPS with auto certificates** — `https://` with generated certs, zero config
- **HTTP/2 support** — multiplexing, header compression, gRPC-ready
- **WebSocket & SignalR** — real-time communication through the proxy
- **Config-as-code** — `portless.config.yaml` for declarative route management
- **Daemon mode** — run as a systemd user service (Linux)
- **Shell completion** — bash, zsh, fish
- **Hosts file management** — sync/clean `/etc/hosts` entries
- **Cross-platform** — Windows 10+, macOS 12+, Linux (Ubuntu, Debian, Fedora, Arch)
- **Framework detection** — auto-injects `PORT`, `ASPNETCORE_URLS`, and more

---

## Installation

### From NuGet (recommended)

```bash
dotnet tool install -g Portless.NET.Tool
```

### From Source

```bash
git clone https://github.com/SergioTenza/portless-dotnet.git
cd portless-dotnet
dotnet build Portless.slnx --configuration Release
dotnet pack Portless.Cli/Portless.Cli.csproj -o ./nupkg --configuration Release
dotnet tool install --add-source ./nupkg -g Portless.NET.Tool
```

### Verify

```bash
portless --help
portless proxy status
```

---

## Commands

### Proxy Management

| Command | Description |
|---------|-------------|
| `portless proxy start [--port PORT] [--https]` | Start the proxy server (default port: 1355) |
| `portless proxy stop` | Stop the proxy server |
| `portless proxy status` | Check if proxy is running |

### App Management

| Command | Description |
|---------|-------------|
| `portless run <name> <command...>` | Run an app with a named URL |
| `portless run <name> <command...> --path /api` | Run with path-based routing |
| `portless run <name> <command...> --backend http://host:port` | Add backends for load balancing |
| `portless r <name> <command...>` | Shortcut for `run` |
| `portless list` | List all active routes |
| `portless get <name>` | Get the URL for a named service |

### Static Aliases

| Command | Description |
|---------|-------------|
| `portless alias <name> <port> [--host HOST] [--protocol PROTO]` | Create a static route to an external service |
| `portless alias <name> --remove` | Remove a static alias |

### Config-Based Routing

| Command | Description |
|---------|-------------|
| `portless up [-f config.yaml]` | Register all routes from a config file |

### TCP Proxying

| Command | Description |
|---------|-------------|
| `portless tcp <name> <host:port> --listen <port>` | Create a TCP proxy (e.g., for databases) |
| `portless tcp <name> --remove` | Remove a TCP proxy |

### Certificate Management

| Command | Description |
|---------|-------------|
| `portless cert install` | Install CA certificate to system trust store |
| `portless cert status [--verbose]` | Display certificate trust status |
| `portless cert check [--verbose]` | Check certificate expiration and validity |
| `portless cert renew [--force]` | Renew certificate (auto-renews if expiring soon) |
| `portless cert uninstall` | Remove CA certificate from system trust store |

### Daemon Mode (Linux)

| Command | Description |
|---------|-------------|
| `portless daemon install [--https] [--enable]` | Install proxy as a systemd user service |
| `portless daemon uninstall` | Stop and remove the systemd service |
| `portless daemon status` | Show daemon service status |
| `portless daemon enable` | Enable auto-start on boot |
| `portless daemon disable` | Disable auto-start on boot |

### Hosts File

| Command | Description |
|---------|-------------|
| `portless hosts sync` | Sync `/etc/hosts` entries for active routes |
| `portless hosts clean` | Remove portless entries from `/etc/hosts` |

### Shell Completion

| Command | Description |
|---------|-------------|
| `portless completion bash` | Generate bash completion script |
| `portless completion zsh` | Generate zsh completion script |
| `portless completion fish` | Generate fish completion script |

---

## Configuration

Create a `portless.config.yaml` in your project root:

```yaml
routes:
  # Simple HTTP route
  - host: myapi.localhost
    backends: ["http://localhost:5000"]
    path: /api

  # Multiple backends with load balancing
  - host: frontend.localhost
    backends: ["http://localhost:3000", "http://localhost:3001"]
    loadBalancePolicy: RoundRobin

  # TCP proxy for databases
  - host: db.localhost
    type: tcp
    listenPort: 5432
    backends: ["localhost:5432"]
```

Then start all routes at once:

```bash
portless up
```

### Config File Discovery

Portless searches for `portless.config.yaml` in the current directory and parent directories. You can also specify a custom path:

```bash
portless up -f ./deploy/local-routes.yaml
```

---

## Docker

Portless.NET can be containerized alongside your services:

### Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish Portless.Proxy/Portless.Proxy.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 1355
ENTRYPOINT ["dotnet", "Portless.Proxy.dll", "--urls", "http://0.0.0.0:1355"]
```

### Docker Compose

```yaml
version: "3.8"
services:
  proxy:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "1355:1355"
    volumes:
      - ./portless.config.yaml:/app/portless.config.yaml

  api:
    build: ./src/MyApi
    environment:
      - PORT=5000

  frontend:
    build: ./src/Frontend
    environment:
      - PORT=3000
```

### Use with External Containers

```bash
# Start your containers
docker compose up -d

# Alias external services
portless alias api 5000
portless alias frontend 3000
portless alias redis 6379 --host docker-host

# Or use TCP proxying for databases
portless tcp postgres localhost:5432 --listen 15432
```

---

## Architecture

```
  Browser / Client
        |
        v
  ┌─────────────────────────────────────┐
  │        Portless.NET Proxy           │
  │        (Kestrel + YARP)             │
  │        :1355 (HTTP) / :1356 (HTTPS) │
  │                                     │
  │  ┌───────────────────────────────┐  │
  │  │       Route Table             │  │
  │  │  myapi.localhost   -> :5024   │  │
  │  │  frontend.localhost -> :3001  │  │
  │  │  db.localhost (TCP) -> :5432  │  │
  │  └───────────────────────────────┘  │
  └──────────┬──────────┬───────────────┘
             │          │
        ┌────v───┐ ┌───v────┐
        │  API   │ │Frontend│
        │ :5024  │ │ :3001  │
        └────────┘ └────────┘
```

### Tech Stack

| Component | Technology |
|-----------|-----------|
| CLI | Spectre.Console.Cli 0.53 |
| Proxy Engine | YARP 2.3 (Yet Another Reverse Proxy) |
| HTTP Server | Kestrel (.NET 10) |
| Metrics | prometheus-net 8.2 |
| Language | C# 14 / .NET 10 |
| AOT | Native AOT published binaries |

### Project Structure

```
portless-dotnet/
├── Portless.Core/              # Shared core logic (routing, ports, persistence)
├── Portless.Cli/               # CLI entry point (dotnet tool)
├── Portless.Proxy/             # YARP proxy server (Kestrel)
├── Portless.Tests/             # Unit tests (xUnit)
├── Portless.IntegrationTests/  # Integration tests (CLI, processes)
├── Portless.E2ETests/          # End-to-end tests (tool install, workflows)
├── Examples/                   # Sample apps (WebApi, SignalR, WebSocket, Blazor)
├── TestApi/                    # Test API for development
└── docs/                       # Documentation
```

---

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `PORTLESS_PORT` | Proxy HTTP port | `1355` |
| `PORTLESS_HTTPS_ENABLED` | Enable HTTPS endpoint | `false` |
| `PORTLESS_CERT_WARNING_DAYS` | Days before expiration warning | `30` |
| `PORTLESS_CERT_CHECK_INTERVAL_HOURS` | Hours between certificate checks | `6` |
| `PORTLESS_AUTO_RENEW` | Auto-renew expiring certificates | `true` |
| `PORTLESS_ENABLE_MONITORING` | Enable background monitoring | `false` |

---

## Development

### Prerequisites

- .NET 10 SDK
- Git

### Build

```bash
dotnet build Portless.slnx --configuration Release
```

### Run Tests

```bash
# All tests
dotnet test

# By suite
dotnet test Portless.Tests/Portless.Tests.csproj                    # Unit
dotnet test Portless.IntegrationTests/Portless.IntegrationTests.csproj  # Integration
dotnet test Portless.E2ETests/Portless.E2ETests.csproj              # E2E

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Pack & Install Locally

```bash
dotnet pack Portless.Cli/Portless.Cli.csproj -o ./nupkg --configuration Release
dotnet tool install --add-source ./nupkg -g Portless.NET.Tool
```

### Manual Smoke Test

```bash
portless --help
portless proxy start
portless run testapi dotnet run --project TestApi
portless list
curl http://testapi.localhost
portless proxy stop
```

### Contributing

Contributions are welcome! This project is under active development.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes (`git commit -m 'Add my feature'`)
4. Push to the branch (`git push origin feature/my-feature`)
5. Open a Pull Request

Please ensure all tests pass before submitting:

```bash
dotnet test
```

---

## Cross-Platform Support

| Platform | Status | Notes |
|----------|--------|-------|
| Windows 10+ | ✅ Full support | Certificate auto-trust, full CLI |
| macOS 12+ | ✅ Full support | Keychain integration |
| Linux (Ubuntu/Debian) | ✅ Full support | systemd daemon mode |
| Linux (Fedora/RHEL) | ✅ Full support | certutil-based trust |
| Linux (Arch) | ✅ Full support | Manual trust install |

---

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

---

## Credits

- **[Portless](https://github.com/portless/portless)** — Original Node.js project and design inspiration
- **[YARP](https://microsoft.github.io/reverse-proxy/)** — Microsoft's reverse proxy engine powering the routing
- **[Spectre.Console](https://spectreconsole.net/)** — Beautiful console CLI framework
- .NET community for feedback and support

---

<p align="center">
  Built with ❤️ by <a href="https://github.com/SergioTenza">Sergio Tenza</a> and contributors
</p>
