extern alias Cli;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Portless.Core.Models;
using Portless.Core.Services;
using Spectre.Console.Cli;
using Xunit;

using AliasCommand = Cli::Portless.Cli.Commands.AliasCommand.AliasCommand;
using AliasSettings = Cli::Portless.Cli.Commands.AliasCommand.AliasSettings;
using IProxyProcessManager = Cli::Portless.Cli.Services.IProxyProcessManager;

namespace Portless.Tests.CliCommands;

[Collection("SpectreConsoleTests")]
public class AliasCommandTests
{
    private readonly Mock<IRouteStore> _routeStoreMock;
    private readonly Mock<IProxyProcessManager> _proxyProcessManagerMock;
    private readonly Mock<IProxyRouteRegistrar> _registrarMock;
    private readonly AliasCommand _command;

    public AliasCommandTests()
    {
        _routeStoreMock = new Mock<IRouteStore>();
        _proxyProcessManagerMock = new Mock<IProxyProcessManager>();
        _registrarMock = new Mock<IProxyRouteRegistrar>();
        _command = new AliasCommand(
            _routeStoreMock.Object,
            _proxyProcessManagerMock.Object,
            _registrarMock.Object);
    }

    private static AliasSettings CreateSettings(
        string name = "myapp",
        int? port = 3000,
        bool remove = false,
        string host = "localhost",
        string protocol = "http",
        string? path = null)
    {
        return new AliasSettings
        {
            Name = name,
            Port = port,
            Remove = remove,
            Host = host,
            Protocol = protocol,
            Path = path
        };
    }

    [Fact]
    public async Task ExecuteAsync_AddAlias_NoPort_Returns1()
    {
        var settings = CreateSettings(port: null);
        _routeStoreMock.Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RouteInfo>());

