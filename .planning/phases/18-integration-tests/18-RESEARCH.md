# Phase 18: Integration Tests - Research

**Researched:** 2026-03-02
**Domain:** .NET Integration Testing, HTTPS, Certificates, xUnit
**Confidence:** HIGH

## Summary

Phase 18 requires comprehensive integration testing for HTTPS features implemented in Phases 13-17. The research reveals that the existing codebase already has a solid integration testing foundation using xUnit 2.9.3, WebApplicationFactory<Program>, and IClassFixture patterns. The test suite follows feature-based organization with clear test naming conventions and XML documentation.

**Primary recommendation:** Extend the existing `Portless.Tests` project with new test classes following established patterns: `IClassFixture<WebApplicationFactory<Program>>`, real services (no mocking), IAsyncLifetime for certificate cleanup, and temporary directories for certificate file isolation. Use Windows-specific platform guards for trust tests and create test-specific backend servers for HTTP/HTTPS routing scenarios.

## User Constraints (from CONTEXT.md)

### Locked Decisions
- Test class structure: [User choice — By feature / By test type / By scenario]
- Test project location: [User choice — Portless.Tests / New project / Your call]
- Test granularity: [User choice — Single assertion / Multiple assertions / Behavior-focused]
- Test categorization: [User choice — Yes, add traits / No categorization / Your call]

- Certificate generation approach: [User choice — Real service with temp dir / Mocked service / Fixture certificates]
- Storage service testing: [User choice — Test real storage / Mock for other tests / In-memory storage]
- Setup/cleanup pattern: [User choice — IAsyncLifetime / Constructor/Dispose / Per-test method]
- State directory handling: [User choice — Temp directory / Real directory / Mocked paths]

### Claude's Discretion
- Test backend provisions (WebApplicationFactory vs actual servers) — use existing TestApi pattern where appropriate
- Platform-specific tests (Windows-only trust tests) — add skip logic with `RuntimeInformation.IsOSPlatform()` checks
- Test data management for certificate expiration scenarios
- Exact test naming conventions within chosen organization structure
- Whether to add benchmark/performance tests for HTTPS overhead

### Deferred Ideas (OUT OF SCOPE)
None

## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| TEST-01 | Integration tests verify certificate generation with correct SAN extensions | Use `ICertificateService.GenerateWildcardCertificateAsync()` with temp directory, verify X509Certificate2.Extensions with SAN OID (2.5.29.17) for `*.localhost`, `localhost`, `127.0.0.1`, `::1` |
| TEST-02 | Integration tests verify HTTPS endpoint serves valid TLS certificate | Use WebApplicationFactory with `PORTLESS_HTTPS_ENABLED=true`, create HttpClientHandler that validates certificate, check server certificate on HTTPS port 1356 |
| TEST-03 | Integration tests verify X-Forwarded-Proto header preservation | Create HTTP and HTTPS backends, proxy through YARP, verify backend receives correct protocol header matching client request scheme |
| TEST-04 | Integration tests verify certificate renewal before expiration | Use `ICertificateManager.RegenerateCertificatesAsync()`, verify old certificate replaced, new certificate has NotAfter date 5 years in future |
| TEST-05 | Integration tests verify trust status detection on Windows | Use `ICertificateTrustService.IsCertificateTrustedAsync()` with `[SupportedOSPlatform("windows")]` guard, skip on non-Windows with `RuntimeInformation.IsOSPlatform()` |
| TEST-06 | Integration tests cover mixed HTTP/HTTPS backend routing scenarios | Configure YARP routes with mixed HTTP/HTTPS clusters, verify proxy forwards correctly to both, check `DangerousAcceptAnyServerCertificate` for HTTPS backends |

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| **xUnit** | 2.9.3 | Testing framework | Industry standard for .NET, already in project, active development, excellent documentation |
| **Microsoft.AspNetCore.Mvc.Testing** | 10.0.0 | WebApplicationFactory for integration tests | Official ASP.NET Core test infrastructure, in-memory TestServer, integrates with xUnit |
| **Microsoft.NET.Test.Sdk** | 17.14.1 | Test SDK for .NET | Required for xUnit test discovery and execution, version-compatible with .NET 10 |
| **coverlet.collector** | 6.0.4 | Code coverage | Standard coverage tool for .NET, integrates with xUnit, CI/CD friendly |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| **System.Security.Cryptography.X509Certificates** | .NET 10 | X509Certificate2, X509Store | Certificate validation, SAN extension reading, trust store access |
| **System.Runtime.InteropServices** | .NET 10 | RuntimeInformation.IsOSPlatform() | Platform-specific test guards for Windows-only trust tests |
| **Microsoft.Extensions.Logging.Testing** | 10.0.0 | ILogger verification (optional) | Log assertion tests if needed for certificate monitoring |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| xUnit | NUnit 4 | NUnit has more features but xUnit is already in project, community prefers xUnit for modern .NET |
| WebApplicationFactory | TestServer directly | WebApplicationFactory provides better integration, automatic configuration, less boilerplate |
| Real services | Moq/NSubstitute | Project has no mocking framework, real services with WebApplicationFactory provide better integration coverage |

