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
}
