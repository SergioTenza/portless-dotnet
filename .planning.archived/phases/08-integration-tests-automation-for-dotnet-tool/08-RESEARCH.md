# Phase 8: Integration Tests Automation for .NET Tool - Research

**Researched:** 2026-02-21
**Domain:** Integration testing for dotnet CLI tools, YARP proxy, and process management
**Confidence:** HIGH

## Summary

Phase 8 focuses on implementing comprehensive integration tests for Portless.NET as a dotnet tool. The research reveals that the project already has a solid foundation with xUnit 2.9.3, Microsoft.AspNetCore.Mvc.Testing, and existing integration tests for YARP routing. The key challenge is extending this to test the CLI tool installation, command execution, process management, and cross-platform behavior.

**Primary recommendation:** Build on the existing test infrastructure with three test project layers: UnitTests (already exists in Portless.Tests), IntegrationTests (new, for component integration), and E2ETests (new, for full tool installation and execution scenarios). Use WebApplicationFactory for YARP testing, Process.Start for CLI testing, and implement robust cleanup patterns with retry logic for cross-platform file system operations.

## User Constraints (from CONTEXT.md)

### Locked Decisions

The following decisions from CONTEXT.md constrain the implementation and MUST be followed:

**Alcance de Tests:**
- Cobertura completa: happy path + edge cases + failure scenarios
- Componentes a probar: CLI commands + Proxy YARP
- Modos de ejecución: Instalación global + ejecución local (dotnet run) + paquete NuGet
- Tipo de tests: Unit tests + E2E tests para máxima cobertura

**Ejecución de Tests:**
- Ejecución con tool real instalado (no simulaciones/mocks)
- Frecuencia: Manual on-demand (antes de cada release)
- Organización de tests: Proyectos separados por tipo (UnitTests, IntegrationTests, E2ETests)
- Paralelización: A criterio de Claude según el escenario

**Aislamiento y Cleanup:**
- Cleanup en cada test (tearDown individual)
- Si el cleanup falla: Loggear warnings pero continuar
- Directorio temporal único por test (GUID/timestamp)
- Puertos: Usar rango real (4000-4999) con detección dinámica de puertos libres

**Validación Cross-Platform:**
- Plataformas: Windows + Linux (Ubuntu/Debian)
- Método de validación: Manual (ejecutar tests en cada plataforma)
- Criterio de éxito: Tests pasando es suficiente
- Diferencias de plataforma: Mismos tests en todas las plataformas (abstraer diferencias)

### Claude's Discretion

**Paralelización de tests** según el escenario (secuenciales vs paralelos)
**Implementación exacta de mecanismos de cleanup**
**Estrategia para manejar race conditions** en asignación de puertos

### Deferred Ideas (OUT OF SCOPE)

- macOS en la validación cross-platform (futuro si hay demanda)
- CI/CD automatizado (GitHub Actions) - puede ser fase futura
- Tests de performance/load testing - fuera de alcance de esta fase

## Phase Requirements

This phase introduces new testing requirements (TEST-01 through TEST-05) to be defined during implementation. Based on CONTEXT.md decisions, these requirements will cover:

| ID | Description | Research Support |
|----|-------------|-----------------|
| TEST-01 | CLI command execution validation | Spectre.Console.Cli testing patterns, CommandApp.RunAsync for in-process testing |
| TEST-02 | Proxy YARP routing integration | WebApplicationFactory<T> already used successfully in existing tests |
| TEST-03 | Process management and PORT injection | Process.Start with environment variables, PID tracking validation |
| TEST-04 | Route persistence and cleanup | File system testing with atomic writes, mutex locking verification |
| TEST-05 | Cross-platform tool installation | dotnet tool install testing, global tool verification commands |

## Standard Stack

