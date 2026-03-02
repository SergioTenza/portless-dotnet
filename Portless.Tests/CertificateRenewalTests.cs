using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Portless.Core.Models;
using Portless.Core.Services;
using Portless.Proxy;
using Xunit;
using Xunit.Abstractions;

namespace Portless.Tests;

/// <summary>
/// Certificate renewal integration tests.
/// Tests verify certificate regeneration before expiration and metadata updates.
/// </summary>
public class CertificateRenewalTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;
    private string? _tempDir;
    private ICertificateManager? _certManager;
    private ICertificateStorageService? _storageService;

    public CertificateRenewalTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _output = output;
    }

    public async Task InitializeAsync()
    {
        // Create temp directory
        _tempDir = Path.Combine(Path.GetTempPath(), $"portless-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);

        // Set environment variable before creating factory
        Environment.SetEnvironmentVariable("PORTLESS_STATE_DIR", _tempDir);

        // Configure factory
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // Additional configuration if needed
        });

        // Resolve services
        var scope = _factory.Services.CreateScope();
        _certManager = scope.ServiceProvider.GetRequiredService<ICertificateManager>();
        _storageService = scope.ServiceProvider.GetRequiredService<ICertificateStorageService>();
    }

    public async Task DisposeAsync()
    {
        // Delete temp directory with try-catch
        if (_tempDir != null && Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Warning: Failed to delete temp directory {_tempDir}: {ex.Message}");
            }
        }
    }

    [Fact]
    public async Task Certificate_Renewal_Generates_New_Certificate()
    {
        // Arrange
        Assert.NotNull(_certManager);
        await _certManager.EnsureCertificatesAsync();

        var initialCert = await _certManager.GetServerCertificateAsync();
        Assert.NotNull(initialCert);

        var initialThumbprint = initialCert.GetCertHashString(HashAlgorithmName.SHA256);
        _output.WriteLine($"Initial thumbprint: {initialThumbprint}");

        // Act
        await _certManager.RegenerateCertificatesAsync();

        var newCert = await _certManager.GetServerCertificateAsync();
        Assert.NotNull(newCert);

        var newThumbprint = newCert.GetCertHashString(HashAlgorithmName.SHA256);
        _output.WriteLine($"New thumbprint: {newThumbprint}");

        // Assert - New certificate has different thumbprint
        Assert.NotEqual(initialThumbprint, newThumbprint);

        // Assert - New certificate NotAfter is ~5 years from now
        var now = DateTimeOffset.UtcNow;
        var expectedValidity = TimeSpan.FromDays(365 * 5);
        var actualValidity = newCert.NotAfter - now;
        var tolerance = TimeSpan.FromDays(1);

        var difference = Math.Abs((actualValidity - expectedValidity).TotalDays);
        Assert.True(difference <= tolerance.TotalDays,
            $"Certificate validity period is {actualValidity.TotalDays:F0} days, expected ~{expectedValidity.TotalDays:F0} days (±{tolerance.TotalDays:F0} days)");
    }

    [Fact]
    public async Task Certificate_Renewal_Updates_Metadata_File()
    {
        // Arrange
        Assert.NotNull(_certManager);
        Assert.NotNull(_storageService);
        await _certManager.EnsureCertificatesAsync();

        var initialCert = await _certManager.GetServerCertificateAsync();
        var initialThumbprint = initialCert.GetCertHashString(HashAlgorithmName.SHA256);
        _output.WriteLine($"Initial thumbprint: {initialThumbprint}");

        var initialMetadata = await _storageService.LoadCertificateMetadataAsync();
        Assert.NotNull(initialMetadata);
        Assert.Equal(initialThumbprint, initialMetadata.Sha256Thumbprint);

        // Act
        await _certManager.RegenerateCertificatesAsync();

        // Wait a bit for file update
        await Task.Delay(100);

        var newMetadata = await _storageService.LoadCertificateMetadataAsync();

        // Assert - Metadata file exists
        Assert.NotNull(newMetadata);

        // Assert - New thumbprint in metadata
        Assert.NotEqual(initialThumbprint, newMetadata.Sha256Thumbprint);
        _output.WriteLine($"New metadata thumbprint: {newMetadata.Sha256Thumbprint}");

        // Assert - Expiration date is approximately 5 years from now
        var actualExpiration = DateTimeOffset.FromUnixTimeSeconds(newMetadata.ExpiresAtUnix);
        var expectedExpiration = DateTimeOffset.UtcNow.AddYears(5);
        var daysDiff = (actualExpiration - expectedExpiration).TotalDays;
        Assert.InRange(Math.Abs(daysDiff), 0, 2); // Within 2 days of 5 years
        _output.WriteLine($"Certificate expires at: {actualExpiration:u} (expected ~{expectedExpiration:u})");

        // Verify the new thumbprint matches the actual certificate
        var newCert = await _certManager.GetServerCertificateAsync();
        var actualThumbprint = newCert.GetCertHashString(HashAlgorithmName.SHA256);
        Assert.Equal(actualThumbprint, newMetadata.Sha256Thumbprint);
    }

    [Fact]
    public async Task Certificate_Status_Metadata_Loads_Correctly()
    {
        // Arrange
        Assert.NotNull(_certManager);
        await _certManager.EnsureCertificatesAsync();

        var cert = await _certManager.GetServerCertificateAsync();
        Assert.NotNull(cert);

        var actualThumbprint = cert.GetCertHashString(HashAlgorithmName.SHA256);
        _output.WriteLine($"Certificate thumbprint: {actualThumbprint}");

        // Act - Load certificate metadata
        var metadata = await _certManager.GetCertificateStatusAsync();

        // Assert
        Assert.NotNull(metadata);
        Assert.NotEmpty(metadata.Version);
        Assert.Equal(actualThumbprint, metadata.Sha256Thumbprint);
        Assert.NotEmpty(metadata.CreatedAt);
        Assert.NotEmpty(metadata.ExpiresAt);
        Assert.NotEmpty(metadata.CaThumbprint);

        // Verify the expiration date is approximately 5 years from now
        var now = DateTimeOffset.UtcNow;
        var expiresAt = DateTimeOffset.Parse(metadata.ExpiresAt);
        var daysUntilExpiration = (expiresAt - now).Days;
        var expectedDays = 365 * 5;

        Assert.InRange(daysUntilExpiration, expectedDays - 2, expectedDays + 2);
        _output.WriteLine($"Certificate expires in {daysUntilExpiration} days ({metadata.ExpiresAt})");
    }
}
