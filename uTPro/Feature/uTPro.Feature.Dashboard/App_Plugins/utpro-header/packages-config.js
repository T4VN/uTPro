// Auto-discovery of sibling uTPro "apps" for the dashboard's "uTPro Apps" card.
//
// Instead of a hard-coded list, we scan the backoffice extension registry for the entry points
// that every uTPro package registers, and build a card + redirect link from each. This means a
// newly installed uTPro package (e.g. URL Viewer) shows up automatically — and disappears when
// uninstalled — with no code changes here.
//
// Two entry-point shapes are recognised (this is how the packages register themselves):
//   1. A `section`  (e.g. uTPro.Section.Form)      -> /umbraco/section/{meta.pathname}
//   2. A `menuItem` under the Settings menu         -> /umbraco/section/settings/workspace/{meta.entityType}
//      (File Manager, Audit Log, Job Monitor, URL Viewer all use this shape)

// Only extensions whose alias starts with this prefix are considered "uTPro apps".
export const UTPRO_PREFIX = 'uTPro.';

const SECTION_SETTINGS = 'Umb.Section.Settings';
const SETTINGS_MENU = 'Umb.Menu.AdvancedSettings';

// uTPro-branded extensions that are NOT navigable apps (the dashboard/header app itself).
const EXCLUDED_ALIASES = new Set([
    'uTPro.Dashboard',
    'uTPro.Dashboard.Apps',
    'uTPro.HeaderApp',
]);

const isUtpro = (m) =>
    typeof m?.alias === 'string' && m.alias.startsWith(UTPRO_PREFIX) && !EXCLUDED_ALIASES.has(m.alias);

// A uTPro section becomes an app that links to its own section.
const fromSection = (m) => {
    const pathname = m.meta?.pathname;
    if (!pathname) return null;
    return {
        key: m.alias,
        label: m.meta?.label || m.name || m.alias,
        icon: m.meta?.icon || 'icon-app',
        href: `/umbraco/section/${pathname}`,
        requiredSection: m.alias, // the section alias is the permission to check
    };
};

// A uTPro Settings menu item becomes an app that links to its root workspace.
const fromMenuItem = (m) => {
    const entityType = m.meta?.entityType;
    const menus = m.meta?.menus || [];
    if (!entityType || !menus.includes(SETTINGS_MENU)) return null;
    return {
        key: m.alias,
        label: m.meta?.label || m.name || m.alias,
        icon: m.meta?.icon || 'icon-app',
        href: `/umbraco/section/settings/workspace/${entityType}`,
        requiredSection: SECTION_SETTINGS,
    };
};

// Builds the app list from the registry's current section + menuItem manifests.
// De-duplicated by href and sorted by (localized) label happens in the element.
export function discoverApps(sectionManifests, menuItemManifests) {
    const apps = [];

    for (const m of sectionManifests || []) {
        if (!isUtpro(m)) continue;
        const app = fromSection(m);
        if (app) apps.push(app);
    }
    for (const m of menuItemManifests || []) {
        if (!isUtpro(m)) continue;
        const app = fromMenuItem(m);
        if (app) apps.push(app);
    }

    const seen = new Set();
    return apps.filter((a) => (seen.has(a.href) ? false : seen.add(a.href)));
}

// Can the given user (by allowed sections) open the app? Admins are allowed every section, so
// they always pass. allowedSections === null means "not loaded yet" -> not permitted for now.
export const canUsePackage = (app, allowedSections) =>
    Array.isArray(allowedSections) && allowedSections.includes(app.requiredSection);
