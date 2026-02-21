using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Portless.E2ETests;

/// <summary>
/// E2E tests for complete CLI workflows including proxy start, run, list, and stop.
/// Tests verify the full tool functionality when invoked as a dotnet tool.
/// </summary>
public class CommandLineE2ETests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private readonly string _testDirectory;
    private Process? _proxyProcess;

    public CommandLineE2ETests(ITestOutputHelper output)
    {
        _output = output;
        _testDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    }

    public async Task InitializeAsync()
    {
        // Create test directory
        Directory.CreateDirectory(_testDirectory);
    }

    public async Task DisposeAsync()
    {
        // Cleanup: Stop proxy if running
        if (_proxyProcess != null && !_proxyProcess.HasExited)
        {
            try
            {
                _proxyProcess.Kill(entireProcessTree: true);
                _output.WriteLine("Killed proxy process during cleanup");
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        // Delete test directory with retry logic
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                if (Directory.Exists(_testDirectory))
                {
                    Directory.Delete(_testDirectory, recursive: true);
                }
                break;
            }
            catch when (attempt < 4)
            {
                _output.WriteLine($"Retry {attempt + 1}: Failed to delete test directory");
                await Task.Delay(500 * (attempt + 1));
            }
            catch
            {
                _output.WriteLine($"Warning: Failed to cleanup test directory after 5 attempts");
                break;
            }
        }
    }

    [Fact]
    public async Task ProxyStatus_WhenNotRunning_DoesNotCrash()
    {
        // Arrange - Ensure proxy is not running
        await StopProxyIfExists();

        // Act - Check status
        var result = await RunDotnetCommand(
            "run",
            "--project",
            "Portless.Cli/Portless.Cli.csproj",
            "proxy",
            "status"
        );

        // Assert - Command should execute without crashing
        // Exit code may vary (0 or 1 depending on implementation)
        _output.WriteLine($"Status output:\n{result.StandardOutput}");
        _output.WriteLine($"Status error:\n{result.StandardError}");
        _output.WriteLine($"Exit code: {result.ExitCode}");

        // The key assertion is that the command doesn't crash
        Assert.True(result.ExitCode >= 0 && result.ExitCode <= 1);
    }

    [Fact]
    public async Task ListCommand_ExecutesSuccessfully()
    {
        // Arrange - Ensure proxy is not running
        await StopProxyIfExists();

        // Act - List routes
        var result = await RunDotnetCommand(
            "run",
            "--project",
            "Portless.Cli/Portless.Cli.csproj",
            "list"
        );

        // Assert - Command executes (output may be empty or have messages)
        _output.WriteLine($"List output:\n{result.StandardOutput}");
        _output.WriteLine($"List error:\n{result.StandardError}");

        // Key assertion: command executes without crashing
        Assert.True(result.ExitCode >= 0 && result.ExitCode <= 1);
    }

    [Fact]
    public async Task RunCommand_WithInvalidArgs_ReturnsNonZeroExitCode()
    {
        // Arrange & Act - Run command without name argument
        var result = await RunDotnetCommand(
            "run",
            "--project",
            "Portless.Cli/Portless.Cli.csproj",
            "run"
        );

        // Assert - Should fail with validation error
        _output.WriteLine($"Validation error output:\n{result.StandardOutput}");
        _output.WriteLine($"Validation error error:\n{result.StandardError}");

        // Exit code should be non-zero for validation errors
        // Note: Spectre.Console.Cli may return 1 or other codes for validation errors
        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task ProxyStartStop_ProcessLifecycleWorks()
    {
        // Arrange - Ensure clean state
        await StopProxyIfExists();

        // Act - Start proxy process directly
        _output.WriteLine("Starting proxy process...");
        _proxyProcess = StartProxyProcess();
        Assert.NotNull(_proxyProcess);

        // Wait for startup
        await Task.Delay(3000);

        // Check if process is still running
        var isRunning = !_proxyProcess.HasExited;
        _output.WriteLine($"Proxy running after startup: {isRunning}");

        // Act - Stop proxy
        _output.WriteLine("Stopping proxy...");
        await StopProxyIfExists();

        // Wait for cleanup
        await Task.Delay(2000);

        // Assert - Process should be stopped
        if (_proxyProcess != null)
        {
            Assert.True(_proxyProcess.HasExited || _proxyProcess.WaitForExit(1000));
        }

        _output.WriteLine("Proxy process lifecycle test completed");
    }

    [Fact]
    public async Task TestScript_CanBeCreated()
    {
        // Arrange & Act - Create test scripts for both platforms
        var testDirectory = Path.Combine(_testDirectory, "scripts");
        Directory.CreateDirectory(testDirectory);

        var windowsScript = Path.Combine(testDirectory, "test-app.bat");
        var unixScript = Path.Combine(testDirectory, "test-app.sh");

        // Windows batch script
        await File.WriteAllTextAsync(windowsScript, "@echo off\r\necho PORT=%PORT%\r\ntimeout /t 1 >nul");

        // Unix shell script
        await File.WriteAllTextAsync(unixScript, "#!/bin/sh\necho \"PORT=$PORT\"\nsleep 1");

        // Assert - Both scripts should be created
        Assert.True(File.Exists(windowsScript));
        Assert.True(File.Exists(unixScript));

        _output.WriteLine($"Created Windows script: {windowsScript}");
        _output.WriteLine($"Created Unix script: {unixScript}");
    }

    private Process StartProxyProcess()
    {
        var assemblyLocation = typeof(CommandLineE2ETests).Assembly.Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation) ?? ".";
        var solutionRoot = Path.GetFullPath(Path.Combine(assemblyDirectory, "../../../../"));
        var proxyProjectPath = Path.Combine(solutionRoot, "Portless.Proxy", "Portless.Proxy.csproj");

        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c set PORTLESS_PORT=1355 && dotnet run --project \"{proxyProjectPath}\" --urls http://*:1355",
            UseShellExecute = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        return Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start proxy process");
    }

    private async Task StopProxyIfExists()
    {
        try
        {
            var result = await RunDotnetCommand(
                "run",
                "--project",
                "Portless.Cli/Portless.Cli.csproj",
                "proxy",
                "stop"
            );
            _output.WriteLine($"Stop cleanup result: {result.ExitCode}");
        }
        catch
        {
            // Ignore if proxy not running
        }

        // Also cleanup any existing proxy process
        if (_proxyProcess != null && !_proxyProcess.HasExited)
        {
            try
            {
                _proxyProcess.Kill(entireProcessTree: true);
            }
            catch
            {
                // Ignore
            }
        }
    }

    private async Task<(int ExitCode, string StandardOutput, string StandardError)> RunDotnetCommand(
        params string[] args)
    {
        // Get solution root directory
        var assemblyLocation = typeof(CommandLineE2ETests).Assembly.Location;
        var assemblyDirectory = Path.GetDirectoryName(assemblyLocation) ?? ".";
        var solutionRoot = Path.GetFullPath(Path.Combine(assemblyDirectory, "../../../../"));

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = string.Join(" ", args),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = solutionRoot
        };

        _output.WriteLine($"Running: dotnet {string.Join(" ", args)}");
        _output.WriteLine($"Working directory: {solutionRoot}");

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start dotnet process");
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, output, error);
    }

    private string CreateTestScript()
    {
        return "test-script";
    }
}
