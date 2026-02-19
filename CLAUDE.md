# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Portless.NET** es una herramienta `dotnet tool` que reemplaza números de puerto con URLs estables en `.localhost` para desarrollo local. Es un port de [Portless](https://github.com/portless/portless) (Node.js) a .NET 10 usando YARP como motor de proxy inverso.

### Diferenciadores clave vs Portless original:
- Soporte Windows nativo (Portless original no lo tiene)
- Mejor rendimiento con .NET 10 + Kestrel + Native AOT
- Single binary deployment
- Integración nativa con ecosistema .NET

## Comandos esenciales

### Build y Test
```bash
# Build entire solution
dotnet build Portless.slnx

# Run all tests
dotnet test Portless.Tests/Portless.Tests.csproj

# Run a specific test
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Clean build artifacts
dotnet clean Portless.slnx
```

### Desarrollo local
```bash
# Ejecutar el proxy (desde Portless.Proxy)
dotnet run --project Portless.Proxy/Portless.Proxy.csproj

# Ejecutar la CLI (desde Portless.Cli)
dotnet run --project Portless.Cli/Portless.Cli.csproj
```

### Instalación como tool local
```bash
# Instalar desde fuente
dotnet tool install --add-source . portless.dotnet

# Ejecutar como tool
portless proxy start
portless miapi dotnet run
```

## Arquitectura

### Estructura del proyecto

```
portless-dotnet/
├── Portless.Core/          # Class library - Lógica central compartida
├── Portless.Cli/           # Console app - CLI entry point (dotnet tool)
├── Portless.Proxy/         # Web app - Proxy YARP (Kestrel)
└── Portless.Tests/         # Test suite - xUnit tests
```

### Stack Tecnológico
- **.NET 10** con C# 14
- **YARP 2.3.0** - Reverse proxy de Microsoft
- **Spectre.Console.Cli 0.53.1** - CLI framework para Portless.Cli
- **xUnit 2.9.3** - Testing framework

### Componentes principales

**Portless.Proxy** (`Portless.Proxy/Program.cs`):
- Proxy YARP con configuración en memoria
- Endpoint `/api/v1/add-host` para actualizar rutas dinámicamente
- Usa `InMemoryConfigProvider` para hot-reload de configuración

**Portless.Cli** (`Portless.Cli/Program.cs`):
- CLI con Spectre.Console para output visual
- Entry point para `dotnet tool`

**Portless.Core**:
- Compartidos entre CLI y Proxy (lógica de negocio)

## Conceptos clave de implementación

### Gestión de rutas
Las rutas se almacenan en memoria y se actualizan vía POST al endpoint `/api/v1/add-host` del proxy. Cada ruta tiene:
- **Hostname**: Ej: `miapi.localhost`
- **Cluster**: Destination con puerto asignado (4000-4999)

### Asignación de puertos
- Rango automático: 4000-4999
- Variable `PORT` se inyecta en el comando ejecutado
- Se debe detectar puerto libre antes de asignar

### Arquitectura de comunicación
```
CLI (Portless.Cli) → Proxy (Portless.Proxy) → Apps backend
     ↓                        ↓
  Asigna PORT           Actualiza rutas YARP
  Ejecuta comando       Proxea requests
```

## Variables de entorno relevantes

| Variable | Descripción | Default |
|----------|-------------|---------|
| `PORTLESS_PORT` | Puerto del proxy | `1355` |
| `PORTLESS_HTTPS` | Habilitar HTTPS | `0` |
| `PORTLESS_STATE_DIR` | Directorio de estado | `~/.portless` |
| `PORTLESS=0|skip` | Bypass proxy | - |

## Integración ASP.NET Core

Para que una app use el puerto asignado dinámicamente:

**launchSettings.json:**
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

**appsettings.json:**
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

## Testing

- Framework: xUnit 2.9.3
- Coverage: coverlet.collector 6.0.4
- Tests se organizan en `Portless.Tests/` (actualmente vacío - proyecto inicial)

## Referencias

- [PRD.md](PRD.md) - Product Requirements Document con roadmap completo
- [PLAN.md](PLAN.md) - Plan técnico y análisis de factibilidad
- [Portless original](https://github.com/portless/portless)
- [YARP Documentation](https://microsoft.github.io/reverse-proxy/)
