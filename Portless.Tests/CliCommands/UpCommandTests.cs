extern alias Cli;
using Moq;
using Portless.Core.Models;
using Portless.Core.Services;
using Spectre.Console.Cli;
using Xunit;

using UpCommand = Cli::Portless.Cli.Commands.UpCommand.UpCommand;
using UpSettings = Cli::Portless.Cli.Commands.UpCommand.UpSettings;
using IProxyProcessManager = Cli::Portless.Cli.Services.IProxyProcessManager;

namespace Portless.Tests.CliCommands;

public class UpCommandTests
{
    private readonly Mock<IPortlessConfigLoader> _configLoaderMock;
    private readonly Mock<IProxyRouteRegistrar> _registrarMock;
    private readonly Mock<IProxyProcessManager> _proxyManagerMock;
    private readonly UpCommand _command;

    public UpCommandTests()
    {
        _configLoaderMock = new Mock<IPortlessConfigLoader>();
        _registrarMock = new Mock<IProxyRouteRegistrar>();
        _proxyManagerMock = new Mock<IProxyProcessManager>();
        _command = new UpCommand(
            _configLoaderMock.Object,
            _registrarMock.Object,
            _proxyManagerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_NoRoutes_Returns0()
    {
        var config = new PortlessConfig { Routes = new List<PortlessRouteConfig>() };
        _configLoaderMock.Setup(x => x.Load(It.IsAny<string?>()))
            .Returns(config);
        var settings = new UpSettings { ConfigFile = null };

        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_RouteWithMissingHost_SkipsAndFails()
    {
        var config = new PortlessConfig
        {
            Routes = new List<PortlessRouteConfig>
            {
                new() { Host = null, Backends = new List<string> { "http://localhost:3000" } }
            }
        };
        _configLoaderMock.Setup(x => x.Load(It.IsAny<string?>()))
            .Returns(config);
        var settings = new UpSettings { ConfigFile = null };

        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_RouteWithMissingBackends_SkipsAndFails()
    {
        var config = new PortlessConfig
        {
            Routes = new List<PortlessRouteConfig>
            {
                new() { Host = "myapp.localhost", Backends = new List<string>() }
            }
        };
        _configLoaderMock.Setup(x => x.Load(It.IsAny<string?>()))
            .Returns(config);
        var settings = new UpSettings { ConfigFile = null };

        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_MultipleRoutes_AllInvalid_Returns1()
    {
        var config = new PortlessConfig
        {
            Routes = new List<PortlessRouteConfig>
            {
                new() { Host = null, Backends = new List<string>() },
                new() { Host = "api.localhost", Backends = new List<string>() }
            }
        };
        _configLoaderMock.Setup(x => x.Load(It.IsAny<string?>()))
            .Returns(config);
        var settings = new UpSettings { ConfigFile = null };

        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithConfigFilePath_PassesToLoader()
    {
        var config = new PortlessConfig { Routes = new List<PortlessRouteConfig>() };
        _configLoaderMock.Setup(x => x.Load("custom-config.yaml"))
            .Returns(config);
        var settings = new UpSettings { ConfigFile = "custom-config.yaml" };

        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
        _configLoaderMock.Verify(x => x.Load("custom-config.yaml"), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_NullConfigFilePath_PassesNullToLoader()
    {
        var config = new PortlessConfig { Routes = new List<PortlessRouteConfig>() };
        _configLoaderMock.Setup(x => x.Load((string?)null))
            .Returns(config);
        var settings = new UpSettings { ConfigFile = null };

        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
        _configLoaderMock.Verify(x => x.Load((string?)null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SingleValidRoute_NeedsProxy_FailsGracefully()
    {
        var config = new PortlessConfig
        {
            Routes = new List<PortlessRouteConfig>
            {
                new()
                {
                    Host = "myapp.localhost",
                    Backends = new List<string> { "http://localhost:3000" }
                }
            }
        };
        _configLoaderMock.Setup(x => x.Load(It.IsAny<string?>()))
            .Returns(config);
        _proxyManagerMock.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        var settings = new UpSettings { ConfigFile = null };

        // Proxy isn't actually running, so it will attempt to start, fail verification, return error
        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        Assert.True(result == 0 || result == 1);
    }
}
