// Metrics Tab
const Metrics = {
    charts: {},
    initialized: false,

    async init() {
        const content = document.getElementById('content');
        content.innerHTML = `
            <div class="toolbar">
                <button class="btn" onclick="Metrics.refresh()">↻ Refresh</button>
            </div>
            <div class="charts-grid">
                <div class="chart-card">
                    <h3>Requests per Minute</h3>
                    <canvas id="chart-rpm"></canvas>
                </div>
                <div class="chart-card">
                    <h3>Response Time Distribution</h3>
                    <canvas id="chart-rt"></canvas>
                </div>
                <div class="chart-card">
                    <h3>Status Code Breakdown</h3>
                    <canvas id="chart-status"></canvas>
                </div>
                <div class="chart-card">
                    <h3>Top Routes by Traffic</h3>
                    <canvas id="chart-routes"></canvas>
                </div>
            </div>
        `;
        
        // Load Chart.js from CDN
        if (!window.Chart) {
            const script = document.createElement('script');
            script.src = 'https://cdn.jsdelivr.net/npm/chart.js@4/dist/chart.umd.min.js';
            script.onload = () => this.refresh();
            document.head.appendChild(script);
        } else {
            await this.refresh();
        }
        this.initialized = true;
    },

    async refresh() {
        try {
            const [stats, sessions] = await Promise.all([
                App.api('/inspect/stats'),
                App.api('/inspect/sessions?count=500')
            ]);
            this.renderCharts(stats, sessions);
        } catch(e) {
            console.error('Failed to load metrics:', e);
        }
    },

    renderCharts(stats, sessions) {
        if (!window.Chart) return;
        const defaults = {
            color: '#8b949e',
            borderColor: '#30363d',
        };
        Chart.defaults.color = defaults.color;
        Chart.defaults.borderColor = defaults.borderColor;

        // Requests per minute over time
        this.renderRPMChart(sessions);
        // Response time distribution
        this.renderRTChart(sessions);
        // Status code breakdown
        this.renderStatusChart(sessions);
        // Top routes
        this.renderRoutesChart(sessions);
    },

    renderRPMChart(sessions) {
        const ctx = document.getElementById('chart-rpm');
        if (!ctx) return;
        if (this.charts.rpm) this.charts.rpm.destroy();

        // Group sessions by minute
        const byMinute = {};
        sessions.forEach(s => {
            const d = new Date(s.timestamp);
            const key = `${d.getHours()}:${String(d.getMinutes()).padStart(2,'0')}`;
            byMinute[key] = (byMinute[key] || 0) + 1;
        });

        const labels = Object.keys(byMinute).slice(-30);
        const data = labels.map(l => byMinute[l]);

        this.charts.rpm = new Chart(ctx, {
            type: 'line',
            data: {
                labels,
                datasets: [{
                    label: 'Req/min',
                    data,
                    borderColor: '#58a6ff',
                    backgroundColor: 'rgba(88,166,255,0.1)',
                    fill: true,
                    tension: 0.3
                }]
            },
            options: { responsive: true, plugins: { legend: { display: false } } }
        });
    },

    renderRTChart(sessions) {
        const ctx = document.getElementById('chart-rt');
        if (!ctx) return;
        if (this.charts.rt) this.charts.rt.destroy();

        const buckets = { '<10ms': 0, '10-50ms': 0, '50-100ms': 0, '100-500ms': 0, '>500ms': 0 };
        sessions.forEach(s => {
            const ms = s.durationMs || 0;
            if (ms < 10) buckets['<10ms']++;
            else if (ms < 50) buckets['10-50ms']++;
            else if (ms < 100) buckets['50-100ms']++;
            else if (ms < 500) buckets['100-500ms']++;
            else buckets['>500ms']++;
        });

        this.charts.rt = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: Object.keys(buckets),
                datasets: [{
                    label: 'Requests',
                    data: Object.values(buckets),
                    backgroundColor: ['#3fb950', '#58a6ff', '#d29922', '#f85149', '#bc8cff']
                }]
            },
            options: { responsive: true, plugins: { legend: { display: false } } }
        });
    },

    renderStatusChart(sessions) {
        const ctx = document.getElementById('chart-status');
        if (!ctx) return;
        if (this.charts.status) this.charts.status.destroy();

        const codes = {};
        sessions.forEach(s => {
            const code = s.statusCode || 0;
            const group = `${Math.floor(code / 100)}xx`;
            codes[group] = (codes[group] || 0) + 1;
        });

        const colors = { '2xx': '#3fb950', '3xx': '#58a6ff', '4xx': '#d29922', '5xx': '#f85149' };
        
        this.charts.status = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: Object.keys(codes),
                datasets: [{
                    data: Object.values(codes),
                    backgroundColor: Object.keys(codes).map(k => colors[k] || '#8b949e')
                }]
            },
            options: { responsive: true }
        });
    },

    renderRoutesChart(sessions) {
        const ctx = document.getElementById('chart-routes');
        if (!ctx) return;
        if (this.charts.routes) this.charts.routes.destroy();

        const byRoute = {};
        sessions.forEach(s => {
            const host = s.hostname || 'unknown';
            byRoute[host] = (byRoute[host] || 0) + 1;
        });

        const sorted = Object.entries(byRoute).sort((a, b) => b[1] - a[1]).slice(0, 10);
        
        this.charts.routes = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: sorted.map(s => s[0]),
                datasets: [{
                    label: 'Requests',
                    data: sorted.map(s => s[1]),
                    backgroundColor: '#bc8cff'
                }]
            },
            options: { responsive: true, indexAxis: 'y', plugins: { legend: { display: false } } }
        });
    },

    onEvent(event) {
        if (event.type === 'request.completed') {
            // Refresh charts every 30 seconds max
            if (!this._lastRefresh || Date.now() - this._lastRefresh > 30000) {
                this._lastRefresh = Date.now();
                this.refresh();
            }
        }
    }
};
