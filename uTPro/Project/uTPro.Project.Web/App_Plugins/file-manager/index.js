import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css, nothing } from '@umbraco-cms/backoffice/external/lit';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';

const API = '/umbraco/management/api/v1/utpro/file-manager';

export class UtproFileManagerDashboard extends UmbLitElement {

    static properties = {
        _loading: { type: Boolean, state: true },
        _items: { type: Array, state: true },
        _currentPath: { type: String, state: true },
        _parentPath: { type: String, state: true },
        _showNewFolder: { type: Boolean, state: true },
        _newFolderName: { type: String, state: true },
        _renameItem: { type: Object, state: true },
        _renameName: { type: String, state: true },
        _error: { type: String, state: true },
        _success: { type: String, state: true },
        _uploading: { type: Boolean, state: true },
        _editFile: { type: Object, state: true },
        _editContent: { type: String, state: true },
        _saving: { type: Boolean, state: true },
        _dirty: { type: Boolean, state: true },
    };

    #authContext;
    #boundBeforeUnload;
    #boundKeydown;

    constructor() {
        super();
        this._loading = false;
        this._items = [];
        this._currentPath = '';
        this._parentPath = null;
        this._showNewFolder = false;
        this._newFolderName = '';
        this._renameItem = null;
        this._renameName = '';
        this._error = '';
        this._success = '';
        this._uploading = false;
        this._editFile = null;
        this._editContent = '';
        this._saving = false;
        this._dirty = false;
        this.consumeContext(UMB_AUTH_CONTEXT, (ctx) => { this.#authContext = ctx; });

        this.#boundBeforeUnload = (e) => {
            if (this._dirty) { e.preventDefault(); e.returnValue = ''; }
        };
        this.#boundKeydown = (e) => {
            if ((e.ctrlKey || e.metaKey) && e.key === 's' && this._editFile) {
                e.preventDefault();
                this._saveFile();
            }
        };
    }