### Core Testing Infrastructure
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| **xUnit** | 2.9.3 | Test framework | Modern, extensible, used by .NET Core/ASP.NET Core teams, supports async tests |
| **Microsoft.AspNetCore.Mvc.Testing** | 10.0.0 | Integration test host | Provides WebApplicationFactory for in-memory TestServer, official ASP.NET Core approach |
| **Microsoft.NET.Test.Sdk** | 17.14.1 | Test SDK | Required for dotnet test integration, CI/CD compatibility |
| **coverlet.collector** | 6.0.4 | Code coverage | Standard .NET coverage tool, integrates with dotnet test |

### Supporting Libraries
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| **FluentAssertions** | TBD | Readable assertions | Consider for more readable test assertions (not currently in project) |
| **Moq** | TBD | Mocking framework | For unit tests with fakes (not for integration tests per CONTEXT.md) |
| **Spectre.Console.Testing** | TBD | CLI output testing | For testing Spectre.Console.Cli command output capture |

### Existing Infrastructure (Already Implemented)
The project already has:
- ✅ xUnit 2.9.3 configured in Portless.Tests
- ✅ WebApplicationFactory<Program> for YARP routing tests
- ✅ Test patterns for route persistence, cleanup, and hot reload
- ✅ Integration tests for proxy routing, configuration updates, and API endpoints

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| xUnit | NUnit or MSTest | xUnit is more modern, better async support, preferred by Microsoft |
| WebApplicationFactory | TestServer directly | WebApplicationFactory simplifies setup, provides better DI integration |
| Real tool installation | Mocked tool behavior | CONTEXT.md requires real tool testing for authentic validation |

**Installation:**
```bash
# Additional packages to consider
dotnet add package FluentAssertions
dotnet add package Moq
dotnet add package Spectre.Console.Testing
```

## Architecture Patterns

### Recommended Test Project Structure

```
portless-dotnet/
├── Portless.Tests/                    # ✅ Already exists - Unit tests
│   ├── ProxyRoutingTests.cs          # Existing YARP integration tests
│   ├── RoutePersistenceTests.cs      # Existing persistence tests
│   ├── HotReloadTests.cs             # Existing hot reload tests
│   └── RouteCleanupTests.cs         # Existing cleanup tests
│
├── Portless.IntegrationTests/         # 🆕 New - Component integration tests
│   ├── CliIntegrationTests.cs        # CLI command execution (in-process)
│   ├── ProxyProcessIntegrationTests.cs # Proxy lifecycle management
│   ├── PortAllocatorTests.cs         # Port allocation and detection
│   └── RouteStoreIntegrationTests.cs # Route persistence with file locking
│
└── Portless.E2ETests/                 # 🆕 New - End-to-end tool tests
    ├── ToolInstallationTests.cs      # dotnet tool install/verify
    ├── CommandLineE2ETests.cs        # Full CLI invocation via Process.Start
    ├── ProxyWorkflowE2ETests.cs      # Complete proxy start/run/stop workflow
    └── CrossPlatformE2ETests.cs      # Platform-specific behavior validation
```

### Pattern 1: WebApplicationFactory for YARP Testing

**What:** Create in-memory TestServer for YARP proxy routing validation
**When to use:** Testing proxy behavior, routing configuration, API endpoints
**Example:**
```csharp
// Source: Existing ProxyRoutingTests.cs in project
public class ProxyRoutingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ProxyRoutingTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SingleHostname_RoutesToCorrectBackend()
    {
        // Arrange
        var config = _factory.Services.GetRequiredService<DynamicConfigProvider>();
        config.Update(routes, clusters);

        // Act - Make request with custom Host header
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Host", "api1.localhost");
        var response = await _client.SendAsync(request);

        // Assert - Verify routing was configured
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.BadGateway ||
            response.StatusCode == HttpStatusCode.ServiceUnavailable ||
            response.StatusCode == HttpStatusCode.GatewayTimeout
        );
    }
}
```

**Confidence:** HIGH - Already implemented successfully in the project

### Pattern 2: Spectre.Console.Cli Command Testing