        var result = await _command.ExecuteAsync(
            null!,
            settings, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_AddAlias_Success_Returns0()
    {
        var settings = CreateSettings(name: "myapp", port: 3000);
        _routeStoreMock.Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RouteInfo>());
        _registrarMock.Setup(x => x.RegisterRouteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(true);
        _routeStoreMock.Setup(x => x.SaveRoutesAsync(It.IsAny<RouteInfo[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _command.ExecuteAsync(
            null!,
            settings, CancellationToken.None);

        Assert.Equal(0, result);
        _registrarMock.Verify(x => x.RegisterRouteAsync(
            "myapp.localhost", "http://localhost:3000", null), Times.Once);
        _routeStoreMock.Verify(x => x.SaveRoutesAsync(
            It.IsAny<RouteInfo[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_AddAlias_Duplicate_Returns1()
    {
        var settings = CreateSettings(name: "myapp", port: 3000);
        var existingRoutes = new[]
        {
            new RouteInfo { Hostname = "myapp.localhost", Port = 4000, Pid = 1 }
        };
        _routeStoreMock.Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRoutes);

        var result = await _command.ExecuteAsync(
            null!,
            settings, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_AddAlias_AppendsDotLocalhost()
    {
        var settings = CreateSettings(name: "db", port: 5432);
        _routeStoreMock.Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RouteInfo>());
        _registrarMock.Setup(x => x.RegisterRouteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(true);
        _routeStoreMock.Setup(x => x.SaveRoutesAsync(It.IsAny<RouteInfo[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _command.ExecuteAsync(
            null!,
            settings, CancellationToken.None);

        Assert.Equal(0, result);
        _registrarMock.Verify(x => x.RegisterRouteAsync(
            "db.localhost", "http://localhost:5432", null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_AddAlias_WithExistingSuffix_DoesNotDoubleAppend()
    {
        var settings = CreateSettings(name: "myapp.localhost", port: 3000);
        _routeStoreMock.Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RouteInfo>());
        _registrarMock.Setup(x => x.RegisterRouteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(true);
        _routeStoreMock.Setup(x => x.SaveRoutesAsync(It.IsAny<RouteInfo[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _command.ExecuteAsync(
            null!,
            settings, CancellationToken.None);

        Assert.Equal(0, result);
        _registrarMock.Verify(x => x.RegisterRouteAsync(
            "myapp.localhost", It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_AddAlias_RegistrationFails_StillSaves_Returns0()
    {
        var settings = CreateSettings(name: "myapp", port: 3000);
        _routeStoreMock.Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RouteInfo>());
        _registrarMock.Setup(x => x.RegisterRouteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(false);
        _routeStoreMock.Setup(x => x.SaveRoutesAsync(It.IsAny<RouteInfo[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _command.ExecuteAsync(
            null!,
            settings, CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_AddAlias_RegistrationThrows_StillSaves_Returns0()
    {
        var settings = CreateSettings(name: "myapp", port: 3000);
        _routeStoreMock.Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RouteInfo>());
        _registrarMock.Setup(x => x.RegisterRouteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));
        _routeStoreMock.Setup(x => x.SaveRoutesAsync(It.IsAny<RouteInfo[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _command.ExecuteAsync(
            null!,
            settings, CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_AddAlias_WithCustomProtocol()
    {
        var settings = CreateSettings(name: "myapp", port: 3000, protocol: "https");
        _routeStoreMock.Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RouteInfo>());
        _registrarMock.Setup(x => x.RegisterRouteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(true);
        _routeStoreMock.Setup(x => x.SaveRoutesAsync(It.IsAny<RouteInfo[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _command.ExecuteAsync(
            null!,
            settings, CancellationToken.None);

        Assert.Equal(0, result);
        _registrarMock.Verify(x => x.RegisterRouteAsync(
            "myapp.localhost", "https://localhost:3000", null), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_AddAlias_WithPath()
    {
        var settings = CreateSettings(name: "myapp", port: 3000, path: "/api");
        _routeStoreMock.Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RouteInfo>());
        _registrarMock.Setup(x => x.RegisterRouteAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(true);
        _routeStoreMock.Setup(x => x.SaveRoutesAsync(It.IsAny<RouteInfo[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _command.ExecuteAsync(
            null!,
            settings, CancellationToken.None);

        Assert.Equal(0, result);
        _registrarMock.Verify(x => x.RegisterRouteAsync(
            "myapp.localhost", "http://localhost:3000", "/api"), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_RemoveAlias_Success_Returns0()
    {
        var settings = CreateSettings(name: "myapp", remove: true);
        var existingRoutes = new[]
        {
            new RouteInfo { Hostname = "myapp.localhost", Port = 3000, Pid = 0 }
        };
        _routeStoreMock.Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRoutes);
        _registrarMock.Setup(x => x.RemoveRouteAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _routeStoreMock.Setup(x => x.SaveRoutesAsync(It.IsAny<RouteInfo[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _command.ExecuteAsync(
            null!,
            settings, CancellationToken.None);

        Assert.Equal(0, result);
        _routeStoreMock.Verify(x => x.SaveRoutesAsync(
            It.Is<RouteInfo[]>(r => r.Length == 0), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_RemoveAlias_NotFound_Returns1()
    {
        var settings = CreateSettings(name: "nonexistent", remove: true);
        _routeStoreMock.Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RouteInfo>());

        var result = await _command.ExecuteAsync(
            null!,
            settings, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_RemoveAlias_RegistrarThrows_StillSaves_Returns0()
    {
        var settings = CreateSettings(name: "myapp", remove: true);
        var existingRoutes = new[]
        {
            new RouteInfo { Hostname = "myapp.localhost", Port = 3000, Pid = 0 }
        };
        _routeStoreMock.Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRoutes);
        _registrarMock.Setup(x => x.RemoveRouteAsync(It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));
        _routeStoreMock.Setup(x => x.SaveRoutesAsync(It.IsAny<RouteInfo[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _command.ExecuteAsync(
            null!,
            settings, CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_Exception_Returns1()
    {
        var settings = CreateSettings(name: "myapp", port: 3000);
        _routeStoreMock.Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test error"));

        var result = await _command.ExecuteAsync(
            null!,
            settings, CancellationToken.None);

        Assert.Equal(1, result);
    }
}
