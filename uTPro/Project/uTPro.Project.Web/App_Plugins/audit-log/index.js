import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css } from '@umbraco-cms/backoffice/external/lit';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';

const API_BASE = '/umbraco/management/api/v1/utpro/audit-log';

export class UtproAuditLogDashboard extends UmbLitElement {

    static properties = {
        _activeTab: { type: String, state: true },
        _loading: { type: Boolean, state: true },
        _items: { type: Array, state: true },
        _total: { type: Number, state: true },
        _skip: { type: Number, state: true },
        _take: { type: Number, state: true },
        _eventTypes: { type: Array, state: true },
        _logHeaders: { type: Array, state: true },
        _filterEventType: { type: String, state: true },
        _filterSearch: { type: String, state: true },
        _filterDateFrom: { type: String, state: true },
        _filterDateTo: { type: String, state: true },
    };

    #authContext;

    constructor() {
        super();
        this._activeTab = 'audit';
        this._loading = false;
        this._items = [];
        this._total = 0;
        this._skip = 0;
        this._take = 20;
        this._eventTypes = [];
        this._logHeaders = [];
        this._filterEventType = '';
        this._filterSearch = '';
        this._filterDateFrom = '';
        this._filterDateTo = '';
        this.consumeContext(UMB_AUTH_CONTEXT, (ctx) => { this.#authContext = ctx; });
    }

    async connectedCallback() {
        super.connectedCallback();
        await this._loadEventTypes();
        await this._loadData();
    }

    async _fetchApi(url) {
        const config = this.#authContext?.getOpenApiConfiguration();
        const headers = { 'Content-Type': 'application/json' };
        if (config?.token) {
            const token = await config.token();
            if (token) headers['Authorization'] = 'Bearer ' + token;
        }
        const resp = await fetch(url, { headers, credentials: config?.credentials || 'same-origin' });
        if (!resp.ok) throw new Error('API error: ' + resp.status);
        return resp.json();
    }

    async _loadEventTypes() {
        try {
            const [types, headers] = await Promise.all([
                this._fetchApi(API_BASE + '/event-types'),
                this._fetchApi(API_BASE + '/log-headers')
            ]);
            this._eventTypes = types || [];
            this._logHeaders = headers || [];
        } catch (e) { console.error('Failed to load filter options', e); }
    }

    async _loadData() {
        this._loading = true;
        try {
            const endpoint = this._activeTab === 'audit' ? 'audit-entries' : 'log-entries';
            const params = new URLSearchParams({ skip: this._skip, take: this._take });
            if (this._filterEventType) params.set('eventType', this._filterEventType);
            if (this._filterSearch) params.set('searchTerm', this._filterSearch);
            if (this._filterDateFrom) params.set('dateFrom', this._filterDateFrom);
            if (this._filterDateTo) params.set('dateTo', this._filterDateTo);
            const data = await this._fetchApi(API_BASE + '/' + endpoint + '?' + params);
            this._items = data.items || [];
            this._total = data.total || 0;
        } catch (e) {
            console.error('Failed to load audit data', e);
            this._items = [];
            this._total = 0;
        }
        this._loading = false;
    }

    _switchTab(tab) {
        this._activeTab = tab;
        this._skip = 0;
        this._filterEventType = '';
        this._filterSearch = '';
        this._filterDateFrom = '';
        this._filterDateTo = '';
        this._loadData();
    }

    _applyFilter() { this._skip = 0; this._loadData(); }

    _resetFilter() {
        this._filterEventType = '';
        this._filterSearch = '';
        this._filterDateFrom = '';
        this._filterDateTo = '';
        this._skip = 0;
        this._loadData();
    }

    _prevPage() { if (this._skip > 0) { this._skip = Math.max(0, this._skip - this._take); this._loadData(); } }
    _nextPage() { if (this._skip + this._take < this._total) { this._skip += this._take; this._loadData(); } }

    get _currentPage() { return Math.floor(this._skip / this._take) + 1; }
    get _totalPages() { return Math.max(1, Math.ceil(this._total / this._take)); }

    _formatDate(d) { return d ? new Date(d).toLocaleString() : ''; }

    render() {
        const filterOptions = this._activeTab === 'audit' ? this._eventTypes : this._logHeaders;
        return html`
            <uui-box>
                <div class="header">
                    <h2>Audit Log</h2>
                    <div class="tabs">
                        <uui-button look=${this._activeTab === 'audit' ? 'primary' : 'secondary'}
                            @click=${() => this._switchTab('audit')}>Audit Trail</uui-button>
                        <uui-button look=${this._activeTab === 'log' ? 'primary' : 'secondary'}
                            @click=${() => this._switchTab('log')}>Content Logs</uui-button>
                    </div>
                </div>
                <div class="filters">
                    <uui-input placeholder="Search..." .value=${this._filterSearch}
                        @input=${(e) => { this._filterSearch = e.target.value; }}></uui-input>
                    <select class="filter-select" @change=${(e) => { this._filterEventType = e.target.value; }}>
                        <option value="">All Types</option>
                        ${filterOptions.map(t => html`<option value=${t} ?selected=${this._filterEventType === t}>${t}</option>`)}
                    </select>
                    <input type="date" class="filter-date" .value=${this._filterDateFrom}
                        @change=${(e) => { this._filterDateFrom = e.target.value; }} />
                    <input type="date" class="filter-date" .value=${this._filterDateTo}
                        @change=${(e) => { this._filterDateTo = e.target.value; }} />
                    <uui-button look="primary" @click=${() => this._applyFilter()}>Apply</uui-button>
                    <uui-button look="secondary" @click=${() => this._resetFilter()}>Reset</uui-button>
                </div>
                ${this._loading
                    ? html`<div class="loading"><uui-loader></uui-loader></div>`
                    : this._activeTab === 'audit' ? this._renderAuditTable() : this._renderLogTable()}
                ${this._total > 0 ? html`
                    <div class="pagination">
                        <span class="page-info">${this._total} total records</span>
                        <div class="page-controls">
                            <uui-button look="outline" ?disabled=${this._skip === 0}
                                @click=${() => this._prevPage()}>Previous</uui-button>
                            <span>Page ${this._currentPage} of ${this._totalPages}</span>
                            <uui-button look="outline" ?disabled=${this._skip + this._take >= this._total}
                                @click=${() => this._nextPage()}>Next</uui-button>
                        </div>
                    </div>` : ''}
            </uui-box>`;
    }

    _renderAuditTable() {
        if (!this._items.length) return html`<div class="no-results">No records found</div>`;
        return html`
            <uui-table aria-label="Audit Trail">
                <uui-table-head>
                    <uui-table-head-cell>Date</uui-table-head-cell>
                    <uui-table-head-cell>User</uui-table-head-cell>
                    <uui-table-head-cell>Event Type</uui-table-head-cell>
                    <uui-table-head-cell>Details</uui-table-head-cell>
                    <uui-table-head-cell>IP</uui-table-head-cell>
                    <uui-table-head-cell>Affected</uui-table-head-cell>
                </uui-table-head>
                ${this._items.map(item => html`
                    <uui-table-row>
                        <uui-table-cell>${this._formatDate(item.eventDateUtc)}</uui-table-cell>
                        <uui-table-cell>${item.performingDetails}</uui-table-cell>
                        <uui-table-cell><uui-tag look="primary">${item.eventType}</uui-tag></uui-table-cell>
                        <uui-table-cell class="detail-cell">${item.eventDetails}</uui-table-cell>
                        <uui-table-cell>${item.performingIp}</uui-table-cell>
                        <uui-table-cell>${item.affectedDetails}</uui-table-cell>
                    </uui-table-row>`)}
            </uui-table>`;
    }

    _renderLogTable() {
        if (!this._items.length) return html`<div class="no-results">No records found</div>`;
        return html`
            <uui-table aria-label="Content Logs">
                <uui-table-head>
                    <uui-table-head-cell>Date</uui-table-head-cell>
                    <uui-table-head-cell>User</uui-table-head-cell>
                    <uui-table-head-cell>Log Type</uui-table-head-cell>
                    <uui-table-head-cell>Comment</uui-table-head-cell>
                    <uui-table-head-cell>Node ID</uui-table-head-cell>
                    <uui-table-head-cell>Entity Type</uui-table-head-cell>
                </uui-table-head>
                ${this._items.map(item => html`
                    <uui-table-row>
                        <uui-table-cell>${this._formatDate(item.dateStamp)}</uui-table-cell>
                        <uui-table-cell>${item.userName}</uui-table-cell>
                        <uui-table-cell><uui-tag look="primary">${item.logHeader}</uui-tag></uui-table-cell>
                        <uui-table-cell class="detail-cell">${item.logComment}</uui-table-cell>
                        <uui-table-cell>${item.nodeId}</uui-table-cell>
                        <uui-table-cell>${item.entityType}</uui-table-cell>
                    </uui-table-row>`)}
            </uui-table>`;
    }

    static styles = css`
        :host { display: block; padding: 20px; }
        .header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; }
        .header h2 { margin: 0; font-size: 1.4rem; }
        .tabs { display: flex; gap: 8px; }
        .filters {
            display: flex; gap: 10px; align-items: center; flex-wrap: wrap;
            margin-bottom: 16px; padding: 12px;
            background: var(--uui-color-surface-alt, #f4f4f4); border-radius: 6px;
        }
        .filter-select, .filter-date {
            padding: 6px 10px; border: 1px solid var(--uui-color-border, #ccc);
            border-radius: 4px; font-size: 14px;
            background: var(--uui-color-surface, #fff); color: var(--uui-color-text, #333);
        }
        uui-input { min-width: 200px; }
        .loading { display: flex; justify-content: center; padding: 40px; }
        .no-results { text-align: center; padding: 40px; color: var(--uui-color-text-alt, #888); font-style: italic; }
        .detail-cell { max-width: 300px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
        .pagination {
            display: flex; justify-content: space-between; align-items: center;
            margin-top: 16px; padding-top: 12px; border-top: 1px solid var(--uui-color-border, #eee);
        }
        .page-controls { display: flex; align-items: center; gap: 10px; }
        .page-info { color: var(--uui-color-text-alt, #888); font-size: 0.9rem; }
        uui-table { width: 100%; }
    `;
}

customElements.define('utpro-audit-log-dashboard', UtproAuditLogDashboard);

export default UtproAuditLogDashboard;
