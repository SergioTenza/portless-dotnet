using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Portless.Core.Services;

/// <summary>
/// Enhanced port allocator with pooling integration and lifecycle management.
/// </summary>
public class PortAllocator : IPortAllocator
{
    private readonly IPortPool _portPool;
    private readonly ILogger<PortAllocator> _logger;
    private readonly Random _random = new();

    public PortAllocator(IPortPool portPool, ILogger<PortAllocator> logger)
    {
        _portPool = portPool;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<int> AssignFreePortAsync(int pid)
    {
        const int maxAttempts = 50;
        const int portRangeStart = 4000;
        const int portRangeEnd = 5000;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Generate random port in range
            var port = _random.Next(portRangeStart, portRangeEnd);

            // Check if port is already allocated in pool
            if (_portPool.IsPortAllocated(port))
            {
                _logger.LogTrace("Port {Port} already allocated, retrying...", port);
                continue;
            }

            // Check if port is free via TCP binding
            if (!await IsPortFreeAsync(port))
            {
                _logger.LogTrace("Port {Port} is in use, retrying...", port);
                continue;
            }

            // Allocate port to PID
            _portPool.Allocate(port, pid);
            _logger.LogInformation("Allocated port {Port} to PID {Pid}", port, pid);
            return port;
        }

        throw new InvalidOperationException(
            $"Failed to allocate port after {maxAttempts} attempts. " +
            $"Port range {portRangeStart}-{portRangeEnd - 1} may be exhausted.");
    }

    /// <inheritdoc/>
    public async Task<bool> IsPortFreeAsync(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true; // Port is free if we can bind to it
        }
        catch (SocketException)
        {
            return false; // Port is in use
        }
    }

    /// <inheritdoc/>
    public Task ReleasePortAsync(int port)
    {
        if (_portPool.ReleaseByPort(port))
        {
            _logger.LogInformation("Released port {Port}", port);
        }
        else
        {
            _logger.LogDebug("Port {Port} was not tracked, nothing to release", port);
        }

        return Task.CompletedTask;
    }
}
