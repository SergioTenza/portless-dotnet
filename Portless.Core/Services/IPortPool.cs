namespace Portless.Core.Services;

/// <summary>
/// Manages port allocation tracking with PID mappings for lifecycle management.
/// </summary>
/// <remarks>
/// Ports are tracked by process ID (PID) to enable automatic cleanup when processes terminate.
/// This integration with RouteCleanupService ensures ports are properly released for reuse.
/// </remarks>
public interface IPortPool
{
    /// <summary>
    /// Allocates a port to a specific process ID.
    /// </summary>
    /// <param name="port">The port number to allocate.</param>
    /// <param name="pid">The process ID receiving this port allocation.</param>
    /// <exception cref="InvalidOperationException">Thrown when the port is already allocated.</exception>
    void Allocate(int port, int pid);

    /// <summary>
    /// Releases all ports associated with a specific process ID.
    /// </summary>
    /// <param name="pid">The process ID whose ports should be released.</param>
    /// <returns>The number of ports released.</returns>
    int ReleaseByPid(int pid);

    /// <summary>
    /// Releases a specific port back to the pool.
    /// </summary>
    /// <param name="port">The port number to release.</param>
    /// <returns>True if the port was allocated and released; false if it wasn't tracked.</returns>
    bool ReleaseByPort(int port);

    /// <summary>
    /// Checks if a port is currently allocated.
    /// </summary>
    /// <param name="port">The port number to check.</param>
    /// <returns>True if the port is allocated; false otherwise.</returns>
    bool IsPortAllocated(int port);
}
