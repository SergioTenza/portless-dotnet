// Portless.Core/Services/CertificateTrustServiceMacOS.cs
using System.Runtime.Versioning;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Portless.Core.Models;

namespace Portless.Core.Services;

/// <summary>
/// macOS-specific implementation of certificate trust management using Security framework.
/// Uses the security command-line tool to install certificates to System Keychain.
/// </summary>
[SupportedOSPlatform("macos10.12")]
public class CertificateTrustServiceMacOS : ICertificateTrustService
{
    private readonly ILogger<CertificateTrustServiceMacOS> _logger;
    private readonly IPlatformDetectorService _platformDetector;

    public CertificateTrustServiceMacOS(
        ILogger<CertificateTrustServiceMacOS> logger,
        IPlatformDetectorService platformDetector)
    {
        _logger = logger;
        _platformDetector = platformDetector;
    }

    /// <inheritdoc />
    public async Task<TrustInstallResult> InstallCertificateAuthorityAsync(X509Certificate2 certificate, CancellationToken cancellationToken = default)
    {
        var platformInfo = _platformDetector.GetPlatformInfo();

        if (!platformInfo.IsAdmin)
        {
            return new TrustInstallResult(
                Success: false,
                AlreadyInstalled: false,
                StoreAccessDenied: true,
                ErrorMessage: $"Administrator privileges required. Run: {platformInfo.ElevationCommand} portless cert install"
            );
        }

        try
        {
            _logger.LogInformation("Starting macOS certificate trust installation");

            // Check if already installed
            var existingStatus = await GetTrustStatusAsync(certificate.Thumbprint, cancellationToken);
            if (existingStatus == TrustStatus.Trusted)
            {
                _logger.LogInformation("Certificate already installed in System Keychain");
                return new TrustInstallResult(
                    Success: true,
                    AlreadyInstalled: true,
                    StoreAccessDenied: false,
                    ErrorMessage: null
                );
            }

            // Convert PFX to PEM format using openssl
            var pemPath = await ConvertCertificateToPemAsync(certificate, cancellationToken);

            try
            {
                // Import certificate to System Keychain
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "security",
                        Arguments = $"add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain \"{pemPath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
                var error = await process.StandardError.ReadToEndAsync(cancellationToken);
                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode != 0)
                {
                    _logger.LogError("Failed to install certificate: {Error}", error);
                    return new TrustInstallResult(
                        Success: false,
                        AlreadyInstalled: false,
                        StoreAccessDenied: false,
                        ErrorMessage: $"Failed to install certificate: {error}"
                    );
                }

                _logger.LogInformation("Successfully installed certificate to System Keychain");
                return new TrustInstallResult(
                    Success: true,
                    AlreadyInstalled: false,
                    StoreAccessDenied: false,
                    ErrorMessage: null
                );
            }
            finally
            {
                // Clean up temporary PEM file
                if (File.Exists(pemPath))
                {
                    File.Delete(pemPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install certificate on macOS");
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
        try
        {
            var cert = await FindCertificateAsync(thumbprint, cancellationToken);
            if (cert == null)
            {
                return TrustStatus.NotTrusted;
            }

            var daysUntilExpiration = (cert.NotAfter - DateTime.UtcNow).Days;
            if (daysUntilExpiration <= 30)
            {
                return TrustStatus.ExpiringSoon;
            }

            return TrustStatus.Trusted;
        }
        catch
        {
            return TrustStatus.Unknown;
        }
    }

    /// <inheritdoc />
    public async Task<bool> UninstallCertificateAuthorityAsync(string thumbprint, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Uninstalling certificate from System Keychain");

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "security",
                    Arguments = $"delete-certificate -c /Library/Keychains/System.keychain -Z {thumbprint}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0)
            {
                _logger.LogInformation("Successfully uninstalled certificate from System Keychain");
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to uninstall certificate: {Error}", error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall certificate from macOS");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsAdministratorAsync(CancellationToken cancellationToken = default)
    {
        var platformInfo = _platformDetector.GetPlatformInfo();
        return platformInfo.IsAdmin;
    }

    /// <summary>
    /// Converts a PFX certificate to PEM format using openssl.
    /// </summary>
    private async Task<string> ConvertCertificateToPemAsync(X509Certificate2 certificate, CancellationToken ct)
    {
        var tempPath = Path.GetTempPath();
        var pfxPath = Path.Combine(tempPath, $"temp-cert-{Guid.NewGuid()}.pfx");
        var pemPath = Path.Combine(tempPath, $"portless-ca-{Guid.NewGuid()}.pem");

        try
        {
            // Export certificate to PFX file
            var pfxBytes = certificate.Export(X509ContentType.Pfx, "password");
            await File.WriteAllBytesAsync(pfxPath, pfxBytes, ct);

            // Convert PFX to PEM using openssl
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "openssl",
                    Arguments = $"pkcs12 -in \"{pfxPath}\" -out \"{pemPath}\" -nodes -passin pass:password",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(ct);
            var error = await process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0)
            {
                _logger.LogError("Failed to convert certificate to PEM: {Error}", error);
                throw new InvalidOperationException($"Failed to convert certificate to PEM format: {error}");
            }

            _logger.LogDebug("Certificate converted to PEM format: {Path}", pemPath);
            return pemPath;
        }
        catch
        {
            // Clean up on failure
            if (File.Exists(pfxPath))
            {
                File.Delete(pfxPath);
            }
            if (File.Exists(pemPath))
            {
                File.Delete(pemPath);
            }
            throw;
        }
        finally
        {
            // Clean up temporary PFX file
            if (File.Exists(pfxPath))
            {
                File.Delete(pfxPath);
            }
        }
    }

    /// <summary>
    /// Finds a certificate in the System Keychain by thumbprint.
    /// </summary>
    private async Task<X509Certificate2?> FindCertificateAsync(string thumbprint, CancellationToken ct)
    {
        try
        {
            // Use security command to find certificate
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "security",
                    Arguments = $"find-certificate -c /Library/Keychains/System.keychain -Z \"{thumbprint}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync(ct);
            var error = await process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            // Check if certificate was found
            if (string.IsNullOrEmpty(output) || output.Contains("0 certificates found") || process.ExitCode != 0)
            {
                _logger.LogDebug("Certificate not found in System Keychain");
                return null;
            }

            // For now, we'll create a minimal certificate to indicate it exists
            // A full implementation would parse the output to get the actual certificate
            _logger.LogDebug("Certificate found in System Keychain");
            return null; // Would return actual certificate if parsed
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for certificate");
            return null;
        }
    }
}
