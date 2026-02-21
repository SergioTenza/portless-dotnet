using Microsoft.Extensions.Logging;

namespace Portless.Core.Services;

/// <summary>
/// Thread-safe port pool implementation tracking port-to-PID mappings for lifecycle management.
/// </summary>
public class PortPool : IPortPool
{
    private readonly Dictionary<int, int> _portToPid = new();
    private readonly ILogger<PortPool> _logger;
    private readonly object _lock = new();

    public PortPool(ILogger<PortPool> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public void Allocate(int port, int pid)
    {
        lock (_lock)
        {
            if (_portToPid.ContainsKey(port))
            {
                var existingPid = _portToPid[port];
                throw new InvalidOperationException(
                    $"Port {port} is already allocated to PID {existingPid}");
            }

            _portToPid[port] = pid;
            _logger.LogDebug("Allocated port {Port} to PID {Pid}", port, pid);
        }
    }

    /// <inheritdoc/>
    public int ReleaseByPid(int pid)
    {
        lock (_lock)
        {
            var portsToRelease = _portToPid
                .Where(kvp => kvp.Value == pid)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var port in portsToRelease)
            {
                _portToPid.Remove(port);
            }

            if (portsToRelease.Count > 0)
            {
                _logger.LogInformation(
                    "Released {Count} port(s) for PID {Pid}: {Ports}",
                    portsToRelease.Count,
                    pid,
                    string.Join(", ", portsToRelease.OrderBy(p => p)));
            }

            return portsToRelease.Count;
        }
    }

    /// <inheritdoc/>
    public bool ReleaseByPort(int port)
    {
        lock (_lock)
        {
            if (_portToPid.Remove(port, out var pid))
            {
                _logger.LogDebug("Released port {Port} (was allocated to PID {Pid})", port, pid);
                return true;
            }

            _logger.LogDebug("Attempted to release untracked port {Port}", port);
            return false;
        }
    }

    /// <inheritdoc/>
    public bool IsPortAllocated(int port)
    {
        lock (_lock)
        {
            return _portToPid.ContainsKey(port);
        }
    }
}
