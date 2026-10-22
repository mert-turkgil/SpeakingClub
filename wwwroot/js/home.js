// ============================================================================
// home.js — home page interactions (reveal, card glow and image fallbacks
// come from blog.js, which runs on the home page too).
// ============================================================================
(function () {
    'use strict';

    var root = document.querySelector('.home-page');
    if (!root) return;

    var reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    var finePointer = window.matchMedia('(hover: hover) and (pointer: fine)').matches;

    // ------------------------------------------------------------------------
    // Highlights slider (slides come from the admin panel).
    // Tabs with a timeline, autoplay, swipe and arrow keys. Every change fires
    // "hp:slide" ({ from, to, direction }) on the slider, which home-3d.js uses
    // for the WebGL transition; without it the CSS transition runs.
    // ------------------------------------------------------------------------
    function initSlider() {
        var slider = root.querySelector('[data-slider]');
        if (!slider) return;
        var slides = Array.prototype.slice.call(slider.querySelectorAll('.hp-gl-slide'));
        var tabs = Array.prototype.slice.call(slider.querySelectorAll('[data-go]'));
        var stage = slider.querySelector('.hp-gl-slider__stage');
        var pauseButton = slider.querySelector('[data-pause]');
        var count = slides.length;
        if (count < 2) return;

        var interval = parseInt(slider.getAttribute('data-interval'), 10) || 7000;
        var index = 0;
        var elapsed = 0;
        var last = 0;
        var userPaused = reduceMotion; // no autoplay for people who asked for less motion
        var hovered = false;
        var focused = false;
        var inView = false;
        var frame = null;

        function show(to, direction) {
            to = (to + count) % count;
            if (to === index) return;
            var from = index;
            index = to;
            slides.forEach(function (slide, i) {
                slide.classList.toggle('is-active', i === index);
                slide.classList.toggle('is-leaving', i === from);
                slide.setAttribute('aria-hidden', i === index ? 'false' : 'true');
                slide.querySelectorAll('a').forEach(function (a) { a.tabIndex = i === index ? 0 : -1; });
            });
            tabs.forEach(function (tab, i) {
                tab.classList.toggle('is-active', i === index);
                if (i === index) tab.setAttribute('aria-current', 'true'); else tab.removeAttribute('aria-current');
                tab.style.setProperty('--p', 0);
            });
            elapsed = 0;
            slider.setAttribute('data-active', index);
            slider.dispatchEvent(new CustomEvent('hp:slide', {
                detail: { from: from, to: index, direction: direction || (to > from ? 1 : -1) }
            }));
        }
        function next() { show(index + 1, 1); }
        function prev() { show(index - 1, -1); }

        function running() { return !userPaused && !hovered && !focused && inView && !document.hidden; }
        function tick(now) {
            frame = null;
            var dt = last ? Math.min(now - last, 100) : 0;
            last = now;
            if (running()) {
                elapsed += dt;
                if (elapsed >= interval) next();
            }
            var tab = tabs[index];
            if (tab) tab.style.setProperty('--p', Math.min(1, elapsed / interval).toFixed(4));
            if (inView) frame = window.requestAnimationFrame(tick);
        }
        function startLoop() {
            if (frame == null) { last = 0; frame = window.requestAnimationFrame(tick); }
        }

        if ('IntersectionObserver' in window) {
            new IntersectionObserver(function (entries) {
                inView = entries[0].isIntersecting;
                if (inView) startLoop();
            }, { threshold: 0.35 }).observe(slider);
        } else {
            inView = true;
            startLoop();
        }

        tabs.forEach(function (tab) {
            tab.addEventListener('click', function () {
                var to = parseInt(tab.getAttribute('data-go'), 10);
                show(to, to > index ? 1 : -1);
            });
        });
        var nextButton = slider.querySelector('[data-next]');
        var prevButton = slider.querySelector('[data-prev]');
        if (nextButton) nextButton.addEventListener('click', next);
        if (prevButton) prevButton.addEventListener('click', prev);

        function renderPause() {
            if (!pauseButton) return;
            pauseButton.setAttribute('aria-pressed', userPaused ? 'true' : 'false');
            pauseButton.setAttribute('aria-label', pauseButton.getAttribute(userPaused ? 'data-label-play' : 'data-label-pause'));
            pauseButton.innerHTML = '<i class="fa-solid ' + (userPaused ? 'fa-play' : 'fa-pause') + '" aria-hidden="true"></i>';
        }
        if (pauseButton) {
            pauseButton.addEventListener('click', function () { userPaused = !userPaused; renderPause(); });
            renderPause();
        }

        slider.addEventListener('mouseenter', function () { hovered = true; });
        slider.addEventListener('mouseleave', function () { hovered = false; });
        // Keyboard focus pauses (a mouse click on "next" keeps focus but should not stop autoplay)
        slider.addEventListener('focusin', function (e) { focused = e.target.matches(':focus-visible'); });
        slider.addEventListener('focusout', function (e) { if (!slider.contains(e.relatedTarget)) focused = false; });
        slider.addEventListener('keydown', function (e) {
            if (e.key === 'ArrowRight') { e.preventDefault(); next(); }
            else if (e.key === 'ArrowLeft') { e.preventDefault(); prev(); }
        });

        // Swipe / drag on the image
        var startX = null, startY = 0;
        stage.addEventListener('pointerdown', function (e) {
            if (e.target.closest('a, button')) return;
            startX = e.clientX;
            startY = e.clientY;
        });
        stage.addEventListener('pointerup', function (e) {
            if (startX === null) return;
            var dx = e.clientX - startX, dy = e.clientY - startY;
            startX = null;
            if (Math.abs(dx) > 50 && Math.abs(dx) > Math.abs(dy)) { if (dx < 0) next(); else prev(); }
        });
        stage.addEventListener('pointercancel', function () { startX = null; });
    }

    // ------------------------------------------------------------------------
    // Shorts: a thumbnail until clicked, then the YouTube player
    // ------------------------------------------------------------------------
    function initShorts() {
        root.querySelectorAll('[data-yt]').forEach(function (button) {
            button.addEventListener('click', function () {
                var iframe = document.createElement('iframe');
                iframe.className = 'hp-short__frame';
                iframe.src = 'https://www.youtube-nocookie.com/embed/' + encodeURIComponent(button.getAttribute('data-yt')) +
                    '?autoplay=1&playsinline=1&rel=0';
                iframe.title = button.getAttribute('aria-label') || 'YouTube Shorts';
                iframe.allow = 'accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share';
                iframe.referrerPolicy = 'strict-origin-when-cross-origin';
                iframe.allowFullscreen = true;
                button.replaceWith(iframe);
                iframe.focus();
            });
        });
    }

    // ------------------------------------------------------------------------
    // Shorts rail: arrow buttons + drag to scroll with the mouse
    // ------------------------------------------------------------------------
    function initRail() {
        var rail = root.querySelector('[data-rail]');
        if (!rail) return;
        var prev = root.querySelector('[data-rail-prev]');
        var next = root.querySelector('[data-rail-next]');

        function step() {
            var item = rail.querySelector('.hp-short');
            return item ? (item.getBoundingClientRect().width + 16) * 2 : 400;
        }
        function update() {
            var max = rail.scrollWidth - rail.clientWidth - 2;
            if (prev) prev.disabled = rail.scrollLeft <= 2;
            if (next) next.disabled = rail.scrollLeft >= max;
        }
        var behavior = reduceMotion ? 'auto' : 'smooth';
        if (prev) prev.addEventListener('click', function () { rail.scrollBy({ left: -step(), behavior: behavior }); });
        if (next) next.addEventListener('click', function () { rail.scrollBy({ left: step(), behavior: behavior }); });

        var ticking = false;
        rail.addEventListener('scroll', function () {
            if (!ticking) { ticking = true; window.requestAnimationFrame(function () { ticking = false; update(); }); }
        }, { passive: true });
        window.addEventListener('resize', update, { passive: true });
        update();

        // Mouse drag (touch devices scroll natively)
        var startX = 0, startScroll = 0, dragging = false, moved = false;
        rail.addEventListener('pointerdown', function (e) {
            if (e.pointerType !== 'mouse' || e.button !== 0) return;
            dragging = true;
            moved = false;
            startX = e.clientX;
            startScroll = rail.scrollLeft;
        });
        window.addEventListener('pointermove', function (e) {
            if (!dragging) return;
            var dx = e.clientX - startX;
            if (!moved && Math.abs(dx) > 6) {
                moved = true;
                rail.classList.add('is-dragging');
            }
            if (moved) rail.scrollLeft = startScroll - dx;
        });
        window.addEventListener('pointerup', function () {
            if (!dragging) return;
            dragging = false;
            if (moved) {
                // Let the snap settle on the nearest card, and swallow the click that ends a drag. The click
                // (if any) is dispatched right after pointerup, before the timeout removes the guard.
                rail.classList.remove('is-dragging');
                var swallow = function (ev) { ev.stopPropagation(); ev.preventDefault(); };
                rail.addEventListener('click', swallow, true);
                setTimeout(function () { rail.removeEventListener('click', swallow, true); }, 0);
            }
        });
    }

    // ------------------------------------------------------------------------
    // Hero: speech bubbles drift a little with the pointer
    // ------------------------------------------------------------------------
    function initHeroParallax() {
        if (reduceMotion || !finePointer) return;
        var hero = root.querySelector('.hp-hero');
        var visual = root.querySelector('.hp-hero__visual');
        if (!hero || !visual) return;
        var frame = null;
        hero.addEventListener('pointermove', function (e) {
            if (frame) return;
            frame = window.requestAnimationFrame(function () {
                frame = null;
                var r = visual.getBoundingClientRect();
                visual.style.setProperty('--px', ((e.clientX - r.left) / r.width - 0.5).toFixed(3));
                visual.style.setProperty('--py', ((e.clientY - r.top) / r.height - 0.5).toFixed(3));
            });
        });
        hero.addEventListener('pointerleave', function () {
            visual.style.setProperty('--px', 0);
            visual.style.setProperty('--py', 0);
        });
    }

    initSlider();
    initShorts();
    initRail();
    initHeroParallax();
})();
