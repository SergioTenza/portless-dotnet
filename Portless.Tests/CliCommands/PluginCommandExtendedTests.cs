extern alias Cli;
using Cli::Portless.Cli.Commands.PluginCommand;
using Cli::Portless.Cli.Services;
using Moq;
using Spectre.Console.Cli;
using Xunit;

using IProxyHttpClient = Cli::Portless.Cli.Services.IProxyHttpClient;

namespace Portless.Tests.CliCommands;

/// <summary>
/// Additional PluginCommand tests covering Enable/Disable/Uninstall with real directories,
/// install with .disabled file handling, and more.
/// </summary>
[Collection("SpectreConsoleTests")]
public class PluginCommandExtendedTests
{
    private readonly Mock<IProxyHttpClient> _proxyHttpMock;
    private readonly PluginCommand _command;
    private readonly string _testStateDir;

    public PluginCommandExtendedTests()
    {
        _proxyHttpMock = new Mock<IProxyHttpClient>();
        _command = new PluginCommand(_proxyHttpMock.Object);
        _testStateDir = Path.Combine(Path.GetTempPath(), "portless-test-" + Guid.NewGuid());
        Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", _testStateDir);
    }

    // No IDisposable needed - we set env var per test, cleanup at end

    private static CommandContext CreateContext() =>
        new([], new TestRemainingArguments(), "plugin", null);

    [Fact]
    public async Task Enable_ExistingPluginDir_Returns0()
    {
        Directory.CreateDirectory(_testStateDir);
        var pluginsDir = Path.Combine(_testStateDir, "plugins");
        var pluginDir = Path.Combine(pluginsDir, "test-plugin");
        Directory.CreateDirectory(pluginDir);

        // Create .disabled file
        await File.WriteAllTextAsync(Path.Combine(pluginDir, ".disabled"), "");
        _proxyHttpMock.Setup(x => x.NotifyPluginReloadAsync()).Returns(Task.CompletedTask);

        var settings = new PluginSettings { Action = "enable", Target = "test-plugin" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);

        Assert.Equal(0, result);
        Assert.False(File.Exists(Path.Combine(pluginDir, ".disabled")));
        _proxyHttpMock.Verify(x => x.NotifyPluginReloadAsync(), Times.Once);
    }

    [Fact]
    public async Task Enable_ExistingPluginWithoutDisabledFile_Returns0()
    {
        Directory.CreateDirectory(_testStateDir);
        var pluginsDir = Path.Combine(_testStateDir, "plugins");
        var pluginDir = Path.Combine(pluginsDir, "my-plugin");
        Directory.CreateDirectory(pluginDir);

        _proxyHttpMock.Setup(x => x.NotifyPluginReloadAsync()).Returns(Task.CompletedTask);

        var settings = new PluginSettings { Action = "enable", Target = "my-plugin" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Disable_ExistingPluginDir_Returns0()
    {
        Directory.CreateDirectory(_testStateDir);
        var pluginsDir = Path.Combine(_testStateDir, "plugins");
        var pluginDir = Path.Combine(pluginsDir, "test-plugin");
        Directory.CreateDirectory(pluginDir);

        _proxyHttpMock.Setup(x => x.NotifyPluginReloadAsync()).Returns(Task.CompletedTask);

        var settings = new PluginSettings { Action = "disable", Target = "test-plugin" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);

        Assert.Equal(0, result);
        Assert.True(File.Exists(Path.Combine(pluginDir, ".disabled")));
        _proxyHttpMock.Verify(x => x.NotifyPluginReloadAsync(), Times.Once);
    }

    [Fact]
    public async Task Disable_AlreadyDisabledPlugin_Returns0()
    {
        Directory.CreateDirectory(_testStateDir);
        var pluginsDir = Path.Combine(_testStateDir, "plugins");
        var pluginDir = Path.Combine(pluginsDir, "disabled-plugin");
        Directory.CreateDirectory(pluginDir);
        await File.WriteAllTextAsync(Path.Combine(pluginDir, ".disabled"), "");

        _proxyHttpMock.Setup(x => x.NotifyPluginReloadAsync()).Returns(Task.CompletedTask);

        var settings = new PluginSettings { Action = "disable", Target = "disabled-plugin" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);

        Assert.Equal(0, result);
        Assert.True(File.Exists(Path.Combine(pluginDir, ".disabled")));
    }

    [Fact]
    public async Task Uninstall_ExistingPluginDir_Returns0()
    {
        Directory.CreateDirectory(_testStateDir);
        var pluginsDir = Path.Combine(_testStateDir, "plugins");
        var pluginDir = Path.Combine(pluginsDir, "removable-plugin");
        Directory.CreateDirectory(pluginDir);
        await File.WriteAllTextAsync(Path.Combine(pluginDir, "plugin.yaml"), "test");

        _proxyHttpMock.Setup(x => x.NotifyPluginReloadAsync()).Returns(Task.CompletedTask);

        var settings = new PluginSettings { Action = "uninstall", Target = "removable-plugin" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);

        Assert.Equal(0, result);
        Assert.False(Directory.Exists(pluginDir));
        _proxyHttpMock.Verify(x => x.NotifyPluginReloadAsync(), Times.Once);
    }

    [Fact]
    public async Task Uninstall_RemoveAlias_ExistingPluginDir_Returns0()
    {
        Directory.CreateDirectory(_testStateDir);
        var pluginsDir = Path.Combine(_testStateDir, "plugins");
        var pluginDir = Path.Combine(pluginsDir, "alias-plugin");
        Directory.CreateDirectory(pluginDir);

        _proxyHttpMock.Setup(x => x.NotifyPluginReloadAsync()).Returns(Task.CompletedTask);

        var settings = new PluginSettings { Action = "remove", Target = "alias-plugin" };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);

        Assert.Equal(0, result);
        Assert.False(Directory.Exists(pluginDir));
    }

    [Fact]
    public async Task Install_OverwritesExistingPlugin_Returns0()
    {
        Directory.CreateDirectory(_testStateDir);
        var sourceDir = Path.Combine(_testStateDir, "source-plugin");
        Directory.CreateDirectory(sourceDir);
        // CopyDirectory requires at least one subdirectory to create dest root
        Directory.CreateDirectory(Path.Combine(sourceDir, "lib"));
        await File.WriteAllTextAsync(Path.Combine(sourceDir, "plugin.yaml"), "name: test");
        await File.WriteAllTextAsync(Path.Combine(sourceDir, "lib", "core.dll"), "binary");

        // Pre-create destination
        var pluginsDir = Path.Combine(_testStateDir, "plugins");
        var destDir = Path.Combine(pluginsDir, "source-plugin");
        Directory.CreateDirectory(destDir);
        await File.WriteAllTextAsync(Path.Combine(destDir, "old.yaml"), "old");

        _proxyHttpMock.Setup(x => x.NotifyPluginReloadAsync()).Returns(Task.CompletedTask);

        var settings = new PluginSettings { Action = "install", Target = sourceDir };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);

        Assert.Equal(0, result);
        Assert.True(File.Exists(Path.Combine(destDir, "lib", "core.dll")));
        Assert.False(File.Exists(Path.Combine(destDir, "old.yaml")));
    }

