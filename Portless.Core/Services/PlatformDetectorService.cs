// Portless.Core/Services/PlatformDetectorService.cs
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using Portless.Core.Models;

namespace Portless.Core.Services;

/// <summary>
/// Detects platform information and administrator privileges.
/// </summary>
public class PlatformDetectorService : IPlatformDetectorService
{
    private readonly ILogger<PlatformDetectorService> _logger;
    private PlatformInfo? _cachedInfo;

    public PlatformDetectorService(ILogger<PlatformDetectorService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public PlatformInfo GetPlatformInfo()
    {
        if (_cachedInfo != null)
        {
            return _cachedInfo;
        }

        var osPlatform = DetectOSPlatform();
        var linuxDistro = DetectLinuxDistro();
        var isAdmin = IsAdminUser();
        var elevationCommand = GetAdminElevationCommand();

        _cachedInfo = new PlatformInfo(osPlatform, linuxDistro, isAdmin, elevationCommand);
        return _cachedInfo;
    }

    /// <inheritdoc />
    public bool IsAdminUser()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows admin check
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        else
        {
            // Unix-like: check if running as root (UID == 0)
            try
            {
                var output = ProcessHelper.RunToString("id", "-u");
                return output == "0";
            }
            catch
            {
                _logger.LogWarning("Failed to check if running as root");
                return false;
            }
        }
    }

    /// <inheritdoc />
    public string GetAdminElevationCommand()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows uses UAC prompt, not command elevation
            return string.Empty;
        }
        else
        {
            // Unix-like uses sudo
            return "sudo";
        }
    }

    private OSPlatform DetectOSPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return OSPlatform.Windows;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return OSPlatform.OSX;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return OSPlatform.Linux;

        throw new PlatformNotSupportedException("Unsupported operating system");
    }

    private LinuxDistro? DetectLinuxDistro()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return null;

        try
        {
            var osRelease = File.ReadAllText("/etc/os-release");

            if (osRelease.Contains("ID=ubuntu") || osRelease.Contains("ID_LIKE=ubuntu"))
                return LinuxDistro.Ubuntu;
            if (osRelease.Contains("ID=debian") || osRelease.Contains("ID_LIKE=debian"))
                return LinuxDistro.Debian;
            if (osRelease.Contains("ID=fedora"))
                return LinuxDistro.Fedora;
            if (osRelease.Contains("ID=rhel") || osRelease.Contains("ID_LIKE=rhel"))
                return LinuxDistro.RHEL;
            if (osRelease.Contains("ID=arch"))
                return LinuxDistro.Arch;

            _logger.LogWarning("Unsupported Linux distribution detected");
            return LinuxDistro.Unknown;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect Linux distribution");
            return LinuxDistro.Unknown;
        }
    }
}
