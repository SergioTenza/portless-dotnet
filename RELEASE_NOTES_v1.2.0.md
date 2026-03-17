# Portless.NET v1.2.0 Release Notes

🎉 **Major Release: HTTPS with Automatic Certificates**

Portless.NET v1.2 brings automatic HTTPS certificate generation and management for secure local development. No more certificate warnings when developing with HTTPS!

## 🚀 What's New

### 🔒 Automatic Certificate Management

- **Zero-configuration HTTPS**: Automatic certificate generation for `*.localhost` domains
- **Local Certificate Authority**: CA created automatically for certificate signing
- **5-year validity**: Long-lasting certificates for development
- **Background monitoring**: Optional service checks certificate expiration every 6 hours
- **Auto-renewal**: Certificates automatically renewed within 30 days of expiration
- **Secure storage**: Platform-specific file permissions for certificate protection

### 🌐 HTTPS Proxy Support

- **Dual HTTP/HTTPS endpoints**: HTTP on port 1355, HTTPS on port 1356
- **Automatic certificate binding**: Certificates bound to HTTPS endpoint automatically
- **TLS 1.2+ enforcement**: Minimum TLS version enforced for security
- **HTTP→HTTPS redirect**: Automatic redirect (308) with API endpoint exclusion
- **Wildcard certificate support**: Single certificate for all `*.localhost` domains

### 🎛️ New CLI Commands

```bash
# Enable HTTPS proxy
portless proxy start --https

# Certificate management (NEW)
portless cert install      # Install CA certificate (Windows)
portless cert status       # Check trust status
portless cert check        # Check expiration
portless cert renew        # Renew certificate
portless cert uninstall    # Remove CA certificate (Windows)
```

### 🔀 Mixed Protocol Routing

- **X-Forwarded-Proto header preservation**: Original protocol preserved for backends
- **Simultaneous HTTP/HTTPS backends**: Support for mixed protocol routing
- **YARP SSL validation**: Self-signed certificate acceptance in development mode

### 📚 Documentation

- **Certificate Lifecycle Guide** (907 lines): Complete certificate management
- **Migration Guide** (387 lines): v1.1 to v1.2 upgrade instructions
- **Platform-Specific Troubleshooting** (345 lines): macOS/Linux manual installation
- **Security Considerations** (365 lines): Private key protection and trust implications
- **13 FAQ Items**: Common certificate issues and solutions

## 📊 Release Statistics

- **33/33 requirements verified** (100%)
- **5/5 phases complete** (100%)
- **25 integration tests** created and passing
- **1,845+ lines of documentation** added
- **~3,500 lines of code** added across services, CLI, and tests

## 🔧 Configuration

### New Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `PORTLESS_HTTPS_ENABLED` | Enable HTTPS endpoint | `false` |
| `PORTLESS_CERT_WARNING_DAYS` | Days before expiration warning | `30` |
| `PORTLESS_CERT_CHECK_INTERVAL_HOURS` | Hours between certificate checks | `6` |
| `PORTLESS_AUTO_RENEW` | Auto-renew expiring certificates | `true` |
| `PORTLESS_ENABLE_MONITORING` | Enable background monitoring | `false` |

## 🌍 Platform Support

### Windows 10+ (✅ Full Support)
- Automated trust installation with one command
- Windows Certificate Store integration
- Administrator privileges required

### macOS 12+ (⚠️ Manual Setup)
- Manual trust installation required
- Comprehensive documentation provided
- See [macOS/Linux Installation Guide](docs/certificate-troubleshooting-macos-linux.md)

### Linux (⚠️ Manual Setup)
- Manual trust installation required
- Distribution-specific instructions (Ubuntu/Debian, Fedora/RHEL, Arch)
- See [macOS/Linux Installation Guide](docs/certificate-troubleshooting-macos-linux.md)

## ⚠️ Breaking Changes

**None** - All v1.1 features remain fully functional

### Deprecated
- `PORTLESS_PORT` environment variable - Fixed ports enforced (HTTP=1355, HTTPS=1356)
- Deprecation warning implemented when `PORTLESS_PORT` is set

## 🚦 Quick Start

### Windows (Automated Setup)

```bash
# 1. Install the tool
dotnet tool install -g portless.dotnet --version 1.2.0

# 2. Install CA certificate (run as Administrator)
portless cert install

# 3. Start proxy with HTTPS
portless proxy start --https

# 4. Run your app
portless run myapp dotnet run

# 5. Access via HTTPS
# https://myapp.localhost:1356
```

### macOS/Linux (Manual Setup)

```bash
# 1. Install the tool
dotnet tool install -g portless.dotnet --version 1.2.0

# 2. Start proxy with HTTPS
portless proxy start --https

# 3. Manually trust CA certificate (one-time setup)
# See: https://github.com/SergioTenza/portless-dotnet/blob/main/docs/certificate-troubleshooting-macos-linux.md

# 4. Run your app
portless run myapp dotnet run

# 5. Access via HTTPS
# https://myapp.localhost:1356
```

## 🔒 Security

- ✅ Secure PFX certificate storage with platform-specific permissions
- ✅ Windows Certificate Store integration requires administrator privileges
- ✅ TLS 1.2+ minimum protocol enforcement
- ✅ Private key protection documentation
- ✅ Development vs production certificate guidance

**⚠️ Important:** These certificates are for local development only. NOT suitable for production use.

## 📖 Documentation

- [Certificate Lifecycle Management](docs/certificate-lifecycle.md)
- [Certificate Security Considerations](docs/certificate-security.md)
- [Migration Guide: v1.1 to v1.2](docs/migration-v1.1-to-v1.2.md)
- [Platform-Specific Troubleshooting](docs/certificate-troubleshooting-macos-linux.md)
- [Complete CHANGELOG](CHANGELOG.md)

## 🙏 Acknowledgments

This milestone completes the HTTPS vision for Portless.NET, bringing secure local development with automatic certificate management inspired by the excellent [Portless](https://github.com/portless/portless) project.

## 🔮 What's Next

### v1.3 (Planned)
- macOS/Linux automated trust installation
- HTTP/3 (QUIC) support
- Configurable certificate validity periods
- Multiple CA certificate support

### v2.0 (Planned)
- Production deployment features
- Authentication and authorization
- Advanced monitoring dashboards
- Cluster support
- High availability mode

---

**Download:** [NuGet.org](https://www.nuget.org/packages/portless.dotnet/1.2.0)
**GitHub:** [SergioTenza/portless-dotnet](https://github.com/SergioTenza/portless-dotnet)
**Documentation:** [Full Documentation](docs/)

---

*Portless.NET v1.2.0 - HTTPS with Automatic Certificates*
*Released: 2026-03-17*
*License: MIT*