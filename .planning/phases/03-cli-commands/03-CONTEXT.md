# Phase 3: CLI Commands - Context

**Gathered:** 2026-02-19
**Status:** Ready for planning

## Phase Boundary

CLI completa con comandos para controlar el proxy (start/stop), ejecutar aplicaciones con URLs nombradas (run), y listar rutas activas (list). Los comandos proporcionan feedback claro, manejo de errores accionable, y experiencia de usuario pulida.

## Implementation Decisions

### Organización de comandos
- **Estructura jerárquica**: `portless proxy start/stop`, `portless list`, `portless run <name> <cmd>`
- **Comando run**: `portless run <nombre> <comando>` — formato separado (Claude's discretion)
- **Comandos de gestión**: start/stop/list + `portless proxy status` (Claude's discretion)
- **Configuración de puerto**: Flag `--port` en `portless proxy start --port 1355`

### Salida y feedback
- **Formato de list**: Detección automática — tabla si es TTY, JSON si se redirige
- **Información en list**: Nombre, hostname, puerto, PID (Claude's discretion — balance útil sin abrumar)
- **Feedback de start**: Mensaje breve "Proxy started on http://localhost:1355"
- **Indicador de progreso**: Spinner mientras el proxy se inicia (startup puede tardar)

### Ejecución de apps
- **Modo de ejecución**: Background (detached) — CLI retorna inmediatamente
- **Output del proceso**: Descartar stdout/stderr del proceso ejecutado
- **Manejo de SIGINT**: No propagar Ctrl+C al proceso background (Claude's discretion)
- **Mensaje post-run**: "Running on http://miapi.localhost (port: 4001)"

### Manejo de errores
- **Nivel de detalle**: Minimal — solo problema y solución, sin stack traces
- **Proxy ya corriendo**: "Error: Proxy is already running. Use 'portless proxy stop' first" (Claude's discretion)
- **Ruta existente**: "Error: Route 'api' already exists. Use 'portless list' to see active routes"
- **Puerto ocupado**: "Error: Port 1355 in use. Try: netstat -ano | findstr 1355"

### Claude's Discretion
- **Sintaxis de run**: Elegir formato más estándar para CLI tools
- **Comandos de gestión**: Agregar `status` y/o `restart` si hacen la CLI más completa sin redundancia
- **Información en list**: Balance entre información útil y no abrumar al usuario
- **Manejo de SIGINT**: Elegir comportamiento más estándar para background processes
- **Mensaje de proxy ya corriendo**: Elegir el mensaje más claro y accionable

## Specific Ideas

No specific requirements — open to standard CLI tool patterns and Spectre.Console best practices.

## Deferred Ideas

None — discussion stayed within phase scope.

---

*Phase: 03-cli-commands*
*Context gathered: 2026-02-19*
