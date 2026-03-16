# GSD to Superpowers Migration + v1.2 Completion Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Migrate Portless.NET from GSD framework to Superpowers framework, then complete v1.2 milestone by addressing 18 unsatisfied requirements (VERIFICATION files, Phase 17 lifecycle gaps, integration tests)

**Architecture:** Extract 93+ architectural decisions and 42 validated requirements from GSD structure, archive `.planning/` for historical reference, establish Superpowers workflow with specs/plans/decisions structure. Complete v1.2 through conversational UAT verification, Phase 17 lifecycle implementation if needed, and integration test coverage.

**Tech Stack:** .NET 10, C# 14, YARP 2.3.0, Git, Bash commands

**Prerequisites:**
- Git repository clean (no uncommitted changes)
- Current branch: `development` (not `main`)
- Latest changes pulled from remote
- Superpowers plugin installed and accessible

---

## Chunk 1: Migration - Extract Decisions and Requirements

**Time Estimate:** 45-60 minutes

### Task 1: Create Superpowers Directory Structure

**Files:**
- Create: `docs/superpowers/README.md`
- Create: `docs/superpowers/decisions.md`
- Create: `docs/superpowers/validated-requirements.md`

- [ ] **Step 1: Create Superpowers base directories**

```bash
mkdir -p docs/superpowers/specs
mkdir -p docs/superpowers/plans
```

Expected: Directories created successfully

- [ ] **Step 2: Create Superpowers README.md**

Create `docs/superpowers/README.md`:

```markdown
# Superpowers Framework Workflow

This project uses the Superpowers framework for development planning and execution.

## Quick Start

1. **Planning Phase:** Use `brainstorming` skill to explore design options
2. **Spec Creation:** Use `writing-plans` skill to create implementation specs
3. **Execution:** Use `executing-plans` skill for atomic, checkpoint-based work
4. **Verification:** Use `verification-before-completion` for UAT

## Key Documents

- **decisions.md:** All architectural and technical decisions (93+ decisions)
- **validated-requirements.md:** Accepted requirements by milestone (v1.0, v1.1, v1.2)
- **specs/**: Detailed implementation specifications
- **plans/**: Implementation plans with step-by-step tasks

## Migration from GSD

This project migrated from GSD framework on 2026-03-16.
See `.planning.archived/` for historical GSD planning documents.
See `specs/2026-03-16-gsd-to-superpowers-migration.md` for migration details.

## Superpowers Skills Used

- `brainstorming`: Design exploration and option analysis
- `writing-plans`: Structured implementation plan creation
- `executing-plans`: Atomic commits with clear checkpoints
- `verification-before-completion`: Conversational UAT before claiming done
- `systematic-debugging`: Scientific method debugging workflow

## Workflow Differences from GSD

| GSD | Superpowers | Notes |
|-----|-------------|-------|
| Phase-based plans | Spec-based plans | More flexible scope |
| /gsd:plan-phase | brainstorming + writing-plans | Two-step process |
| /gsd:execute-phase | executing-plans | Atomic commits emphasized |
| /gsd:verify-work | verification-before-completion | Conversational UAT focus |
```

- [ ] **Step 3: Commit Superpowers README**

```bash
git add docs/superpowers/README.md
git commit -m "docs: add Superpowers workflow README

- Document migration from GSD framework
- Explain Superpowers skills and workflow
- Provide quick start guide
- Map GSD concepts to Superpowers equivalents

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

Expected: Commit successful

---

### Task 1.5: Pre-Migration Checks

**Files:**
- Verify: `.planning/PROJECT.md`
- Verify: `.planning/STATE.md`
- Verify: `.planning/ROADMAP.md`
- Verify: `docs/superpowers/specs/2026-03-16-gsd-to-superpowers-migration.md`

- [ ] **Step 1: Verify GSD source files exist**

```bash
# Verify source files exist before extraction
test -f .planning/PROJECT.md || { echo "❌ ERROR: .planning/PROJECT.md not found"; exit 1; }
test -f .planning/STATE.md || { echo "❌ ERROR: .planning/STATE.md not found"; exit 1; }
test -f .planning/ROADMAP.md || { echo "❌ ERROR: .planning/ROADMAP.md not found"; exit 1; }
echo "✓ All GSD source files present"
```

Expected: All files present, no errors

- [ ] **Step 2: Verify migration spec exists**

```bash
# Verify spec file exists (created by brainstorming skill)
test -f docs/superpowers/specs/2026-03-16-gsd-to-superpowers-migration.md || { echo "❌ ERROR: Migration spec not found. Run brainstorming skill first."; exit 1; }
echo "✓ Migration spec found"
```

Expected: Spec file present

---

### Task 2: Extract Key Decisions from GSD

**Files:**
- Create: `docs/superpowers/decisions.md`
- Read: `.planning/PROJECT.md`
- Read: `.planning/STATE.md`

**Expected Decision Counts:**
- PROJECT.md Key Decisions table: 12 decisions
- STATE.md Accumulated Decisions: ~60 decisions (bullet-point format)
- Total extracted: ~72 decisions

- [ ] **Step 1: Extract PROJECT.md Key Decisions (12 decisions)**

Read `.planning/PROJECT.md` section "## Key Decisions" table (lines 166-186) and create `docs/superpowers/decisions.md`:

```markdown
# Architectural and Technical Decisions

Extracted from GSD framework (`.planning.archived/`) on 2026-03-16.

## Table of Contents

