# Phase 14: Trust Installation - Context

**Gathered:** 2026-02-22
**Status:** Ready for planning

## Phase Boundary

Windows-based CA certificate trust installation with status verification via CLI commands. Users can install, verify, and uninstall the Portless CA certificate from Windows Certificate Store. macOS/Linux support is documented as a known limitation (deferred to v1.3+).

## Implementation Decisions

### Installation behavior

- **Auto-elevate**: `portless cert install` uses Windows UAC prompt automatically if not running as administrator
- **Store location**: Certificate installs to "Trusted Root CA" store for system-wide trust (requires admin)
- **Idempotent**: If certificate is already trusted, command succeeds silently (no "already installed" message)
- **Confirmation**: Simple message on success: "Certificate installed successfully"
- **Password handling**: Certificate PFX files are protected; installer prompts for password if needed (or uses stored password from Phase 13)

### Status output format

- **Default output**: Minimal binary status - "Trusted" or "Not Trusted"
- **Verbose mode**: `--verbose` flag shows full certificate details:
  - Fingerprint (SHA-256 thumbprint)
  - Expiration date
  - Store location
  - Subject, Issuer, Serial Number, Key size
- **Color-coded status**:
  - 🟢 Green: Trusted
  - 🔴 Red: Not Trusted
  - 🟡 Yellow: Expiring within 30 days
- **Not trusted response**: Shows Windows-specific manual installation instructions (step-by-step)

### Error handling

- **Permission denied**: Generic error message + "Run as Administrator" instruction + error code
- **Missing certificate**: Auto-generate certificate (with user confirmation prompt) before proceeding with command
- **Corrupted certificate**: Auto-regenerate certificate (with user confirmation prompt)
- **Exit codes**: Distinct error codes for different failure scenarios:
  - 0: Success
  - 1: Generic error
  - 2: Insufficient permissions (not admin)
  - 3: Certificate file missing
  - 4: Certificate file corrupted
  - 5: Certificate store access denied
  - 6: Certificate not found in store (for uninstall)

### Cross-platform messaging

- **Platform detection**: Commands detect Windows vs macOS/Linux
- **Non-Windows behavior**: All commands (install, status, uninstall) show warning + inline instructions
  - Warning: "Trust installation is Windows-only in v1.2. Manual setup required for macOS/Linux."
  - Inline instructions: 3-5 lines explaining manual certificate trust steps for platform
- **Warning frequency**: Warning always appears on every command execution (no caching/suppression)
- **Status command**: On macOS/Linux, status shows certificate file information (fingerprint, expiration) but notes trust is manual

### Claude's Discretion

- Exact wording of error messages and instructions
- UAC prompt implementation details (how to trigger elevation)
- Certificate password prompt styling (Spectre.Console secrets handling)
- Color shades for status output (use Spectre.Console colors)
- Exit code range allocation (can add more codes if needed)

## Specific Ideas

- Keep the user experience simple: one command to install, one to check status
- Windows Certificate Store interaction should feel like a native Windows tool
- Error messages should guide users to the solution, not just report problems
- Cross-platform users should know trust installation is limited but not blocked from using Portless

## Deferred Ideas

- macOS/Linux certificate trust installation (deferred to v1.3+)
- Certificate trust verification via browser API (deferred)
- Automatic trust propagation to browsers (deferred)
- GUI for certificate management (deferred)

---

*Phase: 14-trust-installation*
*Context gathered: 2026-02-22*
