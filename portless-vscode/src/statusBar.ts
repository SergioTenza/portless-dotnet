import * as vscode from 'vscode';

export class PortlessStatusBar {
    private statusBarItem: vscode.StatusBarItem;
    private proxyPort: number;

    constructor(proxyPort: number) {
        this.proxyPort = proxyPort;
        this.statusBarItem = vscode.window.createStatusBarItem(
            vscode.StatusBarAlignment.Left,
            100
        );
        this.update(false);
        this.statusBarItem.show();
    }

    public update(isRunning: boolean): void {
        if (isRunning) {
            this.statusBarItem.text = '$(globe) Portless';
            this.statusBarItem.tooltip = `Portless proxy running on port ${this.proxyPort}`;
            this.statusBarItem.command = 'portless.stopProxy';
        } else {
            this.statusBarItem.text = '$(circle-slash) Portless';
            this.statusBarItem.tooltip = 'Portless proxy stopped';
            this.statusBarItem.command = 'portless.startProxy';
        }
    }

    public setProxyPort(port: number): void {
        this.proxyPort = port;
    }

    public dispose(): void {
        this.statusBarItem.dispose();
    }
}
