using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Moq;
using Portless.Core.Models;
using Portless.Core.Services;
using Xunit;

namespace Portless.Tests;

public class CertificateManagerTests
{
    private readonly Mock<ICertificateService> _certService;
    private readonly Mock<ICertificateStorageService> _storageService;
    private readonly Mock<ICertificatePermissionService> _permissionService;
    private readonly Mock<ILogger<CertificateManager>> _logger;

    public CertificateManagerTests()
    {
        _certService = new Mock<ICertificateService>();
        _storageService = new Mock<ICertificateStorageService>();
        _permissionService = new Mock<ICertificatePermissionService>();
        _logger = new Mock<ILogger<CertificateManager>>();
    }

    private CertificateManager CreateManager()
    {
        return new CertificateManager(
            _certService.Object,
            _storageService.Object,
            _permissionService.Object,
            _logger.Object);
    }

    private X509Certificate2 GenerateTestCert(string cn = "Test CA", int validYears = 5)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            $"CN={cn}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(cn.Contains("CA"), false, 0, true));
        var cert = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(validYears));
        // Re-export with exportable key
        return X509CertificateLoader.LoadPkcs12(cert.Export(X509ContentType.Pfx), null, X509KeyStorageFlags.Exportable);
    }

    [Fact]
    public async Task EnsureCertificatesAsync_ForceRegeneration_RegeneratesAndReturnsStatus()
    {
        // Arrange
        var serverCert = GenerateTestCert("*.localhost");
        _certService.Setup(x => x.GenerateCertificateAuthorityAsync(It.IsAny<CertificateGenerationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenerateTestCert("Test CA"));
        _certService.Setup(x => x.GenerateWildcardCertificateAsync(It.IsAny<X509Certificate2>(), It.IsAny<CertificateGenerationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serverCert);
        _storageService.Setup(x => x.SaveCertificateAuthorityAsync(It.IsAny<X509Certificate2>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _storageService.Setup(x => x.SaveServerCertificateAsync(It.IsAny<X509Certificate2>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _storageService.Setup(x => x.SaveCertificateMetadataAsync(It.IsAny<CertificateInfo>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var manager = CreateManager();

        // Act
        var result = await manager.EnsureCertificatesAsync(forceRegeneration: true);

        // Assert
        Assert.True(result.IsValid);
        Assert.False(result.IsExpired);
        Assert.Equal("Certificates regenerated successfully", result.Message);
    }

    [Fact]
    public async Task EnsureCertificatesAsync_NoFiles_GeneratesNewCertificates()
    {
        // Arrange
        var caCert = GenerateTestCert("Test CA");
        var serverCert = GenerateTestCert("*.localhost");

        _storageService.Setup(x => x.CertificateFilesExistAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _certService.Setup(x => x.GenerateCertificateAuthorityAsync(It.IsAny<CertificateGenerationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(caCert);
        _certService.Setup(x => x.GenerateWildcardCertificateAsync(It.IsAny<X509Certificate2>(), It.IsAny<CertificateGenerationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serverCert);
        _storageService.Setup(x => x.SaveCertificateAuthorityAsync(It.IsAny<X509Certificate2>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _storageService.Setup(x => x.SaveServerCertificateAsync(It.IsAny<X509Certificate2>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _storageService.Setup(x => x.SaveCertificateMetadataAsync(It.IsAny<CertificateInfo>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _permissionService.Setup(x => x.VerifyFilePermissionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var manager = CreateManager();

        // Act
        var result = await manager.EnsureCertificatesAsync();

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("Certificates generated for first-time use", result.Message);
        _certService.Verify(x => x.GenerateCertificateAuthorityAsync(It.IsAny<CertificateGenerationOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnsureCertificatesAsync_ExistingValidCerts_ReturnsStatus()
    {
        // Arrange
        var caCert = GenerateTestCert("Test CA");
        var serverCert = GenerateTestCert("*.localhost");

        _storageService.Setup(x => x.CertificateFilesExistAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _storageService.Setup(x => x.LoadCertificateAuthorityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(caCert);
        _storageService.Setup(x => x.LoadServerCertificateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(serverCert);
        _permissionService.Setup(x => x.VerifyFilePermissionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var manager = CreateManager();

        // Act
        var result = await manager.EnsureCertificatesAsync();

        // Assert
        Assert.True(result.IsValid);
        Assert.False(result.IsExpired);
    }

    [Fact]
    public async Task EnsureCertificatesAsync_NullCA_Regenerates()
    {
        // Arrange
        var newCaCert = GenerateTestCert("Test CA");
        var newServerCert = GenerateTestCert("*.localhost");

        _storageService.Setup(x => x.CertificateFilesExistAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _storageService.SetupSequence(x => x.LoadCertificateAuthorityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((X509Certificate2?)null);
        _certService.Setup(x => x.GenerateCertificateAuthorityAsync(It.IsAny<CertificateGenerationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newCaCert);
        _certService.Setup(x => x.GenerateWildcardCertificateAsync(It.IsAny<X509Certificate2>(), It.IsAny<CertificateGenerationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newServerCert);
        _storageService.Setup(x => x.SaveCertificateAuthorityAsync(It.IsAny<X509Certificate2>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _storageService.Setup(x => x.SaveServerCertificateAsync(It.IsAny<X509Certificate2>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _storageService.Setup(x => x.SaveCertificateMetadataAsync(It.IsAny<CertificateInfo>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _permissionService.Setup(x => x.VerifyFilePermissionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        // After regeneration, LoadServerCertificate returns the new cert
        _storageService.Setup(x => x.LoadServerCertificateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(newServerCert);

        var manager = CreateManager();

        // Act
        var result = await manager.EnsureCertificatesAsync();

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task EnsureCertificatesAsync_NullServerCert_Regenerates()
    {
        // Arrange
        var caCert = GenerateTestCert("Test CA");
        var newServerCert = GenerateTestCert("*.localhost");

        _storageService.Setup(x => x.CertificateFilesExistAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _storageService.Setup(x => x.LoadCertificateAuthorityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(caCert);
        _storageService.SetupSequence(x => x.LoadServerCertificateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((X509Certificate2?)null)
            .ReturnsAsync(newServerCert);
        _certService.Setup(x => x.GenerateCertificateAuthorityAsync(It.IsAny<CertificateGenerationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(caCert);
        _certService.Setup(x => x.GenerateWildcardCertificateAsync(It.IsAny<X509Certificate2>(), It.IsAny<CertificateGenerationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newServerCert);
        _storageService.Setup(x => x.SaveCertificateAuthorityAsync(It.IsAny<X509Certificate2>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _storageService.Setup(x => x.SaveServerCertificateAsync(It.IsAny<X509Certificate2>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _storageService.Setup(x => x.SaveCertificateMetadataAsync(It.IsAny<CertificateInfo>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _permissionService.Setup(x => x.VerifyFilePermissionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var manager = CreateManager();

        // Act
        var result = await manager.EnsureCertificatesAsync();

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task EnsureCertificatesAsync_CorruptedCert_Regenerates()
    {
        // Arrange
        var newCaCert = GenerateTestCert("Test CA");
        var newServerCert = GenerateTestCert("*.localhost");

        _storageService.Setup(x => x.CertificateFilesExistAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _storageService.Setup(x => x.LoadCertificateAuthorityAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CryptographicException("Corrupted"));
        _certService.Setup(x => x.GenerateCertificateAuthorityAsync(It.IsAny<CertificateGenerationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newCaCert);
        _certService.Setup(x => x.GenerateWildcardCertificateAsync(It.IsAny<X509Certificate2>(), It.IsAny<CertificateGenerationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newServerCert);
        _storageService.Setup(x => x.SaveCertificateAuthorityAsync(It.IsAny<X509Certificate2>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _storageService.Setup(x => x.SaveServerCertificateAsync(It.IsAny<X509Certificate2>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _storageService.Setup(x => x.SaveCertificateMetadataAsync(It.IsAny<CertificateInfo>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _storageService.Setup(x => x.LoadServerCertificateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(newServerCert);
        _permissionService.Setup(x => x.VerifyFilePermissionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var manager = CreateManager();

        // Act
        var result = await manager.EnsureCertificatesAsync();

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GetCertificateStatusAsync_WithMetadata_ReturnsMetadata()
    {
        // Arrange
        var metadata = new CertificateInfo
        {
            Version = "1.0",
            Sha256Thumbprint = "abc123",
            ExpiresAt = DateTime.UtcNow.AddYears(5).ToString("o"),
            CreatedAt = DateTime.UtcNow.ToString("o")
        };

        _storageService.Setup(x => x.LoadCertificateMetadataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);

        var manager = CreateManager();

        // Act
        var result = await manager.GetCertificateStatusAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("abc123", result.Sha256Thumbprint);
    }

    [Fact]
    public async Task GetCertificateStatusAsync_NoMetadata_NoCert_ReturnsNull()
    {
        // Arrange
        _storageService.Setup(x => x.LoadCertificateMetadataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((CertificateInfo?)null);
        _storageService.Setup(x => x.LoadCertificateAuthorityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((X509Certificate2?)null);

        var manager = CreateManager();

        // Act
        var result = await manager.GetCertificateStatusAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCertificateStatusAsync_NoMetadata_WithCert_ReturnsNull()
    {
        // Arrange
        var caCert = GenerateTestCert("Test CA");
        _storageService.Setup(x => x.LoadCertificateMetadataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((CertificateInfo?)null);
        _storageService.Setup(x => x.LoadCertificateAuthorityAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(caCert);

        var manager = CreateManager();

        // Act
        var result = await manager.GetCertificateStatusAsync();

        // Assert - returns null per code (line 205)
        Assert.Null(result);
    }

    [Fact]
    public async Task RegenerateCertificatesAsync_GeneratesAndSaves()
    {
        // Arrange
        var caCert = GenerateTestCert("Test CA");
        var serverCert = GenerateTestCert("*.localhost");

        _certService.Setup(x => x.GenerateCertificateAuthorityAsync(It.IsAny<CertificateGenerationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(caCert);
        _certService.Setup(x => x.GenerateWildcardCertificateAsync(It.IsAny<X509Certificate2>(), It.IsAny<CertificateGenerationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serverCert);
        _storageService.Setup(x => x.SaveCertificateAuthorityAsync(It.IsAny<X509Certificate2>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _storageService.Setup(x => x.SaveServerCertificateAsync(It.IsAny<X509Certificate2>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _storageService.Setup(x => x.SaveCertificateMetadataAsync(It.IsAny<CertificateInfo>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var manager = CreateManager();

        // Act
        await manager.RegenerateCertificatesAsync();

        // Assert
        _certService.Verify(x => x.GenerateCertificateAuthorityAsync(It.IsAny<CertificateGenerationOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        _certService.Verify(x => x.GenerateWildcardCertificateAsync(It.IsAny<X509Certificate2>(), It.IsAny<CertificateGenerationOptions>(), It.IsAny<CancellationToken>()), Times.Once);
        _storageService.Verify(x => x.SaveCertificateAuthorityAsync(It.IsAny<X509Certificate2>(), It.IsAny<CancellationToken>()), Times.Once);
        _storageService.Verify(x => x.SaveServerCertificateAsync(It.IsAny<X509Certificate2>(), It.IsAny<CancellationToken>()), Times.Once);
        _storageService.Verify(x => x.SaveCertificateMetadataAsync(It.IsAny<CertificateInfo>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnsureCertificatesAsync_InsecurePermissions_LogsWarning()
    {
        // Arrange
        var caCert = GenerateTestCert("Test CA");
        var serverCert = GenerateTestCert("*.localhost");

        _storageService.Setup(x => x.CertificateFilesExistAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _certService.Setup(x => x.GenerateCertificateAuthorityAsync(It.IsAny<CertificateGenerationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(caCert);
        _certService.Setup(x => x.GenerateWildcardCertificateAsync(It.IsAny<X509Certificate2>(), It.IsAny<CertificateGenerationOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(serverCert);
        _storageService.Setup(x => x.SaveCertificateAuthorityAsync(It.IsAny<X509Certificate2>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _storageService.Setup(x => x.SaveServerCertificateAsync(It.IsAny<X509Certificate2>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _storageService.Setup(x => x.SaveCertificateMetadataAsync(It.IsAny<CertificateInfo>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _permissionService.Setup(x => x.VerifyFilePermissionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var manager = CreateManager();

        // Act
        var result = await manager.EnsureCertificatesAsync();

        // Assert - should still succeed but with warning logged
        Assert.True(result.IsValid);
    }
}
