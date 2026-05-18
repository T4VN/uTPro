/**
 * Cookie Consent Banner
 * ---------------------
 * Lightweight, no-dependency cookie consent implementation.
 * Stores user preference in a first-party cookie for 365 days.
 *
 * Cookie name: "cookie_consent"
 * Values: "accepted" | "rejected"
 *
 * Usage:
 *   if (window.CookieConsent.allowed()) {
 *       // load Google Analytics, Facebook Pixel, etc.
 *   }
 *
 * Auto-blocking third-party scripts:
 *   Add data-cookie-consent to any script tag you want to block until accepted:
 *   <script type="text/plain" data-cookie-consent src="https://www.googletagmanager.com/gtag/js?id=G-XXX"></script>
 *   <script type="text/plain" data-cookie-consent>
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

    var COOKIE_NAME = 'cookie_consent';
    var COOKIE_DAYS = 365;

    function getCookie(name) {
        var match = document.cookie.match(new RegExp('(^| )' + name + '=([^;]+)'));
        return match ? match[2] : null;
    }

    function setCookie(name, value, days) {
        var expires = '';
        if (days) {
            var date = new Date();
            date.setTime(date.getTime() + days * 24 * 60 * 60 * 1000);
            expires = '; expires=' + date.toUTCString();
        }
        document.cookie = name + '=' + value + expires + '; path=/; SameSite=Lax';
    }

    function hideBanner() {
        var banner = document.getElementById('cookieConsent');
        if (banner) {
            banner.classList.remove('is-visible');
        }
    }

    /**
     * Activate all blocked scripts (type="text/plain" with data-cookie-consent).
     * Clones each script tag with correct type so the browser executes it.
     */
    function activateBlockedScripts() {
        var blocked = document.querySelectorAll('script[data-cookie-consent]');
        for (var i = 0; i < blocked.length; i++) {
            var original = blocked[i];
            var newScript = document.createElement('script');

            // Copy all attributes except type
            for (var j = 0; j < original.attributes.length; j++) {
                var attr = original.attributes[j];
                if (attr.name !== 'type' && attr.name !== 'data-cookie-consent') {
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
        setCookie(COOKIE_NAME, 'accepted', COOKIE_DAYS);
        hideBanner();
        activateBlockedScripts();
    }

    function onReject() {
        setCookie(COOKIE_NAME, 'rejected', COOKIE_DAYS);
        hideBanner();
    }

    function init() {
        // If user already accepted, activate blocked scripts
        if (getCookie(COOKIE_NAME) === 'accepted') {
            activateBlockedScripts();
            return;
        }

        // If user already rejected, do nothing
        if (getCookie(COOKIE_NAME) === 'rejected') {
            return;
        }

        // No choice yet — show banner
        var banner = document.getElementById('cookieConsent');
        if (!banner) return;

        banner.classList.add('is-visible');

        var acceptBtn = document.getElementById('cookieAccept');
        var rejectBtn = document.getElementById('cookieReject');

        if (acceptBtn) {
            acceptBtn.addEventListener('click', onAccept);
        }
        if (rejectBtn) {
            rejectBtn.addEventListener('click', onReject);
        }
    }

    // ── Public API ──
    window.CookieConsent = {
        /** Returns true if user accepted cookies */
        allowed: function () {
            return getCookie(COOKIE_NAME) === 'accepted';
        },
        /** Returns true if user rejected cookies */
        rejected: function () {
            return getCookie(COOKIE_NAME) === 'rejected';
        },
        /** Returns true if user hasn't made a choice yet */
        pending: function () {
            return getCookie(COOKIE_NAME) === null;
        },
        /** Reset preference (shows banner again on next page load) */
        reset: function () {
            setCookie(COOKIE_NAME, '', -1);
        }
    };

    // Run when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
