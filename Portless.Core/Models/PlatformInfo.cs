using System.Runtime.InteropServices;

namespace Portless.Core.Models;

/// <summary>
/// Information about the current platform.
/// </summary>
/// <param name="OSPlatform">The detected OS platform (Windows, macOS, Linux).</param>
/// <param name="LinuxDistro">The Linux distribution if applicable.</param>
/// <param name="IsAdmin">Whether the current process has administrator privileges.</param>
/// <param name="ElevationCommand">The command to elevate privileges (e.g., "sudo").</param>
public record PlatformInfo(
    OSPlatform OSPlatform,
    LinuxDistro? LinuxDistro,
    bool IsAdmin,
    string? ElevationCommand
);
