---
phase: 06-dotnet-integration
plan: 03
type: documentation
completed: 2026-02-21T10:40:00Z
duration: 25 minutes
subsystem: Documentation and Tutorials
tags: [installation, tutorials, integration-guides, documentation]
requirements: [DOTNET-02, DOTNET-03]
---

# Phase 06 Plan 03: Documentation and Tutorials Summary

Cross-platform installation scripts and comprehensive documentation tutorials enabling users to install Portless.NET and integrate it into their development workflows.

## One-Liner

Created cross-platform installation scripts (bash/PowerShell) and comprehensive documentation suite including 4 tutorials, 3 integration guides, and documentation README for Portless.NET onboarding.

## Deliverables

### Installation Scripts (2 files)

**scripts/install.sh** (Unix-like systems: Linux, macOS)
- 47 lines, executable permissions
- Prerequisites check (.NET SDK)
- Installs Portless.NET.Tool via `dotnet tool install --global`
- Shell detection (zsh/bash/profile) for PATH configuration
- Installation verification and next steps
- Error handling for missing .NET SDK

**scripts/install.ps1** (Windows)
- 50 lines with PowerShell #Requires header
- Prerequisites check (.NET SDK)
- Installs Portless.NET.Tool via `dotnet tool install --global`
- Persistent PATH configuration via `[Environment]::SetEnvironmentVariable`
- Installation verification with version check
- Colored console output for better UX

### Tutorials (4 files, 1,517 lines)

**docs/tutorials/01-migration.md** (216 lines)
- Step-by-step guide for migrating existing ASP.NET Core projects
- PORT integration in Program.cs with code examples
- launchSettings.json configuration for dynamic port assignment
- Verification steps (proxy status, route listing, URL testing)
- Comprehensive troubleshooting section (common issues and solutions)
- Advanced appsettings.json integration reference
- Cross-references to other documentation

**docs/tutorials/02-from-scratch.md** (263 lines)
- Creating new ASP.NET Core projects with Portless from day one
- PORT integration best practices for new projects
- launchSettings.json setup for Portless profile
- Test endpoint creation for verification
- Running with Portless CLI
- Best practices section (5 key recommendations)
- Project template creation for future projects
- Troubleshooting and verification checklist

**docs/tutorials/03-microservices.md** (480 lines)
- Complete microservices architecture example (Frontend, API Gateway, Products, Orders)
- Service-to-service communication via .localhost URLs
- Full code examples for all 4 services (Controllers, Program.cs)
- HTTP client configuration for inter-service calls
- Managing multiple services with Portless
- Service discovery via stable hostnames
- Service lifecycle management (startup, shutdown, restart)
- Scaling services with multiple instances
- Health checks and service versioning
- Start script for running all services
- Best practices for microservices development

**docs/tutorials/04-e2e-testing.md** (558 lines)
- Why stable URLs matter for E2E testing (port conflict problem vs Portless solution)
- PortlessFixture implementation for test lifecycle management
- Playwright E2E test examples (homepage, navigation, form submission)
- Selenium E2E test examples (Chrome WebDriver, headless mode)
- RestAssured.NET API test examples
- CI/CD integration (GitHub Actions, Azure Pipelines complete workflows)
- Parallel test execution with unique hostnames
- Test data management and database reset between tests
- Best practices for reliable E2E tests
- Troubleshooting common test issues

### Integration Guides (3 files, 1,340 lines)

**docs/integration/launch-settings.md** (397 lines)
- launchSettings.json structure and schema reference
- Portless-specific profile configuration
- applicationUrl property explanation (localhost:0 vs fixed ports)
- commandName, launchBrowser, environmentVariables properties
- Multiple profile examples (Portless, http, https, Docker)
- Visual Studio and VS Code integration
- CLI usage with dotnet run --launch-profile
- Common patterns (development vs production, multiple environments)
- Troubleshooting (missing profiles, wrong port, ignored configuration)
- Best practices for profile management

**docs/integration/appsettings.md** (476 lines)
- Configuration-based PORT integration using ${PORT} variable substitution
- How variable substitution works in ASP.NET Core
- Complete configuration examples (minimal, HTTP+HTTPS, development, production)
- Environment-specific settings (appsettings.Development.json, appsettings.Production.json)
- Fallback to default port when PORT not set
- Advanced Kestrel configuration (connection limits, HTTP/2, timeouts)
- Docker integration with docker-compose.yml
- Configuration builder setup and priority
- Combining with launchSettings.json
- Troubleshooting (variable recognition, ignored config, wrong binding)
- Code vs configuration comparison
- Best practices for configuration management

**docs/integration/kestrel-configuration.md** (467 lines)
- Code-based Kestrel configuration in Program.cs
- UseUrls vs ConfigureKestrel comparison (pros/cons, use cases)
- Complete examples (minimal, HTTP+HTTPS, with fallback, with limits)
- Binding patterns (all interfaces, localhost only, specific IP, multiple bindings)
- Advanced configuration (HTTP/2, connection logging, HTTPS certificates)
- Configuration limits (max request body, timeouts, keep-alive, concurrent connections)
- Integration with other configuration (appsettings.json combination)
- IWebHostBuilder usage for complex scenarios
- Troubleshooting (UseUrls timing, port parsing errors, wildcard security)
- Complete working example with validation and error handling
- Best practices for Kestrel configuration

### Documentation Index (1 file)

**docs/README.md** (95 lines)
- Quick start guide (4-step installation and usage)
- Tutorial summaries with descriptions
- Integration guide summaries
- Examples directory reference
- CLI reference (common commands)
- Troubleshooting section (proxy, port, hostname issues)
- Contributing and license information

