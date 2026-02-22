using System.Security.Cryptography;

namespace Portless.Core.Models;

/// <summary>
/// Configuration options for certificate generation.
/// </summary>
public class CertificateGenerationOptions
{
    /// <summary>
    /// Subject name for the Certificate Authority.
    /// Default: "Portless Local Development CA"
    /// </summary>
    public string SubjectName { get; set; } = "Portless Local Development CA";

    /// <summary>
    /// Validity period in years for both CA and server certificates.
    /// Default: 5 years
    /// </summary>
    public int ValidityYears { get; set; } = 5;

    /// <summary>
    /// RSA key size for the CA certificate in bits.
    /// Default: 4096 bits
    /// </summary>
    public int CaKeySize { get; set; } = 4096;

    /// <summary>
    /// RSA key size for the server certificate in bits.
    /// Default: 2048 bits
    /// </summary>
    public int ServerKeySize { get; set; } = 2048;

    /// <summary>
    /// Hash algorithm to use for certificate signing.
    /// Default: SHA256
    /// </summary>
    public HashAlgorithmName HashAlgorithm { get; set; } = HashAlgorithmName.SHA256;
}
