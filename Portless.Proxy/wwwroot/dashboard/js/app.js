// Portless Dashboard - Main Application
const API_BASE = '/api/v1';

const App = {
    currentTab: 'overview',
    eventSource: null,

    async init() {
        this.setupTabs();
        this.startSSE();
        this.switchTab('overview');
        this.updateUptime();
        setInterval(() => this.updateUptime(), 60000);
    },

    setupTabs() {
        document.querySelectorAll('.tab').forEach(tab => {
            tab.addEventListener('click', () => {
                this.switchTab(tab.dataset.tab);
            });
        });
    },

    switchTab(tabName) {
        this.currentTab = tabName;
        document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
        document.querySelector(`[data-tab="${tabName}"]`).classList.add('active');
        
        const initFn = {
            overview: () => Overview.init(),
            routes: () => Routes.init(),
            inspector: () => Inspector.init(),
            metrics: () => Metrics.init(),
            plugins: () => Plugins.init(),
        };
        
        if (initFn[tabName]) initFn[tabName]();
    },

    async api(path) {
        const res = await fetch(`${API_BASE}${path}`);
        if (!res.ok) throw new Error(`API error: ${res.status}`);
        return res.json();
    },

    startSSE() {
        if (this.eventSource) this.eventSource.close();
        this.eventSource = new EventSource(`${API_BASE}/dashboard/events`);
        this.eventSource.onmessage = (e) => {
            const event = JSON.parse(e.data);
            this.handleEvent(event);
        };
        this.eventSource.onerror = () => {
            // Reconnect after 5s
            setTimeout(() => this.startSSE(), 5000);
        };
    },

    handleEvent(event) {
        switch (this.currentTab) {
            case 'overview': Overview.onEvent(event); break;
            case 'routes': Routes.onEvent(event); break;
            case 'inspector': Inspector.onEvent(event); break;
            case 'metrics': Metrics.onEvent(event); break;
        }
    },

    async updateUptime() {
        try {
            const status = await this.api('/status');
            document.getElementById('uptime').textContent = `Uptime: ${new Date(status.uptime).toLocaleTimeString()}`;
        } catch (e) {
            document.getElementById('uptime').textContent = 'Connecting...';
        }
    },

    formatDuration(ms) {
        if (ms < 1) return '<1ms';
        if (ms < 1000) return `${Math.round(ms)}ms`;
        return `${(ms / 1000).toFixed(2)}s`;
    },

    formatTime(dateStr) {
        return new Date(dateStr).toLocaleTimeString();
    },

    statusCodeClass(code) {
        if (code >= 200 && code < 300) return 'stream-status-2xx';
        if (code >= 300 && code < 400) return 'stream-status-3xx';
        if (code >= 400 && code < 500) return 'stream-status-4xx';
        return 'stream-status-5xx';
    }
};

document.addEventListener('DOMContentLoaded', () => App.init());
