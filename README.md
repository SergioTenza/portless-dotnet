# Portless.NET

> Reemplaza números de puerto con URLs estables y con nombre en `.localhost` para desarrollo local

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10-purple.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey.svg)](https://github.com/SergioTenza/portless-dotnet)
[![HTTP/2](https://img.shields.io/badge/HTTP%2F2-supported-brightgreen.svg)](docs/http2-websocket-guide.md)
[![WebSocket](https://img.shields.io/badge/WebSocket-supported-brightgreen.svg)](docs/http2-websocket-guide.md)

> **🎉 What's New in v1.1**
>
> - HTTP/2 support for improved performance
> - WebSocket proxying for real-time apps
> - SignalR chat example
> - Protocol testing and troubleshooting guides
>
> See [🌐 HTTP/2 and WebSocket Support](#-http2-and-websocket-support) below.

## 🎯 ¿Qué es Portless.NET?

**Portless.NET** es una herramienta `dotnet tool` que elimina los conflictos de puerto en desarrollo local proporcionando URLs estables y nombradas. En lugar de recordar si tu app está en `localhost:3001`, `localhost:8080` o `localhost:5000`, simplemente usas `http://miapp.localhost:1355`.

### Inspirado en [Portless](https://github.com/portless/portless)

Portless.NET es un port a .NET 10 del excelente [Portless](https://github.com/portless/portless) (Node.js), usando [YARP](https://microsoft.github.io/reverse-proxy/) (Yet Another Reverse Proxy) de Microsoft como motor de proxy inverso.

## 🌐 HTTP/2 and WebSocket Support

**Portless.NET v1.1** now supports advanced protocols for improved performance and real-time communication.

### HTTP/2 Benefits

- **Multiplexing**: Multiple concurrent requests over a single connection
- **Header compression**: Reduced bandwidth overhead
- **Better performance**: Especially for services with many small requests
- **Automatic negotiation**: HTTP/2 activates when supported by client

### When to Use HTTP/2

HTTP/2 is most beneficial for:
- **Microservices**: Many small requests between services
- **API gateways**: Multiple concurrent calls to backend services
- **Modern browsers**: Automatically use HTTP/2 when available
- **gRPC services**: HTTP/2 is required for gRPC

**Note:** HTTP/2 is automatically enabled. No configuration needed.

### Verifying HTTP/2 is Active

```bash
# Test HTTP/2 with curl
curl -I --http2 http://miapi.localhost:1355

# Response should include:
# HTTP/2 200
```

In browser DevTools (F12 → Network tab), look for "h2" in the Protocol column.

### WebSocket Support

- **Real-time communication**: Chat apps, live updates, notifications
- **SignalR integration**: Full support for ASP.NET Core SignalR
- **Long-lived connections**: Stable connections for extended periods
- **Transparent proxying**: Works seamlessly through the proxy

**Works with:** HTTP/1.1 WebSocket upgrade (RFC 6455) and HTTP/2 WebSocket (RFC 8441)

### WebSocket Examples

Portless.NET supports WebSockets transparently. Your WebSocket apps work without modification.

**Example: SignalR Chat**

```bash
# Start the proxy
portless proxy start

# Run the SignalR chat example (see Examples/ directory)
portless run chat dotnet run --project Examples/SignalRChat

# Connect multiple clients at:
# http://chat.localhost:1355
```

**Example: WebSocket Echo Server**

```bash
# Run the echo server example
portless run echo dotnet run --project Examples/WebSocketEchoServer

# Test with a WebSocket client
# Messages sent will be echoed back
```

**Long-lived Connections**

WebSocket connections remain stable for extended periods. The proxy is configured with:
- `KeepAliveTimeout`: 10 minutes (configurable)
- `RequestHeadersTimeout`: 30 seconds
- Proper upgrade handling for both HTTP/1.1 and HTTP/2

### Quick Start: Testing HTTP/2 and WebSockets

**1. Verify HTTP/2 is working**

```bash
# Terminal 1: Start proxy
portless proxy start

# Terminal 2: Run any app
portless run testapi dotnet run --project Examples/WebApi

# Terminal 3: Test with curl
curl -I --http2 http://testapi.localhost:1355
# Look for "HTTP/2 200" in response
```

**2. Test WebSocket echo server**

```bash
# Terminal 1: Start proxy (if not running)
portless proxy start

# Terminal 2: Run echo server
portless run echo dotnet run --project Examples/WebSocketEchoServer

# Browser: Open http://echo.localhost:1355
# Use the test page to send WebSocket messages
```

**3. Try SignalR chat**

```bash
# Terminal 1: Start proxy
portless proxy start

# Terminal 2: Run chat server
portless run chat dotnet run --project Examples/SignalRChat

# Browser: Open http://chat.localhost:1355 in multiple tabs
# Chat messages should appear in all tabs in real-time
```

For more details, see:
- [HTTP/2 and WebSocket Testing Guide](docs/http2-websocket-guide.md)
- [SignalR Troubleshooting Guide](docs/signalr-troubleshooting.md)
- [Examples README](Examples/README.md)

### ✨ Ventajas

- ✅ **Soporte Windows nativo** (a diferencia de Portless original)
- ✅ **Mejor rendimiento** con .NET 10 + Kestrel + HTTP/2
- ✅ **Integración nativa** con ecosistema .NET
- ✅ **HTTP/2** con multiplexing y header compression
- ✅ **WebSocket** para comunicación en tiempo real
- ✅ **SignalR** soporte completo para ASP.NET Core SignalR

## 🚀 Quick Start

### Instalación

```bash
# Instalar desde NuGet.org (cuando esté publicado)
dotnet tool install -g portless.dotnet

# O instalar desde fuente local
dotnet tool install --add-source . -g portless.dotnet
```

### Uso Básico

```bash
# Inicia el proxy (una sola vez)
portless proxy start

# Ejecuta tu app con un nombre
portless run miapi dotnet run

# Accede a tu app en:
# http://miapi.localhost:1355
```

### Múltiples Servicios

```bash
# Monorepo con múltiples servicios
portless run orders dotnet run --project services/Orders
portless run products dotnet run --project services/Products
portless run frontend dotnet run --project web/Frontend

# Accede a cada servicio:
# http://orders.localhost:1355
# http://products.localhost:1355
# http://frontend.localhost:1355
```

### Listar Apps Activas

```bash
portless list
```

Output:

```
Active routes:

  http://orders.localhost:1355    ->    localhost:4234    (pid 12345)
  http://products.localhost:1355  ->    localhost:4456    (pid 12346)
  http://frontend.localhost:1355  ->    localhost:4123    (pid 12347)
```

## 📚 Comandos

```bash
portless proxy start [--port <PORT>]    # Inicia proxy (default: puerto 1355)
portless proxy stop                      # Detiene proxy
portless run <name> <command...>         # Ejecuta app con URL nombrada
portless r <name> <command...>           # Alias corto de 'run'
portless list                            # Lista apps activas
```

## 🔧 Integración con ASP.NET Core

Portless.NET inyecta la variable de entorno `PORT` con el puerto asignado. Tu aplicación debe configurarse para usar esta variable.

### En `launchSettings.json`

```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "environmentVariables": {
        "ASPNETCORE_URLS": "http://0.0.0.0:${PORT}"
      }
    }
  }
}
```

### En `appsettings.json`

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:${PORT}",
        "Protocols": "Http1AndHttp2"
      }
    }
  }
}
```

## 🏗️ Arquitectura

Portless.NET está construido con:

- **.NET 10** con C# 14
- **YARP 2.3.0** - Reverse proxy de Microsoft
- **Spectre.Console.Cli 0.53.1** - CLI framework
- **Microsoft.Extensions.Logging** - Logging estructurado

### Estructura del Proyecto

```
portless-dotnet/
├── Portless.Core/              # Lógica central compartida
├── Portless.Cli/               # CLI entry point (dotnet tool)
├── Portless.Proxy/             # Proxy YARP (Kestrel)
├── Portless.Tests/             # Unit tests (xUnit)
├── Portless.IntegrationTests/  # Integration tests (CLI, procesos, puertos)
├── Portless.E2ETests/          # E2E tests (instalación, workflows)
├── TestApi/                    # API de prueba para desarrollo
├── CLAUDE.md                   # Guía para Claude Code
├── PRD.md                      # Product Requirements Document
├── PLAN.md                     # Plan técnico
└── README.md
```

## 🧪 Testing

Portless.NET tiene tres suites de tests para validación completa:

### Unit Tests (Portless.Tests)

Tests de nivel de componente con WebApplicationFactory para routing YARP.

```bash
# Ejecutar tests unitarios
dotnet test Portless.Tests/Portless.Tests.csproj

# Ejecutar tests específicos
dotnet test --filter "FullyQualifiedName~ProxyRoutingTests"
dotnet test --filter "FullyQualifiedName~YarpProxyIntegrationTests"
dotnet test --filter "FullyQualifiedName~RoutePersistenceIntegrationTests"
```

### Integration Tests (Portless.IntegrationTests)

Tests in-process de comandos CLI y gestión de procesos.

```bash
# Ejecutar tests de integración
dotnet test Portless.IntegrationTests/Portless.IntegrationTests.csproj

# Ejecutar por categoría
dotnet test --filter "FullyQualifiedName~CliCommandTests"
dotnet test --filter "FullyQualifiedName~ProxyProcessTests"
dotnet test --filter "FullyQualifiedName~PortAllocatorTests"
```

### E2E Tests (Portless.E2ETests)

Tests de instalación completa de herramienta y workflows CLI.

```bash
# Ejecutar tests E2E
dotnet test Portless.E2ETests/Portless.E2ETests.csproj

# Ejecutar por categoría
dotnet test --filter "FullyQualifiedName~CrossPlatformTests"
dotnet test --filter "FullyQualifiedName~ToolInstallationTests"
dotnet test --filter "FullyQualifiedName~CommandLineE2ETests"
```

### Ejecutar Todos los Tests

```bash
# Ejecutar todas las suites de tests
dotnet test

# Con coverage
dotnet test --collect:"XPlat Code Coverage"

# Con output detallado
dotnet test --logger "console;verbosity=detailed"
```

### Organización de Tests

- **Unit Tests**: Component-level con WebApplicationFactory para YARP routing
- **Integration Tests**: In-process CLI y process management
- **E2E Tests**: Full tool installation y workflow validation

### Cross-Platform Testing

Tests validados en Windows y Linux (Ubuntu/Debian). Cada test es independiente con directorio temporal único y cleanup después de la ejecución.

### Testing Manual

Antes de cada release, ejecutar tests E2E para validar instalación de herramienta:

```bash
# Build
dotnet build Portless.slnx

# Pack
dotnet pack Portless.Cli/Portless.Cli.csproj -o ./nupkg

# Install (local)
dotnet tool install --add-source ./nupkg portless.dotnet

# Verify
portless --help
portless proxy status
portless list
```

## 🔌 SignalR Support

Portless.NET supports **SignalR real-time communication** through the proxy using WebSocket transport.

### Features

- **Automatic WebSocket negotiation** - SignalR negotiates WebSocket connections through proxy without special configuration
- **Bidirectional messaging** - Server can push messages to clients instantly
- **Broadcast patterns** - Hub broadcasts to all connected clients through proxy
- **Multiple clients** - Concurrent connections supported (configurable limit)

### Quick Start

1. Start the proxy:
   ```bash
   portless proxy start
   ```

2. Run your SignalR app:
   ```bash
   portless mychat -- dotnet run --project MySignalRApp/
   ```

3. Connect clients to `http://mychat.localhost:1355`

### Example

See [SignalR Chat Example](Examples/SignalRChat/) for a working demonstration of real-time chat through the proxy.

### Troubleshooting

For common SignalR issues and solutions, see [SignalR Troubleshooting Guide](docs/signalr-troubleshooting.md).

## 🔍 Troubleshooting

### Protocol Issues

**HTTP/2 not working?**
- Check [Protocol Troubleshooting Guide](docs/protocol-troubleshooting.md#silent-http2-downgrade)
- Try with prior knowledge: `curl --http2-prior-knowledge http://miapp.localhost:1355`

**WebSocket connection dropping?**
- See [WebSocket Timeout Issues](docs/protocol-troubleshooting.md#websocket-connection-timeout)
- Check proxy logs: `portless proxy logs`

### Common Issues

**Port already in use:**
```bash
# Windows
netstat -ano | findstr :PORT
taskkill /PID <pid> /F

# macOS/Linux
lsof -ti:PORT | xargs kill -9
```

**Proxy not responding:**
```bash
portless proxy status
portless proxy logs
```

For more troubleshooting, see the [Protocol Troubleshooting Guide](docs/protocol-troubleshooting.md).

## 🎯 Casos de Uso

### Desarrollo Full-Stack

```bash
# Frontend + Backend
portless run web npm run dev
portless run api dotnet run
```

### Microservicios

```bash
# Múltiples servicios independientes
portless run auth dotnet run --project src/Auth
portless run users dotnet run --project src/Users
portless run payments dotnet run --project src/Payments
```

### Testing E2E

```bash
# URLs predecibles para tests automatizados
portless run test-e2e dotnet test
# Test usa: http://test-e2e.localhost:1355
```

## 🌍 Cross-Platform

Portless.NET funciona en:

- ✅ **Windows 10+**
- ✅ **macOS 12+**
- ✅ **Linux** (Ubuntu 20.04+, Debian 11+, Fedora 35+)

## 🔄 Variables de Entorno

| Variable | Descripción | Default |
|----------|-------------|---------|
| `PORTLESS_PORT` | Puerto del proxy | `1355` |
| `PORTLESS_HTTPS_ENABLED` | Habilitar endpoint HTTPS | `false` |
| `PORTLESS_CERT_WARNING_DAYS` | Días antes de expiración para advertencia | `30` |
| `PORTLESS_CERT_CHECK_INTERVAL_HOURS` | Horas entre verificaciones de certificado | `6` |
| `PORTLESS_AUTO_RENEW` | Renovar certificado automáticamente | `true` |
| `PORTLESS_ENABLE_MONITORING` | Habilitar monitoreo en segundo plano | `false` |

Para más detalles sobre gestión de certificados, ver [Certificate Lifecycle](docs/certificate-lifecycle.md).

## 🤝 Contribuyendo

¡Contribuciones bienvenidas! Este proyecto está en desarrollo activo.

### Roadmap

- [x] v1.0 - MVP (HTTP/1.1, routing, CLI, process management)
- [x] v1.1 - Advanced Protocols (HTTP/2, WebSocket, SignalR)
- [ ] v1.2 - Platform Expansion (HTTPS, cross-platform validation)
- [ ] v2.0 - Production Features (TLS, authentication, monitoring)

Ver [PRD.md](PRD.md) para roadmap completo.

## 📖 Recursos

- [Portless original](https://github.com/portless/portless)
- [YARP Documentation](https://microsoft.github.io/reverse-proxy/)
- [.NET 10 Documentation](https://docs.microsoft.com/en-us/dotnet/core/)

## 📄 Licencia

MIT License - ver [LICENSE](LICENSE) para detalles.

## 🙏 Agradecimientos

- [Portless](https://github.com/portless/portless) por la inspiración y diseño original
- [YARP team](https://microsoft.github.io/reverse-proxy/) por el excelente reverse proxy
- Comunidad .NET por el feedback y soporte

---

**Nota:** Portless.NET es actualmente un trabajo en progreso. En desarrollo activo. 🚧
