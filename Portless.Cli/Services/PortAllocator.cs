using Microsoft.Extensions.Logging;
using Portless.Core.Services;

namespace Portless.Cli.Services;

/// <summary>
/// CLI-facing PortAllocator that delegates to Core implementation.
/// </summary>
/// <remarks>
/// This wrapper resolves code duplication by delegating all port allocation
/// logic to the Core.PortAllocator, which has access to IPortPool for lifecycle management.
/// </remarks>
public class PortAllocator : Core.Services.IPortAllocator
{
    private readonly Core.Services.PortAllocator _coreAllocator;

    public PortAllocator(Core.Services.PortAllocator coreAllocator)
    {
        _coreAllocator = coreAllocator;
    }

    public Task<int> AssignFreePortAsync(int pid)
    {
        return _coreAllocator.AssignFreePortAsync(pid);
    }

    public Task<bool> IsPortFreeAsync(int port)
    {
        return _coreAllocator.IsPortFreeAsync(port);
    }

    public Task ReleasePortAsync(int port)
    {
        return _coreAllocator.ReleasePortAsync(port);
    }
}
