import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css, nothing } from '@umbraco-cms/backoffice/external/lit';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';
import { UMB_NOTIFICATION_CONTEXT } from '@umbraco-cms/backoffice/notification';
import { umbExtensionsRegistry } from '@umbraco-cms/backoffice/extension-registry';
import { UMB_CURRENT_USER_CONTEXT } from '@umbraco-cms/backoffice/current-user';
import { UTPRO, fetchVersionInfo, refreshVersionInfo, fetchStats, fetchCurrentUser, fetchRecentActivity, fetchMyActivity, fetchRecentTrail, fetchMyTrail, createSite } from './config.js';
import { discoverApps, canUsePackage } from './packages-config.js';
// Vendored locally (MIT) — no CDN/runtime external call, matching config.js' philosophy.
import { Chart } from './vendor/frappe-charts.min.esm.js';

// "08 Jul 2026 04:36"
const fmtDate = (value) => {
    if (!value) return '—';
    const d = new Date(value);
    if (isNaN(d.getTime())) return '—';
    return d.toLocaleString('en-GB', {
        day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit',
    }).replace(',', '');
};

// Maps Umbraco audit log headers to a friendly label + uui-tag colour.
const ACTION_BADGES = {
    save: { label: 'Saved', color: 'default' },
    savevariant: { label: 'Saved', color: 'default' },
    new: { label: 'Created', color: 'positive' },
    publish: { label: 'Published', color: 'positive' },
    publishvariant: { label: 'Published', color: 'positive' },
    sendtopublish: { label: 'Sent to publish', color: 'default' },
    sendtopublishvariant: { label: 'Sent to publish', color: 'default' },
    unpublish: { label: 'Unpublished', color: 'warning' },
    unpublishvariant: { label: 'Unpublished', color: 'warning' },
    rollback: { label: 'Rolled back', color: 'warning' },
    move: { label: 'Moved', color: 'default' },
    copy: { label: 'Copied', color: 'default' },
    sort: { label: 'Sorted', color: 'default' },
    delete: { label: 'Deleted', color: 'danger' },
};
const actionInfo = (type) => ACTION_BADGES[(type || '').toLowerCase()] || { label: type || 'Action', color: 'default' };

// Friendly label for the audited entity type (e.g. "Document" -> "Content").
const ENTITY_LABELS = {
    document: 'Content',
    media: 'Media',
    member: 'Member',
    dictionaryitem: 'Dictionary',
    template: 'Template',
    datatype: 'Data Type',
    contenttype: 'Document Type',
    mediatype: 'Media Type',
    language: 'Language',
    user: 'User',
    usergroup: 'User Group',
};
const entityTypeLabel = (entityType) => ENTITY_LABELS[(entityType || '').toLowerCase()] || entityType;

// Time windows for the audit-trail chart. Values must match the server (TrailRanges).
const TRAIL_RANGES = [
    { value: 'week', name: 'This week' },
    { value: 'month', name: 'This month' },
    { value: 'quarter', name: 'Last 3 months' },
    { value: 'year', name: 'This year' },
];
const TRAIL_RANGE_LABEL = Object.fromEntries(TRAIL_RANGES.map((r) => [r.value, r.name]));

// Built from the server-provided website/releases URLs (single source of truth: the repo
// configured on the backend), with the static Umbraco docs link.
const buildResources = (website, releasesUrl) => [
    { title: 'Documentation', desc: 'Guides and reference for uTPro packages.', href: website },
    { title: 'Releases', desc: 'Changelog and the latest downloads.', href: releasesUrl },
    { title: 'Report an issue', desc: 'Found a bug? Let us know on GitHub.', href: website + '/issues' },
    { title: 'Umbraco', desc: 'Official Umbraco documentation.', href: 'https://docs.umbraco.com' },
];

export class UtproDashboardElement extends UmbLitElement {
    static properties = {
        _current: { state: true },
        _latest: { state: true },
        _updateAvailable: { state: true },
        _checking: { state: true },
        _website: { state: true },
        _releasesUrl: { state: true },
        _stats: { state: true },
        _user: { state: true },
        _myActivity: { state: true },
        _allActivity: { state: true },
        _trailAll: { state: true },
        _trailMy: { state: true },
        _trailScope: { state: true },
        _trailRange: { state: true },
        _detailsOpen: { state: true },
        _sectionExts: { state: true },
        _menuExts: { state: true },
        _allowedSections: { state: true },
        _showCreate: { state: true },
        _siteName: { state: true },
        _creating: { state: true },
    };

