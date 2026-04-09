extern alias Cli;
using Moq;
using Xunit;

using IDaemonService = Cli::Portless.Cli.Services.IDaemonService;
using DaemonInstallCommand = Cli::Portless.Cli.Commands.DaemonCommand.DaemonInstallCommand;
using DaemonUninstallCommand = Cli::Portless.Cli.Commands.DaemonCommand.DaemonUninstallCommand;
using DaemonStatusCommand = Cli::Portless.Cli.Commands.DaemonCommand.DaemonStatusCommand;
using DaemonEnableCommand = Cli::Portless.Cli.Commands.DaemonCommand.DaemonEnableCommand;
using DaemonDisableCommand = Cli::Portless.Cli.Commands.DaemonCommand.DaemonDisableCommand;
using DaemonInstallSettings = Cli::Portless.Cli.Commands.DaemonCommand.DaemonInstallSettings;
using DaemonUninstallSettings = Cli::Portless.Cli.Commands.DaemonCommand.DaemonUninstallSettings;
using DaemonStatusSettings = Cli::Portless.Cli.Commands.DaemonCommand.DaemonStatusSettings;
using DaemonEnableSettings = Cli::Portless.Cli.Commands.DaemonCommand.DaemonEnableSettings;
using DaemonDisableSettings = Cli::Portless.Cli.Commands.DaemonCommand.DaemonDisableSettings;

namespace Portless.Tests.CliCommands;


[Collection("SpectreConsoleTests")]
public class DaemonUninstallCommandTests
{
    private readonly Mock<IDaemonService> _daemonServiceMock;
    private readonly DaemonUninstallCommand _command;

    public DaemonUninstallCommandTests()
    {
        _daemonServiceMock = new Mock<IDaemonService>();
        _command = new DaemonUninstallCommand(_daemonServiceMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_UninstallsSuccessfully_Returns0()
    {
        var settings = new DaemonUninstallSettings();

        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
        _daemonServiceMock.Verify(x => x.UninstallAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ServiceThrowsException_Returns1()
    {
        _daemonServiceMock.Setup(x => x.UninstallAsync())
            .ThrowsAsync(new InvalidOperationException("Service not found"));

        var settings = new DaemonUninstallSettings();

        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }
}

[Collection("SpectreConsoleTests")]
public class DaemonStatusCommandTests
{
    private readonly Mock<IDaemonService> _daemonServiceMock;
    private readonly DaemonStatusCommand _command;

    public DaemonStatusCommandTests()
    {
        _daemonServiceMock = new Mock<IDaemonService>();
        _command = new DaemonStatusCommand(_daemonServiceMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_NotInstalled_Returns0()
    {
        _daemonServiceMock.Setup(x => x.GetStatusAsync())
            .ReturnsAsync((false, false, false, null));

        var settings = new DaemonStatusSettings();

        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_InstalledButNotRunning_Returns0()
    {
        _daemonServiceMock.Setup(x => x.GetStatusAsync())
            .ReturnsAsync((true, false, false, null));

        var settings = new DaemonStatusSettings();

        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_InstalledAndRunning_Returns0()
    {
        _daemonServiceMock.Setup(x => x.GetStatusAsync())
            .ReturnsAsync((true, true, true, 12345));

        var settings = new DaemonStatusSettings();

        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_InstalledEnabledNotRunning_Returns0()
    {
        _daemonServiceMock.Setup(x => x.GetStatusAsync())
            .ReturnsAsync((true, true, false, null));

        var settings = new DaemonStatusSettings();

        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_ServiceThrowsException_Returns1()
    {
        _daemonServiceMock.Setup(x => x.GetStatusAsync())
            .ThrowsAsync(new Exception("systemctl error"));

        var settings = new DaemonStatusSettings();

        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }
}

[Collection("SpectreConsoleTests")]
public class DaemonEnableCommandTests
{
    private readonly Mock<IDaemonService> _daemonServiceMock;
    private readonly DaemonEnableCommand _command;

    public DaemonEnableCommandTests()
    {
        _daemonServiceMock = new Mock<IDaemonService>();
        _command = new DaemonEnableCommand(_daemonServiceMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_EnableSuccessfully_Returns0()
    {
        var settings = new DaemonEnableSettings();

        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
        _daemonServiceMock.Verify(x => x.EnableAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_NotInstalled_Returns1()
    {
        _daemonServiceMock.Setup(x => x.EnableAsync())
            .ThrowsAsync(new InvalidOperationException("Daemon is not installed"));

        var settings = new DaemonEnableSettings();

        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_SystemctlFails_Returns1()
    {
        _daemonServiceMock.Setup(x => x.EnableAsync())
            .ThrowsAsync(new Exception("Permission denied"));

        var settings = new DaemonEnableSettings();

        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }
}

[Collection("SpectreConsoleTests")]
public class DaemonDisableCommandTests
{
    private readonly Mock<IDaemonService> _daemonServiceMock;
    private readonly DaemonDisableCommand _command;

    public DaemonDisableCommandTests()
    {
        _daemonServiceMock = new Mock<IDaemonService>();
        _command = new DaemonDisableCommand(_daemonServiceMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_DisableSuccessfully_Returns0()
    {
        var settings = new DaemonDisableSettings();

        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
        _daemonServiceMock.Verify(x => x.DisableAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_NotInstalled_Returns1()
    {
        _daemonServiceMock.Setup(x => x.DisableAsync())
            .ThrowsAsync(new InvalidOperationException("Daemon is not installed"));

        var settings = new DaemonDisableSettings();

        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_SystemctlFails_Returns1()
    {
        _daemonServiceMock.Setup(x => x.DisableAsync())
            .ThrowsAsync(new Exception("Permission denied"));

        var settings = new DaemonDisableSettings();

        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }
}
