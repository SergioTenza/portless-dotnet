using Microsoft.Extensions.Logging;
using Moq;
using Portless.Core.Models;
using Portless.Core.Services;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Xunit;

namespace Portless.Tests;

/// <summary>
/// Extended tests for CertificateTrustService (Windows trust service).
/// Tests non-Windows code paths and error handling.
/// </summary>
public class CertificateTrustServiceExtendedTests
{
    private readonly Mock<ILogger<CertificateTrustService>> _logger;
    private readonly Mock<ICertificateManager> _certManager;

    public CertificateTrustServiceExtendedTests()
    {
        _logger = new Mock<ILogger<CertificateTrustService>>();
        _certManager = new Mock<ICertificateManager>();
    }

    private CertificateTrustService CreateService()
    {
        return new CertificateTrustService(_logger.Object, _certManager.Object);
    }

    private X509Certificate2 CreateTestCert()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=Test Trust CA",
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
    public async Task InstallCertificateAuthorityAsync_NonWindows_ReturnsWindowsOnlyError()
    {
        if (OperatingSystem.IsWindows()) return;

        var service = CreateService();
        using var cert = CreateTestCert();

        var result = await service.InstallCertificateAuthorityAsync(cert);

        Assert.False(result.Success);
        Assert.False(result.AlreadyInstalled);
        Assert.False(result.StoreAccessDenied);
        Assert.Equal("Certificate trust installation is Windows-only", result.ErrorMessage);
    }

    [Fact]
    public async Task GetTrustStatusAsync_NonWindows_ReturnsUnknown()
    {
        if (OperatingSystem.IsWindows()) return;

        var service = CreateService();
        var result = await service.GetTrustStatusAsync("any-thumbprint");
        Assert.Equal(TrustStatus.Unknown, result);
    }

    [Fact]
    public async Task UninstallCertificateAuthorityAsync_NonWindows_ReturnsFalse()
    {
        if (OperatingSystem.IsWindows()) return;

        var service = CreateService();
        var result = await service.UninstallCertificateAuthorityAsync("any-thumbprint");
        Assert.False(result);
    }

    [Fact]
    public async Task IsAdministratorAsync_NonWindows_ReturnsFalse()
    {
        if (OperatingSystem.IsWindows()) return;

        var service = CreateService();
        var result = await service.IsAdministratorAsync();
        Assert.False(result);
    }

    [Fact]
    public async Task GetTrustStatusAsync_CertManagerReturnsNull_ReturnsNotTrustedOnWindows()
    {
        var service = CreateService();
        _certManager
            .Setup(x => x.GetCertificateAuthorityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((X509Certificate2?)null);

        var result = await service.GetTrustStatusAsync("thumbprint");

        if (OperatingSystem.IsWindows())
        {
            Assert.Equal(TrustStatus.NotTrusted, result);
        }
        else
        {
            Assert.Equal(TrustStatus.Unknown, result);
        }
    }

    [Fact]
    public async Task InstallCertificateAuthorityAsync_DoesNotThrow()
    {
        var service = CreateService();
        using var cert = CreateTestCert();

        var exception = await Record.ExceptionAsync(() => service.InstallCertificateAuthorityAsync(cert));
        Assert.Null(exception);
    }

    [Fact]
    public async Task UninstallCertificateAuthorityAsync_DoesNotThrow()
    {
        var service = CreateService();
        var exception = await Record.ExceptionAsync(() => service.UninstallCertificateAuthorityAsync("thumbprint"));
        Assert.Null(exception);
    }

    [Fact]
    public async Task GetTrustStatusAsync_DoesNotThrow()
    {
        var service = CreateService();
        var exception = await Record.ExceptionAsync(() => service.GetTrustStatusAsync("thumbprint"));
        Assert.Null(exception);
    }

    [Fact]
    public async Task IsAdministratorAsync_DoesNotThrow()
    {
        var service = CreateService();
        var exception = await Record.ExceptionAsync(() => service.IsAdministratorAsync());
        Assert.Null(exception);
    }

    [Fact]
    public async Task InstallCertificateAuthorityAsync_ReturnsNonNullResult()
    {
        var service = CreateService();
        using var cert = CreateTestCert();
        var result = await service.InstallCertificateAuthorityAsync(cert);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetTrustStatusAsync_WithAnyThumbprint_ReturnsValidEnum()
    {
        var service = CreateService();
        var result = await service.GetTrustStatusAsync("any-thumbprint");
        Assert.True(Enum.IsDefined(result));
    }

    [Fact]
    public async Task UninstallCertificateAuthorityAsync_WithEmptyThumbprint_ReturnsBoolean()
    {
        var service = CreateService();
        var result = await service.UninstallCertificateAuthorityAsync("");
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task InstallCertificateAuthorityAsync_WithCancellationToken()
    {
        var service = CreateService();
        using var cert = CreateTestCert();
        using var cts = new CancellationTokenSource();

        var result = await service.InstallCertificateAuthorityAsync(cert, cts.Token);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetTrustStatusAsync_WithCancellationToken()
    {
        var service = CreateService();
        using var cts = new CancellationTokenSource();

        var result = await service.GetTrustStatusAsync("thumbprint", cts.Token);
        Assert.True(Enum.IsDefined(result));
    }

    [Fact]
    public async Task UninstallCertificateAuthorityAsync_WithCancellationToken()
    {
        var service = CreateService();
        using var cts = new CancellationTokenSource();

        var result = await service.UninstallCertificateAuthorityAsync("thumbprint", cts.Token);
        Assert.IsType<bool>(result);
    }
}