    constructor() {
        super();
        this._current = '';
        this._latest = '';
        this._updateAvailable = false;
        this._checking = false;
        this._website = UTPRO.website;
        this._releasesUrl = UTPRO.releasesUrl;
        this._stats = null;
        this._user = null;
        this._myActivity = null;
        this._allActivity = null;
        // Single trail card: the chart always compares both scopes (All vs You) as grouped
        // bars; the scope toggle only drives the detail list + summary below. Both datasets are
        // loaded together per window, so toggling scope needs no refetch.
        this._trailAll = null;
        this._trailMy = null;
        this._trailScope = 'all';
        this._trailRange = 'month';
        // The detail list (scope tabs + rows) starts hidden; the "Details" caret reveals it.
        this._detailsOpen = false;
        this._authContext = null;
        // Live Frappe Chart instance for the trail card.
        this._chart = null;

        // "uTPro Apps" card. We auto-discover sibling uTPro packages by watching the extension
        // registry for the entry points they register (sections + Settings menu items). Cards
        // appear/disappear as packages are installed/removed — no hard-coded list, no server call.
        this._sectionExts = [];
        this._menuExts = [];
        this._allowedSections = null;

        this.observe(umbExtensionsRegistry.byType('section'), (exts) => {
            this._sectionExts = exts ?? [];
        }, 'utpro-sections');

        this.observe(umbExtensionsRegistry.byType('menuItem'), (exts) => {
            this._menuExts = exts ?? [];
        }, 'utpro-menu-items');

        // The current user's allowed sections drive per-package permission (admins get all).
        this.consumeContext(UMB_CURRENT_USER_CONTEXT, (ctx) => {
            if (!ctx) return;
            this.observe(ctx.currentUser, (user) => {
                this._allowedSections = user?.allowedSections ?? [];
            }, 'utpro-current-user');
        });

        // "Create Site" dialog state.
        this._showCreate = false;
        this._siteName = '';
        this._creating = false;
        this._notificationContext = null;
        this.consumeContext(UMB_NOTIFICATION_CONTEXT, (ctx) => { this._notificationContext = ctx; });

        // All data comes from the authenticated management API, so load once auth is ready.
        this.consumeContext(UMB_AUTH_CONTEXT, async (ctx) => {
            if (!ctx) return; // context can be (re)provided as undefined while navigating — skip
            this._authContext = ctx;
            const v = await fetchVersionInfo(ctx);
            this._current = v.installed || UTPRO.fallbackVersion;
            this._latest = v.latest;
            this._updateAvailable = v.updateAvailable;
            this._website = v.website;
            this._releasesUrl = v.releasesUrl;
            this._stats = await fetchStats(ctx);
            this._user = await fetchCurrentUser(ctx);
            this._myActivity = await fetchMyActivity(ctx);
            this._allActivity = await fetchRecentActivity(ctx);
            this.#loadTrail();
        });
    }

