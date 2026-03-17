// Portless.Core/Services/CertificateTrustServiceMacOS.cs
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Portless.Core.Models;

namespace Portless.Core.Services;

/// <summary>
/// Stub implementation for macOS certificate trust service.
/// Will be implemented in Task 5.
/// </summary>
[SupportedOSPlatform("macos10.12")]
public class CertificateTrustServiceMacOS : ICertificateTrustService
{
    private readonly ILogger<CertificateTrustServiceMacOS> _logger;

    public CertificateTrustServiceMacOS(ILogger<CertificateTrustServiceMacOS> logger)
    {
        _logger = logger;
    }

    public Task<TrustInstallResult> InstallCertificateAuthorityAsync(X509Certificate2 certificate, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("macOS certificate trust installation not yet implemented");
        return Task.FromResult(new TrustInstallResult(
            Success: false,
            AlreadyInstalled: false,
            StoreAccessDenied: false,
            ErrorMessage: "macOS certificate trust installation not yet implemented"
        ));
    }

    public Task<TrustStatus> GetTrustStatusAsync(string thumbprint, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(TrustStatus.Unknown);
    }

    public Task<bool> UninstallCertificateAuthorityAsync(string thumbprint, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public Task<bool> IsAdministratorAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }
}
