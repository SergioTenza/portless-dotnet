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
| `PORTLESS_HTTPS_ENABLED` | Habilitar endpoint HTTPS | `false` |
| `PORTLESS_STATE_DIR` | Directorio de estado | `~/.portless` |
| `PORTLESS=0|skip` | Bypass proxy | - |
| `PORTLESS_CERT_WARNING_DAYS` | Días antes de expiración para advertencia | `30` |
| `PORTLESS_CERT_CHECK_INTERVAL_HOURS` | Horas entre verificaciones de certificado | `6` |
| `PORTLESS_AUTO_RENEW` | Renovar certificado automáticamente | `true` |
| `PORTLESS_ENABLE_MONITORING` | Habilitar monitoreo en segundo plano | `false` |

### Gestión de Certificados

Portless.NET genera certificados TLS automáticamente para desarrollo HTTPS local.

**Comandos de certificados:**
```bash
# Verificar estado del certificado
portless cert check

# Renovar certificado (automático si expira pronto)
portless cert renew

# Renovar forzosamente
portless cert renew --force

# Instalar certificado CA en trust store
portless cert install

# Verificar estado de confianza
portless cert status
```

**Monitoreo automático:**
- El proxy verifica certificados al inicio
- Monitoreo en segundo plano opcional (PORTLESS_ENABLE_MONITORING=true)
- Renovación automática dentro de los 30 días de expiración
- Configurable vía variables de entorno

**Archivos de certificado:**
- Ubicación: `~/.portless/ca.pfx`, `~/.portless/cert.pfx`
- Validez: 5 años desde generación
- Renovación requiere reinicio del proxy

Para más detalles, ver [Certificate Lifecycle](docs/certificate-lifecycle.md) y [Certificate Security](docs/certificate-security.md).

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

## Git Workflow

El proyecto utiliza un flujo de trabajo con dos ramas principales para separar el código de producción del desarrollo activo.

### Estructura de ramas

- **main**: Rama de producción (v1.0 MVP estable)
  - Protegida de commits directos durante el desarrollo
  - Solo se actualiza mediante merges desde development cuando esté lista para release
  - Representa código estable y liberado

- **development**: Rama de desarrollo activo
  - Todo el nuevo desarrollo de funcionalidades ocurre aquí
  - Se fusiona en main cuando esté lista para release
  - Permite experimentación sin afectar la producción

### Reglas de flujo de trabajo

1. **Desarrollo en rama development**: Todas las nuevas funcionalidades, correcciones de bugs y experimentos pasan por development
2. **Merge a main para releases**: Cuando el código esté listo para producción, fusionar development en main
3. **Main está protegida**: Sin commits directos a main durante ciclos de desarrollo activo
4. **Ambas ramas rastreadas en remote**: Permite colaboración y code review

### Comandos comunes

```bash
# Crear una rama de feature desde development
git checkout development
git pull origin development
git checkout -b feature/nueva-funcionalidad

# Sincronizar cambios de development a tu feature branch
git checkout feature/nueva-funcionalidad
git merge development

# Fusionar development en main para un release
git checkout main
git pull origin main
git merge development
git push origin main

# Mantener development actualizado con main (después de release)
git checkout development
git merge main
```

Para documentación detallada del flujo de trabajo, ver [STATE.md](.planning/STATE.md).

## Referencias

- [PRD.md](PRD.md) - Product Requirements Document con roadmap completo
- [PLAN.md](PLAN.md) - Plan técnico y análisis de factibilidad
- [Portless original](https://github.com/portless/portless)
- [YARP Documentation](https://microsoft.github.io/reverse-proxy/)
