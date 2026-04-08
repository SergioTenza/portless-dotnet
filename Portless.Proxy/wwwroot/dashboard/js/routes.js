// Routes Tab
const Routes = {
    initialized: false,

    async init() {
        const content = document.getElementById('content');
        content.innerHTML = `
            <div class="toolbar">
                <button class="btn" onclick="Routes.refresh()">↻ Refresh</button>
            </div>
            <table>
                <thead>
                    <tr>
                        <th>Health</th>
                        <th>Hostname</th>
                        <th>Backend</th>
                        <th>Port</th>
                        <th>Created</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody id="routes-body"></tbody>
            </table>
        `;
        await this.refresh();
        this.initialized = true;
    },

    async refresh() {
        try {
            const routes = await App.api('/dashboard/routes');
            this.renderRoutes(routes);
        } catch (e) {
            document.getElementById('routes-body').innerHTML = 
                '<tr><td colspan="6" style="text-align:center;color:var(--text-secondary)">Failed to load routes</td></tr>';
        }
    },

    renderRoutes(routes) {
        const body = document.getElementById('routes-body');
        if (!routes || routes.length === 0) {
            body.innerHTML = '<tr><td colspan="6" style="text-align:center;color:var(--text-secondary)">No active routes</td></tr>';
            return;
        }
        
        body.innerHTML = routes.map(r => `
            <tr>
                <td><span class="health-dot ${r.health || 'unknown'}"></span>${r.health || 'unknown'}</td>
                <td><strong>${r.hostname}</strong></td>
                <td>${(r.backends || []).join(', ')}</td>
                <td>${r.port}</td>
                <td>${r.createdAt ? App.formatTime(r.createdAt) : '--'}</td>
                <td><button class="btn btn-danger" onclick="Routes.remove('${r.hostname}')">Remove</button></td>
            </tr>
        `).join('');
    },

    async remove(hostname) {
        if (!confirm(`Remove route "${hostname}"?`)) return;
        try {
            await fetch(`/api/v1/remove-host?hostname=${encodeURIComponent(hostname)}`, { method: 'DELETE' });
            this.refresh();
        } catch (e) {
            alert('Failed to remove route');
        }
    },

    onEvent(event) {
        if (event.type === 'route.added' || event.type === 'route.removed') {
            this.refresh();
        }
    }
};
