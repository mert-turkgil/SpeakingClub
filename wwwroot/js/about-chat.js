// ============================================================================
// about-chat.js — the FAQ concierge on the About page.
// Topic → question → answer, like a guided chat: the visitor's choice appears
// as their message, the answer arrives after a short "typing" pause, followed
// by action buttons (exam, WhatsApp, form…) and follow-up questions.
// Visitors can also type a question; it is matched against the questions and
// their keywords, and anything without an answer can go straight to WhatsApp
// with the question already written. Content: Models/AboutFaqContent.cs.
// ============================================================================
(function () {
    'use strict';

    const box = document.querySelector('[data-concierge]');
    if (!box) return;
    const data = JSON.parse(document.getElementById('conciergeData').textContent || '{}');
    const S = data.strings || {};
    const log = box.querySelector('[data-log]');
    const form = box.querySelector('[data-ask]');
    const input = form.querySelector('input');
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const items = (data.topics || []).flatMap((topic) => topic.items.map((item) => Object.assign({ topic: topic }, item)));
    const reach = items.find((item) => item.id === 'reach');
    let busy = false;
    let started = false;

    // ------------------------------------------------------------------
    // Rendering
    // ------------------------------------------------------------------
    function el(tag, className, text) {
        const node = document.createElement(tag);
        if (className) node.className = className;
        if (text != null) node.textContent = text;
        return node;
    }

    function scrollDown() {
        requestAnimationFrame(() => { log.scrollTop = log.scrollHeight; });
    }

    function say(text, who) {
        const message = el('div', 'cmsg' + (who === 'user' ? ' cmsg--user' : ''));
        const bubble = el('div', 'cmsg__bubble', text);
        if (who === 'user') bubble.setAttribute('aria-label', (S.you || '') + ': ' + text);
        message.appendChild(bubble);
        log.appendChild(message);
        scrollDown();
    }

    // The bot "types" for a moment (longer answers take a little longer), then speaks
    function botSay(text, then) {
        busy = true;
        const typing = el('div', 'cmsg cmsg--typing');
        typing.innerHTML = '<div class="cmsg__bubble"><span></span><span></span><span></span></div>';
        typing.firstChild.setAttribute('aria-label', S.typing || '…');
        log.appendChild(typing);
        scrollDown();
        setTimeout(() => {
            typing.remove();
            say(text, 'bot');
            busy = false;
            if (then) then();
        }, reduceMotion ? 0 : Math.min(1300, 420 + text.length * 5));
    }

    function clearOptions() {
        log.querySelectorAll('[data-options]').forEach((node) => node.remove());
    }

    function options(list) {
        clearOptions();
        const wrap = el('div', 'copts');
        wrap.setAttribute('data-options', '');
        list.forEach((option, i) => {
            const button = el('button', 'copt' + (option.ghost ? ' copt--ghost' : ''));
            button.type = 'button';
            button.style.setProperty('--i', i);
            button.innerHTML = '<i class="' + option.icon + '" aria-hidden="true"></i>';
            button.appendChild(el('span', null, option.label));
            if (!option.ghost) button.insertAdjacentHTML('beforeend', '<i class="fa-solid fa-chevron-right copt__chev" aria-hidden="true"></i>');
            button.addEventListener('click', () => { if (!busy) option.run(); });
            wrap.appendChild(button);
        });
        log.appendChild(wrap);
        scrollDown();
    }

    function actions(list) {
        if (!list || !list.length) return;
        const wrap = el('div', 'cacts');
        list.forEach((action, i) => {
            const link = el('a', 'cact cact--' + (action.kind || 'link'));
            link.href = action.href;
            link.style.setProperty('--i', i);
            if (/^https?:/i.test(action.href)) { link.target = '_blank'; link.rel = 'noopener'; }
            link.innerHTML = '<i class="' + action.icon + '" aria-hidden="true"></i>';
            link.appendChild(document.createTextNode(action.label));
            if (action.href.charAt(0) === '#') {
                link.addEventListener('click', (e) => {
                    const target = document.querySelector(action.href);
                    if (!target) return;
                    e.preventDefault();
                    target.scrollIntoView({ behavior: reduceMotion ? 'auto' : 'smooth' });
                });
            }
            wrap.appendChild(link);
        });
        log.appendChild(wrap);
        scrollDown();
    }

    const setStep = (step) => { box.dataset.step = String(step); };

    // ------------------------------------------------------------------
    // Conversation
    // ------------------------------------------------------------------
    const humanOption = { label: S.human, icon: 'fa-brands fa-whatsapp', ghost: true, run: () => human() };
    const topicsOption = { label: S.otherTopic, icon: 'fa-solid fa-table-cells-large', ghost: true, run: () => backToTopics() };

    function showTopics(text) {
        setStep(0);
        botSay(text, () => options(data.topics.map((topic) => ({ label: topic.title, icon: topic.icon, run: () => pickTopic(topic) })).concat([humanOption])));
    }

    function pickTopic(topic) {
        clearOptions();
        say(topic.title, 'user');
        setStep(1);
        botSay(topic.intro, () => options(topic.items.map((item) => ({ label: item.question, icon: 'fa-regular fa-circle-question', run: () => pickQuestion(item) })).concat([topicsOption])));
    }

    function pickQuestion(item) {
        clearOptions();
        say(item.question, 'user');
        answer(item);
    }

    function answer(item) {
        const full = items.find((i) => i.id === item.id) || item;
        setStep(2);
        botSay(full.answer, () => {
            setStep(3);
            actions(full.actions);
            const others = full.topic.items.filter((i) => i.id !== full.id).slice(0, 3);
            setTimeout(() => options(others.map((i) => ({ label: i.question, icon: 'fa-regular fa-circle-question', run: () => pickQuestion(i) })).concat([topicsOption, humanOption])), reduceMotion ? 0 : 220);
        });
    }

    function backToTopics() {
        clearOptions();
        say(S.otherTopic, 'user');
        showTopics(S.more);
    }

    function human() {
        clearOptions();
        say(S.human, 'user');
        setStep(3);
        botSay(S.humanAnswer, () => {
            actions(reach ? reach.actions : [{ label: 'WhatsApp', href: data.whatsapp, icon: 'fa-brands fa-whatsapp', kind: 'whatsapp' }]);
            options([topicsOption]);
        });
    }

    // ------------------------------------------------------------------
    // Typed questions: match against the questions and their keywords
    // ------------------------------------------------------------------
    const normalize = (s) => String(s || '').toLocaleLowerCase('tr').replace(/ı/g, 'i')
        .normalize('NFD').replace(/[̀-ͯ]/g, '').replace(/ß/g, 'ss').replace(/[^a-z0-9\s]/g, ' ');
    items.forEach((item) => {
        item._text = normalize(item.question);
        item._keys = item.keywords.map(normalize);
    });

    function rank(text) {
        const words = normalize(text).split(/\s+/).filter((w) => w.length >= 3);
        return items.map((item) => {
            let score = 0;
            words.forEach((word) => {
                if (item._keys.some((key) => key === word || (word.length >= 4 && (key.indexOf(word) === 0 || word.indexOf(key) === 0)))) score += 3;
                else if (item._text.indexOf(word) >= 0) score += 1;
            });
            return { item: item, score: score };
        }).filter((r) => r.score > 0).sort((a, b) => b.score - a.score);
    }

    form.addEventListener('submit', (e) => {
        e.preventDefault();
        const text = input.value.trim();
        if (!text || busy) return;
        input.value = '';
        clearOptions();
        say(text, 'user');
        const results = rank(text);
        if (results.length && results[0].score >= 3 && (results.length === 1 || results[0].score > results[1].score)) {
            answer(results[0].item);
        } else if (results.length) {
            setStep(1);
            botSay(S.maybe, () => options(results.slice(0, 3).map((r) => ({ label: r.item.question, icon: 'fa-regular fa-circle-question', run: () => pickQuestion(r.item) })).concat([topicsOption])));
        } else {
            setStep(3);
            botSay(S.notFound, () => {
                actions([{ label: S.askOnWhatsApp, href: data.whatsappBase + encodeURIComponent(text), icon: 'fa-brands fa-whatsapp', kind: 'whatsapp' }]);
                options([topicsOption]);
            });
        }
    });

    box.querySelector('[data-restart]').addEventListener('click', () => {
        if (busy) return;
        log.querySelectorAll('.cmsg, .copts, .cacts').forEach((node) => node.remove());
        showTopics(S.greeting);
    });

    // ------------------------------------------------------------------
    // Start when the widget scrolls into view, so the greeting is seen being typed
    // ------------------------------------------------------------------
    box.classList.add('is-ready');
    setStep(0);
    const start = () => {
        if (started) return;
        started = true;
        showTopics(S.greeting);
    };
    if ('IntersectionObserver' in window && !reduceMotion) {
        const observer = new IntersectionObserver((entries) => {
            if (!entries.some((entry) => entry.isIntersecting)) return;
            observer.disconnect();
            start();
        }, { threshold: 0.35 });
        observer.observe(box);
    } else {
        start();
    }
})();
