---
phase: quick-2
plan: 2
type: execute
wave: 1
depends_on: []
files_modified: [.git/config, .planning/STATE.md]
autonomous: true
requirements: [GIT-WORKFLOW-001]
user_setup: []

must_haves:
  truths:
    - "Development branch exists and is tracked on remote"
    - "Main branch is protected from direct commits during development"
    - "Changes can be isolated in development branch"
    - "CI/CD respects branch-specific behaviors"
  artifacts:
    - path: ".git/config"
      provides: "Git branch tracking configuration"
      contains: "branch \"development\""
    - path: "refs/heads/development"
      provides: "Local development branch"
      exists: true
    - path: ".planning/STATE.md"
      provides: "Updated project state with new workflow"
      contains: "development branch"
  key_links:
    - from: "local development branch"
      to: "origin/development"
      via: "git push -u origin development"
      pattern: "branch.*development.*remote"
---

<objective>
Establecer flujo de trabajo git con rama development para aislar cambios en desarrollo

Purpose: Aislar el trabajo en desarrollo del código principal (main) que representa la versión v1.0 MVP estable. Esto permite experimentar con nuevas características sin afectar la rama de producción.

Output: Rama development configurada y sincronizada con remote, con guía de workflow documentada en STATE.md
</objective>

<execution_context>
@C:/Users/serge/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/serge/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/STATE.md
@.planning/PROJECT.md

# Git workflow context
- Current branch: main (v1.0 MVP shipped)
- Local is 20 commits ahead of origin/main
- Need to push main first before creating development branch
- Workflow: main = production, development = active development
</context>

<tasks>

<task type="auto">
  <name>Task 1: Push main branch to remote and create development branch</name>
  <files>.git/config, refs/heads/development, refs/remotes/origin/development</files>
  <action>
    Push current main branch to remote (20 commits ahead):
    ```bash
    git push origin main
    ```

    Create and checkout development branch from main:
    ```bash
    git checkout -b development
    ```

    Push development branch to remote and set upstream:
    ```bash
    git push -u origin development
    ```

    Verify branch structure:
    ```bash
    git branch -vv
    ```

    This establishes:
    - main branch: Represents v1.0 MVP stable production code
    - development branch: Isolated environment for new feature development
    - Both branches tracked on remote for collaboration
  </action>
  <verify>
    ```bash
    git branch -vv
    ```
    Expected output shows:
    - main branch tracking origin/main
    - development branch tracking origin/development
    - Current branch: development
  </verify>
  <done>
    Development branch created, tracking remote origin/development, and checked out as current branch
  </done>
</task>

<task type="auto">
  <name>Task 2: Update STATE.md with git workflow documentation</name>
  <files>.planning/STATE.md</files>
  <action>
    Update STATE.md to document the new git workflow:

    1. Add new section "## Git Workflow" after "## Current Position"
    2. Document branch structure:
       - main: Production branch (v1.0 MVP)
       - development: Active development branch
    3. Add workflow rules:
       - All new development happens in development branch
       - Merge development → main when ready for release
       - main is protected, only for releases
    4. Update "Current Position" to note current branch is development
    5. Add quick task #2 completion to "Quick Tasks Completed" table

    Read current STATE.md first, then append the workflow section without modifying existing content.
  </action>
  <verify>
    ```bash
    grep -A 10 "## Git Workflow" .planning/STATE.md
    ```
    Expected output shows Git Workflow section with branch structure and rules documented
  </verify>
  <done>
    STATE.md updated with git workflow documentation and quick task #2 recorded
  </done>
</task>

</tasks>

<verification>
1. Verify both branches exist locally and remotely:
   ```bash
   git branch -a
   ```
   Should show: main, development, origin/main, origin/development

2. Verify current branch is development:
   ```bash
   git branch --show-current
   ```
   Should output: development

3. Verify STATE.md includes Git Workflow section:
   ```bash
   grep -c "## Git Workflow" .planning/STATE.md
   ```
   Should output: 1

4. Verify quick task recorded in STATE.md:
   ```bash
   grep -A 1 "Quick Tasks Completed" .planning/STATE.md | grep -c "| 2 |"
   ```
   Should output: 1
</verification>

<success_criteria>
- Development branch created and tracked on remote
- Main branch pushed to remote (20 commits synced)
- Current working branch is development
- STATE.md documents git workflow clearly
- Quick task #2 recorded in STATE.md completion table
</success_criteria>

<output>
After completion, create `.planning/quick/2-establecer-flujo-de-trabajo-git-con-rama/2-SUMMARY.md`
</output>
