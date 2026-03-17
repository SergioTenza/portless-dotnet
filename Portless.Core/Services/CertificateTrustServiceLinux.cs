// Portless.Core/Services/CertificateTrustServiceLinux.cs
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Portless.Core.Models;

namespace Portless.Core.Services;

/// <summary>
/// Stub implementation for Linux certificate trust service.
/// Will be implemented in Task 4.
/// </summary>
[SupportedOSPlatform("linux")]
public class CertificateTrustServiceLinux : ICertificateTrustService
{
    private readonly ILogger<CertificateTrustServiceLinux> _logger;

    public CertificateTrustServiceLinux(ILogger<CertificateTrustServiceLinux> logger)
    {
        _logger = logger;
    }

    public Task<TrustInstallResult> InstallCertificateAuthorityAsync(X509Certificate2 certificate, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Linux certificate trust installation not yet implemented");
        return Task.FromResult(new TrustInstallResult(
            Success: false,
            AlreadyInstalled: false,
            StoreAccessDenied: false,
            ErrorMessage: "Linux certificate trust installation not yet implemented"
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
