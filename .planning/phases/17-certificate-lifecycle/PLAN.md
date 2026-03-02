# Phase 17: Certificate Lifecycle - Implementation Plan

**Status:** Ready for Execution
**Estimated Duration:** ~60-90 minutes
**Plans:** 4 sub-plans (17-01 through 17-04)

## Phase Overview

**Goal:** Implement automatic certificate expiration monitoring and renewal with both background service and manual CLI commands.

**User Requirements:**
- Ambos: monitoreo automático + comando manual
- Renovación automática con opción de desactivar
- Integración híbrida: startup check + background service opcional
- CLI adicional: cert check, cert renew --force, proxy start integration, env vars

**Success Criteria:**
1. ✅ Startup check displays warning within 30 days of expiry
2. ✅ Background hosted service checks every 6 hours (opt-in)
3. ✅ Auto-renewal when within 30 days (opt-out via env var)
4. ✅ Manual `portless cert renew` command with --force flag
5. ✅ `portless cert check` diagnostic command
6. ✅ Proxy start integration with warnings
7. ✅ Environment variables for thresholds

## Sub-Plans

### 17-01: Background Certificate Monitoring Service

**Goal:** Implement `IHostedService` for periodic certificate expiration checks with auto-renewal.

**Requirements:** LIFECYCLE-01, LIFECYCLE-02, LIFECYCLE-03, LIFECYCLE-04, LIFECYCLE-07

**Deliverables:**
- `ICertificateMonitoringService` interface
- `CertificateMonitoringService` background service
- Configuration via `IConfiguration` (check interval, warning days, auto-renew)
- Logging for warnings and renewal actions
- DI registration in `AddPortlessCertificateMonitoring`

**Key Design Decisions:**
- Check interval: 6 hours (default, configurable)
- Warning threshold: 30 days (default, configurable)
- Auto-renewal: true (default, opt-out)
- Service only starts if `--enable-monitoring` flag set or `PORTLESS_ENABLE_MONITORING=true`

**Files to Create:**
- `Portless.Core/Services/ICertificateMonitoringService.cs`
- `Portless.Core/Services/CertificateMonitoringService.cs`
- `Portless.Core/Models/CertificateMonitoringOptions.cs`

**Files to Modify:**
- `Portless.Core/Extensions/ServiceCollectionExtensions.cs` (Add monitoring registration)

---

### 17-02: CLI Certificate Renewal and Check Commands

**Goal:** Implement `portless cert renew` and `portless cert check` commands.

**Requirements:** CLI-03, CLI-06, LIFECYCLE-05

**Deliverables:**
- `CertRenewCommand` with --force flag
- `CertCheckCommand` for diagnostic output
- `CertRenewSettings` and `CertCheckSettings`
- Colored Spectre.Console output
- Integration with `ICertificateManager`

**Key Design Decisions:**
- `cert renew` renews if expires within warning days (default 30)
- `cert renew --force` renews immediately regardless of expiration
- `cert check` shows: status badge, days remaining, thumbprint, file status
- Exit codes: 0 (success), 1 (error), 2 (expired), 3 (not found)

**Files to Create:**
- `Portless.Cli/Commands/CertCommand/CertRenewCommand.cs`
- `Portless.Cli/Commands/CertCommand/CertRenewSettings.cs`
- `Portless.Cli/Commands/CertCommand/CertCheckCommand.cs`
- `Portless.Cli/Commands/CertCommand/CertCheckSettings.cs`

**Files to Modify:**
- `Portless.Cli/Program.cs` (register new commands)

---

### 17-03: Proxy Startup Certificate Check Integration

**Goal:** Integrate certificate expiration check into proxy startup with warning display.

**Requirements:** LIFECYCLE-01, LIFECYCLE-02, CLI-06

**Deliverables:**
- Startup check in `Portless.Proxy/Program.cs`
- Warning display via Spectre.Console
- Non-blocking (proxy starts even if cert expiring)
- Suggestion to run `portless cert renew`

**Key Design Decisions:**
- Check runs before Kestrel starts
- Yellow warning for expiring soon (≤ 30 days)
- Red error for expired cert
- Info message for valid cert
- Does not block proxy startup (logs only)

**Files to Modify:**
- `Portless.Proxy/Program.cs`

**Integration Points:**
- After DI services built, before `app.Run()`
- Use `ICertificateManager.GetCertificateStatusAsync()`
- Use `Spectre.Console.AnsiConsole.MarkupLine()` for colored output

---

### 17-04: Environment Variable Configuration and Documentation

**Goal:** Add environment variable support for thresholds and document certificate lifecycle.

**Requirements:** LIFECYCLE-06, DOCS-01, DOCS-05

**Deliverables:**
- Environment variable configuration
- Certificate lifecycle documentation
- Restart requirement documentation
- Security considerations

**Key Design Decisions:**
- `PORTLESS_CERT_WARNING_DAYS=30` (default)
- `PORTLESS_CERT_CHECK_INTERVAL_HOURS=6` (default)
- `PORTLESS_AUTO_RENEW=true` (default, false to disable)
- `PORTLESS_ENABLE_MONITORING=false` (default, true to enable)

**Files to Create:**
- `docs/certificate-lifecycle.md` (user guide)
- `docs/certificate-security.md` (security considerations)

**Files to Modify:**
- `README.md` (add environment variables section)
- `CLAUDE.md` (add certificate lifecycle notes)

---

## Implementation Order

### Sequence (Must execute in order):
1. **17-01**: Background monitoring service (foundation)
2. **17-02**: CLI commands (depends on monitoring options)
3. **17-03**: Proxy startup integration (depends on manager)
4. **17-04**: Documentation (depends on all implementation)

