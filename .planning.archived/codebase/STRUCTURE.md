# Codebase Structure

**Analysis Date:** 2026-02-19

## Directory Layout

```
portless-dotnet/
├── .planning/                 # Planning documentation
├── .vscode/                  # VS Code configuration
├── Portless.Cli/             # Command-line interface project
├── Portless.Core/            # Core library project
├── Portless.Proxy/           # Reverse proxy project
├── Portless.Tests/           # Test project
├── Portless.slnx            # Solution file
├── README.md                # Project documentation
├── CLAUDE.md                # Claude-specific configuration
├── PRD.md                   # Product requirements document
└── PLAN.md                  # Project plan
```

## Directory Purposes

**Portless.Cli/:
- Purpose: Command-line application for user interaction
- Contains: Console application entry point
- Key files: `Program.cs`, `Portless.Cli.csproj`

**Portless.Core/:
- Purpose: Shared business logic and utilities
- Contains: Placeholder for core functionality
- Key files: `Portless.Core.csproj` (currently empty)

**Portless.Proxy/:
- Purpose: HTTP reverse proxy with dynamic configuration
- Contains: Web application with YARP integration
- Key files: `Program.cs`, `Portless.Proxy.csproj`

**Portless.Tests/:
- Purpose: Unit and integration tests
- Contains: Test infrastructure (currently empty)
- Key files: `Portless.Tests.csproj`

**.planning/:
- Purpose: Project planning and documentation
- Contains: Codebase analysis documents

## Key File Locations

**Entry Points:**
- `Portless.Cli/Program.cs`: CLI application entry point
- `Portless.Proxy/Program.cs`: Web server entry point

**Configuration:**
- `Portless.slnx`: Solution definition file
- `*.csproj`: Project-specific build configurations

**Core Logic:**
- `Portless.Proxy/Program.cs`: Reverse proxy implementation
- `Portless.Core/`: Shared functionality (placeholder)

**Testing:**
- `Portless.Tests/`: Test files (currently empty)

## Naming Conventions

**Files:**
- PascalCase for class files (e.g., `Program.cs`)
- kebab-case for documentation files

**Projects:**
- PascalCase following .NET conventions (e.g., `Portless.Cli`)

**Variables:**
- PascalCase for public properties
- camelCase for private fields and parameters

## Where to Add New Code

**New CLI Command:**
- Implementation: `Portless.Cli/`
- Tests: `Portless.Tests/`

**New Core Functionality:**
- Implementation: `Portless.Core/`
- Tests: `Portless.Tests/`

**New Proxy Feature:**
- Implementation: `Portless.Proxy/Program.cs`
- Tests: `Portless.Tests/`

**New API Endpoint:**
- Implementation: `Portless.Proxy/Program.cs`
- Tests: `Portless.Tests/`

## Special Directories

**obj/ directories:**
- Purpose: Build artifacts and intermediate files
- Generated: Yes
- Committed: No (excluded via .gitignore)

**bin/ directories:**
- Purpose: Compiled output files
- Generated: Yes
- Committed: No (excluded via .gitignore)

---

*Structure analysis: 2026-02-19*
