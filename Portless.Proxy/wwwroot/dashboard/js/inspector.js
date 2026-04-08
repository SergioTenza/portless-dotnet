// Inspector Tab
const Inspector = {
    ws: null,
    paused: false,
    sessions: [],
    filterHost: '',
    filterMethod: '',
    filterStatus: '',

    async init() {
        const content = document.getElementById('content');
        content.innerHTML = `
            <div class="toolbar">
                <input type="text" id="filter-host" placeholder="Filter host..." oninput="Inspector.filterHost=this.value;Inspector.renderStream()">
                <select id="filter-method" onchange="Inspector.filterMethod=this.value;Inspector.renderStream()">
                    <option value="">All methods</option>
                    <option value="GET">GET</option>
                    <option value="POST">POST</option>
                    <option value="PUT">PUT</option>
                    <option value="DELETE">DELETE</option>
                    <option value="PATCH">PATCH</option>
                </select>
                <select id="filter-status" onchange="Inspector.filterStatus=this.value;Inspector.renderStream()">
                    <option value="">All statuses</option>
                    <option value="2xx">2xx</option>
                    <option value="3xx">3xx</option>
                    <option value="4xx">4xx</option>
                    <option value="5xx">5xx</option>
                </select>
                <button class="btn" onclick="Inspector.togglePause()">${this.paused ? '▶ Resume' : '⏸ Pause'}</button>
                <button class="btn btn-danger" onclick="Inspector.clear()">✕ Clear</button>
            </div>
            <div class="stream-container">
                <div class="stream-row" style="background:var(--bg-tertiary);font-weight:600;color:var(--text-secondary)">
                    <span>Time</span><span>Method</span><span>Host + Path</span><span>Status</span><span>Duration</span>
                </div>
                <div id="stream-body"></div>
            </div>
        `;
        await this.loadHistory();
        this.connectWS();
    },

    async loadHistory() {
        try {
            this.sessions = await App.api('/inspect/sessions?count=200');
            this.renderStream();
        } catch (e) {
            // Inspector may not be enabled
        }
    },

    connectWS() {
        if (this.ws) { try { this.ws.close(); } catch(e) {} }
        const proto = location.protocol === 'https:' ? 'wss:' : 'ws:';
        this.ws = new WebSocket(`${proto}//${location.host}/api/v1/inspect/stream`);
        this.ws.onmessage = (e) => {
            if (this.paused) return;
            const msg = JSON.parse(e.data);
            if (msg.type === 'request' && msg.data) {
                this.sessions.unshift(msg.data);
                if (this.sessions.length > 500) this.sessions.pop();
                this.renderStream();
            }
        };
        this.ws.onerror = () => {
            // Fall back to polling
            setTimeout(() => this.pollFallback(), 3000);
        };
    },

    async pollFallback() {
        if (this.paused) return;
        try {
            const recent = await App.api('/inspect/sessions?count=20');
            if (recent.length > 0 && (this.sessions.length === 0 || recent[0].id !== this.sessions[0]?.id)) {
                this.sessions = recent;
                this.renderStream();
            }
        } catch(e) {}
        setTimeout(() => this.pollFallback(), 2000);
    },

    renderStream() {
        const body = document.getElementById('stream-body');
        if (!body) return;
        
        const filtered = this.sessions.filter(s => {
            if (this.filterHost && !(s.hostname || '').toLowerCase().includes(this.filterHost.toLowerCase())) return false;
            if (this.filterMethod && s.method !== this.filterMethod) return false;
            if (this.filterStatus) {
                const prefix = this.filterStatus.charAt(0);
                if (String(s.statusCode).charAt(0) !== prefix) return false;
            }
            return true;
        });

        body.innerHTML = filtered.slice(0, 200).map(s => `
            <div class="stream-row" onclick="Inspector.showDetail('${s.id}')">
                <span style="color:var(--text-secondary)">${s.timestamp ? new Date(s.timestamp).toLocaleTimeString() : '--'}</span>
                <span class="stream-method">${s.method || '?'}</span>
                <span>${s.hostname || '?'}${s.path || '/'}</span>
                <span class="${App.statusCodeClass(s.statusCode)}">${s.statusCode || '?'}</span>
                <span style="color:var(--text-secondary)">${App.formatDuration(s.durationMs || 0)}</span>
            </div>
        `).join('');
    },

    async showDetail(id) {
        try {
            const detail = await App.api(`/inspect/sessions/${id}`);
            alert(JSON.stringify(detail, null, 2));
        } catch(e) {
            alert('Could not load request detail');
        }
    },

    togglePause() {
        this.paused = !this.paused;
        const btn = document.querySelector('#content .toolbar .btn');
        if (btn) btn.textContent = this.paused ? '▶ Resume' : '⏸ Pause';
    },

    async clear() {
        try { await fetch('/api/v1/inspect/sessions', { method: 'DELETE' }); } catch(e) {}
        this.sessions = [];
        this.renderStream();
    },

    onEvent(event) {
        if (event.type === 'request.completed' && !this.paused) {
            this.loadHistory();
        }
    }
};
