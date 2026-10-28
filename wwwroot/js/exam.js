// ============================================================================
// exam.js — the exam room (Account/TakeQuiz).
//
// The server owns the attempt: its deadline, the answers it has autosaved and
// the violations it has counted. This script keeps the student informed and
// reports to it:
//   • countdown synced to the server clock; at 0 the exam is submitted
//   • autosave of answers (debounced + heartbeat), with a local copy so a
//     reload or a dropped connection loses nothing
//   • integrity monitoring like a university online exam: fullscreen, tab
//     switches, copy/paste and printing are reported; one incident counts once;
//     at the limit the exam ends
//   • question navigator, flags, keyboard shortcuts, review-before-submit
// ============================================================================
(function () {
    'use strict';

    const root = document.getElementById('exam');
    if (!root) return;

    const strings = JSON.parse(document.getElementById('examStrings').textContent || '{}');
    const savedOnServer = JSON.parse(document.getElementById('examSaved').textContent || '{}');
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const fmt = (s, ...args) => String(s || '').replace(/\{(\d)\}/g, (_, i) => args[i]);

    const attemptId = root.dataset.attempt;
    const saveUrl = root.dataset.saveUrl;
    const limitMs = (parseInt(root.dataset.limit, 10) || 1) * 1000;
    const maxViolations = parseInt(root.dataset.maxViolations, 10) || 3;
    const total = parseInt(root.dataset.total, 10) || 0;
    // Deadline in this device's clock (corrects a wrong system time)
    const clockOffset = Date.parse(root.dataset.serverNow) - Date.now();
    let expiresAt = Date.parse(root.dataset.expires) - clockOffset;

    const form = document.getElementById('examForm');
    const token = form.querySelector('input[name="__RequestVerificationToken"]').value;
    const questions = Array.from(form.querySelectorAll('.exam-q'));
    const gridButtons = Array.from(document.querySelectorAll('#examGrid [data-go]'));
    const side = document.getElementById('examSide');
    const backdrop = document.getElementById('sheetBackdrop');
    const el = {
        timer: document.getElementById('timer'),
        timeLeft: document.getElementById('timeLeft'),
        timeLine: document.getElementById('timeLine'),
        announce: document.getElementById('timeAnnounce'),
        saveState: document.getElementById('saveState'),
        integrity: document.getElementById('integrity'),
        violationCount: document.getElementById('violationCount'),
        answeredBar: document.getElementById('answeredBar'),
        toasts: document.getElementById('toasts'),
        prev: form.querySelector('[data-prev]'),
        next: form.querySelector('[data-next]'),
        review: form.querySelector('[data-review]'),
        gate: document.getElementById('gateDialog'),
        fullscreen: document.getElementById('fullscreenDialog'),
        violation: document.getElementById('violationDialog'),
        reviewDialog: document.getElementById('reviewDialog'),
        busy: document.getElementById('busyDialog')
    };

    const storageKey = 'exam:' + attemptId;
    const state = {
        phase: 'gate',          // gate → active ⇄ locked → submitting
        index: 0,
        answers: {},            // questionId → answerId
        flags: {},              // questionId → true
        times: {},              // questionId → seconds spent
        pendingEvents: [],      // integrity events not yet confirmed by the server
        violations: parseInt(root.dataset.violations, 10) || 0,
        questionStartedAt: Date.now(),
        lastViolationAt: 0,
        suppressUntil: 0,       // while away from the tab, related events belong to the same incident
        awayViolation: false,
        fullscreenEntered: false,
        unloadAt: 0,            // when the "leave page?" prompt was last shown
        leftAt: 0               // when the page was last left (reload, navigation), from the local copy
    };
    const awayLimitMs = 20000;  // a reload is fine; being away longer counts like a tab switch

    // ------------------------------------------------------------------
    // Persistence (local copy of everything the server may not have yet)
    // ------------------------------------------------------------------
    function persist() {
        try {
            localStorage.setItem(storageKey, JSON.stringify({
                answers: state.answers, flags: state.flags, times: state.times,
                index: state.index, events: state.pendingEvents, leftAt: state.leftAt, savedAt: Date.now()
            }));
        } catch (e) { /* private mode or full storage: the server autosave still runs */ }
    }

    function restore() {
        Object.keys(savedOnServer).forEach((q) => { state.answers[q] = savedOnServer[q]; });
        try {
            const local = JSON.parse(localStorage.getItem(storageKey) || 'null');
            if (local) {
                Object.assign(state.answers, local.answers || {});
                Object.assign(state.flags, local.flags || {});
                Object.assign(state.times, local.times || {});
                state.pendingEvents = Array.isArray(local.events) ? local.events : [];
                if (local.index >= 0 && local.index < total) state.index = local.index;
                state.leftAt = local.leftAt || 0;
            }
        } catch (e) { /* ignore a corrupt copy */ }

        Object.keys(state.answers).forEach((q) => {
            const input = form.querySelector('input[name="responses[' + q + ']"][value="' + state.answers[q] + '"]');
            if (input) input.checked = true; else delete state.answers[q];
        });
        questions.forEach((article, i) => {
            const q = article.dataset.q;
            const flagged = !!state.flags[q];
            article.querySelector('[data-flag]').setAttribute('aria-pressed', flagged ? 'true' : 'false');
            gridButtons[i].classList.toggle('is-flagged', flagged);
            gridButtons[i].classList.toggle('is-answered', state.answers[q] != null);
        });
    }

    // ------------------------------------------------------------------
    // Small UI helpers
    // ------------------------------------------------------------------
    function toast(message, icon) {
        const item = document.createElement('div');
        item.className = 'exam-toast';
        item.innerHTML = '<i class="fa-solid ' + (icon || 'fa-circle-info') + '" aria-hidden="true"></i>';
        const text = document.createElement('span');
        text.textContent = message;
        item.appendChild(text);
        el.toasts.appendChild(item);
        setTimeout(() => {
            item.classList.add('is-leaving');
            setTimeout(() => item.remove(), 320);
        }, 4500);
    }

    function setSaveState(saveState) {
        el.saveState.dataset.state = saveState;
        const icon = saveState === 'saving' ? 'fa-rotate' : saveState === 'offline' ? 'fa-cloud-arrow-up' : 'fa-cloud';
        el.saveState.innerHTML = '<i class="fa-solid ' + icon + '" aria-hidden="true"></i>';
        const label = document.createElement('span');
        label.textContent = strings[saveState] || '';
        el.saveState.appendChild(label);
    }

    function closeDialogs() {
        [el.gate, el.fullscreen, el.violation, el.reviewDialog].forEach((d) => { if (d && d.open) d.close(); });
    }

    // ------------------------------------------------------------------
    // Timer
    // ------------------------------------------------------------------
    const milestones = [600, 300, 60];
    const announced = new Set(milestones.filter((m) => expiresAt - Date.now() <= m * 1000));

    function formatTime(ms) {
        const s = Math.max(0, Math.ceil(ms / 1000));
        const h = Math.floor(s / 3600);
        const m = Math.floor((s % 3600) / 60);
        const sec = String(s % 60).padStart(2, '0');
        return h ? h + ':' + String(m).padStart(2, '0') + ':' + sec : String(m).padStart(2, '0') + ':' + sec;
    }

    function tick() {
        const ms = expiresAt - Date.now();
        const text = formatTime(ms);
        el.timeLeft.textContent = text;
        document.querySelectorAll('[data-gate-time]').forEach((n) => { n.textContent = text; });
        el.timer.dataset.state = ms <= 60000 ? 'danger' : ms <= 300000 ? 'warn' : 'ok';
        el.timeLine.style.setProperty('--elapsed', Math.min(1, Math.max(0, 1 - ms / limitMs)).toFixed(4));

        milestones.forEach((m) => {
            if (!announced.has(m) && ms <= m * 1000) {
                announced.add(m);
                el.announce.textContent = fmt(strings.minutesLeft, m / 60);
                if (m === 300) toast(strings.fiveMinutes, 'fa-hourglass-half');
                if (m === 60) toast(strings.oneMinute, 'fa-hourglass-end');
            }
        });

        if (ms <= 0 && state.phase !== 'submitting') submit('timeout');
    }

    // ------------------------------------------------------------------
    // Navigation
    // ------------------------------------------------------------------
    function recordTime() {
        if (state.phase !== 'active') { state.questionStartedAt = Date.now(); return; }
        const q = questions[state.index].dataset.q;
        state.times[q] = (state.times[q] || 0) + Math.round((Date.now() - state.questionStartedAt) / 1000);
        state.questionStartedAt = Date.now();
    }

    // Seconds per question including the one on screen now, without resetting its clock (sent with the autosave,
    // so an attempt collected after the deadline still has its timings)
    function timesSnapshot() {
        const t = Object.assign({}, state.times);
        if (state.phase === 'active') {
            const q = questions[state.index].dataset.q;
            t[q] = (t[q] || 0) + Math.round((Date.now() - state.questionStartedAt) / 1000);
        }
        return t;
    }

    function show(i, direction, focus) {
        if (i < 0 || i >= total) return;
        recordTime();
        const current = questions[state.index];
        const next = questions[i];
        if (current !== next) {
            current.hidden = true;
            next.hidden = false;
            if (!reduceMotion) {
                next.classList.remove('is-enter-next', 'is-enter-prev');
                void next.offsetWidth; // restart the animation
                next.classList.add(direction < 0 ? 'is-enter-prev' : 'is-enter-next');
            }
        }
        state.index = i;
        gridButtons.forEach((b, n) => { if (n === i) b.setAttribute('aria-current', 'step'); else b.removeAttribute('aria-current'); });
        el.prev.disabled = i === 0;
        const last = i === total - 1;
        el.next.hidden = last;
        el.review.hidden = !last;
        if (focus) {
            next.setAttribute('tabindex', '-1');
            next.focus({ preventScroll: true });
        }
        const top = form.getBoundingClientRect().top;
        if (top < 0) window.scrollTo({ top: window.scrollY + top - 90, behavior: reduceMotion ? 'auto' : 'smooth' });
        persist();
    }

    function updateProgress() {
        const answered = Object.keys(state.answers).length;
        document.querySelectorAll('[data-answered-short]').forEach((n) => { n.textContent = answered + '/' + total; });
        document.querySelectorAll('[data-answered-text]').forEach((n) => { n.textContent = fmt(strings.answered, answered, total); });
        el.answeredBar.style.setProperty('--p', total ? (answered / total).toFixed(4) : 0);
    }

    function setSheet(open) {
        side.classList.toggle('is-open', open);
        backdrop.classList.toggle('is-open', open);
    }

    // ------------------------------------------------------------------
    // Autosave
    // ------------------------------------------------------------------
    let saveTimer = null;
    let saving = false;
    let retryDelay = 2000;

    function scheduleSave(delay) {
        clearTimeout(saveTimer);
        saveTimer = setTimeout(save, delay == null ? 700 : delay);
    }

    async function save(options) {
        if (saving) { scheduleSave(600); return; }
        if (state.phase === 'submitting' && !(options && options.final)) return;
        saving = true;
        setSaveState('saving');
        const events = state.pendingEvents.splice(0);
        // A request that hangs must not block every later save; events carry ids, so a retry is safe
        const controller = window.AbortController ? new AbortController() : null;
        const abortTimer = setTimeout(() => { if (controller) controller.abort(); }, 15000);
        try {
            const response = await fetch(saveUrl, {
                method: 'POST',
                credentials: 'same-origin',
                signal: controller ? controller.signal : undefined,
                keepalive: !!(options && options.final),
                headers: { 'Content-Type': 'application/json', 'X-CSRF-TOKEN': token },
                body: JSON.stringify({ attemptId: attemptId, answers: state.answers, times: timesSnapshot(), events: events })
            });
            if (!response.ok) throw new Error('HTTP ' + response.status);
            const data = await response.json();
            retryDelay = 2000;
            setSaveState('saved');
            if (data.submitted && data.redirect) { leave(data.redirect); return; }
            if (typeof data.violations === 'number') setViolations(Math.max(data.violations, state.violations));
            if (typeof data.remaining === 'number') expiresAt = Date.now() + data.remaining * 1000;
        } catch (err) {
            state.pendingEvents = events.concat(state.pendingEvents);
            setSaveState('offline');
            if (state.phase !== 'submitting') {
                scheduleSave(retryDelay);
                retryDelay = Math.min(retryDelay * 2, 30000);
            }
        } finally {
            clearTimeout(abortTimer);
            saving = false;
            persist();
        }
    }

    // Sends whatever is pending once the save in flight (if any) has finished
    async function flush() {
        const until = Date.now() + 2500;
        while (saving && Date.now() < until) await new Promise((r) => setTimeout(r, 100));
        return save({ final: true });
    }

    // The server already finished the attempt (time up or violation limit)
    function leave(url) {
        submitted = true;
        state.phase = 'left';
        window.removeEventListener('beforeunload', onBeforeUnload);
        window.location.href = url;
    }

    // ------------------------------------------------------------------
    // Integrity monitoring
    // ------------------------------------------------------------------
    function setViolations(count) {
        const bumped = count > state.violations;
        state.violations = count;
        el.violationCount.textContent = count;
        el.integrity.dataset.level = count === 0 ? '0' : count === 1 ? '1' : '2';
        if (bumped) {
            el.integrity.classList.remove('is-bumped');
            void el.integrity.offsetWidth;
            el.integrity.classList.add('is-bumped');
        }
    }

    function violationText() { return fmt(strings.violationCount, state.violations, maxViolations); }

    // Reports an event; returns true when it counted as a new violation
    function report(type, isViolation) {
        if (state.phase !== 'active' && state.phase !== 'locked') return false;
        const now = Date.now();
        if (isViolation && (now - state.lastViolationAt < 1500 || now < state.suppressUntil)) return false; // same incident
        const id = now.toString(36) + Math.random().toString(36).slice(2, 8);
        state.pendingEvents.push({ id: id, type: type, at: new Date(now + clockOffset).toISOString() });
        if (isViolation) {
            state.lastViolationAt = now;
            setViolations(state.violations + 1);
            persist();
            if (state.violations >= maxViolations) { terminate(); return true; }
            scheduleSave(150);
            return true;
        }
        scheduleSave(1500);
        persist();
        return false;
    }

    function showViolation(type) {
        if (state.phase !== 'active') return;
        el.violation.querySelector('[data-violation-reason]').textContent = strings['violation.' + type] || strings.violationTitle;
        el.violation.querySelector('[data-violation-detail]').textContent = violationText();
        if (!el.violation.open) el.violation.showModal();
    }

    function terminate() {
        // The server ends the attempt when it receives the last violation; the form post is the fallback
        const finish = () => { if (state.phase !== 'left') submit('violations'); };
        state.phase = 'submitting';
        closeDialogs();
        showBusy('violations');
        Promise.race([flush(), new Promise((r) => setTimeout(r, 4000))]).then(finish, finish);
    }

    const fullscreenSupported = !!(document.documentElement.requestFullscreen || document.documentElement.webkitRequestFullscreen) &&
        document.fullscreenEnabled !== false;
    const isFullscreen = () => !!(document.fullscreenElement || document.webkitFullscreenElement);

    function enterFullscreen() {
        const docEl = document.documentElement;
        const request = docEl.requestFullscreen || docEl.webkitRequestFullscreen;
        if (!request) return Promise.resolve();
        try {
            const result = request.call(docEl, { navigationUI: 'hide' });
            return result && result.then ? result.catch(() => {}) : Promise.resolve();
        } catch (e) {
            return Promise.resolve();
        }
    }

    function lock() {
        state.phase = 'locked';
        root.classList.add('is-locked');
        el.fullscreen.querySelector('[data-violation-detail]').textContent = violationText();
        closeDialogs();
        el.fullscreen.showModal();
    }

    function unlock() {
        state.phase = 'active';
        root.classList.remove('is-locked');
        if (el.fullscreen.open) el.fullscreen.close();
        state.questionStartedAt = Date.now();
    }

    function onFullscreenChange() {
        if (!state.fullscreenEntered || state.phase === 'submitting' || state.phase === 'gate') return;
        if (!isFullscreen() && state.phase === 'active') {
            // The "leave page?" prompt exits fullscreen too: count it only if the student stays on the page
            const delay = Date.now() - state.unloadAt < 1000 ? 1500 : 0;
            setTimeout(() => {
                if (state.phase !== 'active' || isFullscreen()) return;
                report('fullscreen-exit', true);
                if (state.phase === 'active') lock();
            }, delay);
        } else if (isFullscreen() && state.phase === 'locked') {
            unlock();
        }
    }

    function start() {
        state.fullscreenEntered = isFullscreen();
        state.phase = 'active';
        state.questionStartedAt = Date.now();
        if (el.gate.open) el.gate.close();
        // Back after a reload or navigation: a quick reload is logged, a long absence counts as leaving the exam
        if (state.leftAt) {
            const away = Date.now() - state.leftAt;
            state.leftAt = 0;
            if (away > awayLimitMs && report('tab-hidden', true)) showViolation('tab-hidden');
            else report('resume', false);
        }
        if (state.pendingEvents.length) scheduleSave(300);
        persist();
    }

    // ------------------------------------------------------------------
    // Review & submit
    // ------------------------------------------------------------------
    function openReview() {
        if (state.phase !== 'active') return;
        setSheet(false);
        const empty = [];
        const flagged = [];
        questions.forEach((article, i) => {
            const q = article.dataset.q;
            if (state.answers[q] == null) empty.push(i);
            if (state.flags[q]) flagged.push(i);
        });
        const dlg = el.reviewDialog;
        dlg.querySelector('[data-sum="answered"]').textContent = total - empty.length;
        dlg.querySelector('[data-sum="empty"]').textContent = empty.length;
        dlg.querySelector('[data-sum="flagged"]').textContent = flagged.length;
        [['empty', empty], ['flagged', flagged]].forEach(([key, list]) => {
            const box = dlg.querySelector('[data-jump="' + key + '"]');
            box.innerHTML = '';
            list.forEach((i) => {
                const b = document.createElement('button');
                b.type = 'button';
                b.textContent = String(i + 1);
                b.addEventListener('click', () => { dlg.close(); show(i, i > state.index ? 1 : -1, true); });
                box.appendChild(b);
            });
            box.hidden = list.length === 0;
            dlg.querySelector('[data-jump-' + key + '-label]').hidden = list.length === 0;
        });
        dlg.showModal();
    }

    function showBusy(reason) {
        const template = document.getElementById(reason === 'timeout' ? 'busyTimeout' : reason === 'violations' ? 'busyViolations' : null);
        if (template) {
            const [title, text] = template.innerHTML.split('|');
            el.busy.querySelector('[data-busy-title]').innerHTML = title;
            el.busy.querySelector('[data-busy-text]').innerHTML = text;
        }
        if (!el.busy.open) el.busy.showModal();
    }

    let submitted = false;
    function submit(reason) {
        if (submitted) return;
        submitted = true;
        if (state.phase === 'active') recordTime();
        state.phase = 'submitting';
        clearTimeout(saveTimer);
        document.getElementById('submitReason').value = reason;
        form.querySelectorAll('input[data-time]').forEach((n) => n.remove());
        Object.keys(state.times).forEach((q) => {
            const input = document.createElement('input');
            input.type = 'hidden';
            input.name = 'times[' + q + ']';
            input.value = state.times[q];
            input.setAttribute('data-time', '');
            form.appendChild(input);
        });
        window.removeEventListener('beforeunload', onBeforeUnload);
        persist(); // kept until the result page loads, in case this request fails
        closeDialogs();
        showBusy(reason);
        state.phase = 'left';
        form.submit();
    }

    function onBeforeUnload(e) {
        if (state.phase === 'active' || state.phase === 'locked') {
            state.unloadAt = Date.now();
            e.preventDefault();
            e.returnValue = strings.leaveWarning;
            return strings.leaveWarning;
        }
    }

    // ------------------------------------------------------------------
    // Watermark with the student's name (discourages screenshots and sharing)
    // ------------------------------------------------------------------
    function drawWatermark() {
        const box = document.getElementById('watermark');
        if (!box) return;
        const text = (root.dataset.student || '') + ' · ' + new Date().toLocaleDateString();
        const safe = text.replace(/[<>&'"]/g, (c) => ({ '<': '&lt;', '>': '&gt;', '&': '&amp;', "'": '&apos;', '"': '&quot;' }[c]));
        const svg = "<svg xmlns='http://www.w3.org/2000/svg' width='340' height='190'>" +
            "<text x='20' y='120' transform='rotate(-22 170 95)' font-family='Poppins, sans-serif' font-size='15' fill='#2B2220'>" + safe + '</text></svg>';
        box.style.backgroundImage = 'url("data:image/svg+xml;charset=utf-8,' + encodeURIComponent(svg) + '")';
    }

    // ------------------------------------------------------------------
    // Wiring
    // ------------------------------------------------------------------
    // A missing question image disappears instead of showing its alt text (currentSrc stays empty while a lazy image waits)
    function dropBrokenImage(img) {
        const media = img.closest('.exam-q__media');
        img.remove();
        if (media && !media.children.length) media.remove();
    }
    document.addEventListener('error', (e) => { if (e.target.tagName === 'IMG' && e.target.closest('.exam-q__media')) dropBrokenImage(e.target); }, true);
    form.querySelectorAll('.exam-q__media img').forEach((img) => { if (img.complete && img.naturalWidth === 0 && img.currentSrc) dropBrokenImage(img); });

    form.addEventListener('submit', (e) => { e.preventDefault(); openReview(); });

    form.addEventListener('change', (e) => {
        const input = e.target;
        if (!input.matches('input[type="radio"]')) return;
        const match = input.name.match(/\d+/);
        if (!match) return;
        const q = match[0];
        const isNew = state.answers[q] == null;
        state.answers[q] = parseInt(input.value, 10);
        const i = questions.findIndex((a) => a.dataset.q === q);
        if (i >= 0) {
            gridButtons[i].classList.add('is-answered');
            if (isNew && !reduceMotion) {
                gridButtons[i].classList.remove('is-pop');
                void gridButtons[i].offsetWidth;
                gridButtons[i].classList.add('is-pop');
            }
        }
        updateProgress();
        persist();
        scheduleSave();
    });

    form.addEventListener('click', (e) => {
        const flag = e.target.closest('[data-flag]');
        if (!flag) return;
        const article = flag.closest('.exam-q');
        const q = article.dataset.q;
        const on = !state.flags[q];
        if (on) state.flags[q] = true; else delete state.flags[q];
        flag.setAttribute('aria-pressed', on ? 'true' : 'false');
        gridButtons[questions.indexOf(article)].classList.toggle('is-flagged', on);
        persist();
    });

    el.prev.addEventListener('click', () => show(state.index - 1, -1));
    el.next.addEventListener('click', () => show(state.index + 1, 1));
    el.review.addEventListener('click', openReview);
    document.querySelector('[data-review-side]').addEventListener('click', openReview);
    document.querySelector('[data-open-nav]').addEventListener('click', () => setSheet(!side.classList.contains('is-open')));
    backdrop.addEventListener('click', () => setSheet(false));
    gridButtons.forEach((b, i) => b.addEventListener('click', () => { setSheet(false); show(i, i >= state.index ? 1 : -1, true); }));

    el.reviewDialog.querySelector('[data-close]').addEventListener('click', () => el.reviewDialog.close());
    el.reviewDialog.querySelector('[data-submit]').addEventListener('click', () => submit('submitted'));
    el.violation.querySelector('[data-close]').addEventListener('click', () => el.violation.close());

    el.gate.querySelector('[data-enter]').addEventListener('click', () => {
        if (fullscreenSupported) enterFullscreen().then(start); else start();
    });
    el.fullscreen.querySelector('[data-reenter]').addEventListener('click', () => {
        enterFullscreen().then(() => { if (isFullscreen()) unlock(); });
    });
    // Gate, fullscreen and busy dialogs cannot be dismissed with Esc; if the browser closes one anyway, it comes back
    [el.gate, el.fullscreen, el.busy].forEach((d) => d.addEventListener('cancel', (e) => e.preventDefault()));
    el.gate.addEventListener('close', () => { if (state.phase === 'gate') el.gate.showModal(); });
    el.fullscreen.addEventListener('close', () => { if (state.phase === 'locked') el.fullscreen.showModal(); });
    el.busy.addEventListener('close', () => { if (state.phase === 'submitting' || state.phase === 'left') el.busy.showModal(); });

    document.addEventListener('fullscreenchange', onFullscreenChange);
    document.addEventListener('webkitfullscreenchange', onFullscreenChange);

    document.addEventListener('visibilitychange', () => {
        if (document.hidden) {
            if (state.phase === 'active' || state.phase === 'locked') {
                state.awayViolation = report('tab-hidden', true);
                state.suppressUntil = Infinity;
            }
        } else {
            state.suppressUntil = Date.now() + 2000;
            if (state.awayViolation && state.phase === 'active') showViolation('tab-hidden');
            state.awayViolation = false;
            if (state.phase === 'active' || state.phase === 'locked') scheduleSave(200);
        }
    });

    // Focus moving to another window while the tab stays visible (Alt+Tab in fullscreen, a second screen).
    // A short blink is only logged; staying away counts like a tab switch.
    let blurTimer = null;
    window.addEventListener('blur', () => {
        if (state.phase !== 'active') return;
        report('window-blur', false);
        clearTimeout(blurTimer);
        blurTimer = setTimeout(() => {
            const active = document.activeElement;
            if (state.phase !== 'active' || document.hidden || document.hasFocus()) return;
            if (active && active.tagName === 'IFRAME') return; // playing an embedded question video
            state.awayViolation = report('tab-hidden', true) || state.awayViolation;
        }, 2500);
    });
    window.addEventListener('focus', () => {
        clearTimeout(blurTimer);
        if (state.awayViolation && state.phase === 'active' && !document.hidden) {
            state.awayViolation = false;
            showViolation('tab-hidden');
        }
    });
    // Reload or navigation: not a tab switch; the time away is checked when the page comes back
    window.addEventListener('pagehide', () => {
        if (state.phase !== 'active' && state.phase !== 'locked') return;
        recordTime();
        state.phase = 'left';
        state.leftAt = Date.now();
        persist();
    });
    window.addEventListener('pageshow', (e) => { if (e.persisted) window.location.reload(); });

    ['copy', 'cut', 'paste'].forEach((type) => {
        document.addEventListener(type, (e) => {
            if (state.phase !== 'active' && state.phase !== 'locked') return;
            e.preventDefault();
            if (report(type, true)) showViolation(type);
        });
    });
    document.addEventListener('contextmenu', (e) => {
        if (state.phase === 'gate' || state.phase === 'left') return;
        e.preventDefault();
        report('contextmenu', false);
        toast(strings.blocked, 'fa-ban');
    });
    document.addEventListener('dragstart', (e) => { if (state.phase === 'active') e.preventDefault(); });
    window.addEventListener('beforeprint', () => { if (report('print', true)) showViolation('print'); });
    window.addEventListener('beforeunload', onBeforeUnload);
    window.addEventListener('online', () => scheduleSave(200));
    window.addEventListener('offline', () => setSaveState('offline'));

    document.addEventListener('keydown', (e) => {
        if (state.phase !== 'active') return;
        const key = e.key;
        const mod = e.ctrlKey || e.metaKey;
        if (key === 'F12' || (mod && e.shiftKey && /^[ijc]$/i.test(key)) || (mod && /^u$/i.test(key))) {
            e.preventDefault();
            report('devtools-key', false);
            toast(strings.blocked, 'fa-ban');
            return;
        }
        if (mod && /^p$/i.test(key)) {
            e.preventDefault();
            if (report('print', true)) showViolation('print');
            return;
        }
        if (key === 'F5' || (mod && /^r$/i.test(key))) { e.preventDefault(); toast(strings.blocked, 'fa-ban'); return; }
        if (mod && /^[sa]$/i.test(key)) { e.preventDefault(); return; }
        if (mod || e.altKey) return;
        if (document.querySelector('dialog[open]')) return;

        const onRadio = e.target.matches && e.target.matches('input[type="radio"]');
        if ((key === 'ArrowRight' && !onRadio) || key === 'n' || key === 'N') { e.preventDefault(); show(state.index + 1, 1); return; }
        if ((key === 'ArrowLeft' && !onRadio) || key === 'p' || key === 'P') { e.preventDefault(); show(state.index - 1, -1); return; }
        if (key === 'f' || key === 'F') { e.preventDefault(); questions[state.index].querySelector('[data-flag]').click(); return; }

        let optionIndex = -1;
        if (/^[a-e]$/i.test(key)) optionIndex = key.toUpperCase().charCodeAt(0) - 65;
        else if (/^[1-6]$/.test(key)) optionIndex = parseInt(key, 10) - 1;
        if (optionIndex >= 0) {
            const options = questions[state.index].querySelectorAll('input[type="radio"]');
            if (options[optionIndex]) {
                e.preventDefault();
                options[optionIndex].checked = true;
                options[optionIndex].dispatchEvent(new Event('change', { bubbles: true }));
            }
        }
    });

    // ------------------------------------------------------------------
    // Go
    // ------------------------------------------------------------------
    restore();
    const startIndex = state.index;
    state.index = 0;
    show(startIndex, 1);
    questions[startIndex].classList.remove('is-enter-next');
    updateProgress();
    drawWatermark();
    if (!navigator.onLine) setSaveState('offline');
    if (!fullscreenSupported) {
        el.gate.querySelector('[data-no-fullscreen]').hidden = false;
        el.gate.querySelector('[data-enter]').lastChild.textContent = ' ' + (document.documentElement.lang === 'de' ? 'Starten' : 'Başla');
    }
    tick();
    setInterval(tick, 500);
    setInterval(() => { if (state.phase === 'active' || state.phase === 'locked') save(); }, 20000);
    if (state.phase !== 'submitting' && state.phase !== 'left') el.gate.showModal();
})();
