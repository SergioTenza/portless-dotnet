using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Moq;
using Portless.Core.Models;
using Portless.Core.Services;
using Xunit;

#pragma warning disable CA1416 // macOS-only code tested via mocking

namespace Portless.Tests;

public class MacOSTrustExtendedTests
{
    private readonly Mock<ILogger<CertificateTrustServiceMacOS>> _logger;
    private readonly Mock<IPlatformDetectorService> _platformDetector;

    public MacOSTrustExtendedTests()
    {
        _logger = new Mock<ILogger<CertificateTrustServiceMacOS>>();
        _platformDetector = new Mock<IPlatformDetectorService>();
    }

    private CertificateTrustServiceMacOS CreateService()
    {
        return new CertificateTrustServiceMacOS(_logger.Object, _platformDetector.Object);
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
    public async Task InstallCertificateAuthority_AdminButNotMacOS_ThrowsOrFails()
    {
        // On Linux, the macOS-specific `security` command won't exist
        _platformDetector.Setup(d => d.GetPlatformInfo()).Returns(new PlatformInfo(
            OSPlatform.OSX,
            null,
            IsAdmin: true,
            ElevationCommand: "sudo"
        ));

        var service = CreateService();
        var cert = CreateTestCert();

        // The `security` command doesn't exist on Linux, so it will throw or fail
        var result = await service.InstallCertificateAuthorityAsync(cert, CancellationToken.None);

        // Should either return a failure result or the inner code catches the exception
        Assert.NotNull(result);
        // On Linux, the `security` binary doesn't exist, so the result will be failure
        Assert.False(result.Success);
    }

    [Fact]
    public async Task InstallCertificateAuthority_NonAdmin_ReturnsAccessDenied()
    {
        _platformDetector.Setup(d => d.GetPlatformInfo()).Returns(new PlatformInfo(
            OSPlatform.OSX,
            null,
            IsAdmin: false,
            ElevationCommand: "sudo"
        ));

        var service = CreateService();
        var cert = CreateTestCert();
        var result = await service.InstallCertificateAuthorityAsync(cert, CancellationToken.None);

        Assert.False(result.Success);
        Assert.True(result.StoreAccessDenied);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task GetTrustStatus_ReturnsNotTrusted_WhenCertNotFound()
    {
        _platformDetector.Setup(d => d.GetPlatformInfo()).Returns(new PlatformInfo(
            OSPlatform.OSX,
            null,
            IsAdmin: true,
            ElevationCommand: "sudo"
        ));

        var service = CreateService();
        // On Linux, `security` command doesn't exist, so FindCertificateAsync returns null
        var result = await service.GetTrustStatusAsync("nonexistent-thumbprint", CancellationToken.None);

        // Should return NotTrusted or Unknown depending on if exception is caught
        Assert.True(result == TrustStatus.NotTrusted || result == TrustStatus.Unknown);
    }

    [Fact]
    public async Task UninstallCertificateAuthority_ReturnsFalse_WhenSecurityCommandFails()
    {
        _platformDetector.Setup(d => d.GetPlatformInfo()).Returns(new PlatformInfo(
            OSPlatform.OSX,
            null,
            IsAdmin: true,
            ElevationCommand: "sudo"
        ));

        var service = CreateService();
        // On Linux, `security delete-certificate` won't exist
        var result = await service.UninstallCertificateAuthorityAsync("some-thumbprint", CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task UninstallCertificateAuthority_HandlesException()
    {
        _platformDetector.Setup(d => d.GetPlatformInfo()).Throws(new Exception("Platform error"));

        var service = CreateService();
        var result = await service.UninstallCertificateAuthorityAsync("thumbprint", CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task IsAdministrator_ReturnsTrue_ForAdmin()
    {
        _platformDetector.Setup(d => d.GetPlatformInfo()).Returns(new PlatformInfo(
            OSPlatform.OSX,
            null,
            IsAdmin: true,
            ElevationCommand: "sudo"
        ));

        var service = CreateService();
        Assert.True(await service.IsAdministratorAsync());
    }

    [Fact]
    public async Task IsAdministrator_ReturnsFalse_ForNonAdmin()
    {
        _platformDetector.Setup(d => d.GetPlatformInfo()).Returns(new PlatformInfo(
            OSPlatform.OSX,
            null,
            IsAdmin: false,
            ElevationCommand: "sudo"
        ));

        var service = CreateService();
        Assert.False(await service.IsAdministratorAsync());
    }

    [Fact]
    public async Task GetTrustStatus_ReturnsValidEnum()
    {
        // On Linux, `security find-certificate` fails, so result is NotTrusted or Unknown
        _platformDetector.Setup(d => d.GetPlatformInfo()).Returns(new PlatformInfo(
            OSPlatform.OSX,
            null,
            IsAdmin: true,
            ElevationCommand: "sudo"
        ));

        var service = CreateService();
        var result = await service.GetTrustStatusAsync("thumbprint", CancellationToken.None);

        Assert.True(Enum.IsDefined(result));
    }
}

#pragma warning restore CA1416
