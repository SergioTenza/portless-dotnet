import * as vscode from 'vscode';
import { RouteInfo } from './types';
import { PortlessClient } from './portlessClient';

export class RouteItem extends vscode.TreeItem {
    constructor(public readonly routeInfo: RouteInfo) {
        super(routeInfo.hostname, vscode.TreeItemCollapsibleState.None);

        this.contextValue = 'route';
        this.description = `:${routeInfo.port}`;

        // Set icon based on health status
        this.iconPath = this.getHealthIcon(routeInfo.health);

        // Build tooltip
        const lines: string[] = [
            `Hostname: ${routeInfo.hostname}`,
            `Port: ${routeInfo.port}`,
            `Type: ${routeInfo.type}`,
        ];
        if (routeInfo.health) {
            lines.push(`Health: ${routeInfo.health}`);
        }
        if (routeInfo.backends && routeInfo.backends.length > 0) {
            lines.push(`Backends: ${routeInfo.backends.join(', ')}`);
        }
        if (routeInfo.pid) {
            lines.push(`PID: ${routeInfo.pid}`);
        }
        if (routeInfo.path) {
            lines.push(`Path: ${routeInfo.path}`);
        }
        this.tooltip = lines.join('\n');

        // Command to open route on click
        this.command = {
            command: 'portless.openRoute',
            title: 'Open Route',
            arguments: [this],
        };
    }

    private getHealthIcon(health?: string): vscode.ThemeIcon | vscode.Uri {
        switch (health) {
            case 'healthy':
                return new vscode.ThemeIcon('circle-filled', new vscode.ThemeColor('charts.green'));
            case 'degraded':
                return new vscode.ThemeIcon('circle-filled', new vscode.ThemeColor('charts.yellow'));
            case 'unhealthy':
                return new vscode.ThemeIcon('circle-filled', new vscode.ThemeColor('charts.red'));
            default:
                return new vscode.ThemeIcon('circle-outline', new vscode.ThemeColor('disabledForeground'));
        }
    }
}

export class RouteProvider implements vscode.TreeDataProvider<RouteItem> {
    private _onDidChangeTreeData = new vscode.EventEmitter<RouteItem | undefined | null>();
    readonly onDidChangeTreeData = this._onDidChangeTreeData.event;

    private routes: RouteItem[] = [];

    constructor(private client: PortlessClient) {}

    public refresh(): void {
        this._onDidChangeTreeData.fire(undefined);
    }

    public getTreeItem(element: RouteItem): vscode.TreeItem {
        return element;
    }

    public getChildren(_element?: RouteItem): Thenable<RouteItem[]> {
        if (_element) {
            return Promise.resolve([]);
        }
        return this.client.getRoutes().then((routes) => {
            this.routes = routes.map((r) => new RouteItem(r));
            return this.routes;
        });
    }
}
