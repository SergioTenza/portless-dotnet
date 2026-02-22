# CLI Reference

Complete reference for Portless.NET CLI commands.

## Commands

### proxy start

Start the Portless.NET reverse proxy.

**Usage:**
```bash
portless proxy start [--port <PORT>]
```

**Options:**
- `--port <PORT>` - Proxy port (default: 1355)

**Examples:**
```bash
# Start with default port
portless proxy start

# Start with custom port
portless proxy start --port 3000
```

**Protocol Support:**
The proxy supports:
- HTTP/2 for improved performance (automatic negotiation)
- WebSocket for real-time communication
- HTTP/1.1 fallback

---

### proxy stop

Stop the running proxy.

**Usage:**
```bash
portless proxy stop
```

**Behavior:**
- Prompts for confirmation if active processes are running
- Can stop managed processes along with the proxy
- Processes continue running if stop is cancelled

---

### proxy status

Show proxy status and active routes.

**Usage:**
```bash
portless proxy status [--protocol]
```

**Options:**
- `--protocol`, `-p` - Show detailed protocol information

**Examples:**
```bash
# Basic status
$ portless proxy status
✓ Proxy is running
  URL: http://localhost:1355
  PID: 12345
  Protocols: HTTP/2, WebSocket, HTTP/1.1

# Detailed protocol info
$ portless proxy status --protocol
✓ Proxy is running
  URL: http://localhost:1355
  PID: 12345

Protocol Support:
  HTTP/2: Enabled
  WebSocket: Supported
  HTTP/1.1: Supported

Protocol negotiation is automatic.
```

---

### list

List all active routes with their hostnames, ports, and process IDs.

**Usage:**
```bash
portless list
```

**Example Output:**
```
Name                 URL                                 Port    PID
────────────────────────────────────────────────────────────────────
● myapi              http://myapi.localhost              4234    12345
● products           http://products.localhost           4456    12346
○ chat               http://chat.localhost               4123    12347

All routes support HTTP/2 and WebSocket protocols.
```

**Status Indicators:**
- ● (green) - Process is running
- ○ (red) - Process has exited

**JSON Output:**
When output is redirected (e.g., `portless list > routes.json`), the command outputs JSON format instead of a table.

---

### run (alias: r)

Run an application with a named URL.

**Usage:**
```bash
portless run <name> <command...>
portless r <name> <command...>
```

**Arguments:**
- `<name>` - Hostname for the app (e.g., "myapi", "chat")
- `<command...>` - Command to execute

**Environment Variables:**
- `PORT` - Injected with the assigned port (4000-4999)

**Examples:**
```bash
# Run a .NET API
portless run myapi dotnet run

# Run a React dev server
portless run frontend npm start

# Run a SignalR chat app
portless run chat dotnet run --project Examples/SignalRChat

# Run with multiple arguments
portless run app npm run dev -- --port $PORT
```

**Protocol Support:**
Your application can use:
- HTTP/2 (automatic when supported)
- WebSocket for real-time features
- SignalR for ASP.NET Core apps

**Behavior:**
- Automatically starts the proxy if not running
- Assigns a free port from the 4000-4999 range
- Registers the route with the proxy
- Persists route information for recovery
- Forwards signals to spawned processes for graceful shutdown

---

## Protocol Testing Commands

### Test HTTP/2

```bash
# Test with HTTP/2 prior knowledge
curl -I --http2-prior-knowledge http://myapp.localhost:1355

# Test with HTTP/2 negotiation
curl -I --http2 http://myapp.localhost:1355

# Verbose output
curl -v --http2 http://myapp.localhost:1355
```

### Test WebSocket

```bash
# Using websocat
websocat ws://echo.localhost:1355/ws

# Using wscat
wscat -c ws://echo.localhost:1355/ws

# Using Python
python -m websockets ws://echo.localhost:1355/ws
```

For more protocol testing examples, see [HTTP/2 and WebSocket Testing Guide](http2-websocket-guide.md).

---

## Configuration

Portless.NET can be configured using environment variables:

| Variable | Description | Default |
|----------|-------------|---------|
| `PORTLESS_PORT` | Proxy port | `1355` |
| `PORTLESS_STATE_DIR` | State directory | `~/.portless` |
| `PORTLESS=0\|skip`` | Bypass proxy | - |

---

## Exit Codes

- `0` - Success
- `1` - Error (check error message for details)

---

## See Also

- [README](../README.md) - Main project documentation
- [HTTP/2 and WebSocket Testing Guide](http2-websocket-guide.md) - Protocol testing procedures
- [SignalR Troubleshooting](signalr-troubleshooting.md) - SignalR-specific issues

---

*CLI Reference*
*Version: 1.1*
*Updated: 2026-02-22*
