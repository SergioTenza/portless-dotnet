using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Portless.Core.Models;

namespace Portless.Core.Services;

/// <summary>
/// Implementation of certificate storage service for persisting certificates to disk.
/// </summary>
public class CertificateStorageService : ICertificateStorageService
{
    private readonly ILogger<CertificateStorageService> _logger;
    private readonly ICertificatePermissionService _permissionService;
    private readonly string _stateDirectory;
    private readonly string _caCertificatePath;
    private readonly string _serverCertificatePath;
    private readonly string _metadataPath;

    public CertificateStorageService(
        ICertificatePermissionService permissionService,
        ILogger<CertificateStorageService> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
        _stateDirectory = StateDirectoryProvider.GetStateDirectory();
        _caCertificatePath = Path.Combine(_stateDirectory, "ca.pfx");
        _serverCertificatePath = Path.Combine(_stateDirectory, "cert.pfx");
        _metadataPath = Path.Combine(_stateDirectory, "cert-info.json");
    }

    /// <inheritdoc/>
    public async Task SaveCertificateAuthorityAsync(X509Certificate2 certificate, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _logger.LogInformation("Saving Certificate Authority to {Path}", _caCertificatePath);

            // Ensure state directory exists with secure permissions
            _permissionService.CreateSecureDirectoryAsync(_stateDirectory, cancellationToken).Wait(cancellationToken);

            // Export certificate without password (per user context decision)
            byte[] pfxBytes = certificate.Export(X509ContentType.Pkcs12, "");

            // Write PFX file
            File.WriteAllBytes(_caCertificatePath, pfxBytes);

            // Set secure file permissions
            _permissionService.SetSecureFilePermissionsAsync(_caCertificatePath, cancellationToken).Wait(cancellationToken);

            _logger.LogInformation("Certificate Authority saved successfully. Thumbprint: {Thumbprint}", certificate.GetCertHashString(HashAlgorithmName.SHA256));
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SaveServerCertificateAsync(X509Certificate2 certificate, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _logger.LogInformation("Saving server certificate to {Path}", _serverCertificatePath);

            // Ensure state directory exists with secure permissions
            _permissionService.CreateSecureDirectoryAsync(_stateDirectory, cancellationToken).Wait(cancellationToken);

            // Export certificate without password
            byte[] pfxBytes = certificate.Export(X509ContentType.Pkcs12, "");

            // Write PFX file
            File.WriteAllBytes(_serverCertificatePath, pfxBytes);

            // Set secure file permissions
            _permissionService.SetSecureFilePermissionsAsync(_serverCertificatePath, cancellationToken).Wait(cancellationToken);

            _logger.LogInformation("Server certificate saved successfully. Thumbprint: {Thumbprint}", certificate.GetCertHashString(HashAlgorithmName.SHA256));
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<X509Certificate2?> LoadCertificateAuthorityAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(_caCertificatePath))
            {
                _logger.LogDebug("CA certificate file not found at {Path}", _caCertificatePath);
                return null;
            }

            try
            {
                _logger.LogDebug("Loading CA certificate from {Path}", _caCertificatePath);

                // Read PFX file and load without password
                byte[] pfxBytes = File.ReadAllBytes(_caCertificatePath);
                var cert = X509CertificateLoader.LoadPkcs12(pfxBytes, "", X509KeyStorageFlags.Exportable);

                // Verify private key is present
                if (!cert.HasPrivateKey)
                {
                    _logger.LogWarning("CA certificate at {Path} is missing private key", _caCertificatePath);
                    return null;
                }

                _logger.LogDebug("CA certificate loaded successfully. Thumbprint: {Thumbprint}", cert.GetCertHashString(HashAlgorithmName.SHA256));
                return cert;
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "Failed to load CA certificate from {Path} - file may be corrupted", _caCertificatePath);
                return null;
            }
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<X509Certificate2?> LoadServerCertificateAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(_serverCertificatePath))
            {
                _logger.LogDebug("Server certificate file not found at {Path}", _serverCertificatePath);
                return null;
            }

            try
            {
                _logger.LogDebug("Loading server certificate from {Path}", _serverCertificatePath);

                // Read PFX file and load without password
                byte[] pfxBytes = File.ReadAllBytes(_serverCertificatePath);
                var cert = X509CertificateLoader.LoadPkcs12(pfxBytes, "", X509KeyStorageFlags.Exportable);

                // Verify private key is present
                if (!cert.HasPrivateKey)
                {
                    _logger.LogWarning("Server certificate at {Path} is missing private key", _serverCertificatePath);
                    return null;
                }

                _logger.LogDebug("Server certificate loaded successfully. Thumbprint: {Thumbprint}", cert.GetCertHashString(HashAlgorithmName.SHA256));
                return cert;
            }
            catch (CryptographicException ex)
            {
                _logger.LogError(ex, "Failed to load server certificate from {Path} - file may be corrupted", _serverCertificatePath);
                return null;
            }
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SaveCertificateMetadataAsync(CertificateInfo metadata, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            _logger.LogInformation("Saving certificate metadata to {Path}", _metadataPath);

            // Ensure state directory exists with secure permissions
            _permissionService.CreateSecureDirectoryAsync(_stateDirectory, cancellationToken).Wait(cancellationToken);

            // Serialize metadata to JSON with indentation for readability
            var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Write JSON file
            File.WriteAllText(_metadataPath, json);

            // Set secure file permissions
            _permissionService.SetSecureFilePermissionsAsync(_metadataPath, cancellationToken).Wait(cancellationToken);

            _logger.LogDebug("Certificate metadata saved successfully");
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<CertificateInfo?> LoadCertificateMetadataAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            if (!File.Exists(_metadataPath))
            {
                _logger.LogDebug("Certificate metadata file not found at {Path}", _metadataPath);
                return null;
            }

            try
            {
                _logger.LogDebug("Loading certificate metadata from {Path}", _metadataPath);

                // Read and deserialize JSON
                var json = File.ReadAllText(_metadataPath);
                var metadata = JsonSerializer.Deserialize<CertificateInfo>(json);

                if (metadata == null)
                {
                    _logger.LogWarning("Failed to deserialize certificate metadata from {Path}", _metadataPath);
                    return null;
                }

                _logger.LogDebug("Certificate metadata loaded successfully. SHA256: {Thumbprint}", metadata.Sha256Thumbprint);
                return metadata;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse certificate metadata from {Path}", _metadataPath);
                return null;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Failed to read certificate metadata from {Path}", _metadataPath);
                return null;
            }
        }, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> CertificateFilesExistAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            bool caExists = File.Exists(_caCertificatePath);
            bool certExists = File.Exists(_serverCertificatePath);
            bool metadataExists = File.Exists(_metadataPath);

            bool allExist = caExists && certExists && metadataExists;

            _logger.LogDebug("Certificate files check: CA={CA}, Cert={Cert}, Metadata={Metadata}, AllExist={AllExist}",
                caExists, certExists, metadataExists, allExist);

            return allExist;
        }, cancellationToken);
    }
}
