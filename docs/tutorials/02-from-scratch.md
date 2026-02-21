# Tutorial 2: Creating a New Project with Portless

This tutorial shows how to create a new ASP.NET Core project with Portless.NET integration from the start, ensuring you have stable .localhost URLs from day one.

## Prerequisites

- .NET 10 SDK installed
- Portless.NET installed: `dotnet tool install --global Portless.NET.Tool`

## Overview

Creating a new project with Portless involves:
1. Creating a new ASP.NET Core project
2. Configuring PORT integration
3. Setting up launchSettings.json
4. Running with Portless

## Step 1: Create a New Project

Create a new ASP.NET Core Web API project:

```bash
# Create a new Web API project
dotnet new webapi -n MyPortlessApp

# Navigate to the project directory
cd MyPortlessApp
```

Or if you prefer using Visual Studio or VS Code:
- Create a new "ASP.NET Core Web API" project
- Name it `MyPortlessApp`
- Select .NET 10 as the framework

## Step 2: Configure PORT Integration

Open `Program.cs` and add Portless integration at the beginning of the file, right after creating the builder:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Portless integration: Read PORT from environment
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();
```

**Best Practice:** Add this code at the very beginning of your Program.cs, immediately after creating the WebApplicationBuilder. This ensures the PORT configuration is applied before the application is built.

## Step 3: Update launchSettings.json

Open `Properties/launchSettings.json` and configure it for Portless:

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

**Key Points:**
- The "Portless" profile uses `applicationUrl: "http://localhost:0"` for dynamic port assignment
- The "http" profile is preserved for direct execution without Portless
- You can add an "https" profile if needed

## Step 4: Add a Test Endpoint

Add a simple controller to test the integration:

```csharp
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class HelloController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new {
            message = "Hello from Portless!",
            timestamp = DateTime.UtcNow,
            port = Environment.GetEnvironmentVariable("PORT") ?? "not set"
        });
    }
}
```

## Step 5: Run with Portless

Start the Portless proxy:

```bash
portless proxy start
```

Run your application with Portless:

```bash
# From your project directory
portless myapp dotnet run
```

Access your application at: `http://myapp.localhost/Hello`

**Expected Response:**
```json
{
  "message": "Hello from Portless!",
  "timestamp": "2025-02-21T10:30:00Z",
  "port": "4001"
}
```

## Best Practices for New Projects

### 1. Always Add PORT Integration First

Add the PORT configuration code immediately after creating the WebApplicationBuilder. This prevents forgetting to add it later and ensures consistent behavior across all environments.

### 2. Use Descriptive Hostnames

Choose hostnames that reflect your application's purpose:
```bash
portless myapi dotnet run      # Good: descriptive
portless api-v1 dotnet run     # Good: versioned
portless test dotnet run       # Good: indicates environment
```

### 3. Separate Portless and Direct Profiles

Keep separate profiles in launchSettings.json for different scenarios:
- "Portless" profile for development with Portless
- "http" / "https" profiles for direct execution or production testing

### 4. Document PORT Usage

Add a comment in your Program.cs to remind future developers about PORT integration:

```csharp
// Portless integration: Application binds to PORT environment variable
// This allows Portless.NET to assign dynamic ports for stable .localhost URLs
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}
```

### 5. Test Multiple Environments

Ensure your application works in both Portless and direct execution modes:

```bash
# Test with Portless
portless myapp dotnet run
# Access: http://myapp.localhost

# Test without Portless
dotnet run --launch-profile http
# Access: http://localhost:5000
```

## Alternative: Configuration-Based Integration

If you prefer configuration over code, see [appsettings.json Integration](../integration/appsettings.md) for a configuration-based approach that doesn't require code changes.

## Project Templates

For future projects, you can create a custom dotnet template with Portless integration pre-configured:

```bash
# Create a template from your current project
dotnet new install MyPortlessApp

# Use the template for new projects
dotnet new myportlesstemplate -n NewProject
```

## Verification Checklist

Before considering your Portless integration complete:

- [ ] Program.cs includes PORT configuration before `Build()`
- [ ] launchSettings.json has a "Portless" profile with `localhost:0`
- [ ] Application responds correctly at `http://hostname.localhost`
- [ ] Direct execution (without Portless) still works
- [ ] Multiple instances can run with different hostnames

## Next Steps

- [Tutorial 3: Microservices Scenario](03-microservices.md) - Run multiple services
- [Tutorial 4: E2E Testing](04-e2e-testing.md) - Stable URLs for testing
- [Integration Guides](../integration/) - Advanced configuration options

## Troubleshooting

### Application binds to wrong port

**Issue:** Application runs on port 5000 instead of Portless-assigned port

**Solution:** Ensure `UseUrls()` is called before `builder.Build()`. See [Tutorial 1 Troubleshooting](01-migration.md#application-doesnt-use-the-assigned-port).

### Portless command not found

**Issue:** `portless: command not found`

**Solution:** Portless is not in your PATH. Run:
```bash
export PATH="$PATH:$HOME/.dotnet/tools"
```
Or restart your terminal after installation.

### Can't access hostname.localhost

**Issue:** Browser shows DNS error or "can't reach this page"

**Solution:**
1. Ensure proxy is running: `portless proxy status`
2. Check route is registered: `portless list`
3. Try accessing the health endpoint: `curl http://localhost:1355/api/v1/health`

## Example Project

A complete working example is available in the `Examples/WebApi` directory of the Portless.NET repository.
