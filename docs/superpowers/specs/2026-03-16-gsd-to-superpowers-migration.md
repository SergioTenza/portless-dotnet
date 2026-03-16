# GSD to Superpowers Migration + v1.2 Completion

**Design Date:** 2026-03-16
**Status:** Approved
**Next Action:** Create implementation plan with writing-plans skill

---

## Executive Summary

Migrate Portless.NET project from GSD (Get Stuff Done) framework to Superpowers framework, then complete v1.2 milestone gaps identified in recent audit. The migration extracts key decisions and validated requirements from GSD structure, archives the `.planning/` directory for reference, and establishes Superpowers workflow for future development. v1.2 completion addresses 18 unsatisfied requirements through VERIFICATION.md files, Phase 17 certificate lifecycle implementation, and integration test coverage.

**Timeline:** 4-8 hours total (Migration: 1-2h, v1.2 completion: 3-6h)
**Risk:** Low - GSD content preserved in archive, v1.2 gaps well-defined

---

## Background

### Current State

**Portless.NET** is a `.NET 10 dotnet tool` providing stable `.localhost` URLs for local development using YARP reverse proxy. The project has used GSD framework extensively:

- **Milestones:** v1.0 MVP (complete), v1.1 Advanced Protocols (complete), v1.2 HTTPS (partial - 50% complete)
- **Phases:** 19 phases total, phases 13-19 in v1.2 milestone
- **GSD Structure:** `.planning/` directory with PROJECT.md, STATE.md, ROADMAP.md, phases/, milestones/
- **Recent Audit:** v1.2 milestone audit revealed 18 unsatisfied requirements, missing VERIFICATION.md files (critical blocker)

### Why Migrate to Superpowers?

- **User Decision:** Framework change from GSD to Superpowers plugin
- **GSD Limitations:** Phases 13-19 marked "Complete" but lack formal verification (VERIFICATION.md files)
- **Superpowers Benefits:** Structured brainstorming, writing-plans, executing-plans, verification-before-completion skills
- **Workflow Alignment:** Better support for conversational UAT, atomic commits, parallel agent execution

---

## Migration Design

### What Content to Migrate

#### From `.planning/PROJECT.md`

**Core Value Statement:**
> "URLs estables y predecibles para desarrollo local"

**Validated Requirements:**
- **v1.0 (20 requirements):** HTTP proxy, CLI commands, port allocation, route persistence, process management, .NET integration, integration tests
- **v1.1 (15 requirements):** HTTP/2 support, protocol logging, X-Forwarded headers, WebSocket proxy, long-lived connections, SignalR integration, integration tests, documentation
- **v1.2 Partial (7 requirements):** Certificate generation, wildcard certificates, secure storage, Windows Certificate Store, CLI commands, cross-platform messaging, auto-renewal detection

**Key Decisions Table (13 decisions):**
| Decision | Rationale | Outcome |
|----------|-----------|---------|
| YARP en lugar de proxy custom | Production-ready de Microsoft, soporta HTTP/2/WebSockets | ✓ Good |
| Evolutivo vs feature-complete v1 | Validar MVP primero, agregar complejidad gradualmente | ✓ Good |
| .NET 10 con Native AOT | Single binary, mejor performance que Node.js | ✓ Good |
| Spectre.Console.Cli sobre System.CommandLine | Mejor experiencia CLI con output coloreado | ✓ Good |
| PackAsTool para distribución | dotnet tool install global para fácil instalación | ✓ Good |
| Instalación cross-platform | Scripts bash/PowerShell con PATH automático | ✓ Good |
| Ejemplos de integración | 4 proyectos ejemplares (WebApi, Blazor, Worker, Console) | ✓ Good |
| Documentación progresiva | Tutorials + guías de integración (3,049 líneas) | ✓ Good |
| ForwardedHeaders vs YARP transforms | ASP.NET Core built-in middleware más simple | ✓ Good |
| Kestrel timeout configuration (10-min) | Soporta long-lived WebSocket connections | ✓ Good |
| SignalR sin YARP special config | SignalR WebSocket funciona automáticamente | ✓ Good |
| Echo server vs full chat app | Server simple más fácil de testear | ✓ Good |
| Windows Certificate Store integration | LocalMachine Root store para system-wide trust | ✓ Good |

