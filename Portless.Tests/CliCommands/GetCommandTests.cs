extern alias Cli;
using Moq;
using Portless.Core.Models;
using Portless.Core.Services;
using GetCommand = Cli::Portless.Cli.Commands.GetCommand.GetCommand;
using GetSettings = Cli::Portless.Cli.Commands.GetCommand.GetSettings;
using Spectre.Console.Cli;
using Xunit;

namespace Portless.Tests.CliCommands;

[Collection("SpectreConsoleTests")]
public class GetCommandTests
{
    private readonly Mock<IRouteStore> _routeStoreMock;
    private readonly GetCommand _command;

    public GetCommandTests()
    {
        _routeStoreMock = new Mock<IRouteStore>();
        _command = new GetCommand(_routeStoreMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_RouteFound_Returns0()
    {
        // Arrange
        var routes = new[]
        {
            new RouteInfo { Hostname = "myapp.localhost", Port = 3000, Pid = 1234 }
        };
        _routeStoreMock.Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(routes);
        var settings = new GetSettings { Name = "myapp", Json = false };

        // Act
        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_RouteNotFound_Returns1()
    {
        // Arrange
        _routeStoreMock.Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RouteInfo>());
        var settings = new GetSettings { Name = "nonexistent", Json = false };

        // Act
        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_AppendsDotLocalhost_Returns0()
    {
        // Arrange
        var routes = new[]
        {
            new RouteInfo { Hostname = "myapp.localhost", Port = 3000, Pid = 1234 }
        };
        _routeStoreMock.Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(routes);
        var settings = new GetSettings { Name = "myapp", Json = false };

        // Act
        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithFullHostname_Returns0()
    {
        // Arrange
        var routes = new[]
        {
            new RouteInfo { Hostname = "myapp.localhost", Port = 3000, Pid = 1234 }
        };
        _routeStoreMock.Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(routes);
        var settings = new GetSettings { Name = "myapp.localhost", Json = false };

        // Act
        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_JsonOutput_Returns0()
    {
        // Arrange
        var routes = new[]
        {
            new RouteInfo { Hostname = "myapp.localhost", Port = 3000, Pid = 1234 }
        };
        _routeStoreMock.Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(routes);
        var settings = new GetSettings { Name = "myapp", Json = true };

        // Act
        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_CaseInsensitiveMatch_Returns0()
    {
        // Arrange
        var routes = new[]
        {
            new RouteInfo { Hostname = "MyApp.localhost", Port = 3000, Pid = 1234 }
        };
        _routeStoreMock.Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(routes);
        var settings = new GetSettings { Name = "myapp", Json = false };

        // Act
        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_Exception_Returns1()
    {
        // Arrange
        _routeStoreMock.Setup(x => x.LoadRoutesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test error"));
        var settings = new GetSettings { Name = "myapp", Json = false };

        // Act
        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(1, result);
    }
}
