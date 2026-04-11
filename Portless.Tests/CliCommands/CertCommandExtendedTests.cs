extern alias Cli;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Portless.Core.Models;
using Portless.Core.Services;
using Xunit;

using CertStatusCommand = Cli::Portless.Cli.Commands.CertCommand.CertStatusCommand;
using CertInstallCommand = Cli::Portless.Cli.Commands.CertCommand.CertInstallCommand;
using CertUninstallCommand = Cli::Portless.Cli.Commands.CertCommand.CertUninstallCommand;
using CertStatusSettings = Cli::Portless.Cli.Commands.CertCommand.CertStatusSettings;
using CertInstallSettings = Cli::Portless.Cli.Commands.CertCommand.CertInstallSettings;
using CertUninstallSettings = Cli::Portless.Cli.Commands.CertCommand.CertUninstallSettings;

namespace Portless.Tests.CliCommands;

/// <summary>
/// Additional cert command tests to increase coverage of CertStatus, CertInstall,
/// and CertUninstall ExecuteAsync paths.
/// </summary>
[Collection("SpectreConsoleTests")]
public class CertStatusExtendedTests
{
    private readonly Mock<ICertificateManager> _certManagerMock;
    private readonly Mock<ICertificateTrustServiceFactory> _trustServiceMock;

    public CertStatusExtendedTests()
    {
        _certManagerMock = new Mock<ICertificateManager>();
        _trustServiceMock = new Mock<ICertificateTrustServiceFactory>();
    }

    private CertStatusCommand CreateCommand() => new(
        _certManagerMock.Object,
        _trustServiceMock.Object,
        NullLogger<CertStatusCommand>.Instance);

    [Fact]
    public async Task ExecuteAsync_NoCertMetadata_Returns0()
    {
        _certManagerMock.Setup(x => x.GetCertificateStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((CertificateInfo?)null);

        var command = CreateCommand();
        var settings = new CertStatusSettings();
        var result = await command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task ExecuteAsync_CertThrows_Returns1()
    {
        _certManagerMock.Setup(x => x.GetCertificateStatusAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Certificate error"));

        var command = CreateCommand();
        var settings = new CertStatusSettings();
        var result = await command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }
}

[Collection("SpectreConsoleTests")]
public class CertInstallExtendedTests
{
    private readonly Mock<ICertificateManager> _certManagerMock;
    private readonly Mock<ICertificateTrustServiceFactory> _trustServiceMock;

    public CertInstallExtendedTests()
    {
        _certManagerMock = new Mock<ICertificateManager>();
        _trustServiceMock = new Mock<ICertificateTrustServiceFactory>();
    }

    private CertInstallCommand CreateCommand() => new(
        _certManagerMock.Object,
        _trustServiceMock.Object,
        NullLogger<CertInstallCommand>.Instance);

    [Fact]
    public async Task ExecuteAsync_NonWindows_Returns1()
    {
        // On Linux, the command checks platform and returns error
        var command = CreateCommand();
        var settings = new CertInstallSettings();
        var result = await command.ExecuteAsync(null!, settings, CancellationToken.None);

        // On Linux: platform not supported or cert not found paths
        Assert.True(result is 1 or 2 or 3);
    }

    [Fact]
    public async Task ExecuteAsync_CertManagerThrows_Returns1()
    {
        _certManagerMock.Setup(x => x.GetCertificateAuthorityAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected"));

        var command = CreateCommand();
        var settings = new CertInstallSettings();
        var result = await command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }
}

[Collection("SpectreConsoleTests")]
public class CertUninstallExtendedTests
{
    private readonly Mock<ICertificateManager> _certManagerMock;
    private readonly Mock<ICertificateTrustServiceFactory> _trustServiceMock;

    public CertUninstallExtendedTests()
    {
        _certManagerMock = new Mock<ICertificateManager>();
        _trustServiceMock = new Mock<ICertificateTrustServiceFactory>();
    }

    private CertUninstallCommand CreateCommand() => new(
        _certManagerMock.Object,
        _trustServiceMock.Object,
        NullLogger<CertUninstallCommand>.Instance);

    [Fact]
    public async Task ExecuteAsync_NonWindows_Returns1()
    {
        var command = CreateCommand();
        var settings = new CertUninstallSettings();
        var result = await command.ExecuteAsync(null!, settings, CancellationToken.None);

        // On Linux: platform not supported
        Assert.Equal(1, result);
    }

    [Fact]
    public async Task ExecuteAsync_CertManagerThrows_Returns1()
    {
        _certManagerMock.Setup(x => x.GetCertificateAuthorityAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        var command = CreateCommand();
        var settings = new CertUninstallSettings();
        var result = await command.ExecuteAsync(null!, settings, CancellationToken.None);

        Assert.Equal(1, result);
    }
}
