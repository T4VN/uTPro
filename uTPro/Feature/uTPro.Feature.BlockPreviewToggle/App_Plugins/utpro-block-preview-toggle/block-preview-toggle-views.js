// Per-cluster "Preview Mode" toggle for the Block Grid / Block List editors.
//
// Our custom view SUBCLASSES BlockPreview's own custom-view element (obtained from the
// already-registered custom element, not re-imported), so our element IS the preview
// element — a single, slot-managed element identical to vanilla BlockPreview. We only
// override render() to show a compact card when the cluster's toggle is OFF, and we
// inject the "Preview Mode" bar as the first row of the block layout container.
//
// (BlockPreview's own Grid/List views are unregistered in entry-point.js so only ours
// competes for the slot — see the note there.)

import { UmbLitElement } from '@umbraco-cms/backoffice/lit-element';
import { html, css } from '@umbraco-cms/backoffice/external/lit';
import { UMB_PROPERTY_CONTEXT } from '@umbraco-cms/backoffice/property';
import { UMB_BLOCK_GRID_ENTRIES_CONTEXT } from '@umbraco-cms/backoffice/block-grid';
import { UMB_BLOCK_LIST_ENTRIES_CONTEXT } from '@umbraco-cms/backoffice/block-list';
import { UTPRO_BLOCK_PREVIEW_TOGGLE_CONTEXT } from './toggle-context.js';

// ── "Preview Mode" bar ──
// Self-contained: reads the cluster key (property alias) and the toggle state from
// context, so it works both when injected into a grid/list layout container.
export class UtproPreviewBar extends UmbLitElement {
    static properties = { _on: { state: true } };

