// uTPro backoffice header app.
// Icon button on the right of the top header that opens a small popover card
// (logo, name, version, and either an Update or a Dashboard action).

import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css } from '@umbraco-cms/backoffice/external/lit';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';
import { UTPRO, fetchVersionInfo, refreshVersionInfo } from './config.js';

export class UtproHeaderAppElement extends UmbLitElement {
    static properties = {
        _current: { state: true },
        _latest: { state: true },
        _updateAvailable: { state: true },
        _checking: { state: true },
        _releasesUrl: { state: true },
    };

    constructor() {
        super();
        this._current = '';
        this._latest = '';
        this._updateAvailable = false;
        this._checking = false;
        this._releasesUrl = UTPRO.releasesUrl;
        this._authContext = null;

        this.consumeContext(UMB_AUTH_CONTEXT, async (ctx) => {
            if (!ctx) return; // context can be (re)provided as undefined while navigating — skip
            this._authContext = ctx;
            const v = await fetchVersionInfo(ctx);
            this._current = v.installed || UTPRO.fallbackVersion;
            this._latest = v.latest;
            this._updateAvailable = v.updateAvailable;
            this._releasesUrl = v.releasesUrl;
        });
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
            this._releasesUrl = v.releasesUrl;
        } finally {
            this._checking = false;
        }
    }

    render() {
        return html`
            <uui-button
                look="secondary"
                label=${UTPRO.title}
                title=${UTPRO.title}
                compact
                popovertarget="utpro-header-popover"
                class="header-btn">
                <img class="btn-logo" src=${UTPRO.logo} alt=${UTPRO.title} />
            </uui-button>

            <uui-popover-container id="utpro-header-popover" placement="bottom-end">
                <div class="popover-card">
                    <img class="card-logo" src=${UTPRO.logo} alt=${UTPRO.title} />
                    <div class="card-title">${UTPRO.title}</div>
                    <div class="card-subtitle">${UTPRO.subtitle}</div>
                    <div class="card-version">version ${this._current || '—'}</div>

                    ${this._updateAvailable
                        ? html`
                            <div class="card-update">New version ${this._latest} available</div>
                            <uui-button
                                look="primary"
                                color="positive"
                                href=${this._releasesUrl}
                                target="_blank"
                                label="Update">
                                Update
                            </uui-button>`
                        : html`
                            <div class="card-uptodate">You're up to date</div>
                            <uui-button
                                look="secondary"
                                @click=${this.#checkForUpdate}
                                ?disabled=${this._checking}
                                label="Check for Update">
                                ${this._checking ? 'Checking…' : 'Check for Update'}
                            </uui-button>
                            <uui-button
                                look="primary"
                                color="default"
                                href=${UTPRO.dashboardPath}
                                label="Dashboard">
                                Dashboard
                            </uui-button>`}
                </div>
            </uui-popover-container>
        `;
    }

    static styles = css`
        :host {
            display: inline-flex;
            align-items: center;
        }
        .header-btn {
            --uui-button-height: 36px;
            --uui-button-background-color: var(--uui-color-surface, #fff);
            --uui-button-background-color-hover: #f3f3f3;
            --uui-button-border-radius: 8px;
            border-radius: 100%;
            overflow: hidden;
        }
        .btn-logo {
            height: 22px;
            width: auto;
            display: block;
        }
        .popover-card {
            display: flex;
            flex-direction: column;
            align-items: center;
            gap: 6px;
            width: 220px;
            padding: 20px 16px 16px;
            background: var(--uui-color-surface, #fff);
            border-radius: var(--uui-border-radius, 3px);
            box-shadow: var(--uui-shadow-depth-3, 0 8px 22px rgba(0, 0, 0, 0.2));
            text-align: center;
        }
        .card-logo {
            height: 48px;
            width: auto;
            margin-bottom: 4px;
        }
        .card-title {
            font-size: 1.1rem;
            font-weight: 700;
            letter-spacing: 0.04em;
        }
        .card-subtitle {
            font-size: 0.7rem;
            letter-spacing: 0.14em;
            color: var(--uui-color-text-alt, #868686);
        }
        .card-version {
            font-size: 0.8rem;
            color: var(--uui-color-text-alt, #868686);
            margin-bottom: 10px;
        }
        .card-update {
            font-size: 0.8rem;
            font-weight: 600;
            color: var(--uui-color-positive, #2bc37c);
            margin-bottom: 8px;
        }
        .card-uptodate {
            font-size: 0.8rem;
            font-weight: 600;
            color: var(--uui-color-positive, #2bc37c);
            margin-bottom: 8px;
        }
        .popover-card uui-button {
            width: 100%;
        }
    `;
}

customElements.define('utpro-header-app', UtproHeaderAppElement);
export default UtproHeaderAppElement;
