import { html, nothing } from '@umbraco-cms/backoffice/external/lit';

/**
 * Renders the form editor view.
 * @param {object} host - the dashboard element
 */
export function renderEditor(host) {
    const f = host._editForm;
    if (!f) return nothing;

    const needsOptions = (t) => ['select', 'radio', 'checkbox'].includes(t);
    const showSettings = host._showColumnSettings;

    return html`
        <uui-box>
            <div class="toolbar">
                <uui-button look="outline" @click=${() => { host._view = 'list'; host._showColumnSettings = false; }}>&#8592; Back</uui-button>
                <h2>${f.id ? 'Edit' : 'New'} Form</h2>
                <div class="toolbar-right">
                    ${f.id ? html`
                        <uui-button look="outline" compact @click=${() => host._viewEntries(f.id)}>
                            Entries (${host._entryCount ?? 0})
                        </uui-button>
                        <uui-button look="${showSettings ? 'primary' : 'outline'}" compact
                            @click=${() => { host._showColumnSettings = !host._showColumnSettings; host.requestUpdate(); }}>
                            &#9881; Settings
                        </uui-button>
                    ` : nothing}
                    <uui-button look="primary" @click=${() => host._saveForm()}>Save Form</uui-button>
                </div>
            </div>

            <!-- Settings panels (toggle) -->
            ${showSettings && f.id ? html`
                ${_renderEmbedSettings(host, f)}
                ${_renderColumnSettings(host, f)}
            ` : nothing}

            <!-- Form settings -->
            <div class="form-grid">
                <label>Name <uui-input .value=${f.name} @input=${(e) => { f.name = e.target.value; }}></uui-input></label>
                <label>Alias <uui-input .value=${f.alias} @input=${(e) => { f.alias = e.target.value; }}></uui-input></label>
                <label>Success Message <uui-input .value=${f.successMessage || ''} @input=${(e) => { f.successMessage = e.target.value; }}></uui-input></label>
                <label>Redirect URL <uui-input .value=${f.redirectUrl || ''} @input=${(e) => { f.redirectUrl = e.target.value; }}></uui-input></label>
                <label>Email To <uui-input .value=${f.emailTo || ''} @input=${(e) => { f.emailTo = e.target.value; }}></uui-input></label>
                <label>Email Subject <uui-input .value=${f.emailSubject || ''} @input=${(e) => { f.emailSubject = e.target.value; }}></uui-input></label>
                <label class="check-label"><uui-toggle ?checked=${f.storeEntries} @change=${(e) => { f.storeEntries = e.target.checked; }} label="Store Entries"></uui-toggle></label>
                <label class="check-label"><uui-toggle ?checked=${f.isEnabled} @change=${(e) => { f.isEnabled = e.target.checked; }} label="Enabled"></uui-toggle></label>
            </div>

            <!-- Fields -->
            <div class="section-header">
                <h3>Fields</h3>
                <uui-button look="primary" compact @click=${() => host._addField()}>+ Add Field</uui-button>
            </div>
            ${f.fields.map((field, idx) => _renderFieldCard(host, field, idx, needsOptions))}
        </uui-box>
        ${host._typePickerIdx >= 0 ? _renderTypePicker(host) : nothing}`;
}

/**
 * Renders the Embed Code / API settings panel.
 */
function _renderEmbedSettings(host, f) {
    return html`
        <div class="settings-panel">
            <div class="settings-header">
                <h3>Embed API Settings</h3>
            </div>
            <div class="embed-render-row">
                <code>POST /api/utpro/simple-form/submit { "alias": "${f.alias}", "data": { ... } }</code>
            </div>
            <div class="embed-render-row">
                <uui-toggle ?checked=${f.enableRenderApi}
                    @change=${(e) => { f.enableRenderApi = e.target.checked; host.requestUpdate(); }}
                    label=${f.enableRenderApi ? 'Enabled' : 'Disabled'}></uui-toggle>
                <code>GET /api/utpro/simple-form/render/${f.alias}</code>
            </div>
            <div class="embed-render-row">
                <uui-toggle ?checked=${f.enableEntriesApi}
                    @change=${(e) => { f.enableEntriesApi = e.target.checked; host.requestUpdate(); }}
                    label=${f.enableEntriesApi ? 'Enabled' : 'Disabled'}></uui-toggle>
                <code>GET /api/utpro/simple-form/entries/${f.alias}</code>
            </div>
        </div>`;
}

/**
 * Renders the column visibility settings panel with drag & drop reordering.
 */
