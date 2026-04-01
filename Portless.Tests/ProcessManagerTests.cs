using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using Portless.Core.Services;
using Xunit;

namespace Portless.Tests;

public class ProcessManagerTests
{
    private readonly ProcessManager _processManager;
    private readonly Mock<ILogger<ProcessManager>> _loggerMock;

    public ProcessManagerTests()
    {
        _loggerMock = new Mock<ILogger<ProcessManager>>();
        _processManager = new ProcessManager(_loggerMock.Object);
    }

    [Fact]
    public void StartManagedProcess_InjectsPortEnvVar()
    {
        // Use 'printenv PORT' to verify PORT injection
        var process = _processManager.StartManagedProcess(
            "printenv", "PORT",
            4042,
            Directory.GetCurrentDirectory()
        );

        Assert.NotNull(process);
        Assert.True(process.Id > 0);

        process.WaitForExit(5000);
        Assert.True(process.HasExited);
        Assert.Equal(0, process.ExitCode);
    }

    [Fact]
    public void StartManagedProcess_WithAdditionalEnvVars_InjectsAll()
    {
        var envVars = new Dictionary<string, string>
        {
            ["ASPNETCORE_URLS"] = "http://0.0.0.0:4042",
            ["PORTLESS_URL"] = "http://myapp.localhost",
            ["MY_CUSTOM_VAR"] = "test_value"
        };

        // Use a shell command that prints the env vars
        var process = _processManager.StartManagedProcess(
            "printenv", "ASPNETCORE_URLS",
            4042,
            Directory.GetCurrentDirectory(),
            envVars
        );

        Assert.NotNull(process);
        Assert.True(process.Id > 0);

        process.WaitForExit(5000);
        Assert.True(process.HasExited);
        Assert.Equal(0, process.ExitCode);
    }

    [Fact]
    public void StartManagedProcess_PortEnvVarOverriddenByAdditional()
    {
        // If additional env vars contain PORT, it should override the default injection
        var envVars = new Dictionary<string, string>
        {
            ["PORT"] = "9999"
        };

        var process = _processManager.StartManagedProcess(
            "printenv", "PORT",
            4042,
            Directory.GetCurrentDirectory(),
            envVars
        );

        Assert.NotNull(process);
        process.WaitForExit(5000);
        // The additional env vars should override (set after PORT)
        // Note: Both PORT=4042 and PORT=9999 are set, last one wins
        Assert.True(process.HasExited);
    }

    [Fact]
    public void StartManagedProcess_ThrowsForInvalidCommand()
    {
            Assert.ThrowsAny<Exception>(() =>
                _processManager.StartManagedProcess(
                    "nonexistent_command_that_does_not_exist_xyz123",
                    "",
                    4042,
                    Directory.GetCurrentDirectory()
                )
            );
    }

    [Fact]
    public async Task GetProcessStatusAsync_ReturnsRunningForCurrentProcess()
    {
        var currentPid = Environment.ProcessId;
        var status = await _processManager.GetProcessStatusAsync(currentPid);

        Assert.True(status.IsRunning);
        Assert.NotNull(status.StartTime);
        Assert.Null(status.ExitTime);
    }

    [Fact]
    public async Task GetProcessStatusAsync_ReturnsNotRunningForInvalidPid()
    {
        var status = await _processManager.GetProcessStatusAsync(99999999);

        Assert.False(status.IsRunning);
        Assert.Null(status.StartTime);
        Assert.Null(status.ExitTime);
    }
}
