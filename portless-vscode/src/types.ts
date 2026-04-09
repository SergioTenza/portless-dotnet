export interface RouteInfo {
    hostname: string;
    port: number;
    pid?: number;
    path?: string;
    type: string;
    backends?: string[];
    health?: 'healthy' | 'degraded' | 'unhealthy' | 'unknown';
    createdAt?: string;
    lastSeen?: string;
}

export interface DashboardSummary {
    activeRoutes: number;
    uptime: string;
    totalCaptured: number;
    avgDurationMs: number;
    errorRate: number;
    requestsPerMinute: number;
}
