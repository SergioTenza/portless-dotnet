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

/// <summary>
/// Additional UpCommand tests covering successful registration, proxy start failure,
/// and mixed results scenarios.
/// </summary>
[Collection("SpectreConsoleTests")]
public class UpCommandExtendedTests
{
    private readonly Mock<IPortlessConfigLoader> _configLoaderMock;
    private readonly Mock<IProxyRouteRegistrar> _registrarMock;
    private readonly Mock<IProxyProcessManager> _proxyManagerMock;
    private readonly Mock<IProxyConnectionHelper> _proxyConnectionMock;
    private readonly UpCommand _command;

    public UpCommandExtendedTests()
    {
        _configLoaderMock = new Mock<IPortlessConfigLoader>();
        _registrarMock = new Mock<IProxyRouteRegistrar>();
        _proxyManagerMock = new Mock<IProxyProcessManager>();
        _proxyConnectionMock = new Mock<IProxyConnectionHelper>();
        _command = new UpCommand(
            _configLoaderMock.Object,
            _registrarMock.Object,
            _proxyManagerMock.Object,
            _proxyConnectionMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ProxyRunning_SingleRoute_RegistersSuccessfully()
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
        _configLoaderMock.Setup(x => x.Load(It.IsAny<string?>())).Returns(config);
        _proxyConnectionMock.Setup(x => x.IsProxyRunningAsync()).ReturnsAsync(true);
        _registrarMock.Setup(x => x.RegisterRouteAsync("myapp.localhost", "http://localhost:3000"))
            .ReturnsAsync(true);

        var settings = new UpSettings();
        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
        _registrarMock.Verify(x => x.RegisterRouteAsync("myapp.localhost", "http://localhost:3000"), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ProxyRunning_MultipleRoutes_AllRegistered()
    {
        var config = new PortlessConfig
        {
            Routes = new List<PortlessRouteConfig>
            {
                new()
                {
                    Host = "app1.localhost",
                    Backends = new List<string> { "http://localhost:3000" }
                },
                new()
                {
                    Host = "app2.localhost",
                    Backends = new List<string> { "http://localhost:4000" }
                }
            }
        };
        _configLoaderMock.Setup(x => x.Load(It.IsAny<string?>())).Returns(config);
        _proxyConnectionMock.Setup(x => x.IsProxyRunningAsync()).ReturnsAsync(true);
        _registrarMock.Setup(x => x.RegisterRouteAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var settings = new UpSettings();
        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
        _registrarMock.Verify(x => x.RegisterRouteAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteAsync_ProxyRunning_RegistrationFails_Returns1()
    {
        var config = new PortlessConfig
        {
            Routes = new List<PortlessRouteConfig>
            {
                new()
                {
                    Host = "fail.localhost",
                    Backends = new List<string> { "http://localhost:3000" }
                }
            }
        };
        _configLoaderMock.Setup(x => x.Load(It.IsAny<string?>())).Returns(config);
        _proxyConnectionMock.Setup(x => x.IsProxyRunningAsync()).ReturnsAsync(true);
        _registrarMock.Setup(x => x.RegisterRouteAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var settings = new UpSettings();
        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_ProxyNotRunning_StartFailsWithException_Returns1()
    {
        var config = new PortlessConfig
        {
            Routes = new List<PortlessRouteConfig>
            {
                new()
                {
                    Host = "app.localhost",
                    Backends = new List<string> { "http://localhost:3000" }
                }
            }
        };
        _configLoaderMock.Setup(x => x.Load(It.IsAny<string?>())).Returns(config);
        _proxyConnectionMock.Setup(x => x.IsProxyRunningAsync()).ReturnsAsync(false);
        _proxyManagerMock.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<bool>()))
            .ThrowsAsync(new Exception("Start failed"));

        var settings = new UpSettings();
        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_ProxyNotRunning_StartsButVerificationFails_Returns1()
    {
        var config = new PortlessConfig
        {
            Routes = new List<PortlessRouteConfig>
            {
                new()
                {
                    Host = "app.localhost",
                    Backends = new List<string> { "http://localhost:3000" }
                }
            }
        };
        _configLoaderMock.Setup(x => x.Load(It.IsAny<string?>())).Returns(config);
        // First call: not running. Second call: still not running after start attempt
        _proxyConnectionMock.SetupSequence(x => x.IsProxyRunningAsync())
            .ReturnsAsync(false)
            .ReturnsAsync(false);
        _proxyManagerMock.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        var settings = new UpSettings();
        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_RouteWithMultipleBackends_ShowsBackendCount()
    {
        var config = new PortlessConfig
        {
            Routes = new List<PortlessRouteConfig>
            {
                new()
                {
                    Host = "multi.localhost",
                    Backends = new List<string> { "http://localhost:3000", "http://localhost:3001" }
                }
            }
        };
        _configLoaderMock.Setup(x => x.Load(It.IsAny<string?>())).Returns(config);
        _proxyConnectionMock.Setup(x => x.IsProxyRunningAsync()).ReturnsAsync(true);
        _registrarMock.Setup(x => x.RegisterRouteAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        var settings = new UpSettings();
        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_MixedResults_SomeFail_Returns1()
    {
        var config = new PortlessConfig
        {
            Routes = new List<PortlessRouteConfig>
            {
                new()
                {
                    Host = "ok.localhost",
                    Backends = new List<string> { "http://localhost:3000" }
                },
                new()
                {
                    Host = "fail.localhost",
                    Backends = new List<string> { "http://localhost:4000" }
                }
            }
        };
        _configLoaderMock.Setup(x => x.Load(It.IsAny<string?>())).Returns(config);
        _proxyConnectionMock.Setup(x => x.IsProxyRunningAsync()).ReturnsAsync(true);
        _registrarMock.Setup(x => x.RegisterRouteAsync("ok.localhost", It.IsAny<string>()))
            .ReturnsAsync(true);
        _registrarMock.Setup(x => x.RegisterRouteAsync("fail.localhost", It.IsAny<string>()))
            .ReturnsAsync(false);

        var settings = new UpSettings();
        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_RouteWithNullHost_SkipsRoute()
    {
        var config = new PortlessConfig
        {
            Routes = new List<PortlessRouteConfig>
            {
                new() { Host = "", Backends = new List<string> { "http://localhost:3000" } }
            }
        };
        _configLoaderMock.Setup(x => x.Load(It.IsAny<string?>())).Returns(config);
        _proxyConnectionMock.Setup(x => x.IsProxyRunningAsync()).ReturnsAsync(true);

        var settings = new UpSettings();
        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_RouteWithEmptyBackends_SkipsRoute()
    {
        var config = new PortlessConfig
        {
            Routes = new List<PortlessRouteConfig>
            {
                new() { Host = "app.localhost", Backends = new List<string>() }
            }
        };
        _configLoaderMock.Setup(x => x.Load(It.IsAny<string?>())).Returns(config);
        _proxyConnectionMock.Setup(x => x.IsProxyRunningAsync()).ReturnsAsync(true);

        var settings = new UpSettings();
        var result = await _command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }
}
