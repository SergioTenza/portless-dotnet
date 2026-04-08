// Overview Tab
const Overview = {
    initialized: false,

    async init() {
        const content = document.getElementById('content');
        content.innerHTML = `
            <div class="cards" id="overview-cards"></div>
            <div class="events-feed">
                <div style="padding:10px 16px;font-size:12px;color:var(--text-secondary);border-bottom:1px solid var(--border);font-weight:600;">RECENT EVENTS</div>
                <div id="events-list"></div>
            </div>
        `;
        await this.refresh();
        this.initialized = true;
    },

    async refresh() {
        try {
            const [summary, routes] = await Promise.all([
                App.api('/dashboard/summary'),
                App.api('/dashboard/routes')
            ]);
            this.renderCards(summary);
        } catch (e) {
            this.renderCards({
                activeRoutes: 0, uptime: '--', totalRequests: 0,
                errorRate: 0, requestsPerMinute: 0
            });
        }
    },

    renderCards(s) {
        document.getElementById('overview-cards').innerHTML = `
            <div class="card">
                <div class="card-label">Active Routes</div>
                <div class="card-value" style="color:var(--accent)">${s.activeRoutes ?? 0}</div>
            </div>
            <div class="card">
                <div class="card-label">Requests/min</div>
                <div class="card-value">${(s.requestsPerMinute ?? 0).toFixed(1)}</div>
            </div>
            <div class="card">
                <div class="card-label">Avg Response Time</div>
                <div class="card-value">${App.formatDuration(s.avgDurationMs ?? 0)}</div>
            </div>
            <div class="card">
                <div class="card-label">Error Rate</div>
                <div class="card-value" style="color:${(s.errorRate ?? 0) > 0.05 ? 'var(--red)' : 'var(--green)'}">${((s.errorRate ?? 0) * 100).toFixed(1)}%</div>
            </div>
            <div class="card">
                <div class="card-label">Total Captured</div>
                <div class="card-value">${s.totalCaptured ?? 0}</div>
            </div>
        `;
    },

    onEvent(event) {
        if (event.type === 'request.completed') {
            this.refresh();
        }
        this.addEvent(event);
    },

    addEvent(event) {
        const list = document.getElementById('events-list');
        if (!list) return;
        
        const item = document.createElement('div');
        item.className = 'event-item';
        item.innerHTML = `
            <span class="event-time">${new Date().toLocaleTimeString()}</span>
            <span class="event-type" style="color:${event.type === 'health.changed' ? 'var(--yellow)' : 'var(--accent)'}">${event.type}</span>
            <span>${event.message || ''}</span>
        `;
        list.insertBefore(item, list.firstChild);
        
        // Keep max 50 events
        while (list.children.length > 50) list.removeChild(list.lastChild);
    }
};
