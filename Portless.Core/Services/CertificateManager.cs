using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Portless.Core.Models;

namespace Portless.Core.Services;

/// <summary>
/// Orchestrates certificate generation, storage, validation, and lifecycle management.
/// </summary>
public class CertificateManager : ICertificateManager
{
    private readonly ICertificateService _certificateService;
    private readonly ICertificateStorageService _storageService;
    private readonly ICertificatePermissionService _permissionService;
    private readonly ILogger<CertificateManager> _logger;
    private readonly CertificateGenerationOptions _options;

    public CertificateManager(
        ICertificateService certificateService,
        ICertificateStorageService storageService,
        ICertificatePermissionService permissionService,
        ILogger<CertificateManager> logger)
    {
        _certificateService = certificateService;
        _storageService = storageService;
        _permissionService = permissionService;
        _logger = logger;
        _options = new CertificateGenerationOptions
        {
            SubjectName = "Portless Local Development CA",
            ValidityYears = 5,
            CaKeySize = 4096,
            ServerKeySize = 2048,
            HashAlgorithm = System.Security.Cryptography.HashAlgorithmName.SHA256
        };
    }

    /// <inheritdoc/>
    public async Task<CertificateStatus> EnsureCertificatesAsync(bool forceRegeneration = false, CancellationToken cancellationToken = default)
    {
        // If force regeneration is requested, regenerate and return
        if (forceRegeneration)
        {
            _logger.LogInformation("Force regeneration requested - regenerating certificates");
            await RegenerateCertificatesAsync(cancellationToken);
            return new CertificateStatus(
                IsValid: true,
                IsExpired: false,
                IsExpiringSoon: false,
                IsCorrupted: false,
                NeedsRegeneration: false,
                Message: "Certificates regenerated successfully",
                ExpiresAt: DateTimeOffset.UtcNow.AddYears(_options.ValidityYears),
                Thumbprint: null
            );
        }

        // Check if certificate files exist
        bool filesExist = await _storageService.CertificateFilesExistAsync(cancellationToken);

        if (!filesExist)
        {
            // First-time generation - prompt user via logger (per user decision "Preguntar al usuario la primera vez")
            _logger.LogInformation("=================================================================");
            _logger.LogInformation("Certificate files not found. Generating certificates for first-time use.");
            _logger.LogInformation("A Certificate Authority (CA) and wildcard certificate for *.localhost will be created.");
            _logger.LogInformation("Certificates will be valid for {Years} years.", _options.ValidityYears);
            _logger.LogInformation("=================================================================");

            // Generate CA
            var newCaCert = await _certificateService.GenerateCertificateAuthorityAsync(_options, cancellationToken);
            await _storageService.SaveCertificateAuthorityAsync(newCaCert, cancellationToken);

            // Generate server certificate
            var serverCert = await _certificateService.GenerateWildcardCertificateAsync(newCaCert, _options, cancellationToken);
            await _storageService.SaveServerCertificateAsync(serverCert, cancellationToken);

            // Create and save metadata
            var metadata = CreateCertificateMetadata(serverCert, newCaCert);
            await _storageService.SaveCertificateMetadataAsync(metadata, cancellationToken);

            // Verify file permissions
            var stateDir = StateDirectoryProvider.GetStateDirectory();
            bool permissionsSecure = await _permissionService.VerifyFilePermissionsAsync(stateDir, cancellationToken);
            if (!permissionsSecure)
            {
                _logger.LogWarning("WARNING: Certificate file permissions may be insecure. Other users might be able to read these files.");
            }

            return new CertificateStatus(
                IsValid: true,
                IsExpired: false,
                IsExpiringSoon: false,
                IsCorrupted: false,
                NeedsRegeneration: false,
                Message: "Certificates generated for first-time use",
                ExpiresAt: serverCert.NotAfter,
                Thumbprint: serverCert.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256)
            );
        }