    async connectedCallback() {
        super.connectedCallback();
        window.addEventListener('beforeunload', this.#boundBeforeUnload);
        window.addEventListener('keydown', this.#boundKeydown);
        await this._loadDir('');
    }

    disconnectedCallback() {
        super.disconnectedCallback();
        window.removeEventListener('beforeunload', this.#boundBeforeUnload);
        window.removeEventListener('keydown', this.#boundKeydown);
    }

    async _getHeaders() {
        const config = this.#authContext?.getOpenApiConfiguration();
        const headers = {};
        if (config?.token) {
            const token = await config.token();
            if (token) headers['Authorization'] = 'Bearer ' + token;
        }
        return { headers, credentials: config?.credentials || 'same-origin' };
    }

    async _fetchJson(url, options = {}) {
        const auth = await this._getHeaders();
        const resp = await fetch(url, {
            ...options,
            headers: { ...auth.headers, ...options.headers },
            credentials: auth.credentials
        });
        if (!resp.ok) {
            const err = await resp.json().catch(() => ({ message: 'Request failed' }));
            throw new Error(err.message || 'Request failed');
        }
        return resp.json();
    }

    _showMsg(msg, isError = false) {
        if (isError) { this._error = msg; this._success = ''; }
        else { this._success = msg; this._error = ''; }
        setTimeout(() => { this._error = ''; this._success = ''; }, 3000);
    }

    async _loadDir(path) {
        this._loading = true;
        this._error = '';
        try {
            const data = await this._fetchJson(API + '/list', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ path: path || '' })
            });
            this._items = data.items || [];
            this._currentPath = data.currentPath || '';
            this._parentPath = data.parentPath;
        } catch (e) {
            this._showMsg(e.message, true);
            this._items = [];
        }
        this._loading = false;
    }

    _navigate(item) { if (item.isDirectory) this._loadDir(item.path); }
    _goUp() { if (this._parentPath !== null && this._parentPath !== undefined) this._loadDir(this._parentPath); }
    _goRoot() { this._loadDir(''); }

    async _createFolder() {
        if (!this._newFolderName.trim()) return;
        try {
            await this._fetchJson(API + '/create-folder', {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ path: this._currentPath, name: this._newFolderName.trim() })
            });
            this._showMsg('Folder created');
            this._newFolderName = '';
            this._showNewFolder = false;
            await this._loadDir(this._currentPath);
        } catch (e) { this._showMsg(e.message, true); }
    }

    async _deleteItem(item) {
        if (!confirm('Delete "' + item.name + '"?')) return;
        try {
            await this._fetchJson(API + '/delete', {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ path: item.path })
            });
            this._showMsg('Deleted');
            await this._loadDir(this._currentPath);
        } catch (e) { this._showMsg(e.message, true); }
    }

    _startRename(item) { this._renameItem = item; this._renameName = item.name; }

    async _doRename() {
        if (!this._renameName.trim() || !this._renameItem) return;
        try {
            await this._fetchJson(API + '/rename', {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ path: this._renameItem.path, newName: this._renameName.trim() })
            });
            this._showMsg('Renamed');
            this._renameItem = null;
            this._renameName = '';
            await this._loadDir(this._currentPath);
        } catch (e) { this._showMsg(e.message, true); }
    }

    async _downloadItem(item) {
        const auth = await this._getHeaders();
        const resp = await fetch(API + '/download', {
            method: 'POST',
            headers: { ...auth.headers, 'Content-Type': 'application/json' },
            credentials: auth.credentials,
            body: JSON.stringify({ path: item.path })
        });
        if (!resp.ok) { this._showMsg('Download failed', true); return; }
        const blob = await resp.blob();
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a'); a.href = url; a.download = item.name; a.click();
        URL.revokeObjectURL(url);
    }

    async _handleUpload(e) {
        const files = e.target.files;
        if (!files || files.length === 0) return;
        this._uploading = true;
        const auth = await this._getHeaders();
        let ok = 0, fail = 0;
        for (const file of files) {
            try {
                const form = new FormData(); form.append('file', file);
                const resp = await fetch(API + '/upload?path=' + encodeURIComponent(this._currentPath), {
                    method: 'POST', headers: auth.headers, credentials: auth.credentials, body: form
                });
                if (resp.ok) ok++; else fail++;
            } catch { fail++; }
        }
        this._uploading = false;
        e.target.value = '';
        this._showMsg(ok + ' uploaded' + (fail ? ', ' + fail + ' failed' : ''));
        await this._loadDir(this._currentPath);
    }

    _isEditable(item) {
        if (item.isDirectory) return false;
        const ext = (item.extension || '').toLowerCase();
        return ['js','css','html','htm','json','xml','txt','md','csv','svg',
                'cshtml','cs','config','yaml','yml','less','scss','sass','ts',
                'map','env','gitignore'].includes(ext);
    }

    async _openEditor(item) {
        if (this._dirty && !confirm('You have unsaved changes. Discard?')) return;
        try {
            const data = await this._fetchJson(API + '/read-file', {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ path: item.path })
            });
            this._editFile = data;
            this._editContent = data.content;
            this._dirty = false;
        } catch (e) { this._showMsg(e.message, true); }
    }

    _closeEditor() {
        if (this._dirty && !confirm('You have unsaved changes. Discard?')) return;
        this._editFile = null;
        this._editContent = '';
        this._dirty = false;
    }

    _onEditorInput(e) {
        this._editContent = e.target.value;
        if (this._editFile && this._editContent !== this._editFile.content) {
            this._dirty = true;
        } else {
            this._dirty = false;
        }
    }

    async _saveFile() {
        if (!this._editFile || this._saving) return;
        this._saving = true;
        try {
            await this._fetchJson(API + '/save-file', {
                method: 'POST', headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ path: this._editFile.path, content: this._editContent })
            });
            this._showMsg('File saved');
            this._editFile = { ...this._editFile, content: this._editContent };
            this._dirty = false;
        } catch (e) { this._showMsg(e.message, true); }
        this._saving = false;
    }

    _formatSize(bytes) {
        if (!bytes || bytes === 0) return '-';
        const units = ['B', 'KB', 'MB', 'GB'];
        let i = 0; let size = bytes;
        while (size >= 1024 && i < units.length - 1) { size /= 1024; i++; }
        return size.toFixed(i > 0 ? 1 : 0) + ' ' + units[i];
    }

    _formatDate(d) { return d ? new Date(d).toLocaleString() : ''; }

    _getIcon(item) {
        if (item.isDirectory) return html`<span class="icon icon-folder" title="Folder">&#128193;</span>`;
        const ext = (item.extension || '').toLowerCase();
        if (['jpg','jpeg','png','gif','svg','webp','ico'].includes(ext))
            return html`<span class="icon icon-image" title="Image">&#128444;</span>`;
        if (['pdf'].includes(ext))
            return html`<span class="icon icon-pdf" title="PDF">&#128196;</span>`;
        if (['zip','rar','7z','gz','tar'].includes(ext))
            return html`<span class="icon icon-archive" title="Archive">&#128230;</span>`;
        if (['js','ts','css','html','json','xml','cs','cshtml'].includes(ext))
            return html`<span class="icon icon-code" title="Code">&#128221;</span>`;
        return html`<span class="icon icon-file" title="File">&#128196;</span>`;
    }

    render() {
        return html`
            <uui-box>
                ${this._error ? html`<div class="msg error">${this._error}</div>` : nothing}
                ${this._success ? html`<div class="msg success">${this._success}</div>` : nothing}

                <div class="toolbar">
                    <div class="breadcrumb">
                        <uui-button look="outline" compact @click=${this._goRoot}>
                            <span class="icon">&#127968;</span> Root
                        </uui-button>
                        ${this._parentPath !== null && this._parentPath !== undefined
                            ? html`<uui-button look="outline" compact @click=${this._goUp}>
                                <span class="icon">&#11014;</span> Up
                            </uui-button>` : nothing}
                        <span class="current-path">/${this._currentPath || ''}</span>
                    </div>
                    <div class="actions">
                        <uui-button look="primary" compact @click=${() => { this._showNewFolder = !this._showNewFolder; }}>
                            + Folder
                        </uui-button>
                        <label class="upload-btn">
                            ${this._uploading ? 'Uploading...' : html`<span class="icon">&#11014;</span> Upload`}
                            <input type="file" multiple hidden @change=${this._handleUpload} ?disabled=${this._uploading} />
                        </label>
                        <uui-button look="outline" compact @click=${() => this._loadDir(this._currentPath)}>
                            <span class="icon">&#8635;</span>
                        </uui-button>
                    </div>
                </div>

                ${this._showNewFolder ? html`
                    <div class="inline-row">
                        <uui-input placeholder="Folder name" .value=${this._newFolderName}
                            @input=${(e) => { this._newFolderName = e.target.value; }}
                            @keyup=${(e) => { if (e.key === 'Enter') this._createFolder(); }}></uui-input>
                        <uui-button look="primary" compact @click=${this._createFolder}>Create</uui-button>
                        <uui-button look="secondary" compact @click=${() => { this._showNewFolder = false; }}>Cancel</uui-button>
                    </div>` : nothing}

                ${this._renameItem ? html`
                    <div class="inline-row">
                        <span>Rename "${this._renameItem.name}" to:</span>
                        <uui-input .value=${this._renameName}
                            @input=${(e) => { this._renameName = e.target.value; }}
                            @keyup=${(e) => { if (e.key === 'Enter') this._doRename(); }}></uui-input>
                        <uui-button look="primary" compact @click=${this._doRename}>Rename</uui-button>
                        <uui-button look="secondary" compact @click=${() => { this._renameItem = null; }}>Cancel</uui-button>
                    </div>` : nothing}

                ${this._loading
                    ? html`<div class="loading"><uui-loader></uui-loader></div>`
                    : this._renderTable()}
            </uui-box>
            ${this._editFile ? this._renderEditor() : nothing}`;
    }

    _renderEditor() {
        return html`
            <div class="editor-overlay" @click=${(e) => { if (e.target === e.currentTarget) this._closeEditor(); }}>
                <div class="editor-panel">
                    <div class="editor-header">
                        <span class="editor-title">
                            <span class="icon">&#128221;</span>
                            ${this._editFile.name}
                            ${this._dirty ? html`<span class="dirty-dot" title="Unsaved changes">*</span>` : nothing}
                        </span>
                        <span class="editor-path">${this._editFile.path}</span>
                        <span class="editor-hint">Ctrl+S to save</span>
                        <div class="editor-actions">
                            <uui-button look="primary" @click=${this._saveFile} ?disabled=${this._saving}>
                                ${this._saving ? 'Saving...' : 'Save'}
                            </uui-button>
                            <uui-button look="secondary" @click=${this._closeEditor}>Close</uui-button>
                        </div>
                    </div>
                    <textarea class="editor-textarea"
                        .value=${this._editContent}
                        @input=${this._onEditorInput}
                        spellcheck="false"></textarea>
                </div>
            </div>`;
    }

    _renderTable() {
        if (!this._items.length) return html`<div class="empty">This folder is empty</div>`;
        return html`
            <uui-table aria-label="File Manager">
                <uui-table-head>
                    <uui-table-head-cell style="width:40px"></uui-table-head-cell>
                    <uui-table-head-cell>Name</uui-table-head-cell>
                    <uui-table-head-cell style="width:100px">Size</uui-table-head-cell>
                    <uui-table-head-cell style="width:180px">Modified</uui-table-head-cell>
                    <uui-table-head-cell style="width:220px">Actions</uui-table-head-cell>
                </uui-table-head>
                ${this._items.map(item => html`
                    <uui-table-row>
                        <uui-table-cell>${this._getIcon(item)}</uui-table-cell>
                        <uui-table-cell>
                            ${item.isDirectory
                                ? html`<a class="dir-link" @click=${() => this._navigate(item)}>${item.name}</a>`
                                : html`<span>${item.name}</span>`}
                        </uui-table-cell>
                        <uui-table-cell>${item.isDirectory ? '-' : this._formatSize(item.size)}</uui-table-cell>
                        <uui-table-cell>${this._formatDate(item.lastModified)}</uui-table-cell>
                        <uui-table-cell class="action-cell">
                            ${!item.isDirectory && this._isEditable(item) ? html`
                                <uui-button look="outline" compact @click=${() => this._openEditor(item)} title="Edit file">
                                    <span class="icon">&#9998;</span>
                                </uui-button>` : nothing}
                            ${!item.isDirectory ? html`
                                <uui-button look="outline" compact @click=${() => this._downloadItem(item)} title="Download">
                                    <span class="icon">&#11015;</span>
                                </uui-button>` : nothing}
                            <uui-button look="outline" compact @click=${() => this._startRename(item)} title="Rename">
                                <span class="icon">&#9999;</span>
                            </uui-button>
                            <uui-button look="outline" color="danger" compact @click=${() => this._deleteItem(item)} title="Delete">
                                <span class="icon">&#10005;</span>
                            </uui-button>
                        </uui-table-cell>
                    </uui-table-row>`)}
            </uui-table>`;
    }

    static styles = css`
        :host { display: block; padding: 20px; }
        .icon { font-style: normal; }
        .icon-folder { color: #e8a838; }
        .icon-image { color: #4caf50; }
        .icon-pdf { color: #e53935; }
        .icon-archive { color: #8e6e53; }
        .icon-code { color: #1976d2; }
        .icon-file { color: #757575; }
        .toolbar {
            display: flex; justify-content: space-between; align-items: center;
            margin-bottom: 12px; flex-wrap: wrap; gap: 8px;
        }
        .breadcrumb { display: flex; align-items: center; gap: 8px; }
        .current-path {
            font-family: monospace; font-size: 0.95rem;
            color: var(--uui-color-text-alt, #666); padding: 4px 8px;
            background: var(--uui-color-surface-alt, #f4f4f4); border-radius: 4px;
        }
        .actions { display: flex; gap: 6px; align-items: center; }
        .upload-btn {
            display: inline-flex; align-items: center; gap: 4px;
            padding: 4px 12px; border-radius: 4px; cursor: pointer;
            background: var(--uui-color-positive, #2bc37c); color: #fff;
            font-size: 0.85rem; font-weight: 500;
        }
        .upload-btn:hover { opacity: 0.9; }
        .inline-row {
            display: flex; gap: 8px; align-items: center;
            margin-bottom: 12px; padding: 10px;
            background: var(--uui-color-surface-alt, #f4f4f4); border-radius: 6px;
        }
        .inline-row uui-input { min-width: 250px; }
        .loading { display: flex; justify-content: center; padding: 40px; }
        .empty {
            text-align: center; padding: 60px 20px;
            color: var(--uui-color-text-alt, #888); font-style: italic; font-size: 1.1rem;
        }
        .dir-link {
            color: var(--uui-color-interactive, #1b264f); cursor: pointer;
            text-decoration: none; font-weight: 500;
        }
        .dir-link:hover { text-decoration: underline; }
        .action-cell { display: flex; gap: 4px; }
        .msg { padding: 8px 14px; border-radius: 4px; margin-bottom: 10px; font-size: 0.9rem; }
        .error { background: #fde8e8; color: #c0392b; }
        .success { background: #e8fde8; color: #27ae60; }
        uui-table { width: 100%; }
        .editor-overlay {
            position: fixed; top: 0; left: 0; right: 0; bottom: 0;
            background: rgba(0,0,0,0.5); z-index: 9999;
            display: flex; justify-content: center; align-items: center;
        }
        .editor-panel {
            background: var(--uui-color-surface, #fff); border-radius: 8px;
            width: 90vw; height: 85vh; display: flex; flex-direction: column;
            box-shadow: 0 8px 32px rgba(0,0,0,0.3);
        }
        .editor-header {
            display: flex; align-items: center; gap: 12px; padding: 12px 16px;
            border-bottom: 1px solid var(--uui-color-border, #e0e0e0); flex-wrap: wrap;
        }
        .editor-title { font-weight: 600; font-size: 1rem; display: flex; align-items: center; gap: 4px; }
        .dirty-dot { color: #e53935; font-size: 1.2rem; font-weight: bold; }
        .editor-path {
            font-family: monospace; font-size: 0.8rem;
            color: var(--uui-color-text-alt, #888);
            background: var(--uui-color-surface-alt, #f4f4f4);
            padding: 2px 8px; border-radius: 3px;
        }
        .editor-hint {
            font-size: 0.75rem; color: var(--uui-color-text-alt, #999);
            background: var(--uui-color-surface-alt, #f0f0f0);
            padding: 2px 8px; border-radius: 3px;
        }
        .editor-actions { margin-left: auto; display: flex; gap: 6px; }
        .editor-textarea {
            flex: 1; margin: 0; padding: 16px; border: none; outline: none;
            font-family: 'Cascadia Code', 'Fira Code', 'Consolas', monospace;
            font-size: 13px; line-height: 1.5; resize: none;
            background: #1e1e1e; color: #d4d4d4;
            tab-size: 4; white-space: pre; overflow: auto;
        }
    `;
}

customElements.define('utpro-file-manager-dashboard', UtproFileManagerDashboard);
export default UtproFileManagerDashboard;
