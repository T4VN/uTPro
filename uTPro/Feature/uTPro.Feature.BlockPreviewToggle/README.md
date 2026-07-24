# uTPro.Feature.BlockPreviewToggle

A per-cluster **"Preview Mode"** switch for [Umbraco.Community.BlockPreview](https://marketplace.umbraco.com/package/umbraco.community.blockpreview) in the backoffice.

Each **Block Grid / Block List** property shows a full-width **Preview Mode** bar as the first row of its layout. Toggle it to switch how that cluster renders its blocks:

- **ON** — the full visual server-rendered preview (BlockPreview).
- **OFF** — a compact block card (icon + label + content type).

The toggle is **per cluster** (keyed by property alias) and remembered per browser (`localStorage`). No settings or code changes are needed at edit time.

## Requirements

- Umbraco 17 (net10). Frontend-only — no server code.
- `Umbraco.Community.BlockPreview` installed and enabled (this feature toggles *its* output).

## How it works

- `block-preview-toggle-views.js` registers two `blockEditorCustomView`s (grid + list) that **subclass** BlockPreview's own element (`block-grid-preview` / `block-list-preview`). Our element *is* the preview element (single, slot-managed), so `render()` just switches between the inherited preview (ON) and a compact card (OFF). It also injects the `<utpro-preview-bar>` into the layout container (grid: root only, so nested areas don't get their own bar).
- `toggle-context.js` is a `globalContext` holding the per-cluster on/off map (persisted to `localStorage`) and shared with all block views + bars.
- `entry-point.js` runs at startup and **unregisters BlockPreview's own Grid/List custom views** so only ours competes for the block editor's single-view slot. This avoids the editor re-instantiating the block view during initial mount, which otherwise triggered BlockPreview's stylesheet race (`getOrCreateStylesheet` unhandled rejection). A narrow rejection guard remains as a safety net.

## Extensions

| Type | Purpose |
|------|---------|
| `backofficeEntryPoint` | Prunes BlockPreview's Grid/List views + rejection safety net. |
| `globalContext` | Per-cluster on/off preference map (localStorage-backed). |
| `blockEditorCustomView` (grid) | Grid preview/card switch + Preview Mode bar. |
| `blockEditorCustomView` (list) | List preview/card switch + Preview Mode bar. |

## Project layout & runtime

This project has no C#. It exists to organise the backoffice assets in the solution and to be ready to split into a standalone package later. It is **not** referenced by `uTPro.Project.Web`; instead the web app syncs `App_Plugins/**` on build via the `CopyUtproBlockPreviewToggleAppPlugins` MSBuild target (same pattern as `uTPro.Feature.Dashboard`).

## Maintenance notes

- If a future `Umbraco.Community.BlockPreview` release renames its custom-view aliases, update `BLOCKPREVIEW_VIEWS_WE_REPLACE` in `entry-point.js` (`BlockPreview.GridCustomView`, `BlockPreview.ListCustomView`).
- Backoffice JS/manifests are read at app startup and cached by the browser — after changes, restart the site and hard-refresh (DevTools → Network → *Disable cache*).
