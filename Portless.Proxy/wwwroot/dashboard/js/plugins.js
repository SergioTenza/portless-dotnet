// Plugins Tab
const Plugins = {
    initialized: false,

    async init() {
        const content = document.getElementById('content');
        content.innerHTML = `
            <div class="toolbar">
                <button class="btn" onclick="Plugins.reload()">↻ Reload All</button>
            </div>
            <div class="plugin-grid" id="plugins-grid"></div>
        `;
        await this.refresh();
        this.initialized = true;
    },

    async refresh() {
        try {
            const plugins = await App.api('/plugins');
            this.renderPlugins(plugins);
        } catch (e) {
            document.getElementById('plugins-grid').innerHTML = 
                '<p style="color:var(--text-secondary)">Failed to load plugins</p>';
        }
    },

    renderPlugins(plugins) {
        const grid = document.getElementById('plugins-grid');
        if (!plugins || plugins.length === 0) {
            grid.innerHTML = '<p style="color:var(--text-secondary)">No plugins loaded</p>';
            return;
        }
        
        grid.innerHTML = plugins.map(p => `
            <div class="plugin-card">
                <div class="plugin-info">
                    <h3>${p.name}</h3>
                    <p>v${p.version} · <span class="badge badge-${p.status === 'enabled' ? 'green' : 'yellow'}">${p.status}</span></p>
                </div>
            </div>
        `).join('');
    },

    async reload() {
        try {
            await fetch('/api/v1/plugins/reload', { method: 'POST' });
            this.refresh();
        } catch(e) {
            alert('Failed to reload plugins');
        }
    },

    onEvent(event) {}
};
