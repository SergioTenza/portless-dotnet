# Technical Debt - Portless.NET

**Last Updated:** 2026-03-16
**Project Status:** v1.2 HTTPS Complete, Ready for v1.3 Planning

---

## Priority Classification

- 🔴 **CRITICAL:** Blocks development or breaks CI/CD
- 🟡 **HIGH:** Significantly impacts development workflow
- 🟢 **MEDIUM:** Annoying but workable
- 🔵 **LOW:** Nice to have eventually

---

## 🔴 CRITICAL

### 1. Test Suite Flakiness
**Category:** Testing Infrastructure
**Impact:** Unreliable CI/CD, wasted debugging time, blocking releases
**Discovered:** 2026-03-16 during migration Chunk 6 verification

**Symptoms:**
- Non-deterministic test failures
- Inconsistent results between test runs
- Sometimes 67/67 pass, sometimes multiple failures
- File lock acquisition timeouts during tests
- Port binding conflicts

**Root Causes:**
1. **Test Isolation Issues**
   - Tests share resources (files, ports, processes)
   - No proper cleanup/teardown procedures
   - Concurrent test execution interference

2. **File Lock Contention**
   - RouteStore.AcquireFileLockAsync timeouts
   - Multiple tests accessing same route files
   - Lock acquisition race conditions

3. **Background Service Interference**
   - RouteCleanupService running during tests
   - ProcessHealthMonitor active during tests
   - RouteFileWatcher triggering during tests
   - Services not properly stopped between tests

4. **Port Management**
   - Dynamic port allocation can conflict
   - Ports not properly released after tests
   - No centralized port management for tests

**Evidence:**
```
Error during route cleanup cycle
System.Threading.Tasks.TaskCanceledException: A task was canceled.
   at Portless.Core.Services.RouteCleanupService.ExecuteAsync()

Error persisting route to file
System.IO.IOException: Timeout acquiring route store lock
   at Portless.Core.Services.RouteStore.AcquireFileLockAsync()
```

**Test Runs Show Inconsistency:**
- Run 1: 21 failed (SignalR, YARP, certificate tests)
- Run 2: 1 failed (RoutePersistenceTests)
- Run 3: 0 failed (67/67 passed) ✅

**Recommended Actions:**

1. **Immediate (Week 1):**
   - Add `[Collection("Integration Tests")]` attribute to isolate integration tests
   - Implement proper test cleanup in `Dispose()` methods
   - Use `IAsyncLifetime` for async setup/teardown
   - Add test-specific port ranges to avoid conflicts

2. **Short-term (Month 1):**
   - Refactor test infrastructure to use dependency injection properly
   - Create test-specific service configurations that disable background services
   - Implement proper test file management (unique temp directories per test)
   - Add test ordering controls where needed

3. **Long-term (Quarter 1):**
   - Consider test serialization for flaky integration tests
   - Implement shared test fixture with proper lifecycle management
   - Add integration test stress testing to catch race conditions
   - Create test infrastructure documentation

**Estimated Effort:** 2-3 weeks
**Risk if Ignored:** CI/CD failures, blocked releases, wasted development time

---

## 🟡 HIGH

### 2. Archive Documentation Structure
**Category:** Documentation
**Impact:** Historical context lost, difficult to reference past decisions
**Discovered:** 2026-03-16 during Chunk 2 (Archive GSD Structure)

**Issue:**
The `.planning.archived/` directory contains 19 phases of GSD planning but lacks comprehensive documentation for easy navigation.

**Current State:**
- `.planning.archived/README.md` exists but minimal
- 19 phase directories with rich historical content
- No roadmap or guide to historical phases
- Difficult to find specific decisions or requirements

**Recommended Actions:**
1. Create historical roadmap in `.planning.archived/README.md`
2. Add phase-by-phase decision index
3. Document evolution of architecture decisions
4. Create cross-reference between phases and features

**Estimated Effort:** 1 week
**Risk if Ignored:** Loss of historical context, repeated mistakes

---

## 🟢 MEDIUM

### 3. Certificate File Permissions
**Category:** Security/Platform Compatibility
**Impact:** Warnings in build, potential permission issues on some platforms
**Discovered:** Ongoing warnings in build output

**Issue:**
CA1416 warnings for platform-specific API usage in CertificatePermissionService:
- `AccessControlType.Allow` is Windows-only
- `File.SetUnixFileMode()` is Unix-only
- `WindowsIdentity.User` is Windows-only

**Current State:**
- Code compiles with warnings
- Platform checks exist at runtime
- Build shows 10+ CA1416 warnings

**Recommended Actions:**
1. Add platform-specific compilation symbols
2. Use `#if WINDOWS` / `#if UNIX` preprocessor directives
3. Separate platform-specific implementations into different files
4. Add analyzer suppressions with explanations

**Estimated Effort:** 3-5 days
**Risk if Ignored:** Missed platform bugs, warning fatigue

---

### 4. Configuration Management
**Category:** Configuration
**Impact:** Hard-coded values, difficult to customize
**Discovered:** During v1.2 development