- [Architectural Decisions](#architectural-decisions)
- [Protocol Decisions](#protocol-decisions)
- [Certificate Decisions](#certificate-decisions)
- [Platform Decisions](#platform-decisions)
- [Testing Decisions](#testing-decisions)
- [CLI Decisions](#cli-decisions)
- [Phase-Level Decisions](#phase-level-decisions)

---

## Architectural Decisions

### Decision A1: YARP vs Custom Proxy

**Date:** 2026-02-19 (Project inception)
**Status:** ✓ Good Outcome

**Decision:** Use YARP (Yet Another Reverse Proxy) instead of building custom reverse proxy

**Rationale:**
- YARP is production-ready from Microsoft
- Native support for HTTP/2 and WebSockets
- Active development and community support
- Battle-tested at Microsoft scale

**Alternatives Considered:**
- Custom proxy implementation with Kestrel
- Ocelot (API Gateway)
- Reverse proxy middleware

**Outcome:**
- HTTP/1.1 proxy working correctly
- HTTP/2 support added in v1.1
- WebSocket transparent proxy working
- No regrets, YARP proven solid choice

**Files:** `Portless.Proxy/Program.cs`

---

### Decision A2: Evolutivo vs Feature-Complete v1

**Date:** 2026-02-19
**Status:** ✓ Good Outcome

**Decision:** Validate MVP first with HTTP basic proxy, then add complexity (HTTP/2, HTTPS) in later phases

**Rationale:**
- Reduce risk by validating core value proposition early
- Learn from real usage before adding features
- Avoid premature optimization
- Ship faster with working MVP

**Alternatives Considered:**
- Build all features (HTTP/2, HTTPS) from start
- Start with HTTPS only

**Outcome:**
- v1.0 MVP shipped in 8 phases (2026-02-21)
- v1.1 Advanced Protocols shipped (2026-02-22)
- v1.2 HTTPS in progress
- Phased approach allowed learning and iteration

---

### Decision A3: .NET 10 with Native AOT

**Date:** 2026-02-19
**Status:** ✓ Good Outcome

**Decision:** Use .NET 10 with Native AOT compilation for single binary deployment

**Rationale:**
- Single binary deployment (no runtime dependencies)
- Better performance than Node.js original
- Fast startup time (<100ms cold start)
- Smaller memory footprint

**Alternatives Considered:**
- .NET 8 (LTS)
- Traditional .NET deployment (requires runtime)
- Multi-file assembly

**Outcome:**
- PackAsTool working with acceptable warnings (ASP0000)
- 761KB NuGet package size
- Startup performance excellent
- Cross-platform single binary achieved

**Trade-offs:**
- Native AOT limitations with some reflection scenarios
- ASP0000 warnings during build (accepted)

---

### Decision A4: Spectre.Console.Cli over System.CommandLine

**Date:** 2026-02-19
**Status:** ✓ Good Outcome

**Decision:** Use Spectre.Console.Cli framework for CLI instead of System.CommandLine

**Rationale:**
- Better colored output and formatting
- More intuitive command/argument API
- Built-in progress bars and prompts
- Cleaner testability

**Alternatives Considered:**
- System.CommandLine (Microsoft official)
- McMaster.Extensions.CommandLineUtils
- Custom CLI parser

**Outcome:**
- CLI working correctly with colored output
- Commands: `proxy start/stop/status`, `list`, `cert install/status/uninstall`
- User-friendly help text
- Easy to add new commands

**Files:** `Portless.Cli/Commands/`

---

### Decision A5: PackAsTool Distribution

**Date:** 2026-02-21
**Status:** ✓ Good Outcome

**Decision:** Distribute as `dotnet tool` global tool using PackAsTool

**Rationale:**
- Standard .NET distribution mechanism
- Easy installation: `dotnet tool install --add-source . portless.dotnet`
- Automatic PATH management
- Version management via `dotnet tool update`

**Alternatives Considered:**
- Standalone exe download
- Chocolatey package
- Scoop installer
- NuGet package (not as tool)

**Outcome:**
- 761KB package size
- Simple installation workflow
- Good developer experience
- Integrates with .NET ecosystem

**Files:** `Portless.Cli/Portless.Cli.csproj`

---

### Decision A6: Cross-Platform Installation Scripts

**Date:** 2026-02-21
**Status:** ✓ Good Outcome

**Decision:** Provide bash and PowerShell scripts for PATH configuration

**Rationale:**
- Windows and Unix-like systems require different PATH setup
- Automate PATH configuration for better UX
- Follow Microsoft patterns for tool installation
- Reduce installation friction

**Alternatives Considered:**
- Manual PATH configuration (docs only)
- Single cross-platform script (not possible)
- No PATH automation (user manual setup)

**Outcome:**
- `scripts/install.ps1` for Windows
- `scripts/install.sh` for Linux/macOS
- Automatic PATH configuration working
- Users can run `portless` immediately after install

**Files:** `scripts/install.ps1`, `scripts/install.sh`

---

### Decision A7: Integration Examples

**Date:** 2026-02-21
**Status:** ✓ Good Outcome

**Decision:** Provide 4 example projects demonstrating Portless.NET integration

**Rationale:**
- Show real-world usage patterns
- Demonstrate PORT environment variable usage
- Cover common .NET application types (WebApi, Blazor, Worker, Console)
- Reduce onboarding friction

**Alternatives Considered:**
- Single comprehensive example
- No examples (documentation only)
- Community contributions only

**Outcome:**
- Examples/WebApi: ASP.NET Core API
- Examples/Blazor: Blazor Server app
- Examples/Worker: Background service
- Examples/Console: CLI application
- All using `PORT` environment variable consistently
- 3,049 lines of documentation and examples

**Files:** `Examples/`

---

### Decision A8: Progressive Documentation Strategy

**Date:** 2026-02-22
**Status:** ✓ Good Outcome

**Decision:** Build documentation progressively with each milestone, not all upfront

**Rationale:**
- Avoid documenting features that might change
- Learn from real usage to guide documentation
- Ship faster with minimal docs, improve iteratively
- Focus on actual pain points from users

**Alternatives Considered:**
- Complete documentation before v1.0
- Community-driven documentation only
- API docs auto-generation only

**Outcome:**
- v1.0: Basic documentation (README, CLI reference)
- v1.1: Protocol documentation (HTTP/2, WebSocket, SignalR)
- v1.2: Certificate management documentation
- 3,049 total lines across guides, tutorials, troubleshooting
- Documentation evolved with features

**Files:** `docs/`

---

### Decision A9: ForwardedHeaders vs YARP Transforms

**Date:** 2026-02-22
**Status:** ✓ Good Outcome

**Decision:** Use ASP.NET Core ForwardedHeaders middleware instead of YARP transforms for X-Forwarded-* headers

**Rationale:**
- Built-in middleware simpler than custom YARP transforms
- Standard ASP.NET Core pattern
- Less code to maintain
- Well-documented behavior

**Alternatives Considered:**
- YARP forward transform middleware
- Custom header injection
- No forwarded headers

**Outcome:**
- X-Forwarded-For, X-Forwarded-Proto, X-Forwarded-Host working correctly
- Backend services can detect original protocol
- HTTP/2 upgrades working with forwarded headers
- No issues reported

**Files:** `Portless.Proxy/Startup.cs`

---

### Decision A10: Kestrel Timeout Configuration

**Date:** 2026-02-22
**Status:** ✓ Good Outcome

**Decision:** Configure Kestrel with 10-minute timeouts for long-lived WebSocket connections

**Rationale:**
- WebSocket connections need to stay open for real-time apps
- Default 2-minute timeout too short for SignalR chat
- 10 minutes reasonable balance (not infinite)
- Configurable if needed

**Alternatives Considered:**
- Default timeouts (2 minutes)
- Infinite timeout (no timeout)
- 5-minute timeout

**Outcome:**
- SignalR chat working without disconnections
- WebSocket echo server working
- 10-minute timeout sufficient for real-time apps
- No timeout-related issues reported

**Configuration:** `Portless.Proxy/Program.cs` - Kestrel configuration

---

### Decision A11: SignalR Without YARP Special Config

**Date:** 2026-02-22
**Status:** ✓ Good Outcome

**Decision:** Don't add special YARP configuration for SignalR (WebSocket transport works automatically)

**Rationale:**
- Test if SignalR works through proxy without special handling
- YARP supports WebSocket by default
- Simpler configuration (less code)
- Can add special config later if needed

**Alternatives Considered:**
- Explicit WebSocket route configuration
- SignalR-specific YARP transforms
- Special headers for SignalR

**Outcome:**
- SignalR WebSocket transport works automatically
- No special configuration needed
- Chat example working end-to-end
- Minimal code, maximum simplicity

**Files:** `Examples/SignalRChat/`

---

### Decision A12: Echo Server vs Full Chat App

**Date:** 2026-02-22
**Status:** ✓ Good Outcome

**Decision:** Use simple echo server for WebSocket testing instead of full-featured chat application

**Rationale:**
- Echo server easier to test automatically
- No state management complexity
- Focus on WebSocket protocol, not app features
- Can add SignalR chat separately

**Alternatives Considered:**
- Full chat app from start
- No WebSocket example (SignalR only)
- Real-time dashboard example

**Outcome:**
- Echo server simple to test (unit + integration)
- WebSocket protocol coverage good
- SignalR chat added as separate example
- Clear separation of concerns

**Files:** `Examples/WebSocketEchoServer/`

---

### Decision A13: Windows Certificate Store Integration

**Date:** 2026-02-23
**Status:** ✓ Good Outcome

**Decision:** Use Windows X509Store API for certificate trust installation (LocalMachine Root store)

**Rationale:**
- Native Windows API for certificate management
- System-wide trust (all browsers accept)
- No manual certificate import required
- Standard Windows security model

**Alternatives Considered:**
- Current user store (user-specific trust)
- Manual certificate import (user action required)
- Firefox-specific trust store
- No trust installation (browser warnings)

**Outcome:**
- `portless cert install` working correctly
- System-wide trust achieved
- Browsers accept certificates without warnings
- Idempotent operations (install/uninstall)
- Platform guards for Windows-only code

**Constraints:**
- Windows 10+ only (macOS/Linux deferred to v1.3+)
- Requires admin permissions for LocalMachine store
- Manual installation instructions for non-Windows platforms

**Files:** `Portless.Core/Services/CertificateTruster.cs`

---

## Protocol Decisions

### Decision P1: HTTP/2 Support with ALPN Negotiation

**Date:** 2026-02-22 (Phase 9)
**Status:** ✓ Good Outcome

**Decision:** Enable HTTP/2 with ALPN negotiation in Kestrel, log protocol used for each connection

**Rationale:**
- HTTP/2 provides performance benefits (multiplexing, header compression)
- ALPN allows automatic negotiation with HTTP/1.1 fallback
- Protocol logging helps debug silent downgrades
- Standard Kestrel feature

**Outcome:**
- HTTP/2 working with compatible clients
- Automatic fallback to HTTP/1.1 for incompatible clients
- Protocol logging detects silent downgrades
- No protocol-related issues

**Files:** `Portless.Proxy/Program.cs`, `Portless.Proxy/Middleware/ProtocolLoggingMiddleware.cs`

---

### Decision P2: WebSocket Transparent Proxy

**Date:** 2026-02-22 (Phase 10)
**Status:** ✓ Good Outcome

**Decision:** Support WebSocket upgrades for both HTTP/1.1 and HTTP/2 (Extended CONNECT)

**Rationale:**
- WebSocket requires transparent proxy (no interception)
- HTTP/1.1 uses Upgrade header
- HTTP/2 uses Extended CONNECT method
- YARP supports both automatically

**Outcome:**
- WebSocket connections working for both protocols
- SignalR working through proxy
- Long-lived connections (10-minute timeout)
- Echo server and SignalR chat examples working

**Files:** `Portless.Proxy/Program.cs` (Kestrel configuration)

---

### Decision P3: SignalR Integration Example

**Date:** 2026-02-22 (Phase 11)
**Status:** ✓ Good Outcome

**Decision:** Create SignalR chat example demonstrating real-time bidirectional messaging

**Rationale:**
- SignalR is popular .NET real-time library
- Demonstrates WebSocket + fallback transport support
- Shows broadcast patterns
- Common use case for .NET developers

**Outcome:**
- SignalR chat with browser client
- Console client example
- Broadcast and direct messaging patterns
- Integration tests passing

**Files:** `Examples/SignalRChat/`

---

## Certificate Decisions

### Decision C1: .NET Native APIs for Certificate Generation

**Date:** 2026-02-22 (Phase 13)
**Status:** ✓ Good Outcome

**Decision:** Use .NET 10 native APIs (System.Security.Cryptography) for CA and certificate generation

**Rationale:**
- No external dependencies (OpenSSL, BouncyCastle)
- Cross-platform built-in APIs
- 5-year validity for development certificates
- Standard X.509 certificate format

**Outcome:**
- CertificateAuthority (4096-bit RSA, 5-year validity)
- Wildcard certificate (2048-bit RSA, SAN for *.localhost)
- PFX format with private keys
- Secure file permissions (chmod 600 on Unix)

**Files:** `Portless.Core/Services/CertificateService.cs`

---

### Decision C2: PFX Storage with JSON Metadata

**Date:** 2026-02-22 (Phase 13)
**Status:** ✓ Good Outcome

**Decision:** Store certificates as PFX files with accompanying JSON metadata

**Rationale:**
- PFX includes private key (single file)
- JSON metadata for certificate info (expiration, fingerprint)
- Three-file strategy: ca.pfx, cert.pfx, cert-info.json
- Easy certificate validation without loading PFX

**Outcome:**
- `~/.portless/ca.pfx` (Certificate Authority)
- `~/.portless/cert.pfx` (Wildcard server certificate)
- `~/.portless/cert-info.json` (Metadata: expiration, fingerprint, subject)
- Secure permissions enforced

**Files:** `Portless.Core/Services/CertificateService.cs`

---

### Decision C3: Secure Cross-Platform File Permissions

**Date:** 2026-02-22 (Phase 13)
**Status:** ✓ Good Outcome

**Decision:** Implement platform-specific file permission enforcement (chmod 600 on Unix, ACL on Windows)

**Rationale:**
- Private keys must be protected
- Unix: chmod 600 (owner read/write only)
- Windows: ACL with DACL
- Security warnings if permissions incorrect

**Outcome:**
- FilePermissionService with platform-specific logic
- Automatic permission enforcement on certificate creation
- Security warnings on startup if permissions incorrect
- Cross-platform compatible

**Files:** `Portless.Core/Services/FilePermissionService.cs`

---

### Decision C4: Windows Certificate Store for Trust

**Date:** 2026-02-23 (Phase 14)
**Status:** ✓ Good Outcome

**Decision:** Use Windows X509Store (LocalMachine Root) for CA certificate trust installation

**Rationale:**
- System-wide trust (all browsers respect)
- Native Windows API
- Idempotent operations (install twice succeeds)
- Platform guards prevent execution on non-Windows

**Outcome:**
- `portless cert install` working
- `portless cert status` checking trust state
- `portless cert uninstall` for cleanup
- Cross-platform messaging for macOS/Linux (manual instructions)

**Constraints:**
- Windows 10+ only
- Requires admin for LocalMachine store
- Manual trust for macOS/Linux (documented limitation)

**Files:** `Portless.Core/Services/CertificateTruster.cs`

---

### Decision C5: Fixed HTTP/HTTPS Ports

**Date:** 2026-02-23 (Phase 15)
**Status:** ✓ Good Outcome

**Decision:** Use fixed ports (HTTP=1355, HTTPS=1356) with PORTLESS_PORT deprecation warning

**Rationale:**
- Simplified HTTPS configuration (no port conflicts)
- Breaking change but simpler UX
- Port 1356 reserved for HTTPS
- Deprecation warning for PORTLESS_PORT env var

**Outcome:**
- No port configuration needed for HTTPS
- HTTP→HTTPS redirect on port 1356
- Certificate pre-startup validation
- TLS 1.2+ minimum protocol enforced

**Trade-offs:**
- Breaking change from configurable ports
- Simplicity over flexibility

**Files:** `Portless.Proxy/Program.cs`

---

### Decision C6: X-Forwarded-Proto Headers

**Date:** 2026-03-02 (Phase 16)
**Status:** ✓ Good Outcome

**Decision:** Add X-Forwarded-Proto headers for mixed HTTP/HTTPS backend support

**Rationale:**
- Backend services need to know original protocol
- HTTP backend receives `X-Forwarded-Proto: http`
- HTTPS backend receives `X-Forwarded-Proto: https`
- YARP HttpClient configuration for SSL validation

**Outcome:**
- Mixed backend routing working
- Self-signed certificate acceptance in development
- Protocol forwarding transparent to backend
- Integration tests covering header preservation

**Files:** `Portless.Proxy/Program.cs` (YARP configuration)

---

### Decision C7: Background Certificate Monitoring

**Date:** 2026-02-23 (Phase 17)
**Status:** ✓ Good (Planned)

**Decision:** Implement IHostedService for certificate expiration monitoring (6-hour interval)

**Rationale:**
- Automatic expiration detection
- 30-day warning window
- Auto-renewal within warning window
- Configurable via environment variables

**Status:** Planned (implementation pending investigation)

**Environment Variables:**
- `PORTLESS_CERT_WARNING_DAYS` (default: 30)
- `PORTLESS_CERT_CHECK_INTERVAL_HOURS` (default: 6)
- `PORTLESS_AUTO_RENEW` (default: true)
- `PORTLESS_ENABLE_MONITORING` (default: false)

**Files:** `Portless.Proxy/Services/CertificateMonitor.cs` (if exists)

---

### Decision C8: Certificate Metadata JSON

**Date:** 2026-02-23 (Phase 17)
**Status:** ✓ Good (Planned)

**Decision:** Store certificate metadata in `~/.portless/cert-info.json` (creation, expiration, fingerprint)

**Rationale:**
- Quick certificate info without loading PFX
- Expiration warning calculation
- Fingerprint validation
- CLI status command uses this

**Status:** Planned (implementation pending investigation)

**Metadata Fields:**
```json
{
  "createdAt": "2026-02-22T10:00:00Z",
  "expiresAt": "2031-02-22T10:00:00Z",
  "fingerprint": "SHA256_HASH",
  "subject": "CN=localhost",
  "issuer": "CN=Portless Development CA"
}
```

**Files:** `~/.portless/cert-info.json` (if exists)

---

## Platform Decisions

### Decision PL1: Windows-First Development

**Date:** 2026-02-19
**Status:** ✓ Good Outcome

**Decision:** Prioritize Windows 10+ support, validate macOS/Linux in later milestones

**Rationale:**
- Portless original has no Windows support (key differentiator)
- Most .NET developers use Windows
- Faster MVP by focusing on one platform
- Cross-platform validation deferred to v1.3+

**Outcome:**
- Windows 10+ fully supported
- macOS and Linux basic support (deferred full validation)
- Platform guards with [SupportedOSPlatform] attributes
- Cross-platform messaging where features missing

**Constraints:**
- Certificate trust: Windows only (macOS/Linux manual)
- Certificate lifecycle: Windows only
- Full cross-platform validation: v1.3+

**Files:** Multiple files with platform guards

---

### Decision PL2: Platform Guards for Windows-Only Features

**Date:** 2026-02-23
**Status:** ✓ Good Outcome

**Decision:** Use [SupportedOSPlatform] and OperatingSystem.IsWindows() for platform-specific code

**Rationale:**
- Clear platform requirements
- Runtime checks for platform
- Compile-time warnings for unsupported platforms
- Graceful degradation with helpful error messages

**Outcome:**
- Certificate trust installation: Windows-only with clear error on other platforms
- Platform-specific services guarded
- Cross-platform messaging for manual workarounds
- No silent failures on unsupported platforms

**Files:** `Portless.Core/Services/CertificateTruster.cs`

---

## Testing Decisions

### Decision T1: WebApplicationFactory for Integration Tests

**Date:** 2026-02-21 (Phase 8)
**Status:** ✓ Good Outcome

**Decision:** Use ASP.NET Core WebApplicationFactory for integration testing

**Rationale:**
- In-memory test server (no real TCP ports)
- Fast test execution
- Full middleware pipeline testing
- Standard ASP.NET Core testing pattern

**Outcome:**
- 45 integration tests passing
- Test isolation with temp directories
- HTTPS configuration testing (not actual TLS handshake)
- Certificate generation and renewal tests

**Files:** `Portless.IntegrationTests/`

---

### Decision T2: Test Isolation with PORTLESS_STATE_DIR

**Date:** 2026-03-02 (Phase 18)
**Status:** ✓ Good Outcome

**Decision:** Use PORTLESS_STATE_DIR environment variable for test isolation

**Rationale:**
- Each test gets unique temp directory
- No certificate file conflicts between tests
- Cleanup after test completion
- IAsyncLifetime for temp directory management

**Outcome:**
- Tests running in parallel without conflicts
- Clean test state for each test
- Temp directory cleanup after tests
- No file leakage

**Files:** `Portless.IntegrationTests/Certificate/CertificateGenerationTests.cs`

---

### Decision T3: Configuration Verification vs TLS Handshake

**Date:** 2026-03-02 (Phase 18)
**Status:** ✓ Good Outcome

**Decision:** Test HTTPS configuration and certificate properties, not actual TLS handshake

**Rationale:**
- WebApplicationFactory doesn't bind real TCP ports
- Can't test actual TLS handshake in TestServer
- Verify certificate properties (key size, validity, SAN extensions)
- Test endpoint configuration and binding

**Outcome:**
- HTTPS endpoint configuration tested
- Certificate properties verified
- TLS protocol enforcement tested
- Certificate binding validated
- Documented test limitations

**Files:** `Portless.IntegrationTests/Https/HttpsEndpointTests.cs`

---

## CLI Decisions

### Decision CLI1: Colored Output with Spectre.Console

**Date:** 2026-02-19
**Status:** ✓ Good Outcome

**Decision:** Use Spectre.Console for colored CLI output (red=error, yellow=warning, green=success)

**Rationale:**
- Better UX with visual feedback
- Color-coded status information
- Cross-platform color support
- Consistent with Spectre.Console.Cli framework

**Outcome:**
- Certificate status with color coding (expired=red, expiring=yellow, valid=green)
- Error messages in red
- Success messages in green
- Warnings in yellow

**Files:** `Portless.Cli/Commands/` (multiple commands)

---

### Decision CLI2: Exit Codes Following CONTEXT.md Spec

**Date:** 2026-02-23
**Status:** ✓ Good Outcome

**Decision:** Use standardized exit codes (0=success, 1=generic/platform, 2=permissions, 3=missing, 5=store access)

**Rationale:**
- Scriptable CLI requires meaningful exit codes
- Standardize across all commands
- Follow CONTEXT.md specification
- Enables automation and CI/CD integration

**Outcome:**
- All commands use consistent exit codes
- `portless cert install`: 2 if not admin
- `portless cert status`: 3 if certificates missing
- `portless proxy start`: 1 if generic error
- Exit codes documented in help

**Files:** `Portless.Cli/Commands/` (all commands)

---

## Phase-Level Decisions (80+ decisions from STATE.md Accumulated Context)

### Phase 13: Certificate Generation (2026-02-22)

**Decision 13-01:** Use .NET native APIs for certificate generation (no external dependencies)
**Decision 13-02:** CA certificate validity: 5 years (development certificates)
**Decision 13-03:** Server certificate validity: 5 years (matches CA)
**Decision 13-04:** CA key size: 4096-bit RSA (security margin)
**Decision 13-05:** Server certificate key size: 2048-bit RSA (balance security/performance)
**Decision 13-06:** SAN extensions: localhost, *.localhost, 127.0.0.1, ::1 (comprehensive coverage)
**Decision 13-07:** Three-file storage: ca.pfx, cert.pfx, cert-info.json (separation of concerns)
**Decision 13-08:** Certificate auto-regeneration on corruption detection
**Decision 13-09:** First-time generation user notification (logger prompt)
**Decision 13-10:** Existing certificate reuse without prompting

---

### Phase 14: Trust Installation (2026-02-23)

**Decision 14-01:** Windows X509Store API for trust management (LocalMachine Root store)
**Decision 14-02:** System-wide trust via LocalMachine store (requires admin)
**Decision 14-03:** Idempotent install operations (install twice succeeds)
**Decision 14-04:** Idempotent uninstall operations (uninstall non-existent cert succeeds)
**Decision 14-05:** Platform guards with [SupportedOSPlatform("windows10.0.0")]
**Decision 14-06:** Runtime checks with OperatingSystem.IsWindows()
**Decision 14-07:** Cross-platform messaging for macOS/Linux (3-5 line manual instructions)
**Decision 14-08:** Trust status detection with 30-day expiration warning
**Decision 14-09:** Exit codes follow CONTEXT.md specification
**Decision 14-10:** Certificate fingerprint validation for trust status

---

### Phase 15: HTTPS Endpoint (2026-02-23)

**Decision 15-01:** Fixed ports enforced (HTTP=1355, HTTPS=1356)
**Decision 15-02:** PORTLESS_PORT deprecation warning
**Decision 15-03:** 308 Permanent Redirect for HTTP→HTTPS (preserves POST methods)
**Decision 15-04:** /api/v1/* endpoints excluded from HTTPS redirect (CLI needs HTTP access)
**Decision 15-05:** Certificate pre-startup validation exits with code 1 if invalid
**Decision 15-06:** Clear error message: run 'portless cert install'
**Decision 15-07:** TLS 1.2+ minimum protocol via ConfigureHttpsDefaults
**Decision 15-08:** Temporary BuildServiceProvider for certificate loading (accept ASP0000 warning)

---

### Phase 16: Mixed Protocol Support (2026-03-02)

**Decision 16-01:** YARP HttpClient configuration for mixed HTTP/HTTPS backends
**Decision 16-02:** Development SSL validation accepts self-signed certificates
**Decision 16-03:** X-Forwarded-Proto header: http for HTTP backends
**Decision 16-04:** X-Forwarded-Proto header: https for HTTPS backends
**Decision 16-05:** Backend SSL validation disabled in development mode

---

### Phase 17: Certificate Lifecycle (2026-02-23)

**Decision 17-01:** Background monitoring with IHostedService
**Decision 17-02:** 6-hour check interval (configurable via PORTLESS_CERT_CHECK_INTERVAL_HOURS)
**Decision 17-03:** 30-day expiration warning (configurable via PORTLESS_CERT_WARNING_DAYS)
**Decision 17-04:** Auto-renewal enabled by default (PORTLESS_AUTO_RENEW=true)
**Decision 17-05:** Auto-renewal within 30 days of expiration
**Decision 17-06:** Restart required after renewal (documented limitation)
**Decision 17-07:** Certificate metadata in cert-info.json (creation, expiration, fingerprint)
**Decision 17-08:** CLI colored output for certificate status (red/yellow/green)
**Decision 17-09:** Environment variable configuration for thresholds

---

### Phase 18: Integration Tests (2026-03-02)

**Decision 18-01:** WebApplicationFactory for HTTPS testing (configuration, not TLS handshake)
**Decision 18-02:** PORTLESS_STATE_DIR for test isolation (temp directories)
**Decision 18-03:** Certificate private key export with CopyWithPrivateKey()
**Decision 18-04:** Server certificate validity ≤ CA certificate validity (prevents signing errors)
**Decision 18-05:** IAsyncLifetime for temp directory cleanup
**Decision 18-06:** HTTP endpoint tested via /api/v1/add-host (not /api/v1/status)
**Decision 18-07:** TLS protocol enforcement via certificate properties (2048-bit, validity period)

---

### Phase 19: Documentation (2026-03-02)

**Decision 19-01:** Migration guide structure follows v1.0-to-v1.1 pattern
**Decision 19-02:** FAQ troubleshooting format: Symptom → Diagnosis → Cause → Solutions → Prevention
**Decision 19-03:** Platform availability warnings prominently displayed
**Decision 19-04:** Certificate lifecycle documentation with CLI commands
**Decision 19-05:** Security considerations for development certificates

---

## Summary Statistics

**Total Decisions:** 93+
- Architectural: 13
- Protocol: 3
- Certificate: 8
- Platform: 2
- Testing: 3
- CLI: 2
- Phase-Level: 62+

**Outcomes:**
- ✓ Good: 90+
- ⚠️ Partial: 2 (Phase 17 pending verification)
- ✓ Good (Planned): 1 (background monitoring)

**Date Range:** 2026-02-19 to 2026-03-02

---

## Related Documents

- **Migration Spec:** `specs/2026-03-16-gsd-to-superpowers-migration.md`
- **GSD Archive:** `.planning.archived/PROJECT.md` (original decisions)
- **GSD Archive:** `.planning.archived/STATE.md` (phase-level decisions)
- **Validation:** See extraction validation in migration plan Step 1.5

---

*Extracted: 2026-03-16*
*Sources: .planning.archived/PROJECT.md, .planning.archived/STATE.md*
*Total: 93+ architectural and technical decisions*
```

Expected: File created successfully

- [ ] **Step 2: Validate decision extraction**

```bash
# Count extracted decisions (should be ~72: 12 from PROJECT.md + ~60 from STATE.md)
# Pattern matches both "### Decision A1" and "**Decision 13-01**" formats
grep -E "^### Decision|^\*\*Decision [A-Z0-9]+" docs/superpowers/decisions.md | wc -l

# Verify key sections present
grep -q "Architectural Decisions" docs/superpowers/decisions.md && echo "✓ Architectural section present"
grep -q "Phase-Level Decisions" docs/superpowers/decisions.md && echo "✓ Phase-Level section present"

# Spot-check: Verify first and last decisions exist
grep -q "Decision A1: YARP vs Custom Proxy" docs/superpowers/decisions.md && echo "✓ First architectural decision present"
grep -q "Decision 19-05" docs/superpowers/decisions.md && echo "✓ Last phase decision present"
```

Expected Output:
- Decision count: ~72 (31 architectural + ~40 phase-level)
- All sections present
- First and last decisions present

- [ ] **Step 3: Commit decisions.md**

```bash
git add docs/superpowers/decisions.md
git commit -m "docs: extract ~72 architectural decisions from GSD

- Extract 12 key decisions from PROJECT.md Key Decisions table
- Extract ~60 phase-level decisions from STATE.md Accumulated Context
- Organize by category: Architectural, Protocol, Certificate, Platform, Testing, CLI
- Document outcomes and rationale for each decision
- Provide statistics and related document references

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

Expected: Commit successful

---

### Task 3: Extract Validated Requirements

**Files:**
- Create: `docs/superpowers/validated-requirements.md`
- Read: `.planning/PROJECT.md`

- [ ] **Step 1: Create validated-requirements.md**

Create `docs/superpowers/validated-requirements.md`:

```markdown
# Validated Requirements

Extracted from GSD framework (`.planning.archived/PROJECT.md`) on 2026-03-16.

## Milestone v1.0 MVP (Complete 2026-02-21)

**Total Requirements:** 20
**Status:** ✅ All Validated

### Proxy Core (PROXY)

- ✅ PROXY-01: HTTP proxy with routing by hostname
- ✅ PROXY-02: HTTP/1.1 complete implementation
- ✅ PROXY-03: Reverse proxy using YARP
- ✅ PROXY-04: Headers X-Forwarded-* correct

### Route Persistence (ROUTE)

- ✅ ROUTE-01: Persistence in JSON file
- ✅ ROUTE-02: File locking for concurrency
- ✅ ROUTE-03: Hot reload of configuration

### Port Management (PORT)

- ✅ PORT-01: Automatic port detection (4000-4999 range)
- ✅ PORT-02: Port availability verification
- ✅ PORT-03: Pool of reusable ports

### Process Management (PROC)

- ✅ PROC-01: Spawn command with PORT env var
- ✅ PROC-02: PID tracking
- ✅ PROC-03: Cleanup on exit
- ✅ PROC-04: Signal forwarding (SIGTERM, SIGINT)

### CLI Commands (CLI)

- ✅ CLI-START: `portless proxy start` command
- ✅ CLI-STOP: `portless proxy stop` command
- ✅ CLI-LIST: `portless list` command
- ✅ CLI-RUN: `portless <name> <command...>` execution

### .NET Integration (NET)

- ✅ NET-01: PackAsTool for global tool distribution
- ✅ NET-02: launchSettings.json integration examples
- ✅ NET-03: appsettings.json integration examples

### Testing (TEST)

- ✅ TEST-V1: Integration test automation
- ✅ TEST-COV: >80% code coverage target

---

## Milestone v1.1 Advanced Protocols (Complete 2026-02-22)

**Total Requirements:** 15
**Status:** ✅ All Validated

### HTTP/2 Support (HTTP2)

- ✅ HTTP2-01: HTTP/2 support with Kestrel
- ✅ HTTP2-02: ALPN negotiation with HTTP/1.1 fallback
- ✅ HTTP2-03: Protocol logging with silent downgrade detection

### WebSocket Support (WS)

- ✅ WS-01: WebSocket transparent proxy (HTTP/1.1 upgrade)
- ✅ WS-02: WebSocket for HTTP/2 (Extended CONNECT)
- ✅ WS-03: Long-lived connections (10-min timeout)

### SignalR Integration (SIGNALR)

- ✅ SIGNALR-01: SignalR chat example
- ✅ SIGNALR-02: Browser client example
- ✅ SIGNALR-03: Console client example

### Integration Tests (TEST)

- ✅ TEST-HTTP2: HTTP/2 integration tests
- ✅ TEST-WS: WebSocket integration tests
- ✅ TEST-SIGNALR: SignalR integration tests

### Documentation (DOCS)

- ✅ DOCS-HTTP2: HTTP/2 and WebSocket guide
- ✅ DOCS-MIGRATION: v1.0 to v1.1 migration guide
- ✅ DOCS-TROUBLESHOOT: Protocol troubleshooting guide
- ✅ DOCS-PROTOCOL: Protocol testing documentation

---

## Milestone v1.2 HTTPS (Partial - Certificate Management Complete 2026-02-23)

**Total Requirements:** 36 (18 satisfied, 18 unsatisfied per audit)
**Status:** ⚠️ 50% Complete - VERIFICATION files pending

### Certificate Generation (CERT)

- ✅ CERT-01: CA auto-generation with .NET native APIs
- ✅ CERT-02: CA 5-year validity
- ✅ CERT-03: Wildcard certificate for *.localhost
- ✅ CERT-04: SAN extensions (localhost, *.localhost, 127.0.0.1, ::1)
- ✅ CERT-05: Server 5-year validity
- ✅ CERT-06: Exportable private keys
- ⚠️ CERT-07: Secure file permissions (partial - needs VERIFICATION)
- ⚠️ CERT-08: Certificate persistence (partial - needs VERIFICATION)
- ✅ CERT-09: .NET native APIs used

### Trust Installation (TRUST)

- ✅ TRUST-01: Install command via Windows Certificate Store
- ✅ TRUST-02: Windows LocalMachine Root store
- ⚠️ TRUST-03: Trust status command (partial - needs VERIFICATION)
- ✅ TRUST-04: Platform detection (OperatingSystem.IsWindows)
- ✅ TRUST-05: Uninstall command
- ✅ TRUST-06: macOS/Linux documentation

### CLI Commands (CLI)

- ⚠️ CLI-01: Install CLI command (partial - needs VERIFICATION)
- ⚠️ CLI-02: Status CLI command (partial - needs VERIFICATION)
- ⚠️ CLI-03: Renew CLI command (needs implementation + VERIFICATION)
- ⚠️ CLI-04: Uninstall CLI command (partial - needs VERIFICATION)
- ✅ CLI-05: HTTPS flag (`--https`)
- ⚠️ CLI-06: Colored certificate output (needs implementation + VERIFICATION)

### HTTPS Endpoint (HTTPS)

- ✅ HTTPS-01: Dual HTTP/HTTPS endpoints (1355/1356)
- ❌ HTTPS-02: Configurable HTTPS port (fixed port 1356 - breaking change)
- ✅ HTTPS-03: Certificate binding
- ✅ HTTPS-04: TLS 1.2+ minimum protocol
- ✅ HTTPS-05: HTTP backward compatibility

### Mixed Protocol Support (MIXED)

- ✅ MIXED-01: X-Forwarded-Proto header for HTTP backends
- ✅ MIXED-02: X-Forwarded-Proto header for HTTPS backends
- ✅ MIXED-03: YARP HttpClient configuration
- ✅ MIXED-04: Mixed routing (HTTP + HTTPS backends)
- ✅ MIXED-05: SSL validation for self-signed certs

### Certificate Lifecycle (LIFECYCLE)

- ❌ LIFECYCLE-01: Startup certificate check (needs VERIFICATION)
- ❌ LIFECYCLE-02: Expiration warning within 30 days (needs VERIFICATION)
- ❌ LIFECYCLE-03: Background monitoring service (needs VERIFICATION)
- ❌ LIFECYCLE-04: Auto-renewal within 30 days (needs VERIFICATION)
- ❌ LIFECYCLE-05: Renew command (needs VERIFICATION)
- ❌ LIFECYCLE-06: Restart required after renewal (needs VERIFICATION)
- ❌ LIFECYCLE-07: Certificate metadata storage (needs VERIFICATION)

### Testing (TEST)

- ✅ TEST-01: Certificate generation tests (SAN extensions)
- ⚠️ TEST-02: HTTPS endpoint tests (partial - needs VERIFICATION)
- ❌ TEST-03: X-Forwarded-Proto header tests (needs implementation)
- ✅ TEST-04: Certificate renewal tests
- ⚠️ TEST-05: Trust status tests (partial - needs VERIFICATION)
- ❌ TEST-06: Mixed HTTP/HTTPS backend routing tests (needs implementation)

### Documentation (DOCS)

- ⚠️ DOCS-01: User guide for certificate management (created but checkbox not marked)
- ⚠️ DOCS-02: Troubleshooting guide (created but checkbox not marked)
- ⚠️ DOCS-03: Migration guide v1.1 to v1.2 (created but checkbox not marked)
- ⚠️ DOCS-04: Platform-specific notes (created but checkbox not marked)
- ⚠️ DOCS-05: Security considerations (created but checkbox not marked)

**Note:** All DOCS requirements implemented in Phase 19 but REQUIREMENTS.md checkboxes not marked. VERIFICATION will confirm completion.

---

## Requirements Statistics

**Total Validated Requirements:** 62
- v1.0 MVP: 20 ✅
- v1.1 Advanced Protocols: 15 ✅
- v1.2 HTTPS: 7 ✅ (partial milestone, 18/36 satisfied)

**Total Unsatisfied Requirements:** 18
- Certificate lifecycle: 9 (LIFECYCLE-01 through 07, CLI-03, CLI-06)
- Integration tests: 2 (TEST-03, TEST-06)
- Verification gaps: 7 (need VERIFICATION.md files)

**Requirements Status Legend:**
- ✅ Validated: Implemented and tested
- ⚠️ Partial: Implemented but needs formal verification
- ❌ Unsatisfied: Not implemented or needs investigation

---

## Next Steps

1. **Create VERIFICATION.md files** for phases 13-19 (7 files)
2. **Investigate Phase 17 lifecycle features** (determine if implementation needed)
3. **Complete missing integration tests** (TEST-03, TEST-06)
4. **Formally validate all 18 unsatisfied requirements**

See: `plans/2026-03-16-gsd-to-superpowers-migration.md` for implementation plan

---

*Extracted: 2026-03-16*
*Source: .planning.archived/PROJECT.md Validated Requirements section*
*Total: 42 validated requirements (20 + 15 + 7)*
*Pending: 18 unsatisfied requirements requiring VERIFICATION or implementation*
```

Expected: File created successfully

- [ ] **Step 2: Validate requirement extraction**

```bash
# Count requirements (should be 62 validated)
echo "v1.0 requirements:" && grep -c "✅.*PROXY\|✅.*ROUTE\|✅.*PORT\|✅.*PROC\|✅.*CLI\|✅.*NET\|✅.*TEST" docs/superpowers/validated-requirements.md || true
echo "v1.1 requirements:" && grep -c "✅.*HTTP2\|✅.*WS\|✅.*SIGNALR\|✅.*DOCS" docs/superpowers/validated-requirements.md || true
echo "v1.2 requirements:" && grep -c "✅.*CERT\|✅.*TRUST\|✅.*HTTPS\|✅.*MIXED\|✅.*TEST" docs/superpowers/validated-requirements.md || true

# Verify unsatisfied requirements listed
grep -q "18 Unsatisfied Requirements" docs/superpowers/validated-requirements.md && echo "✓ Unsatisfied section present"
```

Expected Output:
- v1.0 requirements: ~20
- v1.1 requirements: ~15
- v1.2 requirements: ~7
- Unsatisfied section present

- [ ] **Step 3: Commit validated-requirements.md**

```bash
git add docs/superpowers/validated-requirements.md
git commit -m "docs: extract validated requirements from GSD

- Extract 42 validated requirements across v1.0, v1.1, v1.2 partial
- Document 18 unsatisfied requirements from v1.2 audit
- Organize by milestone and category (PROXY, ROUTE, CERT, etc.)
- Provide status legend and next steps for VERIFICATION
- Cross-reference with migration implementation plan

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

Expected: Commit successful

---

**End of Chunk 1**

**Summary:**
- Created Superpowers directory structure
- Created README.md explaining Superpowers workflow
- Extracted 93+ decisions from GSD to `decisions.md`
- Extracted 42 validated requirements to `validated-requirements.md`
- Validated extraction completeness (decision counts, requirement counts)
- Committed all extraction files

**Next:** Review this chunk, then proceed to Chunk 2 (Archive GSD structure)

---

### Rollback Procedure (if Chunk 1 fails)

**If extraction fails midway, use this rollback:**

```bash
# Clean up partial work
rm -rf docs/superpowers/

# Undo the 3 commits from this chunk
git reset HEAD~3

echo "✓ Chunk 1 rolled back - ready to retry"
```

**Use rollback if:**
- Source files not found (pre-checks fail)
- Validation commands show wrong counts
- Content extraction incomplete or corrupted
