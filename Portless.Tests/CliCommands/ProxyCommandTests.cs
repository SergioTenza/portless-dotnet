extern alias Cli;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Spectre.Console.Cli;
using Xunit;

using IProxyProcessManager = Cli::Portless.Cli.Services.IProxyProcessManager;
using ProxyStartCommand = Cli::Portless.Cli.Commands.ProxyCommand.ProxyStartCommand;
using ProxyStartSettings = Cli::Portless.Cli.Commands.ProxyCommand.ProxyStartSettings;
using ProxyStatusCommand = Cli::Portless.Cli.Commands.ProxyCommand.ProxyStatusCommand;
using ProxyStatusSettings = Cli::Portless.Cli.Commands.ProxyCommand.ProxyStatusSettings;
using ProxyStopCommand = Cli::Portless.Cli.Commands.ProxyCommand.ProxyStopCommand;
using ProxyStopSettings = Cli::Portless.Cli.Commands.ProxyCommand.ProxyStopSettings;

namespace Portless.Tests.CliCommands;

[Collection("SpectreConsoleTests")]
public class ProxyStartCommandTests
{
    private readonly Mock<IProxyProcessManager> _proxyManagerMock;
    private readonly ProxyStartCommand _command;

    public ProxyStartCommandTests()
    {
        _proxyManagerMock = new Mock<IProxyProcessManager>();
        _command = new ProxyStartCommand(_proxyManagerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_AlreadyRunning_Returns1()
    {
        _proxyManagerMock.Setup(x => x.IsRunningAsync())
            .ReturnsAsync(true);
        var settings = new ProxyStartSettings { Port = 1355, EnableHttps = false };

        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
        _proxyManagerMock.Verify(x => x.StartAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_NotRunning_StartsSuccessfully_Returns0()
    {
        _proxyManagerMock.Setup(x => x.IsRunningAsync())
            .ReturnsAsync(false);
        _proxyManagerMock.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);
        var settings = new ProxyStartSettings { Port = 1355, EnableHttps = false };

        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
        _proxyManagerMock.Verify(x => x.StartAsync(1355, false), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_NotRunning_WithHttps_Returns0()
    {
        _proxyManagerMock.Setup(x => x.IsRunningAsync())
            .ReturnsAsync(false);
        _proxyManagerMock.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);
        var settings = new ProxyStartSettings { Port = 8080, EnableHttps = true };

        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
        _proxyManagerMock.Verify(x => x.StartAsync(8080, true), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_StartThrowsInvalidOperation_Returns1()
    {
        _proxyManagerMock.Setup(x => x.IsRunningAsync())
            .ReturnsAsync(false);
        _proxyManagerMock.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<bool>()))
            .ThrowsAsync(new InvalidOperationException("Port in use"));
        var settings = new ProxyStartSettings { Port = 1355, EnableHttps = false };

        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_StartThrowsGenericException_Returns1()
    {
        _proxyManagerMock.Setup(x => x.IsRunningAsync())
            .ReturnsAsync(false);
        _proxyManagerMock.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<bool>()))
            .ThrowsAsync(new Exception("Unexpected error"));
        var settings = new ProxyStartSettings { Port = 1355, EnableHttps = false };

        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_CustomPort_Returns0()
    {
        _proxyManagerMock.Setup(x => x.IsRunningAsync())
            .ReturnsAsync(false);
        _proxyManagerMock.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);
        var settings = new ProxyStartSettings { Port = 8080, EnableHttps = false };

        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
        _proxyManagerMock.Verify(x => x.StartAsync(8080, false), Times.Once);
    }
}

[Collection("SpectreConsoleTests")]
public class ProxyStatusCommandTests
{
    private readonly Mock<IProxyProcessManager> _proxyManagerMock;
    private readonly ProxyStatusCommand _command;

    public ProxyStatusCommandTests()
    {
        _proxyManagerMock = new Mock<IProxyProcessManager>();
        _command = new ProxyStatusCommand(_proxyManagerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_NotRunning_Returns0()
    {
        _proxyManagerMock.Setup(x => x.GetStatusAsync())
            .ReturnsAsync((false, null, null));
        var settings = new ProxyStatusSettings { Protocol = false };

        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_Running_Returns0()
    {
        _proxyManagerMock.Setup(x => x.GetStatusAsync())
            .ReturnsAsync((true, 1355, 9876));
        var settings = new ProxyStatusSettings { Protocol = false };

        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_Running_WithProtocol_Returns0()
    {
        _proxyManagerMock.Setup(x => x.GetStatusAsync())
            .ReturnsAsync((true, 1355, 9876));
        var settings = new ProxyStatusSettings { Protocol = true };

        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_Running_NullPort_Returns0()
    {
        _proxyManagerMock.Setup(x => x.GetStatusAsync())
            .ReturnsAsync((true, null, null));
        var settings = new ProxyStatusSettings { Protocol = false };

        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_Exception_Returns1()
    {
        _proxyManagerMock.Setup(x => x.GetStatusAsync())
            .ThrowsAsync(new Exception("Test error"));
        var settings = new ProxyStatusSettings();

        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }
}

[Collection("SpectreConsoleTests")]
public class ProxyStopCommandTests
{
    private readonly Mock<IProxyProcessManager> _proxyManagerMock;
    private readonly ProxyStopCommand _command;

    public ProxyStopCommandTests()
    {
        _proxyManagerMock = new Mock<IProxyProcessManager>();
        _command = new ProxyStopCommand(_proxyManagerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_NotRunning_Returns0()
    {
        _proxyManagerMock.Setup(x => x.IsRunningAsync())
            .ReturnsAsync(false);
        var settings = new ProxyStopSettings();

        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_Running_NoManagedProcesses_Returns0()
    {
        _proxyManagerMock.Setup(x => x.IsRunningAsync())
            .ReturnsAsync(true);
        _proxyManagerMock.Setup(x => x.GetActiveManagedProcessesAsync())
            .ReturnsAsync(Array.Empty<int>());
        _proxyManagerMock.Setup(x => x.StopAsync())
            .Returns(Task.CompletedTask);
        var settings = new ProxyStopSettings();

        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
        _proxyManagerMock.Verify(x => x.StopAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_StopThrowsInvalidOperation_Returns1()
    {
        _proxyManagerMock.Setup(x => x.IsRunningAsync())
            .ReturnsAsync(true);
        _proxyManagerMock.Setup(x => x.GetActiveManagedProcessesAsync())
            .ReturnsAsync(Array.Empty<int>());
        _proxyManagerMock.Setup(x => x.StopAsync())
            .ThrowsAsync(new InvalidOperationException("Stop failed"));
        var settings = new ProxyStopSettings();

        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_StopThrowsGenericException_Returns1()
    {
        _proxyManagerMock.Setup(x => x.IsRunningAsync())
            .ReturnsAsync(true);
        _proxyManagerMock.Setup(x => x.GetActiveManagedProcessesAsync())
            .ReturnsAsync(Array.Empty<int>());
        _proxyManagerMock.Setup(x => x.StopAsync())
            .ThrowsAsync(new Exception("Unexpected"));
        var settings = new ProxyStopSettings();

        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }
}