**Issues:**
- Some ports are hard-coded (1355, 1356)
- File paths hard-coded in some places
- Environment variable handling inconsistent
- No centralized configuration validation

**Current State:**
- Some environment variables supported (PORTLESS_PORT, etc.)
- Configuration scattered across multiple files
- No configuration schema validation

**Recommended Actions:**
1. Consolidate configuration into options pattern
2. Add configuration validation on startup
3. Document all environment variables
4. Create configuration documentation generator

**Estimated Effort:** 1 week
**Risk if Ignored:** Deployment issues, user confusion

---

## 🔵 LOW

### 5. Error Messages
**Category:** User Experience
**Impact:** Confusing error messages, harder debugging
**Discovered:** Ongoing

**Issues:**
- Some error messages are cryptic
- Inconsistent error message formats
- Missing contextual information in errors
- No error code documentation

**Recommended Actions:**
1. Audit all error messages for clarity
2. Create error message style guide
3. Add error code documentation
4. Implement structured error logging

**Estimated Effort:** 1 week
**Risk if Ignored:** User frustration, slower debugging

---

### 6. Documentation Completeness
**Category:** Documentation
**Impact:** Harder to onboard new contributors
**Discovered:** During migration

**Issues:**
- Some advanced features lack documentation
- API documentation incomplete
- Contributing guide minimal
- Architecture diagram outdated

**Recommended Actions:**
1. Complete feature documentation gaps
2. Generate API documentation from XML comments
3. Expand contributing guide
4. Update architecture diagrams

**Estimated Effort:** 2 weeks
**Risk if Ignored:** Slower onboarding, more support burden

---

## Debt Metrics

### Code Quality Indicators
- **Test Coverage:** Unknown (need to measure)
- **Test Flakiness:** ~15-30% of integration tests show non-deterministic behavior
- **Build Warnings:** 10+ CA1416 platform warnings
- **Technical Debt Ratio:** Estimated 15-20% (needs SonarQube analysis)

### Debt by Category
- **Testing Infrastructure:** 1 critical, 1 high (40% of total debt impact)
- **Documentation:** 1 high, 1 low (25% of total debt impact)
- **Platform Compatibility:** 1 medium (15% of total debt impact)
- **Configuration:** 1 medium (15% of total debt impact)
- **User Experience:** 1 low (5% of total debt impact)

### Debt by Effort
- **Critical:** 2-3 weeks (test flakiness fixes)
- **High:** 1-2 weeks (archive documentation + config)
- **Medium:** 1-2 weeks (permissions + config)
- **Low:** 3 weeks (error messages + documentation)

**Total Estimated Effort:** 8-12 weeks to address all debt

---

## Debt Paydown Strategy

### Phase 1: Critical (Weeks 1-3)
**Focus:** Fix test suite flakiness
**Outcome:** Reliable CI/CD, confident releases
**Deliverables:**
- Stable test suite (67/67 tests consistently passing)
- Test infrastructure improvements
- Test documentation

### Phase 2: High & Medium (Weeks 4-8)
**Focus:** Documentation and configuration
**Outcome:** Better developer experience, easier deployments
**Deliverables:**
- Archive documentation improvements
- Configuration management system
- Platform-specific code separation

### Phase 3: Low (Weeks 9-12)
**Focus:** Polish and enhancement
**Outcome:** Better UX, easier onboarding
**Deliverables:**
- Improved error messages
- Complete documentation
- API documentation generation

---

## Preventing Future Debt

### Development Guidelines
1. **Test-First Development:** Always write tests before implementation
2. **Test Isolation:** Ensure tests are independent and deterministic
3. **Platform Awareness:** Consider cross-platform implications from start
4. **Documentation-First:** Document features alongside implementation
5. **Code Review:** Include debt assessment in PR reviews

### Automated Checks
1. **Test Flakiness Detection:** Run tests multiple times in CI
2. **Platform Testing:** Test on Windows, macOS, Linux
3. **Documentation Coverage:** Warn when undocumented code changes
4. **Warning-Free Build:** Treat new warnings as errors

### Regular Debt Audits
1. **Monthly:** Review and prioritize technical debt
2. **Quarterly:** Allocate sprint time for debt paydown
3. **Annually:** Comprehensive debt assessment and planning

---

## Next Steps

### Immediate (This Week)
1. Fix test suite flakiness (CRITICAL)
2. Set up debt tracking in project management
3. Allocate dedicated debt paydown time

### Short-term (This Month)
1. Implement test infrastructure improvements
2. Create archive documentation improvements
3. Begin configuration management refactoring

### Long-term (This Quarter)
1. Complete all critical and high-priority debt
2. Establish debt prevention practices
3. Set up regular debt audit schedule

---

**Document Status:** Active - Update as debt is resolved or discovered
**Owner:** Development Team
**Review Frequency:** Monthly

---

*Technical debt is not a sign of failure - it's a sign of growth and learning. The key is managing it intentionally rather than letting it accumulate unchecked.*