        // Files exist - load and validate CA certificate
        X509Certificate2? caCert = null;
        try
        {
            caCert = await _storageService.LoadCertificateAuthorityAsync(cancellationToken);

            if (caCert == null)
            {
                _logger.LogWarning("Failed to load CA certificate - regenerating");
                await RegenerateCertificatesAsync(cancellationToken);
                return await CreateStatusFromGeneratedCerts(cancellationToken);
            }

            // Validate certificate
            var (isValid, reason) = ValidateCertificate(caCert);

            // Verify file permissions on first load (only once per session)
            bool permissionsSecure = await _permissionService.VerifyFilePermissionsAsync(
                StateDirectoryProvider.GetStateDirectory(), cancellationToken);
            if (!permissionsSecure)
            {
                _logger.LogWarning("WARNING: Certificate file permissions may be insecure. Other users might be able to read these files.");
            }

            if (!isValid)
            {
                _logger.LogWarning("CA certificate validation failed: {Reason} - regenerating", reason);
                await RegenerateCertificatesAsync(cancellationToken);
                return await CreateStatusFromGeneratedCerts(cancellationToken);
            }

            // Certificate is valid - load server certificate and check status
            var serverCert = await _storageService.LoadServerCertificateAsync(cancellationToken);
            if (serverCert == null)
            {
                _logger.LogWarning("Failed to load server certificate - regenerating");
                await RegenerateCertificatesAsync(cancellationToken);
                return await CreateStatusFromGeneratedCerts(cancellationToken);
            }

            return CreateStatusFromCertificate(serverCert);
        }
        catch (CryptographicException ex)
        {
            // Corrupted PFX file
            _logger.LogError(ex, "CA certificate file is corrupted - regenerating");
            await RegenerateCertificatesAsync(cancellationToken);
            return await CreateStatusFromGeneratedCerts(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<X509Certificate2> GetCertificateAuthorityAsync(CancellationToken cancellationToken = default)
    {
        // Ensure certificates exist
        await EnsureCertificatesAsync(forceRegeneration: false, cancellationToken);

        // Load CA certificate
        var caCert = await _storageService.LoadCertificateAuthorityAsync(cancellationToken);
        if (caCert == null)
        {
            throw new InvalidOperationException("Failed to load CA certificate after ensuring it exists");
        }

        return caCert;
    }

    /// <inheritdoc/>
    public async Task<X509Certificate2> GetServerCertificateAsync(CancellationToken cancellationToken = default)
    {
        // Ensure certificates exist
        await EnsureCertificatesAsync(forceRegeneration: false, cancellationToken);

        // Load server certificate
        var serverCert = await _storageService.LoadServerCertificateAsync(cancellationToken);
        if (serverCert == null)
        {
            throw new InvalidOperationException("Failed to load server certificate after ensuring it exists");
        }

        return serverCert;
    }

    /// <inheritdoc/>
    public async Task<CertificateInfo?> GetCertificateStatusAsync(CancellationToken cancellationToken = default)
    {
        // Try to load metadata first
        var metadata = await _storageService.LoadCertificateMetadataAsync(cancellationToken);

        if (metadata != null)
        {
            return metadata;
        }

        // Metadata doesn't exist - load CA certificate and create status from it
        var caCert = await _storageService.LoadCertificateAuthorityAsync(cancellationToken);
        if (caCert == null)
        {
            return null;
        }

        // Return null indicating no certificate info available
        return null;
    }

    /// <inheritdoc/>
    public async Task RegenerateCertificatesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Regenerating certificates...");

        // Generate new CA
        var caCert = await _certificateService.GenerateCertificateAuthorityAsync(_options, cancellationToken);
        await _storageService.SaveCertificateAuthorityAsync(caCert, cancellationToken);

        // Generate new server certificate
        var serverCert = await _certificateService.GenerateWildcardCertificateAsync(caCert, _options, cancellationToken);
        await _storageService.SaveServerCertificateAsync(serverCert, cancellationToken);

        // Create and save metadata
        var metadata = CreateCertificateMetadata(serverCert, caCert);
        await _storageService.SaveCertificateMetadataAsync(metadata, cancellationToken);

        _logger.LogInformation("Certificates regenerated successfully. New thumbprint: {Thumbprint}",
            serverCert.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256));
    }

