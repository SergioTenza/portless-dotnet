using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Moq;
using Portless.Core.Models;
using Portless.Core.Services;
using Xunit;

#pragma warning disable CA1416 // Linux-only code tested via mocking

namespace Portless.Tests;

public class LinuxTrustExtendedTests
{
    private readonly Mock<ILogger<CertificateTrustServiceLinux>> _logger;
    private readonly Mock<IPlatformDetectorService> _platformDetector;

    public LinuxTrustExtendedTests()
    {
        _logger = new Mock<ILogger<CertificateTrustServiceLinux>>();
        _platformDetector = new Mock<IPlatformDetectorService>();
    }

    private CertificateTrustServiceLinux CreateService()
    {
        return new CertificateTrustServiceLinux(_logger.Object, _platformDetector.Object);
    }

    private X509Certificate2 CreateTestCert()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=Portless Test CA",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(true, false, 0, true));
        return request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(5));
    }

    [Fact]
    public async Task InstallCertificateAuthority_ReturnsFailure_ForNullDistro()
    {
        _platformDetector.Setup(d => d.GetPlatformInfo()).Returns(new PlatformInfo(
            OSPlatform.Linux,
            LinuxDistro: null,
            IsAdmin: true,
            ElevationCommand: "sudo"
        ));

        var service = CreateService();
        var result = await service.InstallCertificateAuthorityAsync(CreateTestCert(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("not supported", result.ErrorMessage ?? string.Empty);
    }

    [Fact]
    public async Task InstallCertificateAuthority_ReturnsFailure_ForUnsupportedDistro()
    {
        _platformDetector.Setup(d => d.GetPlatformInfo()).Returns(new PlatformInfo(
            OSPlatform.Linux,
            LinuxDistro.Unknown,
            IsAdmin: true,
            ElevationCommand: "sudo"
        ));

        var service = CreateService();
        var result = await service.InstallCertificateAuthorityAsync(CreateTestCert(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("not supported", result.ErrorMessage ?? string.Empty);
    }

    [Fact]
    public async Task GetTrustStatus_ReturnsUnknown_ForUnsupportedDistro()
    {
        _platformDetector.Setup(d => d.GetPlatformInfo()).Returns(new PlatformInfo(
            OSPlatform.Linux,
            LinuxDistro.Unknown,
            IsAdmin: true,
            ElevationCommand: "sudo"
        ));

        var service = CreateService();
        var result = await service.GetTrustStatusAsync("any-thumbprint", CancellationToken.None);

        Assert.Equal(TrustStatus.Unknown, result);
    }

    [Fact]
    public async Task GetTrustStatus_ReturnsNotTrusted_WhenFileNotFound()
    {
        // Use a temp dir for testing
        var tempDir = Path.Combine(Path.GetTempPath(), $"portless-test-linux-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        try
        {
            _platformDetector.Setup(d => d.GetPlatformInfo()).Returns(new PlatformInfo(
                OSPlatform.Linux,
                LinuxDistro.Ubuntu,
                IsAdmin: true,
                ElevationCommand: "sudo"
            ));

            var service = CreateService();
            var result = await service.GetTrustStatusAsync("nonexistent", CancellationToken.None);

            // Certificate file won't exist at the system path
            Assert.Equal(TrustStatus.NotTrusted, result);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task UninstallCertificateAuthority_ReturnsFalse_ForUnsupportedDistro()
    {
        _platformDetector.Setup(d => d.GetPlatformInfo()).Returns(new PlatformInfo(
            OSPlatform.Linux,
            LinuxDistro: null,
            IsAdmin: true,
            ElevationCommand: "sudo"
        ));

        var service = CreateService();
        var result = await service.UninstallCertificateAuthorityAsync("any-thumbprint", CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task IsAdministrator_ReturnsTrue_WhenPlatformReportsAdmin()
    {
        _platformDetector.Setup(d => d.GetPlatformInfo()).Returns(new PlatformInfo(
            OSPlatform.Linux,
            LinuxDistro.Ubuntu,
            IsAdmin: true,
            ElevationCommand: "sudo"
        ));

        var service = CreateService();
        var result = await service.IsAdministratorAsync(CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task IsAdministrator_ReturnsFalse_WhenPlatformReportsNonAdmin()
    {
        _platformDetector.Setup(d => d.GetPlatformInfo()).Returns(new PlatformInfo(
            OSPlatform.Linux,
            LinuxDistro.Ubuntu,
            IsAdmin: false,
            ElevationCommand: "sudo"
        ));

        var service = CreateService();
        var result = await service.IsAdministratorAsync(CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task InstallCertificateAuthority_ReturnsRootRequiredError_ForNonAdmin()
    {
        _platformDetector.Setup(d => d.GetPlatformInfo()).Returns(new PlatformInfo(
            OSPlatform.Linux,
            LinuxDistro.Debian,
            IsAdmin: false,
            ElevationCommand: "sudo"
        ));

        var service = CreateService();
        var result = await service.InstallCertificateAuthorityAsync(CreateTestCert(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.True(result.StoreAccessDenied);
        Assert.Contains("root", result.ErrorMessage ?? string.Empty);
    }

    [Fact]
    public async Task GetTrustStatus_HandlesException_ReturnsUnknown()
    {
        _platformDetector.Setup(d => d.GetPlatformInfo()).Throws(new Exception("Platform error"));

        var service = CreateService();
        var result = await service.GetTrustStatusAsync("any", CancellationToken.None);

        Assert.Equal(TrustStatus.Unknown, result);
    }

    [Fact]
    public async Task UninstallCertificateAuthority_HandlesException_ReturnsFalse()
    {
        _platformDetector.Setup(d => d.GetPlatformInfo()).Throws(new Exception("Platform error"));

        var service = CreateService();
        var result = await service.UninstallCertificateAuthorityAsync("any", CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task InstallCertificateAuthority_WithFedoraDistro_FailsNonAdmin()
    {
        _platformDetector.Setup(d => d.GetPlatformInfo()).Returns(new PlatformInfo(
            OSPlatform.Linux,
            LinuxDistro.Fedora,
            IsAdmin: false,
            ElevationCommand: "sudo"
        ));

        var service = CreateService();
        var result = await service.InstallCertificateAuthorityAsync(CreateTestCert(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.True(result.StoreAccessDenied);
    }

    [Fact]
    public async Task InstallCertificateAuthority_WithArchDistro_FailsNonAdmin()
    {
        _platformDetector.Setup(d => d.GetPlatformInfo()).Returns(new PlatformInfo(
            OSPlatform.Linux,
            LinuxDistro.Arch,
            IsAdmin: false,
            ElevationCommand: "sudo"
        ));

        var service = CreateService();
        var result = await service.InstallCertificateAuthorityAsync(CreateTestCert(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.True(result.StoreAccessDenied);
    }

    [Fact]
    public async Task InstallCertificateAuthority_WithRHELDistro_FailsNonAdmin()
    {
        _platformDetector.Setup(d => d.GetPlatformInfo()).Returns(new PlatformInfo(
            OSPlatform.Linux,
            LinuxDistro.RHEL,
            IsAdmin: false,
            ElevationCommand: "sudo"
        ));

        var service = CreateService();
        var result = await service.InstallCertificateAuthorityAsync(CreateTestCert(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.True(result.StoreAccessDenied);
    }
}

#pragma warning restore CA1416
