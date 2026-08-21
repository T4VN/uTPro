(function() {
    'use strict';

    var images = [];
    var currentIndex = 0;
    var overlay, imgEl, btnClose, btnPrev, btnNext, counter, imgWrap;
    var caption, thumbnailStrip, zoomIndicator, loader, bgBlurImg;
    var isTransitioning = false;

    /* Zoom state */
    var scale = 1;
    var minScale = 1;
    var maxScale = 4;
    var translateX = 0;
    var translateY = 0;
    var isZoomed = false;

    /* Touch/swipe state */
    var touchStartX = 0;
    var touchStartY = 0;
    var touchEndX = 0;
    var touchEndY = 0;
    var isSwiping = false;
    var swipeThreshold = 50;

    /* Swipe-to-close state */
    var swipeCloseStartY = 0;
    var swipeCloseEndY = 0;
    var isSwipingClose = false;
    var swipeCloseThreshold = 100;

    /* Pinch zoom state */
    var initialPinchDist = 0;
    var initialScale = 1;
    var isPinching = false;

    /* Pan state (when zoomed) */
    var isPanning = false;
    var panStartX = 0;
    var panStartY = 0;
    var panStartTransX = 0;
    var panStartTransY = 0;

    function createLightbox() {
        overlay = document.createElement('div');
        overlay.className = 'lightbox-overlay';
        overlay.setAttribute('role', 'dialog');
        overlay.setAttribute('aria-modal', 'true');
        overlay.setAttribute('aria-label', 'Image viewer');

        overlay.innerHTML =
            '<div class="lightbox-bg-blur"><img class="lightbox-bg-img" alt="" draggable="false"></div>' +
            '<button class="lightbox-close" aria-label="Close">&times;</button>' +
            '<button class="lightbox-prev" aria-label="Previous image">&#10094;</button>' +
            '<button class="lightbox-next" aria-label="Next image">&#10095;</button>' +
            '<div class="lightbox-img-wrap">' +
                '<div class="lightbox-loader"><div class="lightbox-spinner"></div></div>' +
                '<img class="lightbox-img" alt="" draggable="false">' +
            '</div>' +
            '<div class="lightbox-caption"></div>' +
            '<div class="lightbox-bottom-bar">' +
                '<div class="lightbox-counter"></div>' +
                '<div class="lightbox-zoom-indicator"></div>' +
            '</div>' +
            '<div class="lightbox-zoom-controls">' +
                '<button class="lightbox-zoom-in" aria-label="Zoom in">+</button>' +
                '<button class="lightbox-zoom-reset" aria-label="Reset zoom">1:1</button>' +
                '<button class="lightbox-zoom-out" aria-label="Zoom out">&minus;</button>' +
            '</div>' +
            '<div class="lightbox-thumbnails"></div>';

        document.body.appendChild(overlay);

        imgWrap = overlay.querySelector('.lightbox-img-wrap');
        imgEl = overlay.querySelector('.lightbox-img');
        bgBlurImg = overlay.querySelector('.lightbox-bg-img');
        btnClose = overlay.querySelector('.lightbox-close');
        btnPrev = overlay.querySelector('.lightbox-prev');
        btnNext = overlay.querySelector('.lightbox-next');
        counter = overlay.querySelector('.lightbox-counter');
        caption = overlay.querySelector('.lightbox-caption');
        thumbnailStrip = overlay.querySelector('.lightbox-thumbnails');
        zoomIndicator = overlay.querySelector('.lightbox-zoom-indicator');
        loader = overlay.querySelector('.lightbox-loader');

        var btnZoomIn = overlay.querySelector('.lightbox-zoom-in');
        var btnZoomReset = overlay.querySelector('.lightbox-zoom-reset');
        var btnZoomOut = overlay.querySelector('.lightbox-zoom-out');

        btnClose.addEventListener('click', close);
        btnPrev.addEventListener('click', function() { slideTo('prev'); });
        btnNext.addEventListener('click', function() { slideTo('next'); });

        btnZoomIn.addEventListener('click', function(e) { e.stopPropagation(); zoomIn(); });
        btnZoomOut.addEventListener('click', function(e) { e.stopPropagation(); zoomOut(); });
        btnZoomReset.addEventListener('click', function(e) { e.stopPropagation(); zoomReset(); });

        overlay.addEventListener('click', function(e) {
            if (e.target === overlay || e.target === imgWrap) {
                if (!isZoomed) close();
            }
        });

        /* Double-tap/double-click to toggle zoom */
        var lastTap = 0;
        imgEl.addEventListener('click', function(e) {
            var now = Date.now();
            if (now - lastTap < 300) {
                e.stopPropagation();
                if (isZoomed) {
                    zoomReset();
                } else {
                    zoomTo(2.5);
                }
            }
            lastTap = now;
        });

        /* Mouse wheel zoom */
        imgWrap.addEventListener('wheel', function(e) {
            e.preventDefault();
            if (e.deltaY < 0) {
                zoomIn();
            } else {
                zoomOut();
            }
        }, { passive: false });

        /* Touch events for swipe and pinch */
        imgWrap.addEventListener('touchstart', onTouchStart, { passive: false });
        imgWrap.addEventListener('touchmove', onTouchMove, { passive: false });
        imgWrap.addEventListener('touchend', onTouchEnd, { passive: true });

        /* Image load event for loader */
        imgEl.addEventListener('load', function() {
            loader.classList.remove('visible');
        });

        /* Build thumbnail strip */
        buildThumbnails();
    }

    /* ─── Thumbnails ─── */

    function buildThumbnails() {
        if (images.length <= 1) {
            thumbnailStrip.style.display = 'none';
            return;
        }
        var html = '';
        for (var i = 0; i < images.length; i++) {
            html += '<button class="lightbox-thumb" data-index="' + i + '" aria-label="Go to image ' + (i + 1) + '">' +
                '<img src="' + images[i].src + '" alt="" draggable="false">' +
                '</button>';
        }
        thumbnailStrip.innerHTML = html;
        thumbnailStrip.style.display = '';

        thumbnailStrip.addEventListener('click', function(e) {
            var btn = e.target.closest('.lightbox-thumb');
            if (!btn) return;
            var idx = parseInt(btn.getAttribute('data-index'), 10);
            if (idx !== currentIndex && !isTransitioning) {
                var direction = idx > currentIndex ? 'next' : 'prev';
                currentIndex = idx;
                slideToIndex(direction);
            }
        });
    }

    function updateThumbnails() {
        if (!thumbnailStrip || images.length <= 1) return;
        var thumbs = thumbnailStrip.querySelectorAll('.lightbox-thumb');
        for (var i = 0; i < thumbs.length; i++) {
            if (i === currentIndex) {
                thumbs[i].classList.add('active');
            } else {
                thumbs[i].classList.remove('active');
            }
        }
        /* Scroll active thumb into view */
        var activeThumb = thumbs[currentIndex];
        if (activeThumb) {
            activeThumb.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' });
        }
    }

    /* ─── Preload ─── */

    function preloadAdjacent() {
        if (images.length <= 1) return;
        var nextIdx = (currentIndex + 1) % images.length;
        var prevIdx = (currentIndex - 1 + images.length) % images.length;
        preloadImage(images[nextIdx].src);
        preloadImage(images[prevIdx].src);
    }

    function preloadImage(src) {
        var img = new Image();
        img.src = src;
    }

    /* ─── Loading indicator ─── */

    function showLoader() {
        loader.classList.add('visible');
    }

    /* ─── Zoom indicator ─── */

    function updateZoomIndicator() {
        var pct = Math.round(scale * 100);
        zoomIndicator.textContent = pct + '%';
        if (scale > 1.05) {
            zoomIndicator.classList.add('visible');
        } else {
            zoomIndicator.classList.remove('visible');
        }
    }

    /* ─── Caption ─── */

    function updateCaption() {
        var alt = images[currentIndex].alt || '';
        if (alt) {
            caption.textContent = alt;
            caption.classList.add('visible');
        } else {
            caption.textContent = '';
            caption.classList.remove('visible');
        }
    }

    /* ─── Origin animation (fly from thumbnail) ─── */

    function openWithAnimation(index) {
        var sourceImg = images[index];
        var rect = sourceImg.getBoundingClientRect();

        overlay.classList.add('active');
        document.body.style.overflow = 'hidden';

        /* Position image at source location */
        imgEl.style.transition = 'none';
        imgEl.style.position = 'fixed';
        imgEl.style.left = rect.left + 'px';
        imgEl.style.top = rect.top + 'px';
        imgEl.style.width = rect.width + 'px';
        imgEl.style.height = rect.height + 'px';
        imgEl.style.maxWidth = 'none';
        imgEl.style.maxHeight = 'none';
        imgEl.style.transform = 'none';
        imgEl.style.opacity = '1';
        imgEl.style.borderRadius = getComputedStyle(sourceImg).borderRadius;

        /* Force reflow */
        void imgEl.offsetWidth;

        /* Animate to center */
        imgEl.style.transition = 'all 0.35s cubic-bezier(0.4, 0, 0.2, 1)';
        imgEl.style.position = '';
        imgEl.style.left = '';
        imgEl.style.top = '';
        imgEl.style.width = '';
        imgEl.style.height = '';
        imgEl.style.maxWidth = '100%';
        imgEl.style.maxHeight = '85vh';
        imgEl.style.transform = '';
        imgEl.style.borderRadius = '6px';

        setTimeout(function() {
            imgEl.style.transition = '';
            imgEl.style.position = '';
        }, 360);
    }

    function closeWithAnimation() {
        var sourceImg = images[currentIndex];
        var rect = sourceImg.getBoundingClientRect();

        /* Animate back to source */
        imgEl.style.transition = 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)';
        imgEl.style.position = 'fixed';
        imgEl.style.left = rect.left + 'px';
        imgEl.style.top = rect.top + 'px';
        imgEl.style.width = rect.width + 'px';
        imgEl.style.height = rect.height + 'px';
        imgEl.style.maxWidth = 'none';
        imgEl.style.maxHeight = 'none';
        imgEl.style.borderRadius = getComputedStyle(sourceImg).borderRadius;
        imgEl.style.opacity = '0.5';

        overlay.style.transition = 'opacity 0.3s ease';
        overlay.style.opacity = '0';

        setTimeout(function() {
            overlay.classList.remove('active');
            overlay.style.transition = '';
            overlay.style.opacity = '';
            imgEl.style.transition = '';
            imgEl.style.position = '';
            imgEl.style.left = '';
            imgEl.style.top = '';
            imgEl.style.width = '';
            imgEl.style.height = '';
            imgEl.style.maxWidth = '';
            imgEl.style.maxHeight = '';
            imgEl.style.borderRadius = '';
            imgEl.style.opacity = '';
            imgEl.style.transform = '';
            document.body.style.overflow = '';
        }, 310);
    }

    /* ─── Slide transition ─── */

    function slideTo(direction) {
        if (isTransitioning || images.length < 2) return;
        isTransitioning = true;

        var slideOut = direction === 'next' ? '-110%' : '110%';
        var slideIn = direction === 'next' ? '110%' : '-110%';

        imgEl.style.transition = 'transform 0.25s ease, opacity 0.2s ease';
        imgEl.style.transform = 'translateX(' + slideOut + ')';
        imgEl.style.opacity = '0';

        setTimeout(function() {
            if (direction === 'next') {
                currentIndex = (currentIndex + 1) % images.length;
            } else {
                currentIndex = (currentIndex - 1 + images.length) % images.length;
            }

            showLoader();
            imgEl.style.transition = 'none';
            imgEl.style.transform = 'translateX(' + slideIn + ')';
            imgEl.src = images[currentIndex].src;
            imgEl.alt = images[currentIndex].alt || '';
            updateCounter();
            updateCaption();
            updateBgBlur();
            updateThumbnails();
            preloadAdjacent();

            void imgEl.offsetWidth;

            imgEl.style.transition = 'transform 0.25s ease, opacity 0.2s ease';
            imgEl.style.transform = 'translateX(0) scale(1)';
            imgEl.style.opacity = '1';

            setTimeout(function() {
                imgEl.style.transition = '';
                imgEl.style.transform = '';
                scale = 1;
                translateX = 0;
                translateY = 0;
                isZoomed = false;
                updateZoomUI();
                updateZoomIndicator();
                isTransitioning = false;
            }, 260);
        }, 220);
    }

    function slideToIndex(direction) {
        /* Used by thumbnail click — index already set */
        if (isTransitioning) return;
        isTransitioning = true;

        var slideOut = direction === 'next' ? '-110%' : '110%';
        var slideIn = direction === 'next' ? '110%' : '-110%';

        imgEl.style.transition = 'transform 0.25s ease, opacity 0.2s ease';
        imgEl.style.transform = 'translateX(' + slideOut + ')';
        imgEl.style.opacity = '0';

        setTimeout(function() {
            showLoader();
            imgEl.style.transition = 'none';
            imgEl.style.transform = 'translateX(' + slideIn + ')';
            imgEl.src = images[currentIndex].src;
            imgEl.alt = images[currentIndex].alt || '';
            updateCounter();
            updateCaption();
            updateBgBlur();
            updateThumbnails();
            preloadAdjacent();

            void imgEl.offsetWidth;

            imgEl.style.transition = 'transform 0.25s ease, opacity 0.2s ease';
            imgEl.style.transform = 'translateX(0) scale(1)';
            imgEl.style.opacity = '1';

            setTimeout(function() {
                imgEl.style.transition = '';
                imgEl.style.transform = '';
                scale = 1;
                translateX = 0;
                translateY = 0;
                isZoomed = false;
                updateZoomUI();
                updateZoomIndicator();
                isTransitioning = false;
            }, 260);
        }, 220);
    }

    /* ─── Touch handlers ─── */

    function getDistance(t1, t2) {
        var dx = t1.clientX - t2.clientX;
        var dy = t1.clientY - t2.clientY;
        return Math.sqrt(dx * dx + dy * dy);
    }

    function onTouchStart(e) {
        if (isTransitioning) return;

        if (e.touches.length === 2) {
            isPinching = true;
            isSwiping = false;
            isPanning = false;
            isSwipingClose = false;
            initialPinchDist = getDistance(e.touches[0], e.touches[1]);
            initialScale = scale;
            e.preventDefault();
        } else if (e.touches.length === 1) {
            if (isZoomed) {
                isPanning = true;
                isSwiping = false;
                isSwipingClose = false;
                panStartX = e.touches[0].clientX;
                panStartY = e.touches[0].clientY;
                panStartTransX = translateX;
                panStartTransY = translateY;
            } else {
                isSwiping = true;
                isPanning = false;
                isSwipingClose = false;
                touchStartX = e.touches[0].clientX;
                touchStartY = e.touches[0].clientY;
                touchEndX = touchStartX;
                touchEndY = touchStartY;
                swipeCloseStartY = touchStartY;
                swipeCloseEndY = touchStartY;
                imgEl.style.transition = 'none';
            }
        }
    }

    function onTouchMove(e) {
        if (isTransitioning) return;

        if (isPinching && e.touches.length === 2) {
            e.preventDefault();
            var dist = getDistance(e.touches[0], e.touches[1]);
            var newScale = initialScale * (dist / initialPinchDist);
            zoomTo(newScale);
        } else if (isPanning && e.touches.length === 1) {
            e.preventDefault();
            var dx = e.touches[0].clientX - panStartX;
            var dy = e.touches[0].clientY - panStartY;
            translateX = panStartTransX + dx;
            translateY = panStartTransY + dy;
            applyTransform();
        } else if (isSwiping && e.touches.length === 1) {
            touchEndX = e.touches[0].clientX;
            touchEndY = e.touches[0].clientY;
            swipeCloseEndY = touchEndY;

            var diffX = touchEndX - touchStartX;
            var diffY = touchEndY - touchStartY;

            /* Determine if this is a vertical swipe (close) or horizontal (nav) */
            if (!isSwipingClose && Math.abs(diffY) > 30 && Math.abs(diffY) > Math.abs(diffX) * 1.5) {
                isSwipingClose = true;
            }

            if (isSwipingClose) {
                /* Vertical drag — swipe to close */
                var dragY = diffY;
                var opacity = 1 - Math.abs(dragY) * 0.003;
                var s = 1 - Math.abs(dragY) * 0.001;
                imgEl.style.transform = 'translateY(' + dragY + 'px) scale(' + Math.max(0.85, s) + ')';
                overlay.style.background = 'rgba(0,0,0,' + Math.max(0.3, 0.92 * opacity) + ')';
            } else {
                /* Horizontal drag — swipe nav */
                imgEl.style.transform = 'translateX(' + diffX + 'px) scale(' + (1 - Math.abs(diffX) * 0.0003) + ')';
                imgEl.style.opacity = String(1 - Math.abs(diffX) * 0.002);
            }
        }
    }

    function onTouchEnd(e) {
        if (isTransitioning) return;

        if (isPinching) {
            isPinching = false;
            if (scale <= 1) zoomReset();
            return;
        }

        if (isPanning) {
            isPanning = false;
            constrainPan();
            return;
        }

        if (isSwiping) {
            isSwiping = false;

            if (isSwipingClose) {
                isSwipingClose = false;
                var diffY = swipeCloseEndY - swipeCloseStartY;
                if (Math.abs(diffY) > swipeCloseThreshold) {
                    /* Close with animation */
                    imgEl.style.transition = 'transform 0.3s ease, opacity 0.3s ease';
                    imgEl.style.transform = 'translateY(' + (diffY > 0 ? '100vh' : '-100vh') + ') scale(0.8)';
                    imgEl.style.opacity = '0';
                    overlay.style.transition = 'background 0.3s ease';
                    overlay.style.background = 'rgba(0,0,0,0)';
                    setTimeout(function() {
                        overlay.classList.remove('active');
                        document.body.style.overflow = '';
                        document.removeEventListener('keydown', onKey);
                        resetAllStates();
                    }, 310);
                } else {
                    /* Snap back */
                    imgEl.style.transition = 'transform 0.25s ease';
                    imgEl.style.transform = '';
                    overlay.style.transition = 'background 0.25s ease';
                    overlay.style.background = '';
                    setTimeout(function() {
                        imgEl.style.transition = '';
                        overlay.style.transition = '';
                    }, 260);
                }
                return;
            }

            var diffX = touchEndX - touchStartX;
            var diffYH = touchEndY - touchStartY;

            if (Math.abs(diffX) > swipeThreshold && Math.abs(diffX) > Math.abs(diffYH) && images.length > 1) {
                if (diffX < 0) {
                    slideTo('next');
                } else {
                    slideTo('prev');
                }
            } else {
                /* Snap back */
                imgEl.style.transition = 'transform 0.2s ease, opacity 0.2s ease';
                imgEl.style.transform = '';
                imgEl.style.opacity = '1';
                setTimeout(function() { imgEl.style.transition = ''; }, 210);
            }
        }
    }

    /* ─── Zoom helpers ─── */

    function zoomTo(newScale) {
        scale = Math.max(minScale, Math.min(maxScale, newScale));
        isZoomed = scale > 1.05;
        if (!isZoomed) {
            translateX = 0;
            translateY = 0;
        }
        applyTransform();
        updateZoomUI();
        updateZoomIndicator();
    }

    function zoomIn() {
        zoomTo(scale * 1.4);
    }

    function zoomOut() {
        zoomTo(scale / 1.4);
    }

    function zoomReset() {
        scale = 1;
        translateX = 0;
        translateY = 0;
        isZoomed = false;
        imgEl.style.transition = 'transform 0.2s ease';
        applyTransform();
        updateZoomUI();
        updateZoomIndicator();
        setTimeout(function() { imgEl.style.transition = ''; }, 210);
    }

    function applyTransform() {
        imgEl.style.transform = 'scale(' + scale + ') translate(' + (translateX / scale) + 'px, ' + (translateY / scale) + 'px)';
    }

    function constrainPan() {
        var rect = imgEl.getBoundingClientRect();
        var wrapRect = imgWrap.getBoundingClientRect();
        var maxPanX = Math.max(0, (rect.width - wrapRect.width) / 2);
        var maxPanY = Math.max(0, (rect.height - wrapRect.height) / 2);

        translateX = Math.max(-maxPanX, Math.min(maxPanX, translateX));
        translateY = Math.max(-maxPanY, Math.min(maxPanY, translateY));
        imgEl.style.transition = 'transform 0.15s ease';
        applyTransform();
        setTimeout(function() { imgEl.style.transition = ''; }, 160);
    }

    function updateZoomUI() {
        if (isZoomed) {
            overlay.classList.add('zoomed');
        } else {
            overlay.classList.remove('zoomed');
        }
    }

    /* ─── Background blur ─── */

    function updateBgBlur() {
        bgBlurImg.src = images[currentIndex].src;
    }

    /* ─── Helpers ─── */

    function updateCounter() {
        counter.textContent = (currentIndex + 1) + ' / ' + images.length;
    }

    function resetAllStates() {
        scale = 1;
        translateX = 0;
        translateY = 0;
        isZoomed = false;
        isTransitioning = false;
        imgEl.style.transform = '';
        imgEl.style.transition = '';
        imgEl.style.opacity = '';
        imgEl.style.position = '';
        imgEl.style.left = '';
        imgEl.style.top = '';
        imgEl.style.width = '';
        imgEl.style.height = '';
        imgEl.style.maxWidth = '';
        imgEl.style.maxHeight = '';
        imgEl.style.borderRadius = '';
        overlay.style.background = '';
        overlay.style.transition = '';
        overlay.style.opacity = '';
        updateZoomUI();
        updateZoomIndicator();
    }

    /* ─── Core lightbox ─── */

    function open(index) {
        currentIndex = index;
        resetAllStates();
        imgEl.src = images[currentIndex].src;
        imgEl.alt = images[currentIndex].alt || '';
        showLoader();
        updateCounter();
        updateCaption();
        updateBgBlur();
        updateThumbnails();
        preloadAdjacent();

        var hasMultiple = images.length > 1;
        btnPrev.style.display = hasMultiple ? '' : 'none';
        btnNext.style.display = hasMultiple ? '' : 'none';
        counter.style.display = hasMultiple ? '' : 'none';

        /* Origin animation */
        openWithAnimation(index);
        document.addEventListener('keydown', onKey);
    }

    function close() {
        document.removeEventListener('keydown', onKey);

        /* Try origin close animation */
        var sourceImg = images[currentIndex];
        var rect = sourceImg.getBoundingClientRect();

        /* Only animate back if source is visible in viewport */
        if (rect.top > -rect.height && rect.top < window.innerHeight + rect.height && !isZoomed) {
            closeWithAnimation();
        } else {
            /* Fallback: simple fade out */
            overlay.style.transition = 'opacity 0.25s ease';
            overlay.style.opacity = '0';
            setTimeout(function() {
                overlay.classList.remove('active');
                document.body.style.overflow = '';
                resetAllStates();
            }, 260);
        }
    }

    function onKey(e) {
        if (e.key === 'Escape') close();
        else if (e.key === 'ArrowLeft' && !isZoomed) slideTo('prev');
        else if (e.key === 'ArrowRight' && !isZoomed) slideTo('next');
        else if (e.key === '+' || e.key === '=') zoomIn();
        else if (e.key === '-') zoomOut();
        else if (e.key === '0') zoomReset();
    }

    /* ─── Init ─── */

    function isScreenshot(img) {
        var src = (img.getAttribute('src') || '').toLowerCase();
        return src.indexOf('/screenshots/') !== -1;
    }

    function init() {
        var imgNodes = document.querySelectorAll('.doc-content img');
        if (imgNodes.length === 0) return;

        var hasScreenshots = false;

        for (var i = 0; i < imgNodes.length; i++) {
            if (!isScreenshot(imgNodes[i])) continue;
            if (!hasScreenshots) {
                hasScreenshots = true;
            }
            images.push(imgNodes[i]);
            imgNodes[i].style.cursor = 'pointer';
            imgNodes[i].setAttribute('tabindex', '0');
            imgNodes[i].setAttribute('role', 'button');
            imgNodes[i].setAttribute('aria-label', 'View full size: ' + (imgNodes[i].alt || 'image'));
        }

        if (!hasScreenshots) return;
        createLightbox();

        for (var j = 0; j < images.length; j++) {
            (function(index, node) {
                node.addEventListener('click', function() { open(index); });
                node.addEventListener('keydown', function(e) {
                    if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); open(index); }
                });
            })(j, images[j]);
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
