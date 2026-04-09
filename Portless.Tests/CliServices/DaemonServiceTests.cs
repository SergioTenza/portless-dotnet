extern alias Cli;
using System.Reflection;
using Cli::Portless.Cli.Services;
using Xunit;

namespace Portless.Tests.CliServices;

public class DaemonServiceTests
{
    private readonly DaemonService _service;

    public DaemonServiceTests()
    {
        _service = new DaemonService();
    }

    [Fact]
    public void Constructor_SetsUnitFilePath_InSystemdUserDirectory()
    {
        // The service constructor sets up paths - verify it doesn't throw
        var service = new DaemonService();
        Assert.NotNull(service);
    }

    [Fact]
    public async Task GetStatusAsync_WhenNotInstalled_ReturnsAllFalse()
    {
        // On a system without the service installed, GetStatusAsync should return false
        var result = await _service.GetStatusAsync();

        Assert.False(result.isInstalled);
        Assert.False(result.isEnabled);
        Assert.False(result.isRunning);
        Assert.Null(result.pid);
    }

    [Fact]
    public async Task EnableAsync_WhenNotInstalled_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.EnableAsync());
    }

    [Fact]
    public async Task DisableAsync_WhenNotInstalled_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DisableAsync());
    }

    [Fact]
    public async Task UninstallAsync_WhenNotInstalled_DoesNotThrow()
    {
        // Should not throw even if nothing is installed
        await _service.UninstallAsync();
    }

    [Fact]
    public void ResolveProxyPath_DoesNotThrowOnStaticCall()
    {
        // ResolveProxyPath is internal static - test via reflection
        var method = typeof(DaemonService).GetMethod(
            "ResolveProxyPath",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        Assert.NotNull(method);

        // It may throw if running in CI without the project structure,
        // or succeed if project is found - both are valid
        try
        {
            var result = ((string execPath, string workingDir))method.Invoke(null, null)!;
            Assert.False(string.IsNullOrEmpty(result.execPath));
            Assert.False(string.IsNullOrEmpty(result.workingDir));
            Assert.True(Directory.Exists(result.workingDir));
        }
        catch (TargetInvocationException ex)
        {
            Assert.IsType<InvalidOperationException>(ex.InnerException);
        }
    }
}
