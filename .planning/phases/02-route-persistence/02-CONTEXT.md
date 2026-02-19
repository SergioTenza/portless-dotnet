# Phase 02: Route Persistence - Context & Decisions

**Phase:** 02 - Route Persistence
**Goal:** Sistema persiste rutas en archivo JSON con file locking para concurrencia y hot-reload
**Created:** 2026-02-19
**Status:** Ready for Planning

## Phase Goal

Sistema persiste rutas en archivo JSON con:
1. **Persistencia:** Rutas guardadas en archivo y recuperadas al restart
2. **Concurrencia:** File locking para prevenir corrupción cuando múltiples procesos acceden simultáneamente
3. **Limpieza:** Rutas de procesos terminados se eliminan automáticamente
4. **Hot-reload:** Proxy recarga configuración sin restart cuando cambia el archivo

## Architectural Decisions

### 1. Ubicación del Archivo de Persistencia

**Decisión:** Detectar por plataforma automáticamente

**Rationale:** Portless.NET debe ser cross-platform nativo. Cada plataforma tiene convenciones diferentes para almacenamiento de configuración de usuario.

**Implementation:**
```csharp
// Windows: %APPDATA%/portless/
// macOS/Linux: ~/.portless/

static string GetStateDirectory()
{
    if (OperatingSystem.IsWindows())
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "portless");
    }
    else
    {
        // macOS/Linux: home directory
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".portless");
    }
}
```

**Archivos en el directorio de estado:**
- `routes.json` - Rutas configuradas (hostname → puerto/pid mappings)
- `proxy.pid` - PID del proceso proxy corriendo (para detección)
- `certs/` - Directorio para certificados HTTPS (fase futura, no en esta fase)

**Suposiciones a validar:**
- [ ] Environment.SpecialFolder.ApplicationData funciona correctamente en Windows 10+
- [ ] Environment.SpecialFolder.UserProfile funciona en macOS 12+ y Linux
- [ ] Directorio debe crearse automáticamente si no existe
- [ ] Permisos de escritura están disponibles en todas las plataformas

---

### 2. File Locking Strategy

**Decisión:** Mutex nombrado cross-platform

**Rationale:**
- Más robusto que FileShare.None bajo alta concurrencia
- Semaforo a nivel de sistema operativo previene accesos simultáneos
- Tiempo de lock limitado previene deadlocks
- Funciona idénticamente en Windows, macOS, y Linux

**Implementation Pattern:**
```csharp
public class RouteStore
{
    private const string MutexName = "Portless.Routes.Lock";
    private const int MutexTimeoutMs = 5000; // 5 segundos max wait

    public async Task<RouteInfo[]> LoadRoutesAsync()
    {
        using var mutex = new Mutex(false, MutexName);
        try
        {
            var acquired = mutex.WaitOne(MutexTimeoutMs);
            if (!acquired)
            {
                throw new IOException("Timeout acquiring route store lock");
            }

            // Leer archivo JSON
            var json = await File.ReadAllTextAsync(RoutesFilePath);
            return JsonSerializer.Deserialize<RouteInfo[]>(json);
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    public async Task SaveRoutesAsync(RouteInfo[] routes)
    {
        using var mutex = new Mutex(false, MutexName);
        try
        {
            var acquired = mutex.WaitOne(MutexTimeoutMs);
            if (!acquired)
            {
                throw new IOException("Timeout acquiring route store lock");
            }

            // Escribir archivo JSON (atomic write via temp file)
            var tempPath = RoutesFilePath + ".tmp";
            var json = JsonSerializer.Serialize(routes, JsonOptions);
            await File.WriteAllTextAsync(tempPath, json);

            // Atomic rename (cross-platform)
            File.Move(tempPath, RoutesFilePath, overwrite: true);
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }
}
```

**Suposiciones a validar:**
- [ ] Mutex con nombre funciona idénticamente en Windows/macOS/Linux
- [ ] Timeout de 5 segundos es suficiente para operaciones normales
- [ ] Write-via-temp-file + atomic rename es atómico en todas las plataformas
- [ ] Mutex se libera correctamente incluso si hay excepciones

**Riesgos identificados:**
- **Mutex abandonment:** Si un proceso crash sin liberar el mutex, puede quedar abandonado. .NET lanza `AbandonedMutexException` que debemos catch y manejar.
- **Timeout sensibility:** 5 segundos puede ser muy largo para UX pero muy corto para disk I/O lento. Ajustar según benchmarks.

---

### 3. Limpieza de Rutas Muertas

**Decisión:** Verificación periódica en background

**Rationale:**
- Más proactivo que solo verificar al startup
- Intervalo de 30 segundos es balanceado entre overhead y limpieza oportuna
- Permite que rutas huérfanas se limpien eventualmente sin intervención manual
- Compatible con el modelo de YARP de recarga dinámica de configuración

