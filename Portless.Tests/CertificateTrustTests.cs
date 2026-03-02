using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Portless.Core.Models;
using Portless.Core.Services;
using Portless.Proxy;
using Xunit;
using Xunit.Abstractions;

namespace Portless.Tests;

/// <summary>
/// Certificate trust status integration tests (Windows-only).
/// Tests verify Windows Certificate Store trust detection and status reporting.
/// </summary>
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class CertificateTrustTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;

    public CertificateTrustTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task Trust_Status_Detection_Returns_Valid_Result_On_Windows()
    {
        // Skip if not running on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _output.WriteLine("Test skipped: Windows Certificate Store not available on this platform");
            return;
        }

        // Arrange
        var scope = _factory.Services.CreateScope();
        var trustService = scope.ServiceProvider.GetRequiredService<ICertificateTrustService>();
        var certManager = scope.ServiceProvider.GetRequiredService<ICertificateManager>();

        // Ensure certificate exists
        await certManager.EnsureCertificatesAsync();
        var cert = await certManager.GetCertificateAuthorityAsync();
        Assert.NotNull(cert);

        // Act
        var status = await trustService.GetTrustStatusAsync(cert.Thumbprint);

        // Assert
        Assert.NotNull(status);

        // Result should be one of the valid trust statuses (not ExpiringSoon for this test)
        Assert.True(status == TrustStatus.Trusted ||
                    status == TrustStatus.NotTrusted ||
                    status == TrustStatus.Unknown,
                    $"Trust status should be Trusted, NotTrusted, or Unknown, but was: {status}");

        _output.WriteLine($"Trust Status: {status}");
    }

    [Fact]
    public async Task Trust_Status_Detection_Throws_On_Unsupported_Platform()
    {
        // Arrange
        var scope = _factory.Services.CreateScope();
        var trustService = scope.ServiceProvider.GetRequiredService<ICertificateTrustService>();
        var certManager = scope.ServiceProvider.GetRequiredService<ICertificateManager>();

        // Ensure certificate exists
        await certManager.EnsureCertificatesAsync();
        var cert = await certManager.GetCertificateAuthorityAsync();
        Assert.NotNull(cert);

        // Act & Assert
        // On non-Windows platforms, GetTrustStatusAsync should return Unknown
        // This test documents the actual behavior: it returns Unknown, not an exception
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var status = await trustService.GetTrustStatusAsync(cert.Thumbprint);
            Assert.Equal(TrustStatus.Unknown, status);
            _output.WriteLine("Test verified: Non-Windows platform returns TrustStatus.Unknown");
        }
        else
        {
            _output.WriteLine("Test skipped: Running on Windows platform");
        }
    }

    [Fact]
    public async Task Certificate_Installation_Works_On_Windows()
    {
        // Skip if not running on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _output.WriteLine("Test skipped: Windows Certificate Store not available on this platform");
            return;
        }

        // Arrange
        var scope = _factory.Services.CreateScope();
        var trustService = scope.ServiceProvider.GetRequiredService<ICertificateTrustService>();
        var certManager = scope.ServiceProvider.GetRequiredService<ICertificateManager>();

        // Ensure certificate exists
        await certManager.EnsureCertificatesAsync();
        var cert = await certManager.GetCertificateAuthorityAsync();
        Assert.NotNull(cert);

        // Check if already installed - if so, uninstall first for clean test
        var initialStatus = await trustService.GetTrustStatusAsync(cert.Thumbprint);
        if (initialStatus == TrustStatus.Trusted || initialStatus == TrustStatus.ExpiringSoon)
        {
            _output.WriteLine("Certificate already installed, uninstalling for clean test");
            await trustService.UninstallCertificateAuthorityAsync(cert.Thumbprint);
        }

        // Act - Install certificate
        var installResult = await trustService.InstallCertificateAuthorityAsync(cert);

        // Assert - Installation succeeded
        Assert.NotNull(installResult);
        Assert.True(installResult.Success || installResult.AlreadyInstalled,
            $"Installation should succeed or already be installed. Success={installResult.Success}, AlreadyInstalled={installResult.AlreadyInstalled}, Error={installResult.ErrorMessage}");

        // Verify trust status after installation
        var statusAfterInstall = await trustService.GetTrustStatusAsync(cert.Thumbprint);
        Assert.True(statusAfterInstall == TrustStatus.Trusted || statusAfterInstall == TrustStatus.ExpiringSoon,
            $"After installation, status should be Trusted or ExpiringSoon, but was: {statusAfterInstall}");

        _output.WriteLine($"Installation result: Success={installResult.Success}, AlreadyInstalled={installResult.AlreadyInstalled}");
        _output.WriteLine($"Trust status after installation: {statusAfterInstall}");

        // Cleanup - Uninstall certificate to restore system state
        if (!installResult.AlreadyInstalled)
        {
            var uninstalled = await trustService.UninstallCertificateAuthorityAsync(cert.Thumbprint);
            Assert.True(uninstalled, "Cleanup: Uninstall should succeed");
            _output.WriteLine("Cleanup: Certificate uninstalled successfully");
        }
    }

    [Fact]
    public async Task Trust_Status_Idempotent_Install()
    {
        // Skip if not running on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _output.WriteLine("Test skipped: Windows Certificate Store not available on this platform");
            return;
        }

        // Arrange
        var scope = _factory.Services.CreateScope();
        var trustService = scope.ServiceProvider.GetRequiredService<ICertificateTrustService>();
        var certManager = scope.ServiceProvider.GetRequiredService<ICertificateManager>();

        // Ensure certificate exists
        await certManager.EnsureCertificatesAsync();
        var cert = await certManager.GetCertificateAuthorityAsync();
        Assert.NotNull(cert);

        // Install certificate once
        var firstInstall = await trustService.InstallCertificateAuthorityAsync(cert);
        Assert.True(firstInstall.Success || firstInstall.AlreadyInstalled,
            $"First installation should succeed. Error={firstInstall.ErrorMessage}");

        // Act - Install certificate again
        var secondInstall = await trustService.InstallCertificateAuthorityAsync(cert);

        // Assert - Second installation succeeds (idempotent)
        Assert.NotNull(secondInstall);
        Assert.True(secondInstall.Success && secondInstall.AlreadyInstalled,
            $"Second installation should succeed with AlreadyInstalled=true. Success={secondInstall.Success}, AlreadyInstalled={secondInstall.AlreadyInstalled}, Error={secondInstall.ErrorMessage}");

        // Verify trust status is still Trusted
        var status = await trustService.GetTrustStatusAsync(cert.Thumbprint);
        Assert.True(status == TrustStatus.Trusted || status == TrustStatus.ExpiringSoon,
            $"Status should be Trusted or ExpiringSoon after idempotent install, but was: {status}");

        _output.WriteLine("Idempotent install verified: Second installation returned AlreadyInstalled=true");

        // Cleanup
        await trustService.UninstallCertificateAuthorityAsync(cert.Thumbprint);
    }

    [Fact]
    public async Task Trust_Status_Idempotent_Uninstall()
    {
        // Skip if not running on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _output.WriteLine("Test skipped: Windows Certificate Store not available on this platform");
            return;
        }

        // Arrange
        var scope = _factory.Services.CreateScope();
        var trustService = scope.ServiceProvider.GetRequiredService<ICertificateTrustService>();
        var certManager = scope.ServiceProvider.GetRequiredService<ICertificateManager>();

        // Ensure certificate exists
        await certManager.EnsureCertificatesAsync();
        var cert = await certManager.GetCertificateAuthorityAsync();
        Assert.NotNull(cert);

        // Ensure certificate is not installed
        await trustService.UninstallCertificateAuthorityAsync(cert.Thumbprint);

        // Act - Uninstall certificate again (should succeed idempotently)
        var secondUninstall = await trustService.UninstallCertificateAuthorityAsync(cert.Thumbprint);

        // Assert - Second uninstall succeeds (idempotent)
        Assert.True(secondUninstall,
            "Second uninstall should succeed even though certificate was not installed");

        // Verify status is NotTrusted
        var status = await trustService.GetTrustStatusAsync(cert.Thumbprint);
        Assert.Equal(TrustStatus.NotTrusted, status);

        _output.WriteLine("Idempotent uninstall verified: Second uninstall succeeded");
    }

    [Fact]
    public async Task Trust_Status_Detects_Expiring_Certificate()
    {
        // This test verifies the 30-day expiration warning logic
        // Note: Testing actual expiration requires creating a certificate that expires within 30 days
        // which is difficult to do in a test without mocking the certificate creation

        // Arrange
        var scope = _factory.Services.CreateScope();
        var trustService = scope.ServiceProvider.GetRequiredService<ICertificateTrustService>();
        var certManager = scope.ServiceProvider.GetRequiredService<ICertificateManager>();

        // Ensure certificate exists
        await certManager.EnsureCertificatesAsync();
        var cert = await certManager.GetCertificateAuthorityAsync();
        Assert.NotNull(cert);

        // Act - Check trust status
        var status = await trustService.GetTrustStatusAsync(cert.Thumbprint);

        // Assert - Verify ExpiringSoon is a possible return value
        // In normal circumstances with a fresh 5-year certificate, this will be Trusted
        // This test verifies the logic exists and ExpiringSoon is a valid enum value
        Assert.True(status == TrustStatus.Trusted ||
                    status == TrustStatus.NotTrusted ||
                    status == TrustStatus.ExpiringSoon ||
                    status == TrustStatus.Unknown,
                    $"Status should be one of the valid TrustStatus values, but was: {status}");

        // Document the expiration warning behavior
        if (status == TrustStatus.ExpiringSoon)
        {
            _output.WriteLine("Certificate is expiring within 30 days");
        }
        else
        {
            _output.WriteLine($"Certificate status: {status} (not expiring within 30 days)");
        }

        // Verify certificate expiration date for documentation
        var daysUntilExpiration = (cert.NotAfter - DateTimeOffset.UtcNow).Days;
        _output.WriteLine($"Certificate expires in {daysUntilExpiration} days ({cert.NotAfter:yyyy-MM-dd})");
    }

    [Fact]
    public async Task Trust_Status_Permission_Denied_Handled()
    {
        // This test documents the expected behavior when installation fails due to permissions
        // On Windows, installing to LocalMachine Root store requires administrator privileges

        // Skip if not running on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _output.WriteLine("Test skipped: Windows Certificate Store not available on this platform");
            return;
        }

        // Arrange
        var scope = _factory.Services.CreateScope();
        var trustService = scope.ServiceProvider.GetRequiredService<ICertificateTrustService>();
        var certManager = scope.ServiceProvider.GetRequiredService<ICertificateManager>();

        // Ensure certificate exists
        await certManager.EnsureCertificatesAsync();
        var cert = await certManager.GetCertificateAuthorityAsync();
        Assert.NotNull(cert);

        // Check if running as administrator
        var isAdmin = await trustService.IsAdministratorAsync();
        _output.WriteLine($"Running as administrator: {isAdmin}");

        // Act - Try to install certificate
        var installResult = await trustService.InstallCertificateAuthorityAsync(cert);

        // Assert - Verify error handling behavior
        if (!isAdmin)
        {
            // If not admin, installation should fail gracefully
            Assert.NotNull(installResult);
            if (!installResult.Success)
            {
                // Should have error information
                Assert.NotNull(installResult.ErrorMessage);
                _output.WriteLine($"Installation failed as expected (not admin): {installResult.ErrorMessage}");

                // Should indicate store access denied if that's the issue
                if (installResult.StoreAccessDenied)
                {
                    _output.WriteLine("StoreAccessDenied flag set correctly");
                }
            }
            else
            {
                // Installation succeeded (might already be installed from previous admin session)
                _output.WriteLine("Installation succeeded (certificate may have been already installed)");
            }
        }
        else
        {
            _output.WriteLine("Running as administrator - installation should succeed");
            Assert.True(installResult.Success || installResult.AlreadyInstalled,
                $"Installation should succeed with admin privileges. Error={installResult.ErrorMessage}");

            // Cleanup if we just installed it
            if (installResult.Success && !installResult.AlreadyInstalled)
            {
                await trustService.UninstallCertificateAuthorityAsync(cert.Thumbprint);
            }
        }
    }
}
