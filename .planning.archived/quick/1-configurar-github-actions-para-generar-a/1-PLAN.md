---
phase: quick
plan: 1
type: execute
wave: 1
depends_on: []
files_modified: [.github/workflows/release.yml]
autonomous: true
requirements: []
user_setup: []

must_haves:
  truths:
    - "GitHub Actions workflow triggers on git tags matching v*.*.* pattern"
    - "Workflow builds cross-platform binaries (win-x64, linux-x64, osx-x64, osx-arm64)"
    - "Published executables are attached to GitHub releases as downloadable artifacts"
    - "Build process uses Native AOT for single binary deployment"
    - "Workflow runs tests before creating release artifacts"
  artifacts:
    - path: ".github/workflows/release.yml"
      provides: "GitHub Actions workflow for automated releases"
      contains: "on: push: tags: - 'v*.*.*'"
    - path: "Portless.Cli/bin/Release/net10.0/*/publish/portless"
      provides: "Cross-platform native executable"
      min_lines: 1
  key_links:
    - from: ".github/workflows/release.yml"
      to: "Portless.Cli/Portless.Cli.csproj"
      via: "dotnet publish command"
      pattern: "dotnet publish.*Portless.Cli"
    - from: ".github/workflows/release.yml"
      to: "GitHub Releases"
      via: "actions/upload-release-asset action"
      pattern: "upload-release-asset"
---

<objective>
Configure GitHub Actions to automatically build and publish cross-platform release binaries when version tags are pushed.

Purpose: Enable automated distribution of Portless.NET as downloadable executables for Windows, Linux, and macOS (both Intel and Apple Silicon).
Output: GitHub Actions workflow that builds, tests, and publishes Native AOT binaries to GitHub Releases.
</objective>

<execution_context>
@C:/Users/serge/.claude/get-shit-done/workflows/execute-plan.md
@C:/Users/serge/.claude/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/STATE.md
@.planning/PROJECT.md
@./CLAUDE.md
@./Portless.Cli/Portless.Cli.csproj
</context>

<tasks>

<task type="auto">
  <name>Task 1: Create GitHub Actions release workflow</name>
  <files>.github/workflows/release.yml</files>
  <action>
Create GitHub Actions workflow file at `.github/workflows/release.yml` with:

**Trigger configuration:**
- `on: push: tags: - 'v*.*.*'` (triggers on version tags like v1.0.0)

**Build matrix:**
- Four OS targets: win-x64, linux-x64, osx-x64, osx-arm64
- Each target publishes self-contained Native AOT binary

**Job steps:**
1. Checkout code (actions/checkout@v4)
2. Setup .NET 10 SDK (actions/setup-dotnet@v4)
3. Run tests: `dotnet test Portless.slnx --configuration Release`
4. Publish Native AOT executable:
   ```
   dotnet publish Portless.Cli/Portless.Cli.csproj \
     --configuration Release \
     --runtime ${{ matrix.runtime }} \
     --self-contained true \
     -p:PublishAot=true \
     -o publish/${{ matrix.runtime }}
   ```
5. Upload to GitHub release using softprops/action-gh-release@v2:
   - Windows: `portless.exe` (win-x64)
   - Linux/macOS: `portless` (linux-x64, osx-x64, osx-arm64)

**Do NOT include:**
- Automatic version incrementing (version controlled via git tags)
- NuGet package publishing (out of scope for this quick plan)
- Docker image builds (out of scope)

**Rationale:** Native AOT with --self-contained produces single binary per platform, no .NET runtime required on user machine.
  </action>
  <verify>
Workflow file exists at `.github/workflows/release.yml` and contains:
- `on: push: tags: - 'v*.*.*'` trigger
- Build matrix with 4 runtime targets
- Test step before publish
- dotnet publish with PublishAot=true
- Upload to GitHub release step
  </verify>
  <done>
Pushing a git tag like `git tag v1.0.1 && git push origin v1.0.1` triggers the workflow, which runs tests and publishes 4 cross-platform binaries to the GitHub Release page.
  </done>
</task>

</tasks>

<verification>
- Workflow YAML syntax is valid (can be validated with `yamllint` or similar)
- All required runtimes are specified in the matrix
- Upload step uses correct binary names for each platform (portless.exe on Windows, portless on Unix)
- Tests run before publish to prevent broken releases
</verification>

<success_criteria>
- Pushing a version tag triggers GitHub Actions workflow
- Workflow successfully builds Portless.Cli for all 4 platforms
- Test suite passes before release artifacts are created
- GitHub Release includes 4 downloadable binaries (Windows x64, Linux x64, macOS x64, macOS ARM64)
- Each binary is a standalone executable that doesn't require .NET runtime installation
</success_criteria>

<output>
After completion, create `.planning/quick/1-configurar-github-actions-para-generar-a/1-SUMMARY.md`
</output>
