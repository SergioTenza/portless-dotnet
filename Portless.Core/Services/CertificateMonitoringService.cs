using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Portless.Core.Services;

/// <summary>
/// Background service that periodically checks certificate expiration and performs automatic renewals.
/// </summary>
public class CertificateMonitoringService : BackgroundService, ICertificateMonitoringService
{
    private readonly ICertificateManager _certificateManager;
    private readonly ILogger<CertificateMonitoringService> _logger;
    private readonly CertificateMonitoringOptions _options;

    public CertificateMonitoringService(
        ICertificateManager certificateManager,
        ILogger<CertificateMonitoringService> logger,
        IOptions<CertificateMonitoringOptions> options)
    {
        _certificateManager = certificateManager;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.IsEnabled)
        {
            _logger.LogInformation("Certificate monitoring service is disabled. Enable with PORTLESS_ENABLE_MONITORING=true or --enable-monitoring flag");
            return;
        }

        _logger.LogInformation("Certificate monitoring service started. Checking every {Hours} hours", _options.CheckIntervalHours);

        // Perform initial check on startup
        await CheckAndRenewCertificateAsync(stoppingToken);

        // Set up periodic checks
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(_options.CheckIntervalHours), stoppingToken);
                await CheckAndRenewCertificateAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                // Log error but continue monitoring
                _logger.LogError(ex, "Error during certificate monitoring check. Will retry at next interval");
            }
        }

        _logger.LogInformation("Certificate monitoring service stopped");
    }

    /// <inheritdoc/>
    public async Task CheckAndRenewCertificateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking certificate status...");

            // Get certificate status from metadata
            var metadata = await _certificateManager.GetCertificateStatusAsync(cancellationToken);

            if (metadata == null)
            {
                _logger.LogWarning("No certificate found. Monitoring will check again on next interval");
                return;
            }

            // Parse expiration date
            if (!DateTimeOffset.TryParse(metadata.ExpiresAt, out var expiresAt))
            {
                _logger.LogWarning("Failed to parse certificate expiration date: {ExpiresAt}", metadata.ExpiresAt);
                return;
            }

            var now = DateTimeOffset.UtcNow;
            var daysUntilExpiration = (expiresAt - now).Days;

            _logger.LogDebug("Certificate expires on {ExpiresAt} ({Days} days from now)", expiresAt.ToString("yyyy-MM-dd"), daysUntilExpiration);

            // Check if certificate is expired
            if (now > expiresAt)
            {
                _logger.LogWarning("Certificate has expired on {ExpiresAt}. Renewing...", expiresAt.ToString("yyyy-MM-dd"));
                await RenewCertificateAsync(cancellationToken);
                return;
            }

            // Check if certificate is within warning period
            if (daysUntilExpiration <= _options.WarningDays)
            {
                if (_options.AutoRenew)
                {
                    _logger.LogWarning("Certificate expires in {Days} days (within {WarningDays}-day warning period). Auto-renewing...",
                        daysUntilExpiration, _options.WarningDays);
                    await RenewCertificateAsync(cancellationToken);
                }
                else
                {
                    _logger.LogWarning("Certificate expires in {Days} days (within {WarningDays}-day warning period). Auto-renewal is disabled. Run 'portless cert renew' to renew manually",
                        daysUntilExpiration, _options.WarningDays);
                }
            }
            else
            {
                _logger.LogDebug("Certificate is valid and not within warning period ({Days} days remaining)", daysUntilExpiration);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking certificate status");
        }
    }

    /// <inheritdoc/>
    public async Task<Models.CertificateStatus?> GetCertificateStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cert = await _certificateManager.GetServerCertificateAsync(cancellationToken);
            if (cert == null)
            {
                return null;
            }

            var now = DateTimeOffset.UtcNow;
            bool isExpired = now > cert.NotAfter;
            bool isExpiringSoon = now > cert.NotAfter.AddDays(-_options.WarningDays);
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

            return new Models.CertificateStatus(
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting certificate status");
            return null;
        }
    }

    /// <summary>
    /// Renews the certificate by regenerating it.
    /// </summary>
    private async Task RenewCertificateAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Regenerating certificate...");
            await _certificateManager.RegenerateCertificatesAsync(cancellationToken);
            _logger.LogInformation("Certificate regenerated successfully");

            // Log new expiration
            var newCert = await _certificateManager.GetServerCertificateAsync(cancellationToken);
            if (newCert != null)
            {
                _logger.LogInformation("New certificate expires on {ExpiresAt}", newCert.NotAfter.ToString("yyyy-MM-dd"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to regenerate certificate. Will retry on next interval");
        }
    }
}
