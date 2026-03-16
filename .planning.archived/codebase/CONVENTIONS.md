# Coding Conventions

**Analysis Date:** 2026-02-19

## Naming Patterns

**Files:**
- PascalCase for class files (Program.cs)
- kebab-case for project names in directory structure
- No clear pattern for file naming as project has minimal source files

**Functions:**
- PascalCase for public methods (e.g., `MapReverseProxy()`, `UpdateConfigRequest()`)
- Lowercase for built-in methods (e.g., `Write()`, `CreateBuilder()`, `Build()`)

**Variables:**
- PascalCase for public properties and records (e.g., `UpdateConfigRequest`)
- camelCase for local variables (e.g., `builder`, `app`, `config`)

**Types:**
- PascalCase for class and record names (e.g., `UpdateConfigRequest`)
- Record type used for data transfer objects

## Code Style

**Formatting:**
- No explicit formatter configuration detected
- Visual Studio may be using default C# formatting
- Implicit usings enabled in all projects

**Linting:**
- No custom linting rules detected
- IDE likely providing default C# linting
- No .editorconfig found

**Readability:**
- Minimal codebase allows for clear readability
- Direct implementation without unnecessary complexity

## Import Organization

**Order:**
1. Framework namespaces (e.g., `using Yarp.ReverseProxy.Configuration;`)
2. Project-specific imports (minimal in this codebase)
3. No grouping conventions needed due to small scale

**Path Aliases:**
- No path aliases detected
- Direct imports using fully qualified namespaces

## Error Handling

**Patterns:**
- Basic error handling present in proxy endpoint
- No explicit try-catch blocks observed
- Returning HTTP results for API endpoints

**HTTP Error Handling:**
- Using ASP.NET Core Results pattern (e.g., `Results.Ok()`)
- No custom error classes detected

## Logging

**Framework:** ASP.NET Core built-in logging

**Patterns:**
- Logging configuration in `appsettings.json`
- Log level hierarchy configured
- No custom logging patterns observed

## Comments

**When to Comment:**
- Minimal commenting in current codebase
- Code appears self-documenting due to small scale

**XML Documentation:**
- No XML doc comments detected
- Namespace comments not present

## Function Design

**Size:**
- Small functions (typically < 10 lines)
- Single responsibility per method

**Parameters:**
- Direct parameter passing
- Records used for complex parameter objects

**Return Values:**
- Results pattern for HTTP endpoints
- Records for data structures

## Module Design

**Exports:**
- No explicit module exports
- Direct class usage

**Barrel Files:**
- Not used in this minimal codebase

---

*Convention analysis: 2026-02-19*