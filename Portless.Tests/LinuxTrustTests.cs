// Portless.Tests/LinuxTrustTests.cs
using Portless.Core.Services;
using Portless.Core.Models;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;
using Microsoft.Extensions.Logging;

namespace Portless.Tests;

/// <summary>
/// Tests for Linux certificate trust service.
/// Note: These are basic smoke tests. Full integration testing requires Docker containers.
/// </summary>
#pragma warning disable CA1416 // CertificateTrustServiceLinux is Linux-only; tested via mocking
public class LinuxTrustTests
{
    [Fact]
    public async Task InstallCertificateAuthority_ReturnsFailure_ForNonAdmin()
    {
        // This test verifies non-root behavior
        var loggerFactory = LoggerFactory.Create(builder => { });
        var mockDetector = new Moq.Mock<IPlatformDetectorService>();
        mockDetector.Setup(d => d.GetPlatformInfo()).Returns(new PlatformInfo(
            OSPlatform.Linux,
            LinuxDistro.Ubuntu,
            IsAdmin: false,  // Not admin
            ElevationCommand: "sudo"
        ));

        var service = new CertificateTrustServiceLinux(
            loggerFactory.CreateLogger<CertificateTrustServiceLinux>(),
            mockDetector.Object
        );

        var cert = CreateTestCertificate();
        var result = await service.InstallCertificateAuthorityAsync(cert, CancellationToken.None);

        // Should fail without admin privileges
        Assert.False(result.Success);
        Assert.True(result.StoreAccessDenied);
        Assert.Contains("root", result.ErrorMessage ?? string.Empty);
    }

    [Fact]
    public async Task InstallCertificateAuthority_ReturnsFailure_ForUnsupportedDistro()
    {
        // This test verifies unsupported distro behavior
        var loggerFactory = LoggerFactory.Create(builder => { });
        var mockDetector = new Moq.Mock<IPlatformDetectorService>();
        mockDetector.Setup(d => d.GetPlatformInfo()).Returns(new PlatformInfo(
            OSPlatform.Linux,
            LinuxDistro.Unknown,  // Unsupported distro
            IsAdmin: true,
            ElevationCommand: "sudo"
        ));

        var service = new CertificateTrustServiceLinux(
            loggerFactory.CreateLogger<CertificateTrustServiceLinux>(),
            mockDetector.Object
        );

        var cert = CreateTestCertificate();
        var result = await service.InstallCertificateAuthorityAsync(cert, CancellationToken.None);

        // Should fail with unsupported distro
        Assert.False(result.Success);
        Assert.Contains("not supported", result.ErrorMessage ?? string.Empty);
    }

    private X509Certificate2 CreateTestCertificate()
    {
        var distinguishedName = new X500DistinguishedName("CN=Portless Test CA");
        var request = new CertificateRequest(distinguishedName, ECDsa.Create(), HashAlgorithmName.SHA256);
        var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(5));
        return cert;
    }
}
#pragma warning restore CA1416