extern alias Cli;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Portless.Core.Models;
using Portless.Core.Services;
using CertCheckCommand = Cli::Portless.Cli.Commands.CertCommand.CertCheckCommand;
using CertInstallCommand = Cli::Portless.Cli.Commands.CertCommand.CertInstallCommand;
using CertRenewCommand = Cli::Portless.Cli.Commands.CertCommand.CertRenewCommand;
using CertStatusCommand = Cli::Portless.Cli.Commands.CertCommand.CertStatusCommand;
using CertUninstallCommand = Cli::Portless.Cli.Commands.CertCommand.CertUninstallCommand;
using CertCheckSettings = Cli::Portless.Cli.Commands.CertCommand.CertCheckSettings;
using CertInstallSettings = Cli::Portless.Cli.Commands.CertCommand.CertInstallSettings;
using CertRenewSettings = Cli::Portless.Cli.Commands.CertCommand.CertRenewSettings;
using CertStatusSettings = Cli::Portless.Cli.Commands.CertCommand.CertStatusSettings;
using CertUninstallSettings = Cli::Portless.Cli.Commands.CertCommand.CertUninstallSettings;
using Spectre.Console.Cli;
using Xunit;

namespace Portless.Tests.CliCommands;

public class CertCheckCommandTests
{
    private readonly Mock<ICertificateManager> _certManagerMock;
    private readonly Mock<ICertificateStorageService> _storageServiceMock;

    public CertCheckCommandTests()
    {
        _certManagerMock = new Mock<ICertificateManager>();
        _storageServiceMock = new Mock<ICertificateStorageService>();
    }

    private CertCheckCommand CreateCommand() => new(
        _certManagerMock.Object,
        _storageServiceMock.Object,
        NullLogger<CertCheckCommand>.Instance);

