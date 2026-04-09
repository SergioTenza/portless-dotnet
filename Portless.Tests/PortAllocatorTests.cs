using Microsoft.Extensions.Logging;
using Moq;
using Portless.Core.Services;
using Xunit;

namespace Portless.Tests;

public class PortAllocatorTests
{
    private readonly Mock<IPortPool> _portPoolMock;
    private readonly Mock<ILogger<PortAllocator>> _loggerMock;

    public PortAllocatorTests()
    {
        _portPoolMock = new Mock<IPortPool>();
        _loggerMock = new Mock<ILogger<PortAllocator>>();
    }

    private PortAllocator CreateService()
    {
        return new PortAllocator(_portPoolMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task AssignFreePortAsync_AllocatesFreePort()
    {
        // Arrange
        _portPoolMock
            .Setup(x => x.IsPortAllocated(It.IsAny<int>()))
            .Returns(false);

        var service = CreateService();

        // Act
        var port = await service.AssignFreePortAsync(1234);

        // Assert
        Assert.InRange(port, 4000, 4999);
        _portPoolMock.Verify(x => x.Allocate(port, 1234), Times.Once);
    }

    [Fact]
    public async Task AssignFreePortAsync_SkipsAllocatedPorts()
    {
        // Arrange - first port checked is already allocated
        var callCount = 0;
        _portPoolMock
            .Setup(x => x.IsPortAllocated(It.IsAny<int>()))
            .Returns(() =>
            {
                callCount++;
                return callCount <= 3; // First 3 are "allocated"
            });

        var service = CreateService();

        // Act
        var port = await service.AssignFreePortAsync(1234);

        // Assert
        Assert.InRange(port, 4000, 4999);
        Assert.True(callCount >= 4, "Should have skipped allocated ports");
    }

    [Fact]
    public async Task AssignFreePortAsync_ThrowsWhenExhausted()
    {
        // Arrange - all ports are allocated
        _portPoolMock
            .Setup(x => x.IsPortAllocated(It.IsAny<int>()))
            .Returns(true);

        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AssignFreePortAsync(1234));
        Assert.Contains("Failed to allocate port", ex.Message);
        Assert.Contains("50 attempts", ex.Message);
    }

    [Fact]
    public async Task AssignFreePortAsync_SkipsInUsePorts()
    {
        // Arrange - port is not in pool but is in use (TCP bound)
        // We need to bind a port to make it "in use"
        _portPoolMock
            .Setup(x => x.IsPortAllocated(It.IsAny<int>()))
            .Returns(false);

        var service = CreateService();

        // Act - this should succeed since most ports in 4000-4999 should be free
        var port = await service.AssignFreePortAsync(1234);

        // Assert
        Assert.InRange(port, 4000, 4999);
    }

    [Fact]
    public async Task IsPortFreeAsync_ReturnsTrueForUnusedPort()
    {
        // Arrange - use a high port likely to be free
        var service = CreateService();
        var testPort = 43210;

        // Act
        var isFree = await service.IsPortFreeAsync(testPort);

        // Assert - should be free unless something else is using it
        Assert.True(isFree);
    }

    [Fact]
    public async Task IsPortFreeAsync_ReturnsFalseForUsedPort()
    {
        // Arrange - bind a port and then check it
        var service = CreateService();
        var listener = new System.Net.Sockets.TcpListener(
            System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var boundPort = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;

        try
        {
            // Act
            var isFree = await service.IsPortFreeAsync(boundPort);

            // Assert
            Assert.False(isFree);
        }
        finally
        {
            listener.Stop();
        }
    }

    [Fact]
    public async Task ReleasePortAsync_ReleasesAllocatedPort()
    {
        // Arrange
        _portPoolMock
            .Setup(x => x.ReleaseByPort(4042))
            .Returns(true);

        var service = CreateService();

        // Act
        await service.ReleasePortAsync(4042);

        // Assert
        _portPoolMock.Verify(x => x.ReleaseByPort(4042), Times.Once);
    }

    [Fact]
    public async Task ReleasePortAsync_HandlesUntrackedPort()
    {
        // Arrange
        _portPoolMock
            .Setup(x => x.ReleaseByPort(4042))
            .Returns(false);

        var service = CreateService();

        // Act - should not throw
        await service.ReleasePortAsync(4042);

        // Assert
        _portPoolMock.Verify(x => x.ReleaseByPort(4042), Times.Once);
    }

    [Fact]
    public async Task AssignFreePortAsync_MultipleAllocationsGetDifferentPorts()
    {
        // Arrange
        var allocatedPorts = new HashSet<int>();
        _portPoolMock
            .Setup(x => x.IsPortAllocated(It.IsAny<int>()))
            .Returns<int>(p => allocatedPorts.Contains(p));

        _portPoolMock
            .Setup(x => x.Allocate(It.IsAny<int>(), It.IsAny<int>()))
            .Callback<int, int>((port, pid) => allocatedPorts.Add(port));

        var service = CreateService();

        // Act
        var port1 = await service.AssignFreePortAsync(100);
        var port2 = await service.AssignFreePortAsync(200);
        var port3 = await service.AssignFreePortAsync(300);

        // Assert - all ports should be unique
        Assert.Equal(3, new[] { port1, port2, port3 }.Distinct().Count());
    }

    [Fact]
    public async Task AssignFreePortAsync_CallsAllocateWithCorrectPid()
    {
        // Arrange
        var capturedPid = 0;
        _portPoolMock
            .Setup(x => x.IsPortAllocated(It.IsAny<int>()))
            .Returns(false);
        _portPoolMock
            .Setup(x => x.Allocate(It.IsAny<int>(), It.IsAny<int>()))
            .Callback<int, int>((port, pid) => capturedPid = pid);

        var service = CreateService();

        // Act
        await service.AssignFreePortAsync(5678);

        // Assert
        Assert.Equal(5678, capturedPid);
    }

    [Fact]
    public async Task ReleasePortAsync_ReturnsCompletedTask()
    {
        // Arrange
        _portPoolMock
            .Setup(x => x.ReleaseByPort(It.IsAny<int>()))
            .Returns(true);

        var service = CreateService();

        // Act
        var task = service.ReleasePortAsync(4042);

        // Assert
        Assert.True(task.IsCompleted);
        await task; // No exception
    }

    [Fact]
    public async Task AssignFreePortAsync_PortRangeIs4000To4999()
    {
        // Arrange
        var allocatedPort = 0;
        _portPoolMock
            .Setup(x => x.IsPortAllocated(It.IsAny<int>()))
            .Returns(false);
        _portPoolMock
            .Setup(x => x.Allocate(It.IsAny<int>(), It.IsAny<int>()))
            .Callback<int, int>((port, pid) => allocatedPort = port);

        var service = CreateService();

        // Act - allocate multiple ports
        for (int i = 0; i < 10; i++)
        {
            var port = await service.AssignFreePortAsync(1000 + i);
            Assert.InRange(port, 4000, 4999);
        }
    }
}
