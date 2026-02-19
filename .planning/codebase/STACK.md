# Technology Stack

**Analysis Date:** 2026-02-19

## Languages

**Primary:**
- C# 10.0 - Core language used across all projects
- .NET 10.0 - Runtime framework

## Runtime

**Environment:**
- .NET 10.0 - Target runtime framework
- Console application support (CLI)
- Web application support (ASP.NET Core)

**Package Manager:**
- NuGet - Primary package manager for .NET dependencies
- Lockfile: Implicit via project file references

## Frameworks

**Core:**
- .NET 10.0 - Base runtime framework

**Web:**
- ASP.NET Core - Web framework for proxy service (Portless.Proxy)
- Yarp.ReverseProxy 2.3.0 - Reverse proxy library

**CLI:**
- Spectre.Console.Cli 0.53.1 - Command-line interface framework

**Testing:**
- xunit 2.9.3 - Unit testing framework
- xunit.runner.visualstudio 3.1.4 - Test runner for Visual Studio
- Microsoft.NET.Test.Sdk 17.14.1 - Test SDK
- coverlet.collector 6.0.4 - Code coverage tool

## Key Dependencies

**Critical:**
- Yarp.ReverseProxy 2.3.0 - Core proxy functionality
- Spectre.Console.Cli 0.53.1 - CLI interface

**Infrastructure:**
- Microsoft.NET.Sdk.Web - Web project templates
- Microsoft.NET.Sdk - Standard project templates
- Microsoft.NET.Test.Sdk - Testing infrastructure

## Configuration

**Environment:**
- appsettings.json - Main configuration file
- appsettings.Development.json - Development configuration
- Environment-specific settings via JSON
- No external configuration management detected

**Build:**
- MSBuild - Build system via .NET SDK
- TargetFramework: net10.0
- ImplicitUsings: enabled
- Nullable: enabled

## Platform Requirements

**Development:**
- .NET 10.0 SDK
- Visual Studio or VS Code with .NET support

**Production:**
- .NET 10.0 Runtime
- Cross-platform support (Windows/Linux/macOS)

---

*Stack analysis: 2026-02-19*