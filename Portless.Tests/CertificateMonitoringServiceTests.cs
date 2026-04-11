using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Portless.Core.Models;
using Portless.Core.Services;
using Xunit;

namespace Portless.Tests;

public class CertificateMonitoringServiceTests
{
    private readonly Mock<ICertificateManager> _certificateManagerMock;
    private readonly Mock<ILogger<CertificateMonitoringService>> _loggerMock;
    private readonly CertificateMonitoringOptions _options;

    public CertificateMonitoringServiceTests()
    {
        _certificateManagerMock = new Mock<ICertificateManager>();
        _loggerMock = new Mock<ILogger<CertificateMonitoringService>>();
        _options = new CertificateMonitoringOptions
        {
            IsEnabled = true,
            CheckIntervalHours = 6,
            WarningDays = 30,
            AutoRenew = true
        };
    }

    private CertificateMonitoringService CreateService(CertificateMonitoringOptions? options = null)
    {
        var opts = Options.Create(options ?? _options);
        return new CertificateMonitoringService(
            _certificateManagerMock.Object,
            _loggerMock.Object,
            opts);
    }

    [Fact]
    public async Task CheckAndRenewCertificateAsync_WhenNoCertificate_DoesNotRenew()
    {
        // Arrange
        _certificateManagerMock
            .Setup(m => m.GetCertificateStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((CertificateInfo?)null);

        var service = CreateService();

        // Act
        await service.CheckAndRenewCertificateAsync(CancellationToken.None);

        // Assert
        _certificateManagerMock.Verify(
            m => m.RegenerateCertificatesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckAndRenewCertificateAsync_WhenExpired_RenewsCertificate()
    {
        // Arrange
        var certInfo = new CertificateInfo
        {
            ExpiresAt = DateTime.UtcNow.AddDays(-5).ToString("o")
        };

        _certificateManagerMock
            .Setup(m => m.GetCertificateStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(certInfo);

        _certificateManagerMock
            .Setup(m => m.RegenerateCertificatesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _certificateManagerMock
            .Setup(m => m.GetServerCertificateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((X509Certificate2?)null);

        var service = CreateService();

        // Act
        await service.CheckAndRenewCertificateAsync(CancellationToken.None);

        // Assert
        _certificateManagerMock.Verify(
            m => m.RegenerateCertificatesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckAndRenewCertificateAsync_WhenWithinWarningPeriodAndAutoRenew_Renews()
    {
        // Arrange
        var certInfo = new CertificateInfo
        {
            ExpiresAt = DateTime.UtcNow.AddDays(10).ToString("o") // Within 30-day warning period
        };

        _certificateManagerMock
            .Setup(m => m.GetCertificateStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(certInfo);

        _certificateManagerMock
            .Setup(m => m.RegenerateCertificatesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _certificateManagerMock
            .Setup(m => m.GetServerCertificateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((X509Certificate2?)null);

        var service = CreateService();

        // Act
        await service.CheckAndRenewCertificateAsync(CancellationToken.None);

        // Assert
        _certificateManagerMock.Verify(
            m => m.RegenerateCertificatesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CheckAndRenewCertificateAsync_WhenWithinWarningPeriodButAutoRenewFalse_DoesNotRenew()
    {
        // Arrange
        var certInfo = new CertificateInfo
        {
            ExpiresAt = DateTime.UtcNow.AddDays(10).ToString("o") // Within 30-day warning period
        };

        _certificateManagerMock
            .Setup(m => m.GetCertificateStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(certInfo);

        var service = CreateService(new CertificateMonitoringOptions
        {
            IsEnabled = true,
            WarningDays = 30,
            AutoRenew = false
        });

        // Act
        await service.CheckAndRenewCertificateAsync(CancellationToken.None);

        // Assert
        _certificateManagerMock.Verify(
            m => m.RegenerateCertificatesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckAndRenewCertificateAsync_WhenCertificateIsValid_DoesNotRenew()
    {
        // Arrange
        var certInfo = new CertificateInfo
        {
            ExpiresAt = DateTime.UtcNow.AddDays(180).ToString("o") // Far from expiry
        };

        _certificateManagerMock
            .Setup(m => m.GetCertificateStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(certInfo);

        var service = CreateService();

        // Act
        await service.CheckAndRenewCertificateAsync(CancellationToken.None);

        // Assert
        _certificateManagerMock.Verify(
            m => m.RegenerateCertificatesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CheckAndRenewCertificateAsync_WhenInvalidExpiresAtFormat_DoesNotRenew()
    {
        // Arrange
        var certInfo = new CertificateInfo
        {
            ExpiresAt = "not-a-valid-date"
        };

        _certificateManagerMock
            .Setup(m => m.GetCertificateStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(certInfo);

        var service = CreateService();

        // Act
        await service.CheckAndRenewCertificateAsync(CancellationToken.None);

        // Assert
        _certificateManagerMock.Verify(
            m => m.RegenerateCertificatesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetCertificateStatusAsync_WhenNoCertificate_ReturnsNull()
    {
        // Arrange
        _certificateManagerMock
            .Setup(m => m.GetServerCertificateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((X509Certificate2?)null);

        var service = CreateService();

        // Act
        var result = await service.GetCertificateStatusAsync(CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDisabled_ReturnsImmediately()
    {
        // Arrange
        var service = CreateService(new CertificateMonitoringOptions
        {
            IsEnabled = false
        });

        // Act
        await service.StartAsync(CancellationToken.None);

        // Give it a moment to complete
        await Task.Delay(100);

        // Assert - should not have called GetCertificateStatusAsync since disabled
        _certificateManagerMock.Verify(
            m => m.GetCertificateStatusAsync(It.IsAny<CancellationToken>()),
            Times.Never);

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task GetCertificateStatusAsync_WhenCertificateExists_ReturnsStatus()
    {
        // Arrange
        using var cert = CreateSelfSignedCert();
        _certificateManagerMock
            .Setup(m => m.GetServerCertificateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cert);

        var service = CreateService();

        // Act
        var result = await service.GetCertificateStatusAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result!.IsCorrupted);
    }

    private static X509Certificate2 CreateSelfSignedCert()
    {
        // Create a self-signed certificate for testing
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var request = new System.Security.Cryptography.X509Certificates.CertificateRequest(
            "CN=test.portless.local",
            rsa,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);

        var cert = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(365));

        return new X509Certificate2(cert.Export(X509ContentType.Pfx));
    }
}
