using Portless.Core.Models;

namespace Portless.Core.Services;

/// <summary>
/// Service for detecting platform information and administrator privileges.
/// </summary>
public interface IPlatformDetectorService
{
    /// <summary>
    /// Gets information about the current platform.
    /// </summary>
    /// <returns>PlatformInfo containing OS, distribution, admin status, and elevation command.</returns>
    PlatformInfo GetPlatformInfo();

    /// <summary>
    /// Checks if the current process has administrator privileges.
    /// </summary>
    /// <returns>True if running with admin/root privileges, false otherwise.</returns>
    bool IsAdminUser();

    /// <summary>
    /// Gets the command to elevate privileges for the current platform.
    /// </summary>
    /// <returns>The elevation command (e.g., "sudo" for Unix-like, empty string for Windows admin).</returns>
    string GetAdminElevationCommand();
}