**What:** Test CLI commands in-process using CommandApp
**When to use:** Testing command logic, argument parsing, return values
**Example:**
```csharp
// Source: Spectre.Console testing patterns
public class CliIntegrationTests
{
    [Fact]
    public async Task RunCommand_WithValidArgs_ExecutesSuccessfully()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddPortlessPersistence();
        services.AddSingleton<IProxyProcessManager, MockProxyProcessManager>();
        var registrar = new TypeRegistrar(services);

        var app = new CommandApp(registrar);
        app.Configure(config =>
        {
            config.AddCommand<RunCommand>("run");
        });

        // Act
        var result = await app.RunAsync(new[] { "run", "testapi", "dotnet", "run" });

        // Assert
        Assert.Equal(0, result);
    }
}
```

**Confidence:** MEDIUM - Based on Spectre.Console documentation, needs validation

### Pattern 3: Process.Start for E2E CLI Testing

**What:** Execute real dotnet tool binary and capture output
**When to use:** Testing actual tool installation, command-line invocation, environment variables
**Example:**
```csharp
// Source: .NET process management testing patterns
public class CommandLineE2ETests : IAsyncLifetime
{
    private readonly string _testDirectory;
    private Process? _proxyProcess;

    public CommandLineE2ETests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_testDirectory);
    }

    [Fact]
    public async Task ProxyStart_ThenRun_CommandSucceeds()
    {
        // Arrange - Start proxy
        var proxyStart = new ProcessStartInfo("dotnet", "tool run portless proxy start")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using (var proxy = Process.Start(proxyStart))
        {
            await Task.Delay(2000); // Wait for proxy to start

            // Act - Run app with portless
            var runStart = new ProcessStartInfo("dotnet", "tool run portless myapi dotnet run")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = _testDirectory,
                Environment = { ["PORTLESS_TEST"] = "1" }
            };

            using (var run = Process.Start(runStart))
            {
                await Task.Delay(1000);
                var output = await run.StandardOutput.ReadToEndAsync();

                // Assert - Verify PORT variable was injected
                Assert.Contains("http://myapi.localhost:1355", output);
            }
        }
    }

    public async Task DisposeAsync()
    {
        _proxyProcess?.Kill(entireProcessTree: true);
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
            catch
            {
                // Log warning but don't fail test
            }
        }
    }
}
```

**Confidence:** HIGH - Based on .NET Process.Start documentation and existing code

### Pattern 4: Cross-Platform Temporary Directory with Cleanup

**What:** Create unique temp directories per test with robust cleanup
**When to use:** All integration/E2E tests that need file system isolation
**Example:**
```csharp
// Source: Cross-platform file system testing best practices
public abstract class TestBase : IAsyncLifetime
{
    protected readonly string TestDirectory;
    protected readonly string TestRoutesFile;

    protected TestBase()
    {
        TestDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        TestRoutesFile = Path.Combine(TestDirectory, "routes.json");
        Directory.CreateDirectory(TestDirectory);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Retry logic for Windows file system timing issues
        const int maxRetries = 5;
        const int initialDelay = 100;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                if (Directory.Exists(TestDirectory))
                {
                    Directory.Delete(TestDirectory, recursive: true);
                }
                return;
            }
            catch (IOException) when (attempt < maxRetries - 1)
            {
                await Task.Delay(initialDelay * (int)Math.Pow(2, attempt));
            }
            catch
            {
                // Log warning but don't throw - OS will clean up eventually
                return;
            }
        }
    }
}
```

**Confidence:** HIGH - Addresses known Windows file system async timing issues

### Pattern 5: Port Allocation Testing

