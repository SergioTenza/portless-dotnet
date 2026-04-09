# v4.1 VS Code Extension - Phase 1 (MVP)

## Goal
Create a VS Code extension that integrates with the Portless.NET proxy for route management, health monitoring, and quick access to `.localhost` URLs.

## Scope (Phase 1 MVP)
- Detect `portless.config.yaml` in workspace → activate extension
- Sidebar tree view showing routes with health status icons
- Status bar button to start/stop proxy
- Click route → open in browser
- Auto-refresh via polling `/api/v1/dashboard/routes`

**Out of scope** (Phase 2+): config validation, autocomplete, inspector webview, debug target auto-register

## Architecture

```
portless-vscode/
├── src/
│   ├── extension.ts          # Activation, deactivation, register commands
│   ├── portlessClient.ts     # HTTP client to proxy API (localhost:1355)
│   ├── routeProvider.ts      # TreeDataProvider for sidebar
│   ├── statusBar.ts          # Status bar item (proxy running/stopped)
│   └── types.ts              # TypeScript interfaces matching API responses
├── package.json              # Extension manifest (contributes, commands, views)
├── tsconfig.json
├── esbuild.js                # Build script
└── .vscodeignore
```

## API Endpoints Consumed
- `GET /api/v1/dashboard/routes` → route list with health
- `GET /api/v1/dashboard/summary` → proxy status (activeRoutes, uptime)
- `POST /api/v1/add-host` → add route
- `DELETE /api/v1/remove-host` → remove route

## Implementation Steps

### Step 1: Project scaffold
- Create `portless-vscode/` directory inside repo
- `npm init -y`, install dependencies
- `package.json` with extension manifest (activationEvents, contributes)
- `tsconfig.json` for ES modules
- `esbuild.js` bundler script

### Step 2: portlessClient.ts
- `PortlessClient` class
- `isRunning(): Promise<boolean>` — try GET /summary
- `getRoutes(): Promise<Route[]>` — GET /routes
- `getSummary(): Promise<Summary>` — GET /summary
- `startProxy(): Promise<void>` — spawn `portless proxy start` via child_process
- `stopProxy(): Promise<void>` — spawn `portless proxy stop`

### Step 3: routeProvider.ts
- Implements `vscode.TreeDataProvider<RouteItem>`
- RouteItem = tree item with icon based on health status
- Icons: ● green (healthy), ● yellow (degraded), ● red (unhealthy), ○ gray (unknown)
- Click → opens `http://hostname.localhost:1355` in browser
- Auto-refresh every 5s when proxy is running

### Step 4: statusBar.ts
- Status bar item: `$(globe) Portless: Running` / `$(circle-slash) Portless: Stopped`
- Click toggles proxy start/stop
- Updates on proxy state changes

### Step 5: extension.ts
- Activation: on workspace contains `portless.config.yaml`
- Register tree view, status bar, commands
- Deactivation: cleanup intervals, listeners

### Step 6: package.json manifest
- `activationEvents: ["workspaceContains:portless.config.yaml"]`
- Commands: `portless.startProxy`, `portless.stopProxy`, `portless.refreshRoutes`, `portless.openRoute`
- Views: `portless-routes` in explorer sidebar
- Configuration: `portless.proxyPort` (default 1355)

## Validation
- `npm run compile` builds without errors
- F5 launches extension in Extension Development Host
- Sidebar shows routes when proxy is running
- Status bar toggles proxy
- Click route opens browser

## Open Questions
- Separate npm package or monorepo subfolder? → Subfolder in repo for simplicity
- VS Code Marketplace publish now or later? → Later, after Phase 2
