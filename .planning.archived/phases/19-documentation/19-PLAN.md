# Phase 19 Plan: Documentation - HTTPS Certificate Management

**Wave:** 1 of 1
**Depends on:** Phase 14 (Trust Installation), Phase 15 (HTTPS Endpoint), Phase 17 (Certificate Lifecycle)
**Files Modified:** 5 files
**Autonomous:** Yes

## Overview

Complete user-facing documentation for HTTPS certificate management in Portless.NET v1.2. This plan creates comprehensive documentation covering certificate trust management, troubleshooting, migration guidance from v1.1 to v1.2, and platform-specific notes for Windows (supported) vs macOS/Linux (manual installation).

## Requirements

- DOCS-01: User guide for certificate management (install, verify, renew, uninstall)
- DOCS-02: Troubleshooting guide for common certificate issues (untrusted CA, expired cert, SAN mismatch)
- DOCS-03: Migration guide from v1.1 HTTP-only to v1.2 HTTPS
- DOCS-04: Platform-specific notes (Windows Certificate Store, macOS/Linux deferred to v1.3)
- DOCS-05: Security considerations for development certificates (private key protection, trust implications)

## Goal-Backward Verification

### Must Haves

1. **Certificate Trust Documentation**: Users can install, verify, and uninstall CA certificates using documented commands with clear examples
2. **Troubleshooting Coverage**: Users can resolve common certificate issues (untrusted CA, expired certificates, SAN mismatch, file permissions) using FAQ-style troubleshooting
3. **Migration Path**: Users can upgrade from v1.1 HTTP-only to v1.2 HTTPS with clear guidance on what's new and what's required
4. **Platform Clarity**: Users understand platform limitations (Windows automatic vs macOS/Linux manual) with prominent warnings
5. **Security Awareness**: Users understand security implications of development certificates and best practices

### Verification Criteria

- [ ] All certificate commands (install, status, uninstall, check, renew) documented in `certificate-lifecycle.md`
- [ ] Troubleshooting section expanded with 10-15 FAQ-style issues covering 4 categories
- [ ] Migration guide `migration-v1.1-to-v1.2.md` created following v1.0-to-v1.1 structure
- [ ] Platform support warnings prominently displayed in relevant sections
- [ ] `docs/README.md` updated with links to new certificate management documentation
- [ ] macOS/Linux manual installation appendix document created
- [ ] All documentation follows established patterns (kebab-case, markdown, FAQ troubleshooting)

## Tasks

### Task 1: Expand Certificate Lifecycle Documentation

**File:** `/home/sergeix/Work/portless-dotnet/docs/certificate-lifecycle.md`

Add "Certificate Trust" section with install/status/uninstall commands following existing pattern for check/renew commands.

**Subtasks:**
1. Add platform support warning box at top of document after Overview section
2. Create "## Certificate Trust" section after "Certificate Status Commands"
3. Document `portless cert install` command with usage, behavior, exit codes, examples
4. Document `portless cert status` command with colored output examples, exit codes, platform-specific behavior
5. Document `portless cert uninstall` command with usage, behavior, exit codes
6. Add "## Security Considerations" subsection referencing `certificate-security.md`
7. Update "## Related Commands" section to remove commands now documented in main sections

**Commands to document:**

```bash
# Install Certificate Authority
portless cert install
# Exit codes: 0 (success/already installed), 1 (platform not supported), 2 (permissions), 3 (missing), 5 (store access)

# Check Trust Status
portless cert status
# Exit codes: 0 (trusted), 1 (not trusted), 2 (error), 3 (not found)
# Output: Colored Spectre.Console format with fingerprint, expiration, trust state

# Uninstall Certificate Authority
portless cert uninstall
# Exit codes: 0 (success), 1 (platform not supported), 2 (permissions), 3 (not found), 5 (store access)
```

**Platform warning box format:**
```markdown
> **⚠️ Platform Availability**
>
> - **v1.2 (Current):** Windows — Automatic trust installation
> - **macOS/Linux:** Manual installation required (automatic coming in v1.3)
>
> See [Platform-Specific Installation](#platform-specific-installation) for details.
```

