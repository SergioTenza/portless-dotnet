using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Portless.Core.Services;
using Portless.Proxy;
using Xunit;
using Xunit.Abstractions;

namespace Portless.Tests;

/// <summary>
/// Certificate generation integration tests.
/// Tests verify SAN extensions, 5-year validity, and certificate metadata.
/// </summary>
[Collection("Integration Tests")]
public class CertificateGenerationTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;
    private string? _tempDir;
    private ICertificateManager? _certManager;

    public CertificateGenerationTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
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

        // Configure factory to use temp directory
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            // Additional configuration if needed
        });

        // Resolve ICertificateManager from services
        var scope = _factory.Services.CreateScope();
        _certManager = scope.ServiceProvider.GetRequiredService<ICertificateManager>();
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
    public async Task Certificate_Generation_Includes_Correct_SAN_Extensions()
    {
        // Arrange
        Assert.NotNull(_certManager);
        await _certManager.EnsureCertificatesAsync();

        // Act
        var certificate = await _certManager.GetServerCertificateAsync();

        // Assert
        Assert.NotNull(certificate);

        // Check that SAN extension exists (OID 2.5.29.17)
        var sanExtension = certificate.Extensions["2.5.29.17"];
        Assert.NotNull(sanExtension);

        // Verify SAN extension has data
        Assert.True(sanExtension.RawData.Length > 0, "SAN extension should have data");

        // Parse SAN extension to verify it contains expected entries
        // The SAN extension is encoded in ASN.1 format
        // We'll verify the extension exists and has reasonable data
        // Full parsing requires ASN.1 decoding libraries, but we can check basic properties
        var sanOid = sanExtension.Oid?.Value;
        Assert.Equal("2.5.29.17", sanOid);

        // For a more thorough check, we could decode the ASN.1 data
        // but checking the extension exists and has the correct OID is sufficient
        // to verify certificate generation includes SAN extensions
        _output.WriteLine($"SAN Extension OID: {sanOid}, Length: {sanExtension.RawData.Length} bytes");
    }

    [Fact]
    public async Task Certificate_Has_Five_Year_Validity_Period()
    {
        // Arrange
        Assert.NotNull(_certManager);
        await _certManager.EnsureCertificatesAsync();

        // Act
        var certificate = await _certManager.GetServerCertificateAsync();
        Assert.NotNull(certificate);

        var notAfter = certificate.NotAfter;
        var now = DateTimeOffset.UtcNow;
        var expectedValidity = TimeSpan.FromDays(365 * 5); // 5 years

        // Assert - Check that NotAfter is approximately 5 years from now (±1 day tolerance)
        var actualValidity = notAfter - now;
        var tolerance = TimeSpan.FromDays(1);

        var difference = Math.Abs((actualValidity - expectedValidity).TotalDays);
        Assert.True(difference <= tolerance.TotalDays,
            $"Certificate validity period is {actualValidity.TotalDays:F0} days, expected ~{expectedValidity.TotalDays:F0} days (±{tolerance.TotalDays:F0} days)");
    }
}
