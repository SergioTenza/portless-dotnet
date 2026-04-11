extern alias Cli;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Portless.Core.Services;
using Xunit;

using CliPortAllocator = Cli::Portless.Cli.Services.PortAllocator;

namespace Portless.Tests.CliServices;

public class CliPortAllocatorTests
{
    private readonly Mock<IPortPool> _portPoolMock;
    private readonly Core.Services.PortAllocator _coreAllocator;

    public CliPortAllocatorTests()
    {
        _portPoolMock = new Mock<IPortPool>();
        _coreAllocator = new Core.Services.PortAllocator(_portPoolMock.Object, NullLogger<Core.Services.PortAllocator>.Instance);
    }

    private CliPortAllocator CreateService() => new(_coreAllocator);

    [Fact]
    public async Task AssignFreePortAsync_DelegatesToCore()
    {
        _portPoolMock.Setup(x => x.IsPortAllocated(It.IsAny<int>()))
            .Returns(false);

        var service = CreateService();
        var port = await service.AssignFreePortAsync(1234);

        Assert.InRange(port, 4000, 4999);
        _portPoolMock.Verify(x => x.Allocate(port, 1234), Times.Once);
    }

    [Fact]
    public async Task IsPortFreeAsync_DelegatesToCore()
    {
        var service = CreateService();
        var isFree = await service.IsPortFreeAsync(43210);

        Assert.True(isFree);
    }

    [Fact]
    public async Task ReleasePortAsync_DelegatesToCore()
    {
        _portPoolMock.Setup(x => x.ReleaseByPort(4042))
            .Returns(true);

        var service = CreateService();
        await service.ReleasePortAsync(4042);

        _portPoolMock.Verify(x => x.ReleaseByPort(4042), Times.Once);
    }

    [Fact]
    public async Task AssignFreePortAsync_ThrowsWhenExhausted()
    {
        _portPoolMock.Setup(x => x.IsPortAllocated(It.IsAny<int>()))
            .Returns(true);

        var service = CreateService();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AssignFreePortAsync(1234));
        Assert.Contains("Failed to allocate port", ex.Message);
    }
}
