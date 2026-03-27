import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css, nothing } from '@umbraco-cms/backoffice/external/lit';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';

const API = '/umbraco/management/api/v1/utpro/simple-form';

export class UtproSimpleFormDashboard extends UmbLitElement {
    static properties = {
        _view: { type: String, state: true },
        _forms: { type: Array, state: true },
        _loading: { type: Boolean, state: true },
        _editForm: { type: Object, state: true },
        _fieldTypes: { type: Array, state: true },
        _submissions: { type: Array, state: true },
        _subTotal: { type: Number, state: true },
        _subSkip: { type: Number, state: true },
        _viewFormId: { type: Number, state: true },
        _error: { type: String, state: true },
        _success: { type: String, state: true },
        _selectedSubs: { type: Array, state: true },
        _detailSub: { type: Object, state: true },
    };
    #authContext;
    constructor() {
        super();
        this._view = 'list';
        this._forms = [];
        this._loading = false;
        this._editForm = null;
        this._fieldTypes = [];
        this._submissions = [];
        this._subTotal = 0;
        this._subSkip = 0;
        this._viewFormId = 0;
        this._error = '';
        this._success = '';
        this._selectedSubs = [];
        this._detailSub = null;
        this.consumeContext(UMB_AUTH_CONTEXT, (ctx) => { this.#authContext = ctx; });
    }
    async connectedCallback() {
        super.connectedCallback();
        await this._loadForms();
        await this._loadFieldTypes();
    }
    async _getHeaders() {
        const config = this.#authContext?.getOpenApiConfiguration();
        const h = {};
        if (config?.token) { const t = await config.token(); if (t) h['Authorization'] = 'Bearer ' + t; }
        return { headers: h, credentials: config?.credentials || 'same-origin' };
    }
    async _api(url, body = {}) {
        const auth = await this._getHeaders();
        const resp = await fetch(url, {
            method: 'POST', headers: { ...auth.headers, 'Content-Type': 'application/json' },
            credentials: auth.credentials, body: JSON.stringify(body)
        });
        if (!resp.ok) { const e = await resp.json().catch(() => ({})); throw new Error(e.message || 'Failed'); }
        return resp.json();
    }
    _msg(m, err = false) {
        if (err) { this._error = m; this._success = ''; } else { this._success = m; this._error = ''; }
        setTimeout(() => { this._error = ''; this._success = ''; }, 3000);
    }
    async _loadForms() {
        this._loading = true;
        try { this._forms = await this._api(API + '/list'); } catch (e) { this._msg(e.message, true); }
        this._loading = false;
    }
    async _loadFieldTypes() {
        try { this._fieldTypes = await this._api(API + '/field-types'); } catch {}
    }

    _newForm() {
        this._editForm = {
            id: 0, name: '', alias: '', fields: [],
            successMessage: 'Thank you!', redirectUrl: '', emailTo: '', emailSubject: '',
            storeSubmissions: true, isEnabled: true
        };
        this._view = 'edit';
    }
    async _editExisting(id) {
        try {
            const form = await this._api(API + '/get', { id });
            this._editForm = form;
            this._view = 'edit';
        } catch (e) { this._msg(e.message, true); }
    }
    async _saveForm() {
        if (!this._editForm.name || !this._editForm.alias) { this._msg('Name and Alias required', true); return; }
        try {
            const res = await this._api(API + '/save', this._editForm);
            this._msg(res.message);
            this._editForm.id = res.id;
            await this._loadForms();
        } catch (e) { this._msg(e.message, true); }
    }
    async _deleteForm(id) {
        if (!confirm('Delete this form and all submissions?')) return;
        try {
            await this._api(API + '/delete', { id });
            this._msg('Deleted');
            await this._loadForms();
            if (this._editForm?.id === id) { this._editForm = null; this._view = 'list'; }
        } catch (e) { this._msg(e.message, true); }
    }
    _addField() {
        const f = this._editForm;
        const idx = f.fields.length;
        f.fields = [...f.fields, {
            id: crypto.randomUUID?.() || Date.now().toString(36),
            type: 'text', label: '', name: 'field_' + idx,
            placeholder: '', cssClass: '', required: false,
            validation: '', validationMessage: '', defaultValue: '',
            options: [], sortOrder: idx, colSpan: 1, attributes: {}
        }];
        this.requestUpdate();
    }
    _removeField(idx) {
        this._editForm.fields = this._editForm.fields.filter((_, i) => i !== idx);
        this.requestUpdate();
    }
    _moveField(idx, dir) {
        const arr = [...this._editForm.fields];
        const newIdx = idx + dir;
        if (newIdx < 0 || newIdx >= arr.length) return;
        [arr[idx], arr[newIdx]] = [arr[newIdx], arr[idx]];
        arr.forEach((f, i) => f.sortOrder = i);
        this._editForm.fields = arr;
        this.requestUpdate();
    }
    _updateField(idx, key, val) {
        this._editForm.fields[idx][key] = val;
        this.requestUpdate();
    }
    _addOption(idx) {
        if (!this._editForm.fields[idx].options) this._editForm.fields[idx].options = [];
        this._editForm.fields[idx].options.push({ text: '', value: '' });
        this.requestUpdate();
    }
    _removeOption(fIdx, oIdx) {
        this._editForm.fields[fIdx].options.splice(oIdx, 1);
        this.requestUpdate();
    }
    async _viewSubmissions(formId) {
        this._viewFormId = formId;
        this._subSkip = 0;
        this._view = 'submissions';
        await this._loadSubmissions();
    }
    async _loadSubmissions() {
        try {
            const res = await this._api(API + '/submissions', { formId: this._viewFormId, skip: this._subSkip, take: 20 });
            this._submissions = res.items || [];
            this._subTotal = res.total || 0;
        } catch (e) { this._msg(e.message, true); }
    }
    async _deleteSubmission(id) {
        if (!confirm('Delete this submission?')) return;
        try { await this._api(API + '/delete-submission', { id }); this._msg('Deleted'); this._selectedSubs = this._selectedSubs.filter(x => x !== id); await this._loadSubmissions(); }
        catch (e) { this._msg(e.message, true); }
    }
    _toggleSubSelect(id) {
        if (this._selectedSubs.includes(id)) this._selectedSubs = this._selectedSubs.filter(x => x !== id);
        else this._selectedSubs = [...this._selectedSubs, id];
    }
    _toggleSelectAll() {
        if (this._selectedSubs.length === this._submissions.length) this._selectedSubs = [];
        else this._selectedSubs = this._submissions.map(s => s.id);
    }
    async _bulkDelete() {
        if (!this._selectedSubs.length) return;
        if (!confirm('Delete ' + this._selectedSubs.length + ' submissions?')) return;
        for (const id of this._selectedSubs) {
            try { await this._api(API + '/delete-submission', { id }); } catch {}
        }
        this._selectedSubs = [];
        this._msg('Deleted');
        await this._loadSubmissions();
    }
    _exportCsv() {
        if (!this._submissions.length) return;
        const allKeys = [...new Set(this._submissions.flatMap(s => Object.keys(s.data || {})))];
        const headers = ['Date', 'IP', ...allKeys];
        const rows = this._submissions.map(s => {
            const date = new Date(s.createdUtc).toLocaleString();
            const ip = s.ipAddress || '';
            const fields = allKeys.map(k => '"' + (s.data?.[k] || '').replace(/"/g, '""') + '"');
            return ['"' + date + '"', '"' + ip + '"', ...fields].join(',');
        });
        const csv = headers.join(',') + '\n' + rows.join('\n');
        const blob = new Blob(['\uFEFF' + csv], { type: 'text/csv;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        const formName = this._forms.find(f => f.id === this._viewFormId)?.alias || 'form';
        a.href = url; a.download = formName + '-submissions.csv'; a.click();
        URL.revokeObjectURL(url);
    }
    _viewDetail(sub) { this._detailSub = sub; }
    _closeDetail() { this._detailSub = null; }

    render() {
        return html`
            ${this._error ? html`<div class="msg error">${this._error}</div>` : nothing}
            ${this._success ? html`<div class="msg success">${this._success}</div>` : nothing}
            ${this._view === 'list' ? this._renderList()
                : this._view === 'edit' ? this._renderEditor()
                : this._renderSubmissions()}
            ${this._detailSub ? this._renderDetail() : nothing}`;
    }
    _renderList() {
        return html`
            <uui-box>
                <div class="toolbar">
                    <h2>Form Builder</h2>
                    <uui-button look="primary" @click=${this._newForm}>+ New Form</uui-button>
                </div>
                ${this._loading ? html`<div class="loading"><uui-loader></uui-loader></div>` : nothing}
                ${!this._forms.length && !this._loading ? html`<div class="empty">No forms yet. Create one!</div>` : nothing}
                ${this._forms.length ? html`
                <uui-table aria-label="Forms">
                    <uui-table-head>
                        <uui-table-head-cell>Name</uui-table-head-cell>
                        <uui-table-head-cell>Alias</uui-table-head-cell>
                        <uui-table-head-cell>Fields</uui-table-head-cell>
                        <uui-table-head-cell>Status</uui-table-head-cell>
                        <uui-table-head-cell style="width:260px">Actions</uui-table-head-cell>
                    </uui-table-head>
                    ${this._forms.map(f => html`
                        <uui-table-row>
                            <uui-table-cell><a class="link" @click=${() => this._editExisting(f.id)}>${f.name}</a></uui-table-cell>
                            <uui-table-cell><code>${f.alias}</code></uui-table-cell>
                            <uui-table-cell>${f.fields?.length || 0}</uui-table-cell>
                            <uui-table-cell>${f.isEnabled ? html`<span class="badge on">Active</span>` : html`<span class="badge off">Disabled</span>`}</uui-table-cell>
                            <uui-table-cell class="action-cell">
                                <uui-button look="outline" compact @click=${() => this._editExisting(f.id)}>Edit</uui-button>
                                <uui-button look="outline" compact @click=${() => this._viewSubmissions(f.id)}>Submissions</uui-button>
                                <uui-button look="outline" color="danger" compact @click=${() => this._deleteForm(f.id)}>Delete</uui-button>
                            </uui-table-cell>
                        </uui-table-row>`)}
                </uui-table>` : nothing}
            </uui-box>`;
    }

    _renderEditor() {
        const f = this._editForm;
        if (!f) return nothing;
        const needsOptions = (t) => ['select','radio','checkbox'].includes(t);
        return html`
            <uui-box>
                <div class="toolbar">
                    <uui-button look="outline" @click=${() => { this._view = 'list'; }}>&#8592; Back</uui-button>
                    <h2>${f.id ? 'Edit' : 'New'} Form</h2>
                    <uui-button look="primary" @click=${() => this._saveForm()}>Save Form</uui-button>
                </div>
                <div class="form-grid">
                    <label>Name <uui-input .value=${f.name} @input=${(e) => { f.name = e.target.value; }}></uui-input></label>
                    <label>Alias <uui-input .value=${f.alias} @input=${(e) => { f.alias = e.target.value; }}></uui-input></label>
                    <label>Success Message <uui-input .value=${f.successMessage || ''} @input=${(e) => { f.successMessage = e.target.value; }}></uui-input></label>
                    <label>Redirect URL <uui-input .value=${f.redirectUrl || ''} @input=${(e) => { f.redirectUrl = e.target.value; }}></uui-input></label>
                    <label>Email To <uui-input .value=${f.emailTo || ''} @input=${(e) => { f.emailTo = e.target.value; }}></uui-input></label>
                    <label>Email Subject <uui-input .value=${f.emailSubject || ''} @input=${(e) => { f.emailSubject = e.target.value; }}></uui-input></label>
                    <label class="check-label"><input type="checkbox" ?checked=${f.storeSubmissions} @change=${(e) => { f.storeSubmissions = e.target.checked; }} /> Store Submissions</label>
                    <label class="check-label"><input type="checkbox" ?checked=${f.isEnabled} @change=${(e) => { f.isEnabled = e.target.checked; }} /> Enabled</label>
                </div>
                <div class="section-header">
                    <h3>Fields</h3>
                    <uui-button look="primary" compact @click=${() => this._addField()}>+ Add Field</uui-button>
                </div>
                ${f.fields.map((field, idx) => html`
                    <div class="field-card">
                        <div class="field-header">
                            <span class="field-num">#${idx + 1}</span>
                            <select .value=${field.type} @change=${(e) => this._updateField(idx, 'type', e.target.value)}>
                                ${this._fieldTypes.map(ft => html`<option value=${ft.type} ?selected=${field.type === ft.type}>${ft.label}</option>`)}
                            </select>
                            <div class="field-actions">
                                <uui-button look="outline" compact @click=${() => this._moveField(idx, -1)} ?disabled=${idx === 0}>&#9650;</uui-button>
                                <uui-button look="outline" compact @click=${() => this._moveField(idx, 1)} ?disabled=${idx === f.fields.length - 1}>&#9660;</uui-button>
                                <uui-button look="outline" color="danger" compact @click=${() => this._removeField(idx)}>&#10005;</uui-button>
                            </div>
                        </div>
                        <div class="field-body">
                            <label>Label <uui-input .value=${field.label} @input=${(e) => this._updateField(idx, 'label', e.target.value)}></uui-input></label>
                            <label>Name <uui-input .value=${field.name} @input=${(e) => this._updateField(idx, 'name', e.target.value)}></uui-input></label>
                            <label>Placeholder <uui-input .value=${field.placeholder || ''} @input=${(e) => this._updateField(idx, 'placeholder', e.target.value)}></uui-input></label>
                            <label>CSS Class <uui-input .value=${field.cssClass || ''} @input=${(e) => this._updateField(idx, 'cssClass', e.target.value)}></uui-input></label>
                            <label>Default Value <uui-input .value=${field.defaultValue || ''} @input=${(e) => this._updateField(idx, 'defaultValue', e.target.value)}></uui-input></label>
                            <label class="check-label"><input type="checkbox" ?checked=${field.required} @change=${(e) => this._updateField(idx, 'required', e.target.checked)} /> Required</label>
                            <label>Validation Regex <uui-input .value=${field.validation || ''} @input=${(e) => this._updateField(idx, 'validation', e.target.value)}></uui-input></label>
                            <label>Validation Message <uui-input .value=${field.validationMessage || ''} @input=${(e) => this._updateField(idx, 'validationMessage', e.target.value)}></uui-input></label>
                            <label>Col Span
                                <select @change=${(e) => this._updateField(idx, 'colSpan', parseInt(e.target.value))}>
                                    <option value="1" ?selected=${(field.colSpan || 1) === 1}>Half width</option>
                                    <option value="2" ?selected=${field.colSpan === 2}>Full width</option>
                                </select>
                            </label>
                        </div>
                        ${needsOptions(field.type) ? html`
                            <div class="options-section">
                                <div class="section-header"><span>Options</span>
                                    <uui-button look="outline" compact @click=${() => this._addOption(idx)}>+ Option</uui-button>
                                </div>
                                ${(field.options || []).map((opt, oIdx) => html`
                                    <div class="option-row">
                                        <uui-input placeholder="Text" .value=${opt.text} @input=${(e) => { opt.text = e.target.value; this.requestUpdate(); }}></uui-input>
                                        <uui-input placeholder="Value" .value=${opt.value} @input=${(e) => { opt.value = e.target.value; this.requestUpdate(); }}></uui-input>
                                        <uui-button look="outline" color="danger" compact @click=${() => this._removeOption(idx, oIdx)}>&#10005;</uui-button>
                                    </div>`)}
                            </div>` : nothing}
                    </div>`)}
                ${f.id ? html`<div class="embed-info">
                    <h4>Embed Code</h4>
                    <code>POST /api/utpro/simple-form/submit { "alias": "${f.alias}", "data": { ... } }</code><br/>
                    <code>GET /api/utpro/simple-form/render/${f.alias}</code>
                </div>` : nothing}
            </uui-box>`;
    }

    _renderSubmissions() {
        const form = this._forms.find(f => f.id === this._viewFormId);
        const formName = form?.name || 'Form';
        const pages = Math.max(1, Math.ceil(this._subTotal / 20));
        const page = Math.floor(this._subSkip / 20) + 1;
        const allKeys = [...new Set(this._submissions.flatMap(s => Object.keys(s.data || {})))];
        const allSelected = this._submissions.length > 0 && this._selectedSubs.length === this._submissions.length;
        return html`
            <uui-box>
                <div class="toolbar">
                    <uui-button look="outline" @click=${() => { this._view = 'list'; this._selectedSubs = []; }}>&#8592; Back</uui-button>
                    <h2>Submissions: ${formName}</h2>
                    <div class="toolbar-right">
                        <span class="page-info">${this._subTotal} entries</span>
                        ${this._submissions.length ? html`
                            <uui-button look="outline" compact @click=${this._exportCsv}>Export CSV</uui-button>
                        ` : nothing}
                        ${this._selectedSubs.length ? html`
                            <uui-button look="outline" color="danger" compact @click=${this._bulkDelete}>
                                Delete (${this._selectedSubs.length})
                            </uui-button>
                        ` : nothing}
                    </div>
                </div>
                ${!this._submissions.length ? html`<div class="empty">No submissions yet</div>` : html`
                <uui-table aria-label="Submissions">
                    <uui-table-head>
                        <uui-table-head-cell style="width:40px">
                            <input type="checkbox" ?checked=${allSelected} @change=${this._toggleSelectAll} />
                        </uui-table-head-cell>
                        <uui-table-head-cell style="width:160px">Date</uui-table-head-cell>
                        <uui-table-head-cell style="width:120px">IP</uui-table-head-cell>
                        ${allKeys.map(k => html`<uui-table-head-cell>${k}</uui-table-head-cell>`)}
                        <uui-table-head-cell style="width:100px">Actions</uui-table-head-cell>
                    </uui-table-head>
                    ${this._submissions.map(s => html`
                        <uui-table-row class=${this._selectedSubs.includes(s.id) ? 'row-selected' : ''}>
                            <uui-table-cell>
                                <input type="checkbox" ?checked=${this._selectedSubs.includes(s.id)}
                                    @change=${() => this._toggleSubSelect(s.id)} />
                            </uui-table-cell>
                            <uui-table-cell>${new Date(s.createdUtc).toLocaleString()}</uui-table-cell>
                            <uui-table-cell>${s.ipAddress || ''}</uui-table-cell>
                            ${allKeys.map(k => html`<uui-table-cell class="cell-truncate">${s.data?.[k] || ''}</uui-table-cell>`)}
                            <uui-table-cell class="action-cell">
                                <uui-button look="outline" compact @click=${() => this._viewDetail(s)} title="View">&#9776;</uui-button>
                                <uui-button look="outline" color="danger" compact @click=${() => this._deleteSubmission(s.id)} title="Delete">&#10005;</uui-button>
                            </uui-table-cell>
                        </uui-table-row>`)}
                </uui-table>
                ${this._subTotal > 20 ? html`
                    <div class="pagination">
                        <uui-button look="outline" ?disabled=${this._subSkip === 0}
                            @click=${() => { this._subSkip = Math.max(0, this._subSkip - 20); this._loadSubmissions(); }}>Prev</uui-button>
                        <span>Page ${page} of ${pages}</span>
                        <uui-button look="outline" ?disabled=${this._subSkip + 20 >= this._subTotal}
                            @click=${() => { this._subSkip += 20; this._loadSubmissions(); }}>Next</uui-button>
                    </div>` : nothing}
                `}
            </uui-box>`;
    }

    _renderDetail() {
        const s = this._detailSub;
        if (!s) return nothing;
        const entries = Object.entries(s.data || {});
        return html`
            <div class="overlay" @click=${(e) => { if (e.target === e.currentTarget) this._closeDetail(); }}>
                <div class="detail-panel">
                    <div class="detail-header">
                        <h3>Submission #${s.id}</h3>
                        <uui-button look="secondary" compact @click=${this._closeDetail}>&#10005; Close</uui-button>
                    </div>
                    <div class="detail-body">
                        <div class="detail-row">
                            <span class="detail-label">Date</span>
                            <span class="detail-value">${new Date(s.createdUtc).toLocaleString()}</span>
                        </div>
                        <div class="detail-row">
                            <span class="detail-label">IP Address</span>
                            <span class="detail-value">${s.ipAddress || 'N/A'}</span>
                        </div>
                        ${entries.map(([k, v]) => html`
                            <div class="detail-row">
                                <span class="detail-label">${k}</span>
                                <span class="detail-value">${v || ''}</span>
                            </div>
                        `)}
                    </div>
                    <div class="detail-footer">
                        <uui-button look="outline" color="danger" @click=${() => { this._deleteSubmission(s.id); this._closeDetail(); }}>Delete</uui-button>
                    </div>
                </div>
            </div>`;
    }

    static styles = css`
        :host { display: block; padding: 20px; }
        .toolbar { display: flex; justify-content: space-between; align-items: center; margin-bottom: 16px; gap: 12px; flex-wrap: wrap; }
        .toolbar h2 { margin: 0; font-size: 1.3rem; }
        .msg { padding: 8px 14px; border-radius: 4px; margin-bottom: 10px; font-size: 0.9rem; }
        .error { background: #fde8e8; color: #c0392b; }
        .success { background: #e8fde8; color: #27ae60; }
        .loading { display: flex; justify-content: center; padding: 40px; }
        .empty { text-align: center; padding: 40px; color: #888; font-style: italic; }
        .link { color: var(--uui-color-interactive, #1b264f); cursor: pointer; font-weight: 500; text-decoration: none; }
        .link:hover { text-decoration: underline; }
        .badge { padding: 2px 8px; border-radius: 10px; font-size: 0.8rem; font-weight: 500; }
        .badge.on { background: #e8fde8; color: #27ae60; }
        .badge.off { background: #fde8e8; color: #c0392b; }
        .action-cell { display: flex; gap: 4px; }
        code { background: #f0f0f0; padding: 2px 6px; border-radius: 3px; font-size: 0.85rem; }
        uui-table { width: 100%; }
        .form-grid {
            display: grid; grid-template-columns: 1fr 1fr; gap: 12px;
            margin-bottom: 20px; padding: 16px;
            background: var(--uui-color-surface-alt, #f9f9f9); border-radius: 6px;
        }
        .form-grid label { display: flex; flex-direction: column; gap: 4px; font-size: 0.85rem; font-weight: 500; }
        .check-label { flex-direction: row !important; align-items: center; gap: 8px !important; }
        .section-header { display: flex; justify-content: space-between; align-items: center; margin: 16px 0 8px; }
        .section-header h3 { margin: 0; }
        .field-card {
            border: 1px solid var(--uui-color-border, #ddd); border-radius: 6px;
            margin-bottom: 10px; overflow: hidden;
        }
        .field-header {
            display: flex; align-items: center; gap: 10px; padding: 10px 14px;
            background: var(--uui-color-surface-alt, #f4f4f4);
        }
        .field-num { font-weight: 600; color: #888; min-width: 30px; }
        .field-header select {
            padding: 4px 8px; border: 1px solid #ccc; border-radius: 4px;
            font-size: 0.9rem; background: #fff;
        }
        .field-actions { margin-left: auto; display: flex; gap: 4px; }
        .field-body {
            display: grid; grid-template-columns: 1fr 1fr; gap: 10px; padding: 14px;
        }
        .field-body label { display: flex; flex-direction: column; gap: 4px; font-size: 0.8rem; font-weight: 500; }
        .options-section { padding: 0 14px 14px; }
        .option-row { display: flex; gap: 8px; margin-bottom: 6px; align-items: center; }
        .option-row uui-input { flex: 1; }
        .embed-info {
            margin-top: 20px; padding: 14px; background: #f0f4ff; border-radius: 6px;
            border: 1px solid #c8d6f0;
        }
        .embed-info h4 { margin: 0 0 8px; }
        .embed-info code { display: block; margin: 4px 0; padding: 6px 10px; background: #fff; }
        .pagination { display: flex; justify-content: center; align-items: center; gap: 12px; margin-top: 16px; }
        .page-info { color: #888; font-size: 0.9rem; }
        .toolbar-right { display: flex; align-items: center; gap: 8px; margin-left: auto; }
        .cell-truncate { max-width: 200px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
        .row-selected { background: var(--uui-color-surface-alt, #f0f4ff) !important; }
        .overlay {
            position: fixed; top: 0; left: 0; right: 0; bottom: 0;
            background: rgba(0,0,0,0.5); z-index: 9999;
            display: flex; justify-content: center; align-items: center;
        }
        .detail-panel {
            background: var(--uui-color-surface, #fff); border-radius: 8px;
            width: 600px; max-width: 90vw; max-height: 80vh; display: flex; flex-direction: column;
            box-shadow: 0 8px 32px rgba(0,0,0,0.3);
        }
        .detail-header {
            display: flex; justify-content: space-between; align-items: center;
            padding: 16px 20px; border-bottom: 1px solid #e0e0e0;
        }
        .detail-header h3 { margin: 0; }
        .detail-body { padding: 20px; overflow-y: auto; flex: 1; }
        .detail-row {
            display: flex; padding: 10px 0; border-bottom: 1px solid #f0f0f0;
        }
        .detail-label { font-weight: 600; min-width: 120px; color: #555; font-size: 0.9rem; }
        .detail-value { flex: 1; word-break: break-word; white-space: pre-wrap; }
        .detail-footer { padding: 12px 20px; border-top: 1px solid #e0e0e0; display: flex; justify-content: flex-end; }
    `;
}

customElements.define('utpro-simple-form-dashboard', UtproSimpleFormDashboard);
export default UtproSimpleFormDashboard;
