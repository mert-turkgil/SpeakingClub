// ============================================================================
// auth.js — login and sign-up pages (Account/Login, Account/Register).
// Show/hide password, Caps Lock hint, password strength + rules (the same
// rules Identity enforces: 8+ characters, upper, lower, digit), confirm-match
// check, a loading state on submit, the Turnstile captcha in the site theme,
// and the bot checks (timestamp + honeypot) the forms already had.
// ============================================================================
(function () {
    'use strict';

    const root = document.querySelector('.auth');
    if (!root) return;
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    // Show / hide password
    root.querySelectorAll('[data-toggle-password]').forEach((button) => {
        const input = document.getElementById(button.dataset.togglePassword);
        if (!input) return;
        button.addEventListener('click', () => {
            const show = input.type === 'password';
            input.type = show ? 'text' : 'password';
            button.setAttribute('aria-pressed', show ? 'true' : 'false');
            button.setAttribute('aria-label', show ? button.dataset.labelHide : button.dataset.labelShow);
            button.querySelector('i').className = show ? 'fa-regular fa-eye-slash' : 'fa-regular fa-eye';
            input.focus({ preventScroll: true });
        });
    });

    // Caps Lock warning on password fields
    root.querySelectorAll('input[type="password"]').forEach((input) => {
        const hint = root.querySelector('[data-caps-for="' + input.id + '"]');
        if (!hint) return;
        const check = (e) => { if (e.getModifierState) hint.classList.toggle('is-visible', e.getModifierState('CapsLock')); };
        input.addEventListener('keydown', check);
        input.addEventListener('keyup', check);
        input.addEventListener('blur', () => hint.classList.remove('is-visible'));
    });

    // Password strength (sign-up)
    const meter = root.querySelector('[data-strength-for]');
    if (meter) {
        const password = document.getElementById(meter.dataset.strengthFor);
        const rules = root.querySelector('[data-rules]');
        const tests = {
            length: (v) => v.length >= 8,
            upper: (v) => /[A-ZÇĞİÖŞÜÄ]/.test(v),
            lower: (v) => /[a-zçğıöşüäß]/.test(v),
            digit: (v) => /\d/.test(v)
        };
        const update = () => {
            const value = password.value;
            let met = 0;
            Object.keys(tests).forEach((rule) => {
                const ok = tests[rule](value);
                if (ok) met++;
                const item = rules && rules.querySelector('[data-rule="' + rule + '"]');
                if (item) item.classList.toggle('is-met', ok);
            });
            meter.dataset.score = value ? String(Math.max(1, met)) : '0';
            meter.setAttribute('aria-valuenow', value ? String(met) : '0');
        };
        password.addEventListener('input', update);
        update();
    }

    // Confirm password: the field turns green once it matches
    const confirm = root.querySelector('[data-match-for]');
    if (confirm) {
        const original = document.getElementById(confirm.dataset.matchFor);
        const box = confirm.closest('.auth-input');
        const update = () => box.classList.toggle('is-valid', !!confirm.value && confirm.value === original.value);
        confirm.addEventListener('input', update);
        original.addEventListener('input', update);
    }

    // Forms: bot checks + loading state
    root.querySelectorAll('form[data-auth-form]').forEach((form) => {
        const ts = form.querySelector('input[name="_ts"]');
        if (ts) ts.value = Date.now();
        form.addEventListener('submit', (e) => {
            const trap = ['honeypot', 'phone', 'website'].some((name) => {
                const field = form.querySelector('input[name="' + name + '"]');
                return field && field.value;
            });
            if (trap) { e.preventDefault(); return; }
            // jQuery Unobtrusive Validation stops invalid forms; only show the spinner for one that will post
            const valid = window.jQuery && window.jQuery(form).valid ? window.jQuery(form).valid() : form.checkValidity();
            if (!valid) return;
            const submit = form.querySelector('.auth-submit');
            if (submit) {
                submit.classList.add('is-loading');
                submit.setAttribute('aria-busy', 'true');
            }
        });
    });
    window.addEventListener('pageshow', (e) => {
        if (e.persisted) root.querySelectorAll('.auth-submit.is-loading').forEach((b) => { b.classList.remove('is-loading'); b.removeAttribute('aria-busy'); });
    });

    // Login ⇄ sign-up tabs: the pill slides before the page changes
    const tabs = root.querySelector('.auth__tabs');
    if (tabs && !reduceMotion) {
        tabs.querySelectorAll('a:not([aria-current])').forEach((link) => {
            link.addEventListener('click', (e) => {
                if (e.metaKey || e.ctrlKey || e.shiftKey || e.button !== 0) return;
                e.preventDefault();
                tabs.classList.add('is-leaving');
                setTimeout(() => { window.location.href = link.href; }, 260);
            });
        });
    }

    // Cloudflare Turnstile, rendered in the site's current theme
    window.onloadTurnstileAuth = function () {
        const box = document.getElementById('turnstile-widget');
        if (!box || !window.turnstile) return;
        window.turnstile.render(box, {
            sitekey: box.dataset.sitekey,
            theme: document.documentElement.getAttribute('data-bs-theme') === 'dark' ? 'dark' : 'light',
            language: document.documentElement.lang || 'auto'
        });
    };
})();
