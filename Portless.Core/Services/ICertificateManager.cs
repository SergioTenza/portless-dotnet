using System.Security.Cryptography.X509Certificates;
using Portless.Core.Models;

namespace Portless.Core.Services;

/// <summary>
/// High-level service for orchestrating certificate generation, validation, and lifecycle management.
/// </summary>
public interface ICertificateManager
{
    /// <summary>
    /// Ensures valid certificates exist, generating them if necessary.
    /// </summary>
    /// <param name="forceRegeneration">If true, regenerates certificates even if valid ones exist.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Status indicating certificate validity and any actions taken.</returns>
    Task<CertificateStatus> EnsureCertificatesAsync(bool forceRegeneration = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the Certificate Authority certificate, ensuring it exists first.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The CA certificate with private key.</returns>
    /// <exception cref="InvalidOperationException">Thrown when certificate cannot be loaded or generated.</exception>
    Task<X509Certificate2> GetCertificateAuthorityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the server certificate, ensuring it exists first.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The server certificate with private key.</returns>
    /// <exception cref="InvalidOperationException">Thrown when certificate cannot be loaded or generated.</exception>
    Task<X509Certificate2> GetServerCertificateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of certificates including expiration and validity information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Certificate status information, or null if certificates don't exist.</returns>
    Task<CertificateInfo?> GetCertificateStatusAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Regenerates both CA and server certificates.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="InvalidOperationException">Thrown when certificate generation fails.</exception>
    Task RegenerateCertificatesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Status information about certificate validity and health.
/// </summary>
/// <param name="IsValid">True if certificate is valid and not expired.</param>
/// <param name="IsExpired">True if certificate has expired.</param>
/// <param name="IsExpiringSoon">True if certificate expires within 30 days.</param>
/// <param name="IsCorrupted">True if certificate file is corrupted or missing private key.</param>
/// <param name="NeedsRegeneration">True if certificate should be regenerated.</param>
/// <param name="Message">Human-readable status message.</param>
/// <param name="ExpiresAt">Certificate expiration date (UTC).</param>
/// <param name="Thumbprint">Certificate SHA-256 thumbprint.</param>
public record CertificateStatus(
    bool IsValid,
    bool IsExpired,
    bool IsExpiringSoon,
    bool IsCorrupted,
    bool NeedsRegeneration,
    string? Message,
    DateTimeOffset? ExpiresAt,
    string? Thumbprint
);
