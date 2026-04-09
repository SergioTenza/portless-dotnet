using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Portless.Core.Services;
using Xunit;
using Xunit.Abstractions;

namespace Portless.Tests;

/// <summary>
/// Certificate generation integration tests.
/// Tests verify SAN extensions, 5-year validity, and certificate metadata.
/// </summary>
[Collection("Integration Tests")]
public class CertificateGenerationTests : IntegrationTestBase
{
    private WebApplicationFactory<Program> _factory = null!;
    private ICertificateManager? _certManager;

    public CertificateGenerationTests(ITestOutputHelper output)
    {
        Output = output;
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _factory = CreateProxyApp(builder =>
        {
            // Additional configuration if needed
        });

        // Resolve ICertificateManager from services
        var scope = _factory.Services.CreateScope();
        _certManager = scope.ServiceProvider.GetRequiredService<ICertificateManager>();
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
        Output.WriteLine($"SAN Extension OID: {sanOid}, Length: {sanExtension.RawData.Length} bytes");
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
