extern alias Cli;
using Moq;
using Portless.Core.Models;
using Portless.Core.Services;
using ListCommand = Cli::Portless.Cli.Commands.ListCommand.ListCommand;
using ListSettings = Cli::Portless.Cli.Commands.ListCommand.ListSettings;
using Spectre.Console.Cli;
using Xunit;

namespace Portless.Tests.CliCommands;

public class ListCommandTests
{
    private readonly Mock<IRouteStore> _routeStoreMock;
    private readonly Mock<IProcessLivenessChecker> _livenessCheckerMock;
    private readonly ListCommand _command;

    public ListCommandTests()
    {
        _routeStoreMock = new Mock<IRouteStore>();
        _livenessCheckerMock = new Mock<IProcessLivenessChecker>();
        _command = new ListCommand(_routeStoreMock.Object, _livenessCheckerMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_NoRoutes_Returns0()
    {
        // Arrange
        _routeStoreMock.Setup(x => x.LoadRoutesAsync())
            .ReturnsAsync(Array.Empty<RouteInfo>());

        var settings = new ListSettings();

        // Act
        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithRoutes_Returns0()
    {
        // Arrange
        var routes = new[]
        {
            new RouteInfo
            {
                Hostname = "myapp.localhost", Port = 3000, Pid = 1234,
                CreatedAt = DateTime.UtcNow, LastSeen = DateTime.UtcNow
            },
            new RouteInfo
            {
                Hostname = "api.localhost", Port = 4000, Pid = 5678,
                CreatedAt = DateTime.UtcNow, LastSeen = DateTime.UtcNow
            }
        };
        _routeStoreMock.Setup(x => x.LoadRoutesAsync())
            .ReturnsAsync(routes);
        _livenessCheckerMock.Setup(x => x.IsAlive(It.IsAny<RouteInfo>()))
            .Returns(true);

        var settings = new ListSettings();

        // Act
        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithRoutes_SortsByHostname()
    {
        // Arrange
        var routes = new[]
        {
            new RouteInfo
            {
                Hostname = "z-app.localhost", Port = 3000, Pid = 1234,
                CreatedAt = DateTime.UtcNow, LastSeen = DateTime.UtcNow
            },
            new RouteInfo
            {
                Hostname = "a-app.localhost", Port = 4000, Pid = 5678,
                CreatedAt = DateTime.UtcNow, LastSeen = DateTime.UtcNow
            }
        };
        _routeStoreMock.Setup(x => x.LoadRoutesAsync())
            .ReturnsAsync(routes);
        _livenessCheckerMock.Setup(x => x.IsAlive(It.IsAny<RouteInfo>()))
            .Returns(true);

        var settings = new ListSettings();

        // Act
        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_DeadProcess_Returns0()
    {
        // Arrange
        var routes = new[]
        {
            new RouteInfo
            {
                Hostname = "myapp.localhost", Port = 3000, Pid = 9999,
                CreatedAt = DateTime.UtcNow, LastSeen = DateTime.UtcNow
            }
        };
        _routeStoreMock.Setup(x => x.LoadRoutesAsync())
            .ReturnsAsync(routes);
        _livenessCheckerMock.Setup(x => x.IsAlive(It.IsAny<RouteInfo>()))
            .Returns(false);

        var settings = new ListSettings();

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
        _routeStoreMock.Setup(x => x.LoadRoutesAsync())
            .ThrowsAsync(new Exception("Test error"));

        var settings = new ListSettings();

        // Act
        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_ManyRoutes_Returns0()
    {
        // Arrange
        var routes = Enumerable.Range(1, 50).Select(i => new RouteInfo
        {
            Hostname = $"app{i}.localhost",
            Port = 3000 + i,
            Pid = 1000 + i,
            CreatedAt = DateTime.UtcNow,
            LastSeen = DateTime.UtcNow
        }).ToArray();

        _routeStoreMock.Setup(x => x.LoadRoutesAsync())
            .ReturnsAsync(routes);
        _livenessCheckerMock.Setup(x => x.IsAlive(It.IsAny<RouteInfo>()))
            .Returns(true);

        var settings = new ListSettings();

        // Act
        var result = await _command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
    }
}
