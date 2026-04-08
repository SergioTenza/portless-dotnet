namespace Portless.Cli.Services;

/// <summary>
/// Service for managing the Portless proxy as a systemd user service (daemon mode).
/// </summary>
public interface IDaemonService
{
    /// <summary>
    /// Install the systemd user service unit file.
    /// </summary>
    Task InstallAsync(bool enableHttps, bool enableNow);

    /// <summary>
    /// Uninstall (stop and remove) the systemd user service.
    /// </summary>
    Task UninstallAsync();

    /// <summary>
    /// Get the current daemon status.
    /// </summary>
    /// <returns>Tuple of (isInstalled, isEnabled, isRunning, pid if running).</returns>
    Task<(bool isInstalled, bool isEnabled, bool isRunning, int? pid)> GetStatusAsync();

    /// <summary>
    /// Enable the service to auto-start on boot.
    /// </summary>
    Task EnableAsync();

    /// <summary>
    /// Disable the service from auto-starting on boot.
    /// </summary>
    Task DisableAsync();
}
