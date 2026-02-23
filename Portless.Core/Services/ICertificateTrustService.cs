using System.Security.Cryptography.X509Certificates;
using Portless.Core.Models;

namespace Portless.Core.Services;

/// <summary>
/// Service for managing certificate trust installation on Windows.
/// </summary>
public interface ICertificateTrustService
{
    /// <summary>
    /// Installs the Certificate Authority certificate to the Windows Root certificate store.
    /// </summary>
    /// <param name="certificate">The CA certificate to install.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A TrustInstallResult indicating success, already-installed status, or failure with details.
    /// Returns StoreAccessDenied=true if administrator privileges are required.
    /// </returns>
    /// <remarks>
    /// This method requires administrator privileges to install to the LocalMachine Root store.
    /// The operation is idempotent - installing an already-trusted certificate returns success.
    /// </remarks>
    Task<TrustInstallResult> InstallCertificateAuthorityAsync(X509Certificate2 certificate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the trust status of a certificate in the Windows certificate store.
    /// </summary>
    /// <param name="thumbprint">The SHA-256 thumbprint of the certificate to check.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// TrustStatus indicating the certificate's trust state:
    /// - Trusted: Certificate is installed in Root store and not expiring soon
    /// - NotTrusted: Certificate is not in Root store
    /// - ExpiringSoon: Certificate is trusted but expires within 30 days
    /// - Unknown: Status cannot be determined (non-Windows platform)
    /// </returns>
    /// <remarks>
    /// The method checks the LocalMachine Root store for certificate presence.
    /// Expiration warning is triggered 30 days before certificate expiration.
    /// </remarks>
    Task<TrustStatus> GetTrustStatusAsync(string thumbprint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uninstalls the Certificate Authority certificate from the Windows Root certificate store.
    /// </summary>
    /// <param name="thumbprint">The SHA-256 thumbprint of the certificate to remove.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// True if the certificate was removed or already not installed (idempotent).
    /// False if the operation failed.
    /// </returns>
    /// <remarks>
    /// This method requires administrator privileges to remove from the LocalMachine Root store.
    /// The operation is idempotent - removing a non-existent certificate returns true.
    /// </remarks>
    Task<bool> UninstallCertificateAuthorityAsync(string thumbprint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the current process has administrator privileges on Windows.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if running as administrator, false otherwise.</returns>
    /// <remarks>
    /// Uses WindowsPrincipal to check if the current user is in the Administrator role.
    /// Returns false on non-Windows platforms or if access checks fail.
    /// </remarks>
    Task<bool> IsAdministratorAsync(CancellationToken cancellationToken = default);
}
