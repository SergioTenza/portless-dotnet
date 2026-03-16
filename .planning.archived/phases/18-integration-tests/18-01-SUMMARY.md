---
phase: 18-integration-tests
plan: 01
subsystem: testing
tags: [integration-tests, certificates, xunit, WebApplicationFactory, IAsyncLifetime]

# Dependency graph
requires:
  - phase: 17-certificate-lifecycle
    provides: [ICertificateManager, ICertificateStorageService, CertificateService]
provides:
  - Certificate generation integration tests with SAN extension verification
  - Certificate renewal integration tests with metadata persistence verification
  - Test infrastructure for certificate lifecycle testing
affects: []

# Tech tracking
tech-stack:
  added: []
  patterns: [WebApplicationFactory with temp directory isolation, IAsyncLifetime for test cleanup, environment variable configuration for test state]

key-files:
  created: [Portless.Tests/CertificateGenerationTests.cs, Portless.Tests/CertificateRenewalTests.cs]
  modified: [Portless.Core/Services/CertificateService.cs, Portless.Core/Services/StateDirectoryProvider.cs]

key-decisions:
  - "StateDirectoryProvider respects PORTLESS_STATE_DIR environment variable for test isolation"
  - "Certificate private key export uses CopyWithPrivateKey() to ensure PFX includes private key"
  - "Server certificate validity cannot exceed CA certificate validity to prevent signing errors"

patterns-established:
  - "WebApplicationFactory with IAsyncLifetime pattern for temp directory cleanup"
  - "Environment variable configuration before factory creation for test-specific state"

requirements-completed: [TEST-01, TEST-04]

# Metrics
duration: 15min
completed: 2026-03-02
---

# Phase 18 Plan 01: Certificate Generation and Lifecycle Integration Tests Summary

**Certificate generation and renewal integration tests with SAN extension verification, 5-year validity validation, and metadata persistence checks using WebApplicationFactory and IAsyncLifetime**

## Performance

- **Duration:** 15 min
- **Started:** 2026-03-02T07:05:38Z
- **Completed:** 2026-03-02T07:20:38Z
- **Tasks:** 2
- **Files modified:** 4

## Accomplishments

- Created comprehensive integration tests for certificate generation with SAN extension verification
- Created integration tests for certificate renewal with metadata persistence validation
- Fixed StateDirectoryProvider to respect PORTLESS_STATE_DIR for test isolation
- Fixed certificate private key export issue using CopyWithPrivateKey()
- Fixed server certificate validity to not exceed CA certificate validity

## Task Commits

Each task was committed atomically:

1. **Task 1: Certificate Generation Tests** - `1f624e0` (test)
2. **Task 2: Certificate Renewal Tests** - `cedebda` (test)

**Plan metadata:** (pending final commit)

## Files Created/Modified

- `Portless.Tests/CertificateGenerationTests.cs` - Integration tests for certificate generation with SAN extension and validity period verification
- `Portless.Tests/CertificateRenewalTests.cs` - Integration tests for certificate renewal and metadata persistence
- `Portless.Core/Services/StateDirectoryProvider.cs` - Added PORTLESS_STATE_DIR environment variable support
- `Portless.Core/Services/CertificateService.cs` - Fixed private key export and server certificate validity constraints

## Decisions Made

- **StateDirectoryProvider environment variable support**: Added PORTLESS_STATE_DIR environment variable check to enable test isolation. This allows tests to use temporary directories instead of the default ~/.portless location.
- **Certificate private key export fix**: Changed from `certificate.Export(X509ContentType.Pkcs12)` to `new X509Certificate2(certificate.CopyWithPrivateKey(rsa))` to ensure the private key is properly included in the exported certificate.
- **Server certificate validity constraint**: Added check to ensure server certificate `notAfter` does not exceed CA certificate `notAfter` to prevent "notAfter is later than issuerCertificate.NotAfter" errors during certificate signing.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 2 - Missing Critical Functionality] StateDirectoryProvider does not respect PORTLESS_STATE_DIR environment variable**
- **Found during:** Task 1 (Certificate generation tests setup)
- **Issue:** StateDirectoryProvider.GetStateDirectory() was hardcoded to return ~/.portless (or AppData\portless on Windows), ignoring the PORTLESS_STATE_DIR environment variable. This prevented test isolation.
- **Fix:** Added environment variable check at the beginning of GetStateDirectory() to return PORTLESS_STATE_DIR if set, falling back to default locations otherwise.
- **Files modified:** Portless.Core/Services/StateDirectoryProvider.cs
- **Verification:** Tests now use temp directories (/tmp/portless-test-*) instead of ~/.portless
- **Committed in:** 1f624e0 (Task 1 commit)

**2. [Rule 1 - Bug] Certificate private key missing after export**
- **Found during:** Task 1 (First test run)
- **Issue:** Server certificate was saved successfully but failed to load with "Server certificate is missing private key" error. The `certificate.Export(X509ContentType.Pkcs12, "")` was not including the private key because the RSA object was being disposed before export.
- **Fix:** Changed to use `new X509Certificate2(certificate.CopyWithPrivateKey(rsa))` which creates a new certificate with the private key explicitly attached, then dispose the RSA key.
- **Files modified:** Portless.Core/Services/CertificateService.cs
- **Verification:** All certificate tests pass, private key is present when loading certificates
- **Committed in:** 1f624e0 (Task 1 commit)

**3. [Rule 1 - Bug] Server certificate validity exceeds CA certificate validity**
- **Found during:** Task 1 (First test run)
- **Issue:** Certificate creation failed with "The requested notAfter value is later than issuerCertificate.NotAfter" error. Both CA and server certificates calculated `notAfter` as `DateTimeOffset.UtcNow.AddDays(options.ValidityYears * 365)` at slightly different times, causing the server certificate to be 1 second later than the CA certificate.
- **Fix:** Added check in GenerateWildcardCertificateAsync to ensure server certificate `notAfter` does not exceed CA certificate `notAfter` by setting `notAfter = caCertificate.NotAfter.AddSeconds(-1)` if needed.
- **Files modified:** Portless.Core/Services/CertificateService.cs
- **Verification:** All certificate generation tests pass without validity errors
- **Committed in:** 1f624e0 (Task 1 commit)

---

**Total deviations:** 3 auto-fixed (1 missing critical functionality, 2 bugs)
**Impact on plan:** All auto-fixes were necessary for tests to function correctly. The StateDirectoryProvider fix enables test isolation, the private key fix is critical for certificate functionality, and the validity constraint fix prevents certificate generation failures.

## Issues Encountered

- **WebApplicationFactory environment variable timing**: Initial attempt to set PORTLESS_STATE_DIR via `WithWebHostBuilder()` failed because services were already instantiated. Fixed by setting environment variable before creating WebApplicationFactory.
- **RSA key disposal timing**: Using `using var rsa` caused the private key to be disposed before certificate export. Fixed by manual disposal after certificate creation.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Certificate generation and renewal integration tests complete
- Test infrastructure pattern established (WebApplicationFactory + IAsyncLifetime + temp directory isolation)
- Ready for next integration test plans (18-02, 18-03, 18-04)

---
*Phase: 18-integration-tests*
*Plan: 01*
*Completed: 2026-03-02*