**Constraints:**
- Tech Stack: .NET 10, C# 14, YARP 2.3.0
- Platform: Windows 10+ (primary), macOS 12+, Linux (validation deferred to v1.3+)
- Distribution: dotnet tool global con Native AOT
- Performance: <5ms overhead, >10,000 req/sec
- Compatibility: Idéntico comportamiento en Windows, macOS, Linux

**Out of Scope:**
- Interfaz gráfica (CLI tool para desarrolladores)
- Soporte remoto (solo desarrollo local)
- Load balancing (single destination por hostname)
- Auth/Z (no expone servicios externamente)
- Cross-platform completo (macOS/Linux validación deferida)
- HTTP/3 (QUIC) (deferred to v1.3+)
- Certificate revocation (desarrollo local no requiere)
- Multiple certificate authorities (single CA integrado suficiente)
- EV certificates / Organization validation (self-signed certs)

#### From `.planning/STATE.md`

**Accumulated Context → Decisions (80+ decisions):**
Extract 80+ phase-level decisions covering:
- Certificate generation (Phase 13): .NET native APIs, PFX storage, secure permissions
- Trust installation (Phase 14): Windows X509Store API, platform guards, idempotent operations
- HTTPS endpoint (Phase 15): Fixed ports (HTTP=1355, HTTPS=1356), TLS 1.2+ enforcement
- Mixed protocol support (Phase 16): X-Forwarded-Proto headers, YARP HttpClient configuration
- Certificate lifecycle (Phase 17): Background IHostedService monitoring, auto-renewal
- Integration tests (Phase 18): WebApplicationFactory patterns, test isolation
- Documentation (Phase 19): Migration guide structure, troubleshooting format

