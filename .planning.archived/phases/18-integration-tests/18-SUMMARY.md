# Phase 18: Integration Tests - Summary

**Status:** Ready for Execution
**Plans:** 4 plans created
**Total Tasks:** 19 tasks across 4 waves
**Estimated Duration:** ~45 minutes (based on ~10 min/plan average)

## Overview

Phase 18 delivers comprehensive integration testing for HTTPS features implemented in Phases 13-17. The test suite verifies certificate generation, HTTPS endpoint functionality, protocol header preservation, certificate lifecycle management, and trust status detection on Windows.

## Plans by Wave

### Wave 1: Certificate Generation and Lifecycle Tests (Plan 18-01)
- **File:** `Portless.Tests/CertificateGenerationTests.cs`
- **File:** `Portless.Tests/CertificateRenewalTests.cs`
- **Requirements:** TEST-01, TEST-04
- **Tests:** 5 integration tests (2 generation, 3 renewal)
- **Key Features:**
  - SAN extension verification (*.localhost, localhost, 127.0.0.1, ::1)
  - 5-year validity period validation
  - Certificate regeneration with thumbprint comparison
  - Metadata file (cert-info.json) update verification
  - Expiration detection logic testing
  - Temp directory isolation via IAsyncLifetime

### Wave 2: HTTPS Endpoint Tests (Plan 18-02)
- **File:** `Portless.Tests/HttpsEndpointTests.cs`
- **Requirements:** TEST-02
- **Tests:** 6 integration tests
- **Key Features:**
  - Dual HTTP/HTTPS endpoint binding (1355/1356)
  - TLS certificate serving verification
  - HTTP endpoint functionality with HTTPS enabled
  - TLS 1.2+ minimum protocol enforcement
  - Certificate reload on startup
  - HTTPS disabled by default

### Wave 3: Protocol Header and Mixed Routing Tests (Plan 18-03)
- **File:** `Portless.Tests/XForwardedProtoTests.cs`
- **File:** `Portless.Tests/MixedProtocolRoutingTests.cs`
- **File:** `Portless.Tests/TestApi/HeaderEchoServer.cs` (new)
- **Requirements:** TEST-03, TEST-06
- **Tests:** 7 integration tests (3 protocol header, 4 mixed routing)
- **Key Features:**
  - X-Forwarded-Proto header preservation (http/https)
  - Mixed HTTP/HTTPS backend routing
  - Self-signed certificate acceptance (DangerousAcceptAnyServerCertificate)
  - SSL protocol configuration (TLS 1.2/1.3)
  - Test backend server for header verification
  - Independent route protocol handling

### Wave 4: Certificate Trust Status Tests (Plan 18-04)
- **File:** `Portless.Tests/CertificateTrustTests.cs`
- **Requirements:** TEST-05
- **Tests:** 7 integration tests
- **Key Features:**
  - Windows Certificate Store trust detection
  - Certificate installation/uninstallation
  - Idempotent operations
  - Permission denied handling
  - Platform-specific guards ([SupportedOSPlatform], Runtime checks)
  - Cross-platform skip documentation

## Test Organization

### Project Structure
```
Portless.Tests/
├── CertificateGenerationTests.cs      # TEST-01: SAN extensions, validity
├── CertificateRenewalTests.cs         # TEST-04: Renewal, metadata, expiration
├── HttpsEndpointTests.cs              # TEST-02: HTTPS endpoint, TLS protocol
├── XForwardedProtoTests.cs            # TEST-03: Protocol header preservation
├── MixedProtocolRoutingTests.cs       # TEST-06: Mixed HTTP/HTTPS backends
├── CertificateTrustTests.cs           # TEST-05: Windows trust status
├── TestApi/
│   └── HeaderEchoServer.cs            # Test backend for header verification
├── Http2IntegrationTests.cs           # (existing)
├── SignalRIntegrationTests.cs         # (existing)
└── YarpProxyIntegrationTests.cs       # (existing)
```

### Test Patterns
- **IClassFixture<WebApplicationFactory<Program>>** - Shared test context
- **IAsyncLifetime** - Async setup/cleanup for temp directories
- **ITestOutputHelper** - Console logging for debugging
- **Real services** - No mocking framework, use actual implementations
- **Temp directories** - Isolated certificate storage per test
- **Platform guards** - `[SupportedOSPlatform]` + runtime checks

## User Decisions Honored

From 18-CONTEXT.md, all user choices deferred to Claude:

### Test Organization
- **Test class structure:** By feature (CertificateGenerationTests, HttpsEndpointTests, etc.)
- **Test project location:** Portless.Tests (extend existing project)
- **Test granularity:** Behavior-focused (multiple assertions per test)
- **Test categorization:** No categorization (skip traits)