    #row(label, value, kind = '') {
        return html`
            <div class="row">
                <span class="row-label">${label}</span>
                <span class="row-value ${kind}">${value ?? '—'}</span>
            </div>`;
    }

    // Clears the server + client cache and re-checks GitHub for the latest release.
    async #checkForUpdate() {
        if (this._checking || !this._authContext) return;
        this._checking = true;
        try {
            const v = await refreshVersionInfo(this._authContext);
            this._current = v.installed || UTPRO.fallbackVersion;
            this._latest = v.latest;
            this._updateAvailable = v.updateAvailable;
            this._website = v.website;
            this._releasesUrl = v.releasesUrl;
        } finally {
            this._checking = false;
        }
    }

    // "Check for Update" button, shown when the site is already up to date.
    #checkButton(look = 'secondary') {
        return html`
            <uui-button
                look=${look}
                @click=${this.#checkForUpdate}
                ?disabled=${this._checking}
                label="Check for Update">
                ${this._checking ? 'Checking…' : 'Check for Update'}
            </uui-button>`;
    }

    render() {
        return html`
            <div class="dash">
                <div class="layout">
                    <div class="main">${this.#renderMain()}</div>
                    <div class="side">${this.#renderSide()}</div>
                </div>
            </div>
            ${this._showCreate ? this.#createModal() : nothing}
        `;
    }

    #notify(color, message) {
        this._notificationContext?.peek(color, { data: { message } });
    }

    #openCreate() {
        this._siteName = '';
        this._showCreate = true;
    }

    #closeCreate() {
        if (this._creating) return; // don't close mid-request
        this._showCreate = false;
    }

    async #submitCreate() {
        const name = (this._siteName || '').trim();
        if (!name) {
            this.#notify('warning', 'Please enter a site name.');
            return;
        }
        if (!this._authContext) {
            this.#notify('danger', 'Not ready yet — try again.');
            return;
        }
        this._creating = true;
        try {
            const res = await createSite(this._authContext, name);
            if (res.ok && res.body?.success) {
                this.#notify('positive', `Site "${name}" created.`);
                this._showCreate = false;
                await this.#reloadContentTree();
            } else {
                this.#notify('danger', res.body?.error || 'Could not create the site.');
            }
        } catch (e) {
            console.error(e);
            this.#notify('danger', 'Create request failed.');
        } finally {
            this._creating = false;
        }
    }

    // Asks the backoffice to reload the Content tree root's children so the new site node
    // appears immediately (the node was created via the management API, which the client-side
    // tree doesn't know about otherwise). Best-effort: uses dynamic imports so a backoffice API
    // change can only disable auto-reload, never break the dashboard from loading.
    async #reloadContentTree() {
        try {
            const [{ UMB_ACTION_EVENT_CONTEXT }, { UmbRequestReloadChildrenOfEntityEvent }] = await Promise.all([
                import('@umbraco-cms/backoffice/action'),
                import('@umbraco-cms/backoffice/entity-action'),
            ]);
            const ctx = await this.getContext(UMB_ACTION_EVENT_CONTEXT);
            if (!ctx) return;
            ctx.dispatchEvent(new UmbRequestReloadChildrenOfEntityEvent({
                entityType: 'document-root',
                unique: null,
            }));
        } catch (e) {
            console.warn('uTPro: could not auto-reload the Content tree.', e);
        }
    }

    // Lightweight modal dialog for entering the new site name.
    #createModal() {
        return html`
            <div class="modal-backdrop" @click=${this.#closeCreate}>
                <div class="modal" @click=${(e) => e.stopPropagation()}>
                    <h3 class="modal-title">Create Site</h3>
                    <p class="modal-desc">
                        Creates a new site skeleton in Content:
                        <strong>${this._siteName || 'SiteName'}</strong> › Sites › Navigation Link.
                    </p>
                    <uui-input
                        label="Site name"
                        placeholder="e.g. My New Site"
                        .value=${this._siteName}
                        ?disabled=${this._creating}
                        @input=${(e) => this._siteName = e.target.value}
                        @keydown=${(e) => { if (e.key === 'Enter') this.#submitCreate(); }}>
                    </uui-input>
                    <div class="modal-actions">
                        <uui-button look="secondary" label="Cancel"
                            ?disabled=${this._creating} @click=${this.#closeCreate}>Cancel</uui-button>
                        <uui-button look="primary" color="positive" label="Create"
                            ?disabled=${this._creating} @click=${this.#submitCreate}>
                            ${this._creating ? 'Creating…' : 'Create'}
                        </uui-button>
                    </div>
                </div>
            </div>`;
    }

    // ── Left column: main content ──
    #renderMain() {
        return html`
            <uui-box class="hero-box">
                <div class="hero">
                    <img class="hero-logo" src=${UTPRO.logo} alt=${UTPRO.title} />
                    <div>
                        <div class="hero-title">Welcome to ${UTPRO.subtitle}</div>
                        <p class="hero-desc">
                            uTPro bundles a set of productivity packages for Umbraco — form building,
                            file management and audit logging — with a single place to see status,
                            versions and updates. Use the panels on the right for a quick health check.
                        </p>
                        <div class="hero-actions">
                            ${this._updateAvailable
                                ? html`<uui-button look="primary" color="positive"
                                        href=${this._releasesUrl} target="_blank" label="Update">
                                        Update to ${this._latest}
                                    </uui-button>`
                                : html`
                                    <uui-tag color="positive" look="secondary">Up to date</uui-tag>
                                    ${this.#checkButton()}`}
                            <uui-button look="secondary" href=${this._website} target="_blank" label="Website">
                                Website
                            </uui-button>
                            <uui-button look="outline" color="default" label="Create Site"
                                @click=${this.#openCreate}>
                                <uui-icon name="icon-add"></uui-icon> Create Site
                            </uui-button>
                        </div>
                    </div>
                </div>
                <hr>
                <div class="res-grid">
                    ${buildResources(this._website, this._releasesUrl).map((r) => html`
                        <a class="res-tile" href=${r.href} target="_blank" rel="noopener">
                            <span class="res-title">${r.title}</span>
                            <span class="res-desc">${r.desc}</span>
                        </a>`)}
                </div>
            </uui-box>
            ${this.#appsCard()}
            ${this.#trailCard()}
            <div class="activity-grid">
                ${this.#activityCard('myActivity', 'Your recent activity', this._myActivity, false)}
                ${this.#activityCard('allActivity', 'All recent activity', this._allActivity, true)}
            </div>
        `;
    }

    // Resolves a manifest label that may be an inline dictionary token (e.g. "#simpleForm_title").
    #label(value) {
        if (!value) return '';
        return typeof value === 'string' && value.startsWith('#')
            ? this.localize.string(value)
            : value;
    }

    // uTPro apps this user should see: auto-discovered from the registry AND permitted for this
    // user (admins pass every section). Sorted by their localized label.
    #visiblePackages() {
        return discoverApps(this._sectionExts, this._menuExts)
            .filter((app) => canUsePackage(app, this._allowedSections))
            .map((app) => ({ ...app, label: this.#label(app.label) }))
            .sort((a, b) => a.label.localeCompare(b.label));
    }

    // "uTPro Apps" card: quick links to the sibling uTPro packages that are installed and that
    // this user has access to. Hidden entirely when there's nothing to show, so it never appears
    // as an empty box.
    #appsCard() {
        const items = this.#visiblePackages();
        if (items.length === 0) return nothing;
        return html`
            <uui-box class="apps-box" headline="uTPro Apps">
                <uui-icon slot="header-actions" name="icon-app"></uui-icon>
                <div class="apps-grid">
                    ${items.map((app) => html`
                        <a class="app-tile" href=${app.href} title=${'Open ' + app.label}>
                            <div class="app-head">
                                <uui-icon class="app-icon" name=${app.icon}></uui-icon>
                                <span class="app-title">${app.label}</span>
                            </div>
                            <span class="app-open">Open <uui-icon name="icon-arrow-right"></uui-icon></span>
                        </a>`)}
                </div>
            </uui-box>`;
    }

    // Backoffice edit URL for a content/media node (null for other entity types).
    #nodeHref(entityType, nodeKey) {
        if (!nodeKey) return null;
        const t = (entityType || '').toLowerCase();
        if (t === 'document') return `/umbraco/section/content/workspace/document/edit/${nodeKey}`;
        if (t === 'media') return `/umbraco/section/media/workspace/media/edit/${nodeKey}`;
        if (t === 'dictionaryitem') return `/umbraco/section/translation/workspace/dictionary/edit/${nodeKey}`;
        return null;
    }

    // A "recent activity" card. items === null while loading; [] when empty.
    #activityCard(key, headline, items, showUser) {
        return html`
            <uui-box class="activity-box" headline=${headline}>
                ${items === null
                    ? html`<div class="muted">Loading…</div>`
                    : items.length === 0
                        ? html`<div class="muted">No activity yet.</div>`
                        : items.map((a) => {
                            const href = this.#nodeHref(a.entityType, a.nodeKey);
                            const b = actionInfo(a.type);
                            return html`
                            <div class="act">
                                <div class="act-main">
                                    <div class="act-action">
                                        ${a.node
                                            ? (href
                                                ? html`<a href=${href}>${a.node}</a>`
                                                : html`<strong>${a.node}</strong>`)
                                            : html`<span class="act-detail">${a.action}</span>`}
                                    </div>
                                    <div class="act-meta">
                                        ${fmtDate(a.date)}${showUser && a.user ? html` · ${a.user}` : ''}${a.entityType ? html` · <span class="act-type">${entityTypeLabel(a.entityType)}</span>` : ''}
                                    </div>
                                </div>
                                <uui-tag look="secondary" color=${b.color} class="act-tag">${b.label}</uui-tag>
                            </div>`;
                        })}
            </uui-box>
        `;
    }

    // Fetches both scopes (all + current user) for the window in parallel — the chart compares
    // them, and the list uses whichever scope is active.
    async #loadTrail() {
        if (!this._authContext) return;
        const [all, my] = await Promise.all([
            fetchRecentTrail(this._authContext, this._trailRange),
            fetchMyTrail(this._authContext, this._trailRange),
        ]);
        this._trailAll = all;
        this._trailMy = my;
    }

    // Range <uui-select> change: remember the window, show loading and refetch both scopes.
    #onRangeChange(event) {
        const range = event.target.value;
        if (!range || range === this._trailRange) return;
        this._trailRange = range;
        this._trailAll = null;
        this._trailMy = null;
        this.#loadTrail();
    }

    // Scope toggle (All / Yours): only switches the detail list + summary. Both datasets are
    // already loaded, so no refetch is needed.
    #onScopeChange(scope) {
        if (scope === this._trailScope) return;
        this._trailScope = scope;
    }

    // Shows/hides the detail section (scope tabs + rows) under the chart.
    #toggleDetails() {
        this._detailsOpen = !this._detailsOpen;
    }

    // After each render, (re)draw the Frappe bar chart. Frappe renders imperatively into a real
    // DOM node, so we can't express it declaratively in the template.
    updated(changed) {
        super.updated?.(changed);
        this.#syncTrailChart();
    }

    // Frappe's BaseChart attaches a window 'resize' listener per instance and exposes no
    // destroy(), so recreating on every render would leak listeners. The chart shows two
    // grouped-bar datasets (All vs You), so the signature ignores the scope toggle (which only
    // affects the list) and only rebuilds when the data or the host actually change.
    #syncTrailChart() {
        const host = this.renderRoot?.querySelector('#chart-trail');
        if (!host) return; // loading or empty state — nothing to draw into
        const all = this._trailAll;
        const my = this._trailMy;
        if (!all || !my) return;

        const allSeries = all.series ?? [];
        if (allSeries.length === 0) return;

        // Both scopes share the same range → identical buckets, so align 'my' counts by index.
        const labels = allSeries.map((s) => s.label);
        const allValues = allSeries.map((s) => s.count);
        const myValues = labels.map((_, i) => my.series?.[i]?.count ?? 0);

        const sig = `${all.range}:${allValues.join(',')}|${myValues.join(',')}`;
        if (host.dataset.sig === sig && host.firstElementChild) return; // already current

        host.textContent = '';
        host.dataset.sig = sig;

        // Two theme colours so the chart tracks light/dark mode: accent for All, a warm tone for You.
        const cs = getComputedStyle(this);
        const colorAll = cs.getPropertyValue('--uui-color-selected').trim() || '#3544b1';
        const colorMy = cs.getPropertyValue('--uui-color-warning').trim() || '#f0ad4e';

        this._chart = new Chart(host, {
            type: 'bar',
            height: 220,
            animate: true,
            colors: [colorAll, colorMy],
            axisOptions: { xAxisMode: 'tick', xIsSeries: true },
            barOptions: { spaceRatio: allSeries.length > 20 ? 0.3 : 0.6 },
            tooltipOptions: {
                formatTooltipY: (v) => `${v} event${v === 1 ? '' : 's'}`,
            },
            data: {
                labels,
                datasets: [
                    { name: 'All', values: allValues },
                    { name: 'You', values: myValues },
                ],
            },
        });
    }

    // The single audit-trail card (from umbracoAudit). A scope toggle (All / Yours) + a range
    // picker drive one bar chart summarising the window, followed by the newest detail rows.
    #trailCard() {
        const scope = this._trailScope;
        const showUser = scope === 'all';
        // The list + summary reflect the active scope; the chart (below) always shows both.
        const data = scope === 'my' ? this._trailMy : this._trailAll;
        const loading = this._trailAll === null || this._trailMy === null;

        // Clickable "Details" row with a caret. Reveals/hides the scope tabs + list below.
        const detailsHead = html`
            <button
                class="trail-list-head"
                type="button"
                aria-expanded=${this._detailsOpen ? 'true' : 'false'}
                title=${this._detailsOpen ? 'Hide details' : 'Show details'}
                @click=${() => this.#toggleDetails()}>
                <span class="trail-list-label">Details</span>
                <uui-symbol-expand ?open=${this._detailsOpen}></uui-symbol-expand>
            </button>`;

        // Only shown while the details are open: the scope tabs + the detail rows.
        const detailsBody = html`
            <uui-button-group class="trail-scope">
                <uui-button
                    look=${scope === 'all' ? 'primary' : 'default'}
                    label="All" @click=${() => this.#onScopeChange('all')}>All</uui-button>
                <uui-button
                    look=${scope === 'my' ? 'primary' : 'default'}
                    label="Yours" @click=${() => this.#onScopeChange('my')}>Yours</uui-button>
            </uui-button-group>
            ${(data?.items?.length)
                ? html`<div class="trail-list">
                    ${data.items.map((a) => html`
                        <div class="act">
                            <div class="act-main">
                                <div class="act-action">
                                    <span class="act-detail">${a.details || a.affected || a.type}</span>
                                </div>
                                <div class="act-meta">
                                    ${fmtDate(a.date)}${showUser && a.user ? html` · ${a.user}` : ''}
                                </div>
                            </div>
                            <uui-tag look="secondary" color="default" class="act-tag">${a.type}</uui-tag>
                        </div>`)}
                </div>`
                : html`<div class="muted">No trail yet.</div>`}`;

        const rangeSelect = html`
            <uui-select
                slot="header-actions"
                label="Range"
                title="Time range"
                .options=${TRAIL_RANGES.map((r) => ({ ...r, selected: r.value === this._trailRange }))}
                @change=${(e) => this.#onRangeChange(e)}>
            </uui-select>`;

        // The chart shows both scopes, so it appears whenever either has any events.
        const hasSeries = (this._trailAll?.series?.some((s) => s.count > 0))
            || (this._trailMy?.series?.some((s) => s.count > 0));
        const allTotal = this._trailAll?.total ?? 0;
        const myTotal = this._trailMy?.total ?? 0;
        const body = loading
            ? html`<div class="muted">Loading…</div>`
            : html`
                <div class="trail-summary">
                    <span class="sum sum-all"><strong>${allTotal}</strong> All</span>
                    <span class="sum sum-my"><strong>${myTotal}</strong> You</span>
                    <span class="sum-range">${TRAIL_RANGE_LABEL[this._trailRange] || this._trailRange}</span>
                </div>
                ${hasSeries
                    ? html`<div id="chart-trail" class="chart-host"></div>`
                    : html`<div class="muted chart-empty">No events in this period.</div>`}
                ${detailsHead}
                ${this._detailsOpen ? detailsBody : nothing}`;

        return html`
            <uui-box class="activity-box" headline="Recent trail">
                ${rangeSelect}
                ${body}
            </uui-box>
        `;
    }

    // ── Right column: stacked info cards ──
    #renderSide() {
        const s = this._stats;
        const u = this._user;
        return html`
            <uui-box class="card" headline="Product Information">
                <uui-icon slot="header-actions" name="icon-brackets"></uui-icon>
                ${this.#row('Product Name', UTPRO.subtitle)}
                ${this.#row('Short Name', UTPRO.title)}
                ${this.#row('Installed version', this._current || '—')}
                ${this.#row('Latest release', this._latest || 'unknown')}
                <div class="card-actions">
                    ${this._updateAvailable
                        ? html`<uui-button look="primary" color="positive"
                                href=${this._releasesUrl} target="_blank" label="Update">
                                Update to ${this._latest}
                            </uui-button>`
                        : this.#checkButton()}
                </div>
                <hr>
                ${this.#row('Platform & Dependencies', html`<uui-icon name="icon-chip"></uui-icon>`)}
                ${this.#row('Runtime environment', s ? s.runtimeVersion : '…')}
                ${this.#row('Base CMS', s ? 'Umbraco ' + s.umbracoVersion : '…')}
            </uui-box>

            <uui-box class="card" headline="Site">
                <uui-icon slot="header-actions" name="icon-globe"></uui-icon>
                ${this.#row('Published content items',
                    html`<uui-tag color="positive">${s ? s.publishedContent : '…'}</uui-tag>`)}
                ${this.#row('Content in recycle bin',
                    html`<uui-tag color="danger">${s ? s.contentInRecycleBin : '…'}</uui-tag>`)}
                ${this.#row('Media in recycle bin',
                    html`<uui-tag color="danger">${s ? s.mediaInRecycleBin : '…'}</uui-tag>`)}
                ${this.#row('Users',
                    html`<uui-tag color="positive">${s ? s.usersTotal : '…'}</uui-tag>`)}
                ${this.#row('Disabled users',
                    html`<uui-tag color=${s && s.usersDisabled ? 'warning' : 'default'}>${s ? s.usersDisabled : '…'}</uui-tag>`)}
                ${this.#row('Members',
                    html`<uui-tag color="positive">${s ? s.membersTotal : '…'}</uui-tag>`)}
                ${this.#row('Disabled members',
                    html`<uui-tag color=${s && s.membersDisabled ? 'warning' : 'default'}>${s ? s.membersDisabled : '…'}</uui-tag>`)}
            </uui-box>

            <uui-box class="card" headline="Your Details">
                <uui-icon slot="header-actions" name="icon-user"></uui-icon>
                ${u
                    ? html`
                        ${this.#row('User', u.name)}
                        ${this.#row('2FA', u.twoFactorEnabled ? 'Enabled' : 'Disabled')}
                        ${this.#row('Email', u.email)}
                        ${this.#row('Last login', fmtDate(u.lastLoginDate))}
                        ${this.#row('Password changed', fmtDate(u.lastPasswordChangeDate))}`
                    : html`<div class="muted">Loading…</div>`}
            </uui-box>
        `;
    }

    static styles = css`
        :host {
            display: block;
            padding: var(--uui-size-layout-1, 24px);
        }
        .dash-header {
            display: flex;
            align-items: center;
            gap: 12px;
            background: var(--uui-color-surface, #fff);
            border-radius: var(--uui-border-radius, 3px);
            padding: 14px 18px;
            box-shadow: var(--uui-shadow-depth-1, 0 1px 3px rgba(0, 0, 0, 0.12));
            margin-bottom: 18px;
        }
        .dash-logo { height: 30px; width: auto; }
        .dash-heading { display: flex; align-items: baseline; gap: 8px; }
        .dash-title { font-size: 1.15rem; font-weight: 700; }
        .dash-sep { color: var(--uui-color-border, #d8d7d9); }
        .dash-sub { font-size: 0.8rem; letter-spacing: 0.16em; color: var(--uui-color-text-alt, #868686); }

        /* Two columns: main (left) + fixed-width sidebar (right), stacking on narrow screens. */
        .layout {
            display: grid;
            grid-template-columns: minmax(0, 1fr) 340px;
            gap: 18px;
            align-items: start;
        }
        @media (max-width: 1100px) {
            .layout { grid-template-columns: minmax(0, 1fr); }
        }
        .main, .side {
            display: flex;
            flex-direction: column;
            gap: 18px;
            min-width: 0;
        }

        /* Hero */
        .hero { display: flex; align-items: center; gap: 18px; }
        .hero-logo { height: 56px; width: auto; flex: none; }
        .hero-title { font-size: 1.3rem; font-weight: 700; }
        .hero-desc { margin: 6px 0 0; color: var(--uui-color-text-alt, #868686); line-height: 1.5; }
        .hero-actions { display: flex; flex-wrap: wrap; align-items: center; gap: 10px; margin-top: 14px; }

        /* Resources tiles */
        .res-grid {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
            gap: 12px;
        }
        .res-tile {
            display: flex;
            flex-direction: column;
            gap: 4px;
            padding: 14px;
            border: 1px solid var(--uui-color-divider, #eee);
            border-radius: var(--uui-border-radius, 3px);
            text-decoration: none;
            color: inherit;
            transition: background 0.1s ease;
        }
        .res-tile:hover { background: var(--uui-color-surface-alt, #f7f7f7); }
        .res-title { font-weight: 700; }
        .res-desc { font-size: 0.85rem; color: var(--uui-color-text-alt, #868686); }

        /* uTPro Apps — clickable tiles linking to installed sibling packages. */
        .apps-grid {
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
            gap: 12px;
        }
        .app-tile {
            display: flex;
            flex-direction: column;
            gap: 6px;
            padding: 14px;
            border: 1px solid var(--uui-color-divider, #eee);
            border-radius: var(--uui-border-radius, 3px);
            text-decoration: none;
            color: inherit;
            transition: background 0.1s ease, border-color 0.1s ease;
        }
        .app-tile:hover {
            background: var(--uui-color-surface-alt, #f7f7f7);
            border-color: var(--uui-color-border-emphasis, #c4c4c4);
        }
        .app-head { display: flex; align-items: center; gap: 8px; }
        .app-icon { font-size: 1.2rem; color: var(--uui-color-interactive, #3544b1); }
        .app-title { font-weight: 700; }
        .app-open {
            display: inline-flex;
            align-items: center;
            gap: 4px;
            margin-top: 4px;
            font-size: 0.8rem;
            font-weight: 600;
            color: var(--uui-color-interactive, #3544b1);
        }

        /* Create Site modal */
        .modal-backdrop {
            position: fixed;
            inset: 0;
            background: rgba(0, 0, 0, 0.4);
            display: flex;
            align-items: center;
            justify-content: center;
            z-index: 1000;
        }
        .modal {
            background: var(--uui-color-surface, #fff);
            border-radius: var(--uui-border-radius, 3px);
            box-shadow: var(--uui-shadow-depth-3, 0 8px 24px rgba(0, 0, 0, 0.25));
            padding: 24px;
            width: min(460px, 92vw);
        }
        .modal-title { margin: 0 0 6px; font-size: 1.15rem; font-weight: 700; }
        .modal-desc { margin: 0 0 14px; color: var(--uui-color-text-alt, #868686); line-height: 1.5; }
        .modal uui-input { width: 100%; }
        .modal-actions { display: flex; justify-content: flex-end; gap: 10px; margin-top: 18px; }

        /* Recent activity — two cards side by side, stacking when narrow. */
        .activity-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
            gap: 18px;
            /* Size each card to its own content so a shorter card doesn't stretch
               to match a taller sibling. */
            align-items: start;
        }
        .act {
            display: flex;
            align-items: flex-start;
            justify-content: space-between;
            gap: 10px;
            padding: 8px 0;
            border-bottom: 1px solid var(--uui-color-divider, #eee);
        }
        .act:last-of-type { border-bottom: none; }
        .act-main { min-width: 0; flex: 1; }
        .act-action { font-size: 0.9rem; }
        .act-tag { flex: none; }
        .act-action a {
            color: var(--uui-color-interactive, #3544b1);
            font-weight: 600;
            text-decoration: none;
        }
        .act-action a:hover { text-decoration: underline; }
        .act-meta { font-size: 0.75rem; color: var(--uui-color-text-alt, #868686); margin-top: 2px; }
        .act-type { font-weight: 600; }

        /* Audit-trail chart */
        .trail-summary {
            display: flex;
            align-items: center;
            flex-wrap: wrap;
            gap: 6px 14px;
            font-size: 0.85rem;
            color: var(--uui-color-text-alt, #868686);
            margin-bottom: 4px;
        }
        .trail-summary strong { color: var(--uui-color-text, #1b264f); font-size: 1rem; }
        /* Colour dots matching the chart datasets (All = accent, You = warning). */
        .sum { display: inline-flex; align-items: center; gap: 6px; }
        .sum::before {
            content: '';
            width: 10px;
            height: 10px;
            border-radius: 2px;
            flex: none;
        }
        .sum-all::before { background: var(--uui-color-selected, #3544b1); }
        .sum-my::before { background: var(--uui-color-warning, #f0ad4e); }
        .sum-range { color: var(--uui-color-text-alt, #868686); }
        .chart-host { width: 100%; min-height: 200px; }
        .chart-empty { text-align: center; padding: 24px 0; }
        /* Clickable "Details" row (a caret toggles the section below). */
        .trail-list-head {
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 10px;
            width: 100%;
            margin-top: 12px;
            padding: 10px 0 0;
            border: none;
            border-top: 1px solid var(--uui-color-divider, #eee);
            background: none;
            font: inherit;
            color: inherit;
            cursor: pointer;
            text-align: left;
        }
        .trail-list-label { font-weight: 700; color: var(--uui-color-text, #1b264f); }
        .trail-scope { margin-top: 10px; }
        .trail-list { margin-top: 6px; }
        /* Keep the range picker compact in the box header. */
        uui-select[slot='header-actions'] { font-size: 0.8rem; }

        /* Frappe Charts styles (v1.6.2, MIT). The library injects its CSS into document.head,
           which can't reach this shadow root, so the needed rules live here and use theme
           variables so the chart follows light/dark mode. */
        .chart-container {
            position: relative;
            font-family: inherit;
        }
        .chart-container .axis,
        .chart-container .chart-label { fill: var(--uui-color-text-alt, #868686); }
        .chart-container .axis line,
        .chart-container .chart-label line { stroke: var(--uui-color-divider, #eee); }
        .chart-container line.dashed { stroke-dasharray: 5, 3; }
        .chart-container .axis-line .specific-value { text-anchor: start; }
        .chart-container .axis-line .y-line { text-anchor: end; }
        .chart-container .axis-line .x-line { text-anchor: middle; }
        .chart-container .legend-dataset-text { fill: var(--uui-color-text, #1b264f); font-weight: 600; }
        .graph-svg-tip {
            position: absolute;
            z-index: 99999;
            padding: 10px;
            font-size: 12px;
            color: #959da5;
            text-align: center;
            background: rgba(0, 0, 0, 0.85);
            border-radius: 3px;
        }
        .graph-svg-tip ul { padding-left: 0; display: flex; margin: 0; }
        .graph-svg-tip ul.data-point-list li { min-width: 90px; flex: 1; font-weight: 600; }
        .graph-svg-tip strong { color: #dfe2e5; font-weight: 600; }
        .graph-svg-tip .svg-pointer {
            position: absolute;
            height: 5px;
            margin: 0 0 0 -5px;
            border: 5px solid transparent;
            border-top-color: rgba(0, 0, 0, 0.85);
        }

        .card { width: 100%; }

        .row {
            display: flex;
            align-items: center;
            justify-content: space-between;
            gap: 12px;
            padding: 10px 0;
            border-bottom: 1px solid var(--uui-color-divider, #eee);
        }
        .row:last-of-type { border-bottom: none; }
        .row-label { color: var(--uui-color-text, #1b264f); }
        .row-value { font-weight: 600; text-align: right; word-break: break-word; }

        .card-actions {
            display: flex;
            flex-wrap: wrap;
            align-items: center;
            gap: 10px;
            margin-top: 14px;
        }
        .muted { color: var(--uui-color-text-alt, #868686); padding: 8px 0; }
    `;
}

customElements.define('utpro-dashboard', UtproDashboardElement);
export default UtproDashboardElement;
