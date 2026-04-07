using Portless.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Portless.Tests;

public class TcpForwardingTests
{
    [Fact]
    public async Task StartListenerAsync_AddsToActiveListeners()
    {
        var logger = new Mock<ILogger<TcpForwardingService>>().Object;
        using var service = new TcpForwardingService(logger);

        // Use port 0 for OS-assigned port to avoid conflicts
        await service.StartListenerAsync("test-tcp", 0, "localhost", 6379);

        var listeners = service.GetActiveListeners();
        Assert.Single(listeners);
        Assert.True(listeners.ContainsKey("test-tcp"));

        await service.StopListenerAsync("test-tcp");
    }

    [Fact]
    public async Task StopListenerAsync_RemovesFromActiveListeners()
    {
        var logger = new Mock<ILogger<TcpForwardingService>>().Object;
        using var service = new TcpForwardingService(logger);

        await service.StartListenerAsync("test-tcp-2", 0, "localhost", 5432);
        await service.StopListenerAsync("test-tcp-2");

        var listeners = service.GetActiveListeners();
        Assert.Empty(listeners);
    }

    [Fact]
    public async Task StartListenerAsync_DuplicateName_DoesNotThrow()
    {
        var logger = new Mock<ILogger<TcpForwardingService>>().Object;
        using var service = new TcpForwardingService(logger);

        await service.StartListenerAsync("dup-tcp", 0, "localhost", 6379);
        // Should not throw
        await service.StartListenerAsync("dup-tcp", 0, "localhost", 6379);

        var listeners = service.GetActiveListeners();
        Assert.Single(listeners);

        await service.StopListenerAsync("dup-tcp");
    }
}