### Parallel Opportunities:
- None (each sub-plan depends on previous)

---

## Testing Strategy

### Manual Testing
- Start proxy with expiring certificate (verify warning)
- Start proxy with valid certificate (verify info)
- Run `portless cert check` (verify diagnostic output)
- Run `portless cert renew` on valid cert (verify no-op)
- Run `portless cert renew --force` (verify forced renewal)
- Run `portless cert renew` on expiring cert (verify auto-renewal)
- Enable monitoring with `--enable-monitoring` (verify background checks)
- Test environment variables override defaults

### Automated Testing (deferred to Phase 18)
- Unit tests for monitoring service
- Integration tests for CLI commands
- End-to-end tests for certificate renewal

---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                     CLI Commands                             │
│  ┌──────────────────┐  ┌──────────────────┐                │
│  │ portless cert    │  │ portless cert    │                │
│  │ check            │  │ renew [--force]  │                │
│  └────────┬─────────┘  └────────┬─────────┘                │
└───────────┼─────────────────────┼───────────────────────────┘
            │                     │
            └──────────┬──────────┘
                       ▼
┌─────────────────────────────────────────────────────────────┐
│              Certificate Manager (Core)                      │
│  ┌─────────────────────────────────────────────────────┐   │
│  │ ICertificateManager                                   │   │
│  │  - GetCertificateStatusAsync()                        │   │
│  │  - EnsureCertificatesAsync(forceRegeneration)        │   │
│  └─────────────────────────────────────────────────────┘   │
└────────────────────────┬────────────────────────────────────┘
                       │
        ┌──────────────┴──────────────┐
        ▼                             ▼
┌──────────────────┐      ┌──────────────────────┐
│ Background       │      │ Proxy Startup        │
│ Monitoring       │      │ Integration          │
│ Service          │      │ (Portless.Proxy)      │
│ (opt-in)         │      │                      │
│                  │      │ - Startup check       │
│ - Checks every   │      │ - Warning display     │
│   6 hours        │      │ - Non-blocking        │
│ - Auto-renews    │      │                      │
│   when ≤ 30 days │      │                       │
└──────────────────┘      └──────────────────────┘
        │
        ▼
┌─────────────────────────────────────────────────────────────┐
│              Configuration (Environment Variables)           │
│  - PORTLESS_CERT_WARNING_DAYS=30                            │
│  - PORTLESS_CERT_CHECK_INTERVAL_HOURS=6                     │
│  - PORTLESS_AUTO_RENEW=true                                 │
│  - PORTLESS_ENABLE_MONITORING=false                         │
└─────────────────────────────────────────────────────────────┘
```

---

## Rollout Plan

### Phase 17 Execution
1. Execute 17-01: Background monitoring service (~20-30 min)
2. Execute 17-02: CLI commands (~20-30 min)
3. Execute 17-03: Proxy startup integration (~10 min)
4. Execute 17-04: Documentation (~10-20 min)

### Verification
- Manual testing of all features
- Verify environment variables work
- Verify colored output displays correctly
- Verify restart requirement is documented

### Deployment
- Merge to development branch
- Update ROADMAP.md (mark Phase 17 complete)
- Commit with message style: `feat(phase-17): complete certificate lifecycle implementation`

---

## Known Limitations

### By Design (Deferred to v1.3+)
- **Hot Reload:** Certificate changes require proxy restart
- **Cross-Platform Trust:** macOS/Linux trust installation manual
- **Multiple CAs:** Only one CA certificate supported
- **Configurable Validity:** 5-year fixed validity period

### Technical Constraints
- **File Locking:** Certificate files locked during proxy operation
- **Background Service:** Requires proxy running for monitoring
- **Permission Issues:** Certificate regeneration requires write access to `~/.portless/`

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Background service crashes | Wrap in try-catch, log errors, continue monitoring |
| Certificate regeneration fails | Log error, keep old certificate, retry next interval |
| Environment variable conflicts | Document clearly, provide defaults, validate on startup |
| Proxy startup delay due to check | Make check async, non-blocking, fast (< 100ms) |
| User confusion about restart requirement | Document clearly in CLI output, show warning after renewal |

---

## Success Metrics

### Completion Criteria
- [ ] 17-01: Background monitoring service implemented and tested
- [ ] 17-02: CLI commands (renew, check) implemented and tested
- [ ] 17-03: Proxy startup integration implemented and tested
- [ ] 17-04: Documentation created and reviewed
- [ ] All requirements LIFECYCLE-01 through LIFECYCLE-07 validated
- [ ] All requirements CLI-03, CLI-06 validated
- [ ] Manual testing completed
- [ ] ROADMAP.md updated

### Quality Metrics
- Code follows existing patterns (Phase 13-14 style)
- All commands have proper exit codes
- Colored output uses Spectre.Console correctly
- Environment variables documented
- Security considerations documented

---

## Next Steps

**Immediate:**
1. Execute 17-01-PLAN.md (Background monitoring service)
2. Execute 17-02-PLAN.md (CLI commands)
3. Execute 17-03-PLAN.md (Proxy startup integration)
4. Execute 17-04-PLAN.md (Documentation)

**After Phase 17:**
- Execute Phase 18: Integration Tests
- Execute Phase 19: Documentation
- Archive milestone v1.2
- Plan v1.3 features

---

**Plan Status:** ✅ Ready for Execution
**Created:** 2026-02-23
**Author:** Claude (GSD Planning)
**User Context:** Captured in 17-CONTEXT.md
