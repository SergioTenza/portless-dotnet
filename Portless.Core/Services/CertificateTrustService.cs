using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Runtime.Versioning;
using System.Security;
using Microsoft.Extensions.Logging;
using Portless.Core.Models;

namespace Portless.Core.Services;

/// <summary>
/// Windows-specific implementation of certificate trust management using X509Store API.
/// </summary>
[SupportedOSPlatform("windows")]
public class CertificateTrustService : ICertificateTrustService
{
    private readonly ILogger<CertificateTrustService> _logger;
    private readonly ICertificateManager _certificateManager;

    public CertificateTrustService(
        ILogger<CertificateTrustService> logger,
        ICertificateManager certificateManager)
    {
        _logger = logger;
        _certificateManager = certificateManager;
    }

    /// <inheritdoc />
    public async Task<TrustInstallResult> InstallCertificateAuthorityAsync(X509Certificate2 certificate, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new TrustInstallResult(
                Success: false,
                AlreadyInstalled: false,
                StoreAccessDenied: false,
                ErrorMessage: "Certificate trust installation is Windows-only"
            );
        }

        try
        {
            using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);

            // Check if certificate already exists
            var existingCertificates = store.Certificates.Find(
                X509FindType.FindByThumbprint,
                certificate.Thumbprint,
                validOnly: false
            );

            if (existingCertificates.Count > 0)
            {
                _logger.LogWarning("Certificate authority is already installed in trust store");
                return new TrustInstallResult(
                    Success: true,
                    AlreadyInstalled: true,
                    StoreAccessDenied: false,
                    ErrorMessage: null
                );
            }

            // Install the certificate
            store.Add(certificate);
            _logger.LogInformation("Certificate authority installed successfully to trust store");

            return new TrustInstallResult(
                Success: true,
                AlreadyInstalled: false,
                StoreAccessDenied: false,
                ErrorMessage: null
            );
        }
        catch (Exception ex) when (ex.Message.Contains("Access is denied") || ex.HResult == -2146829211)
        {
            _logger.LogError(ex, "Access denied to certificate store. Administrator privileges required.");
            return new TrustInstallResult(
                Success: false,
                AlreadyInstalled: false,
                StoreAccessDenied: true,
                ErrorMessage: "Run as Administrator"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install certificate authority to trust store");
            return new TrustInstallResult(
                Success: false,
                AlreadyInstalled: false,
                StoreAccessDenied: false,
                ErrorMessage: ex.Message
            );
        }
    }

    /// <inheritdoc />
    public async Task<TrustStatus> GetTrustStatusAsync(string thumbprint, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return TrustStatus.Unknown;
        }

        try
        {
            // Load the certificate to get expiration date
            var cert = await _certificateManager.GetCertificateAuthorityAsync(cancellationToken);
            if (cert == null)
            {
                return TrustStatus.NotTrusted;
            }

            using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);

            var certificates = store.Certificates.Find(
                X509FindType.FindByThumbprint,
                thumbprint,
                validOnly: true
            );

            if (certificates.Count == 0)
            {
                return TrustStatus.NotTrusted;
            }

            // Check if expiring soon (within 30 days)
            if (cert.NotAfter < DateTimeOffset.UtcNow.AddDays(30))
            {
                return TrustStatus.ExpiringSoon;
            }

            return TrustStatus.Trusted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to access certificate store for trust status check");
            return TrustStatus.NotTrusted;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UninstallCertificateAuthorityAsync(string thumbprint, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        try
        {
            using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);

            var certificates = store.Certificates.Find(
                X509FindType.FindByThumbprint,
                thumbprint,
                validOnly: false
            );

            // Idempotent: if not found, consider it successful
            if (certificates.Count == 0)
            {
                _logger.LogWarning("Certificate not found in trust store (already uninstalled)");
                return true;
            }

            store.Remove(certificates[0]);
            _logger.LogInformation("Certificate authority removed from trust store");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall certificate authority from trust store");
            return false;
        }
    }

    /// <inheritdoc />
    public Task<bool> IsAdministratorAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!OperatingSystem.IsWindows())
            {
                return Task.FromResult(false);
            }

            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            var isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            return Task.FromResult(isAdmin);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }
}
