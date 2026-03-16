# Codebase Concerns

**Analysis Date:** 2026-02-19

## Tech Debt

**Empty Core Library:**
- Issue: Portless.Core project exists but contains no source code
- Files: `Portless.Core/Portless.Core.csproj`
- Impact: Violates purposeful architecture; core logic should reside here
- Fix approach: Move proxy configuration logic from Program.cs to Core library

**Missing Test Implementation:**
- Issue: Test project configured with dependencies but no actual tests
- Files: `Portless.Tests/Portless.Tests.csproj`
- Impact: No coverage validation, difficult to maintain code quality
- Fix approach: Add unit tests for proxy configuration and CLI functionality

**Package Reference Versions:**
- Issue: Hardcoded package versions without version management
- Files: `Portless.Proxy/Portless.Proxy.csproj`, `Portless.Cli/Portless.Cli.csproj`, `Portless.Tests/Portless.Tests.csproj`
- Impact: Manual version updates, potential dependency conflicts
- Fix approach: Implement global version ranges or central package management

## Known Bugs

**Memory Leak Potential:**
- Issue: In-memory configuration storage without cleanup
- Files: `Portless.Proxy/Program.cs` lines 14-16
- Trigger: Frequent configuration updates accumulate in memory
- Workaround: Implement configuration cleanup or size limits

**Hardcoded Empty Configuration:**
- Issue: Proxy loads with empty routes and clusters
- Files: `Portless.Proxy/Program.cs` line 6
- Impact: Service non-functional until configuration added
- Workaround: Load default configuration on startup

## Security Considerations

**Open Host Configuration:**
- Risk: No authentication on configuration endpoint
- Files: `Portless.Proxy/Program.cs` line 12-19
- Current mitigation: Local development only
- Recommendations: Add authentication for production deployments

**Wildcard Hosts Allowed:**
- Risk: Allows any host to connect
- Files: `Portless.Proxy/appsettings.json` line 8
- Current mitigation: Development environment
- Recommendations: Configure specific allowed hosts in production

**No Input Validation:**
- Risk: Raw configuration objects accepted without validation
- Files: `Portless.Proxy/Program.cs` line 24-27
- Current mitigation: Minimal surface area
- Recommendations: Add validation for route and cluster configurations

## Performance Bottlenecks

**In-Memory Configuration:**
- Problem: All configurations loaded into memory
- Files: `Portless.Proxy/Program.cs` line 6
- Cause: No persistence layer
- Improvement path: Implement configuration caching with limits

**Synchronous Configuration Updates:**
- Problem: Config updates block request processing
- Files: `Portless.Proxy/Program.cs` line 16
- Cause: Direct synchronous configuration modification
- Improvement path: Implement async configuration updates

## Fragile Areas

**Single Point of Failure:**
- Component: Main proxy service
- Files: `Portless.Proxy/Program.cs`
- Why fragile: All logic in single file, no error boundaries
- Safe modification: Extract configuration management to separate class
- Test coverage: None

**Direct Service Access:**
- Component: Configuration update endpoint
- Files: `Portless.Proxy/Program.cs` line 14
- Why fragile: Direct service access without abstraction
- Safe modification: Use dependency injection pattern
- Test coverage: None

## Scaling Limits

**Memory-Based Configuration:**
- Current capacity: Limited only by available memory
- Limit: No persistence, restart loses configuration
- Scaling path: Add database persistence for configurations

**Single Instance Proxy:**
- Current capacity: Single service instance
- Limit: No load balancing or failover
- Scaling path: Multiple proxy instances with shared configuration store

## Dependencies at Risk

**YARP Reverse Proxy:**
- Risk: Early version (2.3.0) with potential breaking changes
- Impact: Proxy functionality affected
- Migration plan: Monitor for updates, test upgrade paths

## Missing Critical Features

**No Configuration Persistence:**
- Problem: Configuration lost on service restart
- Blocks: Production deployment scenarios
- Missing: Database or file storage for configurations

**No Monitoring/Telemetry:**
- Problem: No observability into proxy performance
- Blocks: Troubleshooting and optimization
- Missing: Request logging, metrics, health checks

**No API Security:**
- Problem: Configuration endpoint unprotected
- Blocks: Production deployment
- Missing: Authentication, authorization, rate limiting

## Test Coverage Gaps

**No Unit Tests:**
- What's not tested: Core proxy functionality
- Files: `Portless.Tests/Portless.Tests.csproj`
- Risk: Code changes break functionality unnoticed
- Priority: High

**No Integration Tests:**
- What's not tested: Proxy routing behavior
- Files: No test files exist
- Risk: Configuration changes cause routing failures
- Priority: High

*Concerns audit: 2026-02-19*