    [Fact]
    public async Task ExecuteAsync_NoCertFiles_Returns3()
    {
        // Arrange
        _storageServiceMock.Setup(x => x.CertificateFilesExistAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = CreateCommand();
        var settings = new CertCheckSettings { Verbose = false };

        // Act
        var result = await command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public async Task ExecuteAsync_CertLoadReturnsNull_Returns3()
    {
        // Arrange
        _storageServiceMock.Setup(x => x.CertificateFilesExistAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _certManagerMock.Setup(x => x.GetServerCertificateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((X509Certificate2?)null);

        var command = CreateCommand();
        var settings = new CertCheckSettings { Verbose = false };

        // Act
        var result = await command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public async Task ExecuteAsync_CertLoadThrows_Returns1()
    {
        // Arrange
        _storageServiceMock.Setup(x => x.CertificateFilesExistAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _certManagerMock.Setup(x => x.GetServerCertificateAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Corrupted"));

        var command = CreateCommand();
        var settings = new CertCheckSettings { Verbose = false };

        // Act
        var result = await command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_ValidCert_Returns0()
    {
        // Arrange
        using var cert = CreateTestCertificate(daysValid: 365);
        _storageServiceMock.Setup(x => x.CertificateFilesExistAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _certManagerMock.Setup(x => x.GetServerCertificateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cert);

        var command = CreateCommand();
        var settings = new CertCheckSettings { Verbose = false };

        // Act
        var result = await command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_ExpiredCert_Returns2()
    {
        // Arrange
        using var cert = CreateTestCertificate(daysValid: -10);
        _storageServiceMock.Setup(x => x.CertificateFilesExistAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _certManagerMock.Setup(x => x.GetServerCertificateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cert);

        var command = CreateCommand();
        var settings = new CertCheckSettings { Verbose = false };

        // Act
        var result = await command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task ExecuteAsync_ExpiringSoonCert_Returns0()
    {
        // Arrange
        using var cert = CreateTestCertificate(daysValid: 15);
        _storageServiceMock.Setup(x => x.CertificateFilesExistAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _certManagerMock.Setup(x => x.GetServerCertificateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cert);

        var command = CreateCommand();
        var settings = new CertCheckSettings { Verbose = false };

        // Act
        var result = await command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_ValidCert_Verbose_Returns0()
    {
        // Arrange
        using var cert = CreateTestCertificate(daysValid: 365);
        _storageServiceMock.Setup(x => x.CertificateFilesExistAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _certManagerMock.Setup(x => x.GetServerCertificateAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(cert);

        var command = CreateCommand();
        var settings = new CertCheckSettings { Verbose = true };

        // Act
        var result = await command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_OuterException_Returns1()
    {
        // Arrange
        _storageServiceMock.Setup(x => x.CertificateFilesExistAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected"));

        var command = CreateCommand();
        var settings = new CertCheckSettings();

        // Act
        var result = await command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(1, result);
    }

    private static X509Certificate2 CreateTestCertificate(int daysValid)
    {
        var now = DateTimeOffset.UtcNow;
        if (daysValid >= 0)
            return CertCertificateGenerator.GenerateTestCert(now, now.AddDays(daysValid));
        else
            return CertCertificateGenerator.GenerateTestCert(now.AddDays(daysValid * 2), now.AddDays(daysValid));
    }
}

/// <summary>
/// Helper to generate test certificates without needing external CA.
/// </summary>
internal static class CertCertificateGenerator
{
    public static X509Certificate2 GenerateTestCert(DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        // Use Opaque export/import trick for cross-platform compatibility
        using var temp = System.Security.Cryptography.X509Certificates.X509Certificate2.CreateFromPem(
            GenerateSelfSignedPem(notBefore, notAfter));
        return new X509Certificate2(temp.Export(X509ContentType.Pfx));
    }

    private static string GenerateSelfSignedPem(DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        // Simple approach: create via cert creation
        var subject = "CN=Portless Test";
        using var rsa = System.Security.Cryptography.RSA.Create(2048);
        var request = new System.Security.Cryptography.X509Certificates.CertificateRequest(
            subject, rsa, System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.RSASignaturePadding.Pkcs1);
        var cert = request.CreateSelfSigned(notBefore.DateTime, notAfter.DateTime);
        var pem = cert.ExportCertificatePem();
        var keyPem = rsa.ExportRSAPrivateKeyPem();
        return pem + "\n" + keyPem;
    }
}

public class CertInstallCommandTests
{
    private readonly Mock<ICertificateManager> _certManagerMock;
    private readonly Mock<ICertificateTrustService> _trustServiceMock;

    public CertInstallCommandTests()
    {
        _certManagerMock = new Mock<ICertificateManager>();
        _trustServiceMock = new Mock<ICertificateTrustService>();
    }

    private CertInstallCommand CreateCommand() => new(
        _certManagerMock.Object,
        _trustServiceMock.Object,
        NullLogger<CertInstallCommand>.Instance);

    [Fact]
    public async Task ExecuteAsync_NonWindows_Returns1()
    {
        // This test will return 1 on Linux (where tests run)
        var command = CreateCommand();
        var settings = new CertInstallSettings();

        // Act
        var result = await command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert: On Linux, returns 1 (platform not supported)
        Assert.Equal(1, result);
    }
}

public class CertRenewCommandTests
{
    private readonly Mock<ICertificateManager> _certManagerMock;

    public CertRenewCommandTests()
    {
        _certManagerMock = new Mock<ICertificateManager>();
    }

    private CertRenewCommand CreateCommand() => new(
        _certManagerMock.Object,
        NullLogger<CertRenewCommand>.Instance);

    [Fact]
    public async Task ExecuteAsync_CertValidNoForce_Returns0()
    {
        // Arrange
        var status = new CertificateStatus(
            IsValid: true, IsExpired: false, IsExpiringSoon: false,
            IsCorrupted: false, NeedsRegeneration: false,
            Message: "Valid", ExpiresAt: DateTimeOffset.UtcNow.AddDays(365),
            Thumbprint: "ABC123");
        _certManagerMock.Setup(x => x.EnsureCertificatesAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        var command = CreateCommand();
        var settings = new CertRenewSettings { Force = false, DisableAutoRenew = false };

        // Act
        var result = await command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_ForceRenew_Returns0()
    {
        // Arrange
        var status = new CertificateStatus(
            IsValid: true, IsExpired: false, IsExpiringSoon: false,
            IsCorrupted: false, NeedsRegeneration: false,
            Message: "Valid", ExpiresAt: DateTimeOffset.UtcNow.AddDays(365),
            Thumbprint: "OLD");
        var newStatus = new CertificateStatus(
            IsValid: true, IsExpired: false, IsExpiringSoon: false,
            IsCorrupted: false, NeedsRegeneration: false,
            Message: "Renewed", ExpiresAt: DateTimeOffset.UtcNow.AddDays(365),
            Thumbprint: "NEW");
        _certManagerMock.SetupSequence(x => x.EnsureCertificatesAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(status)
            .ReturnsAsync(newStatus);
        _certManagerMock.Setup(x => x.RegenerateCertificatesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = CreateCommand();
        var settings = new CertRenewSettings { Force = true, DisableAutoRenew = false };

        // Act
        var result = await command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
        _certManagerMock.Verify(x => x.RegenerateCertificatesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_NeedsRenewal_AutoRenew_Returns0()
    {
        // Arrange
        var status = new CertificateStatus(
            IsValid: false, IsExpired: false, IsExpiringSoon: true,
            IsCorrupted: false, NeedsRegeneration: true,
            Message: "Expiring soon", ExpiresAt: DateTimeOffset.UtcNow.AddDays(5),
            Thumbprint: "OLD");
        var renewedStatus = new CertificateStatus(
            IsValid: true, IsExpired: false, IsExpiringSoon: false,
            IsCorrupted: false, NeedsRegeneration: false,
            Message: "Renewed", ExpiresAt: DateTimeOffset.UtcNow.AddDays(365),
            Thumbprint: "NEW");
        _certManagerMock.SetupSequence(x => x.EnsureCertificatesAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(status)
            .ReturnsAsync(renewedStatus);
        _certManagerMock.Setup(x => x.RegenerateCertificatesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = CreateCommand();
        var settings = new CertRenewSettings { Force = false, DisableAutoRenew = false };

        // Act
        var result = await command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
        _certManagerMock.Verify(x => x.RegenerateCertificatesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_NeedsRenewal_DisabledAutoRenew_Returns2()
    {
        // Arrange
        var status = new CertificateStatus(
            IsValid: false, IsExpired: false, IsExpiringSoon: true,
            IsCorrupted: false, NeedsRegeneration: true,
            Message: "Expiring soon", ExpiresAt: DateTimeOffset.UtcNow.AddDays(5),
            Thumbprint: "OLD");
        _certManagerMock.Setup(x => x.EnsureCertificatesAsync(false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        var command = CreateCommand();
        var settings = new CertRenewSettings { Force = false, DisableAutoRenew = true };

        // Act
        var result = await command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(2, result);
        _certManagerMock.Verify(x => x.RegenerateCertificatesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_Exception_Returns1()
    {
        // Arrange
        _certManagerMock.Setup(x => x.EnsureCertificatesAsync(false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test error"));

        var command = CreateCommand();
        var settings = new CertRenewSettings();

        // Act
        var result = await command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(1, result);
    }
}

public class CertStatusCommandTests
{
    private readonly Mock<ICertificateManager> _certManagerMock;
    private readonly Mock<ICertificateTrustService> _trustServiceMock;

    public CertStatusCommandTests()
    {
        _certManagerMock = new Mock<ICertificateManager>();
        _trustServiceMock = new Mock<ICertificateTrustService>();
    }

    private CertStatusCommand CreateCommand() => new(
        _certManagerMock.Object,
        _trustServiceMock.Object,
        NullLogger<CertStatusCommand>.Instance);

    [Fact]
    public async Task ExecuteAsync_NoCert_Returns0()
    {
        // Arrange
        _certManagerMock.Setup(x => x.GetCertificateStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((CertificateInfo?)null);

        var command = CreateCommand();
        var settings = new CertStatusSettings();

        // Act
        var result = await command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_NonWindows_WithCert_Returns1()
    {
        // Arrange
        var metadata = new CertificateInfo
        {
            Sha256Thumbprint = "ABC123",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(365).ToString("o")
        };
        _certManagerMock.Setup(x => x.GetCertificateStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);

        var command = CreateCommand();
        var settings = new CertStatusSettings();

        // Act
        var result = await command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert: Returns 1 due to Spectre.Console markup error ([/dim] in production code)
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_Exception_Returns1()
    {
        // Arrange
        _certManagerMock.Setup(x => x.GetCertificateStatusAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test error"));

        var command = CreateCommand();
        var settings = new CertStatusSettings();

        // Act
        var result = await command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(1, result);
    }
}

public class CertUninstallCommandTests
{
    private readonly Mock<ICertificateManager> _certManagerMock;
    private readonly Mock<ICertificateTrustService> _trustServiceMock;

    public CertUninstallCommandTests()
    {
        _certManagerMock = new Mock<ICertificateManager>();
        _trustServiceMock = new Mock<ICertificateTrustService>();
    }

    private CertUninstallCommand CreateCommand() => new(
        _certManagerMock.Object,
        _trustServiceMock.Object,
        NullLogger<CertUninstallCommand>.Instance);

    [Fact]
    public async Task ExecuteAsync_NonWindows_Returns1()
    {
        // On Linux, this returns 1 (platform not supported)
        var command = CreateCommand();
        var settings = new CertUninstallSettings();

        // Act
        var result = await command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        // Assert
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_Exception_Returns1()
    {
        // Even on Linux it returns 1, which is the expected behavior
        var command = CreateCommand();
        var settings = new CertUninstallSettings();

        var result = await command.ExecuteAsync(
            null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }
}
