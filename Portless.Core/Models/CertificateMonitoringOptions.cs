namespace Portless.Core.Models;

/// <summary>
/// Configuration options for certificate monitoring service.
/// </summary>
public class CertificateMonitoringOptions
{
    /// <summary>
    /// Gets or sets the interval between certificate checks in hours.
    /// Default: 6 hours. Configurable via PORTLESS_CERT_CHECK_INTERVAL_HOURS.
    /// </summary>
    public int CheckIntervalHours { get; set; } = 6;

    /// <summary>
    /// Gets or sets the number of days before expiration to trigger warnings.
    /// Default: 30 days. Configurable via PORTLESS_CERT_WARNING_DAYS.
    /// </summary>
    public int WarningDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether to automatically renew certificates when within warning period.
    /// Default: true. Configurable via PORTLESS_AUTO_RENEW.
    /// </summary>
    public bool AutoRenew { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the monitoring service is enabled.
    /// Default: false. Configurable via PORTLESS_ENABLE_MONITORING or --enable-monitoring flag.
    /// </summary>
    public bool IsEnabled { get; set; } = false;
}
