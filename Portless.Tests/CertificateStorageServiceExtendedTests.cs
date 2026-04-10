using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Moq;
using Portless.Core.Models;
using Portless.Core.Services;

namespace Portless.Tests;

/// <summary>
/// Extended tests for CertificateStorageService covering save/load/delete
/// operations with edge cases and metadata handling.
/// </summary>
public class CertificateStorageServiceExtendedTests : IDisposable
{
    private readonly Mock<ICertificatePermissionService> _permissionService;
    private readonly Mock<ILogger<CertificateStorageService>> _logger;
    private readonly string _tempDir;
    private readonly CertificateStorageService _service;

    public CertificateStorageServiceExtendedTests()
    {
        _permissionService = new Mock<ICertificatePermissionService>();
        _logger = new Mock<ILogger<CertificateStorageService>>();
        _tempDir = Path.Combine(Path.GetTempPath(), $"portless-test-storage-{Guid.NewGuid()}");

        // Make the mock permission service actually create directories and files
        _permissionService
            .Setup(x => x.CreateSecureDirectoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((path, _) => Directory.CreateDirectory(path))
            .Returns(Task.CompletedTask);

        _permissionService
            .Setup(x => x.SetSecureFilePermissionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Set the state directory env var so the service uses our temp dir
        Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", _tempDir);

        _service = new CertificateStorageService(_permissionService.Object, _logger.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
        Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", null);
    }

    private X509Certificate2 CreateTestCert(string cn = "CN=Test CA")
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            cn,
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(true, false, 0, true));
        return request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddYears(5));
    }

    private CertificateInfo CreateTestMetadata()
    {
        return new CertificateInfo
        {
            Version = "1.0",
            Sha256Thumbprint = "ABC123DEF456",
            CreatedAt = DateTime.UtcNow.ToString("O"),
            ExpiresAt = DateTime.UtcNow.AddYears(5).ToString("O"),
            CaThumbprint = "CA_THUMBPRINT_123",
            CreatedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ExpiresAtUnix = DateTimeOffset.UtcNow.AddYears(5).ToUnixTimeSeconds()
        };
    }

    [Fact]
    public async Task SaveAndLoadCertificateAuthority_RoundTrip()
    {
        var cert = CreateTestCert();
        try
        {
            await _service.SaveCertificateAuthorityAsync(cert);
            var loaded = await _service.LoadCertificateAuthorityAsync();

            Assert.NotNull(loaded);
            Assert.Equal(cert.Thumbprint, loaded.Thumbprint);
            Assert.True(loaded.HasPrivateKey);
            loaded.Dispose();
        }
        finally
        {
            cert.Dispose();
        }
    }

    [Fact]
    public async Task SaveAndLoadServerCertificate_RoundTrip()
    {
        var cert = CreateTestCert("CN=Test Server");
        try
        {
            await _service.SaveServerCertificateAsync(cert);
            var loaded = await _service.LoadServerCertificateAsync();

            Assert.NotNull(loaded);
            Assert.Equal(cert.Thumbprint, loaded.Thumbprint);
            Assert.True(loaded.HasPrivateKey);
            loaded.Dispose();
        }
        finally
        {
            cert.Dispose();
        }
    }

