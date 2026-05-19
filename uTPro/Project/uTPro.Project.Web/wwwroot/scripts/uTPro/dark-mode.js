/**
 * Dark Mode Toggle
 * Persists user preference in localStorage and respects system preference.
 */
(function () {
    'use strict';

    var STORAGE_KEY = 'uTPro-theme';

    function getPreferredTheme() {
        var stored = localStorage.getItem(STORAGE_KEY);
        if (stored) return stored;
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    }

    function setTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem(STORAGE_KEY, theme);
    }

    function toggleTheme(e) {
        e.preventDefault();
        e.stopPropagation();
        var current = document.documentElement.getAttribute('data-theme') || 'light';
        setTheme(current === 'dark' ? 'light' : 'dark');
    }

    // Apply theme immediately
    setTheme(getPreferredTheme());

    // Bind click events
    function bindToggles() {
        var toggles = document.querySelectorAll('.dark-mode-toggle');
        for (var i = 0; i < toggles.length; i++) {
            if (!toggles[i].dataset.bound) {
                toggles[i].dataset.bound = '1';
                toggles[i].addEventListener('click', toggleTheme);
            }
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        bindToggles();

        // Inject toggle into mobile navPanel when it appears
        var observer = new MutationObserver(function () {
            var navPanel = document.getElementById('navPanel');
            if (navPanel && !navPanel.querySelector('.dark-mode-toggle')) {
                var link = document.createElement('a');
                link.href = '#';
                link.className = 'dark-mode-toggle';
                link.title = 'Toggle Dark Mode';
                link.setAttribute('aria-label', 'Toggle Dark Mode');
                link.innerHTML = '<span class="dmt-slider"><span class="dmt-knob"></span></span>';
                navPanel.appendChild(link);
                bindToggles();
            }
        });
        observer.observe(document.body, { childList: true, subtree: true });
    });

    // Listen for system preference changes
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function (e) {
        if (!localStorage.getItem(STORAGE_KEY)) {
            setTheme(e.matches ? 'dark' : 'light');
        }
    });
})();
