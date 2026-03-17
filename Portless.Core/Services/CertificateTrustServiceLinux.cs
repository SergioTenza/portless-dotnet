// Portless.Core/Services/CertificateTrustServiceLinux.cs
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Portless.Core.Models;
using System.Diagnostics;
using System.Text;

namespace Portless.Core.Services;

/// <summary>
/// Linux certificate trust service that automates certificate installation for Linux distributions.
/// Supports Ubuntu/Debian (update-ca-certificates), Fedora/RHEL (update-ca-trust), and Arch (trust anchor).
/// </summary>
[SupportedOSPlatform("linux")]
public class CertificateTrustServiceLinux : ICertificateTrustService
{
    private readonly ILogger<CertificateTrustServiceLinux> _logger;
    private readonly IPlatformDetectorService _platformDetector;
    private readonly Dictionary<LinuxDistro, string> _distroPaths;

    public CertificateTrustServiceLinux(
        ILogger<CertificateTrustServiceLinux> logger,
        IPlatformDetectorService platformDetector)
    {
        _logger = logger;
        _platformDetector = platformDetector;

        // Distribution-specific certificate paths
        _distroPaths = new Dictionary<LinuxDistro, string>
        {
            { LinuxDistro.Ubuntu, "/usr/local/share/ca-certificates/portless-ca.crt" },
            { LinuxDistro.Debian, "/usr/local/share/ca-certificates/portless-ca.crt" },
            { LinuxDistro.Fedora, "/etc/pki/ca-trust/source/anchors/portless-ca.crt" },
            { LinuxDistro.RHEL, "/etc/pki/ca-trust/source/anchors/portless-ca.crt" },
            { LinuxDistro.Arch, "/usr/local/share/trust-anchor/portless-ca.crt" }
        };
    }

    /// <inheritdoc />
    public async Task<TrustInstallResult> InstallCertificateAuthorityAsync(X509Certificate2 certificate, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting Linux certificate trust installation");

            // Get platform information
            var platformInfo = _platformDetector.GetPlatformInfo();

            // Verify we're on Linux
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var error = "Certificate trust installation is only supported on Linux";
                _logger.LogError(error);
                return new TrustInstallResult(false, false, false, error);
            }

            // Check for supported distribution
            if (platformInfo.LinuxDistro == null || !_distroPaths.ContainsKey(platformInfo.LinuxDistro.Value))
            {
                var error = $"Linux distribution '{platformInfo.LinuxDistro}' is not supported";
                _logger.LogError(error);
                return new TrustInstallResult(false, false, false, error);
            }

            // Check for admin privileges
            if (!platformInfo.IsAdmin)
            {
                var error = "Certificate trust installation requires root privileges. Run with sudo.";
                _logger.LogError(error);
                return new TrustInstallResult(false, false, true, error);
            }

            var distro = platformInfo.LinuxDistro.Value;
            var certPath = _distroPaths[distro];

            _logger.LogInformation("Installing certificate for {Distro} at {Path}", distro, certPath);

            // Check if already installed
            if (await IsCertificateInstalledAsync(certificate, certPath, cancellationToken))
            {
                _logger.LogInformation("Certificate is already installed");
                return new TrustInstallResult(true, true, false, null);
            }

