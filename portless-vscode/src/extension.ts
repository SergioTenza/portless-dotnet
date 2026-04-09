import * as vscode from 'vscode';
import { PortlessClient } from './portlessClient';
import { RouteProvider, RouteItem } from './routeProvider';
import { PortlessStatusBar } from './statusBar';

let pollInterval: NodeJS.Timeout | undefined;
let client: PortlessClient;
let routeProvider: RouteProvider;
let statusBar: PortlessStatusBar;

export async function activate(context: vscode.ExtensionContext): Promise<void> {
    const proxyPort = vscode.workspace.getConfiguration('portless').get<number>('proxyPort', 1355);

    client = new PortlessClient();
    routeProvider = new RouteProvider(client);
    statusBar = new PortlessStatusBar(proxyPort);

    // Register tree view
    const treeView = vscode.window.createTreeView('portless-routes', {
        treeDataProvider: routeProvider,
        showCollapseAll: false,
    });
    context.subscriptions.push(treeView);

    // Register commands
    context.subscriptions.push(
        vscode.commands.registerCommand('portless.startProxy', async () => {
            try {
                await vscode.window.withProgress(
                    { location: vscode.ProgressLocation.Notification, title: 'Starting Portless proxy...' },
                    async () => {
                        await client.startProxy();
                    }
                );
                // Wait a moment for the proxy to start, then refresh
                setTimeout(async () => {
                    await refreshAll();
                }, 2000);
                vscode.window.showInformationMessage('Portless proxy started.');
            } catch (err) {
                vscode.window.showErrorMessage(`Failed to start proxy: ${err}`);
            }
        })
    );

    context.subscriptions.push(
        vscode.commands.registerCommand('portless.stopProxy', async () => {
            try {
                await vscode.window.withProgress(
                    { location: vscode.ProgressLocation.Notification, title: 'Stopping Portless proxy...' },
                    async () => {
                        await client.stopProxy();
                    }
                );
                await refreshAll();
                vscode.window.showInformationMessage('Portless proxy stopped.');
            } catch (err) {
                vscode.window.showErrorMessage(`Failed to stop proxy: ${err}`);
            }
        })
    );

    context.subscriptions.push(
        vscode.commands.registerCommand('portless.refreshRoutes', async () => {
            await refreshAll();
        })
    );

    context.subscriptions.push(
        vscode.commands.registerCommand('portless.openRoute', async (item?: RouteItem) => {
            if (!item || !item.routeInfo) {
                vscode.window.showWarningMessage('No route selected.');
                return;
            }
            const hostname = item.routeInfo.hostname;
            const port = vscode.workspace.getConfiguration('portless').get<number>('proxyPort', 1355);
            const url = `http://${hostname}:${port}`;
            vscode.env.openExternal(vscode.Uri.parse(url));
        })
    );

    // Listen for configuration changes
    context.subscriptions.push(
        vscode.workspace.onDidChangeConfiguration((e) => {
            if (e.affectsConfiguration('portless.proxyPort')) {
                const newPort = vscode.workspace.getConfiguration('portless').get<number>('proxyPort', 1355);
                statusBar.setProxyPort(newPort);
                refreshAll();
            }
        })
    );

    context.subscriptions.push(statusBar);

    // Initial state check
    await refreshAll();

    // Set up polling interval (5s)
    pollInterval = setInterval(async () => {
        await refreshAll();
    }, 5000);
}

async function refreshAll(): Promise<void> {
    try {
        const running = await client.isRunning();
        statusBar.update(running);
        routeProvider.refresh();
    } catch {
        statusBar.update(false);
    }
}

export function deactivate(): void {
    if (pollInterval) {
        clearInterval(pollInterval);
        pollInterval = undefined;
    }
}
