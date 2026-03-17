# Portless.NET v1.3.0 Release Notes

## 🎉 Platform Parity Achieved

Portless.NET v1.3 brings complete platform parity for certificate trust automation across Windows, macOS, and Linux.

## What's New

### Automated Certificate Trust Installation
- **macOS 12+**: One-command installation to System Keychain
- **Linux**: Distribution-specific installation (Ubuntu, Debian, Fedora, RHEL, Arch)
- **Windows 10+**: Existing automation unchanged

### Key Features
- Platform-specific certificate trust services
- Distribution-specific certificate store paths
- System Keychain integration (macOS)
- Root privilege detection and sudo prompts
- Graceful degradation with manual instructions

## Breaking Changes
**None** - Zero breaking changes from v1.2

## Platform Support
- Windows 10+ ✅
- macOS 12+ ✅
- Ubuntu 20.04+ ✅
- Debian 11+ ✅
- Fedora 38+ ✅
- RHEL 9+ ✅
- Arch Linux ✅

## Documentation
- README.md updated with platform parity
- Troubleshooting guide simplified (345 → 50 lines)
- VERIFICATION.md files for all phases

## Migration from v1.2
No migration needed - fully backward compatible.