function _renderColumnSettings(host, f) {
    const allFieldNames = f.fields.map(field => field.name).filter(n => n);

    // Build ordered list: visibleColumns first (in order), then unchecked ones
    let orderedNames;
    if (f.visibleColumns && f.visibleColumns.length > 0) {
        const checked = f.visibleColumns.filter(n => allFieldNames.includes(n));
        const unchecked = allFieldNames.filter(n => !f.visibleColumns.includes(n));
        orderedNames = [...checked, ...unchecked];
    } else {
        orderedNames = [...allFieldNames];
    }

    return html`
        <div class="settings-panel">
            <div class="settings-header">
                <h3>&#9881; Entries Column Settings</h3>
                <span class="settings-hint">Select which fields to show as columns in the Entries view. Drag to reorder.</span>
            </div>
            <div class="settings-body">
                ${allFieldNames.length === 0 ? html`<div class="empty">No fields yet. Add fields first.</div>` : nothing}
                ${orderedNames.map((name, idx) => {
                    const isVisible = f.visibleColumns === null || f.visibleColumns === undefined
                        ? true
                        : f.visibleColumns.includes(name);
                    return html`
                        <label class="check-label settings-col-item"
                            draggable="true"
                            @dragstart=${(e) => { e.dataTransfer.setData('text/plain', idx.toString()); e.dataTransfer.effectAllowed = 'move'; e.currentTarget.classList.add('dragging'); }}
                            @dragend=${(e) => { e.currentTarget.classList.remove('dragging'); }}
                            @dragover=${(e) => { e.preventDefault(); e.dataTransfer.dropEffect = 'move'; e.currentTarget.classList.add('drag-over'); }}
                            @dragleave=${(e) => { e.currentTarget.classList.remove('drag-over'); }}
                            @drop=${(e) => {
                                e.preventDefault();
                                e.currentTarget.classList.remove('drag-over');
                                const fromIdx = parseInt(e.dataTransfer.getData('text/plain'));
                                const toIdx = idx;
                                if (fromIdx === toIdx) return;
                                // Initialize visibleColumns if needed
                                if (!f.visibleColumns) {
                                    f.visibleColumns = [...allFieldNames];
                                }
                                // Reorder: work on the full ordered list, then update visibleColumns
                                const arr = [...orderedNames];
                                const [moved] = arr.splice(fromIdx, 1);
                                arr.splice(toIdx, 0, moved);
                                // visibleColumns = only checked items, in new order
                                f.visibleColumns = arr.filter(n => f.visibleColumns.includes(n));
                                host.requestUpdate();
                            }}>
                            <span class="drag-handle">&#9776;</span>
                            <input type="checkbox" ?checked=${isVisible}
                                @change=${(e) => {
                                    if (!f.visibleColumns) {
                                        f.visibleColumns = [...allFieldNames];
                                    }
                                    if (e.target.checked) {
                                        if (!f.visibleColumns.includes(name)) f.visibleColumns.push(name);
                                    } else {
                                        f.visibleColumns = f.visibleColumns.filter(c => c !== name);
                                    }
                                    host.requestUpdate();
                                }} />
                            ${name}
                        </label>`;
                })}
            </div>
        </div>`;
}

/**
 * Renders the field type picker dialog with search.
 */
function _renderTypePicker(host) {
    const search = (host._typePickerSearch || '').toLowerCase();
    const filtered = host._fieldTypes.filter(ft =>
        ft.label.toLowerCase().includes(search) || ft.type.toLowerCase().includes(search)
    );
    const idx = host._typePickerIdx;
    const currentType = host._editForm?.fields[idx]?.type;

    return html`
        <div class="overlay" @click=${(e) => { if (e.target === e.currentTarget) { host._typePickerIdx = -1; host.requestUpdate(); } }}>
            <div class="type-picker-dialog">
                <div class="type-picker-header">
                    <h3>Select Field Type</h3>
                    <uui-button look="secondary" compact @click=${() => { host._typePickerIdx = -1; host.requestUpdate(); }}>&#10005;</uui-button>
                </div>
                <div class="type-picker-search">
                    <uui-input placeholder="Search field types..."
                        .value=${host._typePickerSearch || ''}
                        @input=${(e) => { host._typePickerSearch = e.target.value; host.requestUpdate(); }}>
                    </uui-input>
                </div>
                <div class="type-picker-list">
                    ${filtered.map(ft => html`
                        <button class="type-picker-option ${ft.type === currentType ? 'active' : ''}"
                            @click=${() => {
                                host._updateField(idx, 'type', ft.type);
                                host._typePickerIdx = -1;
                                host.requestUpdate();
                            }}>
                            <span class="type-picker-label">${ft.label}</span>
                            <span class="type-picker-type">${ft.type}</span>
                        </button>
                    `)}
                    ${filtered.length === 0 ? html`<div class="type-picker-empty">No matching types</div>` : nothing}
                </div>
            </div>
        </div>`;
}

/**
 * Renders type-specific attribute fields.
 * Uses field.attributes dict to store extra config per type.
 */
