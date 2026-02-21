# Kestrel Configuration Guide

This guide explains how to configure Kestrel (ASP.NET Core's built-in web server) for Portless.NET integration using code-based configuration in `Program.cs`.

## Overview

Kestrel is the cross-platform web server for ASP.NET Core. When using Portless, you configure Kestrel to bind to the port specified by the `PORT` environment variable, enabling stable .localhost URLs without hardcoded ports.

## Basic Configuration

### Minimal PORT Integration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Portless integration: Read PORT from environment
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}

// ... rest of configuration
```

**How it works:**
1. Portless starts your application with `PORT=4001`
2. `UseUrls()` configures Kestrel to bind to `http://*:4001`
3. Application is accessible via `http://hostname.localhost`

### Using ConfigureKestrel (Alternative)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Portless integration using ConfigureKestrel
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(int.Parse(port));
    });
}

// ... rest of configuration
```

**Difference:** `UseUrls()` is simpler, `ConfigureKestrel()` provides more control.

## Complete Examples

### HTTP + HTTPS Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
var httpsPort = Environment.GetEnvironmentVariable("HTTPS_PORT");

if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}

if (!string.IsNullOrEmpty(httpsPort))
{
    builder.WebHost.UseUrls($"https://*:{httpsPort}");
}
```

### with Fallback to Default Port

```csharp
var builder = WebApplication.CreateBuilder(args);

// Portless integration with fallback
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}
else
{
    // Fallback to default port when not using Portless
    builder.WebHost.UseUrls("http://localhost:5000");
}

// ... rest of configuration
```

### ConfigureKestrel with Limits

```csharp
var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(int.Parse(port), listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
            listenOptions.UseConnectionLogging();
        });

        // Configure limits
        options.Limits.MaxConcurrentConnections = 100;
        options.Limits.MaxRequestBodySize = 10485760; // 10 MB
        options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
    });
}
```

## UseUrls vs ConfigureKestrel

### UseUrls

```csharp
builder.WebHost.UseUrls($"http://*:{port}");
```

**Pros:**
- Simple, concise
- Sufficient for most scenarios
- Easy to understand

**Cons:**
- Limited configuration options
- Less fine-grained control

**Use when:** You just need to bind to a specific port

### ConfigureKestrel

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port), listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});
```

**Pros:**
- Full Kestrel configuration
- Per-endpoint configuration
- Advanced features (limits, timeouts, HTTP/2)

**Cons:**
- More verbose
- Requires parsing port to int

**Use when:** You need advanced Kestrel features

## Common Patterns

### Binding to All Interfaces

```csharp
// Bind to all interfaces (localhost, 127.0.0.1, actual IP)
builder.WebHost.UseUrls($"http://*:{port}");
```

### Binding to Localhost Only

```csharp
// Bind only to localhost (not accessible from other machines)
builder.WebHost.UseUrls($"http://localhost:{port}");
```

### Binding to Specific IP

```csharp
// Bind to specific IP address
builder.WebHost.UseUrls($"http://192.168.1.100:{port}");
```

### Multiple Bindings

```csharp
// Bind to both HTTP and HTTPS
builder.WebHost.UseUrls(
    $"http://*:{port}",
    $"https://*:{httpsPort}"
);
```

## Advanced Configuration

### HTTP/2 Only

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port), listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});
```

### HTTP/1 and HTTP/2

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port), listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
});
```

### with Connection Logging

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port), listenOptions =>
    {
        listenOptions.UseConnectionLogging();
    });
});
```

### with HTTPS Certificate

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));

    options.ListenAnyIP(int.Parse(httpsPort), listenOptions =>
    {
        listenOptions.UseHttps("certificate.pfx", "password");
    });
});
```

## Configuration Limits

### Max Request Body Size

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
    options.Limits.MaxRequestBodySize = 10485760; // 10 MB
});
```

### Request Headers Timeout

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
    options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
});
```

### Keep-Alive Timeout

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
});
```

