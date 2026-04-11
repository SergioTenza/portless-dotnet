extern alias Cli;
using Cli::Portless.Cli.Services;
using Moq;
using Xunit;

using DaemonInstallCommand = Cli::Portless.Cli.Commands.DaemonCommand.DaemonInstallCommand;
using DaemonInstallSettings = Cli::Portless.Cli.Commands.DaemonCommand.DaemonInstallSettings;

namespace Portless.Tests.CliCommands;

/// <summary>
/// Tests for DaemonInstallCommand (the only daemon sub-command without existing coverage).
/// </summary>
[Collection("SpectreConsoleTests")]
public class DaemonInstallCommandTests
{
    private readonly Mock<IDaemonService> _daemonServiceMock;
    private readonly DaemonInstallCommand _command;

    public DaemonInstallCommandTests()
    {
        _daemonServiceMock = new Mock<IDaemonService>();
        _command = new DaemonInstallCommand(_daemonServiceMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_Success_Returns0()
    {
        _daemonServiceMock.Setup(x => x.InstallAsync(false, false))
            .Returns(Task.CompletedTask);

        var settings = new DaemonInstallSettings { EnableHttps = false, EnableNow = false };
        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
        _daemonServiceMock.Verify(x => x.InstallAsync(false, false), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithHttps_Returns0()
    {
        _daemonServiceMock.Setup(x => x.InstallAsync(true, false))
            .Returns(Task.CompletedTask);

        var settings = new DaemonInstallSettings { EnableHttps = true, EnableNow = false };
        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
        _daemonServiceMock.Verify(x => x.InstallAsync(true, false), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithEnableNow_Returns0()
    {
        _daemonServiceMock.Setup(x => x.InstallAsync(false, true))
            .Returns(Task.CompletedTask);

        var settings = new DaemonInstallSettings { EnableHttps = false, EnableNow = true };
        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithHttpsAndEnable_Returns0()
    {
        _daemonServiceMock.Setup(x => x.InstallAsync(true, true))
            .Returns(Task.CompletedTask);

        var settings = new DaemonInstallSettings { EnableHttps = true, EnableNow = true };
        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
        _daemonServiceMock.Verify(x => x.InstallAsync(true, true), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ServiceThrows_Returns1()
    {
        _daemonServiceMock.Setup(x => x.InstallAsync(It.IsAny<bool>(), It.IsAny<bool>()))
            .ThrowsAsync(new InvalidOperationException("systemctl failed"));

        var settings = new DaemonInstallSettings();
        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_ServiceThrowsGeneric_Returns1()
    {
        _daemonServiceMock.Setup(x => x.InstallAsync(It.IsAny<bool>(), It.IsAny<bool>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        var settings = new DaemonInstallSettings();
        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }
}
