# Plan: Dotnet Tool con YARP - Alternativa a Portless

## Contexto

El usuario está considerando crear una herramienta `dotnet tool` que proporcione la misma experiencia que Portless (reemplazar números de puerto con URLs .localhost estables), pero usando YARP como proxy inverso en lugar del proxy HTTP personalizado de Portless.

## Análisis de Factibilidad

### ✅ Ventajas de usar .NET + YARP

1. **YARP es producción-ready**: Mantenido por Microsoft, usado en servicios de Microsoft
2. **Rendimiento superior**: HTTP/2 nativo, Kestrel altamente optimizado
3. **Cross-platform de verdad**: Mismo runtime en Windows/Mac/Linux
4. **Integración con ecosistema .NET**: Mejor para desarrolladores .NET
5. **Soporte para escenarios avanzados**:
   - gRPC
   - WebSockets
   - Server-Sent Events
   - Custom transforms
   - Health checks
   - Service discovery

### 📁 Arquitectura Propuesta

```
portless-dotnet/
├── src/
│   ├── Portless.Proxy/          # Proxy YARP (Console App)
│   │   ├── ProxyService.cs      # Servicio YARP
│   │   ├── RouteManager.cs      # Gestión de rutas
│   │   ├── PortAllocator.cs     # Asignación de puertos
│   │   └── Program.cs
│   │
│   ├── Portless.Cli/            # CLI Tool (dotnet tool)
│   │   ├── Commands/
│   │   │   ├── ProxyCommand.cs
│   │   │   ├── RunCommand.cs
│   │   │   ├── ListCommand.cs
│   │   │   └── TrustCommand.cs
│   │   └── Program.cs
│   │
│   └── Portless.Core/           # Shared logic
│       ├── Models/
│       ├── Services/
│       └── Extensions/
│
├── tests/
└── README.md
```

### 🔧 Componentes Técnicos Clave

#### 1. **Proxy con YARP**

```csharp
// Implementación básica del proxy YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(config)  // Rutas dinámicas desde archivo
    .AddTransforms<CustomTransforms>();

// Actualización en caliente de rutas
IProxyStateLookup stateLookup = ...;
```

#### 2. **Gestión de Rutas (reemplazo de routes.ts)**

- Almacenamiento: JSON en `~/.portless` o `/tmp/portless`
- Bloqueo de archivos: `FileStream` con `FileShare.None`
- Limpieza de rutas muertas: Verificar PIDs con `Process.GetProcessById()`

#### 3. **Asignación de Puertos**

- Rango: 4000-4999 (configurable)
- Detección de puerto libre: `TcpListener.Start(0)`
- Persistencia en JSON

#### 4. **CLI con System.CommandLine**

```csharp
// dotnet tool install -g portless.dotnet
portless proxy start [--https] [-p <port>]
portless proxy stop
portless <name> <command...>
portless list
portless trust
```

#### 5. **Certificados HTTPS**

- Generación: `System.Security.Cryptography.X509Certificates`
- CA local: Similar a Portless
- Trust store:
  - Windows: Certificate store
  - macOS: `security add-trusted-cert`
  - Linux: `update-ca-certificates`

### 📦 Distribución como Dotnet Tool

```bash
# Instalación
dotnet tool install -g portless.dotnet

# Uso
portless proxy start
portless myapi dotnet run
```

### 🔄 Diferencias Arquitectónicas con Portless

| Aspecto | Portless (Node.js) | Portless.NET |
|---------|-------------------|--------------|
| Proxy | HTTP module personalizado | YARP (Kestrel) |
| HTTP/2 | http2 module + byte-peeking | Nativo en Kestrel |
| Configuración | CLI args + env vars | CLI + appsettings.json |
| Hot reload | fs.watch | FileSystemWatcher o IOptions |
| Process mgmt | child_process.spawn | Process.Start |
| State | JSON + file locking | JSON + FileStream lock |

### ⚠️ Desafíos a Resolver

1. **Proxy como daemon en background**:
   - Windows: Task Scheduler o servicio Windows
   - Mac/Linux: fork/detach como en Portless
   - Alternativa: Mantener en foreground con `&`

2. **Actualización en caliente de rutas YARP**:
   - YARP soporta recarga de configuración
   - Necesita implementar `IConfigFilter` o `IProxyConfigProvider`

3. **Cross-platform trust store**:
   - Windows: Cert store API
   - Mac: CLI `security`
   - Linux: Distribución-dependiente

4. **File locking cross-platform**:
   - .NET `FileStream` es consistente across platforms
   - Similar a Portless

### 📊 Comparativa de Rendimiento Esperada

| Métrica | Node.js Portless | .NET 10 + YARP |
|---------|------------------|---------------|
| Memory base | ~50-80 MB | ~80-120 MB |
| CPU idle | Bajo | Bajo |
| Throughput HTTP/1.1 | Bueno | Excelente |
| HTTP/2 multiplexing | Bueno | Excelente |
| Cold start | Muy rápido | Rápido (~1s) |
| AOT Compilation | No | Sí (Native AOT) |

#### ✅ Conclusión

**Sí, es totalmente factible** y podría ofrecer:

1. **Mejor rendimiento** con Kestrel + YARP + .NET 10
2. **Native AOT** para startup instantáneo y menor footprint
3. **Mayor flexibilidad** para escenarios .NET
4. **Misma experiencia de usuario** que Portless
5. **Mejor integración** con ecosistema .NET
6. **Feature parity** con Portless actual

### 🚀 Recomendación

Si el target principal son desarrolladores .NET, esta herramienta tendría mucho sentido. Podría incluso coexistir con Portless original.

**Próximos pasos si decides implementarlo:**

1. Crear proof-of-concept del proxy YARP con rutas dinámicas
2. Implementar RouteStore (persistencia de rutas)
3. Crear CLI básica con System.CommandLine
4. Probar cross-platform (Windows/Mac/Linux)
5. Implementar daemon/background process
