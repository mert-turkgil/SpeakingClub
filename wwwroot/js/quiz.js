// ============================================================================
// quiz.js — the quiz list (/sinavlar, /pruefungen), the exam lobby
// (Account/StartQuiz) and the result page (Account/ReviewQuiz).
// blog.js handles the shared parts (scroll reveal, card glow, broken images);
// the exam room itself is exam.js. Everything here is progressive enhancement.
// ============================================================================
(function () {
    'use strict';

    const root = document.querySelector('.quiz-page[data-page]');
    if (!root) return;

    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const fmt = (s, ...args) => String(s || '').replace(/\{(\d)\}/g, (_, i) => args[i]);
    const readJson = (id) => {
        const node = document.getElementById(id);
        try { return node ? JSON.parse(node.textContent) : {}; } catch (e) { return {}; }
    };

    // Runs fn once the element scrolls into view (immediately without IntersectionObserver)
    function whenVisible(el, fn) {
        if (!('IntersectionObserver' in window)) { fn(); return; }
        const observer = new IntersectionObserver((entries) => {
            if (!entries.some((e) => e.isIntersecting)) return;
            observer.disconnect();
            fn();
        }, { threshold: 0.2 });
        observer.observe(el);
    }

    // Numbers that count up from 0 (stats, result KPIs)
    function countUp(el) {
        const target = parseInt(el.dataset.count, 10);
        if (!target || reduceMotion) return;
        el.textContent = '0';
        whenVisible(el, () => {
            const duration = 900 + Math.min(target, 100) * 6;
            const started = performance.now();
            const step = (now) => {
                const t = Math.min(1, (now - started) / duration);
                el.textContent = String(Math.round(target * (1 - Math.pow(1 - t, 3))));
                if (t < 1) requestAnimationFrame(step);
            };
            requestAnimationFrame(step);
        });
    }

    // Score rings draw themselves when they come into view (quiz.css keeps them empty until .is-drawn)
    function initRings() {
        document.querySelectorAll('.qz-ring').forEach((ring) => {
            if (reduceMotion) { ring.classList.add('is-drawn'); return; }
            whenVisible(ring, () => requestAnimationFrame(() => ring.classList.add('is-drawn')));
        });
    }

    function initNotices() {
        document.addEventListener('click', (e) => {
            const button = e.target.closest('[data-dismiss]');
            const notice = button && button.closest('.qz-notice');
            if (!notice) return;
            notice.style.transition = 'opacity 0.25s ease, transform 0.25s ease';
            notice.style.opacity = '0';
            notice.style.transform = 'translateY(-6px)';
            setTimeout(() => notice.remove(), reduceMotion ? 0 : 260);
        });
    }

    // Local exam copies are only needed while an attempt is open
    function cleanExamStorage(attemptId) {
        try {
            if (attemptId) localStorage.removeItem('exam:' + attemptId);
            Object.keys(localStorage).forEach((key) => {
                if (key.indexOf('exam:') !== 0) return;
                const saved = JSON.parse(localStorage.getItem(key) || '{}');
                if (!saved.savedAt || Date.now() - saved.savedAt > 2 * 86400000) localStorage.removeItem(key);
            });
        } catch (e) { /* storage disabled */ }
    }

    // ------------------------------------------------------------------
    // Quiz list: search, level and category filters, pagination
    // ------------------------------------------------------------------
    function initList() {
        const grid = document.getElementById('quizGrid');
        if (!grid) return;
        const cards = Array.from(grid.querySelectorAll('.qz-card'));
        const search = document.getElementById('quizSearch');
        const count = document.getElementById('quizCount');
        const empty = document.getElementById('quizEmpty');
        const pager = document.getElementById('quizPages');
        const perPage = 9;
        const filters = { level: '', category: '', q: '' };
        let page = 1;

        // Case- and accent-insensitive matching (Turkish ı/İ and German umlauts included)
        const normalize = (s) => String(s || '').toLocaleLowerCase('tr').replace(/ı/g, 'i')
            .normalize('NFD').replace(/[̀-ͯ]/g, '').replace(/ß/g, 'ss');
        cards.forEach((card) => { card._search = normalize(card.dataset.search); });

        function matches(card) {
            if (filters.level && card.dataset.level !== filters.level) return false;
            if (filters.category && card.dataset.category !== filters.category) return false;
            return filters.q.split(/\s+/).every((word) => !word || card._search.indexOf(word) >= 0);
        }

        function renderPager(pages) {
            pager.innerHTML = '';
            if (pages < 2) return;
            const add = (label, target, options) => {
                const li = document.createElement('li');
                const b = document.createElement('button');
                b.type = 'button';
                b.innerHTML = label;
                if (options && options.aria) b.setAttribute('aria-label', options.aria);
                if (target === page && !(options && options.arrow)) b.setAttribute('aria-current', 'page');
                b.disabled = target < 1 || target > pages;
                b.addEventListener('click', () => {
                    page = target;
                    render(true);
                    const top = grid.getBoundingClientRect().top + window.scrollY - 140;
                    window.scrollTo({ top: top, behavior: reduceMotion ? 'auto' : 'smooth' });
                });
                li.appendChild(b);
                pager.appendChild(li);
            };
            add('<i class="fa-solid fa-arrow-left" aria-hidden="true"></i>', page - 1, { arrow: true, aria: root.dataset.labelPrev });
            for (let i = 1; i <= pages; i++) add(String(i), i);
            add('<i class="fa-solid fa-arrow-right" aria-hidden="true"></i>', page + 1, { arrow: true, aria: root.dataset.labelNext });
        }

        function render(animate) {
            const visible = cards.filter(matches);
            const pages = Math.max(1, Math.ceil(visible.length / perPage));
            page = Math.min(page, pages);
            const onPage = new Set(visible.slice((page - 1) * perPage, page * perPage));
            let n = 0;
            cards.forEach((card) => {
                const show = onPage.has(card);
                card.classList.toggle('is-filtered-out', !show);
                card.classList.remove('is-entering');
                if (show && animate && !reduceMotion) {
                    void card.offsetWidth;
                    card.style.animationDelay = (n++ * 45) + 'ms';
                    card.classList.add('is-entering', 'is-visible');
                }
            });
            const parts = (root.dataset.labelCount || '{0}').split('{0}');
            count.innerHTML = '';
            count.append(parts[0], Object.assign(document.createElement('strong'), { textContent: visible.length }), parts[1] || '');
            empty.classList.toggle('is-visible', visible.length === 0);
            renderPager(pages);
        }

        function syncUrl() {
            if (!window.history.replaceState) return;
            const url = new URL(window.location.href);
            if (filters.level) url.searchParams.set('level', filters.level); else url.searchParams.delete('level');
            window.history.replaceState(null, '', url.pathname + url.search + url.hash);
        }

        function select(group, value) {
            group.querySelectorAll('[data-value]').forEach((b) => {
                const on = b.dataset.value === value;
                b.classList.toggle('is-active', on);
                b.setAttribute('aria-pressed', on ? 'true' : 'false');
            });
            filters[group.dataset.filter] = value;
        }

        document.querySelectorAll('[data-filter]').forEach((group) => {
            group.addEventListener('click', (e) => {
                const button = e.target.closest('[data-value]');
                if (!button) return;
                select(group, button.dataset.value);
                page = 1;
                if (group.dataset.filter === 'level') syncUrl();
                render(true);
            });
        });

        let searchTimer = null;
        if (search) {
            search.addEventListener('input', () => {
                clearTimeout(searchTimer);
                searchTimer = setTimeout(() => {
                    filters.q = normalize(search.value.trim());
                    page = 1;
                    render(true);
                }, 140);
            });
            // "/" jumps to the search field, Esc clears it
            document.addEventListener('keydown', (e) => {
                if (e.key === '/' && !/^(input|textarea|select)$/i.test(document.activeElement.tagName)) {
                    e.preventDefault();
                    search.focus();
                } else if (e.key === 'Escape' && document.activeElement === search && search.value) {
                    search.value = '';
                    search.dispatchEvent(new Event('input'));
                }
            });
        }

        document.querySelectorAll('[data-reset]').forEach((b) => b.addEventListener('click', () => {
            document.querySelectorAll('[data-filter]').forEach((group) => select(group, ''));
            if (search) search.value = '';
            filters.q = '';
            page = 1;
            syncUrl();
            render(true);
        }));

        const preselected = root.dataset.selectedLevel;
        const levelGroup = document.querySelector('[data-filter="level"]');
        if (preselected && levelGroup && levelGroup.querySelector('[data-value="' + preselected + '"]')) select(levelGroup, preselected);
        render(false);
    }

    // ------------------------------------------------------------------
    // Lobby: system check, honour pledge, running attempt countdown
    // ------------------------------------------------------------------
    function initLobby() {
        const strings = readJson('lobbyStrings');
        const icons = { ok: 'fa-circle-check', warn: 'fa-circle-exclamation', fail: 'fa-circle-xmark', pending: 'fa-circle-notch' };

        function setCheck(name, checkState, detail) {
            const item = document.querySelector('[data-check="' + name + '"]');
            if (!item) return;
            item.dataset.state = checkState;
            item.querySelector('i').className = 'fa-solid ' + icons[checkState];
            item.querySelector('[data-detail]').textContent = detail || '';
        }

        const checks = [
            () => {
                const ok = !!(document.fullscreenEnabled || document.webkitFullscreenEnabled);
                setCheck('fullscreen', ok ? 'ok' : 'warn', ok ? strings.fullscreenOk : strings.fullscreenNo);
            },
            () => setCheck('online', navigator.onLine ? 'ok' : 'fail', navigator.onLine ? strings.online : strings.offline),
            () => {
                let ok = false;
                try { localStorage.setItem('exam:probe', '1'); localStorage.removeItem('exam:probe'); ok = true; } catch (e) { ok = false; }
                setCheck('storage', ok ? 'ok' : 'warn', ok ? strings.storageOk : strings.storageNo);
            },
            () => {
                const w = window.innerWidth;
                const h = window.innerHeight;
                setCheck('screen', w >= 600 ? 'ok' : 'warn', fmt(w >= 600 ? strings.screenOk : strings.screenSmall, w, h));
            }
        ];
        // One after another, so the student sees the check happen
        checks.forEach((check, i) => setTimeout(check, reduceMotion ? 0 : 350 + i * 320));
        window.addEventListener('online', checks[1]);
        window.addEventListener('offline', checks[1]);
        let resizeTimer = null;
        window.addEventListener('resize', () => { clearTimeout(resizeTimer); resizeTimer = setTimeout(checks[3], 200); });

        const pledge = document.querySelector('[data-pledge]');
        const start = document.querySelector('[data-start]');
        if (pledge && start) {
            const sync = () => { start.disabled = !pledge.checked; };
            pledge.addEventListener('change', sync);
            sync(); // the browser may restore the checkbox on back navigation
        }

        const form = document.querySelector('[data-begin-form]');
        if (form) {
            form.addEventListener('submit', () => {
                const button = form.querySelector('button[type="submit"]');
                if (!button) return;
                // Prevent a double start; re-enabled if the page is restored from the back/forward cache
                setTimeout(() => { button.disabled = true; button.setAttribute('aria-busy', 'true'); }, 0);
            });
            window.addEventListener('pageshow', (e) => {
                if (!e.persisted) return;
                const button = form.querySelector('button[type="submit"]');
                if (button) { button.removeAttribute('aria-busy'); button.disabled = pledge ? !pledge.checked : false; }
            });
        }

        const countdown = document.querySelector('[data-countdown]');
        const remaining = parseInt(root.dataset.remaining, 10);
        if (countdown && remaining > 0) {
            const endsAt = Date.now() + remaining * 1000;
            const tick = () => {
                const s = Math.max(0, Math.ceil((endsAt - Date.now()) / 1000));
                countdown.textContent = s > 0 ? Math.floor(s / 60) + ':' + String(s % 60).padStart(2, '0') : strings.expired;
                countdown.classList.toggle('is-urgent', s > 0 && s <= 60);
                if (s > 0) setTimeout(tick, 1000 - ((endsAt - Date.now()) % 1000 || 0) + 5);
            };
            tick();
        }
    }

    // ------------------------------------------------------------------
    // Result page: breakdown bar, answer review filter, confetti
    // ------------------------------------------------------------------
    function initResult() {
        cleanExamStorage(root.dataset.attempt);

        const breakdown = document.querySelector('.qr-breakdown');
        if (breakdown) {
            whenVisible(breakdown, () => requestAnimationFrame(() => {
                breakdown.querySelectorAll('[data-width]').forEach((span, i) => {
                    span.style.transitionDelay = reduceMotion ? '0s' : (0.15 + i * 0.12) + 's';
                    span.style.width = span.dataset.width + '%';
                });
            }));
        }

        const list = document.querySelector('.qr-list');
        const chips = document.querySelector('[data-review-filter]');
        if (list && chips) {
            const items = Array.from(list.querySelectorAll('.qr-item'));
            chips.addEventListener('click', (e) => {
                const button = e.target.closest('[data-value]');
                if (!button) return;
                const value = button.dataset.value;
                chips.querySelectorAll('[data-value]').forEach((b) => {
                    const on = b === button;
                    b.classList.toggle('is-active', on);
                    b.setAttribute('aria-pressed', on ? 'true' : 'false');
                });
                let n = 0;
                items.forEach((item) => {
                    const show = !value || item.dataset.status === value;
                    item.classList.toggle('is-hidden', !show);
                    item.classList.remove('is-entering');
                    if (show && !reduceMotion) {
                        void item.offsetWidth;
                        item.style.animationDelay = Math.min(n++, 12) * 35 + 'ms';
                        item.classList.add('is-entering', 'is-visible');
                    }
                });
                const emptyNote = list.querySelector('[data-review-empty]');
                if (emptyNote) emptyNote.hidden = items.some((item) => !item.classList.contains('is-hidden'));
            });
        }

        const score = parseInt(root.dataset.score, 10);
        if (score >= 80 && root.dataset.endReason !== 'violations' && !reduceMotion) setTimeout(confetti, 700);
    }

    function confetti() {
        const canvas = document.createElement('canvas');
        canvas.className = 'qr-confetti';
        canvas.setAttribute('aria-hidden', 'true');
        document.body.appendChild(canvas);
        const ctx = canvas.getContext('2d');
        const dpr = Math.min(window.devicePixelRatio || 1, 2);
        const resize = () => {
            canvas.width = window.innerWidth * dpr;
            canvas.height = window.innerHeight * dpr;
            ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
        };
        resize();
        const colors = ['#FF9494', '#E9573F', '#FFD1D1', '#2B2220', '#F6B93B', '#3BA776'];
        const w = window.innerWidth;
        const pieces = Array.from({ length: Math.min(160, Math.round(w / 6)) }, (_, i) => {
            const fromLeft = i % 2 === 0;
            return {
                x: fromLeft ? -10 : w + 10,
                y: window.innerHeight * (0.55 + Math.random() * 0.25),
                vx: (fromLeft ? 1 : -1) * (4 + Math.random() * 7),
                vy: -(9 + Math.random() * 8),
                size: 5 + Math.random() * 6,
                rotation: Math.random() * Math.PI,
                spin: (Math.random() - 0.5) * 0.3,
                color: colors[i % colors.length],
                round: Math.random() < 0.3
            };
        });
        const started = performance.now();
        const frame = (now) => {
            const elapsed = now - started;
            ctx.clearRect(0, 0, canvas.width, canvas.height);
            ctx.globalAlpha = elapsed > 2600 ? Math.max(0, 1 - (elapsed - 2600) / 900) : 1;
            pieces.forEach((p) => {
                p.vy += 0.32;
                p.vx *= 0.985;
                p.x += p.vx;
                p.y += p.vy;
                p.rotation += p.spin;
                ctx.save();
                ctx.translate(p.x, p.y);
                ctx.rotate(p.rotation);
                ctx.fillStyle = p.color;
                if (p.round) { ctx.beginPath(); ctx.arc(0, 0, p.size / 2, 0, Math.PI * 2); ctx.fill(); }
                else ctx.fillRect(-p.size / 2, -p.size / 4, p.size, p.size / 2);
                ctx.restore();
            });
            if (elapsed < 3500) requestAnimationFrame(frame);
            else canvas.remove();
        };
        requestAnimationFrame(frame);
    }

    // ------------------------------------------------------------------
    initNotices();
    initRings();
    document.querySelectorAll('[data-count]').forEach(countUp);
    if (root.dataset.page === 'quiz-list') initList();
    if (root.dataset.page === 'quiz-lobby') initLobby();
    if (root.dataset.page === 'quiz-result') initResult();
})();
