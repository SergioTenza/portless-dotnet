# launchSettings.json Integration Guide

This guide explains how to configure `launchSettings.json` for Portless.NET integration, enabling dynamic port assignment and stable .localhost URLs during development.

## Overview

`launchSettings.json` is a Visual Studio and VS Code configuration file that defines how your application starts. With Portless, you create a dedicated profile that allows the proxy to assign ports dynamically.

## File Location

```
YourProject/
├── Properties/
│   └── launchSettings.json    # Configuration file
├── Program.cs                  # Application code
└── YourProject.csproj
```

## Basic Structure

### Standard launchSettings.json

```json
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "launchBrowser": true,
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "launchBrowser": true,
      "applicationUrl": "https://localhost:5001;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### Portless-Enabled launchSettings.json

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
    "http": {
      "commandName": "Project",
      "launchBrowser": true,
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

**Key Change:** The "Portless" profile uses `applicationUrl: "http://localhost:0"` instead of a specific port like `http://localhost:5000`.

## Configuration Properties

### applicationUrl

The `applicationUrl` property specifies where the application listens:

| Value | Behavior | Use Case |
|-------|----------|----------|
| `http://localhost:0` | Dynamic port assignment (Portless) | Development with Portless |
| `http://localhost:5000` | Fixed port 5000 | Direct execution |
| `http://0.0.0.0:5000` | Bind to all interfaces | Docker/network access |

**For Portless:** Always use `http://localhost:0` to allow the `PORT` environment variable to control the binding.

### commandName

| Value | Behavior |
|-------|----------|
| `Project` | Run the project directly |
| `Executable` | Run a specific executable |
| `DevCode` | Custom development command |

**For Portless:** Use `"commandName": "Project"` for standard ASP.NET Core applications.

### launchBrowser

Automatically opens a browser when the application starts:

```json
"launchBrowser": true,
"launchUrl": "swagger"  // Optional: specific path
```

**For Portless:** Set to `true` for convenience, or `false` if you prefer manual browser navigation.

### environmentVariables

Environment variables passed to the application:

```json
"environmentVariables": {
  "ASPNETCORE_ENVIRONMENT": "Development",
  "PORTLESS_HOSTNAME": "myapp",
  "CUSTOM_VAR": "value"
}
```

**For Portless:** The `PORT` variable is injected automatically by the `portless` CLI command.

## Profile Examples

### Minimal Portless Profile

```json
{
  "profiles": {
    "Portless": {
      "commandName": "Project",
      "applicationUrl": "http://localhost:0"
    }
  }
}
```

### Portless with Browser Launch

```json
{
  "profiles": {
    "Portless": {
      "commandName": "Project",
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:0",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

### Complete Multi-Profile Configuration

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
    "http": {
      "commandName": "Project",
      "launchBrowser": true,
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "launchBrowser": true,
      "applicationUrl": "https://localhost:5001;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "Docker": {
      "commandName": "Project",
      "applicationUrl": "http://0.0.0.0:80",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

## Using launchSettings.json

### Visual Studio

1. Open your project in Visual Studio
2. Click the dropdown next to the "Run" button
3. Select "Portless" profile
4. Click Run (or press F5)

**Note:** Visual Studio will use the selected profile, but you still need to start the Portless proxy separately.

### Visual Studio Code

VS Code uses `.vscode/launch.json`. Configure it to use the Portless profile:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Core Launch (Portless)",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/bin/Debug/net10.0/YourApp.dll",
      "args": [],
      "cwd": "${workspaceFolder}",
      "stopAtEntry": false,
      "serverReadyAction": {
        "action": "openExternally",
        "pattern": "\\bNow listening on:\\s+(https?://\\S+)",
        "uriFormat": "%s/swagger"
      },
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  ]
}
```

### CLI with dotnet run

When using `dotnet run`, specify the launch profile:

```bash
# Use the Portless profile
dotnet run --launch-profile Portless

# Or use the default http profile
dotnet run --launch-profile http
```

**With Portless CLI:**
```bash
# Portless injects PORT automatically
portless myapp dotnet run
```

## Common Patterns

### Development vs Production

```json
{
  "profiles": {
    "Portless": {
      "commandName": "Project",
      "applicationUrl": "http://localhost:0",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "Production": {
      "commandName": "Project",
      "applicationUrl": "http://0.0.0.0:80",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Production"
      }
    }
  }
}
```

### Multiple Environments

```json
{
  "profiles": {
    "Portless-Dev": {
      "commandName": "Project",
      "applicationUrl": "http://localhost:0",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "Portless-Staging": {
      "commandName": "Project",
      "applicationUrl": "http://localhost:0",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Staging"
      }
    }
  }
}
```

### Docker Configuration

```json
{
  "profiles": {
    "Portless": {
      "commandName": "Project",
      "applicationUrl": "http://localhost:0",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "Docker": {
      "commandName": "Project",
      "applicationUrl": "http://0.0.0.0:80",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development",
        "ASPNETCORE_URLS": "http://+:80"
      }
    }
  }
}
```

## Troubleshooting

### Profile Not Available

**Issue:** "Portless" profile doesn't appear in Visual Studio dropdown

**Solution:**
1. Ensure `launchSettings.json` is in the `Properties/` folder
2. Verify JSON is valid (no syntax errors)
3. Reload the project in Visual Studio

### Wrong Port Used

**Issue:** Application runs on port 5000 instead of Portless-assigned port

**Solution:** Ensure `Program.cs` reads the PORT variable:
```csharp
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}
```

### applicationUrl Ignored

**Issue:** The application ignores the `applicationUrl` in launchSettings.json

**Cause:** `Program.cs` configuration overrides launchSettings.json

**Solution:** Ensure Program.cs respects the PORT variable:
```csharp
// Check PORT first (Portless)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}
else
{
    // Fallback to default behavior (respects launchSettings.json)
}
```

## Best Practices

1. **Preserve existing profiles** - Keep your http/https profiles for non-Portless development
2. **Use descriptive profile names** - "Portless", "Portless-Dev", "Docker"
3. **Document profile usage** - Add comments explaining when to use each profile
4. **Separate concerns** - Different profiles for development, staging, production
5. **Test all profiles** - Ensure each profile works as expected

## See Also

- [Tutorial 1: Migrating an Existing Project](../tutorials/01-migration.md) - Step-by-step migration guide
- [appsettings.json Integration](./appsettings.md) - Configuration-based approach
- [Kestrel Configuration](./kestrel-configuration.md) - Advanced Kestrel options

## Schema Reference

Full `launchSettings.json` schema: [json.schemastore.org/launchsettings.json](http://json.schemastore.org/launchsettings.json)

Example schema validation:
```json
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": { }
}
```
