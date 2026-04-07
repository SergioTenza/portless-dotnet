using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Portless.Core.Models;

namespace Portless.Core.Services;

public class ProcessLivenessChecker : IProcessLivenessChecker
{
    private readonly ILogger<ProcessLivenessChecker> _logger;

    public ProcessLivenessChecker(ILogger<ProcessLivenessChecker> logger)
    {
        _logger = logger;
    }

    public bool IsAlive(RouteInfo route)
    {
        if (route.Pid <= 0) return false;

        try
        {
            var process = Process.GetProcessById(route.Pid);
            if (process.HasExited) return false;

            // PID recycling protection: if the process started AFTER the route was created
            // (plus a 1-second buffer), it's likely a recycled PID
            if (process.StartTime > route.CreatedAt.AddSeconds(1)) return false;

            return true;
        }
        catch (ArgumentException)
        {
            // PID doesn't exist
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking liveness of PID {Pid}", route.Pid);
            return false;
        }
    }
}