**Implementation Pattern:**
```csharp
public class RouteCleanupService : BackgroundService
{
    private readonly IRouteStore _routeStore;
    private readonly ILogger<RouteCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Route cleanup service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);

                var routes = await _routeStore.LoadRoutesAsync();
                var aliveRoutes = routes.Where(IsProcessAlive).ToArray();

                if (aliveRoutes.Length != routes.Length)
                {
                    var deadCount = routes.Length - aliveRoutes.Length;
                    _logger.LogInformation("Cleaning up {Count} dead routes", deadCount);

                    await _routeStore.SaveRoutesAsync(aliveRoutes);

                    // Trigger YARP reload
                    await _yarpConfigUpdater.UpdateRoutesAsync(aliveRoutes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during route cleanup");
            }
        }
    }

    private static bool IsProcessAlive(RouteInfo route)
    {
        try
        {
            // Process.GetProcessById lanza ArgumentException si PID no existe
            var process = Process.GetProcessById(route.Pid);
            return !process.HasExited; // True si proceso sigue vivo
        }
        catch (ArgumentException)
        {
            return false; // PID no existe
        }
    }
}
```

**Suposiciones a validar:**
- [ ] Process.GetProcessById funciona en todas las plataformas target
- [ ] Verificación cada 30 segundos no introduce overhead significativo
- [ ] Limpieza periódica no interfiere con YARP recarga activa
- [ ] HasExited es confiable para detectar procesos terminados

**Riesgos identificados:**
- **Race condition:** Proceso puede terminar entre verificación y guardado. Aceptable - se limpiará en el siguiente ciclo.
- **Zombie processes:** En Unix, procesos zombie pueden aparecer como "vivos" pero no están realmente ejecutando. Aceptable - el evento de cierre eventual limpiará la ruta.
- **PID recycling:** Si el SO recicla el PID y otro proceso lo toma, la ruta puede persistir incorrectamente. Riesgo bajo pero posible. Considerar agregar timestamp/creation time para validar.

---

### 4. Hot-Reload Strategy

**Decisión:** FileSystemWatcher con debounce

**Rationale:**
- Recarga inmediata cuando archivo cambia (reactivo)
- Debounce de 500ms previene múltiples recargas por el mismo cambio
- No requiere polling continuo (más eficiente)
- Compatible con el modelo de eventos de .NET

**Implementation Pattern:**
```csharp
public class RouteFileWatcher
{
    private readonly FileSystemWatcher _watcher;
    private readonly Timer _debounceTimer;
    private const int DebounceMs = 500;

    public RouteFileWatcher(string directoryPath)
    {
        _watcher = new FileSystemWatcher(directoryPath)
        {
            Filter = "routes.json",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };

        _watcher.Changed += OnFileChanged;
        _watcher.EnableRaisingEvents = true;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce: resetear timer en cada cambio
        _debounceTimer.Change(DebounceMs, Timeout.Infinite);
    }

    private async void OnDebounceElapsed(object state)
    {
        try
        {
            // Leer archivo actualizado
            var routes = await _routeStore.LoadRoutesAsync();

            // Actualizar configuración YARP
            await _yarpConfigUpdater.UpdateRoutesAsync(routes);

            _logger.LogInformation("Routes reloaded from file: {Count} routes", routes.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reloading routes from file");
        }
    }
}
```

**Suposiciones a validar:**
- [ ] FileSystemWatcher funciona correctamente en Windows/macOS/Linux
- [ ] Debounce de 500ms es suficiente para absorber múltiples writes atómicos
- [ ] FileSystemWatcher no pierde eventos bajo alta carga
- [ ] Error handling previene crashes si archivo está corrupto o lockeado

**Riesgos identificados:**
- **FileSystemWatcher reliability:** En algunas plataformas, FSW puede perder eventos o disparar múltiples veces por el mismo cambio. El debounce mitiga esto.
- **Race condition con file locking:** Si FSW dispara mientras el archivo está siendo escrito por otro proceso, podemos leer contenido parcial. La estrategia de write-via-temp-file + atomic rename mitiga esto.
- **File access errors:** FSW puede disparar Changed pero el archivo puede estar temporalmente inaccesible. Necesitamos retry con backoff.

---

## Data Models

### RouteInfo Model

```csharp
public class RouteInfo
{
    public string Hostname { get; init; } = string.Empty;
    public int Port { get; init; }
    public int Pid { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? LastSeen { get; set; } // Para validar PID recycling
}

// routes.json structure
[
  {
    "Hostname": "api1.localhost",
    "Port": 4001,
    "Pid": 12345,
    "CreatedAt": "2026-02-19T10:30:00Z",
    "LastSeen": "2026-02-19T14:45:00Z"
  },
  {
    "Hostname": "web1.localhost",
    "Port": 4002,
    "Pid": 12346,
    "CreatedAt": "2026-02-19T11:00:00Z",
    "LastSeen": "2026-02-19T14:45:00Z"
  }
]
```