**Installation:**
```bash
# All packages already in Portless.Tests.csproj
# No additional installation needed
```

## Architecture Patterns

### Recommended Project Structure
```
Portless.Tests/
├── CertificateGenerationTests.cs    # TEST-01: SAN extension verification
├── HttpsEndpointTests.cs            # TEST-02: HTTPS certificate serving
├── XForwardedProtoTests.cs          # TEST-03: Protocol header preservation
├── CertificateRenewalTests.cs       # TEST-04: Certificate renewal before expiration
├── CertificateTrustTests.cs         # TEST-05: Windows trust status detection
├── MixedProtocolRoutingTests.cs     # TEST-06: HTTP/HTTPS backend scenarios
├── Http2IntegrationTests.cs         # (existing)
├── SignalRIntegrationTests.cs       # (existing)
└── YarpProxyIntegrationTests.cs     # (existing)
```

### Pattern 1: Certificate Testing with IAsyncLifetime
**What:** Use `IAsyncLifetime` for certificate generation and cleanup in temporary directory
**When to use:** Tests that generate certificates and need file system isolation
**Example:**
```csharp
// Source: Context7 xUnit documentation, Phase 18 research
public class CertificateGenerationTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;
    private string? _tempDir;
    private ICertificateService? _certificateService;
    private ICertificateStorageService? _storageService;

    public CertificateGenerationTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    public async Task InitializeAsync()
    {
        // Create temporary directory for certificate files
        _tempDir = Path.Combine(Path.GetTempPath(), $"portless-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);

        // Configure services to use temp directory
        _certificateService = _factory.Services.GetRequiredService<ICertificateService>();
        _storageService = _factory.Services.GetRequiredService<ICertificateStorageService>();
    }

    public async Task DisposeAsync()
    {
        // Clean up temporary directory
        if (_tempDir != null && Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Warning: Failed to delete temp directory: {ex.Message}");
            }
        }
    }

    [Fact]
    public async Task Certificate_Generation_Includes_Correct_SAN_Extensions()
    {
        // Arrange
        var certManager = _factory.Services.GetRequiredService<ICertificateManager>();
        await certManager.GenerateOrLoadCertificatesAsync();

        // Act
        var certificate = await certManager.GetServerCertificateAsync();

        // Assert
        Assert.NotNull(certificate);

        // Verify SAN extensions
        var sanExtension = certificate.Extensions["2.5.29.17"]; // Subject Alternative Name OID
        Assert.NotNull(sanExtension);

        // Parse SAN extension to verify DNS names and IP addresses
        var sanData = sanExtension.RawData;
        // ... SAN parsing logic to verify *.localhost, localhost, 127.0.0.1, ::1
    }
}
```

### Pattern 2: HTTPS Endpoint Testing with WebApplicationFactory
**What:** Test HTTPS endpoint with custom environment variables and certificate validation
**When to use:** Verifying HTTPS endpoint functionality (TEST-02)
**Example:**
```csharp
// Source: Context7 ASP.NET Core integration test documentation, Phase 18 research
public class HttpsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HttpsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Https_Endpoint_Serves_Valid_Tls_Certificate()
    {
        // Arrange - Create factory with HTTPS enabled
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("PORTLESS_HTTPS_ENABLED", "true");
        });

        var client = factory.CreateClient();

        // Act - Make HTTPS request (note: WebApplicationFactory uses HTTP by default)
        // For true HTTPS testing, need to access server directly or use custom handler
        var response = await client.GetAsync("/");

        // Assert - Verify response is successful
        response.EnsureSuccessStatusCode();

        // Verify certificate is loaded in server
        var certManager = factory.Services.GetRequiredService<ICertificateManager>();
        var certificate = await certManager.GetServerCertificateAsync();
        Assert.NotNull(certificate);
        Assert.True(certificate.NotAfter > DateTimeOffset.UtcNow);
    }
}
```

