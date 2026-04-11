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

/// <summary>
/// Additional RunCommand tests covering successful proxy auto-start,
/// framework with no injected flags, and various error conditions.
/// </summary>
[Collection("SpectreConsoleTests")]
public class RunCommandAdditionalTests
{
    private readonly Mock<IPortAllocator> _portAllocatorMock;
    private readonly Mock<IRouteStore> _routeStoreMock;
    private readonly Mock<IProxyProcessManager> _proxyManagerMock;
    private readonly Mock<IProcessManager> _processManagerMock;
    private readonly Mock<IFrameworkDetector> _frameworkDetectorMock;
    private readonly Mock<IProjectNameDetector> _projectNameDetectorMock;
    private readonly Mock<IProxyRouteRegistrar> _registrarMock;
    private readonly Mock<IProxyConnectionHelper> _proxyConnectionMock;
    private readonly RunCommand _command;

    public RunCommandAdditionalTests()
    {
        _portAllocatorMock = new Mock<IPortAllocator>();
        _routeStoreMock = new Mock<IRouteStore>();
        _proxyManagerMock = new Mock<IProxyProcessManager>();
        _processManagerMock = new Mock<IProcessManager>();
        _frameworkDetectorMock = new Mock<IFrameworkDetector>();
        _projectNameDetectorMock = new Mock<IProjectNameDetector>();
        _registrarMock = new Mock<IProxyRouteRegistrar>();
        _proxyConnectionMock = new Mock<IProxyConnectionHelper>();

        _command = new RunCommand(
            _portAllocatorMock.Object,
            _routeStoreMock.Object,
            _proxyManagerMock.Object,
            _processManagerMock.Object,
            _frameworkDetectorMock.Object,
            _projectNameDetectorMock.Object,
            NullLogger<RunCommand>.Instance,
            _registrarMock.Object,
            _proxyConnectionMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_ProxyAutoStart_Succeeds_Returns0()
    {
        var settings = new RunSettings { Name = "myapp" };
        var context = new CommandContext(["myapp", "node", "server.js"], new TestRemainingArguments(), "run", null);

        _proxyConnectionMock.SetupSequence(x => x.IsProxyRunningAsync())
            .ReturnsAsync(false)  // First check: not running
            .ReturnsAsync(true);  // After start: running
        _proxyManagerMock.Setup(x => x.StartAsync(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);
        _routeStoreMock.Setup(x => x.LoadRoutesAsync()).ReturnsAsync(Array.Empty<RouteInfo>());
        _portAllocatorMock.Setup(x => x.AssignFreePortAsync(It.IsAny<int>())).ReturnsAsync(4500);
        _portAllocatorMock.Setup(x => x.ReleasePortAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _frameworkDetectorMock.Setup(x => x.Detect(It.IsAny<string?>()))
            .Returns((DetectedFramework?)null);

        using var process = System.Diagnostics.Process.Start("sleep", "30");
        try
        {
            _processManagerMock.Setup(x => x.StartManagedProcess(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .Returns(process!);
            _proxyManagerMock.Setup(x => x.RegisterManagedProcessAsync(It.IsAny<int>()))
                .Returns(Task.CompletedTask);
            _registrarMock.Setup(x => x.RegisterRouteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
                .ReturnsAsync(true);

            var result = await _command.ExecuteAsync(context, settings, CancellationToken.None);
            Assert.Equal(0, result);
        }
        finally
        {
            try { process?.Kill(); } catch { }
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithFrameworkEnvVars_Returns0()
    {
        var settings = new RunSettings { Name = "myapp" };
        var context = new CommandContext(["myapp", "python", "-m", "http.server"], new TestRemainingArguments(), "run", null);

        _proxyConnectionMock.Setup(x => x.IsProxyRunningAsync()).ReturnsAsync(true);
        _routeStoreMock.Setup(x => x.LoadRoutesAsync()).ReturnsAsync(Array.Empty<RouteInfo>());
        _portAllocatorMock.Setup(x => x.AssignFreePortAsync(It.IsAny<int>())).ReturnsAsync(8080);
        _portAllocatorMock.Setup(x => x.ReleasePortAsync(It.IsAny<int>())).Returns(Task.CompletedTask);
        _frameworkDetectorMock.Setup(x => x.Detect(It.IsAny<string?>()))
            .Returns(new DetectedFramework
            {
                Name = "python",
                DisplayName = "Python",
                InjectedEnvVars = ["PORT={port}", "HOST={hostname}"],
                InjectedFlags = Array.Empty<string>()
            });

        using var process = System.Diagnostics.Process.Start("sleep", "30");
        try
        {
            _processManagerMock.Setup(x => x.StartManagedProcess(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
                .Returns(process!);
            _proxyManagerMock.Setup(x => x.RegisterManagedProcessAsync(It.IsAny<int>()))
                .Returns(Task.CompletedTask);
            _registrarMock.Setup(x => x.RegisterRouteAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
                .ReturnsAsync(true);

            var result = await _command.ExecuteAsync(context, settings, CancellationToken.None);
            Assert.Equal(0, result);

            // Verify env vars were passed
            _processManagerMock.Verify(x => x.StartManagedProcess(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
                It.IsAny<string>(),
                It.Is<Dictionary<string, string>>(d =>
                    d.ContainsKey("PORTLESS_URL") &&
                    d.ContainsKey("PORT") &&
                    d.ContainsKey("HOST"))), Times.Once);
        }
        finally
        {
            try { process?.Kill(); } catch { }
        }
    }

    [Fact]
    public async Task ExecuteAsync_ProcessStartThrows_Returns1()
    {
        var settings = new RunSettings { Name = "myapp" };
        var context = new CommandContext(["myapp", "node", "server.js"], new TestRemainingArguments(), "run", null);

        _proxyConnectionMock.Setup(x => x.IsProxyRunningAsync()).ReturnsAsync(true);
        _routeStoreMock.Setup(x => x.LoadRoutesAsync()).ReturnsAsync(Array.Empty<RouteInfo>());
        _portAllocatorMock.Setup(x => x.AssignFreePortAsync(It.IsAny<int>())).ReturnsAsync(4500);
        _frameworkDetectorMock.Setup(x => x.Detect(It.IsAny<string?>()))
            .Returns((DetectedFramework?)null);
        _processManagerMock.Setup(x => x.StartManagedProcess(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<Dictionary<string, string>>()))
            .Throws(new InvalidOperationException("Process start failed"));

        var result = await _command.ExecuteAsync(context, settings, CancellationToken.None);
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_PortAllocatorThrows_Returns1()
    {
        var settings = new RunSettings { Name = "myapp" };
        var context = new CommandContext(["myapp", "node", "server.js"], new TestRemainingArguments(), "run", null);

        _proxyConnectionMock.Setup(x => x.IsProxyRunningAsync()).ReturnsAsync(true);
        _routeStoreMock.Setup(x => x.LoadRoutesAsync()).ReturnsAsync(Array.Empty<RouteInfo>());
        _portAllocatorMock.Setup(x => x.AssignFreePortAsync(It.IsAny<int>()))
            .ThrowsAsync(new Exception("No free ports"));

        var result = await _command.ExecuteAsync(context, settings, CancellationToken.None);
        Assert.Equal(1, result);
    }
}
