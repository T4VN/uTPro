/**
 * uTPro Auto Translation - Workspace Action for Document & Media
 * 
 * Umbraco 16 workspace action that translates text fields from the default
 * language to the current variant language.
 */

const API_BASE = '/umbraco/api/utpro/auto-translation';

export class AutoTranslateWorkspaceAction {

  host;
  args;

  constructor(host, args) {
    this.host = host;
    this.args = args;
  }

  /**
   * Called by Umbraco when the workspace action button is clicked.
   */
  async execute() {
    console.log('[AutoTranslation] Execute called');

    try {
      // Get auth token
      const token = await this._getToken();
      console.log('[AutoTranslation] Token obtained:', !!token);

      // Get current workspace info from URL
      const workspaceInfo = this._getWorkspaceInfoFromUrl();
      console.log('[AutoTranslation] Workspace info:', workspaceInfo);

      if (!workspaceInfo.unique) {
        alert('Auto Translation: Could not determine the current item. Please save the item first.');
        return;
      }

      if (!workspaceInfo.culture) {
        alert('Auto Translation: Please switch to a non-default language variant first (use the language selector at the top right).');
        return;
      }

      // Call the API to translate the persisted content
      const apiType = workspaceInfo.entityType === 'media' ? 'media' : 'content';
      const url = `${API_BASE}/${apiType}/${workspaceInfo.unique}?targetCulture=${encodeURIComponent(workspaceInfo.culture)}`;

      console.log('[AutoTranslation] Calling API:', url);

      const headers = { 'Content-Type': 'application/json' };
      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }

      const response = await fetch(url, { method: 'GET', headers, credentials: 'same-origin' });

      if (!response.ok) {
        const errorText = await response.text();
        console.error('[AutoTranslation] API error:', response.status, errorText);
        alert(`Auto Translation failed: ${response.status} - ${errorText}`);
        return;
      }

      const result = await response.json();
      console.log('[AutoTranslation] API result:', result);

      if (result && result.values && Object.keys(result.values).length > 0) {
        const count = Object.keys(result.values).length;
        const valuesText = Object.entries(result.values)
          .filter(([_, val]) => val)
          .map(([key, val]) => `• ${key}: ${val?.substring(0, 80)}`)
          .join('\n');

        alert(`✅ Auto Translation completed!\n\nTranslated & saved ${count} field(s):\n${result.sourceCulture} → ${result.targetCulture}\n\nReloading page to show translated values...`);

        // Reload the page to show the saved translated values
        window.location.reload();
      } else {
        alert('Auto Translation: No translatable text fields found in the default language, or the item has no saved content yet.\n\nMake sure you have saved the item with content in the default language first.');
      }

    } catch (error) {
      console.error('[AutoTranslation] Error:', error);
      alert(`Auto Translation error: ${error.message}`);
    }
  }

  /**
   * Get the auth token from Umbraco's auth system.
   */
  async _getToken() {
    try {
      // Method 1: Try getContext (UmbControllerHostElement)
      if (this.host && typeof this.host.getContext === 'function') {
        try {
          const authContext = await this.host.getContext('UMB_AUTH_CONTEXT');
          if (authContext && authContext.getOpenApiToken) {
            const token = await authContext.getOpenApiToken();
            if (token) return token;
          }
        } catch (e) {
          console.warn('[AutoTranslation] getContext failed:', e);
        }
      }

      // Method 2: Try consumeContext (callback-based)
      if (this.host && typeof this.host.consumeContext === 'function') {
        const token = await new Promise((resolve) => {
          let resolved = false;
          this.host.consumeContext('UMB_AUTH_CONTEXT', (authContext) => {
            if (resolved) return;
            resolved = true;
            if (authContext && authContext.getOpenApiToken) {
              authContext.getOpenApiToken().then(resolve).catch(() => resolve(null));
            } else {
              resolve(null);
            }
          });
          setTimeout(() => { if (!resolved) { resolved = true; resolve(null); } }, 3000);
        });
        if (token) return token;
      }

      // Method 3: Try to find auth token from Umbraco's stored token
      // Umbraco 16 stores the token in sessionStorage or via OIDC client
      const storedKeys = Object.keys(sessionStorage);
      for (const key of storedKeys) {
        if (key.includes('oidc') || key.includes('token') || key.includes('auth')) {
          try {
            const data = JSON.parse(sessionStorage.getItem(key));
            if (data && data.access_token) {
              return data.access_token;
            }
          } catch (e) { /* not JSON */ }
        }
      }

      // Method 4: Try localStorage
      const localKeys = Object.keys(localStorage);
      for (const key of localKeys) {
        if (key.includes('oidc') || key.includes('token') || key.includes('auth')) {
          try {
            const data = JSON.parse(localStorage.getItem(key));
            if (data && data.access_token) {
              return data.access_token;
            }
          } catch (e) { /* not JSON */ }
        }
      }

    } catch (e) {
      console.warn('[AutoTranslation] Could not get auth token:', e);
    }
    return null;
  }

  /**
   * Extract workspace info from the current URL hash/path.
   */
  _getWorkspaceInfoFromUrl() {
    // Full URL path (Umbraco 16 uses path-based routing, not hash)
    const fullPath = window.location.pathname + window.location.hash;
    const info = { unique: null, entityType: 'document', culture: null };

    console.log('[AutoTranslation] Full path:', fullPath);

    // Pattern: /workspace/document/edit/{guid}/{culture}/...
    // Pattern: /workspace/media/edit/{guid}/{culture}/...
    // GUID can be 32-36 chars with dashes
    const editMatch = fullPath.match(/\/workspace\/(\w+)\/edit\/([a-f0-9-]+?)\/([a-z]{2}-[A-Z]{2})/i);
    if (editMatch) {
      info.entityType = editMatch[1]; // 'document' or 'media'
      info.unique = editMatch[2];
      info.culture = editMatch[3];
      console.log('[AutoTranslation] Parsed from path:', info);
      return info;
    }

    // Fallback: try to get GUID without culture
    const guidMatch = fullPath.match(/\/edit\/([a-f0-9-]+)/i);
    if (guidMatch) {
      info.unique = guidMatch[1];
    }

    if (fullPath.includes('/media/')) {
      info.entityType = 'media';
    }

    // Try to get culture from URL segments
    const cultureMatch = fullPath.match(/\/([a-z]{2}-[A-Z]{2})\//);
    if (cultureMatch) {
      info.culture = cultureMatch[1];
    }

    // Fallback: query params
    if (!info.culture) {
      const urlParams = new URLSearchParams(window.location.search);
      info.culture = urlParams.get('culture') || urlParams.get('variantId');
    }

    console.log('[AutoTranslation] Parsed info:', info);
    return info;
  }
}

export { AutoTranslateWorkspaceAction as api };
export default AutoTranslateWorkspaceAction;