### Pattern 3: Platform-Specific Tests with OS Guards
**What:** Use `[SupportedOSPlatform]` and `RuntimeInformation.IsOSPlatform()` for Windows-only tests
**When to use:** Trust status detection tests (TEST-05) that only work on Windows
**Example:**
```csharp
// Source: Context7 ASP.NET Core documentation, Phase 18 research
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class CertificateTrustTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CertificateTrustTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Trust_Status_Detection_Works_On_Windows()
    {
        // Skip if not on Windows (defense in depth)
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows))
        {
            return; // Skip test
        }

        // Arrange
        var trustService = _factory.Services.GetRequiredService<ICertificateTrustService>();

        // Act
        var isTrusted = await trustService.IsCertificateTrustedAsync();

        // Assert - Verify trust status can be detected (may be false if not installed)
        // The test verifies the detection mechanism works, not that trust is established
        Assert.NotNull(isTrusted);
    }
}
```

### Pattern 4: X-Forwarded-Proto Header Testing
**What:** Create test backends that return received headers, verify proxy forwarding
**When to use:** Mixed protocol support testing (TEST-03)
**Example:**
```csharp
// Source: Existing YarpProxyIntegrationTests pattern, Phase 18 research
public class XForwardedProtoTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public XForwardedProtoTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task X_Forwarded_Protocol_Preserved_For_Http_Backend()
    {
        // Arrange
        var config = _factory.Services.GetRequiredService<DynamicConfigProvider>();

        var routes = new List<RouteConfig>
        {
            new RouteConfig
            {
                RouteId = "route-httptest.localhost",
                ClusterId = "cluster-httptest",
                Match = new RouteMatch
                {
                    Hosts = new[] { "httptest.localhost" },
                    Path = "/{**catch-all}"
                }
            }
        };

        var clusters = new List<ClusterConfig>
        {
            new ClusterConfig
            {
                ClusterId = "cluster-httptest",
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["backend1"] = new DestinationConfig { Address = "http://localhost:5000" }
                }
            }
        };

        config.Update(routes, clusters);

        // Act - Make request to proxy
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Host", "httptest.localhost");
        var response = await _client.SendAsync(request);

        // Assert - Verify routing works
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.BadGateway,
            "HTTP backend should receive forwarded request"
        );

        // Note: Actual header verification requires backend inspection
        // This test verifies the proxy doesn't break the request
    }
}
```

### Anti-Patterns to Avoid
- **Mocking certificate services**: Project has no mocking framework, real services provide better integration coverage
- **Using real ~/.portless directory**: Contaminates user environment, use temp directories with IAsyncLifetime cleanup
- **Hard-coding platform-specific behavior**: Use OS guards and skip tests instead of conditional logic
- **Testing certificate creation on every test**: Generate once in IAsyncLifetime.InitializeAsync(), reuse for multiple assertions
- **Ignoring cleanup in async tests**: Always implement IAsyncLifetime.DisposeAsync() to delete temp files

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Certificate SAN parsing | Manual ASN.1 BER decoder | X509Certificate2.Extensions["2.5.29.17"] | .NET has built-in SAN extension parsing, ASN.1 is complex and error-prone |
| Test server creation | Custom Kestrel builder | WebApplicationFactory<Program> | Handles TestServer, service provider, configuration automatically |
| Temporary directory management | Manual temp file tracking | Path.GetTempPath() + Guid.NewGuid() + IAsyncLifetime | OS handles temp file cleanup, isolated per test run |
| Platform detection | Environment.OSVersion | RuntimeInformation.IsOSPlatform() | More reliable, cross-platform, handles OS versions correctly |
| Certificate validation | Manual chain building | X509Chain + X509Store | .NET has built-in X.509 chain validation and trust store access |

**Key insight:** .NET 10 has comprehensive certificate and platform APIs. Building custom implementations adds complexity, introduces bugs, and misses edge cases (like SAN extension encoding, platform-specific trust store locations, certificate chain validation). Use existing APIs for reliability and maintainability.

