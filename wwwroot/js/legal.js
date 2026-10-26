// ============================================================================
// legal.js — privacy policy and terms of use (Views/Home/_LegalLayout.cshtml).
// Progressive enhancement: the pages are complete without it.
//  - reading time in the meta row, Print / PDF button
//  - table of contents: marks the section being read; open beside the text on
//    wide screens, a collapsible box on phones
//  - reading progress bar (.bp-progress from blog.css)
//  - a "copy link" button on every numbered section
// The back-to-top button (.bp-top) and scroll reveals come from blog.js.
// ============================================================================
(function () {
    'use strict';

    var root = document.querySelector('.site-page.legal');
    if (!root) return;
    var body = root.querySelector('.legal-body');
    var toc = root.querySelector('[data-legal-toc]');
    var reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    var wide = window.matchMedia('(min-width: 901px)');
    var text = function (key) { return root.getAttribute('data-' + key) || ''; };

    // ------------------------------------------------------------------
    // Reading time (≈ 200 words a minute) and print
    // ------------------------------------------------------------------
    var reading = root.querySelector('[data-reading-time]');
    if (reading && body) {
        var words = (body.innerText || body.textContent || '').trim().split(/\s+/).length;
        reading.querySelector('span').textContent = text('reading').replace('{0}', Math.max(1, Math.round(words / 200)));
        reading.hidden = false;
    }
    var print = root.querySelector('[data-print]');
    if (print) print.addEventListener('click', function () { window.print(); });

    // ------------------------------------------------------------------
    // Table of contents: open on wide screens, collapsible on phones
    // ------------------------------------------------------------------
    var links = toc ? Array.prototype.slice.call(toc.querySelectorAll('a[href^="#"]')) : [];
    function syncOpen() { if (toc) toc.open = wide.matches; }
    syncOpen();
    if (wide.addEventListener) wide.addEventListener('change', syncOpen);
    if (toc) {
        toc.addEventListener('toggle', function () { if (wide.matches && !toc.open) toc.open = true; });
    }

    links.forEach(function (link) {
        link.addEventListener('click', function (e) {
            var target = document.getElementById(decodeURIComponent(link.hash.slice(1)));
            if (!target) return;
            e.preventDefault();
            target.scrollIntoView({ behavior: reduceMotion ? 'auto' : 'smooth', block: 'start' });
            history.replaceState(null, '', link.hash);
            if (!wide.matches && toc) toc.open = false;
            var heading = target.querySelector('h2');
            if (heading) {
                heading.setAttribute('tabindex', '-1');
                heading.focus({ preventScroll: true });
            }
        });
    });

    // ------------------------------------------------------------------
    // Mark the section being read
    // ------------------------------------------------------------------
    var sections = links
        .map(function (link) { return document.getElementById(decodeURIComponent(link.hash.slice(1))); })
        .filter(Boolean);

    function setActive(id) {
        links.forEach(function (link) {
            var on = link.hash === '#' + id;
            link.classList.toggle('is-active', on);
            if (on) link.setAttribute('aria-current', 'location'); else link.removeAttribute('aria-current');
        });
    }

    // The marked section is the last one whose start has passed the reading line (just under the sticky
    // header). At the very end of the page the last section is marked, even if it is too short to get there.
    function markCurrent() {
        if (!sections.length) return;
        var line = 150;
        var current = null;
        sections.forEach(function (s) { if (s.getBoundingClientRect().top <= line) current = s; });
        if (window.innerHeight + window.scrollY >= document.documentElement.scrollHeight - 4)
            current = sections[sections.length - 1];
        setActive(current ? current.id : '');
    }

    // ------------------------------------------------------------------
    // Reading progress
    // ------------------------------------------------------------------
    var bar = document.querySelector('.bp-progress');
    function updateProgress() {
        if (!bar || !body) return;
        var rect = body.getBoundingClientRect();
        var total = body.offsetHeight - window.innerHeight * 0.6;
        var progress = total > 0 ? (window.innerHeight * 0.25 - rect.top) / total : 1;
        bar.style.setProperty('--bp-progress', Math.min(1, Math.max(0, progress)).toFixed(4));
    }

    var ticking = false;
    function onScroll() {
        ticking = false;
        markCurrent();
        updateProgress();
    }
    window.addEventListener('scroll', function () {
        if (!ticking) { ticking = true; window.requestAnimationFrame(onScroll); }
    }, { passive: true });
    window.addEventListener('resize', onScroll, { passive: true });
    onScroll();

    // ------------------------------------------------------------------
    // "Copy link to this section"
    // ------------------------------------------------------------------
    var toastEl = document.querySelector('.bp-toast');
    var toastTimer;
    function toast(message) {
        if (!toastEl) return;
        toastEl.innerHTML = '<i class="fa-solid fa-circle-check" aria-hidden="true"></i>';
        toastEl.appendChild(document.createTextNode(message));
        toastEl.classList.add('is-visible');
        clearTimeout(toastTimer);
        toastTimer = setTimeout(function () { toastEl.classList.remove('is-visible'); }, 2200);
    }
    function copy(value) {
        if (navigator.clipboard && window.isSecureContext) return navigator.clipboard.writeText(value);
        return new Promise(function (resolve, reject) {
            var area = document.createElement('textarea');
            area.value = value;
            area.setAttribute('readonly', '');
            area.style.position = 'fixed';
            area.style.opacity = '0';
            document.body.appendChild(area);
            area.select();
            try { if (document.execCommand('copy')) resolve(); else reject(); } catch (err) { reject(err); }
            document.body.removeChild(area);
        });
    }

    sections.forEach(function (section) {
        var heading = section.querySelector('h2');
        if (!heading) return;
        var button = document.createElement('button');
        button.type = 'button';
        button.className = 'legal-anchor';
        button.setAttribute('aria-label', text('copy-label') + ': ' + heading.textContent.trim());
        button.innerHTML = '<i class="fa-solid fa-link" aria-hidden="true"></i>';
        button.addEventListener('click', function () {
            var url = location.origin + location.pathname + '#' + section.id;
            history.replaceState(null, '', '#' + section.id);
            copy(url).then(function () { toast(text('copied')); }, function () { toast(url); });
        });
        heading.appendChild(button);
    });
})();
