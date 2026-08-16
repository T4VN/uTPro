(function() {
    // Mobile sidebar toggle
    var sidebar = document.getElementById('sidebar');
    var toggle = document.getElementById('menu-toggle');
    var closeBtn = document.getElementById('sidebar-close');

    if (toggle) {
        toggle.addEventListener('click', function(e) {
            e.stopPropagation();
            sidebar.classList.add('open');
        });
    }
    if (closeBtn) {
        closeBtn.addEventListener('click', function() { sidebar.classList.remove('open'); });
    }
    document.addEventListener('click', function(e) {
        if (sidebar && sidebar.classList.contains('open') && !sidebar.contains(e.target) && !toggle.contains(e.target)) {
            sidebar.classList.remove('open');
        }
    });

    // Close sidebar when clicking a nav link (mobile)
    if (sidebar) {
        var sidebarLinks = sidebar.querySelectorAll('a');
        for (var i = 0; i < sidebarLinks.length; i++) {
            sidebarLinks[i].addEventListener('click', function() {
                if (window.innerWidth <= 900) {
                    sidebar.classList.remove('open');
                }
            });
        }
    }

    // Nav group toggle
    var toggleProject = document.getElementById('toggle-project');
    var toggleFeatures = document.getElementById('toggle-features');
    var listProject = document.getElementById('nav-list-project');
    var listFeatures = document.getElementById('nav-list-features');

    function activateGroup(activeToggle, activeList, inactiveToggle, inactiveList) {
        if (activeToggle.classList.contains('active')) return;
        activeToggle.classList.add('active');
        activeToggle.setAttribute('aria-expanded', 'true');
        activeList.style.display = '';
        inactiveToggle.classList.remove('active');
        inactiveToggle.setAttribute('aria-expanded', 'false');
        inactiveList.style.display = 'none';
    }

    if (toggleProject && toggleFeatures) {
        toggleProject.addEventListener('click', function() {
            activateGroup(toggleProject, listProject, toggleFeatures, listFeatures);
        });
        toggleFeatures.addEventListener('click', function() {
            activateGroup(toggleFeatures, listFeatures, toggleProject, listProject);
        });
    }

    // Auto-generate TOC from h2 headings
    var tocNav = document.getElementById('toc-nav');
    var tocMobileNav = document.getElementById('toc-mobile-nav');
    var tocSidebar = document.getElementById('toc-sidebar');
    var tocMobile = document.getElementById('toc-mobile');
    var headings = document.querySelectorAll('.doc-content h2');

    if (tocNav && headings.length > 0) {
        var tocHtml = '';
        for (var h = 0; h < headings.length; h++) {
            if (!headings[h].id) {
                headings[h].id = headings[h].textContent
                    .trim()
                    .toLowerCase()
                    .replace(/[^\w\s-]/g, '')
                    .replace(/\s+/g, '-');
            }
            tocHtml += '<a href="#' + headings[h].id + '" class="toc-link">' + headings[h].textContent.trim() + '</a>';
        }
        tocNav.innerHTML = tocHtml;
        if (tocMobileNav) tocMobileNav.innerHTML = tocHtml;

        // Highlight active TOC on scroll
        var tocLinks = tocNav.querySelectorAll('.toc-link');
        var tocMobileLinks = tocMobileNav ? tocMobileNav.querySelectorAll('.toc-link') : [];

        function getScrollParent(el) {
            var parent = el.parentElement;
            while (parent) {
                var style = window.getComputedStyle(parent);
                if (style.overflowY === 'auto' || style.overflowY === 'scroll') {
                    return parent;
                }
                parent = parent.parentElement;
            }
            return null;
        }

        var scrollContainer = getScrollParent(headings[0]);

        function updateActiveToc() {
            var currentId = '';
            for (var i = 0; i < headings.length; i++) {
                var rect = headings[i].getBoundingClientRect();
                if (rect.top <= 100) {
                    currentId = headings[i].id;
                }
            }
            var activeText = '';
            for (var j = 0; j < tocLinks.length; j++) {
                if (tocLinks[j].getAttribute('href') === '#' + currentId) {
                    tocLinks[j].classList.add('active');
                    activeText = tocLinks[j].textContent;
                } else {
                    tocLinks[j].classList.remove('active');
                }
            }
            for (var k = 0; k < tocMobileLinks.length; k++) {
                if (tocMobileLinks[k].getAttribute('href') === '#' + currentId) {
                    tocMobileLinks[k].classList.add('active');
                } else {
                    tocMobileLinks[k].classList.remove('active');
                }
            }
            // Update mobile toggle text with active section name
            var mobileToggleText = document.querySelector('#toc-mobile-toggle span');
            if (mobileToggleText) {
                mobileToggleText.textContent = activeText || 'On this page';
            }
            // Update URL hash to match active section
            if (currentId && window.location.hash !== '#' + currentId) {
                history.replaceState(null, '', '#' + currentId);
            }
        }

        var rafPending = false;
        function onScroll() {
            if (!rafPending) {
                rafPending = true;
                requestAnimationFrame(function() {
                    updateActiveToc();
                    rafPending = false;
                });
            }
        }

        if (scrollContainer) {
            scrollContainer.addEventListener('scroll', onScroll, { passive: true });
        }
        window.addEventListener('scroll', onScroll, { passive: true });
        document.addEventListener('scroll', onScroll, { passive: true });

        // On load: activate TOC item matching URL hash and scroll to it
        var hash = window.location.hash;
        if (hash) {
            for (var hi = 0; hi < tocLinks.length; hi++) {
                if (tocLinks[hi].getAttribute('href') === hash) {
                    tocLinks[hi].classList.add('active');
                } else {
                    tocLinks[hi].classList.remove('active');
                }
            }
            for (var hm = 0; hm < tocMobileLinks.length; hm++) {
                if (tocMobileLinks[hm].getAttribute('href') === hash) {
                    tocMobileLinks[hm].classList.add('active');
                } else {
                    tocMobileLinks[hm].classList.remove('active');
                }
            }
            // Scroll to the target element
            var targetEl = document.getElementById(hash.substring(1));
            if (targetEl) {
                setTimeout(function() {
                    targetEl.scrollIntoView({ behavior: 'auto', block: 'start' });
                    // Re-run after scroll settles to ensure correct active state
                    setTimeout(updateActiveToc, 200);
                }, 50);
            }
        } else {
            updateActiveToc();
        }

        // Active on click
        function handleTocClick(e) {
            var href = e.currentTarget.getAttribute('href');
            for (var j = 0; j < tocLinks.length; j++) {
                if (tocLinks[j].getAttribute('href') === href) {
                    tocLinks[j].classList.add('active');
                } else {
                    tocLinks[j].classList.remove('active');
                }
            }
            for (var k = 0; k < tocMobileLinks.length; k++) {
                if (tocMobileLinks[k].getAttribute('href') === href) {
                    tocMobileLinks[k].classList.add('active');
                } else {
                    tocMobileLinks[k].classList.remove('active');
                }
            }
        }
        for (var cl = 0; cl < tocLinks.length; cl++) {
            tocLinks[cl].addEventListener('click', handleTocClick);
        }
        for (var cm = 0; cm < tocMobileLinks.length; cm++) {
            tocMobileLinks[cm].addEventListener('click', handleTocClick);
        }

        // Mobile TOC toggle
        var mobileToggle = document.getElementById('toc-mobile-toggle');
        if (mobileToggle && tocMobileNav) {
            mobileToggle.addEventListener('click', function() {
                var expanded = mobileToggle.getAttribute('aria-expanded') === 'true';
                mobileToggle.setAttribute('aria-expanded', String(!expanded));
                tocMobileNav.style.display = expanded ? 'none' : 'block';
            });
            var mobileLinks = tocMobileNav.querySelectorAll('a');
            for (var m = 0; m < mobileLinks.length; m++) {
                mobileLinks[m].addEventListener('click', function() {
                    mobileToggle.setAttribute('aria-expanded', 'false');
                    tocMobileNav.style.display = 'none';
                });
            }
        }
    } else {
        // No headings — hide TOC
        if (tocSidebar) tocSidebar.style.display = 'none';
        if (tocMobile) tocMobile.style.display = 'none';
    }

    // Doc search
    var searchInputs = [];
    var ds = document.getElementById('doc-search');
    var dsr = document.getElementById('search-results');
    var dsm = document.getElementById('doc-search-mobile');
    var dsmr = document.getElementById('search-results-mobile');
    var hs = document.getElementById('hero-search');
    var hsr = document.getElementById('hero-search-results');
    if (ds && dsr) searchInputs.push({ input: ds, results: dsr });
    if (dsm && dsmr) searchInputs.push({ input: dsm, results: dsmr });
    if (hs && hsr) searchInputs.push({ input: hs, results: hsr });

    if (searchInputs.length > 0) {
        var searchData = null;

        function loadSearchData(callback) {
            if (searchData) { callback(searchData); return; }
            fetch('/search.json').then(function(res) { return res.json(); }).then(function(data) {
                searchData = data;
                callback(data);
            }).catch(function() {
                searchData = [];
                callback([]);
            });
        }

        function highlightMatch(text, query) {
            var idx = text.toLowerCase().indexOf(query.toLowerCase());
            if (idx === -1) return text.substring(0, 80) + '...';
            var start = Math.max(0, idx - 30);
            var end = Math.min(text.length, idx + query.length + 50);
            var snippet = (start > 0 ? '...' : '') + text.substring(start, end) + (end < text.length ? '...' : '');
            return snippet.replace(new RegExp('(' + query.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + ')', 'gi'), '<mark>$1</mark>');
        }

        function doSearch(query, resultsEl) {
            if (query.length < 2) {
                resultsEl.style.display = 'none';
                resultsEl.innerHTML = '';
                return;
            }
            loadSearchData(function(data) {
                var q = query.toLowerCase();
                var results = [];
                for (var i = 0; i < data.length && results.length < 8; i++) {
                    if (data[i].title.toLowerCase().indexOf(q) !== -1 || data[i].content.toLowerCase().indexOf(q) !== -1) {
                        results.push(data[i]);
                    }
                }
                if (results.length === 0) {
                    resultsEl.innerHTML = '<div class="search-no-result">No results found</div>';
                    resultsEl.style.display = 'block';
                    return;
                }
                var html = '';
                for (var r = 0; r < results.length; r++) {
                    html += '<a href="' + results[r].url + '" class="search-result-item">' +
                        '<div class="search-result-title">' + results[r].title + '</div>' +
                        '<div class="search-result-snippet">' + highlightMatch(results[r].content, query) + '</div>' +
                        '</a>';
                }
                resultsEl.innerHTML = html;
                resultsEl.style.display = 'block';
            });
        }

        for (var si = 0; si < searchInputs.length; si++) {
            (function(obj) {
                var timer = null;
                var clearBtn = obj.input.parentElement.querySelector('.search-clear-btn');

                function updateClearBtn() {
                    if (clearBtn) clearBtn.style.display = obj.input.value.length > 0 ? '' : 'none';
                }

                obj.input.addEventListener('input', function() {
                    clearTimeout(timer);
                    updateClearBtn();
                    var val = obj.input.value.trim();
                    timer = setTimeout(function() { doSearch(val, obj.results); }, 200);
                });
                obj.input.addEventListener('focus', function() {
                    if (obj.input.value.trim().length >= 2) doSearch(obj.input.value.trim(), obj.results);
                });

                if (clearBtn) {
                    clearBtn.addEventListener('click', function() {
                        obj.input.value = '';
                        obj.results.style.display = 'none';
                        obj.results.innerHTML = '';
                        updateClearBtn();
                        obj.input.focus();
                    });
                }
            })(searchInputs[si]);
        }

        document.addEventListener('click', function(e) {
            for (var ci = 0; ci < searchInputs.length; ci++) {
                if (!searchInputs[ci].input.contains(e.target) && !searchInputs[ci].results.contains(e.target)) {
                    searchInputs[ci].results.style.display = 'none';
                }
            }
        });

        // Ctrl+K shortcut
        document.addEventListener('keydown', function(e) {
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault();
                var searchInput = document.getElementById('doc-search');
                if (searchInput) searchInput.focus();
            }
        });
    }

    // Auto-detect tables with "#" column
    var tables = document.querySelectorAll('.doc-content table:not(.hljs-ln)');
    for (var t = 0; t < tables.length; t++) {
        var firstTh = tables[t].querySelector('thead th:first-child');
        if (firstTh && firstTh.textContent.trim() === '#') {
            tables[t].classList.add('has-index-col');
        }

        // Add data-label to td for mobile card layout
        var headers = tables[t].querySelectorAll('thead th');
        if (headers.length > 0) {
            var rows = tables[t].querySelectorAll('tbody tr');
            for (var r = 0; r < rows.length; r++) {
                var cells = rows[r].querySelectorAll('td');
                for (var c = 0; c < cells.length; c++) {
                    if (headers[c]) {
                        cells[c].setAttribute('data-label', headers[c].textContent.trim());
                    }
                }
            }
        }
    }
})();
