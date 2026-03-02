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

**Generate certificates first:**
```bash
portless proxy start --https
```

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
# Convert PFX to PEM format (if needed)
openssl pkcs12 -in ~/.portless/ca.pfx -clcerts -nokeys -out /tmp/portless-ca.pem
openssl pkcs12 -in ~/.portless/ca.pfx -cacerts -nokeys -out /tmp/portless-ca-root.pem

# Copy CA certificate to certificates directory
sudo cp /tmp/portless-ca-root.pem /usr/local/share/ca-certificates/portless-ca.crt

# Update certificates store
sudo update-ca-certificates

# Verify installation
ls /etc/ssl/certs/ | grep portless
```

### Fedora/RHEL/CentOS

```bash
# Convert PFX to PEM format (if needed)
openssl pkcs12 -in ~/.portless/ca.pfx -clcerts -nokeys -out /tmp/portless-ca.pem
openssl pkcs12 -in ~/.portless/ca.pfx -cacerts -nokeys -out /tmp/portless-ca-root.pem

# Copy CA certificate
sudo cp /tmp/portless-ca-root.pem /etc/pki/ca-trust/source/anchors/portless-ca.crt

# Update trust store
sudo update-ca-trust

# Verify installation
ls /etc/pki/ca-trust/source/anchors/ | grep portless
```

### Arch Linux

```bash
# Convert PFX to PEM format (if needed)
openssl pkcs12 -in ~/.portless/ca.pfx -clcerts -nokeys -out /tmp/portless-ca.pem
openssl pkcs12 -in ~/.portless/ca.pfx -cacerts -nokeys -out /tmp/portless-ca-root.pem

# Copy CA certificate to trust store
sudo cp /tmp/portless-ca-root.pem /etc/ca-certificates/trust-source/anchors/portless-ca.crt

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

**Alternative (macOS):** Install to user keychain instead:
```bash
security add-trusted-cert -d -r trustRoot -k ~/Library/Keychains/login.keychain-db ~/.portless/ca.pfx
```

### Certificate Format Error

**Problem:** "Unable to load certificate" or "certificate format error"

**Cause:** The `.pfx` file format may not be directly compatible

**Solution:** Convert to PEM format (macOS only):
```bash
# Extract certificate from PFX
openssl pkcs12 -in ~/.portless/ca.pfx -clcerts -nokeys -out /tmp/portless-ca.pem
openssl pkcs12 -in ~/.portless/ca.pfx -cacerts -nokeys -out /tmp/portless-ca-root.pem

# Install extracted certificate (macOS)
sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain /tmp/portless-ca-root.pem
```

**For Linux:** Use the PEM conversion steps shown in each distribution's installation section above.

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

**Note:** Automatic Firefox installation planned for v1.3.

**Alternative:** Use Chrome/Edge for development (they use system trust store).

### certutil Command Not Found

**Problem:** `certutil: command not found` (Firefox installation)

**Solution:** Install certutil:

**Ubuntu/Debian:**
```bash
sudo apt-get install libnss3-tools
```

**Fedora/RHEL:**
```bash
sudo dnf install nss-tools
```

**Arch:**
```bash
sudo pacman -S nss
```

## Uninstalling CA Certificate

### macOS

```bash
# Remove from System keychain
sudo security delete-certificate -c "Portless.NET Development CA" /Library/Keychains/System.keychain

# Remove from User keychain
security delete-certificate -c "Portless.NET Development CA" ~/Library/Keychains/login.keychain-db
```

### Linux

**Ubuntu/Debian:**
```bash
sudo rm /usr/local/share/ca-certificates/portless-ca.crt
sudo update-ca-certificates
```

**Fedora/RHEL:**
```bash
sudo rm /etc/pki/ca-trust/source/anchors/portless-ca.crt
sudo update-ca-trust
```

**Arch:**
```bash
sudo rm /etc/ca-certificates/trust-source/anchors/portless-ca.crt
sudo update-ca-trust-extract
```

**Firefox:**
```bash
# Remove from Firefox database
certutil -D -n "Portless.NET Development CA" -d ~/.mozilla/firefox/xxxxx.default-release
```

## Quick Reference

### macOS Commands

```bash
# Install (System)
sudo security add-trusted-cert -d -r trustRoot -k /Library/Keychains/System.keychain ~/.portless/ca.pfx

# Install (User)
security add-trusted-cert -d -r trustRoot -k ~/Library/Keychains/login.keychain-db ~/.portless/ca.pfx

# Verify
security find-certificate -c "Portless.NET Development CA" -p /Library/Keychains/System.keychain

# Uninstall (System)
sudo security delete-certificate -c "Portless.NET Development CA" /Library/Keychains/System.keychain
```

### Linux Commands

```bash
# Ubuntu/Debian
sudo cp /tmp/portless-ca-root.pem /usr/local/share/ca-certificates/portless-ca.crt
sudo update-ca-certificates

# Fedora/RHEL
sudo cp /tmp/portless-ca-root.pem /etc/pki/ca-trust/source/anchors/portless-ca.crt
sudo update-ca-trust

# Arch
sudo cp /tmp/portless-ca-root.pem /etc/ca-certificates/trust-source/anchors/portless-ca.crt
sudo update-ca-trust-extract
```

## Platform-Specific Notes

### macOS

- **System keychain** requires sudo but affects all users
- **User keychain** doesn't require sudo but only affects current user
- **Keychain Access** app can also be used for GUI installation
- Certificate must be marked as "Always Trust" for system-wide trust

### Linux

- **Distribution-specific** commands for certificate installation
- **Browser cache** may need to be cleared after installation
- **Firefox** requires separate installation (NSS database)
- **Chromium-based browsers** use system trust store

## Security Considerations

When manually installing certificates:

**Verify certificate source:**
```bash
# Check certificate details before installing
openssl pkcs12 -in ~/.portless/ca.pfx -info -nokeys
```

**Install only to appropriate trust store:**
- Use system trust store for development (affects all users)
- Use user trust store for single-user development
- Never install to production servers

**Remove when no longer needed:**
```bash
# Uninstall after completing development
# See uninstallation commands above
```

For detailed security information, see [Certificate Security Considerations](certificate-security.md).

## Related Documentation

- [Certificate Lifecycle Management](certificate-lifecycle.md) - Certificate commands and monitoring
- [Migration Guide v1.1 to v1.2](migration-v1.1-to-v1.2.md) - Upgrading to HTTPS
- [Certificate Security Considerations](certificate-security.md) - Security best practices

## Need Help?

- [Certificate Lifecycle Troubleshooting](certificate-lifecycle.md#troubleshooting)
- [Main README](../README.md)
- [Report an issue](https://github.com/yourusername/portless-dotnet/issues)

---

**Platform:** macOS/Linux (manual installation)
**Version:** v1.2
**Automatic Installation:** Planned for v1.3
