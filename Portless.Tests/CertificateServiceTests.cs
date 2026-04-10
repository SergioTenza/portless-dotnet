using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Moq;
using Portless.Core.Services;
using Xunit;

namespace Portless.Tests;

public class CertificateServiceTests
{
    private readonly Mock<ILogger<CertificateService>> _logger;
    private readonly string _tempDir;

    public CertificateServiceTests()
    {
        _logger = new Mock<ILogger<CertificateService>>();
        _tempDir = Path.Combine(Path.GetTempPath(), $"portless-test-certsrv-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    private CertificateService CreateService()
    {
        // Set state directory to temp dir
        Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", _tempDir);
        return new CertificateService(_logger.Object);
    }

    private X509Certificate2 GenerateTestCert(string cn = "Test CA")
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            $"CN={cn}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(true, false, 0, true));
        var cert = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(5));
        return cert;
    }

    [Fact]
    public async Task LoadCertificateAuthorityAsync_NoFile_ReturnsNull()
    {
        var service = CreateService();
        var result = await service.LoadCertificateAuthorityAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task LoadCertificateAuthorityAsync_ValidFile_ReturnsCert()
    {
        // Arrange
        var cert = GenerateTestCert();
        var certPath = Path.Combine(_tempDir, "ca.pfx");
        await File.WriteAllBytesAsync(certPath, cert.Export(X509ContentType.Pfx));

        var service = CreateService();

        // Act
        var result = await service.LoadCertificateAuthorityAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Test CA", result.Subject);
    }

    [Fact]
    public async Task LoadCertificateAuthorityAsync_CorruptedFile_ReturnsNull()
    {
        // Arrange
        var certPath = Path.Combine(_tempDir, "ca.pfx");
        await File.WriteAllBytesAsync(certPath, "not a real cert"u8.ToArray());

        var service = CreateService();

        // Act
        var result = await service.LoadCertificateAuthorityAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task LoadCertificateAuthorityAsync_EmptyFile_ReturnsNull()
    {
        // Arrange
        var certPath = Path.Combine(_tempDir, "ca.pfx");
        await File.WriteAllBytesAsync(certPath, []);

        var service = CreateService();

        // Act
        var result = await service.LoadCertificateAuthorityAsync();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateCertificateAuthorityAsync_ReturnsValidCert()
    {
        var service = CreateService();
        var options = new Portless.Core.Models.CertificateGenerationOptions
        {
            SubjectName = "Test CA",
            CaKeySize = 2048,
            ValidityYears = 1
        };

        var cert = await service.GenerateCertificateAuthorityAsync(options);

        Assert.NotNull(cert);
        Assert.Contains("Test CA", cert.Subject);
        Assert.True(cert.HasPrivateKey);
        Assert.True(cert.NotAfter > DateTime.UtcNow);
    }

    [Fact]
    public async Task GenerateWildcardCertificateAsync_ReturnsValidCert()
    {
        var service = CreateService();
        var caCert = await service.GenerateCertificateAuthorityAsync(new Portless.Core.Models.CertificateGenerationOptions
        {
            CaKeySize = 2048,
            ValidityYears = 1
        });

        var serverCert = await service.GenerateWildcardCertificateAsync(caCert, new Portless.Core.Models.CertificateGenerationOptions
        {
            ServerKeySize = 2048,
            ValidityYears = 1
        });

        Assert.NotNull(serverCert);
        Assert.Contains("*.localhost", serverCert.Subject);
        Assert.True(serverCert.HasPrivateKey);
    }

    [Fact]
    public async Task GenerateWildcardCertificateAsync_WithoutCAPrivateKey_Throws()
    {
        var service = CreateService();
        // Create a cert without private key
        var caCert = GenerateTestCert();
        var certWithoutKey = new X509Certificate2(caCert.Export(X509ContentType.Cert));

        var options = new Portless.Core.Models.CertificateGenerationOptions
        {
            ServerKeySize = 2048,
            ValidityYears = 1
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GenerateWildcardCertificateAsync(certWithoutKey, options));
    }

    [Fact]
    public async Task GenerateCertificateAuthorityAsync_CorrectKeySize()
    {
        var service = CreateService();
        var options = new Portless.Core.Models.CertificateGenerationOptions
        {
            CaKeySize = 2048,
            ValidityYears = 1
        };

        var cert = await service.GenerateCertificateAuthorityAsync(options);
        Assert.NotNull(cert);

        using var rsa = cert.GetRSAPublicKey();
        Assert.NotNull(rsa);
        Assert.Equal(2048, rsa.KeySize);
    }
}
