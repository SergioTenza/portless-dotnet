using Portless.Core.Models;

namespace Portless.Core.Services;

/// <summary>
/// Checks whether the process associated with a route is still alive.
/// Consolidates process liveness checking logic used by multiple services.
/// </summary>
public interface IProcessLivenessChecker
{
    /// <summary>
    /// Checks if the process associated with the given route is still running.
    /// Handles PID recycling by comparing process start time with route creation time.
    /// </summary>
    bool IsAlive(RouteInfo route);
}
