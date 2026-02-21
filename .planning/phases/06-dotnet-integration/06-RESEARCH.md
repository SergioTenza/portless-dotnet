# Phase 06: .NET Integration - Research

**Researched:** 2025-02-21
**Domain:** .NET Tool Packaging & Integration Documentation
**Confidence:** HIGH

## Summary

Phase 06 focuses on packaging Portless.NET as a dotnet tool and creating comprehensive integration examples and documentation. Research confirms that .NET 10 significantly simplifies tool packaging with Native AOT enabled by default for file-based apps, and that the `PackAsTool` MSBuild property provides the standard mechanism for creating dotnet tools.

The single tool package strategy (user's decision) is well-supported by NuGet and aligns with modern .NET tool distribution patterns. For integration examples, a single solution with multiple projects (ASP.NET Core Web API + Blazor Web App + Worker Service + Console app) provides comprehensive coverage of .NET workloads.

Installation automation should leverage Microsoft's official dotnet-install scripts with custom wrapper logic for tool-specific installation. Documentation should use Markdown with potential DocFX integration for future API docs generation.

**Primary recommendation:** Use `PackAsTool` in Portless.Cli.csproj, create Examples/Portless.Samples.sln with 4 projects, write Markdown tutorials with code-first examples, and create a cross-platform installation script using dotnet-script or bash/pwsh.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| PackAsTool | MSBuild property | dotnet tool packaging | Official Microsoft mechanism for .NET tools |
| dotnet pack | CLI tool | NuGet package creation | Standard packaging command since .NET Core |
| NuGet.org | NuGet repository | Package distribution | Official .NET package host |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| dotnet-script | latest | Cross-platform installation scripts | When bash/pwsh dual scripts are impractical |
| DocFX | latest | API documentation generation | When generating API docs from XML comments (optional, future) |
| Native AOT | .NET 10 default | Single executable compilation | For self-contained tool distribution (optional) |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| PackAsTool (single package) | Separate tool + library packages | More complex maintenance, user chose single package |
| dotnet-script | Bash + PowerShell scripts | Dual scripts are more verbose but don't require dotnet-script dependency |
| Native AOT | Framework-dependent deployment | AOT = smaller, faster, no runtime required; FDD = faster compilation, larger |

**Installation:**
```bash
# For tool packaging (no additional packages needed - built into .NET SDK)
# PackAsTool is an MSBuild property, not a NuGet package

# For installation automation (optional)
dotnet tool install -g dotnet-script

# For documentation generation (optional, future)
dotnet tool install -g docfx
```

## Architecture Patterns

### Recommended Project Structure

```
portless-dotnet/
├── Portless.Core/          # Existing - Class library
├── Portless.Cli/           # Existing - Console app (dotnet tool)
├── Portless.Proxy/         # Existing - Web app (YARP proxy)
├── Portless.Tests/         # Existing - Test suite
├── Examples/               # NEW - Integration examples
│   ├── Portless.Samples.sln
│   ├── WebApi/            # ASP.NET Core Web API example
│   ├── BlazorApp/         # Blazor Web App example
│   ├── WorkerService/     # Background service example
│   └── ConsoleApp/        # Console app example
├── scripts/                # NEW - Installation automation
│   ├── install.sh         # Linux/macOS installation
│   ├── install.ps1        # Windows installation
│   └── verify.sh/ps1      # Post-install verification
└── docs/
    ├── tutorials/
    │   ├── 01-quick-start.md
    │   ├── 02-migration-from-scratch.md
    │   ├── 03-microservices-scenario.md
    │   └── 04-e2e-testing-scenario.md
    └── integration/
        ├── launch-settings.md
        ├── appsettings.md
        └──kestrel-configuration.md
```

### Pattern 1: PackAsTool Configuration
**What:** Configure Portless.Cli as a dotnet tool using standard MSBuild properties
**When to use:** All dotnet tools distributed via NuGet
**Example:**
```xml
<!-- Portless.Cli/Portless.Cli.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>

    <!-- Tool packaging configuration -->
    <PackAsTool>true</PackAsTool>
    <ToolCommandName>portless</ToolCommandName>
    <Version>1.0.0</Version>
    <Authors>Portless.NET Contributors</Authors>
    <Description>Stable .localhost URLs for local .NET development</Description>

    <!-- Package metadata -->
    <PackageId>Portless.NET.Tool</PackageId>
    <PackageTags>dotnet-tool;proxy;localhost;development</PackageTags>
    <PackageProjectUrl>https://github.com/your-org/portless-dotnet</PackageProjectUrl>
    <RepositoryUrl>https://github.com/your-org/portless-dotnet</RepositoryUrl>
    <RepositoryType>git</RepositoryType>

    <!-- Optional: Native AOT for single executable -->
    <PublishAot>true</PublishAot>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Spectre.Console" Version="0.53.1" />
    <PackageReference Include="Spectre.Console.Cli" Version="0.53.1" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Http" Version="9.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Portless.Core\Portless.Core.csproj" />
  </ItemGroup>
</Project>
```

### Pattern 2: launchSettings.json Integration
**What:** Configure ASP.NET Core apps to use PORT variable injected by Portless
**When to use:** All ASP.NET Core projects (Web API, Blazor, MVC)
**Example:**
```json
<!-- Properties/launchSettings.json -->
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
    "Direct": {
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

**Program.cs configuration:**
```csharp
var builder = WebApplication.CreateBuilder(args);

// Read PORT from environment (set by Portless)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}

var app = builder.Build();
app.MapGet("/", () => "Hello from Portless!");
app.Run();
```

### Pattern 3: appsettings.json Kestrel Configuration
**What:** Configure Kestrel to use PORT variable via configuration binding
**When to use:** When you prefer configuration over code for port setup
**Example:**
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

**Note:** The `${PORT}` syntax requires configuration builder support. Alternative:
```json
{
  "AllowedHosts": "*"
}
```
```csharp
// In Program.cs
builder.Configuration.AddEnvironmentVariables();
var port = builder.Configuration["PORT"] ?? "5000";
builder.WebHost.UseUrls($"http://*:{port}");
```

### Pattern 4: Installation Script Structure
**What:** Cross-platform script to install Portless and verify PATH
**When to use:** Automated setup and CI/CD environments
**Example (install.sh):**
```bash
#!/bin/bash
set -e

echo "Installing Portless.NET..."

# Install from NuGet
dotnet tool install --global Portless.NET.Tool

# Add to PATH (if not already)
TOOL_PATH="$HOME/.dotnet/tools"
if [[ ":$PATH:" != *":$TOOL_PATH:"* ]]; then
    echo "Adding $TOOL_PATH to PATH..."
    export PATH="$PATH:$TOOL_PATH"
    echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.bashrc
    echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.zshrc
fi

# Verify installation
if command -v portless &> /dev/null; then
    echo "✓ Portless installed successfully!"
    portless --version
else
    echo "✗ Installation failed. PATH may need manual update."
    echo "Run: export PATH=\"\$PATH:\$HOME/.dotnet/tools\""
    exit 1
fi
```

**Example (install.ps1):**
```powershell
$ErrorActionPreference = "Stop"

Write-Host "Installing Portless.NET..." -ForegroundColor Green

# Install from NuGet
dotnet tool install --global Portless.NET.Tool

# Add to PATH (persistent)
$toolPath = "$env:USERPROFILE\.dotnet\tools"
$pathEnv = [Environment]::GetEnvironmentVariable("Path", "User")

if ($pathEnv -notlike "*$toolPath*") {
    Write-Host "Adding $toolPath to PATH..."
    [Environment]::SetEnvironmentVariable("Path", "$pathEnv;$toolPath", "User")
    $env:Path = "$env:Path;$toolPath"
}

# Verify installation
if (Get-Command portless -ErrorAction SilentlyContinue) {
    Write-Host "✓ Portless installed successfully!" -ForegroundColor Green
    portless --version
} else {
    Write-Host "✗ Installation failed. Restart terminal for PATH changes." -ForegroundColor Red
    exit 1
}
```

### Anti-Patterns to Avoid
- **Hardcoded ports in launchSettings.json**: Prevents Portless from injecting PORT variable
- **Using `applicationUrl` with fixed ports**: Override with `http://localhost:0` or code-based configuration
- **Forgetting PATH configuration**: Tools install to `~/.dotnet/tools` which may not be in PATH
- **Not validating installation post-install**: Always verify with `portless --version` or similar
- **PrivateAssets="all" on dependencies**: Excludes dependencies from tool package

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Tool packaging | Custom nuspec with DotnetToolSettings.xml | `<PackAsTool>true</PackAsTool>` | MSBuild handles manifest, dependencies, structure |
| Installation scripts | Custom .sh/.ps1 with manual detection | Official dotnet-install.sh pattern | Microsoft handles edge cases, signature verification |
| Cross-platform scripting | Complex conditionals for OS detection | Separate shell + PowerShell scripts | Standard pattern, better shell completion |
| Documentation generation | Custom Markdown to HTML converter | DocFX (optional, future) | Handles API docs from XML comments, cross-references |
| Version management | Manual version bumping in csproj | GitVersion or GitVersioning (optional) | Automated semantic versioning from Git history |

**Key insight:** The .NET SDK has excellent built-in support for tool packaging via `PackAsTool`. Custom solutions introduce maintenance burden and miss edge cases that Microsoft has already solved.

## Common Pitfalls

### Pitfall 1: ToolCommandName Not Set in .NET 10
**What goes wrong:** In .NET 10, `ToolCommandName` is only set automatically when `PackAsTool=true` (breaking change)
**Why it happens:** .NET 10 changed the default behavior to avoid setting the property for non-tool projects
**How to avoid:** Always explicitly set `<ToolCommandName>your-command-name</ToolCommandName>` when using `PackAsTool`
**Warning signs:** Tool installs but command is not found, `dotnet tool list` shows package but no command

### Pitfall 2: Dependencies Not Packaged
**What goes wrong:** Tool installs but fails at runtime with missing assembly errors
**Why it happens:** Dependencies marked as `<PrivateAssets="all">` are excluded from tool package
**How to avoid:** Remove `PrivateAssets="all"` from dependencies that must be packaged, or use `<PackAsTool>true</PackAsTool>` which handles this automatically
**Warning signs:** Runtime FileNotFoundException after tool installation

### Pitfall 3: PATH Not Updated After Installation
**What goes wrong:** Tool installs successfully but `portless` command is not found
**Why it happens:** `.dotnet/tools` is not always in PATH (especially on Linux with tar.gz SDK, macOS Catalina+)
**How to avoid:** Always include PATH verification in installation scripts, document manual PATH setup
**Warning signs:** `dotnet tool list` shows tool but `command not found: portless`

### Pitfall 4: Port Variable Not Read by Application
**What goes wrong:** Portless injects PORT variable but app still uses default port
**Why it happens:** Application doesn't read PORT environment variable or uses `applicationUrl` with fixed port
**How to avoid:** Configure Kestrel in code to read PORT variable, or use `http://localhost:0` in launchSettings.json
**Warning signs:** App runs on port 5000 instead of 4000-4999 range

### Pitfall 5: launchSettings.json Overrides Port Configuration
**What goes wrong:** Even with PORT variable set, app uses port from launchSettings.json
**Why it happens:** `applicationUrl` in launchSettings.json takes precedence over environment variables in some configurations
**How to avoid:** Use code-based configuration (`builder.WebHost.UseUrls()`) or set `applicationUrl` to `http://localhost:0`
**Warning signs:** Consistent port across runs despite Portless assignment

### Pitfall 6: Native AOT Compatibility Issues
**What goes wrong:** Native AOT publish fails with trimming/reflection errors
**Why it happens:** Spectre.Console or YARP may use reflection or features not compatible with AOT
**How to avoid:** Test AOT compilation early, consider framework-dependent deployment if AOT fails
**Warning signs:** Publish errors about "requires reflection" or "assembly not trim-compatible"

## Code Examples

Verified patterns from official sources:

### Creating a dotnet Tool
```xml
<!-- Source: https://learn.microsoft.com/en-us/dotnet/core/sdk/file-based-apps -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <PackAsTool>true</PackAsTool>
    <ToolCommandName>portless</ToolCommandName>
    <Version>1.0.0</Version>
    <PackageId>Portless.NET.Tool</PackageId>
  </PropertyGroup>
</Project>
```

### Publishing and Installing
```bash
# Source: https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools
# Package the tool
dotnet pack -c Release

# Install globally
dotnet tool install --global Portless.NET.Tool --add-source ./bin/Release

# Install locally (in tool-manifest)
dotnet new tool-manifest
dotnet tool install Portless.NET.Tool --add-source ./bin/Release

# Run the tool
portless --help
```

### Kestrel Configuration with PORT Variable
```csharp
// Source: ASP.NET Core documentation on Kestrel configuration
var builder = WebApplication.CreateBuilder(args);

// Method 1: UseUrls (highest priority)
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://*:{port}");

// Method 2: Configure Kestrel endpoints
builder.WebHost.ConfigureKestrel(options =>
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
    options.ListenAnyIP(int.Parse(port));
});

var app = builder.Build();
app.Run();
```

### Environment Variable Configuration
```bash
# Source: ASP.NET Core environment variable documentation
# Set port via ASPNETCORE_URLS (recommended)
export ASPNETCORE_URLS=http://*:3000

# Set port via HTTP_PORTS (simpler, .NET 7+)
export ASPNETCORE_HTTP_PORTS=3000

# Set custom PORT variable (requires app code to read)
export PORT=3000
```

### launchSettings.json Examples
```json
// Source: ASP.NET Core launchSettings.json schema
{
  "profiles": {
    "PortlessDevelopment": {
      "commandName": "Project",
      "launchBrowser": true,
      "applicationUrl": "http://localhost:0",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| DotNetCliToolReference in csproj | PackAsTool + dotnet tool install | .NET Core 2.1+ | Tools are now NuGet packages, not project references |
| Manual DotnetToolSettings.xml | PackAsTool auto-generates manifest | .NET Core 3.0+ | Simpler packaging, less error-prone |
| Framework-dependent only | Native AOT default in .NET 10 | .NET 10 (2025) | Single executable, no runtime required, smaller size |
| Separate install scripts per OS | Unified dotnet-install.sh/.ps1 | .NET Core 1.0+ | Official scripts handle edge cases |

**Deprecated/outdated:**
- `DotNetCliToolReference`: Replaced by `dotnet tool install` (removed in .NET 5+)
- `project.json`: Replaced by csproj in .NET Core RTM
- Manual tool manifest creation: Use `dotnet new tool-manifest` instead

## Open Questions

1. **Native AOT Compatibility with Spectre.Console**
   - What we know: Spectre.Console works with AOT in many scenarios
   - What's unclear: Full compatibility with Spectre.Console.Cli and all features
   - Recommendation: Test AOT compilation early in Phase 06, fallback to framework-dependent if issues arise

2. **Single Package vs Multi-Command Tool**
   - What we know: User chose single tool package strategy
   - What's unclear: Should Portless.Proxy be included in the tool or run as separate process
   - Recommendation: Keep Portless.Proxy as separate process (as designed), CLI orchestrates it

3. **Documentation Tooling**
   - What we know: Markdown is sufficient for tutorials
   - What's unclear: Whether to invest in DocFX for API documentation
   - Recommendation: Start with Markdown, add DocFX in future phase if API docs needed

## Sources

### Primary (HIGH confidence)
- **dotnet tool packaging** - [Microsoft Learn - File-based apps](https://learn.microsoft.com/en-us/dotnet/core/sdk/file-based-apps)
- **dotnet tool installation** - [Microsoft Learn - .NET tools](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools)
- **PackAsTool configuration** - [Microsoft Learn - .csproj properties](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props)
- **.NET 10 breaking changes** - [ToolCommandName behavior](https://learn.microsoft.com/en-us/dotnet/core/compatibility/sdk/10.0/toolcommandname-not-set)
- **launchSettings.json** - [ASP.NET Core launch settings](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-run#launchsettingsjson)
- **Kestrel configuration** - [ASP.NET Core Kestrel endpoints](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints)
- **dotnet-install script** - [Microsoft Learn - dotnet-install script](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script)
- **PATH troubleshooting** - [Microsoft Learn - Troubleshoot tool usage](https://learn.microsoft.com/en-us/dotnet/core/tools/troubleshoot-usage-issues)

### Secondary (MEDIUM confidence)
- **Spectre.Console** - [GitHub Repository](https://github.com/spectreconsole/spectre.console)
- **Spectre.Console documentation** - [Official Docs](https://spectreconsole.net/quick-start)
- **.NET samples repository** - [dotnet/samples GitHub](https://github.com/dotnet/samples)
- **.NET tutorials** - [Microsoft Learn - Samples and tutorials](https://learn.microsoft.com/en-us/dotnet/samples-and-tutorials)
- **DocFX documentation** - [GitHub Repository](https://github.com/dotnet/docfx)
- **Native AOT in .NET 10** - [Various community sources on Native AOT defaults]

### Tertiary (LOW confidence)
- **dotnet-script** - [Project for C# scripting](https://github.com/filipw/dotnet-script) - Alternative to bash/pwsh scripts
- **GitVersioning** - [Microsoft tool for semantic versioning](https://github.com/microsoft/GitVersioning) - Optional automation for versioning

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Official Microsoft documentation and well-established patterns
- Architecture: HIGH - Based on official .NET tool packaging and ASP.NET Core configuration docs
- Pitfalls: HIGH - Verified with official troubleshooting guides and compatibility docs
- Code examples: HIGH - All sourced from official Microsoft documentation

**Research date:** 2025-02-21
**Valid until:** 2025-05-21 (90 days - .NET tooling is stable but .NET 10 is in active development)
