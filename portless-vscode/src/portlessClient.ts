import * as vscode from 'vscode';
import * as http from 'http';
import { execFile } from 'child_process';
import { RouteInfo, DashboardSummary } from './types';

export class PortlessClient {
    private get proxyPort(): number {
        return vscode.workspace.getConfiguration('portless').get<number>('proxyPort', 1355);
    }

    private get baseUrl(): string {
        return `http://localhost:${this.proxyPort}`;
    }

    private httpGet<T>(path: string): Promise<T> {
        return new Promise((resolve, reject) => {
            const url = `${this.baseUrl}${path}`;
            const req = http.get(url, { timeout: 3000 }, (res) => {
                if (res.statusCode !== 200) {
                    reject(new Error(`HTTP ${res.statusCode}`));
                    return;
                }
                let data = '';
                res.on('data', (chunk: Buffer | string) => {
                    data += chunk.toString();
                });
                res.on('end', () => {
                    try {
                        resolve(JSON.parse(data) as T);
                    } catch (e) {
                        reject(new Error(`Invalid JSON: ${e}`));
                    }
                });
            });
            req.on('error', (err: Error) => reject(err));
            req.on('timeout', () => {
                req.destroy();
                reject(new Error('Request timeout'));
            });
        });
    }

    public async isRunning(): Promise<boolean> {
        try {
            await this.getSummary();
            return true;
        } catch {
            return false;
        }
    }

    public async getRoutes(): Promise<RouteInfo[]> {
        try {
            const routes = await this.httpGet<RouteInfo[]>('/api/v1/dashboard/routes');
            return routes;
        } catch {
            return [];
        }
    }

    public async getSummary(): Promise<DashboardSummary> {
        return this.httpGet<DashboardSummary>('/api/v1/dashboard/summary');
    }

    public startProxy(): Promise<void> {
        return new Promise((resolve, reject) => {
            const child = execFile('portless', ['proxy', 'start'], (error, _stdout, _stderr) => {
                if (error) {
                    reject(error);
                    return;
                }
                resolve();
            });
            child.unref();
        });
    }

    public stopProxy(): Promise<void> {
        return new Promise((resolve, reject) => {
            execFile('portless', ['proxy', 'stop'], (error, _stdout, _stderr) => {
                if (error) {
                    reject(error);
                    return;
                }
                resolve();
            });
        });
    }
}
