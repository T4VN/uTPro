// Loaded at backoffice startup (backofficeEntryPoint).
//
// Root cause of the console error
//   "Cannot read properties of undefined (reading 'getOrCreateStylesheet')"
// is that TWO blockEditorCustomView extensions match a Block Grid/List block: ours
// (weight 1000) and Umbraco.Community.BlockPreview's own. When BlockPreview registers
// its view (asynchronously, in its own entry point) the editor's single-view slot
// re-instantiates the block view, tearing down a BlockPreview element while it is
// still fetching stylesheets → the unhandled rejection.
//
// Fix: keep exactly ONE custom view for grid/list. Our views subclass BlockPreview's
// element (so the visual preview is identical), so we unregister BlockPreview's own
// Grid/List custom views. We leave BlockPreview's Single-block and Rich-text views
// untouched. As a belt-and-suspenders, we also swallow that specific rejection.

import { umbExtensionsRegistry } from '@umbraco-cms/backoffice/extension-registry';

const BLOCKPREVIEW_VIEWS_WE_REPLACE = [
    'BlockPreview.GridCustomView',
    'BlockPreview.ListCustomView',
];

function aliasOf(item) {
    return item?.alias ?? item?.manifest?.alias ?? '';
}

function prune() {
    try {
        const observable = umbExtensionsRegistry.byType('blockEditorCustomView');
        observable.subscribe((list) => {
            if (!Array.isArray(list)) return;
            for (const alias of BLOCKPREVIEW_VIEWS_WE_REPLACE) {
                if (list.some((m) => aliasOf(m) === alias)) {
                    try { umbExtensionsRegistry.unregister(alias); } catch { /* ignore */ }
                }
            }
        });
    } catch { /* registry API not available — rely on the rejection guard below */ }
}

function isBlockPreviewStylesheetRace(reason) {
    const msg = typeof reason === 'string' ? reason : (reason?.message ?? '');
    const stack = reason?.stack ?? '';
    return msg.includes('getOrCreateStylesheet') && /block-preview/i.test(stack + ' ' + msg);
}

function installGuard() {
    if (window.__utproBlockPreviewGuard) return;
    window.__utproBlockPreviewGuard = true;

    window.addEventListener('unhandledrejection', (e) => {
        if (isBlockPreviewStylesheetRace(e?.reason)) {
            e.preventDefault();
            e.stopImmediatePropagation();
        }
    });
    window.addEventListener('error', (e) => {
        if (isBlockPreviewStylesheetRace(e?.error ?? e?.message)) {
            e.preventDefault();
            e.stopImmediatePropagation();
        }
    }, true);

    prune();
}

installGuard();

export const onInit = () => installGuard();
export default onInit;
