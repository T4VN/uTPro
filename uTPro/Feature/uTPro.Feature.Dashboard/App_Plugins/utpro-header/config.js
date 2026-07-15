// Shared config + data helpers for the uTPro backoffice header app and dashboard.
// All server calls go through the authenticated backoffice management API — nothing is
// exposed publicly, and the GitHub "latest release" lookup happens server-side (cached)
// so the browser never calls GitHub directly (avoids CORS / proxy / rate-limit issues).

const API_BASE = '/umbraco/management/api/v1/utpro/dashboard';

export const UTPRO = {
    logo: '/assets/img/logo.svg',
    title: 'uTPro',
    subtitle: 'Umbraco Turbo Pro',
    versionApi: `${API_BASE}/version`,                       // installed + latest + updateAvailable
    versionRefreshApi: `${API_BASE}/version?refresh=true`,   // force a fresh GitHub lookup (clears server cache)
    statsApi: `${API_BASE}/stats`,                           // site statistics
    currentUserApi: `${API_BASE}/current-user`,              // backoffice user
    recentActivityApi: `${API_BASE}/recent-activity`,        // all users' recent activity
    myActivityApi: `${API_BASE}/my-activity`,                // current user's recent activity
    recentTrailApi: `${API_BASE}/recent-trail`,              // all users' recent audit trail
    myTrailApi: `${API_BASE}/my-trail`,                      // current user's recent audit trail
    releasesUrl: 'https://github.com/T4VN/uTPro/releases',   // "Update" target
    website: 'https://github.com/T4VN/uTPro',                // "Website" target
    dashboardPath: '/umbraco/section/content/dashboard/utpro', // uTPro dashboard tab
    fallbackVersion: '1.0.0',
};

// Normalises a version/tag for display ("v17.0.4" -> "17.0.4").
export const normalize = (v) => (v || '').toString().replace(/^v/i, '').trim();

// Builds the fetch init with the backoffice bearer token from UMB_AUTH_CONTEXT.
// Returns null when no token can be obtained yet (context not ready / navigating),
// so the caller can skip the call instead of firing an unauthenticated request that 401s.
async function authInit(authContext) {
    const config = authContext?.getOpenApiConfiguration?.();
    let token;
    if (config?.token) {
        try { token = await config.token(); } catch { /* token not ready */ }
    }
    if (!token) return null;
    return {
        headers: { Accept: 'application/json', Authorization: 'Bearer ' + token },
        credentials: config?.credentials || 'same-origin',
    };
}

// Calls a management-API endpoint with auth; returns parsed JSON or null.
// If the auth token isn't available yet, the request is skipped (avoids the 401 that
// otherwise fires on tab/section navigation while the auth context is being re-provided).
async function apiSend(url, authContext, method = 'GET') {
    const init = await authInit(authContext);
    if (!init) return null;
    try {
        const resp = await fetch(url, { ...init, method });
        if (resp.ok) return await resp.json();
    } catch { /* ignore */ }
    return null;
}

const apiGet = (url, authContext) => apiSend(url, authContext, 'GET');

// Version info: installed version + latest GitHub release + whether an update is available.
// The GitHub call is made (and cached) server-side. Cached client-side for the session too,
// so the global header app doesn't re-hit /version on every tab/section navigation.
let _versionCache = null;
export async function fetchVersionInfo(authContext) {
    if (_versionCache) return _versionCache;
    const data = await apiGet(UTPRO.versionApi, authContext);
    if (!data) {
        // Auth not ready yet — return safe defaults without caching, so it retries later.
        return {
            installed: '', latest: '', updateAvailable: false,
            website: UTPRO.website, releasesUrl: UTPRO.releasesUrl,
        };
    }
    _versionCache = {
        installed: normalize(data.installed),
        latest: normalize(data.latest),
        updateAvailable: !!data.updateAvailable,
        website: data.website || UTPRO.website,
        releasesUrl: data.releasesUrl || UTPRO.releasesUrl,
    };
    return _versionCache;
}

// Forces a fresh check: clears the client-side cache and asks the server to drop its
// cached GitHub result and re-fetch the latest release immediately. Returns the same
// shape as fetchVersionInfo. Falls back to the existing/default info if the call fails.
export async function refreshVersionInfo(authContext) {
    _versionCache = null;
    const data = await apiSend(UTPRO.versionRefreshApi, authContext, 'GET');
    if (!data) {
        return _versionCache || {
            installed: '', latest: '', updateAvailable: false,
            website: UTPRO.website, releasesUrl: UTPRO.releasesUrl,
        };
    }
    _versionCache = {
        installed: normalize(data.installed),
        latest: normalize(data.latest),
        updateAvailable: !!data.updateAvailable,
        website: data.website || UTPRO.website,
        releasesUrl: data.releasesUrl || UTPRO.releasesUrl,
    };
    return _versionCache;
}

// Site statistics for the dashboard.
export async function fetchStats(authContext) {
    return apiGet(UTPRO.statsApi, authContext);
}

// Current backoffice user (name, email, 2FA, last login, password changed).
export async function fetchCurrentUser(authContext) {
    return apiGet(UTPRO.currentUserApi, authContext);
}

// Recent activity (audit trail).
export async function fetchRecentActivity(authContext) {
    return (await apiGet(UTPRO.recentActivityApi, authContext)) || [];
}
export async function fetchMyActivity(authContext) {
    return (await apiGet(UTPRO.myActivityApi, authContext)) || [];
}

// Recent audit trail (umbracoAudit): sign-in, save, user management, etc.
// Returns { range, from, to, total, series: [{date,label,count}], items: [...] } for the
// requested window (week|month|quarter|year, default month). emptyTrail() keeps the shape
// stable when auth isn't ready yet, so the dashboard can render without null checks everywhere.
const emptyTrail = (range) => ({ range, from: null, to: null, total: 0, series: [], items: [] });

export async function fetchRecentTrail(authContext, range = 'month') {
    return (await apiGet(`${UTPRO.recentTrailApi}?range=${encodeURIComponent(range)}`, authContext)) || emptyTrail(range);
}
export async function fetchMyTrail(authContext, range = 'month') {
    return (await apiGet(`${UTPRO.myTrailApi}?range=${encodeURIComponent(range)}`, authContext)) || emptyTrail(range);
}