    constructor() {
        super();
        this._on = true;
        this._key = '';
        this._ctx = null;

        this.consumeContext(UMB_PROPERTY_CONTEXT, (propCtx) => {
            if (!propCtx) return;
            this.observe(propCtx.alias, (alias) => { this._key = alias ?? ''; this.#sync(); }, 'utproBarKey');
        });
        this.consumeContext(UTPRO_BLOCK_PREVIEW_TOGGLE_CONTEXT, (ctx) => {
            this._ctx = ctx;
            if (!ctx) return;
            this.observe(ctx.state, () => this.#sync(), 'utproBarState');
        });
    }

    #sync() { this._on = this._ctx ? this._ctx.isEnabled(this._key) : true; }
    #toggle() { this._ctx?.toggle(this._key); }

    render() {
        const on = this._on;
        return html`
            <div class="bar ${on ? 'is-on' : 'is-off'}">
                <uui-icon class="ico" name=${on ? 'icon-eye' : 'icon-block'}></uui-icon>
                <uui-toggle label="" ?checked=${on} @change=${this.#toggle}></uui-toggle>
                <span class="lbl">Preview Mode</span>
            </div>`;
    }

    static styles = css`
        :host { display: block; width: 100%; grid-column: 1 / -1; margin-bottom: 4px; }
        .bar {
            display: flex; align-items: center; gap: 10px;
            box-sizing: border-box; width: 100%;
            padding: 6px 12px; border-radius: 4px;
            border: 1px solid var(--uui-color-border, #d8d7d9);
            background: var(--uui-color-default, #fff);
            height: 60px
        }
        .bar.is-on {
            border-color: var(--uui-color-positive, #2bc37c);
            background: color-mix(in srgb, var(--uui-color-positive, #2bc37c) 12%, var(--uui-color-surface, #fff));
        }
        .ico { color: var(--uui-color-text-alt, #868686); }
        .bar.is-on .ico { color: var(--uui-color-positive, #2bc37c); }
        .lbl { font-size: 12px; font-weight: 700; letter-spacing: 0.08em; text-transform: uppercase; color: var(--uui-color-text, #1b264f); }
        .bar.is-off .lbl { color: var(--uui-color-text-alt, #868686); }
    `;
}
if (!customElements.get('utpro-preview-bar')) {
    customElements.define('utpro-preview-bar', UtproPreviewBar);
}

// Keep exactly one <utpro-preview-bar> as the first row of a layout container, and
// re-insert it if a re-render removes it.
function ensureBarIn(container) {
    if (!container?.isConnected) return;
    const bars = container.querySelectorAll(':scope > utpro-preview-bar');
    if (bars.length === 0) {
        container.insertBefore(document.createElement('utpro-preview-bar'), container.firstChild);
    } else {
        for (let i = 1; i < bars.length; i++) bars[i].remove();
    }
    if (!container.__utproBarObserver) {
        const observer = new MutationObserver(() => ensureBarIn(container));
        observer.observe(container, { childList: true });
        container.__utproBarObserver = observer;
    }
}

// Card renderers (OFF state). Kept as literal-tag templates because lit cannot
// interpolate a dynamic tag name.
const renderGridCard = (el) => html`
    <umb-block-grid-block class="umb-block-grid__block--view"
        .label=${el.label} .icon=${el.icon} .unpublished=${el.unpublished}
        .config=${el.config} .content=${el.content} .settings=${el.settings}></umb-block-grid-block>`;

const renderListCard = (el) => html`
    <umb-block-list-block class="umb-block-grid__block--view"
        .label=${el.label} .icon=${el.icon} .unpublished=${el.unpublished}
        .config=${el.config} .content=${el.content} .settings=${el.settings}></umb-block-list-block>`;

// Define a toggle custom view that subclasses BlockPreview's element.
//   baseTag      - the BlockPreview element to subclass (e.g. 'block-grid-preview')
//   tagName      - the tag to register our subclass under
//   entriesToken - the block editor entries context (for the bar + root detection)
//   rootOnly     - only inject the bar at the grid root (areaKey == null)
//   renderCard   - OFF-state template
function defineToggleView({ baseTag, tagName, entriesToken, rootOnly, renderCard }) {
    const Base = customElements.get(baseTag);
    if (!Base || customElements.get(tagName)) return;

    class UtproToggleView extends Base {
        static properties = { _enabled: { state: true }, _isRoot: { state: true } };

        constructor() {
            super();
            this._enabled = true;
            this._isRoot = !rootOnly; // list has no areas → always "root"
            this._key = '';
            this._toggleCtx = null;
            this._entries = null;

            this.consumeContext(UMB_PROPERTY_CONTEXT, (propCtx) => {
                if (!propCtx) return;
                this.observe(propCtx.alias, (alias) => { this._key = alias ?? ''; this.#recompute(); }, 'utproKey');
            });
            this.consumeContext(UTPRO_BLOCK_PREVIEW_TOGGLE_CONTEXT, (ctx) => {
                this._toggleCtx = ctx;
                if (!ctx) return;
                this.observe(ctx.state, () => this.#recompute(), 'utproState');
            });
            this.consumeContext(entriesToken, (entries) => {
                this._entries = entries || null;
                if (rootOnly) this._isRoot = entries?.getAreaKey?.() == null;
                this.#ensureBar();
            });
        }

        #recompute() {
            this._enabled = this._toggleCtx ? this._toggleCtx.isEnabled(this._key) : true;
        }

        #ensureBar() {
            if (!this._isRoot) return;
            const container = this._entries?.getLayoutContainerElement?.();
            if (container) ensureBarIn(container);
        }

        updated(changed) {
            super.updated?.(changed);
            this.#ensureBar();
        }

        render() {
            // ON → BlockPreview markup (super.render()); OFF → compact card.
            return this._enabled ? super.render() : renderCard(this);
        }
    }

    customElements.define(tagName, UtproToggleView);
}

function defineAll() {
    defineToggleView({
        baseTag: 'block-grid-preview',
        tagName: 'utpro-block-grid-preview-toggle',
        entriesToken: UMB_BLOCK_GRID_ENTRIES_CONTEXT,
        rootOnly: true,
        renderCard: renderGridCard,
    });
    defineToggleView({
        baseTag: 'block-list-preview',
        tagName: 'utpro-block-list-preview-toggle',
        entriesToken: UMB_BLOCK_LIST_ENTRIES_CONTEXT,
        rootOnly: false,
        renderCard: renderListCard,
    });
}

// BlockPreview's elements are defined at startup, so they normally exist by the time
// this module loads. Retry once via whenDefined in case of an unusual load order.
defineAll();
if (!customElements.get('utpro-block-grid-preview-toggle') || !customElements.get('utpro-block-list-preview-toggle')) {
    Promise.all([
        customElements.whenDefined('block-grid-preview').catch(() => {}),
        customElements.whenDefined('block-list-preview').catch(() => {}),
    ]).then(defineAll);
}