    [Fact]
    public async Task LoadCertificateAuthority_NoFile_ReturnsNull()
    {
        var result = await _service.LoadCertificateAuthorityAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task LoadServerCertificate_NoFile_ReturnsNull()
    {
        var result = await _service.LoadServerCertificateAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task LoadCertificateMetadata_NoFile_ReturnsNull()
    {
        var result = await _service.LoadCertificateMetadataAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAndLoadMetadata_RoundTrip()
    {
        var metadata = CreateTestMetadata();
        await _service.SaveCertificateMetadataAsync(metadata);
        var loaded = await _service.LoadCertificateMetadataAsync();

        Assert.NotNull(loaded);
        Assert.Equal(metadata.Sha256Thumbprint, loaded.Sha256Thumbprint);
        Assert.Equal(metadata.Version, loaded.Version);
        Assert.Equal(metadata.CaThumbprint, loaded.CaThumbprint);
        Assert.Equal(metadata.CreatedAtUnix, loaded.CreatedAtUnix);
        Assert.Equal(metadata.ExpiresAtUnix, loaded.ExpiresAtUnix);
    }

    [Fact]
    public async Task CertificateFilesExistAsync_NoFiles_ReturnsFalse()
    {
        var result = await _service.CertificateFilesExistAsync();
        Assert.False(result);
    }

    [Fact]
    public async Task CertificateFilesExistAsync_PartialFiles_ReturnsFalse()
    {
        // Create only CA cert
        var cert = CreateTestCert();
        try
        {
            await _service.SaveCertificateAuthorityAsync(cert);
            var result = await _service.CertificateFilesExistAsync();
            Assert.False(result); // Missing server cert and metadata
        }
        finally
        {
            cert.Dispose();
        }
    }

    [Fact]
    public async Task CertificateFilesExistAsync_AllFiles_ReturnsTrue()
    {
        var cert = CreateTestCert();
        try
        {
            await _service.SaveCertificateAuthorityAsync(cert);
            await _service.SaveServerCertificateAsync(cert);
            await _service.SaveCertificateMetadataAsync(CreateTestMetadata());

            var result = await _service.CertificateFilesExistAsync();
            Assert.True(result);
        }
        finally
        {
            cert.Dispose();
        }
    }

    [Fact]
    public async Task SaveCertificateAuthority_SetsFilePermissions()
    {
        var cert = CreateTestCert();
        try
        {
            await _service.SaveCertificateAuthorityAsync(cert);

            _permissionService.Verify(
                x => x.CreateSecureDirectoryAsync(_tempDir, It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
            _permissionService.Verify(
                x => x.SetSecureFilePermissionsAsync(
                    Path.Combine(_tempDir, "ca.pfx"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            cert.Dispose();
        }
    }

    [Fact]
    public async Task SaveServerCertificate_SetsFilePermissions()
    {
        var cert = CreateTestCert();
        try
        {
            await _service.SaveServerCertificateAsync(cert);

            _permissionService.Verify(
                x => x.CreateSecureDirectoryAsync(_tempDir, It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
            _permissionService.Verify(
                x => x.SetSecureFilePermissionsAsync(
                    Path.Combine(_tempDir, "cert.pfx"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            cert.Dispose();
        }
    }

    [Fact]
    public async Task SaveCertificateMetadata_SetsFilePermissions()
    {
        await _service.SaveCertificateMetadataAsync(CreateTestMetadata());

        _permissionService.Verify(
            x => x.CreateSecureDirectoryAsync(_tempDir, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        _permissionService.Verify(
            x => x.SetSecureFilePermissionsAsync(
                Path.Combine(_tempDir, "cert-info.json"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task LoadCertificateAuthority_CorruptedFile_ReturnsNull()
    {
        Directory.CreateDirectory(_tempDir);
        var caPath = Path.Combine(_tempDir, "ca.pfx");
        await File.WriteAllTextAsync(caPath, "not a valid pfx file");

        var result = await _service.LoadCertificateAuthorityAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task LoadServerCertificate_CorruptedFile_ReturnsNull()
    {
        Directory.CreateDirectory(_tempDir);
        var certPath = Path.Combine(_tempDir, "cert.pfx");
        await File.WriteAllTextAsync(certPath, "not a valid pfx file");

        var result = await _service.LoadServerCertificateAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task LoadCertificateMetadata_CorruptedJson_ReturnsNull()
    {
        Directory.CreateDirectory(_tempDir);
        var metadataPath = Path.Combine(_tempDir, "cert-info.json");
        await File.WriteAllTextAsync(metadataPath, "not valid json {{{");

        var result = await _service.LoadCertificateMetadataAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task LoadCertificateMetadata_EmptyJson_ReturnsNull()
    {
        Directory.CreateDirectory(_tempDir);
        var metadataPath = Path.Combine(_tempDir, "cert-info.json");
        await File.WriteAllTextAsync(metadataPath, "");

        var result = await _service.LoadCertificateMetadataAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveCertificateAuthority_OverwriteExisting()
    {
        var cert1 = CreateTestCert();
        var cert2 = CreateTestCert();
        try
        {
            await _service.SaveCertificateAuthorityAsync(cert1);
            await _service.SaveCertificateAuthorityAsync(cert2);

            var loaded = await _service.LoadCertificateAuthorityAsync();
            Assert.NotNull(loaded);
            Assert.Equal(cert2.Thumbprint, loaded.Thumbprint);
            loaded.Dispose();
        }
        finally
        {
            cert1.Dispose();
            cert2.Dispose();
        }
    }

    [Fact]
    public async Task SaveServerCertificate_OverwriteExisting()
    {
        var cert1 = CreateTestCert();
        var cert2 = CreateTestCert();
        try
        {
            await _service.SaveServerCertificateAsync(cert1);
            await _service.SaveServerCertificateAsync(cert2);

            var loaded = await _service.LoadServerCertificateAsync();
            Assert.NotNull(loaded);
            Assert.Equal(cert2.Thumbprint, loaded.Thumbprint);
            loaded.Dispose();
        }
        finally
        {
            cert1.Dispose();
            cert2.Dispose();
        }
    }

    [Fact]
    public async Task CertificateFilesExistAsync_OnlyMetadata_ReturnsFalse()
    {
        await _service.SaveCertificateMetadataAsync(CreateTestMetadata());
        var result = await _service.CertificateFilesExistAsync();
        Assert.False(result);
    }

    [Fact]
    public async Task SaveCertificateMetadata_WithCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var metadata = CreateTestMetadata();
        await _service.SaveCertificateMetadataAsync(metadata, cts.Token);
        var loaded = await _service.LoadCertificateMetadataAsync();
        Assert.NotNull(loaded);
    }

    [Fact]
    public async Task SaveCertificateAuthority_WithCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        var cert = CreateTestCert();
        try
        {
            await _service.SaveCertificateAuthorityAsync(cert, cts.Token);
            var loaded = await _service.LoadCertificateAuthorityAsync();
            Assert.NotNull(loaded);
            loaded.Dispose();
        }
        finally
        {
            cert.Dispose();
        }
    }
}