            // Convert PFX to CRT format and install
            var tempCertPath = await ConvertCertificateToCrtAsync(certificate, cancellationToken);
            try
            {
                // Ensure directory exists
                var directory = Path.GetDirectoryName(certPath)!;
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Copy certificate to target location
                File.Copy(tempCertPath, certPath, overwrite: true);
                _logger.LogInformation("Certificate installed to {Path}", certPath);

                // Run distribution-specific update command
                await RunCertificateUpdateCommandAsync(distro, cancellationToken);

                _logger.LogInformation("Certificate trust installation completed successfully");
                return new TrustInstallResult(true, false, false, null);
            }
            finally
            {
                // Clean up temporary file
                if (File.Exists(tempCertPath))
                {
                    File.Delete(tempCertPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install certificate trust");
            return new TrustInstallResult(false, false, false, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<TrustStatus> GetTrustStatusAsync(string thumbprint, CancellationToken cancellationToken = default)
    {
        try
        {
            var platformInfo = _platformDetector.GetPlatformInfo();

            // Verify we're on Linux with a supported distro
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                platformInfo.LinuxDistro == null ||
                !_distroPaths.ContainsKey(platformInfo.LinuxDistro.Value))
            {
                return TrustStatus.Unknown;
            }

            var certPath = _distroPaths[platformInfo.LinuxDistro.Value];

            // Check if certificate file exists
            if (!File.Exists(certPath))
            {
                _logger.LogDebug("Certificate file not found: {Path}", certPath);
                return TrustStatus.NotTrusted;
            }

            // Read certificate and check thumbprint
            var certBytes = await File.ReadAllBytesAsync(certPath, cancellationToken);
            var cert = new X509Certificate2(certBytes);

            if (cert.Thumbprint != thumbprint)
            {
                _logger.LogDebug("Certificate thumbprint mismatch");
                return TrustStatus.NotTrusted;
            }

            // Check expiration
            var daysUntilExpiration = (cert.NotAfter - DateTime.UtcNow).Days;
            if (daysUntilExpiration < 30)
            {
                _logger.LogWarning("Certificate expires in {Days} days", daysUntilExpiration);
                return TrustStatus.ExpiringSoon;
            }

            return TrustStatus.Trusted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get certificate trust status");
            return TrustStatus.Unknown;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UninstallCertificateAuthorityAsync(string thumbprint, CancellationToken cancellationToken = default)
    {
        try
        {
            var platformInfo = _platformDetector.GetPlatformInfo();

            // Verify we're on Linux with a supported distro
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                platformInfo.LinuxDistro == null ||
                !_distroPaths.ContainsKey(platformInfo.LinuxDistro.Value))
            {
                _logger.LogWarning("Cannot uninstall: unsupported platform or distribution");
                return false;
            }

            var distro = platformInfo.LinuxDistro.Value;
            var certPath = _distroPaths[distro];

            // Check if certificate exists
            if (!File.Exists(certPath))
            {
                _logger.LogInformation("Certificate file not found, already uninstalled");
                return true; // Idempotent
            }

            // Verify thumbprint matches before deleting
            var certBytes = await File.ReadAllBytesAsync(certPath, cancellationToken);
            var cert = new X509Certificate2(certBytes);

            if (cert.Thumbprint != thumbprint)
            {
                _logger.LogWarning("Certificate thumbprint mismatch, refusing to uninstall");
                return false;
            }

            // Delete certificate file
            File.Delete(certPath);
            _logger.LogInformation("Certificate removed from {Path}", certPath);

            // Run distribution-specific update command
            await RunCertificateUpdateCommandAsync(distro, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall certificate trust");
            return false;
        }
    }

    /// <inheritdoc />
    public Task<bool> IsAdministratorAsync(CancellationToken cancellationToken = default)
    {
        var platformInfo = _platformDetector.GetPlatformInfo();
        return Task.FromResult(platformInfo.IsAdmin);
    }

    /// <summary>
    /// Checks if the certificate is already installed by comparing thumbprints.
    /// </summary>
    private async Task<bool> IsCertificateInstalledAsync(X509Certificate2 certificate, string certPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(certPath))
        {
            return false;
        }

        try
        {
            var existingCertBytes = await File.ReadAllBytesAsync(certPath, cancellationToken);
            var existingCert = new X509Certificate2(existingCertBytes);
            return existingCert.Thumbprint == certificate.Thumbprint;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Converts a PFX certificate to CRT format and saves to a temporary file.
    /// </summary>
    private async Task<string> ConvertCertificateToCrtAsync(X509Certificate2 certificate, CancellationToken cancellationToken)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"portless-ca-{Guid.NewGuid()}.crt");

        // Export certificate in DER format (not PFX)
        var certBytes = certificate.Export(X509ContentType.Cert);
        await File.WriteAllBytesAsync(tempPath, certBytes, cancellationToken);

        _logger.LogDebug("Certificate converted to CRT format: {Path}", tempPath);
        return tempPath;
    }

    /// <summary>
    /// Runs the distribution-specific certificate update command.
    /// </summary>
    private async Task RunCertificateUpdateCommandAsync(LinuxDistro distro, CancellationToken cancellationToken)
    {
        string command;
        string arguments;

        switch (distro)
        {
            case LinuxDistro.Ubuntu:
            case LinuxDistro.Debian:
                command = "update-ca-certificates";
                arguments = "--fresh";
                break;

            case LinuxDistro.Fedora:
            case LinuxDistro.RHEL:
                command = "update-ca-trust";
                arguments = "extract";
                break;

            case LinuxDistro.Arch:
                command = "trust";
                arguments = "extract-compat";
                break;

            default:
                _logger.LogWarning("Unknown distribution, skipping certificate update command");
                return;
        }

        _logger.LogInformation("Running certificate update command: {Command} {Arguments}", command, arguments);

        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(processInfo);
            if (process == null)
            {
                _logger.LogWarning("Failed to start certificate update command");
                return;
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                _logger.LogWarning("Certificate update command exited with code {ExitCode}. Error: {Error}",
                    process.ExitCode, error);
            }
            else
            {
                _logger.LogInformation("Certificate update command completed successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run certificate update command");
            throw;
        }
    }
}
