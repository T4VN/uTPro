// Preference store for "Preview Mode" in the Block Grid / Block List editors.
//
// Scope is PER CLUSTER: each block editor property (keyed by its property alias)
// keeps its own on/off state, so you can show the visual preview on one grid and
// compact cards on another. State is persisted per-browser in localStorage and
// shared via an Umbraco context so the cluster's toggle bar and all its block
// views stay in sync and re-render live.

import { UmbControllerBase } from '@umbraco-cms/backoffice/class-api';
import { UmbContextToken } from '@umbraco-cms/backoffice/context-api';
import { UmbObjectState } from '@umbraco-cms/backoffice/observable-api';

export const UTPRO_BLOCK_PREVIEW_TOGGLE_CONTEXT = new UmbContextToken('uTPro.BlockPreviewToggle.Context');

const STORAGE_KEY = 'utpro:blockPreview:enabledMap';

export class UtproBlockPreviewToggleContext extends UmbControllerBase {

    // { [clusterKey]: boolean }
    #state = new UmbObjectState({});
    /** Observable of the whole preference map; subscribers recompute their own key. */
    state = this.#state.asObservable();

    constructor(host) {
        super(host);

        let map = {};
        try {
            const stored = localStorage.getItem(STORAGE_KEY);
            if (stored) map = JSON.parse(stored) || {};
        } catch {
            /* localStorage unavailable / bad JSON — start empty */
        }
        this.#state.setValue(map);

        this.provideContext(UTPRO_BLOCK_PREVIEW_TOGGLE_CONTEXT, this);
    }

    /** Preview is ON by default when a cluster has no stored preference. */
    isEnabled(key) {
        const v = this.#state.getValue()?.[key ?? ''];
        return v === undefined ? true : !!v;
    }

    setEnabled(key, value) {
        const map = { ...this.#state.getValue(), [key ?? '']: !!value };
        this.#state.setValue(map);
        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify(map));
        } catch {
            /* ignore persistence failures */
        }
    }

    toggle(key) {
        this.setEnabled(key, !this.isEnabled(key));
    }
}

export { UtproBlockPreviewToggleContext as api };
export default UtproBlockPreviewToggleContext;
