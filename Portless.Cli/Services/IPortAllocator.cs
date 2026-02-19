using System.Net;

namespace Portless.Cli.Services;

public interface IPortAllocator
{
    Task<int> AssignFreePortAsync();
    Task<bool> IsPortFreeAsync(int port);
    Task ReleasePortAsync(int port); // Placeholder for future port pooling
}
