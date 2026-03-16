# Phase 12: Documentation - Plan

**Phase:** 12 of 12 (v1.1 Advanced Protocols)
**Created:** 2026-02-22
**Status:** Planned

## Goal Verification

**Goal:** Complete documentation for HTTP/2 and WebSocket features added in v1.1

**What "Complete Documentation" Means:**
- Users can discover and use HTTP/2 and WebSocket features
- Common issues have troubleshooting guidance
- Examples demonstrate all new capabilities
- API/CLI changes are documented
- Performance characteristics are explained

## Dependency Analysis

**Depends on:** Phase 11 (SignalR Integration)
- ✓ HTTP/2 baseline is working (Phase 9)
- ✓ WebSocket proxy is working (Phase 10)
- ✓ SignalR example demonstrates real-time communication (Phase 11)
- ✓ Integration tests cover HTTP/2 and WebSocket scenarios
- ✓ Troubleshooting scenarios identified during testing

**Provides for:**
- Complete v1.1 milestone documentation
- User onboarding for advanced protocols
- Foundation for v1.2 documentation (HTTPS, cross-platform)

## Requirements Coverage

From REQUIREMENTS.md:

- [ ] **DOC-01**: README actualizado con HTTP/2 y WebSocket support section
- [ ] **DOC-02**: Troubleshooting guide para protocol issues (silent downgrade, timeouts)
- [ ] **DOC-03**: CLI help text updates (`--protocols` flag documentation)
- [ ] **DOC-04**: Protocol testing guide (curl commands, browser DevTools)

## Success Criteria

**What must be TRUE for this phase to succeed:**

1. ✅ README prominently features HTTP/2 and WebSocket support
2. ✅ Troubleshooting guide covers common protocol issues
3. ✅ CLI help text includes new protocol-related flags
4. ✅ Protocol testing guide provides verification steps
5. ✅ Examples README links HTTP/2 and WebSocket examples
6. ✅ Migration guide from v1.0 to v1.1 exists

## Research Findings

**Documentation Structure:**
```
README.md
├── HTTP/2 and WebSocket Support (NEW)
│   ├── HTTP/2 Benefits
│   ├── WebSocket Support
│   └── Quick Start Examples
├── CLI Commands (UPDATED)
│   └── New --protocols flag
└── Troubleshooting (UPDATED)
    └── Protocol Issues

docs/
├── http2-websocket-guide.md (NEW)
├── protocol-troubleshooting.md (NEW)
└── migration-v1.0-to-v1.1.md (NEW)

Examples/
└── README.md (UPDATED)
    ├── HTTP/2 Integration Test
    ├── WebSocket Echo Server
    └── SignalR Chat
```

**Key Documentation Points:**
- HTTP/2: How to verify it's working (curl --http2, browser DevTools)
- WebSocket: How to test (echo server, SignalR example)
- Silent downgrade: What it is, how to detect, how to fix
- Timeouts: KeepAliveTimeout configuration, long-lived connections
- Migration: What changed from v1.0, new features, breaking changes

**Testing Verification:**
- Include curl commands for protocol verification
- Include browser DevTools instructions
- Include PowerShell/Bash scripts for automated testing
- Include expected output examples

## Plans

### Plan 12-01: Update Main README with HTTP/2 and WebSocket Support

**Goal:** Add prominent HTTP/2 and WebSocket section to main README with examples

**Success Criteria:**
1. README has "HTTP/2 and WebSocket Support" section near top
2. Section explains benefits and use cases
3. Quick start examples show how to use
4. Links to examples and detailed documentation
5. README structure is reorganized to highlight new features

**Tasks:**
1. **Add HTTP/2 and WebSocket section to README** - Insert after Overview, before Installation
2. **Document HTTP/2 benefits** - Multiplexing, header compression, server push mention
3. **Document WebSocket support** - Real-time apps, SignalR, long-lived connections
4. **Add quick start examples** - curl commands for testing, example links
5. **Reorganize README structure** - Feature highlights, better flow

**Estimated Tasks:** 5
**Estimated Duration:** 10-12 minutes

**Key Files:**
- Modify: `README.md`

**Dependencies:**
- None (can start in parallel with other plans)

**Acceptance Criteria:**
- [ ] HTTP/2 and WebSocket section is prominent in README
- [ ] Benefits and use cases explained clearly
- [ ] Quick start examples provided (curl, browser DevTools)
- [ ] Links to examples and detailed docs
- [ ] README structure is logical and scannable

### Plan 12-02: Create Protocol Troubleshooting Guide

**Goal:** Comprehensive troubleshooting guide for HTTP/2 and WebSocket issues

**Success Criteria:**
1. Troubleshooting guide covers silent downgrade issue
2. Guide covers WebSocket timeout issues
3. Guide covers HTTP/2 negotiation failures
4. Guide includes diagnostic commands (curl, DevTools)
5. Guide includes solutions and workarounds

**Tasks:**
1. **Create troubleshooting guide document** - docs/protocol-troubleshooting.md
2. **Document silent downgrade issue** - Detection, causes, solutions (HTTPS, prior knowledge)
3. **Document WebSocket timeout issues** - KeepAliveTimeout config, long-lived connections
4. **Document diagnostic commands** - curl --http2, browser DevTools Network tab
5. **Add troubleshooting section to README** - Link to detailed guide

**Estimated Tasks:** 5
**Estimated Duration:** 10-12 minutes

**Key Files:**
- Create: `docs/protocol-troubleshooting.md`
- Modify: `README.md` (add Troubleshooting section)

**Dependencies:**
- Phase 9, 10, 11 complete (issues identified during testing)

