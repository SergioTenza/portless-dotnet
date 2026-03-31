using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Extensions.Logging;

namespace Portless.Core.Services;

/// <summary>
/// Cross-platform service for managing secure file permissions for certificate storage.
/// Implements chmod 700/600 on Unix and ACL restrictions on Windows.
/// </summary>
public class CertificatePermissionService : ICertificatePermissionService
{
    private readonly ILogger<CertificatePermissionService> _logger;

    /// <summary>
    /// Initializes a new instance of the CertificatePermissionService.
    /// </summary>
    /// <param name="logger">Logger for permission operations.</param>
    public CertificatePermissionService(ILogger<CertificatePermissionService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task CreateSecureDirectoryAsync(string path, CancellationToken cancellationToken = default)
    {
        await Task.Yield(); // Ensure async contract

        try
        {
            if (OperatingSystem.IsWindows())
            {
                CreateSecureDirectoryWindows(path);
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                CreateSecureDirectoryUnix(path);
            }
            else
            {
                // Fallback for other platforms
                _logger.LogWarning("Platform not supported for secure directory creation, using default permissions for: {Path}", path);
                Directory.CreateDirectory(path);
            }
        }
        catch (PlatformNotSupportedException ex)
        {
            _logger.LogError(ex, "Platform not supported for secure directory creation: {Path}", path);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create secure directory: {Path}", path);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SetSecureFilePermissionsAsync(string path, CancellationToken cancellationToken = default)
    {
        await Task.Yield(); // Ensure async contract

        try
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                SetSecureFilePermissionsUnix(path);
            }
            else if (OperatingSystem.IsWindows())
            {
                SetSecureFilePermissionsWindows(path);
            }
            else
            {
                // No-op for other platforms - log warning
                _logger.LogWarning("Platform not supported for secure file permissions, skipping for: {Path}", path);
            }
        }
        catch (PlatformNotSupportedException ex)
        {
            _logger.LogError(ex, "Platform not supported for secure file permissions: {Path}", path);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set secure file permissions: {Path}", path);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> VerifyFilePermissionsAsync(string path, CancellationToken cancellationToken = default)
    {
        await Task.Yield(); // Ensure async contract

        try
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                return VerifyFilePermissionsUnix(path);
            }
            else if (OperatingSystem.IsWindows())
            {
                return VerifyFilePermissionsWindows(path);
            }
            else
            {
                // Best effort for unsupported platforms
                _logger.LogWarning("Platform not supported for permission verification, returning true for: {Path}", path);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify file permissions: {Path}", path);
            return false;
        }
    }

    /// <summary>
    /// Creates a directory with secure permissions on Windows (ACL restricting to current user).
    /// </summary>
#pragma warning disable CA1416 // Platform-specific APIs are guarded by runtime OS checks in caller
    private void CreateSecureDirectoryWindows(string path)
    {
        var security = new DirectorySecurity();

        var currentUser = WindowsIdentity.GetCurrent().User;
        if (currentUser != null)
        {
            security.AddAccessRule(
                new FileSystemAccessRule(
                    currentUser,
                    FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow
                )
            );
        }

        // Remove inherited permissions and set explicit ACL
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

        // Use reflection or cast to avoid Unix overload resolution
        Directory.CreateDirectory(path);

        // Set the security on the existing directory
        new DirectoryInfo(path).SetAccessControl(security);

        _logger.LogDebug("Created secure directory with Windows ACL: {Path}", path);
    }
#pragma warning restore CA1416

    /// <summary>
    /// Creates a directory with secure permissions on Unix (chmod 700).
    /// </summary>
#pragma warning disable CA1416 // Unix-specific APIs are guarded by runtime OS checks in caller
    private void CreateSecureDirectoryUnix(string path)
    {
        // chmod 700 (rwx------)
        Directory.CreateDirectory(
            path,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
        );

        _logger.LogDebug("Created secure directory with chmod 700: {Path}", path);
    }
#pragma warning restore CA1416

    /// <summary>
    /// Sets secure file permissions on Unix (chmod 600).
    /// </summary>
#pragma warning disable CA1416 // Unix-specific APIs are guarded by runtime OS checks in caller
    private void SetSecureFilePermissionsUnix(string path)
    {
        // chmod 600 (rw-------)
        File.SetUnixFileMode(
            path,
            UnixFileMode.UserRead | UnixFileMode.UserWrite
        );

        _logger.LogDebug("Set secure file permissions with chmod 600: {Path}", path);
    }
#pragma warning restore CA1416

    /// <summary>
    /// Sets secure file permissions on Windows (ACL restricting to current user).
    /// </summary>
#pragma warning disable CA1416 // Platform-specific APIs are guarded by runtime OS checks in caller
    private void SetSecureFilePermissionsWindows(string path)
    {
        var security = new FileSecurity();

        var currentUser = WindowsIdentity.GetCurrent().User;
        if (currentUser != null)
        {
            security.AddAccessRule(
                new FileSystemAccessRule(
                    currentUser,
                    FileSystemRights.FullControl,
                    AccessControlType.Allow
                )
            );
        }

        // Remove inherited permissions and set explicit ACL
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

        new FileInfo(path).SetAccessControl(security);

        _logger.LogDebug("Set secure file permissions with Windows ACL: {Path}", path);
    }
#pragma warning restore CA1416

    /// <summary>
    /// Verifies file permissions on Unix.
    /// </summary>
#pragma warning disable CA1416 // Unix-specific APIs are guarded by runtime OS checks in caller
    private bool VerifyFilePermissionsUnix(string path)
    {
        try
        {
            var mode = File.GetUnixFileMode(path);

            // Check for UserRead and UserWrite (chmod 600 requires both)
            var hasUserRead = (mode & UnixFileMode.UserRead) != 0;
            var hasUserWrite = (mode & UnixFileMode.UserWrite) != 0;

            var isSecure = hasUserRead && hasUserWrite;

            if (!isSecure)
            {
                _logger.LogWarning(
                    "File permissions insecure on {Path}: UserRead={UserRead}, UserWrite={UserWrite}",
                    path, hasUserRead, hasUserWrite
                );
            }

            return isSecure;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Unix file mode for: {Path}", path);
            return false;
        }
    }
#pragma warning restore CA1416

    /// <summary>
    /// Verifies file permissions on Windows.
    /// </summary>
#pragma warning disable CA1416 // Platform-specific APIs are guarded by runtime OS checks in caller
    private bool VerifyFilePermissionsWindows(string path)
    {
        try
        {
            var security = new FileInfo(path).GetAccessControl();
            var currentUser = WindowsIdentity.GetCurrent().User;

            if (currentUser == null)
            {
                _logger.LogWarning("Could not determine current user for permission check: {Path}", path);
                return false;
            }

            // Check if current user has Read and Write access
            var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));
            var hasRead = false;
            var hasWrite = false;

            foreach (FileSystemAccessRule rule in rules)
            {
                if (rule.IdentityReference == currentUser)
                {
                    if (rule.AccessControlType == AccessControlType.Allow)
                    {
                        if ((rule.FileSystemRights & FileSystemRights.Read) != 0)
                        {
                            hasRead = true;
                        }
                        if ((rule.FileSystemRights & FileSystemRights.Write) != 0)
                        {
                            hasWrite = true;
                        }
                    }
                }
            }

            var isSecure = hasRead && hasWrite;

            if (!isSecure)
            {
                _logger.LogWarning(
                    "File permissions insecure on {Path}: HasRead={HasRead}, HasWrite={HasWrite}",
                    path, hasRead, hasWrite
                );
            }

            return isSecure;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Windows file ACL for: {Path}", path);
            return false;
        }
    }
#pragma warning restore CA1416
}
