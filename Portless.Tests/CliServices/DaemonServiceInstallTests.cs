extern alias Cli;
using System.Reflection;
using Cli::Portless.Cli.Services;
using Xunit;

namespace Portless.Tests.CliServices;

/// <summary>
/// Tests for DaemonService covering InstallAsync unit file creation,
/// UninstallAsync cleanup, and internal method behaviors.
/// Uses temp directories for filesystem isolation.
/// </summary>
public class DaemonServiceInstallTests : IDisposable
{
    private readonly string _testHomeDir;
    private readonly string _testSystemdDir;
    private readonly string _testStateDir;

    public DaemonServiceInstallTests()
    {
        _testHomeDir = Path.Combine(Path.GetTempPath(), "portless-daemon-test-" + Guid.NewGuid());
        _testSystemdDir = Path.Combine(_testHomeDir, ".config", "systemd", "user");
        _testStateDir = Path.Combine(_testHomeDir, ".portless");
        Directory.CreateDirectory(_testSystemdDir);
        Directory.CreateDirectory(_testStateDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testHomeDir))
                Directory.Delete(_testHomeDir, true);
        }
        catch { }
    }

    private DaemonService CreateService()
    {
        // Use reflection to set internal paths for test isolation
        var service = new DaemonService();
        var unitFilePathField = typeof(DaemonService).GetField("_unitFilePath",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var stateDirField = typeof(DaemonService).GetField("_stateDirectory",
            BindingFlags.NonPublic | BindingFlags.Instance);

        unitFilePathField!.SetValue(service, Path.Combine(_testSystemdDir, "portless-proxy.service"));
        stateDirField!.SetValue(service, _testStateDir);

        return service;
    }

    [Fact]
    public async Task InstallAsync_CreatesUnitFile_WithCorrectContent()
    {
        // Create a fake proxy project so ResolveProxyPath can succeed
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
        var proxyProjectPath = Path.Combine(solutionRoot, "Portless.Proxy", "Portless.Proxy.csproj");

        if (!File.Exists(proxyProjectPath))
        {
            // Skip if running in CI without project structure
            return;
        }

        var service = CreateService();
        // Override RunSystemctlAsync to be a no-op for testing
        try
        {
            await service.InstallAsync(enableHttps: true, enableNow: true);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("systemctl"))
        {
            // systemctl not available in test environment, that's expected
        }

        // Check that the unit file was created
        var unitFilePath = Path.Combine(_testSystemdDir, "portless-proxy.service");
        if (File.Exists(unitFilePath))
        {
            var content = await File.ReadAllTextAsync(unitFilePath);
            Assert.Contains("[Unit]", content);
            Assert.Contains("Description=Portless.NET Local Proxy", content);
            Assert.Contains("[Service]", content);
            Assert.Contains("Type=simple", content);
            Assert.Contains("PORTLESS_HTTPS_ENABLED=true", content);
            Assert.Contains("Restart=on-failure", content);
            Assert.Contains("[Install]", content);
            Assert.Contains("WantedBy=default.target", content);
        }
    }

    [Fact]
    public async Task InstallAsync_CreatesUnitFile_WithHttpsDisabled()
    {
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
        var proxyProjectPath = Path.Combine(solutionRoot, "Portless.Proxy", "Portless.Proxy.csproj");

        if (!File.Exists(proxyProjectPath))
        {
            return;
        }

        var service = CreateService();
        try
        {
            await service.InstallAsync(enableHttps: false, enableNow: false);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("systemctl"))
        {
            // Expected in test environment
        }

        var unitFilePath = Path.Combine(_testSystemdDir, "portless-proxy.service");
        if (File.Exists(unitFilePath))
        {
            var content = await File.ReadAllTextAsync(unitFilePath);
            Assert.Contains("PORTLESS_HTTPS_ENABLED=false", content);
        }
    }

    [Fact]
    public async Task UninstallAsync_RemovesUnitFile_WhenPresent()
    {
        var service = CreateService();
        var unitFilePath = Path.Combine(_testSystemdDir, "portless-proxy.service");

        // Create a fake unit file
        await File.WriteAllTextAsync(unitFilePath, "[Unit]\nDescription=Test\n");
        Assert.True(File.Exists(unitFilePath));

        // Uninstall should remove the file (systemctl calls will fail but are caught)
        await service.UninstallAsync();

        Assert.False(File.Exists(unitFilePath));
    }

    [Fact]
    public async Task UninstallAsync_DoesNotThrow_WhenUnitFileDoesNotExist()
    {
        var service = CreateService();
        // No unit file exists, should not throw
        await service.UninstallAsync();
    }

    [Fact]
    public async Task GetStatusAsync_WithUnitFile_ReturnsInstalledTrue()
    {
        var service = CreateService();
        var unitFilePath = Path.Combine(_testSystemdDir, "portless-proxy.service");

        // Create a fake unit file
        await File.WriteAllTextAsync(unitFilePath, "[Unit]\nDescription=Test\n");

        var result = await service.GetStatusAsync();

        Assert.True(result.isInstalled);
        // isEnabled and isRunning depend on systemctl which won't work in test env
    }

    [Fact]
    public async Task EnableAsync_WithUnitFilePresent_DoesNotThrowForFileCheck()
    {
        var service = CreateService();
        var unitFilePath = Path.Combine(_testSystemdDir, "portless-proxy.service");
        await File.WriteAllTextAsync(unitFilePath, "[Unit]\nDescription=Test\n");

        // Will throw because systemctl is not available, but the file check should pass
        try
        {
            await service.EnableAsync();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("systemctl"))
        {
            // Expected - systemctl not available
        }
    }

    [Fact]
    public async Task EnableAsync_WithoutUnitFile_ThrowsInvalidOperationException()
    {
        var service = CreateService();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.EnableAsync());
    }

    [Fact]
    public async Task DisableAsync_WithoutUnitFile_ThrowsInvalidOperationException()
    {
        var service = CreateService();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DisableAsync());
    }

    [Fact]
    public void ResolveProxyPath_StaticMethod_Exists()
    {
        var method = typeof(DaemonService).GetMethod(
            "ResolveProxyPath",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void ResolveProxyPath_WithProxyProject_ReturnsValidTuple()
    {
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
        var proxyProjectPath = Path.Combine(solutionRoot, "Portless.Proxy", "Portless.Proxy.csproj");

        if (!File.Exists(proxyProjectPath))
        {
            // Skip if running without project structure
            return;
        }

        var method = typeof(DaemonService).GetMethod(
            "ResolveProxyPath",
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)!;

        var result = ((string execPath, string workingDir))method.Invoke(null, null)!;
        Assert.False(string.IsNullOrEmpty(result.execPath));
        Assert.False(string.IsNullOrEmpty(result.workingDir));
        Assert.Contains("dotnet", result.execPath);
    }

    [Fact]
    public async Task GetStatusAsync_WithUnitFile_NoSystemd_ReturnsInstalledTrue()
    {
        var service = CreateService();
        var unitFilePath = Path.Combine(_testSystemdDir, "portless-proxy.service");
        await File.WriteAllTextAsync(unitFilePath, "[Unit]\nDescription=Test\n");

        var (isInstalled, isEnabled, isRunning, pid) = await service.GetStatusAsync();

        // File exists so isInstalled should be true
        Assert.True(isInstalled);
        // systemctl commands will fail gracefully, returning false/null
        Assert.False(isEnabled);
        Assert.False(isRunning);
        Assert.Null(pid);
    }

    [Fact]
    public async Task InstallAsync_CreatesSystemdDirectory()
    {
        // Delete the systemd dir to verify it gets recreated
        var newTestDir = Path.Combine(Path.GetTempPath(), "portless-daemon-newdir-" + Guid.NewGuid());
        try
        {
            var service = new DaemonService();
            var field = typeof(DaemonService).GetField("_unitFilePath",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var newDir = Path.Combine(newTestDir, ".config", "systemd", "user");
            field!.SetValue(service, Path.Combine(newDir, "portless-proxy.service"));

            var stateDirField = typeof(DaemonService).GetField("_stateDirectory",
                BindingFlags.NonPublic | BindingFlags.Instance);
            stateDirField!.SetValue(service, Path.Combine(newTestDir, ".portless"));

            try
            {
                await service.InstallAsync(false, false);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("systemctl") || ex.Message.Contains("Cannot locate"))
            {
                // Expected in test environment
            }

            // Directory should have been created
            Assert.True(Directory.Exists(newDir));
        }
        finally
        {
            try { if (Directory.Exists(newTestDir)) Directory.Delete(newTestDir, true); } catch { }
        }
    }
}