**Recent Decisions Affecting Current Work:**
- Fixed ports enforced with PORTLESS_PORT deprecation warning
- 308 Permanent Redirect used instead of 301 for HTTP→HTTPS
- /api/v1/* management endpoints excluded from HTTPS redirect
- Certificate pre-startup validation exits with code 1 if invalid
- TLS 1.2+ minimum protocol enforced via ConfigureHttpsDefaults
- Background monitoring service with 6-hour check interval (configurable)
- Certificate auto-renewal enabled by default with opt-out
- Integration tests use configuration verification vs actual TLS handshake

#### From `.planning/ROADMAP.md`

**Phase Summaries:**
- v1.0 MVP (Phases 1-8): HTTP proxy, CLI, port management, process management, .NET integration, integration tests
- v1.1 Advanced Protocols (Phases 9-12): HTTP/2, WebSocket, SignalR, documentation
- v1.2 HTTPS (Phases 13-19): Certificate generation, trust installation, HTTPS endpoint, mixed protocol, certificate lifecycle, integration tests, documentation

**Dependencies:**
- Phase 14 depends on Phase 13 (certificate generation → trust installation)
- Phase 15 depends on Phase 13 (certificate generation → HTTPS endpoint)
- Phase 16 depends on Phase 15 (HTTPS endpoint → mixed protocol)
- Phase 17 depends on Phase 13 (certificate generation → lifecycle)
- Phase 18 depends on Phases 15-17 (HTTPS features → integration tests)
- Phase 19 depends on Phases 14-15-17 (certificate features → documentation)

#### From `.planning/v1.2-MILESTONE-AUDIT.md`

**Critical Blockers Identified:**
1. **Missing VERIFICATION.md files** (7 phases) - All phases technically unverified
2. **Phase 17 Certificate Lifecycle** - 9 requirements unchecked (LIFECYCLE-01 through 07, CLI-03, CLI-06)
3. **Integration Test Gaps** - TEST-03 (X-Forwarded-Proto), TEST-06 (mixed routing) unchecked

**18 Unsatisfied Requirements:**
- CERT-07, CERT-08 (certificate security, persistence)
- TRUST-03, CLI-01, CLI-02, CLI-04 (trust commands)
- HTTPS-02 (configurable HTTPS port)
- LIFECYCLE-01 through LIFECYCLE-07 (7 lifecycle requirements)
- CLI-03, CLI-06 (renew command, colored output)
- TEST-03, TEST-06 (header preservation, mixed routing tests)
- DOCS-01 through DOCS-05 (documentation checkboxes)

---

### Superpowers Directory Structure

```
docs/superpowers/
├── specs/
│   ├── 2026-03-16-gsd-to-superpowers-migration.md  (this file)
│   ├── 2026-03-16-v1.2-completion-verification.md  (VERIFICATION.md files)
│   └── 2026-03-16-v1.2-certificate-lifecycle.md     (Phase 17 gaps)
├── decisions.md                                     (93+ extracted decisions)
├── validated-requirements.md                        (v1.0, v1.1, v1.2 partial)
└── README.md                                        (Superpowers workflow guide)

.planning.archived/                                   (renamed from .planning/)
├── PROJECT.md                                        (reference)
├── STATE.md                                          (reference)
├── ROADMAP.md                                        (reference)
├── v1.2-MILESTONE-AUDIT.md                           (reference)
├── phases/                                           (reference)
│   ├── 13-certificate-generation/
│   ├── 14-trust-installation/
│   ├── 15-https-endpoint/
│   ├── 16-mixed-protocol-support/
│   ├── 17-certificate-lifecycle/
│   ├── 18-integration-tests/
│   └── 19-documentation/
└── milestones/
    ├── v1.0-ROADMAP.md
    ├── v1.1-ROADMAP.md
    └── v1.2-ROADMAP.md
```

---

### Migration Workflow

#### Phase 1: Extract and Archive (1-2 hours)

**Step 1.1: Extract Key Decisions**
1. Read `.planning/PROJECT.md` Key Decisions table (13 decisions)
2. Read `.planning/STATE.md` Accumulated Context → Decisions section (80+ decisions)
3. Consolidate into `docs/superpowers/decisions.md` with categories:
   - Architectural decisions (YARP, .NET 10, Spectre.Console, Native AOT)
   - Protocol decisions (HTTP/2, WebSocket, SignalR, timeouts)
   - Certificate decisions (Windows Certificate Store, PFX storage, secure permissions)
   - Platform decisions (Windows focus, macOS/Linux deferred)
   - Testing decisions (WebApplicationFactory, test isolation, configuration verification)
   - CLI decisions (Spectre.Console.Cli, colored output, exit codes)

**Step 1.2: Extract Validated Requirements**
1. Copy validated requirements from PROJECT.md
2. Organize by milestone (v1.0: 20 requirements, v1.1: 15 requirements, v1.2 partial: 7 requirements)
3. Create `docs/superpowers/validated-requirements.md`

**Step 1.3: Archive GSD Structure**
1. Rename `.planning/` → `.planning.archived/`
2. Add `.planning.archived/README.md`:
   ```markdown
   # GSD Framework Archive

   This directory contains the original GSD (Get Stuff Done) framework planning structure,
   preserved for historical reference. The project has migrated to the Superpowers framework.

   ## Contents
   - PROJECT.md: Original project reference with key decisions
   - STATE.md: GSD state tracking with phase decisions
   - ROADMAP.md: Phase summaries and milestone progress
   - phases/: Individual phase plans and summaries
   - milestones/: Milestone-specific roadmaps and requirements

   ## Migration Date
   - Migrated to Superpowers: 2026-03-16
   - See: docs/superpowers/ for current planning structure
   ```

**Step 1.4: Update CLAUDE.md**
1. Remove GSD workflow references
2. Add Superpowers workflow reference:
   ```markdown
   ## Development Workflow

   This project uses the Superpowers framework for development.
   See docs/superpowers/README.md for workflow details.
   ```

#### Phase 2: Create Migration Spec (30 min)

**Step 2.1: Write Migration Spec**
1. Create this file: `docs/superpowers/specs/2026-03-16-gsd-to-superpowers-migration.md`
2. Document migration approach, directory structure, workflow
3. Commit to git with message: "docs: add GSD to Superpowers migration spec"

**Step 2.2: Create v1.2 Completion Spec**
1. Create `docs/superpowers/specs/2026-03-16-v1.2-completion-verification.md`
2. Document VERIFICATION.md file creation workflow
3. Document Phase 17 gap closure approach
4. Commit to git with message: "docs: add v1.2 completion spec"

---

## v1.2 Completion Design

### Scope: Address Audit Gaps Only

**18 Unsatisfied Requirements to Address:**

1. **VERIFICATION.md Files (7 phases)**
   - Phase 13: Certificate Generation
   - Phase 14: Trust Installation
   - Phase 15: HTTPS Endpoint
   - Phase 16: Mixed Protocol Support
   - Phase 17: Certificate Lifecycle
   - Phase 18: Integration Tests
   - Phase 19: Documentation

2. **Phase 17: Certificate Lifecycle (9 requirements)**
   - LIFECYCLE-01: Startup certificate check with warning
   - LIFECYCLE-02: 30-day expiration warning
   - LIFECYCLE-03: Background monitoring service (6-hour interval)
   - LIFECYCLE-04: Auto-renewal within 30 days
   - LIFECYCLE-05: `portless cert renew` command
   - LIFECYCLE-06: Restart required after renewal
   - LIFECYCLE-07: Certificate metadata in `~/.portless/cert-info.json`
   - CLI-03: Certificate renew command
   - CLI-06: Colored certificate output

3. **Phase 18: Integration Tests (2 requirements)**
   - TEST-03: X-Forwarded-Proto header preservation tests
   - TEST-06: Mixed HTTP/HTTPS backend routing tests

**Not in Scope:**
- Re-doing completed phases 13-16, 19 (already implemented and tested)
- New features beyond v1.2
- Cross-platform validation (deferred to v1.3+)

---

### v1.2 Completion Workflow

#### Phase 3: Investigation (30 min)

**Step 3.1: Investigate Phase 17 Features**
1. Check if certificate lifecycle features exist in code:
   ```bash
   grep -r "CertificateMonitor" Portless.Core/
   grep -r "IHostedService" Portless.Proxy/
   ls -la ~/.portless/cert-info.json  # check if metadata file exists
   ```
2. Test `portless cert renew` command:
   ```bash
   dotnet run --project Portless.Cli/ cert renew --help
   ```
3. Verify background monitoring service code
4. Determine if implementation needed or just verification

**Step 3.2: Check Integration Test Coverage**
1. Verify TEST-03 coverage:
   ```bash
   grep -r "X-Forwarded-Proto" Portless.IntegrationTests/
   ```
2. Verify TEST-06 coverage:
   ```bash
   grep -r "mixed.*HTTP.*HTTPS" Portless.IntegrationTests/
   ```
3. Identify test gaps

#### Phase 4: Implementation (if needed, 1-2 hours)

**Step 4.1: Implement Phase 17 Lifecycle Features (if missing)**
1. Background monitoring service:
   - Create `CertificateMonitor.cs` inheriting `IHostedService`
   - Check certificate expiration every 6 hours (configurable via PORTLESS_CERT_CHECK_INTERVAL_HOURS)
   - Log warning if within 30 days of expiration (PORTLESS_CERT_WARNING_DAYS)
   - Auto-renew if enabled (PORTLESS_AUTO_RENEW=true, default)

2. Certificate metadata:
   - Create `~/.portless/cert-info.json` on certificate generation
   - Store: creation timestamp, expiration, fingerprint, subject, issuer
   - Update on renewal

3. CLI renew command (if missing):
   - Add `cert renew` command to Portless.Cli
   - Colored Spectre.Console output (red=expired, yellow=expiring, green=valid)
   - Exit codes: 0=success, 1=error, 2=permissions, 3=missing

**Step 4.2: Implement Integration Tests**
1. TEST-03: X-Forwarded-Proto header preservation
   - Test HTTP backend receives `X-Forwarded-Proto: http`
   - Test HTTPS backend receives `X-Forwarded-Proto: https`
   - Verify header not overwritten by proxy

2. TEST-06: Mixed HTTP/HTTPS backend routing
   - Test proxy routes HTTP backend correctly
   - Test proxy routes HTTPS backend correctly
   - Verify YARP backend SSL validation accepts self-signed certs

#### Phase 5: VERIFICATION.md Files (2 hours)

**Step 5.1: Create VERIFICATION.md for Each Phase (13-19)**

**Format:**
```markdown
# Phase XX: [Phase Name] - Verification

**Verified:** 2026-03-16
**Verifier:** Claude Code (Superpowers verification-before-completion)
**Method:** Conversational User Acceptance Testing (UAT)

## Requirements Verified

### REQ-ID: [Requirement Name]
- **Status:** ✅ Verified / ❌ Failed / ⚠️ Partial
- **Test:** [Description of test performed]
- **Evidence:** [Output, screenshots, or test results]
- **Issues:** [Any issues found]

## User Acceptance Testing

### Test Case 1: [Test Name]
- **Steps:**
  1. [Step 1]
  2. [Step 2]
- **Expected Result:** [Expected outcome]
- **Actual Result:** [Actual outcome]
- **Status:** Pass / Fail

## Integration Verification

- [ ] Cross-phase integration tested
- [ ] End-to-end flow verified
- [ ] No regressions in existing features

## Sign-off

- [ ] All requirements satisfied
- [ ] All tests passing
- [ ] Documentation complete
- [ ] Ready for milestone completion

**Verified by:** [Name]
**Date:** 2026-03-16
```

**Verification Approach:**
- Use conversational UAT (ask user to test features)
- Document test results per phase
- Confirm requirements satisfaction
- Create files in `.planning.archived/phases/*/VERIFICATION.md`

#### Phase 6: Final Verification (30 min)

**Step 6.1: Verify All 18 Requirements Addressed**
1. Check VERIFICATION.md files created (7 files)
2. Verify Phase 17 lifecycle features work (manual testing)
3. Verify integration tests pass (TEST-03, TEST-06)
4. Run full test suite: `dotnet test`
5. Verify no regressions

**Step 6.2: Update v1.2 Milestone Status**
1. Update `.planning.archived/milestones/v1.2-ROADMAP.md`
2. Mark all phases 13-19 as "Complete - Verified"
3. Update milestone status to "Complete"
4. Document remaining technical debt (if any)

---

## Error Handling and Edge Cases

### Migration Risks

**Risk 1: Lost Context During Migration**
- **Probability:** Low (10%)
- **Impact:** High
- **Mitigation:**
  - Keep `.planning.archived/` intact, don't delete
  - Extract all decisions before archiving
  - Verify extraction completeness (check counts: 93+ decisions)
- **Recovery:** Restore from `.planning.archived/` if needed

**Risk 2: Incomplete Requirements Understanding**
- **Probability:** Medium (30%)
- **Impact:** Medium
- **Mitigation:**
  - Cross-reference REQUIREMENTS.md checkboxes with SUMMARY.md files
  - Verify against actual code implementation
  - Document assumptions
- **Recovery:** Re-extract from GSD archive if gaps found

**Risk 3: Superpowers Workflow Learning Curve**
- **Probability:** Medium (40%)
- **Impact:** Low
- **Mitigation:**
  - Start with simple tasks (VERIFICATION.md files)
  - Reference superpowers skill documentation
  - Use brainstorming → writing-plans → executing-plans pattern
- **Recovery:** Fall back to manual workflow if needed

### v1.2 Completion Risks

**Risk 1: VERIFICATION.md Reveals Missing Features**
- **Probability:** High (60%)
- **Impact:** Medium
- **Mitigation:**
  - Plan for potential implementation work (buffer 1-2 hours)
  - Timebox investigation (30 min max)
  - Prioritize critical gaps only
- **Recovery:** Implement missing features, update timeline

**Risk 2: Phase 17 Features May Already Exist**
- **Probability:** Medium (50%)
- **Impact:** Low (wasted investigation time)
- **Mitigation:**
  - Investigate code before implementing
  - Tests will reveal what's missing
  - Document findings clearly
- **Recovery:** Skip implementation if features exist

**Risk 3: Certificate Lifecycle Features Complex to Test**
- **Probability:** Medium (40%)
- **Impact:** Medium
- **Mitigation:**
  - Use integration tests with time mocking
  - Document manual testing procedures
  - Focus on observable behavior (CLI output, file creation)
- **Recovery:** Accept manual testing if automated tests too complex

**Risk 4: Test Environment May Not Support HTTPS**
- **Probability:** Low (20%)
- **Impact:** Low
- **Mitigation:**
  - WebApplicationFactory doesn't bind real TCP ports
  - Test configuration, not actual TLS handshake
  - Verify certificate properties, not connection security
- **Recovery:** Document test limitations

---

## Testing Strategy

### Migration Testing

**Pre-Migration Checks:**
- [ ] `.planning/` directory exists and is readable
- [ ] PROJECT.md has 13 key decisions
- [ ] STATE.md has 80+ accumulated decisions
- [ ] ROADMAP.md has phase summaries
- [ ] All phase directories (13-19) exist

**Post-Migration Checks:**
- [ ] `.planning.archived/` directory exists
- [ ] `docs/superpowers/decisions.md` has 93+ decisions extracted
- [ ] `docs/superpowers/validated-requirements.md` has all validated requirements
- [ ] Migration spec written and committed
- [ ] CLAUDE.md updated to remove GSD references
- [ ] Git history preserved

**Extraction Completeness:**
- Verify decision count: 13 (PROJECT.md) + 80+ (STATE.md) = 93+ decisions
- Verify requirement count: v1.0 (20) + v1.1 (15) + v1.2 partial (7) = 42 requirements
- Verify archive accessibility: all original files still readable

### v1.2 Completion Testing

**VERIFICATION.md Creation:**
- [ ] 7 VERIFICATION.md files created (phases 13-19)
- [ ] Each file has UAT section with test cases
- [ ] Each file has requirements verification table
- [ ] Each file has integration verification checklist
- [ ] Each file has sign-off section

**Phase 17 Lifecycle Features:**
- [ ] Background monitoring service exists (`CertificateMonitor.cs`)
- [ ] `~/.portless/cert-info.json` created with metadata
- [ ] `portless cert renew` command works
- [ ] Colored output displayed (red/yellow/green)
- [ ] Auto-renewal within 30 days works
- [ ] Restart warning displayed

**Integration Tests:**
- [ ] TEST-03: X-Forwarded-Proto header tests pass
- [ ] TEST-06: Mixed HTTP/HTTPS backend routing tests pass
- [ ] All integration tests pass (`dotnet test Portless.IntegrationTests`)
- [ ] No test regressions

**Final Verification:**
- [ ] All 18 unsatisfied requirements satisfied
- [ ] All tests pass (`dotnet test`)
- [ ] Manual testing confirms features work
- [ ] v1.2 milestone status updated to "Complete"

---

## Success Criteria

### Migration Success

**Must Have:**
- ✅ `.planning.archived/` contains all original GSD content
- ✅ `docs/superpowers/decisions.md` has 93+ decisions extracted
- ✅ `docs/superpowers/validated-requirements.md` has all validated requirements
- ✅ Migration spec written and committed to git
- ✅ CLAUDE.md updated to remove GSD references
- ✅ No data loss (all GSD content preserved)

**Should Have:**
- ✅ Clear mapping from GSD concepts to Superpowers workflow
- ✅ Superpowers README.md explaining new workflow
- ✅ Git commit history shows clean migration

**Nice to Have:**
- ✅ Migration script for automated extraction
- ✅ Decision categories well-organized
- ✅ Cross-references between archive and new structure

### v1.2 Completion Success

**Must Have:**
- ✅ 7 VERIFICATION.md files created (phases 13-19)
- ✅ 18 unsatisfied requirements satisfied
- ✅ Phase 17 lifecycle features implemented and tested
- ✅ TEST-03 and TEST-06 integration tests passing
- ✅ All tests pass (`dotnet test`)
- ✅ v1.2 milestone can be marked complete

**Should Have:**
- ✅ No regressions in existing features
- ✅ Documentation updated (README.md, migration guide)
- ✅ Manual testing confirms certificate lifecycle works
- ✅ Integration tests cover edge cases

**Nice to Have:**
- ✅ Performance benchmarks for certificate operations
- ✅ Troubleshooting guide for certificate issues
- ✅ Automated tests for background monitoring service

---

## Timeline Estimate

**Session Breakdown:**

**Session 1: Migration Complete (1-2 hours)**
- Extract decisions (30 min)
- Extract requirements (15 min)
- Archive GSD structure (15 min)
- Create migration spec (30 min)
- Update CLAUDE.md (15 min)
- Commit changes (15 min)

**Session 2: v1.2 VERIFICATION Files (2 hours)**
- Phase 13 VERIFICATION (15 min)
- Phase 14 VERIFICATION (15 min)
- Phase 15 VERIFICATION (15 min)
- Phase 16 VERIFICATION (15 min)
- Phase 17 VERIFICATION (30 min - includes investigation)
- Phase 18 VERIFICATION (15 min)
- Phase 19 VERIFICATION (15 min)
- Commit VERIFICATION files (15 min)

**Session 3: Phase 17 Implementation (1-2 hours)**
- Investigate existing code (30 min)
- Implement missing features (30-60 min)
- Write integration tests (30 min)
- Manual testing (15 min)
- Commit changes (15 min)

**Session 4: Tests and Final Verification (1-2 hours)**
- Implement TEST-03 (30 min)
- Implement TEST-06 (30 min)
- Run full test suite (15 min)
- Manual verification (15 min)
- Update milestone status (15 min)
- Final commit (15 min)

**Total: 4-8 hours** (can be split across multiple sessions)

---

## Dependencies and Blocking Items

**Migration Dependencies:**
- None (can start immediately)

**v1.2 Completion Dependencies:**
- Migration must complete first
- Phase 17 implementation depends on investigation results
- VERIFICATION files may reveal additional dependencies

**External Dependencies:**
- None (all work is internal to codebase)

---

## Open Questions

**For User:**
1. Should VERIFICATION.md files be created in `.planning.archived/phases/*/` or new location?
2. Should v1.2 completion be done in single session or split across sessions?
3. Are there any v1.2 requirements you consider "out of scope" despite audit findings?

**For Investigation:**
1. Do Phase 17 lifecycle features already exist in code?
2. Are TEST-03 and TEST-06 already covered by existing tests?
3. What is the actual state of certificate lifecycle implementation?

---

## Next Steps

1. **User Approval:** Review this spec and confirm approach
2. **Invoke writing-plans skill:** Create detailed implementation plan
3. **Execute migration:** Extract content, archive GSD, create Superpowers structure
4. **Execute v1.2 completion:** Create VERIFICATION files, implement gaps, run tests
5. **Final verification:** Confirm all requirements satisfied, milestone complete

---

## Appendix A: GSD to Superpowers Mapping

| GSD Concept | Superpowers Equivalent | Notes |
|-------------|------------------------|-------|
| `/gsd:plan-phase` | `superpowers:brainstorming` + `superpowers:writing-plans` | Brainstorm design, then write implementation plan |
| `/gsd:execute-phase` | `superpowers:executing-plans` | Atomic commits, checkpoint-based execution |
| `/gsd:verify-work` | `superpowers:verification-before-completion` | Conversational UAT before claiming completion |
| `/gsd:debug` | `superpowers:systematic-debugging` | Scientific method debugging with persistent state |
| `PLAN.md` | `docs/superpowers/specs/*-design.md` | Spec documents with implementation plans |
| `SUMMARY.md` | Git commit messages | Atomic commits describe what was done |
| `VERIFICATION.md` | `superpowers:verification-before-completion` output | Verification evidence documented |
| `REQUIREMENTS.md` | `docs/superpowers/validated-requirements.md` | Extracted requirements list |
| `KEY-DECISIONS.md` | `docs/superpowers/decisions.md` | Consolidated decision log |

---

## Appendix B: File Inventory

### Files to Create

**Migration:**
- `docs/superpowers/specs/2026-03-16-gsd-to-superpowers-migration.md` (this file)
- `docs/superpowers/specs/2026-03-16-v1.2-completion-verification.md`
- `docs/superpowers/decisions.md`
- `docs/superpowers/validated-requirements.md`
- `docs/superpowers/README.md`
- `.planning.archived/README.md`

**v1.2 Completion:**
- `.planning.archived/phases/13-certificate-generation/VERIFICATION.md`
- `.planning.archived/phases/14-trust-installation/VERIFICATION.md`
- `.planning.archived/phases/15-https-endpoint/VERIFICATION.md`
- `.planning.archived/phases/16-mixed-protocol-support/VERIFICATION.md`
- `.planning.archived/phases/17-certificate-lifecycle/VERIFICATION.md`
- `.planning.archived/phases/18-integration-tests/VERIFICATION.md`
- `.planning.archived/phases/19-documentation/VERIFICATION.md`

**Potential Implementation (if gaps found):**
- `Portless.Core/Services/CertificateMonitor.cs` (if missing)
- `Portless.IntegrationTests/Certificate/ForwardedProtoTests.cs` (TEST-03)
- `Portless.IntegrationTests/Certificate/MixedBackendRoutingTests.cs` (TEST-06)

### Files to Modify

- `CLAUDE.md` (remove GSD references, add Superpowers reference)
- `.planning/` → `.planning.archived/` (rename directory)

### Files to Read

- `.planning/PROJECT.md` (extract decisions, requirements)
- `.planning/STATE.md` (extract accumulated decisions)
- `.planning/ROADMAP.md` (extract phase summaries)
- `.planning/v1.2-MILESTONE-AUDIT.md` (extract gap details)

---

**Document Version:** 1.0
**Last Updated:** 2026-03-16
**Status:** Ready for implementation plan creation

---

## Sign-off

**Designer:** Claude Code (Superpowers brainstorming skill)
**Reviewers:** [User name]
**Stakeholders:** [User name]

**Approval:**
- [ ] Spec reviewed
- [ ] Approach confirmed
- [ ] Ready for writing-plans skill

**Date:** 2026-03-16
