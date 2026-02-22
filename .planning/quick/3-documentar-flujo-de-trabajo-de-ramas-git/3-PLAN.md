---
phase: quick-3
plan: 3
type: execute
wave: 1
depends_on: []
files_modified: [CLAUDE.md]
autonomous: true
requirements: []
user_setup: []

must_haves:
  truths:
    - "CLAUDE.md contains git workflow documentation section"
    - "Workflow rules are clearly explained for developers"
    - "Branch structure (main/development) is documented"
  artifacts:
    - path: "CLAUDE.md"
      provides: "Git workflow documentation for developers"
      contains: "## Git Workflow"
  key_links:
    - from: "CLAUDE.md"
      to: ".planning/STATE.md"
      via: "Cross-reference to detailed workflow documentation"
      pattern: "STATE.md"
---

<objective>
Document the established git workflow (main + development branches) in CLAUDE.md to provide developers with clear guidance on branch usage and contribution flow.

Purpose: CLAUDE.md is the primary reference for developers working on this project. The git workflow established in quick task #2 should be documented there for easy reference.

Output: Updated CLAUDE.md with Git Workflow section
</objective>

<execution_context>
@C:/Users/serge/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/serge/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/STATE.md
@.planning/quick/2-establecer-flujo-de-trabajo-git-con-rama/2-SUMMARY.md

# Reference existing CLAUDE.md structure
@CLAUDE.md
</context>

<tasks>

<task type="auto">
  <name>Task 1: Add Git Workflow section to CLAUDE.md</name>
  <files>CLAUDE.md</files>
  <action>
Add a new "## Git Workflow" section to CLAUDE.md after the "## Testing" section.

The section should include:
1. Branch structure overview (main = production, development = active)
2. Workflow rules (3-4 bullet points from STATE.md)
3. Example commands for common operations:
   - Creating feature branches from development
   - Merging development to main for releases
   - Keeping branches synced

Reference the workflow documented in .planning/STATE.md (lines 20-38) and the summary from quick task #2.

Keep it concise and practical - focus on what developers need to know day-to-day.

Do NOT modify existing sections of CLAUDE.md - only add the new Git Workflow section.
  </action>
  <verify>
1. Read CLAUDE.md and verify "## Git Workflow" section exists
2. Run: grep -q "## Git Workflow" CLAUDE.md
3. Run: grep -q "main.*production" CLAUDE.md
4. Run: grep -q "development.*active" CLAUDE.md
  </verify>
  <done>
CLAUDE.md contains a complete Git Workflow section with:
- Branch structure explanation (main + development)
- Workflow rules (3-4 clear rules)
- Common git command examples
- Cross-reference to STATE.md for detailed documentation
  </done>
</task>

</tasks>

<verification>
Read CLAUDE.md and verify:
- [ ] Git Workflow section exists after Testing section
- [ ] Branch structure (main/development) is explained
- [ ] Workflow rules are clearly stated
- [ ] Example commands are provided
- [ ] No existing content was modified (only new section added)
</verification>

<success_criteria>
- [ ] CLAUDE.md contains "## Git Workflow" section
- [ ] Section documents main (production) and development (active) branches
- [ ] Workflow rules from STATE.md are included
- [ ] Practical examples for common operations provided
- [ ] STATE.md cross-reference included
</success_criteria>

<output>
After completion, create `.planning/quick/3-documentar-flujo-de-trabajo-de-ramas-git/3-SUMMARY.md`
</output>