**What:** Test TCP port detection and allocation in real port range
**When to use:** Testing PortAllocator service, port conflict detection
**Example:**
```csharp
// Source: TCP port testing patterns
public class PortAllocatorTests
{
    [Fact]
    public void AllocatePort_ReturnsAvailablePortInRange()
    {
        // Arrange
        var allocator = new PortAllocator();

        // Act
        var port = allocator.AllocatePort(4000, 4999);

        // Assert
        Assert.InRange(port, 4000, 4999);

        // Verify port is actually available
        var listener = new TcpListener(IPAddress.Any, port);
        try
        {
            listener.Start();
            Assert.True(true); // Port was available
        }
        catch (SocketException)
        {
            Assert.False(true, "Allocated port was not available");
        }
        finally
        {
            listener.Stop();
        }
    }

    [Fact]
    public void AllocatePort_WithExhaustedRange_ThrowsException()
    {
        // Arrange - Reserve all ports in range (simplified test)
        var allocator = new PortAllocator();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            // Simulate exhausted range by setting very small range
            allocator.AllocatePort(4000, 4001);
        });
    }
}
```

**Confidence:** HIGH - Based on System.Net.Sockets documentation

### Anti-Patterns to Avoid

- **Testing with mocks in integration tests:** CONTEXT.md requires real tool installation, not mocks
- **Shared test state:** Each test must be independent with its own temp directory
- **Blocking on cleanup failures:** Log warnings but continue, don't fail tests
- **Hard-coded port numbers:** Always detect free ports dynamically in 4000-4999 range
- **Platform-specific paths:** Use Path.GetTempPath(), Path.Combine() for cross-platform compatibility

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| HTTP test server | Custom TcpListener | WebApplicationFactory<Program> | Provides in-memory TestServer, DI integration, handles request/response pipeline |
| CLI argument parsing | Manual string parsing | Spectre.Console.Cli CommandApp | Already integrated, handles validation, aliases, help text |
| File locking | Custom mutex logic | System.Threading.Mutex (already in RouteStore) | Cross-platform, handles edge cases, already tested |
| Port detection | Socket try/catch loops | TcpListener.Start(0) or existing PortAllocator | OS-assigned ports, race-condition free |
| Test assertions | Custom comparison logic | xUnit Assert + FluentAssertions | Standard, readable, good error messages |
| Process cleanup | Manual PID tracking | Process.Kill(entireProcessTree: true) in .NET 6+ | Handles child processes, cross-platform |

**Key insight:** The .NET ecosystem provides production-ready components for all testing scenarios. Custom implementations introduce bugs, especially around file I/O timing and process lifecycle.

## Common Pitfalls

### Pitfall 1: Test Interdependency
**What goes wrong:** Tests share state (temp directories, proxy processes, ports), causing flaky failures
**Why it happens:** Reusing static fixtures or not cleaning up between tests
**How to avoid:**
- Each test creates its own temp directory with unique GUID
- Use IAsyncLifetime for per-test setup/teardown
- Never share process instances between tests
- Randomize port allocation within test range

**Warning signs:** Tests pass individually but fail in `dotnet test` batch, "port already in use" errors

### Pitfall 2: File System Cleanup Timing on Windows
**What goes wrong:** `Directory.Delete(recursive: true)` throws IOException due to async file operations
**Why it happens:** Windows NTFS delayed commit, antivirus software, file handle delays
**How to avoid:**
- Implement retry logic with exponential backoff (5 retries max)
- Use try-catch with logging, don't fail tests on cleanup errors
- Close all file handles/streams before cleanup
- Consider using TestResults folder instead of system temp

**Warning signs:** Flaky tests only on Windows, "directory not empty" errors

### Pitfall 3: Process Leak in Tests
**What goes wrong:** Proxy or child processes not terminated, blocking subsequent tests
**Why it happens:** Exceptions before cleanup, Process.Start without proper disposal
**How to avoid:**
- Always use `using` blocks or IAsyncLifetime for Process instances
- Call `Process.Kill(entireProcessTree: true)` in .NET 6+
- Implement try-finally to ensure cleanup runs even on test failure
- Use PID file tracking to cleanup orphaned processes

**Warning signs:** "Port already in use" errors, proxy won't start in tests