### Max Concurrent Connections

```csharp
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(int.Parse(port));
    options.Limits.MaxConcurrentConnections = 100;
});
```

## Integration with Other Configuration

### Combining UseUrls with appsettings.json

You can use both code and configuration:

**Program.cs:**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Code-based configuration for PORT (Portless)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}
// If PORT is not set, appsettings.json is used automatically
```

**appsettings.json:**
```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5000"
      }
    }
  }
}
```

**Result:**
- With Portless: `PORT` variable controls binding (code wins)
- Without Portless: appsettings.json controls binding (default)

### Using IWebHostBuilder

For more complex scenarios, you can use `ConfigureWebHostDefaults`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureWebHostDefaults(webBuilder =>
{
    var port = Environment.GetEnvironmentVariable("PORT");
    if (!string.IsNullOrEmpty(port))
    {
        webBuilder.UseUrls($"http://*:{port}");
    }
});
```

## Troubleshooting

### UseUrls Called Too Late

**Issue:** `UseUrls()` has no effect when called after `Build()`

**Wrong:**
```csharp
var app = builder.Build();
var port = Environment.GetEnvironmentVariable("PORT");
builder.WebHost.UseUrls($"http://*:{port}"); // Too late!
```

**Correct:**
```csharp
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}
var app = builder.Build();
```

### Port Parsing Error

**Issue:** `int.Parse(port)` throws FormatException

**Solution:** Validate port before parsing:
```csharp
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port) && int.TryParse(port, out int portNumber))
{
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.ListenAnyIP(portNumber);
    });
}
```

### Wildcard Binding Security

**Issue:** Binding to `*` exposes application to all network interfaces

**Solution:** For development, `*` is fine. For production, bind to specific IPs:
```csharp
// Development: bind to all interfaces
builder.WebHost.UseUrls($"http://*:{port}");

// Production: bind to specific interface
builder.WebHost.UseUrls($"http://192.168.1.100:{port}");
```

## Best Practices

1. **Place PORT integration early** - Call `UseUrls()` or `ConfigureKestrel()` right after creating the builder
2. **Validate input** - Check for null/empty PORT before parsing
3. **Use wildcards for development** - `http://*:{port}` for easy access
4. **Use specific IPs for production** - Limit exposure to required interfaces only
5. **Document the pattern** - Add comments explaining Portless integration
6. **Test both modes** - Ensure application works with and without Portless

## Complete Example

```csharp
using System;

var builder = WebApplication.CreateBuilder(args);

// Portless integration: Configure Kestrel to bind to PORT variable
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    // Validate and parse port
    if (int.TryParse(port, out int portNumber))
    {
        builder.WebHost.ConfigureKestrel(options =>
        {
            // Bind to all interfaces on the specified port
            options.ListenAnyIP(portNumber, listenOptions =>
            {
                // Support both HTTP/1 and HTTP/2
                listenOptions.Protocols = HttpProtocols.Http1AndHttp2;

                // Enable connection logging for debugging
                listenOptions.UseConnectionLogging();
            });

            // Configure limits for production
            options.Limits.MaxConcurrentConnections = 100;
            options.Limits.MaxRequestBodySize = 10485760; // 10 MB
            options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
        });
    }
    else
    {
        throw new InvalidOperationException($"Invalid PORT value: {port}");
    }
}
else
{
    // Fallback: Use default port when not running under Portless
    builder.WebHost.UseUrls("http://localhost:5000");
}

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
```

## See Also

- [Tutorial 1: Migrating an Existing Project](../tutorials/01-migration.md) - Step-by-step integration
- [appsettings.json Integration](./appsettings.md) - Configuration-based approach
- [launchSettings.json Integration](./launch-settings.md) - IDE configuration
- [Kestrel Documentation](https://docs.microsoft.com/aspnet/core/fundamentals/servers/kestrel/) - Official ASP.NET Core docs