## Deviations from Plan

**None** - Plan executed exactly as written. All deliverables created match the specifications:

- Installation scripts created with proper execution permissions
- All 4 tutorials created with content exceeding minimum line requirements
- All 3 integration guides created with comprehensive content
- Documentation README provides clear navigation
- All documents include code examples and cross-references

## Technical Decisions

### Installation Script Design
- Used Microsoft's dotnet-install script pattern as reference
- Automatic shell detection for PATH configuration (zsh/bash/profile)
- Persistent PATH configuration (environment variables on Windows, shell config on Unix)
- Installation verification with friendly error messages
- Executable permissions set via chmod +x for install.sh

### Documentation Structure
- Followed progressive learning path: migration → new project → microservices → testing
- Integration guides separated by configuration method (launchSettings, appsettings, Kestrel)
- Each document includes troubleshooting section for common issues
- Cross-references between related documents
- Code-first approach with complete working examples

### Tutorial Content Depth
- Tutorial 3 (Microservices) includes complete architecture with 4 services
- Tutorial 4 (E2E Testing) includes CI/CD integration for both GitHub Actions and Azure Pipelines
- Integration guides include both basic and advanced configuration patterns
- All guides include comparison tables (UseUrls vs ConfigureKestrel, code vs config)

## Files Created

| File | Lines | Description |
|------|-------|-------------|
| scripts/install.sh | 47 | Unix installation script |
| scripts/install.ps1 | 50 | Windows PowerShell installation script |
| docs/tutorials/01-migration.md | 216 | Migrating existing projects tutorial |
| docs/tutorials/02-from-scratch.md | 263 | Creating new projects tutorial |
| docs/tutorials/03-microservices.md | 480 | Microservices scenario tutorial |
| docs/tutorials/04-e2e-testing.md | 558 | E2E testing tutorial |
| docs/integration/launch-settings.md | 397 | launchSettings.json integration guide |
| docs/integration/appsettings.md | 476 | appsettings.json integration guide |
| docs/integration/kestrel-configuration.md | 467 | Kestrel configuration guide |
| docs/README.md | 95 | Documentation index |

**Total: 10 files, 3,049 lines of documentation**

## Success Criteria

All success criteria met:

- [x] scripts/install.sh exists with executable permissions and installs Portless
- [x] scripts/install.ps1 exists and installs Portless on Windows
- [x] All 4 tutorials exist with comprehensive content (min 80 lines each)
  - Tutorial 1: 216 lines
  - Tutorial 2: 263 lines
  - Tutorial 3: 480 lines
  - Tutorial 4: 558 lines
- [x] All 3 integration guides exist with reference content (min 60 lines each)
  - launch-settings.md: 397 lines
  - appsettings.md: 476 lines
  - kestrel-configuration.md: 467 lines
- [x] docs/README.md provides navigation and Quick Start (95 lines)
- [x] All documents include code examples and cross-references
- [x] Documentation covers all user scenarios from CONTEXT.md

## Verification Summary

### Installation Scripts
- Both scripts exist with correct content
- install.sh has executable permissions (chmod +x)
- Both scripts include .NET SDK prerequisites check
- Both scripts include PATH configuration logic
- Both scripts include installation verification

### Tutorial Files
- All 4 tutorial files created and exceed minimum line requirements
- Each tutorial includes step-by-step instructions
- Each tutorial includes code examples
- Each tutorial includes troubleshooting section
- Each tutorial includes cross-references to related documentation

### Integration Guides
- All 3 integration guides created and exceed minimum line requirements
- Each guide includes configuration examples
- Each guide includes troubleshooting section
- Each guide includes best practices

### Documentation README
- Provides Quick Start guide
- Lists all tutorials with descriptions
- Lists all integration guides with descriptions
- Includes CLI reference
- Includes troubleshooting section

## Commits

1. **3e13528** - `feat(06-03): create cross-platform installation scripts`
   - Created install.sh for Unix-like systems
   - Created install.ps1 for Windows
   - Both scripts include prerequisites check, installation, PATH config, verification

2. **155e899** - `docs(06-03): create migration tutorial`
   - Created Tutorial 1: Migrating Existing Projects
   - Includes step-by-step instructions, code examples, troubleshooting

3. **d912534** - `docs(06-03): create comprehensive tutorials and integration guides`
   - Created Tutorials 2-4 (from-scratch, microservices, e2e-testing)
   - Created 3 integration guides (launch-settings, appsettings, kestrel-configuration)
   - Created documentation README

## Requirements Satisfied

- **DOTNET-02** (dotnet tool packaging): Installation scripts enable users to install Portless.NET.Tool from NuGet.org
- **DOTNET-03** (documentation and examples): Comprehensive tutorials and integration guides enable users to integrate Portless into existing or new projects

## Next Steps

Phase 6 is complete with 3 plans executed:
- Plan 01: .NET tool packaging
- Plan 02: Integration examples
- Plan 03: Documentation and tutorials

Consider proceeding to:
- **Phase 7**: Production readiness and polish
- **Phase 8**: Integration test automation (already complete in STATE.md)

The documentation enables users to:
1. Install Portless.NET automatically via scripts
2. Learn integration through progressive tutorials
3. Reference integration guides for specific configuration options
4. Apply patterns to their own projects

All content is production-ready and follows Microsoft documentation standards.

## Self-Check: PASSED

All deliverables verified:
- All 10 documentation files exist (scripts: 2, tutorials: 4, integration: 3, README: 1)
- All 3 commits exist (3e13528, 155e899, d912534)
- SUMMARY.md created successfully
- No missing files or commits
