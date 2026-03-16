# Phase 17: Certificate Lifecycle - Context

**Date:** 2026-02-23
**Status:** Planning
**Milestone:** v1.2 HTTPS with Automatic Certificates

## User Decisions from Discussion

### 1. Primary Objective
**Selected:** Ambos: monitoreo automático + comando manual

**Rationale:** Lo mejor de ambos mundos, flexibilidad completa, cumple todos los requisitos LIFECYCLE.

**Features:**
- Background service + comando manual
- Comando `portless cert renew` con --force
- Startup check + background checks (cada 6h)
- Auto-renewal cuando corresponde

### 2. Auto-Renewal Behavior
**Selected:** Renovación automática con opción de desactivar

**Rationale:** Flexibilidad máxima, usuario elige su preferencia, balance entre automatización y control.

**Behavior:**
- Auto-renewal por defecto
- Flag --disable-auto-renew para control manual
- Configurable en config file

### 3. Proxy Integration Level
**Selected:** Híbrido: startup check + background service opcional

**Rationale:** Checks básicos siempre activos, background monitoring opt-in, flexibilidad para el usuario.

**Integration:**
- Startup check siempre en Proxy
- Background service opcional (--enable-monitoring)
- CLI commands independientes

### 4. Additional CLI Functionality
**Selected (Multi-select):**
- Comando `portless cert check` para diagnóstico
- Comando `portless cert renew` con --force
- Integración con `portless proxy start`
- Configuración de umbrales vía variables de entorno

## Requirements to Implement

Based on ROADMAP.md and REQUIREMENTS.md:

### LIFECYCLE Requirements
- [x] **LIFECYCLE-01**: Proxy checks certificate expiration on startup
- [x] **LIFECYCLE-02**: System displays warning when certificate expires within 30 days
- [x] **LIFECYCLE-03**: Background hosted service checks certificate expiration every 6 hours
- [x] **LIFECYCLE-04**: Certificate auto-renews when within 30 days of expiration
- [x] **LIFECYCLE-05**: User can manually renew certificate via `portless cert renew` command
- [x] **LIFECYCLE-06**: Certificate renewal requires proxy restart (hot-reload deferred to v1.3+)
- [x] **LIFECYCLE-07**: Certificate metadata stored in `~/.portless/cert-info.json` (creation timestamp, expiration, fingerprint)

### CLI Requirements
- [x] **CLI-03**: `portless cert renew` command implementation
- [x] **CLI-06**: Renewal notification implementation

## Existing Implementation Context

### Already Implemented (Phase 13)
- `ICertificateService` - Certificate generation (CA + wildcard)
- `ICertificateStorageService` - Three-file storage (ca.pfx, cert.pfx, cert-info.json)
- `ICertificatePermissionService` - Secure file permissions
- `ICertificateManager` - Certificate lifecycle orchestration
- `CertificateInfo` model - Metadata structure with timestamps
- `CertificateStatus` model - Validation and expiration status
- 30-day expiration warning logic in `CreateStatusFromCertificate()`

### Already Implemented (Phase 14)
- `ICertificateTrustService` - Windows trust store integration
- `TrustStatus` enum - Trusted/NotTrusted/ExpiringSoon/Unknown
- `CertInstallCommand`, `CertStatusCommand`, `CertUninstallCommand`

### Code Structure
```
Portless.Core/
├── Services/
│   ├── CertificateService.cs              (CA + wildcard generation)
│   ├── CertificateStorageService.cs       (Three-file storage)
│   ├── CertificatePermissionService.cs    (File permissions)
│   ├── CertificateManager.cs              (Lifecycle orchestration)
│   └── CertificateTrustService.cs         (Trust management)
├── Models/
│   ├── CertificateInfo.cs                 (Metadata for cert-info.json)
│   ├── CertificateStatus.cs               (Validation status)
│   └── CertificateGenerationOptions.cs    (Generation config)
└── Extensions/
    └── ServiceCollectionExtensions.cs     (DI registration)

Portless.Cli/
└── Commands/CertCommand/
    ├── CertInstallCommand.cs              (Windows trust install)
    ├── CertStatusCommand.cs               (Trust status check)
    └── CertUninstallCommand.cs            (Trust uninstall)
```

## Success Criteria (from ROADMAP.md)

1. ✅ Proxy checks certificate expiration on startup and displays warning within 30 days of expiry
2. ✅ Background hosted service checks certificate expiration every 6 hours
3. ✅ Certificate auto-renews when within 30 days of expiration
4. ✅ Certificate metadata stored in `~/.portless/cert-info.json` (creation timestamp, expiration, fingerprint)
5. ✅ User can manually renew certificate via `portless cert renew` command with colored Spectre.Console output
6. ✅ Certificate renewal requires proxy restart (documented limitation, hot-reload deferred to v1.3+)

## Design Decisions

### Architecture Decisions
1. **Background Hosted Service**: Use `IHostedService` or `BackgroundService` for periodic checks
2. **DI Integration**: Register in `ServiceCollectionExtensions.cs` with conditional activation
3. **Configuration**: Support environment variables for thresholds (warning days, check interval)
4. **Startup Integration**: Add check in `Program.cs` of Portless.Proxy
5. **CLI Separation**: New commands in `Portless.Cli/Commands/CertCommand/`

### Technical Decisions
1. **Check Interval**: 6 hours (configurable via `PORTLESS_CERT_CHECK_INTERVAL_HOURS`)
2. **Warning Threshold**: 30 days (configurable via `PORTLESS_CERT_WARNING_DAYS`)
3. **Auto-Renewal**: Enabled by default, opt-out via `PORTLESS_AUTO_RENEW=false`
4. **Force Renewal**: `--force` flag bypasses expiration check
5. **Monitoring Flag**: `--enable-monitoring` flag for background service activation

### Known Limitations
1. **Hot Reload**: Certificate changes require proxy restart (deferred to v1.3+)
2. **Platform**: Background service works cross-platform, but trust install is Windows-only
3. **File Locking**: Certificate files locked during proxy operation, safe to regenerate during startup

## Dependencies

### Phase Dependencies
- **Phase 13** (Certificate Generation): ✅ Complete - provides certificate generation and storage
- **Phase 14** (Trust Installation): ✅ Complete - provides trust status detection

### Code Dependencies
- `ICertificateManager` - For certificate status checks and regeneration
- `CertificateInfo` - For metadata storage
- `CertificateStatus` - For expiration validation
- `IConfiguration` - For environment variable configuration
- `ILogger<T>` - For logging warnings and notifications

## Open Questions

### Resolved via Discussion
1. ✅ Auto-renewal behavior: Automatic with opt-out
2. ✅ Background service integration: Optional opt-in
3. ✅ Startup check behavior: Always active
4. ✅ CLI commands: renew + check + proxy start integration

### To Be Determined During Planning
1. Should background service run as separate process or within proxy process?
2. How to handle certificate renewal when proxy is actively using the certificate?
3. Should `cert check` be separate from `cert status` or merged?
4. Configuration file format for persistent settings (JSON env var?)

## Next Steps

1. Create detailed implementation plan (17-01-PLAN.md)
2. Define background service architecture
3. Define CLI command specifications
4. Define proxy startup integration points
5. Execute implementation

---

**Context captured:** 2026-02-23
**User responses integrated:** ✅
**Ready for planning:** ✅
