# appsettings.json Integration Guide

This guide explains how to configure Portless.NET integration using `appsettings.json`, providing a configuration-based alternative to code-based PORT integration.

## Overview

Instead of modifying `Program.cs` to read the PORT environment variable, you can configure Kestrel directly in `appsettings.json` using variable substitution. This approach keeps your code clean and all configuration in one place.

## Basic Configuration

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
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

**Key Feature:** The `${PORT}` syntax is replaced with the value of the PORT environment variable when Portless starts your application.

## How Variable Substitution Works

1. Portless starts your application with `PORT=4001` (or another available port)
2. ASP.NET Core reads `appsettings.json`
3. The `${PORT}` placeholder is replaced with `"4001"`
4. Kestrel binds to `http://0.0.0.0:4001`

This is the same mechanism ASP.NET Core uses for other environment variables like `${ASPNETCORE_ENVIRONMENT}`.

## Complete Configuration Examples

### Minimal Portless Configuration

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:${PORT}"
      }
    }
  }
}
```

### HTTP + HTTPS Configuration

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:${PORT}",
        "Protocols": "Http1AndHttp2"
      },
      "Https": {
        "Url": "https://0.0.0.0:${PORT}",
        "Protocols": "Http1AndHttp2",
        "Certificate": {
          "Path": "certificate.pfx",
          "Password": "${CERTIFICATE_PASSWORD}"
        }
      }
    }
  }
}
```

### Development Configuration

**appsettings.Development.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
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

### Production Configuration

**appsettings.Production.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Error"
    }
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:${PORT}",
        "Protocols": "Http1AndHttp2",
        "Limits": {
          "MaxConcurrentConnections": 100,
          "MaxConcurrentUpgradedConnections": 100
        }
      }
    }
  }
}
```

## Environment-Specific Settings

### Fallback to Default Port

Configure a fallback port when PORT is not set (e.g., when running without Portless):

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:${PORT|http://0.0.0.0:5000}"
      }
    }
  }
}
```

**Note:** ASP.NET Core doesn't natively support default values in variable substitution. You'll need to implement this in code or use a default configuration provider.

### Multiple Environment Sources

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

**Override in appsettings.Development.json:**
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

## Advanced Kestrel Configuration

### Connection Limits

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:${PORT}",
        "Protocols": "Http1AndHttp2"
      }
    },
    "Limits": {
      "MaxConcurrentConnections": 100,
      "MaxConcurrentUpgradedConnections": 100,
      "MaxRequestBodySize": 10485760,
      "MaxRequestBufferSize": 1048576,
      "RequestHeadersTimeout": "00:00:30"
    }
  }
}
```

### HTTP/2 Configuration

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:${PORT}",
        "Protocols": "Http2"
      }
    },
    "Limits": {
      "MaxConcurrentStreams": 100
    }
  }
}
```

### Timeouts

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:${PORT}",
        "Protocols": "Http1AndHttp2"
      }
    },
    "Limits": {
      "KeepAliveTimeout": "00:10:00",
      "RequestHeadersTimeout": "00:00:30"
    }
  }
}
```

## Configuration Builder Setup

By default, ASP.NET Core loads configuration from:
1. appsettings.json
2. appsettings.{Environment}.json
3. Environment variables
4. Command-line arguments

Ensure your Program.cs is set up correctly:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Configuration is already set up by WebApplication.CreateBuilder
// appsettings.json is automatically loaded

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
```

No additional code is needed for PORT integration when using appsettings.json!

## Environment Variable Priority

Configuration values are loaded in this order (later values override earlier ones):

1. appsettings.json
2. appsettings.{Environment}.json
3. Environment variables
4. Command-line arguments

**Example:**

**appsettings.json:**
```json
{
  "Port": "5000"
}
```

**Environment variable:**
```bash
export PORT=4001
```

**Result:** `PORT=4001` (environment variable overrides appsettings.json)

## Using launchSettings.json with appsettings.json

You can combine both approaches:

**Properties/launchSettings.json:**
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

**appsettings.json:**
```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:${PORT}"
      }
    }
  }
}
```

This provides:
- launchSettings.json for IDE integration
- appsettings.json for runtime configuration
- PORT variable injected by Portless CLI

## Docker Integration

For Docker containers, combine appsettings.json with Docker environment variables:

**appsettings.json:**
```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:${PORT}"
      }
    }
  }
}
```

**Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY . .
ENV PORT=80
ENTRYPOINT ["dotnet", "MyApp.dll"]
```

**docker-compose.yml:**
```yaml
services:
  myapp:
    build: .
    environment:
      - PORT=8080
    ports:
      - "8080:8080"
```

## Troubleshooting

### PORT Variable Not Recognized

**Issue:** `${PORT}` is not replaced, application binds to literal `${PORT}` string

**Solution:** Ensure the PORT environment variable is set:
```bash
# Test manually
export PORT=4001
dotnet run

# Or use Portless
portless myapp dotnet run
```

### Configuration Ignored

**Issue:** appsettings.json configuration is ignored

**Cause:** Program.cs overrides configuration with `UseUrls()`

**Solution:** Remove `UseUrls()` from Program.cs:
```csharp
// Remove this
builder.WebHost.UseUrls($"http://*:{port}");

// Let appsettings.json handle it
```

### Wrong Binding Address

**Issue:** Application binds to `127.0.0.1` instead of `0.0.0.0`

**Solution:** Ensure appsettings.json specifies `0.0.0.0`:
```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:${PORT}"  // Use 0.0.0.0, not localhost
      }
    }
  }
}
```

## Best Practices

1. **Use 0.0.0.0 for binding** - Ensures application is accessible from all interfaces
2. **Separate environment files** - Use appsettings.{Environment}.json for environment-specific settings
3. **Document configuration** - Add comments explaining the ${PORT} variable
4. **Test both modes** - Ensure application works with and without Portless
5. **Version control settings** - Commit appsettings.json (but not secrets)
6. **Use secret management** - Store sensitive values in user secrets or key vault

## Code vs Configuration Comparison

### Code-Based (Tutorial 1 approach)

```csharp
// Program.cs
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}
```

**Pros:**
- Explicit control
- Easy to debug
- No configuration magic

**Cons:**
- Requires code changes
- Mixed concerns (code + config)

### Configuration-Based (This guide)

```json
// appsettings.json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:${PORT}"
      }
    }
  }
}
```

**Pros:**
- No code changes
- All configuration in one place
- Environment-specific overrides

**Cons:**
- Less explicit
- Requires understanding variable substitution

Choose the approach that fits your team's preferences and project structure.

## See Also

- [Tutorial 1: Migrating an Existing Project](../tutorials/01-migration.md) - Code-based approach
- [launchSettings.json Integration](./launch-settings.md) - IDE configuration
- [Kestrel Configuration](./kestrel-configuration.md) - Advanced options
- [ASP.NET Core Configuration](https://docs.microsoft.com/aspnet/core/fundamentals/configuration/) - Official documentation
