# Phase 19: Documentation - Context

**Gathered:** 2026-03-02
**Status:** Ready for planning

## Phase Boundary

Complete user-facing documentation for HTTPS certificate management in v1.2. This includes user guides, troubleshooting documentation, migration guides, and platform-specific notes to enable users to effectively use and manage HTTPS certificates with Portless.NET.

**Scope:** Documentation for certificate management (install, status, uninstall), troubleshooting (untrusted CA, expired cert, SAN mismatch, permissions), migration from v1.1 HTTP-only to v1.2 HTTPS, and platform-specific guidance (Windows Certificate Store integration in v1.2, macOS/Linux deferred to v1.3).

## Implementation Decisions

### User Guide Organization

- **Update existing `certificate-lifecycle.md`** rather than creating new files
  - Keeps all certificate information consolidated in one place
  - Missing commands (install, status, uninstall) will be added

- **Create "Certificate Trust" section** within `certificate-lifecycle.md`
  - Groups install/status/uninstall commands together
  - Separate from lifecycle/monitoring content
  - Logical separation: trust management vs lifecycle management

- **Intermediate documentation level** - Portless.NET specific
  - Users know what HTTPS certificates are
  - Focus on Portless.NET commands and Windows Certificate Store integration
  - Don't explain basic certificate concepts

- **Discoverability via `docs/README.md`**
  - Add prominent links: "Installing Certificates", "Checking Trust Status", "Uninstalling Certificates"
  - Links in quick start section

### Troubleshooting Guide

- **Expand existing troubleshooting section** in `certificate-lifecycle.md`
  - Not a standalone file
  - Keep everything consolidated

- **FAQ style - quick reference format**
  - Problem → Solution in 2-3 lines each
  - 10-15 common issues total
  - Practical and action-oriented

- **Cover four priority issue types:**
  1. Untrusted CA warnings (browser warnings, portless cert status shows "Not Trusted")
  2. Expired certificates (certificate expired, auto-renewal not working, restart required)
  3. SAN mismatch errors (hostname not in certificate SANs, wildcard certificate issues)
  4. File permission errors (permission denied reading ~/.portless/, file permissions too open, Windows ACL issues)

- **Organize by symptom/error message**
  - User-friendly: "I see X" → find solution quickly
  - Examples: "Browser shows warning", "Command fails with error", "HTTPS not working"

### Migration Guide (v1.1 to v1.2)

- **Mirror `migration-v1.0-to-v1.1.md` structure**
  - Maintains consistency across migration guides
  - Structure: Overview → What's New → Breaking Changes → Configuration Changes → CLI Changes → Troubleshooting → Summary

- **Zero breaking changes** - HTTPS is opt-in
  - Existing HTTP-only setups work unchanged
  - HTTPS enabled via `--https` flag
  - No code changes required in applications

- **"What's New in v1.2" highlights all features:**
  1. HTTPS endpoints and certificates (automatic generation, `--https` flag)
  2. New certificate management commands (install, status, uninstall)
  3. Automatic renewal and monitoring (expiration checking, background monitoring)
  4. Mixed HTTP/HTTPS mode (both endpoints can run simultaneously)

- **No rollback section**
  - v1.2 is backward compatible
  - Users just don't use `--https` flag to stay on HTTP
  - Keeps guide focused on forward migration

### Platform-Specific Documentation

- **Main doc + appendix pattern**
  - Windows documentation in `certificate-lifecycle.md` (primary)
  - macOS/Linux manual steps in appendix file
  - Clear separation between supported (v1.2) and unsupported (v1.3) platforms

- **Portless commands focused for Windows**
  - Document Portless.NET commands
  - Minimal OS-level Certificate Store details
  - Focus on what Portless does, not how Windows works internally
  - Users can refer to Windows docs for deep OS details

- **Appendix: Complete manual steps for macOS/Linux**
  - Full manual installation steps with warnings
  - Helpful for advanced users on non-Windows platforms
  - Clear warnings: automatic installation not yet supported
  - Coming in v1.3

- **Warning box in main doc**
  - Prominent "Platform Support" box at top of `certificate-lifecycle.md`
  - Format: "v1.2: Windows automatic | macOS/Linux manual (v1.3)"
  - Makes platform limitations immediately visible

## Specific Ideas

- Follow established documentation patterns from `migration-v1.0-to-v1.1.md`
- Use FAQ style similar to existing troubleshooting in certificate-lifecycle.md
- Certificate commands already documented in certificate-lifecycle.md (check, renew) - extend this pattern
- Platform support warnings should be immediately visible, not buried in text

## Existing Code Insights

### Reusable Assets

- **`certificate-lifecycle.md`** - Existing comprehensive doc covering:
  - Certificate status commands (`portless cert check`)
  - Renew commands (`portless cert renew`)
  - Automatic monitoring configuration
  - Environment variables
  - Certificate files and metadata
  - Basic troubleshooting section (5 issues)
  - **Action:** Expand this file with Certificate Trust section and enhanced troubleshooting

- **`certificate-security.md`** - Complete security considerations:
  - Private key protection
  - Trust implications
  - File permissions
  - Certificate regeneration
  - Development vs production certificates
  - **Action:** Reference this from trust/install sections, don't duplicate

- **`migration-v1.0-to-v1.1.md`** - Migration guide pattern:
  - Structure template
  - Breaking changes section format
  - CLI changes documentation
  - Troubleshooting section
  - Summary section
  - **Action:** Mirror this structure for `migration-v1.1-to-v1.2.md`

- **`docs/README.md`** - Main documentation index:
  - Quick start section
  - CLI reference section
  - Troubleshooting section
  - **Action:** Update with new certificate management links

### Established Patterns

- **Documentation follows kebab-case naming** (e.g., `certificate-lifecycle.md`)
- **Markdown format** throughout `docs/` directory
- **Code blocks with bash examples** for command usage
- **Clear section structure** with ## for main sections, ### for subsections
- **Migration guides** follow `migration-vX.Y-to-vX.Z.md` pattern
- **Comprehensive documentation style** with examples, not minimal reference

### Integration Points

- **`docs/README.md`** - Main index needs updating with:
  - Links to new Certificate Trust section
  - Link to migration guide
  - Platform support notice

- **CLI commands to document:**
  - `portless cert install` - Install CA certificate to system trust (Windows-only automatic)
  - `portless cert status` - Display certificate trust status
  - `portless cert uninstall` - Remove CA certificate from trust store
  - `portless proxy start --https` - Start proxy with HTTPS enabled

- **Related existing docs:**
  - `docs/certificate-lifecycle.md` (update)
  - `docs/certificate-security.md` (reference)
  - `docs/migration-v1.0-to-v1.1.md` (pattern)
  - `docs/README.md` (update index)

## Deferred Ideas

None — discussion stayed within Phase 19 scope (certificate management documentation for v1.2).

---

*Phase: 19-documentation*
*Context gathered: 2026-03-02*
