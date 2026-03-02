# Phase 18: Integration Tests - Context

**Gathered:** 2026-03-02
**Status:** Ready for planning

<domain>
## Phase Boundary

Comprehensive test coverage for HTTPS features including certificate generation, HTTPS endpoint functionality, mixed HTTP/HTTPS protocol support, certificate lifecycle management (expiration and renewal), and trust status detection. Tests must verify that Phase 13-17 implementations work correctly together.

**Success Criteria from ROADMAP.md:**
1. Integration tests verify certificate generation with correct SAN extensions
2. Integration tests verify HTTPS endpoint serves valid TLS certificate
3. Integration tests verify X-Forwarded-Proto header preservation (HTTP vs HTTPS backends)
4. Integration tests verify certificate renewal before expiration
5. Integration tests verify trust status detection on Windows
6. Integration tests cover mixed HTTP/HTTPS backend routing scenarios

**Dependencies:** Phase 15 (HTTPS Endpoint), Phase 16 (Mixed Protocol Support), Phase 17 (Certificate Lifecycle)

</domain>

<decisions>
## Implementation Decisions

### Test Organization
- Test class structure: [User choice — By feature / By test type / By scenario]
- Test project location: [User choice — Portless.Tests / New project / Your call]
- Test granularity: [User choice — Single assertion / Multiple assertions / Behavior-focused]
- Test categorization: [User choice — Yes, add traits / No categorization / Your call]

**Context:**
- Existing tests use `IClassFixture<WebApplicationFactory<Program>>` pattern
- Examples: `Http2IntegrationTests`, `SignalRIntegrationTests`, `YarpProxyIntegrationTests`
- Tests organized in `Portless.Tests/` project with clear feature-based grouping
- XML doc comments explain what each test verifies

### Certificate File Handling
- Certificate generation approach: [User choice — Real service with temp dir / Mocked service / Fixture certificates]
- Storage service testing: [User choice — Test real storage / Mock for other tests / In-memory storage]
- Setup/cleanup pattern: [User choice — IAsyncLifetime / Constructor/Dispose / Per-test method]
- State directory handling: [User choice — Temp directory / Real directory / Mocked paths]

**Context:**
- Certificate services available: `ICertificateService`, `ICertificateStorageService`, `ICertificateManager`, `ICertificatePermissionService`, `ICertificateMonitoringService`, `ICertificateTrustService`
- Certificate files stored in `~/.portless/` directory (ca.pfx, cert.pfx, cert-info.json)
- File permissions: chmod 600 on Unix, ACL on Windows
- Existing tests use real services with `WebApplicationFactory` (no mocking framework in project)

### Claude's Discretion
- Test backend provisions (WebApplicationFactory vs actual servers) — use existing TestApi pattern where appropriate
- Platform-specific tests (Windows-only trust tests) — add skip logic with `RuntimeInformation.IsOSPlatform()` checks
- Test data management for certificate expiration scenarios
- Exact test naming conventions within chosen organization structure
- Whether to add benchmark/performance tests for HTTPS overhead

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- **WebApplicationFactory<Program>** — Test infrastructure for in-memory proxy hosting (used in `Http2IntegrationTests`, `SignalRIntegrationTests`)
- **ITestOutputHelper** — Console output for debugging (used in `SignalRIntegrationTests`)
- **TaskCompletionSource<T>** — Async test coordination with timeouts (used in SignalR message tests)
- **DynamicConfigProvider** — In-memory YARP configuration for test routing
- **TestApi/** — Simple HTTP test server for backend simulation

### Established Patterns
- **Test fixture pattern**: `IClassFixture<WebApplicationFactory<Program>>` for shared test context
- **Test organization**: One test class per feature (e.g., `Http2IntegrationTests`, `WebSocketIntegrationTests`)
- **Test documentation**: XML doc comments with triple-slash summaries explaining test purpose
- **Async testing**: `Task<T>` return types, `await` in test methods, `WaitAsync(TimeSpan)` for timeouts
- **Region organization**: `#region` directives grouping tests by functionality (Connection Tests, Messaging Tests, etc.)
- **Test naming**: `[Feature]_[Scenario]_[ExpectedOutcome]` pattern (e.g., `SignalR_Connection_Established_Through_Proxy`)
- **No mocking**: Tests use real services with `WebApplicationFactory` — no Moq or similar mocking framework
- **xUnit framework**: `[Fact]` for single tests, `[Theory]` with `[InlineData]` for parameterized tests

### Integration Points
- **Portless.Tests** — Main integration test project (add HTTPS tests here or create new project)
- **Portless.IntegrationTests** — CLI and process management tests (separate concern)
- **Portless.E2ETests** — End-to-end tests (separate concern)
- **Portless.Core** — Certificate services under test (`ICertificateService`, `ICertificateStorageService`, `ICertificateManager`, `ICertificatePermissionService`, `ICertificateMonitoringService`, `ICertificateTrustService`)
- **Portless.Proxy** — HTTPS endpoint under test (`Program.cs` with dual HTTP/HTTPS endpoints)
- **TestApi** — Test backend server for HTTP/HTTPS backend simulation

### Certificate Services to Test
- **ICertificateService** — `GenerateCertificateAuthorityAsync()`, `GenerateWildcardCertificateAsync()`, `ValidateCertificateAsync()`
- **ICertificateStorageService** — `SaveCertificateAsync()`, `LoadCertificateAsync()`, `EnsureCertificatePermissionsAsync()`, `DeleteCertificateAsync()`
- **ICertificateManager** — `GenerateOrLoadCertificatesAsync()`, `GetCertificateInfoAsync()`, `RegenerateCertificatesAsync()`, `IsCertificateNearExpiration()`
- **ICertificatePermissionService** — `VerifyFilePermissionsAsync()`, `SetSecureFilePermissionsAsync()`, `HasPermissionIssues()`
- **ICertificateMonitoringService** — `StartAsync()`, `StopAsync()`, `ExecuteAsync()`, `CheckAndRenewCertificateAsync()`
- **ICertificateTrustService** — `IsCertificateTrustedAsync()`, `InstallCertificateAsync()`, `UninstallCertificateAsync()`, `GetCertificateTrustStatusAsync()`

</code_context>

<specifics>
## Specific Ideas

No specific requirements — open to standard approaches that align with existing test patterns in the codebase.

The test suite should validate the Phase 18 success criteria:
1. Certificate generation with correct SAN extensions for `*.localhost`, `localhost`, `127.0.0.1`, `::1`
2. HTTPS endpoint (port 1356) serves valid TLS certificate
3. X-Forwarded-Proto header preservation when backend is HTTP vs HTTPS
4. Certificate renewal triggers before 30-day expiration window
5. Trust status detection works on Windows Certificate Store
6. Mixed routing supports both HTTP and HTTPS backends simultaneously

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 18-integration-tests*
*Context gathered: 2026-03-02*