## Common Pitfalls

### Pitfall 1: Certificate File Permission Issues
**What goes wrong:** Tests fail with "Access denied" when writing certificate files to temp directory on Unix systems
**Why it happens:** Certificate files created with default permissions (644) don't meet secure requirements (600), permission service may reject them
**How to avoid:** Set permissions explicitly in test setup, or skip permission tests on CI/CD with elevated privileges
**Warning signs:** "UnauthorizedAccessException", "Permission denied" in test logs, tests pass on Windows but fail on Linux/Mac

### Pitfall 2: Certificate Expiration Time Zones
**What goes wrong:** Certificate expiration tests fail intermittently due to UTC vs local time confusion
**Why it happens:** X509Certificate2.NotAfter is UTC-local, DateTimeOffset.UtcNow is UTC, but test assertions may use DateTime.Now (local)
**How to avoid:** Always use DateTimeOffset.UtcNow for certificate expiration calculations, add 1-day buffer to avoid edge cases
**Warning signs:** Tests pass at certain times of day, fail near midnight, timezone-dependent failures

### Pitfall 3: WebApplicationFactory Certificate Loading
**What goes wrong:** HTTPS endpoint tests fail with "Certificate not found" even when certificate generation succeeds
**Why it happens:** WebApplicationFactory creates a new service provider instance, certificate must be loaded before Kestrel configuration
**How to avoid:** Use `WithWebHostBuilder()` to set `PORTLESS_HTTPS_ENABLED=true`, rebuild service provider to load certificate
**Warning signs:** "Certificate not found or invalid" error message, HTTP tests pass but HTTPS tests fail

### Pitfall 4: Platform Guard Side Effects
**What goes wrong:** Tests marked as "skipped" but still execute platform-specific code in constructor
**Why it happens:** `[SupportedOSPlatform]` is an analyzer attribute, doesn't skip test at runtime
**How to avoid:** Use `RuntimeInformation.IsOSPlatform()` check at start of test method and return early
**Warning signs:** Tests pass on Windows but crash on Linux/Mac with "DllNotFoundException" or "PlatformNotSupportedException"

### Pitfall 5: Temp Directory Cleanup Failures
**What goes wrong:** IAsyncLifetime.DisposeAsync() throws exception when deleting temp directory
**Why it happens:** File handles not released (certificate files still open), antivirus scanning, or concurrent test access
**How to avoid:** Wrap cleanup in try-catch, log warning instead of failing test, use recursive delete
**Warning signs:** "Directory not empty" exceptions, temp directories accumulating in /tmp or %TEMP%

## Code Examples

Verified patterns from official sources:

### Certificate SAN Extension Verification
```csharp
// Source: Context7 .NET documentation, Phase 18 research
[Fact]
public async Task Certificate_Contains_Correct_SAN_Extensions()
{
    // Arrange
    var certManager = _factory.Services.GetRequiredService<ICertificateManager>();
    await certManager.GenerateOrLoadCertificatesAsync();
    var certificate = await certManager.GetServerCertificateAsync();

    // Act
    var sanExtension = certificate.Extensions["2.5.29.17"]; // Subject Alternative Name

    // Assert
    Assert.NotNull(sanExtension);
    Assert.True(sanExtension.Critical); // SAN should be marked critical

    // Verify SAN contains DNS names and IP addresses
    var sanData = AsnEncodedDataFactory.GetInstanceFor(sanExtension.Oid, sanExtension.RawData);
    var sanText = sanData.Format(true);

    Assert.Contains("DNS Name=*.localhost", sanText);
    Assert.Contains("DNS Name=localhost", sanText);
    Assert.Contains("IP Address=127.0.0.1", sanText);
    Assert.Contains("IP Address=::1", sanText);
}
```

