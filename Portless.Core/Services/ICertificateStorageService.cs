using System.Security.Cryptography.X509Certificates;
using Portless.Core.Models;

namespace Portless.Core.Services;

/// <summary>
/// Service for persisting and loading certificates to/from disk.
/// </summary>
public interface ICertificateStorageService
{
    /// <summary>
    /// Saves the Certificate Authority (CA) certificate to disk.
    /// </summary>
    /// <param name="certificate">The CA certificate to save.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when lacking write permissions.</exception>
    Task SaveCertificateAuthorityAsync(X509Certificate2 certificate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the server certificate to disk.
    /// </summary>
    /// <param name="certificate">The server certificate to save.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when lacking write permissions.</exception>
    Task SaveServerCertificateAsync(X509Certificate2 certificate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the Certificate Authority (CA) certificate from disk.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The CA certificate if found and valid; otherwise, null.</returns>
    Task<X509Certificate2?> LoadCertificateAuthorityAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the server certificate from disk.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The server certificate if found and valid; otherwise, null.</returns>
    Task<X509Certificate2?> LoadServerCertificateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves certificate metadata to JSON file.
    /// </summary>
    /// <param name="metadata">The certificate metadata to save.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when lacking write permissions.</exception>
    Task SaveCertificateMetadataAsync(CertificateInfo metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads certificate metadata from JSON file.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The certificate metadata if found; otherwise, null.</returns>
    Task<CertificateInfo?> LoadCertificateMetadataAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if all certificate files exist (CA, server cert, and metadata).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>True if all certificate files exist; otherwise, false.</returns>
    Task<bool> CertificateFilesExistAsync(CancellationToken cancellationToken = default);
}
