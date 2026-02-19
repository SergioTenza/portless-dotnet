# Portless.NET

> Reemplaza números de puerto con URLs estables y con nombre en `.localhost` para desarrollo local

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-10-purple.svg)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey.svg)](https://github.com/yourusername/portless-dotnet)

## 🎯 ¿Qué es Portless.NET?

**Portless.NET** es una herramienta `dotnet tool` que elimina los conflictos de puerto en desarrollo local proporcionando URLs estables y nombradas. En lugar de recordar si tu app está en `localhost:3001`, `localhost:8080` o `localhost:5000`, simplemente usas `http://miapp.localhost:1355`.

### Inspirado en [Portless](https://github.com/portless/portless)

Portless.NET es un port a .NET 10 del excelente [Portless](https://github.com/portless/portless) (Node.js), usando [YARP](https://microsoft.github.io/reverse-proxy/) (Yet Another Reverse Proxy) de Microsoft como motor de proxy inverso.

### ✨ Ventajas

- ✅ **Soporte Windows nativo** (a diferencia de Portless original)
- ✅ **Mejor rendimiento** con .NET 10 + Kestrel + Native AOT
- ✅ **Single binary deployment**
- ✅ **Integración nativa** con ecosistema .NET
- ✅ **HTTP/2 + HTTPS** con certificados auto-generados
- ✅ **WebSockets** transparentes

## 🚀 Quick Start

### Instalación

```bash
dotnet tool install -g portless.dotnet
```

### Uso Básico

```bash
# Inicia el proxy (una sola vez)
portless proxy start

# Ejecuta tu app con un nombre
portless miapi dotnet run

# Accede a tu app en:
# http://miapi.localhost:1355
```

### Múltiples Servicios

```bash
# Monorepo con múltiples servicios
portless orders dotnet run --project services/Orders
portless products dotnet run --project services/Products
portless frontend dotnet run --project web/Frontend

# Accede a cada servicio:
# http://orders.localhost:1355
# http://products.localhost:1355
# http://frontend.localhost:1355
```

### Con HTTPS

```bash
# Inicia el proxy con HTTPS
portless proxy start --https

# Accede con HTTPS:
# https://miapi.localhost:1355
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
portless proxy start [--https] [-p <port>]    # Inicia proxy (default: puerto 1355)
portless proxy stop                          # Detiene proxy
portless <name> <command...>                 # Ejecuta app con URL nombrada
portless list                                # Lista apps activas
portless trust                               # Confía CA local (para HTTPS)
```

## 🔧 Integración con ASP.NET Core

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
- **YARP 2.x** - Reverse proxy de Microsoft
- **System.CommandLine** - CLI framework
- **Native AOT** - Single binary deployment
- **Serilog** - Logging estructurado

## 📁 Estructura del Proyecto

```
portless-dotnet/
├── src/
│   ├── Portless.Core/        # Lógica central
│   ├── Portless.Cli/         # CLI entry point
│   └── Portless.Tests/       # Tests
├── docs/
├── README.md
├── PRD.md                    # Product Requirements Document
└── PLAN.md                   # Plan técnico
```

## 🎯 Casos de Uso

### Desarrollo Full-Stack

```bash
# Frontend + Backend
portless web npm run dev
portless api dotnet run
```

### Microservicios

```bash
# Múltiples servicios independientes
portless auth dotnet run --project src/Auth
portless users dotnet run --project src/Users
portless payments dotnet run --project src/Payments
```

### Testing E2E

```bash
# URLs predecibles para tests automatizados
portless test-e2e dotnet test
# Test usa: http://test-e2e.localhost:1355
```

## 🔐 HTTPS

Portless.NET genera automáticamente certificados TLS y los agrega a tu sistema trust store. No necesitas configuración manual.

### Trust Store Management

```bash
# Verificar si CA es confiable
portless trust

# Agregar manualmente (automático con --https)
sudo portless trust
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
| `PORTLESS_HTTPS` | Habilitar HTTPS | `0` |
| `PORTLESS_STATE_DIR` | Directorio de estado | `~/.portless` |
| `PORTLESS` | Bypass proxy | - |

### Bypass

```bash
# Ejecutar directamente sin proxy
PORTLESS=0 portless miapp dotnet run

# O
PORTLESS=skip portless miapp dotnet run
```

## 🤝 Contribuyendo

¡Contribuciones bienvenidas! Por favor lee [CONTRIBUTING.md](CONTRIBUTING.md) para detalles.

### Roadmap

- [ ] v0.1.0 - MVP (HTTP/1.1, rutas dinámicas)
- [ ] v0.2.0 - HTTP/2 + HTTPS
- [ ] v0.3.0 - WebSockets
- [ ] v1.0.0 - Stable release

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
