// ============================================================================
// account.js — "Hesabım" (Account/Account): counters and score rings that
// animate into view, the score chart drawing in, the running-exam countdown
// and the delete-account dialog. blog.js handles the scroll reveals.
// ============================================================================
(function () {
    'use strict';

    const root = document.querySelector('.acct');
    if (!root) return;
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    function whenVisible(el, fn) {
        if (reduceMotion || !('IntersectionObserver' in window)) { fn(); return; }
        const observer = new IntersectionObserver((entries) => {
            if (!entries.some((e) => e.isIntersecting)) return;
            observer.disconnect();
            fn();
        }, { threshold: 0.25 });
        observer.observe(el);
    }

    // Numbers count up from 0
    root.querySelectorAll('[data-count]').forEach((el) => {
        const target = parseInt(el.dataset.count, 10);
        if (!target || reduceMotion) return;
        el.textContent = '0';
        whenVisible(el, () => {
            const started = performance.now();
            const duration = 900;
            const step = (now) => {
                const t = Math.min(1, (now - started) / duration);
                el.textContent = String(Math.round(target * (1 - Math.pow(1 - t, 3))));
                if (t < 1) requestAnimationFrame(step);
            };
            requestAnimationFrame(step);
        });
    });

    // Rings fill and the chart draws itself when they come into view (account.css keeps them empty until then)
    root.querySelectorAll('.qz-ring').forEach((ring) => whenVisible(ring, () => requestAnimationFrame(() => ring.classList.add('is-drawn'))));
    root.querySelectorAll('[data-chart]').forEach((chart) => whenVisible(chart, () => requestAnimationFrame(() => chart.classList.add('is-drawn'))));

    // Running exam countdown
    const countdown = root.querySelector('[data-countdown]');
    if (countdown) {
        const endsAt = Date.now() + parseInt(countdown.dataset.countdown, 10) * 1000;
        const tick = () => {
            const s = Math.max(0, Math.ceil((endsAt - Date.now()) / 1000));
            countdown.textContent = Math.floor(s / 60) + ':' + String(s % 60).padStart(2, '0');
            if (s > 0) setTimeout(tick, 1000);
        };
        tick();
    }

    // Dialogs (delete account)
    root.querySelectorAll('[data-open-dialog]').forEach((button) => {
        const dialog = document.getElementById(button.dataset.openDialog);
        if (!dialog || typeof dialog.showModal !== 'function') return;
        button.addEventListener('click', () => dialog.showModal());
        dialog.querySelectorAll('[data-close-dialog]').forEach((b) => b.addEventListener('click', () => dialog.close()));
        // A click on the backdrop closes it
        dialog.addEventListener('click', (e) => { if (e.target === dialog) dialog.close(); });
    });
})();
