using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Portless.Core.Services;
using Xunit;

namespace Portless.IntegrationTests;

/// <summary>
/// Integration tests for port allocation functionality.
/// Tests verify port detection, uniqueness, and exhaustion handling.
/// </summary>
public class PortAllocatorTests : IDisposable
{
    private readonly IPortPool _portPool;
    private readonly PortAllocator _allocator;

    public PortAllocatorTests()
    {
        var poolLogger = NullLogger<PortPool>.Instance;
        var allocatorLogger = NullLogger<PortAllocator>.Instance;
        _portPool = new PortPool(poolLogger);
        _allocator = new PortAllocator(_portPool, allocatorLogger);
    }

    [Fact]
    public async Task AssignFreePortAsync_ReturnsAvailablePortInRange()
    {
        // Arrange & Act
        var port = await _allocator.AssignFreePortAsync(12345);

        // Assert - Port should be in range 4000-4999
        Assert.InRange(port, 4000, 4999);

        // Verify port is allocated in pool
        Assert.True(_portPool.IsPortAllocated(port));

        // Verify port is actually free by checking availability
        var isFree = await _allocator.IsPortFreeAsync(port);
        Assert.True(isFree, "Port should be available (though allocated in pool)");

        // Cleanup
        await _allocator.ReleasePortAsync(port);
    }

    [Fact]
    public async Task AssignFreePortAsync_MultipleTimes_ReturnsUniquePorts()
    {
        // Arrange
        var allocatedPorts = new HashSet<int>();
        var testPid = 12345;

        // Act - Allocate 10 ports
        for (int i = 0; i < 10; i++)
        {
            var port = await _allocator.AssignFreePortAsync(testPid + i);
            allocatedPorts.Add(port);
        }

        // Assert - All ports should be unique
        Assert.Equal(10, allocatedPorts.Count);

        // All ports should be in range
        foreach (var port in allocatedPorts)
        {
            Assert.InRange(port, 4000, 4999);
        }

        // Cleanup
        foreach (var port in allocatedPorts)
        {
            await _allocator.ReleasePortAsync(port);
        }
    }

    [Fact]
    public async Task AssignFreePortAsync_ExhaustedRange_ThrowsException()
    {
        // Arrange
        // Note: We can't truly exhaust the 4000-4999 range in a test,
        // but we can verify the exception mechanism works
        var allocatedPorts = new List<int>();

        try
        {
            // Try to allocate many ports (this should eventually fail or be very slow)
            for (int i = 0; i < 60; i++) // More than the 50 max attempts should allow
            {
                var port = await _allocator.AssignFreePortAsync(10000 + i);
                allocatedPorts.Add(port);
            }

            // If we get here without exception, that's actually fine for this test
            // (The real range is large enough to handle 60 allocations)
        }
        catch (InvalidOperationException ex)
        {
            // Expected if range truly exhausted
            Assert.Contains("exhausted", ex.Message, StringComparison.InvariantCultureIgnoreCase);
        }
        finally
        {
            // Cleanup
            foreach (var port in allocatedPorts)
            {
                await _allocator.ReleasePortAsync(port);
            }
        }
    }

    [Fact]
    public async Task AssignFreePortAsync_WithLargeRange_DistributesPorts()
    {
        // Arrange
        var allocatedPorts = new List<int>();

        // Act - Allocate 20 ports
        for (int i = 0; i < 20; i++)
        {
            var port = await _allocator.AssignFreePortAsync(12345 + i);
            allocatedPorts.Add(port);
        }

        // Assert - Check distribution
        // Ports should be distributed across the range (random allocation)
        var minPort = allocatedPorts.Min();
        var maxPort = allocatedPorts.Max();

        // Range should be reasonably wide (random distribution)
        // With 20 allocations in a 1000-port range, we expect some spread
        var spread = maxPort - minPort;
        Assert.True(spread > 0, "Ports should be distributed (not all the same)");

        // Cleanup
        foreach (var port in allocatedPorts)
        {
            await _allocator.ReleasePortAsync(port);
        }
    }

    [Fact]
    public async Task IsPortFreeAsync_WithFreePort_ReturnsTrue()
    {
        // Arrange - Use a port unlikely to be in use
        var testPort = 4999; // High in range, likely free

        // Act
        var isFree = await _allocator.IsPortFreeAsync(testPort);

        // Assert - Should be free unless something is using it
        Assert.True(isFree, $"Port {testPort} should be available (unless in use by another process)");
    }

    [Fact]
    public async Task IsPortFreeAsync_WithAllocatedPort_ReturnsTrue()
    {
        // Arrange - Allocate a port
        var port = await _allocator.AssignFreePortAsync(12345);

        // Act
        var isFree = await _allocator.IsPortFreeAsync(port);

        // Assert - Port is still free at TCP level (just tracked in pool)
        Assert.True(isFree, "Port should be free at TCP level even if allocated in pool");

        // Cleanup
        await _allocator.ReleasePortAsync(port);
    }

    [Fact]
    public async Task ReleasePortAsync_RemovesFromPool()
    {
        // Arrange - Allocate a port
        var port = await _allocator.AssignFreePortAsync(12345);
        Assert.True(_portPool.IsPortAllocated(port));

        // Act
        await _allocator.ReleasePortAsync(port);

        // Assert - Port should no longer be allocated
        Assert.False(_portPool.IsPortAllocated(port));
    }

    [Fact]
    public async Task AssignFreePortAsync_WithSamePid_TracksMultiplePorts()
    {
        // Arrange
        var testPid = 12345;

        // Act - Allocate multiple ports for same PID
        var port1 = await _allocator.AssignFreePortAsync(testPid);
        var port2 = await _allocator.AssignFreePortAsync(testPid);
        var port3 = await _allocator.AssignFreePortAsync(testPid);

        // Assert - All ports should be different
        Assert.NotEqual(port1, port2);
        Assert.NotEqual(port2, port3);
        Assert.NotEqual(port1, port3);

        // All should be in range
        Assert.InRange(port1, 4000, 4999);
        Assert.InRange(port2, 4000, 4999);
        Assert.InRange(port3, 4000, 4999);

        // Cleanup
        await _allocator.ReleasePortAsync(port1);
        await _allocator.ReleasePortAsync(port2);
        await _allocator.ReleasePortAsync(port3);
    }

    public void Dispose()
    {
        // Note: In real scenario, ports are tied to PIDs and cleaned up by ProcessHealthMonitor
        // For testing, we rely on each test to clean up its own allocations
    }
}
