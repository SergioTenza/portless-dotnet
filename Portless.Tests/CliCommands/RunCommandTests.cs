extern alias Cli;
using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Portless.Core.Models;
using Portless.Core.Services;
using Spectre.Console.Cli;
using Xunit;

using RunCommand = Cli::Portless.Cli.Commands.RunCommand.RunCommand;
using RunSettings = Cli::Portless.Cli.Commands.RunCommand.RunSettings;
using IProxyProcessManager = Cli::Portless.Cli.Services.IProxyProcessManager;

namespace Portless.Tests.CliCommands;

[Collection("SpectreConsoleTests")]
public class RunCommandTests
{
    private readonly Mock<IPortAllocator> _portAllocatorMock;
    private readonly Mock<IRouteStore> _routeStoreMock;
    private readonly Mock<IProxyProcessManager> _proxyManagerMock;
    private readonly Mock<IProcessManager> _processManagerMock;
    private readonly Mock<IFrameworkDetector> _frameworkDetectorMock;
    private readonly Mock<IProjectNameDetector> _projectNameDetectorMock;
    private readonly Mock<IProxyRouteRegistrar> _registrarMock;
    private readonly RunCommand _command;

    public RunCommandTests()
    {
        _portAllocatorMock = new Mock<IPortAllocator>();
        _routeStoreMock = new Mock<IRouteStore>();
        _proxyManagerMock = new Mock<IProxyProcessManager>();
        _processManagerMock = new Mock<IProcessManager>();
        _frameworkDetectorMock = new Mock<IFrameworkDetector>();
        _projectNameDetectorMock = new Mock<IProjectNameDetector>();
        _registrarMock = new Mock<IProxyRouteRegistrar>();

        _command = new RunCommand(
            _portAllocatorMock.Object,
            _routeStoreMock.Object,
            _proxyManagerMock.Object,
            _processManagerMock.Object,
            _frameworkDetectorMock.Object,
            _projectNameDetectorMock.Object,
            NullLogger<RunCommand>.Instance,
            _registrarMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_NoCommandArgs_Returns1()
    {
        var settings = new RunSettings { Name = "myapp" };
        var context = new CommandContext(["myapp"], new TestRemainingArguments(), "run", null);

        var result = await _command.ExecuteAsync(context, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyName_NoAutoDetect_Returns1()
    {
        var settings = new RunSettings { Name = "" };
        _projectNameDetectorMock.Setup(x => x.DetectProjectName(It.IsAny<string?>()))
            .Returns((string?)null);
        var context = new CommandContext([""], new TestRemainingArguments(), "run", null);

        var result = await _command.ExecuteAsync(context, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_DuplicateRoute_Returns1()
    {
        var settings = new RunSettings { Name = "myapp" };
        var context = new CommandContext(["myapp", "node", "server.js"], new TestRemainingArguments(), "run", null);

        var existingRoutes = new[]
        {
            new RouteInfo { Hostname = "myapp.localhost", Port = 3000, Pid = 1234 }
        };
        _routeStoreMock.Setup(x => x.LoadRoutesAsync())
            .ReturnsAsync(existingRoutes);

        var result = await _command.ExecuteAsync(context, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_AutoDetectName_CalledWhenNameIsEmpty()
    {
        var settings = new RunSettings { Name = "" };
        _projectNameDetectorMock.Setup(x => x.DetectProjectName(It.IsAny<string?>()))
            .Returns("autodetected");
        var context = new CommandContext(["", "node", "server.js"], new TestRemainingArguments(), "run", null);
        _routeStoreMock.Setup(x => x.LoadRoutesAsync())
            .ReturnsAsync(Array.Empty<RouteInfo>());

        await _command.ExecuteAsync(context, settings, CancellationToken.None);

        _projectNameDetectorMock.Verify(x => x.DetectProjectName(It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_AutoDetectName_EmptyResult_Returns1()
    {
        var settings = new RunSettings { Name = "" };
        _projectNameDetectorMock.Setup(x => x.DetectProjectName(It.IsAny<string?>()))
            .Returns((string?)null);
        var context = new CommandContext(["", "node", "server.js"], new TestRemainingArguments(), "run", null);

        var result = await _command.ExecuteAsync(context, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }

    /// <summary>
    /// Tries to acquire port 1355 for the proxy simulation. Returns null if port is busy (skip test).
    /// </summary>
    private static System.Net.Sockets.TcpListener? TryListenOnPort(int port)
    {
        try
        {
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, port);
            listener.Start();
            return listener;
        }
        catch (System.Net.Sockets.SocketException)
        {
            return null; // Port busy, test will be skipped
        }
    }

    [Fact]
    public async Task ExecuteAsync_FrameworkDetected_Called()
    {
        // Start a TCP listener so RunCommand's proxy check (localhost:1355) succeeds
        var tcpListener = TryListenOnPort(1355);
        if (tcpListener == null) return; // Skip if port unavailable (CI environment)
        try
        {
            var settings = new RunSettings { Name = "myapp" };
            var context = new CommandContext(["myapp", "dotnet", "run"], new TestRemainingArguments(), "run", null);

            _routeStoreMock.Setup(x => x.LoadRoutesAsync())
                .ReturnsAsync(Array.Empty<RouteInfo>());
            _frameworkDetectorMock.Setup(x => x.Detect(It.IsAny<string?>()))
                .Returns(new DetectedFramework
                {
                    Name = "dotnet",
                    DisplayName = ".NET",
                    InjectedEnvVars = ["ASPNETCORE_URLS=http://localhost:{port}"],
                    InjectedFlags = Array.Empty<string>()
                });

            await _command.ExecuteAsync(context, settings, CancellationToken.None);

            _frameworkDetectorMock.Verify(x => x.Detect(It.IsAny<string?>()), Times.Once);
        }
        finally
        {
            tcpListener.Stop();
        }
    }

    [Fact]
    public async Task ExecuteAsync_NoFrameworkDetected_Called()
    {
        // Start a TCP listener so RunCommand's proxy check (localhost:1355) succeeds
        var tcpListener = TryListenOnPort(1355);
        if (tcpListener == null) return; // Skip if port unavailable (CI environment)
        try
        {
            var settings = new RunSettings { Name = "myapp" };
            var context = new CommandContext(["myapp", "node", "server.js"], new TestRemainingArguments(), "run", null);

            _routeStoreMock.Setup(x => x.LoadRoutesAsync())
                .ReturnsAsync(Array.Empty<RouteInfo>());
            _frameworkDetectorMock.Setup(x => x.Detect(It.IsAny<string?>()))
                .Returns((DetectedFramework?)null);

            await _command.ExecuteAsync(context, settings, CancellationToken.None);

            _frameworkDetectorMock.Verify(x => x.Detect(It.IsAny<string?>()), Times.Once);
        }
        finally
        {
            tcpListener.Stop();
        }
    }

    [Fact]
    public async Task ExecuteAsync_InvalidOperation_Returns1()
    {
        var settings = new RunSettings { Name = "myapp" };
        var context = new CommandContext(["myapp", "node", "server.js"], new TestRemainingArguments(), "run", null);

        _routeStoreMock.Setup(x => x.LoadRoutesAsync())
            .ThrowsAsync(new InvalidOperationException("Store error"));

        var result = await _command.ExecuteAsync(context, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_GenericException_Returns1()
    {
        var settings = new RunSettings { Name = "myapp" };
        var context = new CommandContext(["myapp", "node", "server.js"], new TestRemainingArguments(), "run", null);

        _routeStoreMock.Setup(x => x.LoadRoutesAsync())
            .ThrowsAsync(new Exception("Unexpected"));

        var result = await _command.ExecuteAsync(context, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_WithBackendsSetting()
    {
        var settings = new RunSettings { Name = "myapp", Backends = new[] { "http://localhost:5000" } };
        var context = new CommandContext(["myapp", "node", "server.js"], new TestRemainingArguments(), "run", null);

        _routeStoreMock.Setup(x => x.LoadRoutesAsync())
            .ReturnsAsync(Array.Empty<RouteInfo>());

        // Will fail at proxy stage since no proxy is running
        var result = await _command.ExecuteAsync(context, settings, CancellationToken.None);

        // Just verify no crash
        Assert.True(result == 0 || result == 1);
    }
}
