namespace Portless.Core.Services;

/// <summary>
/// Port allocation interface with PID tracking for lifecycle management.
/// </summary>
public interface IPortAllocator
{
    /// <summary>
    /// Assigns a free port from the pool and tracks it with the specified PID.
    /// </summary>
    /// <param name="pid">The process ID that will receive this port allocation.</param>
    /// <returns>The allocated port number.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no free ports are available.</exception>
    Task<int> AssignFreePortAsync(int pid);

    /// <summary>
    /// Checks if a specific port is available for use.
    /// </summary>
    /// <param name="port">The port number to check.</param>
    /// <returns>True if the port is free; false if it's in use.</returns>
    Task<bool> IsPortFreeAsync(int port);

    /// <summary>
    /// Releases a port back to the pool for reuse.
    /// </summary>
    /// <param name="port">The port number to release.</param>
    Task ReleasePortAsync(int port);
}
