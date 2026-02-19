# Testing Patterns

**Analysis Date:** 2026-02-19

## Test Framework

**Runner:**
- xUnit 2.9.3
- Microsoft.NET.Test.Sdk 17.14.1
- xunit.runner.visualstudio 3.1.4

**Config:**
- No explicit test configuration files
- Test project reference in global usings: `<Using Include="Xunit" />`
- Minimal test project setup

**Run Commands:**
```bash
dotnet test                 # Run all tests
dotnet test --logger "console;verbosity=detailed"  # Detailed output
```

## Test File Organization

**Location:**
- Dedicated `Portless.Tests` project
- Separate from source code
- No test classes currently present

**Naming:**
- No test files detected
- Conventional naming pattern likely (e.g., `*Tests.cs`)

**Structure:**
```
Portless.Tests/
├── Portless.Tests.csproj   # Test project configuration
└── [No test files yet]
```

## Test Structure

**Suite Organization:**
- No test suites detected
- xUnit pattern likely to be used when tests are added

**Patterns:**
- No patterns observed yet
- Expected: `[Fact]` attributes for unit tests
- `[Theory]` with `[InlineData]` for parameterized tests

## Mocking

**Framework:** No mocking framework detected

**Patterns:**
- No mocking patterns observed
- Likely to use Moq or similar when tests are added

**What to Mock:**
- Not applicable yet
- Expected: HTTP clients, services, dependencies

**What NOT to Mock:**
- Not applicable yet
- Expected: Framework classes, built-in types

## Fixtures and Factories

**Test Data:**
- No test fixtures detected
- No factory classes

**Location:**
- No dedicated test data directory
- Likely to use inline data or setup methods

## Coverage

**Requirements:** coverlet.collector 6.0.4 present

**View Coverage:**
```bash
dotnet test --collect:"XPlat Code Coverage"
dotnet reportgenerator -reports:coverage.xml -targetdir:coverage-report
```

**Coverage Target:** No explicit target detected in configuration

## Test Types

**Unit Tests:**
- Not present yet
- Expected to test individual components

**Integration Tests:**
- Not present yet
- Expected to test proxy functionality

**E2E Tests:**
- Not present yet
- Framework likely: Playwright or Selenium

## Common Patterns

**Async Testing:**
- No async tests detected yet
- Expected: `Task<T>` return types, `await` in tests

**Error Testing:**
- No error testing patterns observed
- Expected: `[ExpectedException]` or try-catch in tests

---

*Testing analysis: 2026-02-19*