    [Fact]
    public async Task Install_WithEnableFlag_RemovesDisabledFile()
    {
        Directory.CreateDirectory(_testStateDir);
        var sourceDir = Path.Combine(_testStateDir, "new-plugin");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(Path.Combine(sourceDir, "lib"));
        await File.WriteAllTextAsync(Path.Combine(sourceDir, "plugin.yaml"), "name: test");

        _proxyHttpMock.Setup(x => x.NotifyPluginReloadAsync()).Returns(Task.CompletedTask);

        var settings = new PluginSettings { Action = "install", Target = sourceDir, Enable = true };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);

        Assert.Equal(0, result);
        var destDir = Path.Combine(_testStateDir, "plugins", "new-plugin");
        Assert.False(File.Exists(Path.Combine(destDir, ".disabled")));
    }

    [Fact]
    public async Task Install_WithSubdirectories_CopiesAllContent()
    {
        Directory.CreateDirectory(_testStateDir);
        var sourceDir = Path.Combine(_testStateDir, "nested-plugin");
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(Path.Combine(sourceDir, "lib"));
        await File.WriteAllTextAsync(Path.Combine(sourceDir, "plugin.yaml"), "name: nested");
        await File.WriteAllTextAsync(Path.Combine(sourceDir, "lib", "data.txt"), "content");

        _proxyHttpMock.Setup(x => x.NotifyPluginReloadAsync()).Returns(Task.CompletedTask);

        var settings = new PluginSettings { Action = "install", Target = sourceDir };
        var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);

        Assert.Equal(0, result);
        var destDir = Path.Combine(_testStateDir, "plugins", "nested-plugin");
        Assert.True(File.Exists(Path.Combine(destDir, "plugin.yaml")));
        Assert.True(File.Exists(Path.Combine(destDir, "lib", "data.txt")));
    }

    [Fact]
    public async Task Create_WithSpacesInName_CreatesSlug_Returns0()
    {
        var tempWorkDir = Path.Combine(Path.GetTempPath(), $"plugin-space-{Guid.NewGuid():N}");
        var originalDir = Directory.GetCurrentDirectory();
        try
        {
            Directory.CreateDirectory(tempWorkDir);
            Directory.SetCurrentDirectory(tempWorkDir);

            var settings = new PluginSettings { Action = "create", Target = "my cool plugin" };
            var result = await _command.ExecuteAsync(CreateContext(), settings, CancellationToken.None);

            Assert.Equal(0, result);
            Assert.True(Directory.Exists(Path.Combine(tempWorkDir, "my-cool-plugin")));
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            try { if (Directory.Exists(tempWorkDir)) Directory.Delete(tempWorkDir, true); } catch { }
            Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", null);
        }
    }
}
