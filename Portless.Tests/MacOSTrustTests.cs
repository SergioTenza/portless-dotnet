// Portless.Tests/MacOSTrustTests.cs
using Portless.Core.Services;
using Portless.Core.Models;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Portless.Tests;

/// <summary>
/// Tests for macOS certificate trust service.
/// Note: These are basic unit tests. Full integration testing requires macOS.
/// </summary>
public class MacOSTrustTests
{
    [Fact]
    public async Task InstallCertificateAuthority_ReturnsFailure_ForNonAdmin()
    {
        // This test verifies non-admin behavior
        var loggerFactory = LoggerFactory.Create(builder => { });
        var mockDetector = new Moq.Mock<IPlatformDetectorService>();
        mockDetector.Setup(d => d.GetPlatformInfo()).Returns(new PlatformInfo(
            OSPlatform.OSX,
            null,
            IsAdmin: false,  // Not admin
            ElevationCommand: "sudo"
        ));

        var service = new CertificateTrustServiceMacOS(
            loggerFactory.CreateLogger<CertificateTrustServiceMacOS>(),
            mockDetector.Object
        );

        var cert = CreateTestCertificate();
        var result = await service.InstallCertificateAuthorityAsync(cert, CancellationToken.None);

        // Should fail without admin privileges
        Assert.False(result.Success);
        Assert.True(result.StoreAccessDenied);
    }

    [Fact]
    public async Task IsAdministrator_ReturnsFalse_ForNonAdmin()
    {
        // This test verifies IsAdministrator returns correct status
        var loggerFactory = LoggerFactory.Create(builder => { });
        var mockDetector = new Moq.Mock<IPlatformDetectorService>();
        mockDetector.Setup(d => d.GetPlatformInfo()).Returns(new PlatformInfo(
            OSPlatform.OSX,
            null,
            IsAdmin: false,
            ElevationCommand: "sudo"
        ));

        var service = new CertificateTrustServiceMacOS(
            loggerFactory.CreateLogger<CertificateTrustServiceMacOS>(),
            mockDetector.Object
        );

        var isAdmin = await service.IsAdministratorAsync();

        // Should return false for non-admin
        Assert.False(isAdmin);
    }

    [Fact]
    public async Task IsAdministrator_ReturnsTrue_ForAdmin()
    {
        // This test verifies IsAdministrator returns correct status
        var loggerFactory = LoggerFactory.Create(builder => { });
        var mockDetector = new Moq.Mock<IPlatformDetectorService>();
        mockDetector.Setup(d => d.GetPlatformInfo()).Returns(new PlatformInfo(
            OSPlatform.OSX,
            null,
            IsAdmin: true,
            ElevationCommand: "sudo"
        ));

        var service = new CertificateTrustServiceMacOS(
            loggerFactory.CreateLogger<CertificateTrustServiceMacOS>(),
            mockDetector.Object
        );

        var isAdmin = await service.IsAdministratorAsync();

        // Should return true for admin
        Assert.True(isAdmin);
    }

    private X509Certificate2 CreateTestCertificate()
    {
        var distinguishedName = new X500DistinguishedName("CN=Portless Test CA");
        var request = new CertificateRequest(distinguishedName, ECDsa.Create(), HashAlgorithmName.SHA256);
        var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(5));
        return cert;
    }
}