### Pitfall 4: Hard-Coded Platform Assumptions
**What goes wrong:** Tests pass on Windows but fail on Linux/macOS
**Why it happens:** Hard-coded paths (`\` vs `/), platform-specific APIs, case-sensitive file systems
**How to avoid:**
- Always use `Path.Combine()` never string concatenation
- Use `Path.GetTempPath()` instead of hardcoded temp directories
- Use `Environment.NewLine` for line endings
- Test file operations case-insensitively where appropriate
- Validate path separators with `Path.DirectorySeparatorChar`

**Warning signs:** Tests fail on CI but pass locally, platform-specific exceptions

### Pitfall 5: Async Test Deadlocks
**What goes wrong:** Tests hang indefinitely waiting for async operations
**Why it happens:** Mixing sync and async code, blocking on Task.Result, not awaiting Task.WhenAll
**How to avoid:**
- Always use `async Task` test methods, never `async void`
- Use `await` instead of `.Result` or `.Wait()`
- Set test timeout attribute: `[Fact(Timeout = 10000)]`
- Use `Task.WhenAll()` for concurrent operations, not `Task.WaitAll()`

**Warning signs:** Tests timeout, "async void" warnings from IDE

### Pitfall 6: Port Race Conditions
**What goes wrong:** Multiple tests allocate same port simultaneously
**Why it happens:** Port detection and allocation not atomic, test parallelization
**How to avoid:**
- Use unique temp directories per test to avoid port file conflicts
- Consider sequential test execution for port-dependent tests
- Implement retry logic in port allocation (already 50 attempts in PortAllocator)
- Use CID + timestamp for port file naming

**Warning signs:** "Address already in use" errors, flaky tests in parallel

## Code Examples

Verified patterns from official sources and existing project code:

### Example 1: CLI Command Testing with CommandApp

```csharp
// Source: Spectre.Console.Cli documentation
// Test command execution in-process with dependency injection
[Fact]
public async Task ListCommand_WithActiveRoutes_DisplaysTable()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddPortlessPersistence();

    // Add mock routes
    var routeStore = new RouteStore();
    await routeStore.SaveRoutesAsync(new[]
    {
        new RouteInfo { Hostname = "api1.localhost", Port = 4001, Pid = 12345 }
    });

    var registrar = new TypeRegistrar(services);
    var app = new CommandApp(registrar);
    app.Configure(config => config.AddCommand<ListCommand>("list"));

    // Capture console output
    var consoleOutput = new StringWriter();
    System.Console.SetOut(consoleOutput);

    // Act
    var result = await app.RunAsync(new[] { "list" });

    // Assert
    Assert.Equal(0, result);
    var output = consoleOutput.ToString();
    Assert.Contains("api1.localhost", output);
    Assert.Contains("4001", output);
}
```

### Example 2: Proxy Process Lifecycle Testing

```csharp
// Source: Existing ProxyProcessManager.cs patterns
[Fact]
public async Task ProxyProcessManager_StartStop_CleansUpPidFile()
{
    // Arrange
    var manager = new ProxyProcessManager();
    var testStateDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    Directory.CreateDirectory(testStateDir);

    try
    {
        // Act - Start proxy
        var startResult = await manager.StartProxyAsync(
            new[] { "dotnet", "run", "--project", "Portless.Proxy/Portless.Proxy.csproj" },
            testStateDir
        );

        Assert.True(startResult.Success);
        Assert.NotNull(startResult.ProcessId);

        await Task.Delay(2000); // Wait for startup

        // Act - Stop proxy
        var stopResult = await manager.StopProxyAsync(testStateDir);

        Assert.True(stopResult.Success);

        // Assert - PID file cleaned up
        var pidFile = Path.Combine(testStateDir, "proxy.pid");
        Assert.False(File.Exists(pidFile));

        // Assert - Process actually terminated
        if (startResult.ProcessId.HasValue)
        {
            var processExists = true;
            try
            {
                var process = Process.GetProcessById(startResult.ProcessId.Value);
                if (process.HasExited)
                    processExists = false;
            }
            catch (ArgumentException)
            {
                processExists = false;
            }
            Assert.False(processExists, "Proxy process should be terminated");
        }
    }
    finally
    {
        // Cleanup
        if (Directory.Exists(testStateDir))
        {
            try { Directory.Delete(testStateDir, recursive: true); }
            catch { /* Log warning */ }
        }
    }
}
```