**Acceptance Criteria:**
- [ ] Troubleshooting guide covers common HTTP/2 issues
- [ ] Troubleshooting guide covers common WebSocket issues
- [ ] Diagnostic commands provided (curl, DevTools)
- [ ] Solutions and workarounds documented
- [ ] README links to troubleshooting guide

### Plan 12-03: Update CLI Help Text and Documentation

**Goal:** Update CLI help text for protocol-related flags and commands

**Success Criteria:**
1. CLI help text documents HTTP/2 support
2. CLI help text documents WebSocket support
3. Help text includes --protocols flag (if implemented)
4. Status command shows protocol information
5. Help text examples include protocol testing

**Tasks:**
1. **Review CLI commands for protocol relevance** - Identify commands that need updates
2. **Update help text for relevant commands** - Add HTTP/2 and WebSocket mentions
3. **Add protocol info to status command** - Show HTTP/2 enabled, WebSocket supported
4. **Create CLI documentation section** - Document protocol-related commands
5. **Test CLI help output** - Verify accuracy and clarity

**Estimated Tasks:** 5
**Estimated Duration:** 8-10 minutes

**Key Files:**
- Modify: `Portless.Cli/Commands/` (help text updates)
- Create: `docs/cli-reference.md` (if not exists)

**Dependencies:**
- None (can start in parallel)

**Acceptance Criteria:**
- [ ] CLI help text mentions HTTP/2 and WebSocket support
- [ ] Status command shows protocol information
- [ ] Help text examples include protocol testing
- [ ] CLI documentation updated
- [ ] Help output is clear and accurate

### Plan 12-04: Create Protocol Testing Guide

**Goal:** Comprehensive guide for testing HTTP/2 and WebSocket functionality

**Success Criteria:**
1. Guide includes curl commands for HTTP/2 testing
2. Guide includes browser DevTools instructions
3. Guide includes WebSocket testing steps
4. Guide includes SignalR testing steps
5. Guide includes expected output examples

**Tasks:**
1. **Create protocol testing guide** - docs/http2-websocket-guide.md
2. **Document HTTP/2 testing** - curl commands, browser DevTools, expected output
3. **Document WebSocket testing** - echo server example, SignalR example
4. **Document protocol verification** - How to confirm HTTP/2 is active
5. **Add testing examples** - Copy-paste commands with explanations

**Estimated Tasks:** 5
**Estimated Duration:** 10-12 minutes

**Key Files:**
- Create: `docs/http2-websocket-guide.md`
- Modify: `Examples/README.md` (link to testing guide)

**Dependencies:**
- Phase 9, 10, 11 complete (examples exist)

**Acceptance Criteria:**
- [ ] Testing guide covers HTTP/2 verification
- [ ] Testing guide covers WebSocket verification
- [ ] curl commands provided with explanations
- [ ] Browser DevTools instructions provided
- [ ] Expected output examples included
- [ ] Examples README links to testing guide

### Plan 12-05: Create Migration Guide and Update Examples README

**Goal:** Migration guide from v1.0 to v1.1 and update examples README

**Success Criteria:**
1. Migration guide explains what changed in v1.1
2. Migration guide lists new features
3. Migration guide documents breaking changes (if any)
4. Examples README links all new examples
5. Examples README includes quick start for each example

**Tasks:**
1. **Create migration guide** - docs/migration-v1.0-to-v1.1.md
2. **Document new features** - HTTP/2, WebSocket, SignalR support
3. **Document breaking changes** - Any config changes, behavioral changes
4. **Update Examples README** - Add HTTP/2 test, WebSocket echo, SignalR chat
5. **Add quick start for each example** - How to run, expected output

**Estimated Tasks:** 5
**Estimated Duration:** 8-10 minutes

**Key Files:**
- Create: `docs/migration-v1.0-to-v1.1.md`
- Modify: `Examples/README.md`

**Dependencies:**
- All other Phase 12 plans complete (documentation consistent)

**Acceptance Criteria:**
- [ ] Migration guide explains v1.1 changes
- [ ] New features documented with examples
- [ ] Breaking changes documented (if any)
- [ ] Examples README links all examples
- [ ] Quick start provided for each example

## Phase Completion Checklist

**When all plans are complete:**
- [ ] All requirements (DOC-01 through DOC-04) are satisfied
- [ ] README prominently features HTTP/2 and WebSocket
- [ ] Troubleshooting guide covers common issues
- [ ] CLI help text updated
- [ ] Protocol testing guide provided
- [ ] Migration guide from v1.0 to v1.1 created
- [ ] Examples README updated
- [ ] Phase 12 summary documents completion of v1.1
- [ ] v1.1 milestone ready for completion

## Known Risks and Mitigations

**Risk 1: Documentation becomes outdated quickly**
- **Mitigation:** Keep documentation close to code, use examples as single source of truth
- **Verification:** All documentation tested against running examples

**Risk 2: Documentation is too technical**
- **Mitigation:** Include both quick start (simple) and detailed (technical) sections
- **Verification:** User testing feedback (if available)

**Risk 3: Missing edge cases in troubleshooting**
- **Mitigation:** Document issues found during Phase 9-11 testing
- **Verification:** Review test failures and blocker history

## Notes

- Documentation should be example-driven (show, don't just tell)
- Include copy-paste commands wherever possible
- Use screenshots/browser DevTools screenshots if valuable (ASCII diagrams for text-only)
- Keep troubleshooting guide focused on common issues (90% of cases)
- Link back and forth between documents (README ↔ guides ↔ examples)

---
*Phase: 12-documentation*
*Plan: 01, 02, 03, 04, 05*
*Status: Draft*
