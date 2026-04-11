using Microsoft.Extensions.Logging;
using Moq;
using Portless.Core.Services;
using Xunit;

namespace Portless.Tests;

public class PortPoolTests
{
    private readonly Mock<ILogger<PortPool>> _loggerMock;
    private readonly PortPool _portPool;

    public PortPoolTests()
    {
        _loggerMock = new Mock<ILogger<PortPool>>();
        _portPool = new PortPool(_loggerMock.Object);
    }

    [Fact]
    public void Allocate_SinglePort_Succeeds()
    {
        // Act
        _portPool.Allocate(4042, 1234);

        // Assert
        Assert.True(_portPool.IsPortAllocated(4042));
    }

    [Fact]
    public void Allocate_MultiplePorts_Succeeds()
    {
        // Act
        _portPool.Allocate(4042, 100);
        _portPool.Allocate(4043, 200);
        _portPool.Allocate(4044, 300);

        // Assert
        Assert.True(_portPool.IsPortAllocated(4042));
        Assert.True(_portPool.IsPortAllocated(4043));
        Assert.True(_portPool.IsPortAllocated(4044));
    }

    [Fact]
    public void Allocate_DuplicatePort_ThrowsInvalidOperationException()
    {
        // Arrange
        _portPool.Allocate(4042, 100);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => _portPool.Allocate(4042, 200));
        Assert.Contains("4042", ex.Message);
        Assert.Contains("already allocated", ex.Message);
    }

    [Fact]
    public void IsPortAllocated_ReturnsFalseForUnallocatedPort()
    {
        // Act
        var result = _portPool.IsPortAllocated(4042);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ReleaseByPort_ReturnsTrue_WhenPortWasAllocated()
    {
        // Arrange
        _portPool.Allocate(4042, 100);

        // Act
        var result = _portPool.ReleaseByPort(4042);

        // Assert
        Assert.True(result);
        Assert.False(_portPool.IsPortAllocated(4042));
    }

    [Fact]
    public void ReleaseByPort_ReturnsFalse_WhenPortNotAllocated()
    {
        // Act
        var result = _portPool.ReleaseByPort(4042);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ReleaseByPort_PortedCanBeReallocatedAfterRelease()
    {
        // Arrange
        _portPool.Allocate(4042, 100);
        _portPool.ReleaseByPort(4042);

        // Act - should not throw since port was released
        _portPool.Allocate(4042, 200);

        // Assert
        Assert.True(_portPool.IsPortAllocated(4042));
    }

    [Fact]
    public void ReleaseByPid_ReleasesAllPortsForProcess()
    {
        // Arrange
        _portPool.Allocate(4042, 100);
        _portPool.Allocate(4043, 100);
        _portPool.Allocate(4044, 200);

        // Act
        var count = _portPool.ReleaseByPid(100);

        // Assert
        Assert.Equal(2, count);
        Assert.False(_portPool.IsPortAllocated(4042));
        Assert.False(_portPool.IsPortAllocated(4043));
        Assert.True(_portPool.IsPortAllocated(4044)); // Different PID, still allocated
    }

    [Fact]
    public void ReleaseByPid_ReturnsZero_WhenPidHasNoPorts()
    {
        // Act
        var count = _portPool.ReleaseByPid(999);

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void Allocate_SamePortDifferentPid_AfterReleaseByPid_Works()
    {
        // Arrange
        _portPool.Allocate(4042, 100);
        _portPool.ReleaseByPid(100);

        // Act - should not throw
        _portPool.Allocate(4042, 200);

        // Assert
        Assert.True(_portPool.IsPortAllocated(4042));
    }

    [Fact]
    public void IsPortAllocated_ReturnsFalseAfterReleaseByPid()
    {
        // Arrange
        _portPool.Allocate(4042, 100);
        _portPool.Allocate(4043, 100);

        // Act
        _portPool.ReleaseByPid(100);

        // Assert
        Assert.False(_portPool.IsPortAllocated(4042));
        Assert.False(_portPool.IsPortAllocated(4043));
    }

    [Fact]
    public void Allocate_ManyPorts_TracksAll()
    {
        // Arrange & Act
        for (int i = 5000; i < 5100; i++)
        {
            _portPool.Allocate(i, 1000 + i);
        }

        // Assert
        for (int i = 5000; i < 5100; i++)
        {
            Assert.True(_portPool.IsPortAllocated(i));
        }

        // Unallocated port should still be false
        Assert.False(_portPool.IsPortAllocated(4999));
    }
}
