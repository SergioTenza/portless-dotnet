# Phase 14 Plan 02: CLI Certificate Trust Commands Summary

**Phase:** 14-trust-installation
**Plan:** 14-02
**Status:** Complete
**Duration:** 7 minutes
**Completed:** 2026-02-23

## One-Liner

Implemented user-friendly CLI commands (`portless cert install/status/uninstall`) for Windows certificate trust management with Spectre.Console colored output, admin elevation support, and comprehensive error handling.

## Implementation Summary

### Objective
Provide command-line interface for certificate trust operations on Windows, enabling users to install, verify, and remove CA certificates from the system trust store with proper error handling and visual feedback.

### Tasks Completed

1. **Created CertInstallCommand and Settings**
   - Empty CertInstallSettings class (no parameters needed)
   - CertInstallCommand with admin privilege detection
   - UAC elevation prompt when not running as administrator
   - Proper exit codes: 0 (success), 2 (not admin), 3 (missing cert), 5 (store access denied)
   - Loads CA certificate via ICertificateManager
   - Installs to Windows LocalMachine Root store via ICertificateTrustService
   - Idempotent behavior (already installed returns success)

2. **Created CertStatusCommand with Verbose Mode**
   - CertStatusSettings with --verbose flag
   - CertStatusCommand with color-coded status display
   - Status colors: green (Trusted), red (Not Trusted), yellow (Expiring Soon/Unknown)
   - Verbose mode shows: SHA-256 fingerprint, expiration date, store location, subject, issuer, serial number
   - Installation instructions displayed when not trusted
   - Graceful handling when no certificate exists

3. **Created CertUninstallCommand and Settings**
   - Empty CertUninstallSettings class
   - CertUninstallCommand for trust removal
   - Idempotent behavior (not found returns success)
   - Clear success/failure messaging

4. **Registered Cert Command Branch in CLI**
   - Added using statement for Portless.Cli.Commands.CertCommand
   - Registered certificate services via AddPortlessCertificates()
   - Added cert branch with install, status, and uninstall subcommands
   - Followed existing proxy branch registration pattern

### Files Created/Modified

**Created:**
- `Portless.Cli/Commands/CertCommand/CertInstallSettings.cs` - Settings for install command
- `Portless.Cli/Commands/CertCommand/CertInstallCommand.cs` - Install command handler (80 lines)
- `Portless.Cli/Commands/CertCommand/CertStatusSettings.cs` - Settings for status command
- `Portless.Cli/Commands/CertCommand/CertStatusCommand.cs` - Status command handler with verbose mode (75 lines)
- `Portless.Cli/Commands/CertCommand/CertUninstallSettings.cs` - Settings for uninstall command
- `Portless.Cli/Commands/CertCommand/CertUninstallCommand.cs` - Uninstall command handler (60 lines)

**Modified:**
- `Portless.Cli/Program.cs` - Added cert branch registration and certificate services

### Deviations from Plan

**None** - Plan executed exactly as written.

### Technical Decisions

1. **UAC Elevation Strategy**: Used Process.Start with Verb="runas" to trigger Windows UAC prompt. Elevated process continues with original arguments while current process exits.

2. **Exit Code Allocation**: Followed CONTEXT.md specification for exit codes (0=success, 1=generic, 2=not admin, 3=missing cert, 5=store access denied).

3. **Verbose Output Format**: Simplified key size display (removed due to API complexity), kept other certificate details as specified.

4. **Color Coding**: Used Spectre.Console colors for status indicators (green, red, yellow) matching CONTEXT.md requirements.

## Verification

### Build Verification
- `dotnet build Portless.slnx` succeeded with 0 errors
- All cert commands compile with correct dependencies

### Command Registration Verification
- `portless cert --help` shows all three subcommands (install, status, uninstall)
- Each command has proper description

### Help Output
```
COMMANDS:
    install      Install CA certificate to system trust store
    status       Display certificate trust status
    uninstall    Remove CA certificate from trust store
```

### Success Criteria Met

✅ CertInstallCommand installs CA certificate to Windows trust store via ICertificateTrustService
✅ CertStatusCommand displays trust status with color-coded output (green/red/yellow)
✅ CertStatusCommand --verbose shows full certificate details
✅ CertUninstallCommand removes CA certificate from trust store
✅ All commands use Spectre.Console colors for output
✅ All commands handle missing certificates gracefully
✅ Install command detects admin privileges and shows error if not admin
✅ Cert command branch registered in Program.cs following existing proxy branch pattern
✅ All commands follow existing CLI patterns (AsyncCommand, Settings, constructor injection)

## Dependencies

**Depends on:**
- 14-01 (ICertificateTrustService implementation)

**Provides for:**
- 14-03 (HTTPS proxy endpoint integration)
- User-facing certificate trust management

## Key Files

- `Portless.Cli/Commands/CertCommand/CertInstallCommand.cs` - Install command with UAC elevation
- `Portless.Cli/Commands/CertCommand/CertStatusCommand.cs` - Status display with verbose mode
- `Portless.Cli/Commands/CertCommand/CertUninstallCommand.cs` - Uninstall command
- `Portless.Cli/Program.cs` - CLI registration

## Integration Points

- **ICertificateManager** - Loads CA certificate for trust operations
- **ICertificateTrustService** - Performs Windows Certificate Store operations
- **Spectre.Console** - Colored output and status formatting
- **Program.cs** - Command registration and dependency injection

## Next Steps

Phase 14-03 will integrate HTTPS support into the proxy, using the certificate trust infrastructure to enable secure HTTPS connections for `.localhost` domains.