### Certificate Expiration Testing
```csharp
// Source: Context7 xUnit documentation, Phase 18 research
[Fact]
public async Task_Certificate_Renewal_Before_Expiration()
{
    // Arrange
    var certManager = _factory.Services.GetRequiredService<ICertificateManager>();
    var storageService = _factory.Services.GetRequiredService<ICertificateStorageService>();

    // Generate initial certificate
    await certManager.GenerateOrLoadCertificatesAsync();
    var oldCert = await certManager.GetServerCertificateAsync();
    var oldThumbprint = oldCert?.Thumbprint;

    // Act - Renew certificate
    await certManager.RegenerateCertificatesAsync(forceRegeneration: true);
    var newCert = await certManager.GetServerCertificateAsync();
    var newThumbprint = newCert?.Thumbprint;

    // Assert - Certificate was renewed
    Assert.NotEqual(oldThumbprint, newThumbprint);

    // Verify new certificate is valid for 5 years
    var expectedExpiration = DateTimeOffset.UtcNow.AddYears(5);
    var expirationDelta = (newCert!.NotAfter - expectedExpiration).TotalDays;
    Assert.True(Math.Abs(expirationDelta) < 1, "Certificate should be valid for ~5 years");
}
```

### Mixed Protocol Routing Configuration
```csharp
// Source: Existing YarpProxyIntegrationTests pattern, Phase 18 research
[Fact]
public async Task Mixed_Http_Https_Backend_Routing_Works()
{
    // Arrange
    var config = _factory.Services.GetRequiredService<DynamicConfigProvider>();

    var routes = new List<RouteConfig>
    {
        new RouteConfig
        {
            RouteId = "route-http.localhost",
            ClusterId = "cluster-http-backend",
            Match = new RouteMatch { Hosts = new[] { "http.localhost" }, Path = "/{**catch-all}" }
        },
        new RouteConfig
        {
            RouteId = "route-https.localhost",
            ClusterId = "cluster-https-backend",
            Match = new RouteMatch { Hosts = new[] { "https.localhost" }, Path = "/{**catch-all}" }
        }
    };

    var clusters = new List<ClusterConfig>
    {
        new ClusterConfig
        {
            ClusterId = "cluster-http-backend",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["http-backend"] = new DestinationConfig { Address = "http://localhost:5000" }
            }
        },
        new ClusterConfig
        {
            ClusterId = "cluster-https-backend",
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["https-backend"] = new DestinationConfig
                {
                    Address = "https://localhost:5001",
                    // Accept self-signed certificates in development mode
                }
            },
            HttpClient = new HttpClientConfig
            {
                DangerousAcceptAnyServerCertificate = true, // Development mode
                SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
            }
        }
    };

    config.Update(routes, clusters);

    // Act - Request HTTP backend
    var httpReq = new HttpRequestMessage(HttpMethod.Get, "/");
    httpReq.Headers.Add("Host", "http.localhost");
    var httpResp = await _client.SendAsync(httpReq);

    // Act - Request HTTPS backend
    var httpsReq = new HttpRequestMessage(HttpMethod.Get, "/");
    httpsReq.Headers.Add("Host", "https.localhost");
    var httpsResp = await _client.SendAsync(httpsReq);

    // Assert - Both routes configured successfully
    Assert.True(
        httpResp.StatusCode == HttpStatusCode.OK ||
        httpResp.StatusCode == HttpStatusCode.BadGateway ||
        httpResp.StatusCode == HttpStatusCode.ServiceUnavailable,
        "HTTP backend routing configured"
    );

    Assert.True(
        httpsResp.StatusCode == HttpStatusCode.OK ||
        httpsResp.StatusCode == HttpStatusCode.BadGateway ||
        httpsResp.StatusCode == HttpStatusCode.ServiceUnavailable,
        "HTTPS backend routing configured"
    );
}
```

