using Microsoft.Extensions.Logging;
using Moq;
using Portless.Core.Models;
using Portless.Core.Services;
using Xunit;

namespace Portless.Tests;

public class CertificateTrustServiceTests
{
    private readonly Mock<ILogger<CertificateTrustService>> _loggerMock;
    private readonly Mock<ICertificateManager> _certManagerMock;

    public CertificateTrustServiceTests()
    {
        _loggerMock = new Mock<ILogger<CertificateTrustService>>();
        _certManagerMock = new Mock<ICertificateManager>();
    }

    private CertificateTrustService CreateService()
    {
        return new CertificateTrustService(_loggerMock.Object, _certManagerMock.Object);
    }

    [Fact]
    public async Task InstallCertificateAuthorityAsync_OnNonWindows_ReturnsPlatformError()
    {
        // Arrange
        var service = CreateService();
        using var cert = SelfSignedCertificateHelper.Generate();

        // Act
        var result = await service.InstallCertificateAuthorityAsync(cert);

        // Assert - On Linux, returns failure with Windows-only message
        if (!OperatingSystem.IsWindows())
        {
            Assert.False(result.Success);
            Assert.False(result.AlreadyInstalled);
            Assert.False(result.StoreAccessDenied);
            Assert.Equal("Certificate trust installation is Windows-only", result.ErrorMessage);
        }
        // On Windows, the result depends on admin rights and store state
    }

    [Fact]
    public async Task GetTrustStatusAsync_OnNonWindows_ReturnsUnknown()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetTrustStatusAsync("thumbprint123");

        // Assert
        if (!OperatingSystem.IsWindows())
        {
            Assert.Equal(TrustStatus.Unknown, result);
        }
    }

    [Fact]
    public async Task GetTrustStatusAsync_OnNonWindows_DoesNotCallCertificateManager()
    {
        // Arrange
        var service = CreateService();

        // Act
        await service.GetTrustStatusAsync("thumbprint123");

        // Assert - CertificateManager should NOT be called on non-Windows
        if (!OperatingSystem.IsWindows())
        {
            _certManagerMock.Verify(
                x => x.GetCertificateAuthorityAsync(It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }

    [Fact]
    public async Task UninstallCertificateAuthorityAsync_OnNonWindows_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.UninstallCertificateAuthorityAsync("thumbprint123");

        // Assert
        if (!OperatingSystem.IsWindows())
        {
            Assert.False(result);
        }
    }

    [Fact]
    public async Task IsAdministratorAsync_ReturnsBoolean()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.IsAdministratorAsync();

        // Assert - should not throw and return a boolean
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task IsAdministratorAsync_OnNonWindows_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.IsAdministratorAsync();

        // Assert
        if (!OperatingSystem.IsWindows())
        {
            Assert.False(result);
        }
    }

    [Fact]
    public async Task GetTrustStatusAsync_WhenCertManagerReturnsNull_ReturnsNotTrusted()
    {
        // This test is only meaningful on Windows, but tests the code path
        // Arrange
        var service = CreateService();
        _certManagerMock
            .Setup(x => x.GetCertificateAuthorityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Security.Cryptography.X509Certificates.X509Certificate2?)null);

        // Act
        var result = await service.GetTrustStatusAsync("nonexistent-thumbprint");

        // On non-Windows we get Unknown; on Windows with null cert we get NotTrusted
        if (!OperatingSystem.IsWindows())
        {
            Assert.Equal(TrustStatus.Unknown, result);
        }
        else
        {
            Assert.Equal(TrustStatus.NotTrusted, result);
        }
    }

    [Fact]
    public async Task InstallCertificateAuthorityAsync_WithValidCert_DoesNotThrow()
    {
        // Arrange
        var service = CreateService();
        using var cert = SelfSignedCertificateHelper.Generate();

        // Act & Assert - should not throw regardless of platform
        var result = await service.InstallCertificateAuthorityAsync(cert);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task UninstallCertificateAuthorityAsync_WithAnyThumbprint_DoesNotThrow()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert - should not throw
        var result = await service.UninstallCertificateAuthorityAsync("nonexistent-thumbprint");
        Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task GetTrustStatusAsync_WithAnyThumbprint_DoesNotThrow()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert - should not throw
        var result = await service.GetTrustStatusAsync("any-thumbprint");
        Assert.True(Enum.IsDefined(result));
    }
}

/// <summary>
/// Helper to generate self-signed certificates for testing.
/// </summary>
internal static class SelfSignedCertificateHelper
{
    public static System.Security.Cryptography.X509Certificates.X509Certificate2 Generate()
    {
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var request = new System.Security.Cryptography.X509Certificates.CertificateRequest(
            "CN=Test CA",
            rsa,
            System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension(
                true, false, 0, true));

        return request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddYears(1));
    }
}