function _renderTypeAttributes(host, field, idx) {
    const t = field.type;
    if (!field.attributes) field.attributes = {};
    const attr = field.attributes;
    const setAttr = (key, val) => { field.attributes[key] = val; host.requestUpdate(); };

    // Number: min, max, step
    if (t === 'number') return html`
        <div class="field-attrs">
            <label>Min <uui-input .value=${attr.min || ''} @input=${(e) => setAttr('min', e.target.value)}></uui-input></label>
            <label>Max <uui-input .value=${attr.max || ''} @input=${(e) => setAttr('max', e.target.value)}></uui-input></label>
            <label>Step <uui-input .value=${attr.step || ''} @input=${(e) => setAttr('step', e.target.value)} placeholder="1"></uui-input></label>
        </div>`;

    // Date: min, max
    if (t === 'date') return html`
        <div class="field-attrs">
            <label>Min Date <uui-input type="date" .value=${attr.min || ''} @input=${(e) => setAttr('min', e.target.value)}></uui-input></label>
            <label>Max Date <uui-input type="date" .value=${attr.max || ''} @input=${(e) => setAttr('max', e.target.value)}></uui-input></label>
        </div>`;

    // Time: min, max
    if (t === 'time') return html`
        <div class="field-attrs">
            <label>Min Time <uui-input .value=${attr.min || ''} @input=${(e) => setAttr('min', e.target.value)} placeholder="09:00"></uui-input></label>
            <label>Max Time <uui-input .value=${attr.max || ''} @input=${(e) => setAttr('max', e.target.value)} placeholder="17:00"></uui-input></label>
        </div>`;

    // Textarea: rows
    if (t === 'textarea') return html`
        <div class="field-attrs">
            <label>Rows <uui-input .value=${attr.rows || '4'} @input=${(e) => setAttr('rows', e.target.value)}></uui-input></label>
        </div>`;

    // File: accept, maxSize
    if (t === 'file') return html`
        <div class="field-attrs">
            <label>Accept (MIME) <uui-input .value=${attr.accept || ''} @input=${(e) => setAttr('accept', e.target.value)} placeholder=".pdf,.jpg,.png"></uui-input></label>
            <label>Max Size (MB) <uui-input .value=${attr.maxSize || ''} @input=${(e) => setAttr('maxSize', e.target.value)} placeholder="10"></uui-input></label>
        </div>`;

    // Range: min, max, step
    if (t === 'range') return html`
        <div class="field-attrs">
            <label>Min <uui-input .value=${attr.min || '0'} @input=${(e) => setAttr('min', e.target.value)}></uui-input></label>
            <label>Max <uui-input .value=${attr.max || '100'} @input=${(e) => setAttr('max', e.target.value)}></uui-input></label>
            <label>Step <uui-input .value=${attr.step || '1'} @input=${(e) => setAttr('step', e.target.value)}></uui-input></label>
        </div>`;

    // Accept/Terms: text, linkUrl, linkText
    if (t === 'accept') return html`
        <div class="field-attrs">
            <label>Terms Text <uui-input .value=${attr.text || ''} @input=${(e) => setAttr('text', e.target.value)} placeholder="I agree to the"></uui-input></label>
            <label>Link URL <uui-input .value=${attr.linkUrl || ''} @input=${(e) => setAttr('linkUrl', e.target.value)} placeholder="/terms"></uui-input></label>
            <label>Link Text <uui-input .value=${attr.linkText || ''} @input=${(e) => setAttr('linkText', e.target.value)} placeholder="Terms & Conditions"></uui-input></label>
        </div>`;

    // Step: title
    if (t === 'step') return html`
        <div class="field-attrs">
            <label>Step Title <uui-input .value=${attr.title || ''} @input=${(e) => setAttr('title', e.target.value)} placeholder="Step 2: Details"></uui-input></label>
        </div>`;

    return nothing;
}