### Certificate File Handling
- **Certificate generation:** Real service with temp directory (IAsyncLifetime cleanup)
- **Storage service testing:** Test real storage with temp directory isolation
- **Setup/cleanup pattern:** IAsyncLifetime (InitializeAsync/DisposeAsync)
- **State directory handling:** Temp directory (Path.GetTempPath() + Guid)

### Claude's Discretion Applied
- **Test backend provisions:** WebApplicationFactory + TestApi/HeaderEchoServer.cs for header verification
- **Platform-specific tests:** [SupportedOSPlatform] + RuntimeInformation.IsOSPlatform() checks
- **Test data management:** Real certificates with varying expiration dates (no clock mocking)
- **Exact test naming:** [Feature]_[Scenario]_[ExpectedOutcome] pattern
- **Benchmark tests:** Not added (out of scope for integration testing)

## Dependencies

### Internal Dependencies
- **18-02** depends on **18-01** (certificate generation must work before testing HTTPS endpoint)
- **18-03** depends on **18-01, 18-02** (need certs and HTTPS for protocol/mixed tests)
- **18-04** depends on **18-01** (trust tests need certificate generation)

### External Dependencies
- **Phase 15 (HTTPS Endpoint)** - HTTPS endpoint must be implemented
- **Phase 16 (Mixed Protocol Support)** - Mixed routing must be configured
- **Phase 17 (Certificate Lifecycle)** - Renewal and monitoring must work

## Verification Criteria

### Quality Gate Checklist
- [x] PLAN.md files created in phase directory (4 plans)
- [x] Each plan has valid frontmatter (wave, depends_on, files_modified, autonomous)
- [x] Tasks are specific and actionable (19 total tasks)
- [x] Dependencies correctly identified (waves 1-4)
- [x] Waves assigned for parallel execution
- [x] must_haves derived from phase goal

### Test Coverage
- **Total tests:** 25 integration tests (5 + 6 + 7 + 7)
- **Requirements covered:** 6/6 (TEST-01 through TEST-06)
- **Platforms:** Windows, Linux, macOS (platform guards applied)
- **Execution time:** < 60 seconds total (all test suites)

## Success Metrics

### Quantitative
- 25 integration tests passing
- 100% of TEST-01 through TEST-06 requirements covered
- Test execution time < 60 seconds
- Zero temp directory leaks
- Zero test flakiness (port conflicts, certificate races)

### Qualitative
- Tests follow existing patterns (Http2IntegrationTests, SignalRIntegrationTests)
- Clear XML documentation on all test classes
- Platform-specific behavior handled gracefully
- Comprehensive error messages on test failures
- Tests serve as usage examples for HTTPS features

## Execution Notes

### Pre-execution Checklist
1. Ensure Phase 15 (HTTPS Endpoint) is complete
2. Ensure Phase 16 (Mixed Protocol Support) is complete
3. Ensure Phase 17 (Certificate Lifecycle) is complete
4. Verify Portless.Tests.csproj has all required NuGet packages
5. Verify certificate services are registered in DI container

### Known Limitations
- WebApplicationFactory uses TestServer (in-memory), actual HTTPS testing may require custom HttpClientHandler
- Certificate expiration testing uses detection logic, not time-based tests (unreliable)
- Windows trust tests skip on Linux/macOS with clear messages
- Backend servers for header verification are TestServer instances, not real processes

### Post-execution Verification
1. Run `dotnet test Portless.Tests/Portless.Tests.csproj --filter "FullyQualifiedName~Certificate"`
2. Run `dotnet test Portless.Tests/Portless.Tests.csproj --filter "FullyQualifiedName~Https"`
3. Run `dotnet test Portless.Tests/Portless.Tests.csproj --filter "FullyQualifiedName~XForwarded"`
4. Run `dotnet test Portless.Tests/Portless.Tests.csproj --filter "FullyQualifiedName~Mixed"`
5. Run `dotnet test Portless.Tests/Portless.Tests.csproj --filter "FullyQualifiedName~Trust"` (Windows only)
6. Verify temp directories cleaned up in `/tmp` or `%TEMP%`

## Documentation Updates

No documentation updates in this phase (Phase 19 handles documentation).

## Next Phase

**Phase 19: Documentation** - User-facing documentation for HTTPS certificate management, troubleshooting guides, migration guides, platform-specific notes, and security considerations.

---

**Phase:** 18-integration-tests
**Planned:** 2026-03-02
**Status:** Ready for execution via /gsd:execute-phase
