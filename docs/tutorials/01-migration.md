# Tutorial 1: Migrating an Existing Project to Portless

This tutorial guides you through migrating an existing ASP.NET Core project to use Portless.NET for stable .localhost URLs during development.

## Prerequisites

- .NET 10 SDK installed
- Portless.NET installed: `dotnet tool install --global Portless.NET.Tool`
- An existing ASP.NET Core project

## Overview

Migration involves three steps:
1. Configure your application to read the PORT environment variable
2. Update launchSettings.json for dynamic port assignment
3. Run your application with Portless

## Step 1: Configure PORT Variable Reading

Open your project's `Program.cs` and add PORT integration before `var app = builder.Build();`:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Portless integration: Read PORT from environment
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}

// ... existing service configuration

var app = builder.Build();
// ... existing middleware configuration
```

**Why this works:**
- Portless injects the `PORT` environment variable when starting your application
- `UseUrls()` configures Kestrel to bind to the specified port
- The `*` wildcard binds to all network interfaces (localhost, 127.0.0.1, etc.)

**Alternative: Using Kestrel Configuration**

If you prefer configuration over code, see [Kestrel Configuration](../integration/kestrel-configuration.md).

## Step 2: Update launchSettings.json

Open `Properties/launchSettings.json` and add a "Portless" profile:

```json
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "Portless": {
      "commandName": "Project",
      "launchBrowser": true,
      "applicationUrl": "http://localhost:0",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "launchBrowser": true,
      "applicationUrl": "http://localhost:5000;https://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

**Key changes:**
- Added "Portless" profile with `applicationUrl: "http://localhost:0"`
- `localhost:0` allows dynamic port assignment (Portless assigns the actual port)
- Preserved existing profiles for non-Portless development

**Note:** Keep your existing profiles! You can switch between Portless and direct execution in Visual Studio or VS Code.

## Step 3: Run with Portless

Start the Portless proxy:
```bash
portless proxy start
```

Run your application with Portless:
```bash
# Navigate to your project directory
cd path/to/your/project

# Run with Portless (replace 'myapp' with your preferred hostname)
portless myapp dotnet run
```

Access your application at: `http://myapp.localhost`

## Verification

1. **Check proxy is running:**
   ```bash
   portless proxy status
   ```
   Expected: "Proxy is running on port 1355"

2. **Check your route is registered:**
   ```bash
   portless list
   ```
   Expected output:
   ```
   ┌──────────┬───────┬─────────┬───────┐
   │ Hostname │ Port  │ Process │ PID   │
   ├──────────┼───────┼─────────┼───────┤
   │ myapp    │ 4001  │ dotnet  │ 12345 │
   └──────────┴───────┴─────────┴───────┘
   ```

3. **Test the URL:**
   ```bash
   curl http://myapp.localhost/
   # Or open in browser: http://myapp.localhost/
   ```

## Troubleshooting

### Application doesn't use the assigned port

**Symptom:** Application runs on default port (5000) instead of Portless-assigned port (4001)

**Solution:** Ensure `UseUrls()` is called BEFORE `builder.Build()`. The order matters:

Correct:
```csharp
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}
var app = builder.Build();
```

Incorrect:
```csharp
var app = builder.Build();
var port = Environment.GetEnvironmentVariable("PORT");
// UseUrls after Build() has no effect
```

### Portless proxy not running

**Symptom:** `portless proxy status` returns "Proxy is not running"

**Solution:** Start the proxy:
```bash
portless proxy start
```

### hostname.localhost doesn't resolve

**Symptom:** Browser shows "This site can't be reached" or DNS error

**Solution:** Ensure Portless proxy is running on port 1355 and your route is registered. The `.localhost` TLD is reserved for local development and should resolve automatically on modern OSs.

If you still have issues, try:
```bash
# Check if proxy is listening
curl http://localhost:1355/api/v1/health

# Check routes
portless list
```

### Multiple applications on same port

**Symptom:** Two different applications show the same port in `portless list`

**Solution:** Portless automatically assigns unique ports. If you see conflicts, ensure each application is started with a unique hostname:
```bash
# Terminal 1
portless app1 dotnet run

# Terminal 2
portless app2 dotnet run
```

## Advanced: appsettings.json Integration

If you prefer configuration over code, you can integrate Portless via `appsettings.json`:

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

See [appsettings.json Integration](../integration/appsettings.md) for details.

## Next Steps

- [Tutorial 2: Creating a New Project with Portless](02-from-scratch.md)
- [Tutorial 3: Microservices Scenario](03-microservices.md)
- [launchSettings.json Reference](../integration/launch-settings.md)

## Example Project

See the `Examples/WebApi` directory in the Portless.NET repository for a complete working example.