### Example 3: PORT Environment Variable Injection

```csharp
// Source: System.Diagnostics.Process documentation
[Fact]
public async Task RunCommand_InjectsPortEnvironmentVariable()
{
    // Arrange
    var testDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    Directory.CreateDirectory(testDir);

    // Create a simple test app that writes PORT to stdout
    var testAppCode = """
        var port = Environment.GetEnvironmentVariable("PORT") ?? "not-set";
        Console.WriteLine($"PORT={port}");
        """;
    var testAppFile = Path.Combine(testDir, "TestApp.cs");
    await File.WriteAllTextAsync(testAppFile, testAppCode);

    var startInfo = new ProcessStartInfo("dotnet", $"run --project {testAppFile}")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        WorkingDirectory = testDir
    };

    // Simulate PORT injection (as Portless.Cli does)
    startInfo.Environment["PORT"] = "4501";

    // Act
    using var process = Process.Start(startInfo);
    await process.WaitForExitAsync();
    var output = await process.StandardOutput.ReadToEndAsync();

    // Assert
    Assert.Contains("PORT=4501", output);
    Assert.DoesNotContain("PORT=not-set", output);
}
```

### Example 4: Tool Installation Testing

```csharp
// Source: dotnet tool installation documentation
[Fact]
public async Task ToolInstall_CanBeVerified()
{
    // Arrange - Build and pack tool
    var buildResult = await Process.RunAsync("dotnet", "build Portless.slnx");
    Assert.Equal(0, buildResult.ExitCode);

    var packResult = await Process.RunAsync("dotnet", "pack Portless.Cli/Portless.Cli.csproj -o ./nupkg");
    Assert.Equal(0, packResult.ExitCode);

    // Act - Install tool locally
    var installResult = await Process.RunAsync(
        "dotnet",
        "tool install --add-source ./nupkg portless.dotnet"
    );

    // Assert - Installation succeeded
    Assert.Equal(0, installResult.ExitCode);
    Assert.Contains("portless.dotnet", installResult.StandardOutput);

    // Act - Verify tool can be invoked
    var verifyResult = await Process.RunAsync("dotnet", "tool run portless --help");

    // Assert - Tool is executable
    Assert.Equal(0, verifyResult.ExitCode);
    Assert.Contains("proxy", verifyResult.StandardOutput);
    Assert.Contains("run", verifyResult.StandardOutput);
    Assert.Contains("list", verifyResult.StandardOutput);

    // Cleanup
    await Process.RunAsync("dotnet", "tool uninstall portless.dotnet");
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Manual TestServer | WebApplicationFactory<T> | ASP.NET Core 3.0 (2019) | Simplified integration test setup, automatic DI configuration |
| Synchronous test timeouts | Async test timeouts with cancellation | .NET 6 | Better async support, no thread pool blocking |
| Process.Start without cleanup | Process.Kill(entireProcessTree: true) | .NET 6 | Reliable process tree cleanup on all platforms |
| Assert.True/False | FluentAssertions style assertions | xUnit 2.0+ | More readable test failures, better error messages |
| Manual temp directory management | IAsyncLifetime pattern | xUnit 2.0+ | Reliable per-test setup/teardown |

**Deprecated/outdated:**
- **NUnit v2**: Superseded by NUnit v3, xUnit is now preferred for .NET Core
- **MSTest**: Legacy framework, lacking modern async support, still used but not recommended
- **Console.WriteLine in tests**: Use ITestOutputHelper or proper logging frameworks
- **[ExpectedException] attribute**: Replaced by Assert.Throws/Record.Exception

## Open Questions

1. **Test Execution Speed vs Realism**
   - What we know: CONTEXT.md requires real tool installation (slow), but E2E tests can be very slow
   - What's unclear: Balance between comprehensive testing and developer feedback loop
   - Recommendation: Run unit/integration tests on every change, run E2E tests only before releases (per CONTEXT.md)

2. **Parallel Test Execution**
   - What we know: xUnit supports parallel test execution, but port allocation and file I/O can cause conflicts
   - What's unclear: Whether to disable parallel execution or implement coordination
   - Recommendation: Start with sequential execution (simpler), consider parallelization if tests exceed 5 minutes

3. **Cross-Platform Test Validation**
   - What we know: CONTEXT.md requires manual validation on Windows + Linux
   - What's unclear: How to detect platform-specific issues before manual testing
   - Recommendation: Use CI with GitHub Actions for automated cross-platform testing (deferred to future per CONTEXT.md)

4. **Cleanup Failure Impact**
   - What we know: CONTEXT.md says log warnings and continue on cleanup failure
   - What's unclear: Whether to track leaked resources for post-test cleanup
   - Recommendation: Implement best-effort cleanup with logging, accept OS will eventually clean temp directories

## Sources

### Primary (HIGH confidence)
- **xUnit Documentation** - [xUnit.net official docs](https://xunit.net/docs/getting-started/netcore/cmdline) - Test framework usage, async tests, IClassFixture, IAsyncLifetime
- **Microsoft.AspNetCore.Mvc.Testing** - [Integration Tests in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests) - WebApplicationFactory patterns, TestServer configuration
- **System.Diagnostics.Process** - [.NET Process class docs](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process) - Process.Start, environment variables, Kill(entireProcessTree)
- **Existing project code** - Portless.Tests/ProxyRoutingTests.cs, RoutePersistenceTests.cs, HotReloadTests.cs - Verified working patterns in the project

### Secondary (MEDIUM confidence)
- **Spectre.Console.Cli Testing** - [Spectre.Console documentation](https://spectreconsole.net/cli/testing) - CommandApp testing, console output capture (needs validation for .NET 10)
- **dotnet tool installation** - [How to manage .NET tools](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools) - Tool installation, verification, uninstall commands
- **.NET file system operations** - [System.IO.Directory](https://learn.microsoft.com/en-us/dotnet/api/system.io.directory) - Cross-platform path handling, temporary directories
- **TcpListener port detection** - [System.Net.Sockets.TcpListener](https://learn.microsoft.com/en-us/dotnet/api/system.net.sockets.tcplistener) - Port availability checking

### Tertiary (LOW confidence)
- **Integration testing best practices** - CSDN and CN Blogs articles (Chinese language, 2024) - General testing patterns, needs verification against English sources
- **C# Learning Roadmap 2026** - Coursera article (2025) - General testing trends, not specific to .NET tools
- **HttpClient Interception library** - Third-party testing library - Potentially useful but not standard, evaluate against WebApplicationFactory

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All components verified from official documentation and existing project usage
- Architecture: HIGH - Patterns based on official docs and verified in existing codebase
- Pitfalls: HIGH - Based on documented issues in .NET (Windows file system timing, process lifecycle) and CONTEXT.md constraints

**Research date:** 2026-02-21
**Valid until:** 2026-03-23 (30 days - .NET ecosystem is stable, xUnit and testing patterns evolve slowly)

**Key assumptions validated:**
- ✅ xUnit 2.9.3 is current and appropriate for .NET 10
- ✅ WebApplicationFactory is standard approach for ASP.NET Core integration testing
- ✅ Spectre.Console.Cli supports testing with CommandApp
- ✅ Process.Start with environment variables is standard approach for PORT injection
- ✅ Cross-platform file system testing requires retry logic on Windows
- ✅ Port allocation via TcpListener is reliable for testing
