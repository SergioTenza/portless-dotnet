namespace Portless.Core.Services;

/// <summary>
/// Service for monitoring certificate expiration and performing automatic renewals.
/// </summary>
public interface ICertificateMonitoringService
{
    /// <summary>
    /// Checks the current certificate status and performs renewal if needed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CheckAndRenewCertificateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current certificate status with detailed information.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The certificate status or null if no certificate exists.</returns>
    Task<Models.CertificateStatus?> GetCertificateStatusAsync(CancellationToken cancellationToken = default);
}
