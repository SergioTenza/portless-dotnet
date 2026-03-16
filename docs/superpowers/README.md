# Superpowers Framework Workflow

This project uses the Superpowers framework for development planning and execution.

## Quick Start

1. **Planning Phase:** Use `brainstorming` skill to explore design options
2. **Spec Creation:** Use `writing-plans` skill to create implementation specs
3. **Execution:** Use `executing-plans` skill for atomic, checkpoint-based work
4. **Verification:** Use `verification-before-completion` for UAT

## Key Documents

- **decisions.md:** All architectural and technical decisions (93+ decisions)
- **validated-requirements.md:** Accepted requirements by milestone (v1.0, v1.1, v1.2)
- **specs/**: Detailed implementation specifications
- **plans/**: Implementation plans with step-by-step tasks

## Migration from GSD

This project migrated from GSD framework on 2026-03-16.
See `.planning.archived/` for historical GSD planning documents.
See `specs/2026-03-16-gsd-to-superpowers-migration.md` for migration details.

## Superpowers Skills Used

- `brainstorming`: Design exploration and option analysis
- `writing-plans`: Structured implementation plan creation
- `executing-plans`: Atomic commits with clear checkpoints
- `verification-before-completion`: Conversational UAT before claiming done
- `systematic-debugging`: Scientific method debugging workflow

## Workflow Differences from GSD

| GSD | Superpowers | Notes |
|-----|-------------|-------|
| Phase-based plans | Spec-based plans | More flexible scope |
| /gsd:plan-phase | brainstorming + writing-plans | Two-step process |
| /gsd:execute-phase | executing-plans | Atomic commits emphasized |
| /gsd:verify-work | verification-before-completion | Conversational UAT focus |
