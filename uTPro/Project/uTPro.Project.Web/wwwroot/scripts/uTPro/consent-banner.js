/**
 * Consent Banner
 * ---------------------
 * Lightweight, no-dependency consent implementation.
 * Stores user preference in localStorage.
 *
 * Storage key: "consent_preference"
 * Values: "accepted" | "rejected"
 *
 * Usage:
 *   if (window.ConsentBanner.allowed()) {
 *       // load Google Analytics, Facebook Pixel, etc.
 *   }
 *
 * Auto-blocking third-party scripts:
 *   Add data-consent to any script tag you want to block until accepted:
 *   <script type="text/plain" data-consent src="https://www.googletagmanager.com/gtag/js?id=G-XXX"></script>
 *   <script type="text/plain" data-consent>
 *       fbq('init', '123456');
 *   </script>
 *
 *   These scripts will:
 *   - NOT execute if user rejected or hasn't chosen yet
 *   - Execute automatically when user clicks "Accept"
 *   - Execute on page load if user previously accepted
 */
(function () {
    'use strict';

    var STORAGE_KEY = 'consent_preference';

    function getConsent() {
        try {
            return localStorage.getItem(STORAGE_KEY);
        } catch (e) {
            return null;
        }
    }

    function setConsent(value) {
        try {
            localStorage.setItem(STORAGE_KEY, value);
        } catch (e) {
            // localStorage unavailable (private browsing, etc.)
        }
    }

    function hideBanner() {
        var banner = document.getElementById('consentBanner');
        if (banner) {
            banner.classList.remove('is-visible');
        }
    }

    /**
     * Activate all blocked scripts (type="text/plain" with data-consent).
     * Clones each script tag with correct type so the browser executes it.
     */
    function activateBlockedScripts() {
        var blocked = document.querySelectorAll('script[data-consent]');
        for (var i = 0; i < blocked.length; i++) {
            var original = blocked[i];
            var newScript = document.createElement('script');

            // Copy all attributes except type and data-consent
            for (var j = 0; j < original.attributes.length; j++) {
                var attr = original.attributes[j];
                if (attr.name !== 'type' && attr.name !== 'data-consent') {
                    newScript.setAttribute(attr.name, attr.value);
                }
            }

            // Copy inline content if any
            if (original.textContent) {
                newScript.textContent = original.textContent;
            }

            // Replace original with executable version
            original.parentNode.replaceChild(newScript, original);
        }
    }

    function onAccept() {
        setConsent('accepted');
        hideBanner();
        activateBlockedScripts();
    }

    function onReject() {
        setConsent('rejected');
        hideBanner();
    }

    function init() {
        var consent = getConsent();

        // If user already accepted, activate blocked scripts
        if (consent === 'accepted') {
            activateBlockedScripts();
            return;
        }

        // If user already rejected, do nothing
        if (consent === 'rejected') {
            return;
        }

        // No choice yet — show banner
        var banner = document.getElementById('consentBanner');
        if (!banner) return;

        banner.classList.add('is-visible');

        var acceptBtn = document.getElementById('consentAccept');
        var rejectBtn = document.getElementById('consentReject');

        if (acceptBtn) {
            acceptBtn.addEventListener('click', onAccept);
        }
        if (rejectBtn) {
            rejectBtn.addEventListener('click', onReject);
        }
    }

    // ── Public API ──
    window.ConsentBanner = {
        /** Returns true if user accepted */
        allowed: function () {
            return getConsent() === 'accepted';
        },
        /** Returns true if user rejected */
        rejected: function () {
            return getConsent() === 'rejected';
        },
        /** Returns true if user hasn't made a choice yet */
        pending: function () {
            return getConsent() === null;
        },
        /** Reset preference (shows banner again on next page load) */
        reset: function () {
            try { localStorage.removeItem(STORAGE_KEY); } catch (e) {}
        }
    };

    // Run when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
