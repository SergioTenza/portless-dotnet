using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Portless.Core.Models;

namespace Portless.Core.Services;

/// <summary>
/// Implementation of certificate generation service using .NET native APIs.
/// </summary>
public class CertificateService : ICertificateService
{
    private readonly ILogger<CertificateService> _logger;
    private readonly string _stateDirectory;

    public CertificateService(ILogger<CertificateService> logger)
    {
        _logger = logger;
        _stateDirectory = StateDirectoryProvider.GetStateDirectory();
    }

    /// <inheritdoc/>
    public async Task<X509Certificate2> GenerateCertificateAuthorityAsync(
        CertificateGenerationOptions options,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            _logger.LogInformation(
                "Generating Certificate Authority with {KeySize}-bit RSA key, {Years}-year validity",
                options.CaKeySize,
                options.ValidityYears);

            using var rsa = RSA.Create(options.CaKeySize);

            var request = new CertificateRequest(
                new X500DistinguishedName($"CN={options.SubjectName}"),
                rsa,
                options.HashAlgorithm,
                RSASignaturePadding.Pkcs1
            );

            // Mark as Certificate Authority
            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(
                    certificateAuthority: true,
                    hasPathLengthConstraint: false,
                    pathLengthConstraint: 0,
                    critical: true
                )
            );

            // Add key usage for CA signing
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
                    critical: true
                )
            );

            // Add Subject Key Identifier
            request.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension(request.PublicKey, false)
            );

            // Calculate validity period
            var notBefore = DateTimeOffset.UtcNow.AddDays(-1); // Handle clock skew
            var notAfter = DateTimeOffset.UtcNow.AddDays(options.ValidityYears * 365);

            var certificate = request.CreateSelfSigned(notBefore, notAfter);

            // Export and reload with exportable private key
            byte[] pfxBytes = certificate.Export(X509ContentType.Pkcs12, "");
            var exportable = X509CertificateLoader.LoadPkcs12(pfxBytes, "", X509KeyStorageFlags.Exportable);

            _logger.LogInformation(
                "Certificate Authority generated successfully. Thumbprint: {Thumbprint}, Expires: {Expires}",
                exportable.GetCertHashString(HashAlgorithmName.SHA256),
                exportable.NotAfter.ToString("yyyy-MM-dd"));

            return exportable;
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<X509Certificate2> GenerateWildcardCertificateAsync(
        X509Certificate2 caCertificate,
        CertificateGenerationOptions options,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            if (!caCertificate.HasPrivateKey)
            {
                throw new ArgumentException(
                    "CA certificate must have a private key to sign server certificates",
                    nameof(caCertificate));
            }

            _logger.LogInformation(
                "Generating wildcard certificate for *.localhost with {KeySize}-bit RSA key, {Years}-year validity",
                options.ServerKeySize,
                options.ValidityYears);

            using var rsa = RSA.Create(options.ServerKeySize);

            var request = new CertificateRequest(
                new X500DistinguishedName("CN=*.localhost"),
                rsa,
                options.HashAlgorithm,
                RSASignaturePadding.Pkcs1
            );

            // Add SAN for DNS and IP addresses
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName("localhost");
            sanBuilder.AddDnsName("*.localhost");
            sanBuilder.AddIpAddress(IPAddress.Loopback);      // 127.0.0.1
            sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);  // ::1
            request.CertificateExtensions.Add(sanBuilder.Build());

            // Mark as end-entity (not CA)
            request.CertificateExtensions.Add(
                new X509BasicConstraintsExtension(
                    certificateAuthority: false,
                    hasPathLengthConstraint: false,
                    pathLengthConstraint: 0,
                    critical: true
                )
            );

            // Add Extended Key Usage for Server Authentication
            request.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(
                    new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, // Server Auth
                    critical: false
                )
            );

            // Add Key Usage for digital signature and key encipherment
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                    critical: false
                )
            );

            // Calculate validity period
            var notBefore = DateTimeOffset.UtcNow.AddDays(-1); // Handle clock skew
            var notAfter = DateTimeOffset.UtcNow.AddDays(options.ValidityYears * 365);

            // Load CA private key for signing
            using var caRsa = caCertificate.GetRSAPrivateKey();
            if (caRsa == null)
            {
                throw new InvalidOperationException("CA certificate has no private key");
            }

            // Generate random serial number
            var serialNumber = new byte[12];
            RandomNumberGenerator.Fill(serialNumber);
            serialNumber[0] &= 0x7F; // Ensure positive

            // Create certificate signed by CA
            var certificate = request.Create(caCertificate, notBefore, notAfter, serialNumber);

            // Export and reload with exportable private key
            byte[] pfxBytes = certificate.Export(X509ContentType.Pkcs12, "");
            var exportable = X509CertificateLoader.LoadPkcs12(pfxBytes, "", X509KeyStorageFlags.Exportable);

            _logger.LogInformation(
                "Wildcard certificate generated successfully. Thumbprint: {Thumbprint}, Expires: {Expires}",
                exportable.GetCertHashString(HashAlgorithmName.SHA256),
                exportable.NotAfter.ToString("yyyy-MM-dd"));

            return exportable;
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<X509Certificate2?> LoadCertificateAuthorityAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var caPath = Path.Combine(_stateDirectory, "ca.pfx");

            if (!File.Exists(caPath))
            {
                _logger.LogDebug("CA certificate not found at {Path}", caPath);
                return null;
            }

            try
            {
                byte[] pfxBytes = File.ReadAllBytes(caPath);
                var cert = X509CertificateLoader.LoadPkcs12(pfxBytes, "", X509KeyStorageFlags.Exportable);

                _logger.LogDebug(
                    "CA certificate loaded: {Thumbprint}",
                    cert.GetCertHashString(HashAlgorithmName.SHA256));

                return cert;
            }
            catch (CryptographicException ex)
            {
                _logger.LogWarning(ex, "Failed to load CA certificate from {Path}", caPath);
                return null;
            }
        }, cancellationToken);
    }
}
