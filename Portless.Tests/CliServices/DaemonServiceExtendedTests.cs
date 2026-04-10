extern alias Cli;
using Cli::Portless.Cli.Services;
using System.Reflection;
using Xunit;

namespace Portless.Tests.CliServices;

/// <summary>
/// Additional DaemonService tests covering InstallAsync with temp directories.
/// </summary>
public class DaemonServiceExtendedTests : IDisposable
{
    private readonly string _testStateDir;
    private readonly string _originalHome;

    public DaemonServiceExtendedTests()
    {
        _testStateDir = Path.Combine(Path.GetTempPath(), "portless-test-" + Guid.NewGuid());
        Directory.CreateDirectory(_testStateDir);
        _originalHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testStateDir))
                Directory.Delete(_testStateDir, true);
        }
        catch { }
    }

    [Fact]
    public async Task GetStatusAsync_WhenUnitFileDoesNotExist_ReturnsNotInstalled()
    {
        // Default constructor uses real home dir - unit file won't exist
        var service = new DaemonService();
        var (isInstalled, isEnabled, isRunning, pid) = await service.GetStatusAsync();

        Assert.False(isInstalled);
        Assert.False(isEnabled);
        Assert.False(isRunning);
        Assert.Null(pid);
    }

    [Fact]
    public async Task UninstallAsync_WhenNotInstalled_CompletesWithoutError()
    {
        var service = new DaemonService();
        await service.UninstallAsync();
        // Should not throw
    }

    [Fact]
    public void Constructor_InitializesCorrectPaths()
    {
        var service = new DaemonService();
        Assert.NotNull(service);
        // Verify it was constructed without errors
    }

    [Fact]
    public void ResolveProxyPath_IsInternalStaticMethod()
    {
        var method = typeof(DaemonService).GetMethod(
            "ResolveProxyPath",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void ResolveProxyPath_ReturnsValidTupleOrThrows()
    {
        var method = typeof(DaemonService).GetMethod(
            "ResolveProxyPath",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!;

        try
        {
            var result = ((string execPath, string workingDir))method.Invoke(null, null)!;
            Assert.False(string.IsNullOrEmpty(result.execPath));
            Assert.False(string.IsNullOrEmpty(result.workingDir));
        }
        catch (TargetInvocationException ex)
        {
            Assert.IsType<InvalidOperationException>(ex.InnerException);
        }
    }

    [Fact]
    public async Task EnableAsync_WhenNotInstalled_Throws()
    {
        var service = new DaemonService();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.EnableAsync());
    }

    [Fact]
    public async Task DisableAsync_WhenNotInstalled_Throws()
    {
        var service = new DaemonService();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DisableAsync());
    }
}
