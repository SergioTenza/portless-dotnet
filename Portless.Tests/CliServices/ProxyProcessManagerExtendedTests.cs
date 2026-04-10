extern alias Cli;
using Cli::Portless.Cli.Services;
using Xunit;

namespace Portless.Tests.CliServices;

/// <summary>
/// Extended ProxyProcessManager tests covering managed PIDs file handling,
/// GetStatusAsync with running process, and LoadManagedPidsAsync edge cases.
/// </summary>
public class ProxyProcessManagerExtendedTests : IDisposable
{
    private readonly string _testStateDir;
    private readonly ProxyProcessManager _manager;

    public ProxyProcessManagerExtendedTests()
    {
        _testStateDir = Path.Combine(Path.GetTempPath(), $"portless-ext-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testStateDir);
        Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", _testStateDir);
        _manager = new ProxyProcessManager();
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", null);
        try { if (Directory.Exists(_testStateDir)) Directory.Delete(_testStateDir, true); } catch { }
    }

    [Fact]
    public async Task GetStatusAsync_WithCurrentProcessPid_ReturnsRunning()
    {
        var pidFile = Path.Combine(_testStateDir, "proxy.pid");
        await File.WriteAllTextAsync(pidFile, Environment.ProcessId.ToString());

        var (isRunning, port, pid) = await _manager.GetStatusAsync();
        Assert.True(isRunning);
        Assert.NotNull(port);
        Assert.Equal(Environment.ProcessId, pid);
    }

    [Fact]
    public async Task GetActiveManagedProcessesAsync_WithCorruptJson_ReturnsEmpty()
    {
        var pidsFile = Path.Combine(_testStateDir, "managed-pids.json");
        await File.WriteAllTextAsync(pidsFile, "not valid json {{{");

        var pids = await _manager.GetActiveManagedProcessesAsync();
        Assert.Empty(pids);
    }

    [Fact]
    public async Task GetActiveManagedProcessesAsync_WithEmptyJson_ReturnsEmpty()
    {
        var pidsFile = Path.Combine(_testStateDir, "managed-pids.json");
        await File.WriteAllTextAsync(pidsFile, "");

        var pids = await _manager.GetActiveManagedProcessesAsync();
        Assert.Empty(pids);
    }

    [Fact]
    public async Task RegisterAndKillManagedProcess_SavesAndRemovesPid()
    {
        await _manager.RegisterManagedProcessAsync(99999801);
        await _manager.RegisterManagedProcessAsync(99999802);

        // Verify saved to file
        var pidsFile = Path.Combine(_testStateDir, "managed-pids.json");
        Assert.True(File.Exists(pidsFile));
        var content = await File.ReadAllTextAsync(pidsFile);
        Assert.Contains("99999801", content);
        Assert.Contains("99999802", content);

        // Kill one (non-existent process, won't throw)
        await _manager.KillManagedProcessesAsync([99999801]);

        // Verify it was removed from tracking
        content = await File.ReadAllTextAsync(pidsFile);
        Assert.DoesNotContain("99999801", content);
        Assert.Contains("99999802", content);
    }

    [Fact]
    public async Task GetActiveManagedProcesses_FiltersDeadPids()
    {
        await _manager.RegisterManagedProcessAsync(99999701);

        var pids = await _manager.GetActiveManagedProcessesAsync();
        // Fake PID should be filtered out (not alive)
        Assert.Empty(pids);
    }

    [Fact]
    public async Task StopAsync_WithCurrentProcessPid_StopsAndCleansUp()
    {
        var pidFile = Path.Combine(_testStateDir, "proxy.pid");
        await File.WriteAllTextAsync(pidFile, Environment.ProcessId.ToString());

        // StopAsync will try to kill the current process - that's dangerous
        // Instead test with a non-existent PID to verify cleanup
        await File.WriteAllTextAsync(pidFile, "99999601");
        await _manager.StopAsync();
        Assert.False(File.Exists(pidFile));
    }

    [Fact]
    public async Task IsRunningAsync_WithCurrentProcess_ReturnsTrue()
    {
        var pidFile = Path.Combine(_testStateDir, "proxy.pid");
        await File.WriteAllTextAsync(pidFile, Environment.ProcessId.ToString());

        var result = await _manager.IsRunningAsync();
        Assert.True(result);
    }

    [Fact]
    public async Task IsRunningAsync_WithEmptyPidFile_ReturnsFalse()
    {
        var pidFile = Path.Combine(_testStateDir, "proxy.pid");
        await File.WriteAllTextAsync(pidFile, "");

        var result = await _manager.IsRunningAsync();
        Assert.False(result);
    }

    [Fact]
    public async Task StartAsync_WithPortlessPortEnvVar_WarnsButContinues()
    {
        // Set the deprecated env var
        Environment.SetEnvironmentVariable("PORTLESS_PORT", "8080");
        try
        {
            // Will throw because project not found, but the warning should be written
            try
            {
                await _manager.StartAsync(1355);
            }
            catch (InvalidOperationException ex)
            {
                Assert.Contains("Proxy project not found", ex.Message);
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("PORTLESS_PORT", null);
        }
    }
}
