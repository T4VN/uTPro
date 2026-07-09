import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css, nothing } from '@umbraco-cms/backoffice/external/lit';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';
import { UTPRO, fetchVersionInfo, refreshVersionInfo, fetchStats, fetchCurrentUser, fetchRecentActivity, fetchMyActivity, fetchRecentTrail, fetchMyTrail } from './config.js';

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
        _myTrail: { state: true },
        _allTrail: { state: true },
        _collapsed: { state: true },
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
        this._myTrail = null;
        this._allTrail = null;
        // Per-card collapsed state. Trail cards start collapsed; activity cards expanded.
        this._collapsed = { myTrail: true, allTrail: true, myActivity: false, allActivity: false };
        this._authContext = null;

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
            this._myTrail = await fetchMyTrail(ctx);
            this._allTrail = await fetchRecentTrail(ctx);
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
        `;
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
            <div class="activity-grid">
                ${this.#trailCard('myTrail', 'Your recent trail', this._myTrail, false)}
                ${this.#trailCard('allTrail', 'All recent trail', this._allTrail, true)}
            </div>
            <div class="activity-grid">
                ${this.#activityCard('myActivity', 'Your recent activity', this._myActivity, false)}
                ${this.#activityCard('allActivity', 'All recent activity', this._allActivity, true)}
            </div>
        `;
    }

    // Toggles a card's collapsed state (new object so Lit re-renders).
    #toggleCollapse(key) {
        this._collapsed = { ...this._collapsed, [key]: !this._collapsed[key] };
    }

    // Header toggle button placed in the box's header-actions slot. Uses uui-symbol-expand,
    // an animated caret that points down when collapsed and rotates up when open.
    #collapseToggle(key) {
        const collapsed = this._collapsed[key];
        return html`
            <uui-button
                slot="header-actions"
                look="default"
                compact
                label=${collapsed ? 'Expand' : 'Collapse'}
                title=${collapsed ? 'Expand' : 'Collapse'}
                @click=${() => this.#toggleCollapse(key)}>
                <uui-symbol-expand ?open=${!collapsed}></uui-symbol-expand>
            </uui-button>`;
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
                ${this.#collapseToggle(key)}
                ${this._collapsed[key] ? nothing : (items === null
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
                        }))}
            </uui-box>
        `;
    }

    // An audit-trail card (from umbracoAudit): the raw eventType is the headline, with the
    // details/affected as a subtitle and the timestamp + optional user in the meta line.
    #trailCard(key, headline, items, showUser) {
        return html`
            <uui-box class="activity-box" headline=${headline}>
                ${this.#collapseToggle(key)}
                ${this._collapsed[key] ? nothing : (items === null
                    ? html`<div class="muted">Loading…</div>`
                    : items.length === 0
                        ? html`<div class="muted">No trail yet.</div>`
                        : items.map((a) => html`
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
                            </div>`))}
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

        /* Recent activity — two cards side by side, stacking when narrow. */
        .activity-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
            gap: 18px;
            /* Size each card to its own content so a collapsed card doesn't stretch
               to match a taller expanded sibling. */
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
