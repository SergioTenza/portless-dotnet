# Certificate Trust Installation Troubleshooting

> **Note:** As of v1.3, certificate trust installation is automated on macOS and Linux. This guide covers edge cases only.

## Edge Cases

### macOS: Keychain Locked

If System Keychain is locked:
```bash
sudo security unlock-keychain /Library/Keychains/System.keychain
```

### macOS: System Integrity Protection (SIP)

If SIP blocks installation:
```bash
# Manual installation as fallback
sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain ~/.portless/ca.pfx
```

### Linux: Unsupported Distribution

If your distribution is not supported:
```bash
# Manual installation requires identifying your distro's certificate store
# See distribution documentation for certificate installation instructions
```

### Linux: Command Failed

If certificate update command fails:
```bash
# For Ubuntu/Debian
sudo update-ca-certificates --fresh

# For Fedora/RHEL
sudo update-ca-trust

# For Arch
sudo trust anchor --refresh
```