## Integration with Existing Phase 1

### Phase 1 Artifacts to Reuse

1. **InMemoryConfigProvider** (Portless.Proxy/InMemoryConfigProvider.cs)
   - Ya tiene Update() method para recarga dinámica
   - Recibe RouteConfig[] y ClusterConfig[]
   - Haremos wrapper que convierte RouteInfo[] → YARP config

2. **Program.cs API Endpoint** (/api/v1/add-host)
   - Ahora debe persistir RouteInfo además de actualizar YARP
   - Debe llamar a RouteStore.SaveRoutesAsync()

3. **RequestLoggingMiddleware**
   - No requiere cambios
   - Continuará loggeando requests proxy

### New Components for Phase 2

1. **Portless.Core/Models/RouteInfo.cs** - Data model
2. **Portless.Core/Services/IRouteStore.cs** - Interface
3. **Portless.Core/Services/RouteStore.cs** - Implementation
4. **Portless.Core/Services/RouteCleanupService.cs** - Background cleanup
5. **Portless.Core/Services/RouteFileWatcher.cs** - File system watcher
6. **Portless.Core/Services/StateDirectoryProvider.cs** - Platform detection

---

## Dependencies

### Internal Dependencies
- **Phase 1 (Proxy Core)** - Must be complete
  - InMemoryConfigProvider exists
  - YARP integration works
  - API endpoint exists

### External Dependencies (New in Phase 2)
- **System.IO.FileSystem.Watcher** - Already in .NET 10 BCL
- **System.Threading.Mutex** - Already in .NET 10 BCL
- **System.Diagnostics.Process** - Already in .NET 10 BCL
- **System.Text.Json** - Already in .NET 10 BCL

No new NuGet packages required.

---

## Requirements Coverage

| Requirement | Status | Implementation |
|-------------|--------|----------------|
| ROUTE-01: Sistema persiste rutas en archivo JSON | Pending | RouteStore.SaveRoutesAsync() → ~/.portless/routes.json |
| ROUTE-02: Sistema implementa file locking | Pending | Mutex nombrado "Portless.Routes.Lock" con timeout |
| ROUTE-03: Sistema limpia rutas muertas | Pending | RouteCleanupService con Process.GetProcessById() cada 30s |
| ROUTE-04: Sistema soporta hot-reload | Pending | RouteFileWatcher con FileSystemWatcher + debounce |

---

## Success Criteria (Verification Goals)

Phase 02 is complete when ALL of these are TRUE:

1. **Persistencia:**
   - [ ] Rutas se guardan en `~/.portless/routes.json` (o `%APPDATA%/portless/`)
   - [ ] Rutas se recuperan correctamente al restart del proxy
   - [ ] Archivo se crea automáticamente si el directorio no existe

2. **File Locking:**
   - [ ] Múltiples procesos pueden leer/escribir simultáneamente sin corrupción
   - [ ] Mutex previene accesos concurrentes correctamente
   - [ ] Timeout de 5 segundos funciona sin deadlocks

3. **Limpieza:**
   - [ ] Rutas de procesos terminados se eliminan automáticamente
   - [ ] Verificación de PID funciona cross-platform
   - [ ] Background service corre sin detener el proxy

4. **Hot-Reload:**
   - [ ] Proxy recarga configuración cuando archivo cambia externamente
   - [ ] Debounce previene múltiples recargas rápidas
   - [ ] FileSystemWatcher funciona en todas las plataformas

---

## Open Questions to Validate

1. **Mutex Performance:** ¿El overhead de mutex es significativo? Si operations son muy rápidas (<1ms), tal vez FileShare.None sea suficiente.

2. **Cleanup Interval:** ¿30 segundos es muy frecuente? Puede causar overhead innecesario si hay muchas rutas.

3. **PID Recycling:** ¿Debemos agregar timestamp o creation time validation para prevenir que rutas persistent si el PID es reciclado?

4. **File System Edge Cases:** ¿Qué pasa si el disk está full? ¿Si el usuario borra manualmente el archivo? ¿Si los permisos cambian?

5. **Backwards Compatibility:** ¿Qué pasa si Phase 1 corrió sin persistencia, y luego Phase 2 intenta leer un archivo inexistente? (Debe iniciar vacío, no crash).

---

## Next Steps

1. ✅ **Discussions complete** - This document captures all architectural decisions
2. **Research Phase** - Validate assumptions via small POCs:
   - Mutex behavior on Windows/macOS/Linux
   - FileSystemWatcher reliability
   - Process.GetProcessById cross-platform behavior
3. **Planning Phase** - Create 2-3 detailed plans based on this context
4. **Execution Phase** - Implement plans in order with atomic commits

---

*Context captured: 2026-02-19*
*Architectural decisions approved via /gsd:discuss-phase*
*Ready for research and planning phases*