**Acceptance Criteria:**
- Certificate Trust section follows same format as Certificate Status Commands
- All exit codes documented with explanations
- Platform-specific behavior clearly documented
- Colored output examples shown for status command
- Security considerations reference `certificate-security.md`

---

### Task 2: Expand Troubleshooting Section

**File:** `/home/sergeix/Work/portless-dotnet/docs/certificate-lifecycle.md`

Expand existing troubleshooting section (5 items) to comprehensive FAQ-style guide with 10-15 issues organized by symptom.

**Subtasks:**
1. Keep existing 5 troubleshooting items (they're good)
2. Add 5-10 new FAQ-style issues covering 4 categories
3. Organize all issues by symptom/error message for discoverability
4. Use consistent format: "### Issue: [Symptom]" with **Symptom:**, **Diagnosis:**, **Cause:**, **Solutions:**, **Prevention:** subsections

**New issues to add (6-10 total):**

**Category: Untrusted CA (2-3 issues)**
1. "Browser shows 'Not Trusted' warning for HTTPS connections"
2. "portless cert status shows '✗ Not Trusted'"
3. "Firefox shows certificate warning but Chrome doesn't" (Firefox NSS database)

**Category: Expired Certificates (2-3 issues)**
4. "Certificate expired and auto-renewal not working"
5. "Proxy started but HTTPS shows certificate expired warning"
6. "Certificate expires soon but no renewal warning displayed"

**Category: SAN Mismatch (1-2 issues)**
7. "hostname.localhost doesn't work but localhost does"
8. "Certificate error: hostname not in certificate SANs"

**Category: File Permissions (1-2 issues)**
9. "Permission denied reading ~/.portless/ca.pfx"
10. "Certificate files have insecure permissions (security warning)"

**Format example:**
```markdown
### Issue: Browser Shows "Not Trusted" Warning

**Symptom:** HTTPS connections to `*.localhost` work but browser displays security warning "Certificate is not trusted"

**Diagnosis:**
```bash
# Check trust status
portless cert status
# Expected output: "✗ Not Trusted"
```

**Cause:** CA certificate not installed to system trust store

**Solutions:**

**Windows (automatic installation):**
```bash
# Run as Administrator
portless cert install
# Verify installation
portless cert status
```

**macOS/Linux (manual installation):**
See [Platform-Specific Installation](#platform-specific-installation) for manual steps.

**Prevention:** Run `portless cert install` after first proxy start to enable trusted HTTPS connections.
```

**Acceptance Criteria:**
- 10-15 total troubleshooting issues (5 existing + 5-10 new)
- Issues organized by symptom/error message
- All 4 categories covered (Untrusted CA, Expired, SAN mismatch, Permissions)
- FAQ format with Symptom → Diagnosis → Cause → Solutions → Prevention
- Platform-specific solutions clearly separated

---

### Task 3: Create Migration Guide v1.1 to v1.2

**File:** `/home/sergeix/Work/portless-dotnet/docs/migration-v1.1-to-v1.2.md`

Create comprehensive migration guide following structure of `migration-v1.0-to-v1.1.md`.

**Subtasks:**
1. Create file with YAML frontmatter (version, updated date)
2. Follow exact structure: Overview → What's New → Breaking Changes → Configuration Changes → CLI Changes → Troubleshooting → Summary
3. Document all v1.2 features: HTTPS endpoints, certificate management commands, automatic renewal, mixed HTTP/HTTPS mode
4. Emphasize zero breaking changes - HTTPS is opt-in
5. Include practical examples for each new feature
6. Add migration difficulty assessment and next steps

**Document structure:**

```markdown
# Migration Guide: v1.1 to v1.2

## Overview
**Release Date:** 2026-03-02
**Milestone:** v1.2 HTTPS with Automatic Certificates
**Compatibility:** Fully backward compatible

## What's New in v1.2
### HTTPS Endpoints and Certificates
- Automatic certificate generation for *.localhost
- HTTPS endpoint via --https flag
- 5-year certificate validity
- Dual HTTP/HTTPS endpoints (1355/1356)

### New Certificate Management Commands
- portless cert install — Install CA to trust store
- portless cert status — Check trust status
- portless cert uninstall — Remove CA from trust store

### Automatic Renewal and Monitoring
- Background certificate expiration checking
- Automatic renewal within 30 days
- Configurable monitoring intervals

### Mixed HTTP/HTTPS Mode
- Both endpoints run simultaneously
- X-Forwarded-Proto header preserved

## Breaking Changes
**None!** v1.2 is fully backward compatible with v1.1.
- All existing commands work unchanged
- Existing HTTP-only setups work unchanged
- HTTPS is opt-in via --https flag
- No code changes required in applications

## Configuration Changes
### No Configuration Required
### Optional: Enable HTTPS
```bash
portless proxy start --https
```

### Optional: Certificate Management
```bash
# Install CA certificate (Windows)
portless cert install

# Check certificate status
portless cert check

# Renew certificate
portless cert renew
```

### Optional: Background Monitoring
```bash
export PORTLESS_ENABLE_MONITORING=true
portless proxy start --https
```

## CLI Changes
### New Commands
### New Options
### Unchanged Commands

## New Features Guide
### Using HTTPS
### Managing Certificates
### Mixed HTTP/HTTPS Mode

## Troubleshooting
### Browser shows certificate warnings
### Certificate expired but proxy started
### macOS/Linux manual installation

## Summary
**Upgrade difficulty:** Easy
**Breaking changes:** None
**Required actions:** None (just upgrade)
**Recommended actions:** Try HTTPS with --https flag, install CA certificate

**Next steps:**
1. Upgrade to v1.2
2. Try HTTPS: portless proxy start --https
3. Install CA certificate: portless cert install (Windows)
4. Read [Certificate Lifecycle Management](certificate-lifecycle.md)
5. Read [Certificate Security Considerations](certificate-security.md)
```

**Acceptance Criteria:**
- Follows migration-v1.0-to-v1.1.md structure exactly
- Zero breaking changes clearly stated
- All v1.2 features documented with examples
- Practical guidance for each new feature
- Clear next steps section

---

### Task 4: Create Platform-Specific Appendix

**File:** `/home/sergeix/Work/portless-dotnet/docs/certificate-troubleshooting-macos-linux.md`

Create appendix document with complete manual installation steps for macOS/Linux.

**Subtasks:**
1. Create appendix file with platform-specific title
2. Document macOS manual installation with security add-trusted-cert command
3. Document Linux manual installation with distribution-specific commands (Ubuntu, Fedora, Arch)
4. Include prominent warnings that automatic installation is not yet supported
5. Add troubleshooting for manual installation (permission errors, keychain issues, cert format errors)
6. Link to official OS documentation for detailed steps

**Document structure:**

```markdown
# Certificate Trust Installation for macOS/Linux

> **⚠️ Manual Installation Required**
>
> Automatic trust installation is **not yet supported** on macOS/Linux in v1.2.
>
> This document provides manual installation steps. Automatic installation is planned for v1.3.
>
> **Windows users:** See [Certificate Lifecycle Management](certificate-lifecycle.md) for automatic installation.

## Overview

On macOS and Linux, you must manually install the Portless.NET CA certificate to your system's trust store to enable trusted HTTPS connections for `*.localhost` domains.

## Prerequisites

- Portless.NET proxy must have generated certificates: `~/.portless/ca.pfx`
- Administrator/root privileges for system-wide installation
- Command-line access

## macOS Installation

### Install CA Certificate to System Keychain

```bash
# Install CA certificate to system keychain (requires sudo)
sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain ~/.portless/ca.pfx

# Verify installation
security find-certificate -c "Portless.NET Development CA" -p /Library/Keychains/System.keychain
```

**What this does:**
- `-d`: Adds certificate to admin trust store
- `-r trustRoot`: Marks as trusted root CA
- `-k`: Targets system keychain (all users)

### Install to User Keychain (Alternative)

```bash
# Install to current user's keychain (no sudo required)
security add-trusted-cert -d -r trustRoot -k ~/Library/Keychains/login.keychain-db ~/.portless/ca.pfx
```

**Note:** User keychain installation only affects current user.

### Verify Installation

```bash
# Check if certificate is trusted
security find-certificate -c "Portless.NET Development CA" -p | grep -q "Portless.NET Development CA" && echo "✓ Trusted" || echo "✗ Not Trusted"

# Check certificate details
security find-certificate -c "Portless.NET Development CA" -p /Library/Keychains/System.keychain | openssl x509 -text
```

## Linux Installation

### Ubuntu/Debian

```bash
# Copy CA certificate to certificates directory
sudo cp ~/.portless/ca.pfx /usr/local/share/ca-certificates/portless-ca.crt

# Update certificates store
sudo update-ca-certificates

# Verify installation
ls /etc/ssl/certs/ | grep portless
```

### Fedora/RHEL/CentOS

```bash
# Copy CA certificate
sudo cp ~/.portless/ca.pfx /etc/pki/ca-trust/source/anchors/portless-ca.crt

# Update trust store
sudo update-ca-trust

# Verify installation
ls /etc/pki/ca-trust/source/anchors/ | grep portless
```

### Arch Linux

```bash
# Copy CA certificate to trust store
sudo cp ~/.portless/ca.pfx /etc/ca-certificates/trust-source/anchors/portless-ca.crt

# Update trust store
sudo update-ca-trust-extract

# Verify installation
ls /etc/ca-certificates/trust-source/anchors/ | grep portless
```

### Verify Installation (Linux)

```bash
# Check if certificate is in store
ls -l /etc/ssl/certs/ | grep portless  # Ubuntu/Debian
# or
trust list | grep Portless  # Fedora
# or
ls -l /etc/ca-certificates/trust-source/anchors/ | grep portless  # Arch
```

## Troubleshooting

### Permission Denied

**Problem:** `sudo: command not found` or `Permission denied`

**Solution:** Ensure you have sudo/root privileges:
```bash
# Request sudo access
sudo security add-trusted-cert ...  # macOS
sudo cp ~/.portless/ca.pfx ...  # Linux
```

### Certificate Format Error

**Problem:** "Unable to load certificate" or "certificate format error"

**Cause:** The `.pfx` file format may not be directly compatible

**Solution:** Convert to PEM format (macOS only):
```bash
# Extract certificate from PFX
openssl pkcs12 -in ~/.portless/ca.pfx -clcerts -nokeys -out /tmp/portless-ca.pem
openssl pkcs12 -in ~/.portless/ca.pfx -cacerts -nokeys -out /tmp/portless-ca-root.pem

# Install extracted certificate
sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain /tmp/portless-ca-root.pem
```

### Certificate Not Trusted After Installation

**Problem:** Browser still shows "Not Trusted" warning after installation

**Solutions:**

**macOS:** Ensure certificate is in System keychain, not just user keychain
```bash
security find-certificate -c "Portless.NET Development CA" -p /Library/Keychains/System.keychain
```

**Linux:** Restart browser after installation
```bash
# Chrome/Chromium: Close all windows and reopen
# Firefox: See Firefox-specific section below
```

### Firefox-Specific Issues

**Problem:** Firefox uses its own NSS certificate database and doesn't read system trust store

**Solution:** Install certificate to Firefox database:

```bash
# Find Firefox certificate databases
find ~/.mozilla/firefox -name "cert9.db"

# Install to Firefox (first profile only)
certutil -A -n "Portless.NET Development CA" -t "C,," -i ~/.portless/ca.pfx -d ~/.mozilla/firefox/xxxxx.default-release
```

**Note:** Automatic Firefox installation planned for v1.3

## Uninstalling CA Certificate

### macOS

```bash
# Remove from System keychain
sudo security delete-certificate -c "Portless.NET Development CA" /Library/Keychains/System.keychain

# Remove from User keychain
security delete-certificate -c "Portless.NET Development CA" ~/Library/Keychains/login.keychain-db
```

### Linux

```bash
# Ubuntu/Debian
sudo rm /usr/local/share/ca-certificates/portless-ca.crt
sudo update-ca-certificates

# Fedora/RHEL
sudo rm /etc/pki/ca-trust/source/anchors/portless-ca.crt
sudo update-ca-trust

# Arch
sudo rm /etc/ca-certificates/trust-source/anchors/portless-ca.crt
sudo update-ca-trust-extract
```

## Related Documentation

- [Certificate Lifecycle Management](certificate-lifecycle.md) - Certificate commands and monitoring
- [Migration Guide v1.1 to v1.2](migration-v1.1-to-v1.2.md) - Upgrading to HTTPS
- [Certificate Security Considerations](certificate-security.md) - Security best practices

---

**Platform:** macOS/Linux (manual installation)
**Version:** v1.2
**Automatic Installation:** Planned for v1.3
```

**Acceptance Criteria:**
- Platform-specific manual steps documented for macOS and 3 Linux distributions
- Prominent warnings about manual installation requirement
- Troubleshooting section covers common manual installation issues
- Firefox NSS database section included
- Uninstallation steps documented
- Links to related documentation

---

### Task 5: Update Documentation Index

**File:** `/home/sergeix/Work/portless-dotnet/docs/README.md`

Update main documentation index with links to new certificate management documentation.

**Subtasks:**
1. Add "Certificate Management" section after "Troubleshooting" section
2. Add links to Certificate Trust, Troubleshooting, Migration Guide
3. Add platform support notice in Certificate Management section
4. Update CLI Reference section to include certificate commands
5. Add "What's New in v1.2" callout at top of document

**Updates to make:**

```markdown
## What's New in v1.2

### HTTPS Support with Automatic Certificates

Portless.NET v1.2 adds HTTPS support with automatic certificate generation:

- **Automatic certificates** for `*.localhost` domains (5-year validity)
- **Dual endpoints**: HTTP (1355) and HTTPS (1356)
- **Certificate management**: install, status, uninstall commands
- **Automatic renewal** within 30 days of expiration
- **Zero configuration** - certificates generated on first HTTPS start

**Quick start:**
```bash
# Start proxy with HTTPS
portless proxy start --https

# Install CA certificate (Windows - automatic)
portless cert install

# Verify trust status
portless cert status
```

See [Certificate Management](#certificate-management) for complete documentation.

---

## Certificate Management

Portless.NET provides automatic HTTPS certificate generation and lifecycle management for local development.

### Quick Start

```bash
# Enable HTTPS (automatic certificate generation)
portless proxy start --https

# Install CA certificate to trust store (Windows)
portless cert install

# Check certificate status
portless cert check

# Verify trust status
portless cert status
```

### Documentation

- [Certificate Lifecycle Management](certificate-lifecycle.md)
  - Certificate status commands (check, renew)
  - **Certificate trust commands (install, status, uninstall)**
  - Automatic monitoring configuration
  - Environment variables
  - **Comprehensive troubleshooting guide**

- [Migration Guide v1.1 to v1.2](migration-v1.1-to-v1.2.md)
  - What's new in v1.2
  - Breaking changes (none!)
  - Upgrading from HTTP-only to HTTPS
  - New certificate management features

- [Certificate Security Considerations](certificate-security.md)
  - Private key protection
  - Trust implications
  - Development vs production certificates
  - Security best practices

- [Platform-Specific Installation](certificate-troubleshooting-macos-linux.md)
  - macOS manual installation steps
  - Linux manual installation steps (Ubuntu, Fedora, Arch)
  - Firefox NSS database configuration

### Platform Support

> **⚠️ Platform Availability**
>
> - **v1.2 (Current):** Windows — Automatic trust installation
> - **macOS/Linux:** Manual installation required (automatic coming in v1.3)
>
> See [Platform-Specific Installation](certificate-troubleshooting-macos-linux.md) for manual steps.

### Certificate Commands

- `portless cert install` - Install CA certificate to system trust
- `portless cert status` - Display certificate trust status
- `portless cert check` - Check certificate expiration status
- `portless cert renew` - Renew certificate (automatic or manual)
- `portless cert uninstall` - Remove CA certificate from trust store

---

## CLI Reference

Common commands:
- `portless proxy start` - Start the proxy
- `portless proxy start --https` - Start proxy with HTTPS enabled **[NEW in v1.2]**
- `portless proxy stop` - Stop the proxy
- `portless proxy status` - Check proxy status
- `portless list` - List active routes
- `portless <hostname> <command>` - Run app with URL

**Certificate commands:**
- `portless cert install` - Install CA certificate to system trust **[NEW in v1.2]**
- `portless cert status` - Display certificate trust status **[NEW in v1.2]**
- `portless cert check` - Check certificate expiration status **[NEW in v1.2]**
- `portless cert renew` - Renew certificate **[NEW in v1.2]**
- `portless cert uninstall` - Remove CA certificate from trust store **[NEW in v1.2]**

See `portless --help` for full command reference.
```

**Acceptance Criteria:**
- "What's New in v1.2" callout added at top of document
- "Certificate Management" section added with all relevant links
- Platform support warning included
- CLI Reference updated with certificate commands marked as [NEW in v1.2]
- All links point to correct documentation files

---

## Files Modified

1. `/home/sergeix/Work/portless-dotnet/docs/certificate-lifecycle.md` - Add Certificate Trust section, expand troubleshooting
2. `/home/sergeix/Work/portless-dotnet/docs/migration-v1.1-to-v1.2.md` - Create new migration guide
3. `/home/sergeix/Work/portless-dotnet/docs/certificate-troubleshooting-macos-linux.md` - Create new platform appendix
4. `/home/sergeix/Work/portless-dotnet/docs/README.md` - Update index with new documentation links
5. `/home/sergeix/Work/portless-dotnet/docs/certificate-security.md` - Reference from trust sections (no changes to file)

## Success Criteria

Phase 19 is complete when:

1. **Certificate Trust Documentation**: All certificate commands (install, status, uninstall) documented in `certificate-lifecycle.md` with usage, behavior, exit codes, examples
2. **Comprehensive Troubleshooting**: FAQ-style troubleshooting with 10-15 issues covering untrusted CA, expired certificates, SAN mismatch, file permissions
3. **Migration Guide**: `migration-v1.1-to-v1.2.md` created following v1.0-to-v1.1 structure with zero breaking changes emphasis
4. **Platform Documentation**: macOS/Linux manual installation appendix created with complete steps and troubleshooting
5. **Documentation Index**: `docs/README.md` updated with Certificate Management section and v1.2 feature highlights
6. **Pattern Consistency**: All documentation follows established patterns (kebab-case, markdown, FAQ troubleshooting, structured migration guides)
7. **Platform Warnings**: Platform limitations prominently displayed in all relevant sections

## Notes

- Existing `certificate-lifecycle.md` (334 lines) will be expanded by ~150-200 lines
- New `migration-v1.1-to-v1.2.md` will be ~300-350 lines following v1.0-to-v1.1 pattern
- New `certificate-troubleshooting-macos-linux.md` will be ~250-300 lines
- `docs/README.md` will be updated with ~100 lines of new content
- All documentation uses standard Markdown with GitHub Flavored Markdown (GFM)
- No build step required - plain markdown files
- Link validation should be manual before commit

## Execution Order

Tasks can be executed in any order, but recommended sequence for logical flow:

1. Task 3 (Migration Guide) - Creates foundational v1.2 overview document
2. Task 1 (Certificate Lifecycle) - Expands core documentation
3. Task 2 (Troubleshooting) - Builds on Task 1
4. Task 4 (Platform Appendix) - Standalone document
5. Task 5 (Documentation Index) - Final integration

All tasks are independent and can be executed in parallel if desired.

---

*Phase 19 Plan: Documentation*
*Wave: 1 of 1*
*Created: 2026-03-02*
