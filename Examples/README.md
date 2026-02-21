# Portless.NET Integration Examples

This solution contains four example projects demonstrating how to integrate Portless.NET with different .NET workloads.

## Projects

- **WebApi** - ASP.NET Core Web API with PORT integration
- **BlazorApp** - Blazor Web App with PORT integration
- **WorkerService** - Background service with PORT integration
- **ConsoleApp** - Console application with PORT integration

## Prerequisites

1. Install Portless.NET as a local or global tool:
   ```bash
   # From source (local tool)
   dotnet tool install --add-source . portless.dotnet

   # Or as global tool (if published)
   dotnet tool install --global Portless.NET.Tool
   ```

2. Start the Portless proxy:
   ```bash
   portless proxy start
   ```

## Running the Examples

### WebApi Example

Start the WebApi with Portless:
```bash
cd Examples/WebApi
portless webapi dotnet run
```

Access the API at: `http://webapi.localhost`

The API will respond with:
```json
{
  "message": "WebApi example is running with Portless!",
  "timestamp": "2026-02-21T10:30:00Z",
  "port": "4001"
}
```

### BlazorApp Example

Start the Blazor app with Portless:
```bash
cd Examples/BlazorApp
portless blazor dotnet run
```

Access the app at: `http://blazorapp.localhost`

### WorkerService Example

Start the worker service with Portless:
```bash
cd Examples/WorkerService
portless worker dotnet run
```

The worker will log its assigned port every 5 seconds:
```
info: WorkerService.Worker[0]
      Worker running at: http://localhost:4003 (assigned by Portless)
```

Note: Worker services don't typically expose HTTP endpoints, but this example demonstrates how to access the PORT variable for any purpose (logging, connecting to other services, etc.).

### ConsoleApp Example

Start the console app with Portless:
```bash
cd Examples/ConsoleApp
portless myconsole dotnet run
```

The console will display:
```
Portless Console App Example
Running on port: 4004 (assigned by Portless)
URL: http://localhost:4004
```

## Running Multiple Examples

You can run multiple examples simultaneously - Portless will assign unique ports to each:

```bash
# Terminal 1
cd Examples/WebApi
portless webapi dotnet run

# Terminal 2
cd Examples/BlazorApp
portless blazor dotnet run

# Terminal 3
cd Examples/ConsoleApp
portless myconsole dotnet run
```

Each example will receive a unique port in the 4000-4999 range and be accessible via its `.localhost` URL.

## Viewing Active Routes

List all active Portless routes:
```bash
portless list
```

Example output:
```
Hostname    Port  Process  PID
webapi      4001  dotnet   12345
blazorapp   4002  dotnet   12346
worker      4003  dotnet   12347
myconsole   4004  dotnet   12348
```

## Integration Pattern

All examples use the same PORT integration pattern:

### Program.cs (for ASP.NET Core projects)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Portless integration: Read PORT from environment
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}

var app = builder.Build();
// ... rest of application
```

**Important**: Call `builder.WebHost.UseUrls()` BEFORE `builder.Build()` to ensure Kestrel binds to the correct port.

### Worker.cs (for Background Services)

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    var port = Environment.GetEnvironmentVariable("PORT");
    while (!stoppingToken.IsCancellationRequested)
    {
        if (port != null)
        {
            _logger.LogInformation("Worker running at: http://localhost:{port} (assigned by Portless)", port);
        }
        await Task.Delay(5000, stoppingToken);
    }
}
```

### Program.cs (for Console Apps)

```csharp
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    Console.WriteLine($"Running on port: {port} (assigned by Portless)");
    Console.WriteLine($"URL: http://localhost:{port}");
}
```

### launchSettings.json

The "Portless" profile uses `http://localhost:0` to allow dynamic port assignment:

```json
{
  "profiles": {
    "Portless": {
      "commandName": "Project",
      "applicationUrl": "http://localhost:0",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

Using `localhost:0` tells ASP.NET Core to accept any port, allowing Portless to inject the PORT variable without conflicts.

## Alternative: appsettings.json Integration

You can also configure PORT binding via appsettings.json:

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

Then in Program.cs:
```csharp
builder.Configuration.AddEnvironmentVariables();
```

**Note**: The `${PORT}` syntax requires the configuration builder to support environment variable substitution. In .NET 10, this may require additional configuration.

## Stopping Examples

Press `Ctrl+C` in each terminal to stop the individual applications.

Stop the Portless proxy:
```bash
portless proxy stop
```

## Troubleshooting

### Issue: Application doesn't use the PORT assigned by Portless

**Solution**: Ensure `builder.WebHost.UseUrls()` is called BEFORE `builder.Build()` in Program.cs. The order matters in ASP.NET Core application initialization.

### Issue: Port is not accessible

**Solution**:
1. Check that Portless proxy is running: `portless proxy status`
2. Verify the route exists: `portless list`
3. Ensure your application actually started and is listening on the port

### Issue: Multiple apps on same port

**Solution**: Portless automatically assigns unique ports in the 4000-4999 range. If you see conflicts:
1. Each app must be started with a unique hostname: `portless hostname1 dotnet run`, `portless hostname2 dotnet run`
2. Check for orphaned processes with `portless list` and stop them if needed

### Issue: launchSettings.json not being used

**Solution**:
1. Ensure you're using the correct profile: `dotnet run --launch-profile "Portless"`
2. Or set `DOTNET_LAUNCH_PROFILE` environment variable: `export DOTNET_LAUNCH_PROFILE=Portless`

### Issue: Blazor app shows "Connection failed"

**Solution**: This is normal for the Blazor template's counter component. The main app should still work. If you see connection issues:
1. Check that the Blazor app is running on the PORT assigned by Portless
2. Verify the proxy is routing correctly to the blazorapp.localhost hostname

## Integration with Your Projects

To integrate Portless into your own .NET projects:

1. Add PORT environment variable reading in your Program.cs (before `builder.Build()`)
2. Use `builder.WebHost.UseUrls($"http://*:{port}")` for web projects
3. Optionally create a "Portless" profile in launchSettings.json with `applicationUrl: "http://localhost:0"`
4. Run your app with: `portless yourhostname dotnet run`

Your app will be accessible at `http://yourhostname.localhost`

## Advanced Usage

### Custom Port Range

By default, Portless assigns ports in the 4000-4999 range. To configure a different range:

```bash
export PORTLESS_PORT_RANGE=5000-5999
portless proxy start
```

### HTTPS Support

To enable HTTPS for your applications:

```bash
export PORTLESS_HTTPS=1
portless proxy start
```

Then access your apps at `https://yourapp.localhost` (you may need to trust the self-signed certificate).

### State Directory

Portless stores routes and state in `~/.portless` (Unix) or `%APPDATA%\portless` (Windows). To customize:

```bash
export PORTLESS_STATE_DIR=/custom/path
portless proxy start
```

## Additional Resources

- [Portless.NET Documentation](../../README.md)
- [YARP Reverse Proxy Documentation](https://microsoft.github.io/reverse-proxy/)
- [ASP.NET Core Kestrel Configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel)