    /// <summary>
    /// Validates a certificate to ensure it's properly formed and not expired.
    /// </summary>
    private (bool IsValid, string? Reason) ValidateCertificate(X509Certificate2? cert)
    {
        if (cert == null)
        {
            return (false, "Certificate is null");
        }

        if (!cert.HasPrivateKey)
        {
            return (false, "Certificate missing private key");
        }

        var now = DateTimeOffset.UtcNow;
        if (now < cert.NotBefore)
        {
            return (false, $"Certificate not valid until {cert.NotBefore:yyyy-MM-dd}");
        }

        if (now > cert.NotAfter)
        {
            return (false, $"Certificate expired on {cert.NotAfter:yyyy-MM-dd}");
        }

        return (true, null);
    }

    /// <summary>
    /// Creates a CertificateStatus from an existing certificate.
    /// </summary>
    private CertificateStatus CreateStatusFromCertificate(X509Certificate2 cert)
    {
        var now = DateTimeOffset.UtcNow;
        bool isExpired = now > cert.NotAfter;
        bool isExpiringSoon = now > cert.NotAfter.AddDays(-30); // Within 30 days
        string thumbprint = cert.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256);

        string message;
        if (isExpired)
        {
            message = $"Certificate expired on {cert.NotAfter:yyyy-MM-dd}";
        }
        else if (isExpiringSoon)
        {
            message = $"Certificate expires soon ({cert.NotAfter:yyyy-MM-dd})";
        }
        else
        {
            message = $"Certificate valid until {cert.NotAfter:yyyy-MM-dd}";
        }

        return new CertificateStatus(
            IsValid: !isExpired,
            IsExpired: isExpired,
            IsExpiringSoon: isExpiringSoon,
            IsCorrupted: false,
            NeedsRegeneration: isExpired || isExpiringSoon,
            Message: message,
            ExpiresAt: cert.NotAfter,
            Thumbprint: thumbprint
        );
    }

    /// <summary>
    /// Creates CertificateStatus from newly generated certificates.
    /// </summary>
    private async Task<CertificateStatus> CreateStatusFromGeneratedCerts(CancellationToken cancellationToken)
    {
        var serverCert = await _storageService.LoadServerCertificateAsync(cancellationToken);
        if (serverCert == null)
        {
            throw new InvalidOperationException("Failed to load newly generated server certificate");
        }

        return new CertificateStatus(
            IsValid: true,
            IsExpired: false,
            IsExpiringSoon: false,
            IsCorrupted: false,
            NeedsRegeneration: false,
            Message: "Certificates regenerated successfully",
            ExpiresAt: serverCert.NotAfter,
            Thumbprint: serverCert.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256)
        );
    }

    /// <summary>
    /// Creates CertificateInfo metadata from server and CA certificates.
    /// </summary>
    private CertificateInfo CreateCertificateMetadata(X509Certificate2 serverCert, X509Certificate2 caCert)
    {
        var now = DateTimeOffset.UtcNow;

        return new CertificateInfo
        {
            Version = "1.0",
            Sha256Thumbprint = serverCert.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256),
            CreatedAt = now.ToString("o"), // ISO 8601
            ExpiresAt = serverCert.NotAfter.ToString("o"), // ISO 8601
            CaThumbprint = caCert.GetCertHashString(System.Security.Cryptography.HashAlgorithmName.SHA256),
            CreatedAtUnix = now.ToUnixTimeSeconds(),
            ExpiresAtUnix = new DateTimeOffset(serverCert.NotAfter).ToUnixTimeSeconds()
        };
    }
}
