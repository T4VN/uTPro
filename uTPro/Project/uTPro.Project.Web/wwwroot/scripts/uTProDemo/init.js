// Remove is-preload immediately after paint (don't wait for jQuery)
window.addEventListener('load', function () {
    setTimeout(function () { document.body.classList.remove('is-preload') }, 100);
});

(function () {
    var loaded = false;
    function loadScripts() {
        if (loaded) return; loaded = true;
        var scripts = [
            '/assets/scripts/jquery.min.js',
            '/assets/scripts/jquery.dropotron.min.js',
            '/assets/scripts/browser.min.js',
            '/assets/scripts/breakpoints.min.js',
            '/scripts/uTPro/util.js',
            '/scripts/uTPro/main.js',
            '/scripts/uTPro/select-language.js'
        ];

        if (window.lstRenderScriptQueue && window.lstRenderScriptQueue.length > 0) {
            scripts.push.apply(scripts, window.lstRenderScriptQueue);
        }
        function loadNext(i) {
            if (i >= scripts.length) return;
            var s = document.createElement('script');
            s.src = scripts[i]; s.defer = true;
            s.onload = function () { loadNext(i + 1) };
            document.body.appendChild(s);
        }
        loadNext(0);
    }
    // Trigger on first interaction OR after 3s (whichever comes first)
    ['mousedown', 'touchstart', 'keydown', 'scroll'].forEach(function (e) {
        document.addEventListener(e, loadScripts, { once: true, passive: true });
    });
    if ('requestIdleCallback' in window) {
        requestIdleCallback(loadScripts, { timeout: 3000 });
    } else {
        setTimeout(loadScripts, 3000);
    }
})();