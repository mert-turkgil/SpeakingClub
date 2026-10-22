// ============================================================================
// blog.js — interactions for the blog list, blog post and home pages.
// No dependencies. Everything is progressive enhancement: without this file
// the pages are fully readable, only the motion and the helpers are missing.
// UI strings come from data-* attributes on .blog-page (localized in Razor).
// ============================================================================
(function () {
    'use strict';

    // Also runs on the home page, which reuses the blog cards, buttons and reveal animations
    var root = document.querySelector('.blog-page, .home-page');
    if (!root) return;

    var reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    var text = function (key, fallback) { return root.getAttribute('data-' + key) || fallback; };

    // ------------------------------------------------------------------------
    // Scroll reveal (cards, sections). Elements are only hidden while html has
    // .bp-js, which the page sets in <head>; this reveals them as they arrive.
    // ------------------------------------------------------------------------
    function initReveal() {
        var items = document.querySelectorAll('.reveal');
        if (!items.length) return;
        if (reduceMotion || !('IntersectionObserver' in window)) {
            items.forEach(function (el) { el.classList.add('is-visible'); });
            return;
        }
        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (!entry.isIntersecting) return;
                entry.target.classList.add('is-visible');
                observer.unobserve(entry.target);
            });
        }, { rootMargin: '0px 0px -8% 0px', threshold: 0.08 });
        items.forEach(function (el) { observer.observe(el); });
    }

    // ------------------------------------------------------------------------
    // Pointer-following glow on cards (desktop pointers only)
    // ------------------------------------------------------------------------
    function initCardGlow() {
        if (reduceMotion || !window.matchMedia('(hover: hover) and (pointer: fine)').matches) return;
        document.addEventListener('pointermove', function (e) {
            var card = e.target.closest && e.target.closest('.post-card');
            if (!card) return;
            var r = card.getBoundingClientRect();
            card.style.setProperty('--mx', (e.clientX - r.left) + 'px');
            card.style.setProperty('--my', (e.clientY - r.top) + 'px');
        }, { passive: true });
    }

    // ------------------------------------------------------------------------
    // Broken images: cards fall back to their gradient, the cover disappears
    // ------------------------------------------------------------------------
    function initImageFallbacks() {
        function fallback(img) {
            if (img.closest('.post-card__media')) {
                img.classList.add('is-broken');
            } else if (img.closest('.post-cover')) {
                img.closest('.post-cover').remove();
            }
        }
        document.addEventListener('error', function (e) {
            if (e.target && e.target.tagName === 'IMG') fallback(e.target);
        }, true);
        // Images that already failed before this script ran
        document.querySelectorAll('.post-card__media img, .post-cover img').forEach(function (img) {
            if (img.complete && img.naturalWidth === 0 && img.getAttribute('src')) fallback(img);
        });
    }

    // ------------------------------------------------------------------------
    // Reading progress (top bar) + back-to-top button
    // ------------------------------------------------------------------------
    function initScrollUi() {
        var bar = document.querySelector('.bp-progress');
        var article = document.querySelector('.post-body');
        var topButton = document.querySelector('.bp-top');
        if (!bar && !topButton) return;

        var ticking = false;
        function update() {
            ticking = false;
            var y = window.scrollY || window.pageYOffset;
            if (bar && article) {
                var rect = article.getBoundingClientRect();
                var start = rect.top + y - window.innerHeight * 0.25;
                var total = article.offsetHeight - window.innerHeight * 0.5;
                var progress = total > 0 ? (y - start) / total : 1;
                bar.style.setProperty('--bp-progress', Math.min(1, Math.max(0, progress)).toFixed(4));
            }
            if (topButton) topButton.classList.toggle('is-visible', y > 700);
        }
        window.addEventListener('scroll', function () {
            if (!ticking) { ticking = true; window.requestAnimationFrame(update); }
        }, { passive: true });
        window.addEventListener('resize', update, { passive: true });
        update();

        if (topButton) {
            topButton.addEventListener('click', function () {
                window.scrollTo({ top: 0, behavior: reduceMotion ? 'auto' : 'smooth' });
            });
        }
    }

    // ------------------------------------------------------------------------
    // Table of contents: smooth jump + highlight the section being read
    // ------------------------------------------------------------------------
    function initToc() {
        var toc = document.querySelector('.post-toc');
        if (!toc) return;
        var links = Array.prototype.slice.call(toc.querySelectorAll('a[href^="#"]'));
        var desktop = window.matchMedia('(min-width: 1200px)');

        // Open beside the article on desktop, collapsed above it on smaller screens
        function syncOpen() { toc.open = desktop.matches; }
        syncOpen();
        if (desktop.addEventListener) desktop.addEventListener('change', syncOpen);

        links.forEach(function (link) {
            link.addEventListener('click', function (e) {
                var target = document.getElementById(decodeURIComponent(link.hash.slice(1)));
                if (!target) return;
                e.preventDefault();
                target.scrollIntoView({ behavior: reduceMotion ? 'auto' : 'smooth', block: 'start' });
                history.replaceState(null, '', link.hash);
                if (!desktop.matches) toc.open = false;
                target.setAttribute('tabindex', '-1');
                target.focus({ preventScroll: true });
            });
        });

        if (!('IntersectionObserver' in window)) return;
        var headings = links
            .map(function (link) { return document.getElementById(decodeURIComponent(link.hash.slice(1))); })
            .filter(Boolean);
        var visible = new Set();
        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) visible.add(entry.target); else visible.delete(entry.target);
            });
            // The first heading in view wins; between headings, the last one passed stays active
            var current = headings.find(function (h) { return visible.has(h); });
            if (!current) {
                var passed = headings.filter(function (h) { return h.getBoundingClientRect().top < 120; });
                current = passed[passed.length - 1];
            }
            links.forEach(function (link) {
                link.classList.toggle('is-active', !!current && link.hash === '#' + current.id);
            });
        }, { rootMargin: '-90px 0px -60% 0px' });
        headings.forEach(function (h) { observer.observe(h); });
    }

    // ------------------------------------------------------------------------
    // Toast
    // ------------------------------------------------------------------------
    var toastTimer;
    function toast(message) {
        var el = document.querySelector('.bp-toast');
        if (!el) return;
        el.innerHTML = '<i class="fa-solid fa-circle-check" aria-hidden="true"></i>';
        el.appendChild(document.createTextNode(message));
        el.classList.add('is-visible');
        clearTimeout(toastTimer);
        toastTimer = setTimeout(function () { el.classList.remove('is-visible'); }, 2200);
    }

    function copyText(value) {
        if (navigator.clipboard && window.isSecureContext) {
            return navigator.clipboard.writeText(value);
        }
        return new Promise(function (resolve, reject) {
            var area = document.createElement('textarea');
            area.value = value;
            area.setAttribute('readonly', '');
            area.style.position = 'fixed';
            area.style.opacity = '0';
            document.body.appendChild(area);
            area.select();
            try { document.execCommand('copy') ? resolve() : reject(); } catch (err) { reject(err); }
            document.body.removeChild(area);
        });
    }

    // ------------------------------------------------------------------------
    // Share: copy link + native share sheet where the device has one
    // ------------------------------------------------------------------------
    function initShare() {
        document.querySelectorAll('[data-copy-url]').forEach(function (btn) {
            btn.addEventListener('click', function () {
                copyText(btn.getAttribute('data-copy-url')).then(
                    function () { toast(text('copied', 'Link copied')); },
                    function () { window.prompt(text('copy-manually', 'Copy this link:'), btn.getAttribute('data-copy-url')); }
                );
            });
        });

        if (!navigator.share) return;
        document.querySelectorAll('[data-native-share]').forEach(function (btn) {
            btn.hidden = false;
            btn.addEventListener('click', function () {
                navigator.share({ title: document.title, url: btn.getAttribute('data-native-share') }).catch(function () { });
            });
        });
    }

    // ------------------------------------------------------------------------
    // Code blocks: copy button
    // ------------------------------------------------------------------------
    function initCodeCopy() {
        document.querySelectorAll('.post-body pre').forEach(function (pre) {
            var button = document.createElement('button');
            button.type = 'button';
            button.className = 'bp-copy-code';
            button.textContent = text('copy-code', 'Copy');
            button.addEventListener('click', function () {
                var code = pre.querySelector('code') || pre;
                copyText(code.innerText).then(function () {
                    button.textContent = text('copied-code', 'Copied');
                    setTimeout(function () { button.textContent = text('copy-code', 'Copy'); }, 1600);
                });
            });
            pre.appendChild(button);
        });
    }

    // ------------------------------------------------------------------------
    // Lightbox for article images (not for images that are links)
    // ------------------------------------------------------------------------
    function initLightbox() {
        var dialog = document.querySelector('.bp-lightbox');
        if (!dialog || typeof dialog.showModal !== 'function') return;
        var image = dialog.querySelector('img');
        var caption = dialog.querySelector('figcaption');

        document.querySelectorAll('.post-body img').forEach(function (img) {
            if (img.closest('a')) return;
            img.setAttribute('tabindex', '0');
            img.setAttribute('role', 'button');
            function open() {
                image.src = img.currentSrc || img.src;
                image.alt = img.alt || '';
                var figcaption = img.closest('figure') && img.closest('figure').querySelector('figcaption');
                caption.textContent = figcaption ? figcaption.textContent : (img.alt || '');
                caption.hidden = !caption.textContent;
                dialog.showModal();
            }
            img.addEventListener('click', open);
            img.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); open(); }
            });
        });

        dialog.addEventListener('click', function () { dialog.close(); });
        dialog.addEventListener('close', function () { image.removeAttribute('src'); });
    }

    initReveal();
    initCardGlow();
    initImageFallbacks();
    initScrollUi();
    initToc();
    initShare();
    initCodeCopy();
    initLightbox();
})();
