# Phase 06: .NET Integration - Context

**Created:** 2025-02-21
**Status:** Discussion in progress

## Phase Overview

**Goal:** Empaquetado como dotnet tool global con ejemplos de integración para proyectos .NET

**Dependencies:**
- Phase 5 (Process Management) - Completed
- Phase 4 (Port Management) - Not blocking for packaging

**Requirements Coverage:**
- DOTNET-01: CLI funciona como `dotnet tool` global
- DOTNET-02: Ejemplos para integrar con launchSettings.json
- DOTNET-03: Ejemplos para integrar con appsettings.json

## User Decisions from Discussion

### Packaging Strategy
- **Single tool package**: Portless.NET.Tool contiene todo el tool (sin separar core library)
- **Installation automation**: Scripts cross-platform (PowerShell + Bash) siguiendo patrón de Microsoft
- **Validation level**: Install + path check verification

### Examples Scope
- **Full-stack coverage**: ASP.NET Core Web API + Blazor Web App + Worker Service + Console app
- **Structure**: Single solution `Portless.Samples.sln` con 4 proyectos independientes
- **Ecosystem focus**: Solo ASP.NET Core (no Azure Functions, MAUI, etc.)

### Documentation Strategy
- **Tutorial series**: 4 tutoriales completos
  1. Migración de proyecto existente
  2. Proyecto desde cero
  3. Escenario de microservicios
  4. Testing E2E con URLs estables

### Key Technical Decisions

#### Dotnet Tool Configuration
```xml
<PackAsTool>true</PackAsTool>
<ToolCommandName>portless</ToolCommandName>
<PackageId>Portless.NET.Tool</PackageId>
```

#### Installation Scripts
- `scripts/install.sh` - Linux/macOS
- `scripts/install.ps1` - Windows
- Follow Microsoft's dotnet-install pattern
- PATH configuration and verification

#### Examples Structure
```
Examples/
├── Portless.Samples.sln
├── WebApi/           # ASP.NET Core Web API
├── BlazorApp/        # Blazor Web App
├── WorkerService/    # Background service
└── ConsoleApp/       # Console application
```

#### Documentation Structure
```
docs/
├── tutorials/
│   ├── 01-migration.md
│   ├── 02-from-scratch.md
│   ├── 03-microservices.md
│   └── 04-e2e-testing.md
└── integration/
    ├── launch-settings.md
    ├── appsettings.md
    └── kestrel-configuration.md
```

## Research Summary

Confidence: **HIGH**

Key findings from research:
- `.NET 10` simplifica tool packaging con `PackAsTool` (no custom nuspec needed)
- `Native AOT` enabled by default but needs testing with Spectre.Console.Cli
- PORT variable integration requires code-based configuration or `http://localhost:0` in launchSettings.json
- Installation automation should use separate shell + PowerShell scripts (standard pattern)
- Markdown-first documentation with optional DocFX integration for API docs

## Critical Success Factors

1. **Tool packaging works flawlessly** - PackAsTool configuration must be correct
2. **Examples run out-of-the-box** - Users can execute examples without errors
3. **Tutorials are comprehensive** - Cover all 4 scenarios with step-by-step instructions
4. **Installation is automated** - Scripts handle PATH and verification automatically
5. **Documentation is discoverable** - Clear structure and navigation

## Known Risks & Mitigations

### Risk 1: Native AOT Compatibility with Spectre.Console.Cli
- **Impact**: HIGH - Would require fallback to framework-dependent deployment
- **Probability**: LOW - Spectre.Console generally works with AOT
- **Mitigation**: Test AOT compilation early in Wave 1

### Risk 2: Examples Complexity
- **Impact**: MEDIUM - Could overwhelm users with too much code
- **Probability**: MEDIUM - Full-stack examples are inherently complex
- **Mitigation**: Keep examples minimal, focus on Portless integration only

### Risk 3: Documentation Maintenance Burden
- **Impact**: MEDIUM - Documentation can become outdated
- **Probability**: HIGH - Code evolves faster than docs
- **Mitigation**: Code-first documentation with executable examples

## Open Questions (Resolved)

### Q1: Single vs Multi-Package Strategy
**Resolved:** Single tool package (Portless.NET.Tool)
**Rationale:** User preference, simpler distribution, aligns with modern .NET patterns

### Q2: Examples Structure
**Resolved:** Single solution with 4 projects
**Rationale:** Easier to navigate, consistent dependencies, independent execution

### Q3: Installation Automation Approach
**Resolved:** Separate bash + PowerShell scripts
**Rationale:** Follows Microsoft's pattern, no dotnet-script dependency, better shell completion

### Q4: Documentation Format
**Resolved:** Markdown tutorials with code examples
**Rationale:** Sufficient for tutorials, potential DocFX integration later for API docs

## Next Steps

1. ✓ Research completed (2025-02-21)
2. → Create detailed implementation plans (06-01-PLAN.md, 06-02-PLAN.md)
3. → Execute plans in wave-based approach
4. → Verify all success criteria
5. → User acceptance testing

## Verification Criteria

Phase will be considered complete when:
1. [ ] `dotnet tool install --global Portless.NET.Tool` works
2. [ ] `portless` command is available after installation
3. [ ] All 4 example projects run successfully with Portless
4. [ ] All 4 tutorials are complete and tested
5. [ ] Installation scripts work on Windows, macOS, and Linux

---

**Last updated:** 2025-02-21 during discussion phase
