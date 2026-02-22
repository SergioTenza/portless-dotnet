using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Portless.Core.Models;

namespace Portless.Core.Services;

/// <summary>
/// Service for generating and managing X.509 certificates for local development.
/// </summary>
public interface ICertificateService
{
    /// <summary>
    /// Generates a self-signed Certificate Authority (CA) certificate.
    /// </summary>
    /// <param name="options">Configuration options for certificate generation.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A self-signed CA certificate with exportable private key.</returns>
    Task<X509Certificate2> GenerateCertificateAuthorityAsync(
        CertificateGenerationOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a wildcard server certificate signed by the specified CA.
    /// </summary>
    /// <param name="caCertificate">The CA certificate used to sign the server certificate.</param>
    /// <param name="options">Configuration options for certificate generation.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A wildcard server certificate for *.localhost with exportable private key.</returns>
    /// <exception cref="ArgumentException">Thrown when the CA certificate lacks a private key.</exception>
    Task<X509Certificate2> GenerateWildcardCertificateAsync(
        X509Certificate2 caCertificate,
        CertificateGenerationOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads an existing Certificate Authority from disk.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The CA certificate if found and valid; otherwise, null.</returns>
    Task<X509Certificate2?> LoadCertificateAuthorityAsync(CancellationToken cancellationToken = default);
}