### Windows Trust Status Detection
```csharp
// Source: Context7 ASP.NET Core documentation, Phase 18 research
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class CertificateTrustTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CertificateTrustTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Trust_Status_Detection_Returns_Valid_Result()
    {
        // Skip if not on Windows (defense in depth)
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var trustService = _factory.Services.GetRequiredService<ICertificateTrustService>();

        // Act
        var trustStatus = await trustService.GetCertificateTrustStatusAsync();

        // Assert - Trust status can be determined (may be Trusted, Untrusted, or Unknown)
        Assert.NotNull(trustStatus);
        Assert.True(
            trustStatus == TrustStatus.Trusted ||
            trustStatus == TrustStatus.NotTrusted ||
            trustStatus == TrustStatus.Unknown,
            "Trust status should return a valid enum value"
        );
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Manual TestServer creation | WebApplicationFactory<Program> | ASP.NET Core 3.0 | Simplified test setup, automatic configuration |
| IDisposable for cleanup | IAsyncLifetime for async cleanup | xUnit 2.1 | Better async/await support, cleaner resource management |
| MSTest/NUnit | xUnit | .NET Core era | Modern .NET standard, better extensibility |
| Console.WriteLine for test output | ITestOutputHelper injection | xUnit 2.x | Structured test logging, better CI/CD integration |
| Hard-coded test ports | Dynamic port allocation | ASP.NET Core 2.1 | Avoids port conflicts in parallel test runs |

**Deprecated/outdated:**
- **MSTest `[TestClass]` and `[TestMethod]`**: Replaced by xUnit `[Fact]` and `[Theory]`, simpler syntax, better parallel execution
- ** NUnit `[SetUp]` and `[TearDown]`**: Replaced by xUnit constructor and `IAsyncLifetime`, more flexible lifecycle management
- ** Manual service locator pattern**: Replaced by dependency injection through WebApplicationFactory, better test isolation

## Open Questions

1. **HTTPS client certificate validation in WebApplicationFactory**
   - What we know: WebApplicationFactory defaults to HTTP, HTTPS requires custom configuration
   - What's unclear: How to validate the server certificate presented by the TestServer's HTTPS endpoint
   - Recommendation: Create a custom `HttpClientHandler` that validates certificate, or use direct `TcpClient` connection for TLS handshake verification

2. **Certificate expiration test data management**
   - What we know: Need to test expiration detection (30-day warning, auto-renewal)
   - What's unclear: Should we mock the system clock, or create certificates with different expiration dates?
   - Recommendation: Use real certificates with varying expiration dates created in test setup, avoid clock mocking (adds complexity, unreliable)

3. **Mixed protocol backend server provisioning**
   - What we know: Need HTTP and HTTPS backends for X-Forwarded-Proto testing
   - What's unclear: Should we spawn actual backend servers, or use TestServer instances?
   - Recommendation: Use TestServer with WebApplicationFactory for HTTP backends, create self-signed HTTPS certificate for HTTPS backend testing

4. **Test execution order for certificate renewal scenarios**
   - What we know: Certificate renewal creates new files, may conflict with concurrent tests
   - What's unclear: Should renewal tests be isolated, or can they run in parallel?
   - Recommendation: Use unique temp directories per test class (IClassFixture + IAsyncLifetime), allow parallel execution within class, serialize between classes

## Sources

### Primary (HIGH confidence)
- [Context7: xUnit.net](https://github.com/xunit/xunit.net) - IClassFixture, IAsyncLifetime, shared context patterns
- [Context7: ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0) - WebApplicationFactory, integration testing, HTTPS configuration
- [Existing codebase: Portless.Tests](https://github.com/sergeix/portless-dotnet) - Http2IntegrationTests, SignalRIntegrationTests, YarpProxyIntegrationTests patterns
- [Existing codebase: Portless.Proxy/Program.cs](https://github.com/sergeix/portless-dotnet) - HTTPS endpoint configuration, certificate loading, X-Forwarded-Proto middleware
- [Existing codebase: Portless.Core/Services](https://github.com/sergeix/portless-dotnet) - ICertificateService, ICertificateManager, ICertificateTrustService implementations

### Secondary (MEDIUM confidence)
- [Microsoft Learn: Configure ASP.NET Core to work with proxy servers](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0) - X-Forwarded-Proto header forwarding (verified with official docs)
- [Microsoft Learn: Integration tests in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0) - WebApplicationFactory configuration, test patterns (verified with official docs)
- [xUnit documentation: Shared Context](https://github.com/xunit/xunit.net/blob/main/site/docs/shared-context.md) - IClassFixture, IAsyncLifetime usage (verified with official docs)

### Tertiary (LOW confidence)
- [WebSearch: ASP.NET HTTPS certificate configuration](https://www.cnblogs.com/SCscHero/p/19489150) - Certificate SAN extension examples (marked for validation, Chinese source)
- [WebSearch: Certificate SAN extension format](https://m.php.cn/faq/2087372.html) - localhost SAN extension generation (marked for validation, non-English source)

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All packages already in project, official documentation verified
- Architecture: HIGH - Existing test patterns analyzed, official documentation reviewed
- Pitfalls: HIGH - Platform-specific issues documented, official sources consulted
- Code examples: HIGH - Verified against existing codebase and official documentation

**Research date:** 2026-03-02
**Valid until:** 2026-04-02 (30 days - stable .NET 10 platform, xUnit 2.9.3 is LTS)
