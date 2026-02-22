namespace Portless.Core.Services;

/// <summary>
/// Service for managing secure file permissions for certificate storage.
/// </summary>
public interface ICertificatePermissionService
{
    /// <summary>
    /// Creates a directory with secure permissions (chmod 700 on Unix, ACL on Windows).
    /// </summary>
    /// <param name="path">The directory path to create.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when lacking create permissions.</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown when platform doesn't support secure permissions.</exception>
    Task CreateSecureDirectoryAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets secure file permissions (chmod 600 on Unix, ACL on Windows).
    /// </summary>
    /// <param name="path">The file path to secure.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when lacking permission modification rights.</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown when platform doesn't support secure permissions.</exception>
    Task SetSecureFilePermissionsAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies that a file has secure permissions.
    /// </summary>
    /// <param name="path">The file path to verify.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if the file has secure permissions; otherwise, false.</returns>
    Task<bool> VerifyFilePermissionsAsync(string path, CancellationToken cancellationToken = default);
}