function _renderFieldCard(host, field, idx, needsOptions) {
    const f = host._editForm;
    const currentTypeLabel = host._fieldTypes.find(ft => ft.type === field.type)?.label || field.type;

    return html`
        <div class="field-card${field.isHidden ? ' field-hidden' : ''}">
            <div class="field-header">
                <span class="field-num">#${idx + 1}</span>
                <button class="type-picker-btn" @click=${() => { host._typePickerIdx = idx; host._typePickerSearch = ''; host.requestUpdate(); }}>
                    ${currentTypeLabel} &#9662;
                </button>
                <uui-toggle ?checked=${!field.isHidden} @change=${(e) => host._updateField(idx, 'isHidden', !e.target.checked)} label=${field.isHidden ? 'Hidden' : 'Visible'}></uui-toggle>
                ${field.type !== 'div' && field.type !== 'step' ? html`
                    <uui-toggle ?checked=${field.required} @change=${(e) => host._updateField(idx, 'required', e.target.checked)} label="Required"></uui-toggle>
                    <uui-toggle ?checked=${field.isSensitive || field.type === 'password'} @change=${(e) => host._updateField(idx, 'isSensitive', e.target.checked)} label="Sensitive Data"></uui-toggle>
                ` : nothing}
                <div class="field-actions">
                    <uui-button look="outline" compact @click=${() => host._moveField(idx, -1)} ?disabled=${idx === 0}>&#9650;</uui-button>
                    <uui-button look="outline" compact @click=${() => host._moveField(idx, 1)} ?disabled=${idx === f.fields.length - 1}>&#9660;</uui-button>
                    <uui-button look="outline" color="danger" compact @click=${() => host._removeField(idx)}>&#10005;</uui-button>
                </div>
            </div>
            ${field.type === 'div' || field.type === 'step' ? html`
            <div class="field-body">
                <label>CSS Class <uui-input .value=${field.cssClass || ''} @input=${(e) => host._updateField(idx, 'cssClass', e.target.value)}></uui-input></label>
                <label class="div-content-label">Content
                    <div class="richtext-toolbar">
                        <button type="button" @click=${(e) => { e.preventDefault(); document.execCommand('bold'); }}>B</button>
                        <button type="button" @click=${(e) => { e.preventDefault(); document.execCommand('italic'); }}>I</button>
                        <button type="button" @click=${(e) => { e.preventDefault(); document.execCommand('underline'); }}>U</button>
                        <button type="button" @click=${(e) => { e.preventDefault(); document.execCommand('insertUnorderedList'); }}>&#8226;</button>
                        <button type="button" @click=${(e) => { e.preventDefault(); document.execCommand('insertOrderedList'); }}>1.</button>
                        <button type="button" @click=${(e) => { e.preventDefault(); const url = prompt('Enter URL:'); if (url) document.execCommand('createLink', false, url); }}>&#128279;</button>
                        <button type="button" @click=${(e) => { e.preventDefault(); document.execCommand('formatBlock', false, 'h2'); }}>H2</button>
                        <button type="button" @click=${(e) => { e.preventDefault(); document.execCommand('formatBlock', false, 'h3'); }}>H3</button>
                        <button type="button" @click=${(e) => { e.preventDefault(); document.execCommand('formatBlock', false, 'p'); }}>P</button>
                        <button type="button" @click=${(e) => { e.preventDefault(); document.execCommand('removeFormat'); }}>&#10005;</button>
                    </div>
                    <div class="richtext-editor" contenteditable="true"
                        .innerHTML=${field.defaultValue || ''}
                        @input=${(e) => host._updateField(idx, 'defaultValue', e.target.innerHTML)}></div>
                </label>
            </div>
            ` : html`
            <div class="field-body">
                <label>Label <uui-input .value=${field.label} @input=${(e) => host._updateField(idx, 'label', e.target.value)}></uui-input></label>
                <label>Name <uui-input .value=${field.name} @input=${(e) => host._updateField(idx, 'name', e.target.value)}></uui-input></label>
                <label>Placeholder <uui-input .value=${field.placeholder || ''} @input=${(e) => host._updateField(idx, 'placeholder', e.target.value)}></uui-input></label>
                <label>CSS Class <uui-input .value=${field.cssClass || ''} @input=${(e) => host._updateField(idx, 'cssClass', e.target.value)}></uui-input></label>
                <label>Default Value <uui-input .value=${field.defaultValue || ''} @input=${(e) => host._updateField(idx, 'defaultValue', e.target.value)}></uui-input></label>
                <label>Validation Regex <uui-input .value=${field.validation || ''} @input=${(e) => host._updateField(idx, 'validation', e.target.value)}></uui-input></label>
                <label>Validation Message <uui-input .value=${field.validationMessage || ''} @input=${(e) => host._updateField(idx, 'validationMessage', e.target.value)}></uui-input></label>
            </div>
            `}
            ${_renderTypeAttributes(host, field, idx)}
            ${needsOptions(field.type) ? html`
                <div class="options-section">
                    <div class="section-header"><span>Options</span>
                        <uui-button look="outline" compact @click=${() => host._addOption(idx)}>+ Option</uui-button>
                    </div>
                    ${(field.options || []).map((opt, oIdx) => html`
                        <div class="option-row">
                            <uui-input placeholder="Text" .value=${opt.text} @input=${(e) => { opt.text = e.target.value; host.requestUpdate(); }}></uui-input>
                            <uui-input placeholder="Value" .value=${opt.value} @input=${(e) => { opt.value = e.target.value; host.requestUpdate(); }}></uui-input>
                            <uui-button look="outline" color="danger" compact @click=${() => host._removeOption(idx, oIdx)}>&#10005;</uui-button>
                        </div>`)}
                </div>` : nothing}
        </div>`;
}
