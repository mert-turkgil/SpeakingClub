// ============================================================================
// admin-reports.js — the admin reports: Sınav sonuçları (Views/QuizMonitor/Index.cshtml),
// Kullanıcılar (Views/Admin/Members.cshtml) and one member (Views/Admin/Member.cshtml).
// The pages are rendered on the server; this file adds:
//   • filters that apply as soon as a choice changes (the address keeps every filter)
//   • the chart tabs: their figures are fetched as JSON the first time a tab opens
//     (QuizMonitorController.Stats, AdminController.UserStats) and drawn with Apache ECharts
//     (wwwroot/lib/echarts); charts whose figures come with the page use data-report-inline
//   • a "show as table" switch under every chart, for screen readers and printing
//   • the attempt side panel, and a "new results" notice fed by SignalR
// Plain JavaScript, no jQuery. Text comes from <script id="report-strings"> (TR/DE).
// ============================================================================
(function () {
    'use strict';

    const stringsEl = document.getElementById('report-strings');
    if (!stringsEl) return;
    const S = JSON.parse(stringsEl.textContent);
    const root = document.querySelector('[data-report-filter]');
    const lang = document.documentElement.lang || 'tr';
    const nf = (digits) => new Intl.NumberFormat(lang, { maximumFractionDigits: digits, minimumFractionDigits: digits });
    const n0 = nf(0), n1 = nf(1), n2 = nf(2);
    const pct = (v) => v == null ? '–' : n0.format(v * 100) + '%';
    const clock = (s) => s == null ? '–' : Math.floor(s / 60) + ':' + String(Math.round(s % 60)).padStart(2, '0');
    const esc = (s) => String(s == null ? '' : s).replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    const fmt = (s, ...args) => s.replace(/\{(\d)\}/g, (m, i) => args[i]);
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    // ------------------------------------------------------------------
    // Filters
    // ------------------------------------------------------------------
    if (root) {
        const dates = root.querySelector('[data-report-dates]');
        const defaultPeriod = root.dataset.defaultPeriod || '30';
        const submit = () => { if (root.requestSubmit) root.requestSubmit(); else root.submit(); };
        root.addEventListener('change', (e) => {
            const t = e.target;
            if (t.matches('[data-report-period]')) {
                dates.hidden = t.value !== 'custom';
                if (t.value !== 'custom') submit();
                else dates.querySelector('input')?.focus();
            } else if (t.matches('[data-report-autosubmit]')) {
                submit();
            }
        });
        // Empty fields stay out of the address, so links stay short and readable
        root.addEventListener('submit', () => {
            root.querySelectorAll('input, select').forEach((el) => {
                if ((el.type === 'radio' || el.type === 'checkbox') ? false : !el.value) el.disabled = true;
                if (el.type === 'radio' && el.checked && !el.value) el.disabled = true;
                if (el.name === 'period' && el.checked && el.value === defaultPeriod) el.disabled = true;
            });
        });
        // Back button: the browser may restore this page from its cache with those fields still disabled
        window.addEventListener('pageshow', () => root.querySelectorAll(':disabled').forEach((el) => { el.disabled = false; }));
    }
    document.querySelector('[data-report-size]')?.addEventListener('change', (e) => { location.href = e.target.value; });
    document.querySelector('[data-report-print]')?.addEventListener('click', () => window.print());

    // ------------------------------------------------------------------
    // Charts: colours from the admin design tokens, redrawn when the theme changes
    // ------------------------------------------------------------------
    const charts = [];
    function tokens() {
        const css = getComputedStyle(document.querySelector('.adm-body') || document.body);
        const v = (name) => css.getPropertyValue(name).trim();
        return {
            ink: v('--a-ink'), body: v('--a-body'), muted: v('--a-muted'), line: v('--a-hairline'), surface: v('--a-surface'),
            coral: v('--a-coral'), coralDeep: v('--a-coral-deep'), green: v('--a-green'), amber: v('--a-amber'), red: v('--a-red'),
            blue: v('--a-blue'), coralSoft: v('--a-coral-soft'), font: v('--a-sans') || 'Poppins, sans-serif'
        };
    }
    function base(c) {
        return {
            animation: !reduceMotion,
            textStyle: { fontFamily: c.font, color: c.body },
            grid: { left: 48, right: 18, top: 28, bottom: 42, containLabel: true },
            tooltip: { trigger: 'axis', backgroundColor: c.surface, borderColor: c.line, textStyle: { color: c.ink } },
            xAxis: { axisLine: { lineStyle: { color: c.line } }, axisLabel: { color: c.muted }, splitLine: { show: false } },
            yAxis: { axisLine: { show: false }, axisLabel: { color: c.muted }, splitLine: { lineStyle: { color: c.line } } }
        };
    }
    function merge(a, b) {
        const out = Object.assign({}, a);
        Object.keys(b).forEach((k) => {
            out[k] = (a[k] && typeof a[k] === 'object' && !Array.isArray(a[k]) && typeof b[k] === 'object' && !Array.isArray(b[k])) ? merge(a[k], b[k]) : b[k];
        });
        return out;
    }

    /**
     * Draws a chart in el. build(c) returns the ECharts option; table() returns {head, rows} for the
     * "show as table" view; summary is read out by screen readers.
     */
    function chart(el, build, table, summary) {
        if (!el) return;
        el.setAttribute('role', 'img');
        if (summary) el.setAttribute('aria-label', summary);
        const draw = () => {
            const existing = window.echarts.getInstanceByDom(el);
            if (existing) existing.dispose();
            const instance = window.echarts.init(el, null, { renderer: 'svg' });
            const c = tokens();
            const option = merge(base(c), build(c));
            // ECharts sizes the grid with its default font unless the axis labels name one, and long labels get clipped
            ['xAxis', 'yAxis'].forEach((k) => [].concat(option[k] || []).forEach((axis) => { axis.axisLabel = Object.assign({ fontFamily: c.font }, axis.axisLabel); }));
            instance.setOption(option);
            return instance;
        };
        charts.push({ el, draw, instance: draw() });
        if (table) addTableSwitch(el, table());
    }
    function addTableSwitch(el, data) {
        const wrap = document.createElement('div');
        wrap.className = 'rp-chart-table';
        wrap.hidden = true;
        wrap.innerHTML = tableHtml(data.head, data.rows);
        const button = document.createElement('button');
        button.type = 'button';
        button.className = 'rp-switch';
        button.textContent = S.showTable;
        button.addEventListener('click', () => {
            wrap.hidden = !wrap.hidden;
            el.hidden = !wrap.hidden;
            button.textContent = wrap.hidden ? S.showTable : S.hideTable;
            if (wrap.hidden) resizeAll();
        });
        el.after(button, wrap);
    }
    function tableHtml(head, rows, cls) {
        return '<div class="adm-table-wrap"><table class="adm-table ' + (cls || '') + '"><thead><tr>' +
            head.map((h) => '<th scope="col">' + esc(h) + '</th>').join('') + '</tr></thead><tbody>' +
            rows.map((r) => '<tr>' + r.map((cell, i) => '<td data-label="' + esc(head[i]) + '">' + (cell && cell.html != null ? cell.html : esc(cell)) + '</td>').join('') + '</tr>').join('') +
            '</tbody></table></div>';
    }
    // Charts in a hidden tab have no size; they are resized when their tab is shown (adm:tab-shown)
    function resizeAll() { charts.forEach((ch) => { if (ch.el.clientWidth > 0) ch.instance.resize(); }); }
    let resizeTimer;
    window.addEventListener('resize', () => { clearTimeout(resizeTimer); resizeTimer = setTimeout(resizeAll, 150); }, { passive: true });
    new MutationObserver(() => charts.forEach((ch) => { ch.instance = ch.draw(); }))
        .observe(document.documentElement, { attributes: true, attributeFilter: ['data-bs-theme'] });

    // ------------------------------------------------------------------
    // Tabs: load each one the first time it is shown
    // ------------------------------------------------------------------
    const renderers = {
        overview: renderOverview, items: renderItems, matrix: renderMatrix, progress: renderProgress, activity: renderActivity, integrity: renderIntegrity,
        growth: renderGrowth, cohort: renderCohort, memberTrend: renderMemberTrend
    };
    const loaded = new Set();
    document.addEventListener('adm:tab-shown', (e) => {
        const panel = e.target.closest('[data-report-tab]');
        if (panel) load(panel);
        resizeAll();
    });
    function optionsQuery(panel) {
        const extra = new URLSearchParams();
        panel.querySelectorAll('[data-report-option]').forEach((input) => {
            if ((input.type === 'radio' || input.type === 'checkbox') && !input.checked) return;
            extra.set(input.dataset.reportOption, input.value);
        });
        return extra.toString();
    }
    async function load(panel, force) {
        const tab = panel.dataset.reportTab;
        if (!window.echarts || (loaded.has(tab) && !force)) return;
        if (!panel.querySelector('[data-chart], [data-items], [data-matrix], [data-table]')) return; // "pick a quiz" card
        loaded.add(tab);
        panel.classList.add('is-loading');
        panel.setAttribute('aria-busy', 'true');
        try {
            const extra = optionsQuery(panel);
            const url = panel.dataset.url + (extra ? (panel.dataset.url.includes('?') ? '&' : '?') + extra : '');
            const response = await fetch(url, { headers: { Accept: 'application/json' }, credentials: 'same-origin' });
            if (!response.ok) throw new Error('HTTP ' + response.status);
            const data = await response.json();
            // Axis labels are measured when drawn: with the fallback font they would be clipped once Poppins arrives
            if (document.fonts) await document.fonts.ready;
            panel.querySelectorAll('.rp-switch, .rp-chart-table').forEach((n) => n.remove());
            charts.splice(0, charts.length, ...charts.filter((ch) => !panel.contains(ch.el)));
            renderers[tab](panel, data);
        } catch (err) {
            loaded.delete(tab);
            panel.insertAdjacentHTML('afterbegin', '<p class="adm-alert adm-alert--error rp-error">' + esc(S.failed) + '</p>');
        } finally {
            panel.classList.remove('is-loading');
            panel.removeAttribute('aria-busy');
        }
    }
    // Option chips inside a tab (question analysis: which attempts) reload that tab
    document.addEventListener('change', (e) => {
        if (!e.target.matches('[data-report-option]')) return;
        const panel = e.target.closest('[data-report-tab]');
        if (panel) load(panel, true);
    });
    // Opening a tab from the address (#items, #matrix…) — the "pick a quiz" links use it
    function openFromHash() {
        const tab = document.getElementById('tab-' + location.hash.slice(1));
        if (tab && tab.getAttribute('role') === 'tab') tab.click();
    }
    // ECharts is loaded with defer, like this file: both are ready when this runs
    openFromHash();
    // Charts whose figures come with the page (<div data-report-inline="name"> with a JSON <script> inside)
    document.querySelectorAll('[data-report-inline]').forEach(async (box) => {
        if (!window.echarts) return;
        if (document.fonts) await document.fonts.ready;
        renderers[box.dataset.reportInline](box, JSON.parse(box.querySelector('script[type="application/json"]').textContent));
    });

    function emptyIf(panel, n) {
        if (n > 0) return false;
        panel.querySelectorAll('[data-chart], [data-table]').forEach((el) => { el.innerHTML = '<p class="rp-empty">' + esc(S.empty) + '</p>'; el.removeAttribute('role'); });
        return true;
    }

    // ------------------------------------------------------------------
    // Overview
    // ------------------------------------------------------------------
    function renderOverview(panel, d) {
        if (emptyIf(panel, d.n)) return;
        const bins = d.histogram.map((_, i) => i === 9 ? '90–100' : (i * 10) + '–' + (i * 10 + 9));
        chart(panel.querySelector('[data-chart="histogram"]'), (c) => ({
            tooltip: { trigger: 'axis' },
            xAxis: { type: 'category', data: bins, name: S.score, nameLocation: 'middle', nameGap: 30 },
            yAxis: { type: 'value', minInterval: 1 },
            series: [{
                type: 'bar', data: d.histogram.map((v, i) => ({ value: v, itemStyle: { color: i * 10 >= d.pass ? c.green : c.coral, borderRadius: [6, 6, 0, 0] } })),
                markLine: { silent: true, symbol: 'none', lineStyle: { color: c.ink, type: 'dashed' }, label: { formatter: S.passLine, color: c.muted },
                    data: [{ xAxis: Math.floor(d.pass / 10) - 0.5 }] }
            }]
        }), () => ({ head: [S.score, S.count], rows: d.histogram.map((v, i) => [bins[i], n0.format(v)]) }),
        bins.map((b, i) => b + ': ' + d.histogram[i]).join(', '));

        const pts = d.trend.points;
        const labels = pts.map((p) => new Date(p.date + 'T00:00:00').toLocaleDateString(lang, d.trend.unit === 'week' ? { day: 'numeric', month: 'short' } : { day: 'numeric', month: 'short' }));
        chart(panel.querySelector('[data-chart="trend"]'), (c) => ({
            legend: { top: 0, data: [S.count, S.mean], textStyle: { color: c.muted } },
            xAxis: { type: 'category', data: labels, boundaryGap: true },
            yAxis: [{ type: 'value', min: 0, max: 100, name: S.score }, { type: 'value', minInterval: 1, splitLine: { show: false }, name: S.count }],
            series: [
                { name: S.count, type: 'bar', yAxisIndex: 1, data: pts.map((p) => p.n), itemStyle: { color: c.line }, barMaxWidth: 18 },
                { name: 'Q1', type: 'line', data: pts.map((p) => p.q1), lineStyle: { opacity: 0 }, stack: 'band', symbol: 'none', tooltip: { show: false } },
                { name: 'IQR', type: 'line', data: pts.map((p) => p.q3 - p.q1), lineStyle: { opacity: 0 }, stack: 'band', symbol: 'none', areaStyle: { color: c.coralSoft }, tooltip: { show: false } },
                { name: S.mean, type: 'line', data: pts.map((p) => +p.mean.toFixed(1)), smooth: true, symbolSize: 6, lineStyle: { color: c.coralDeep, width: 3 }, itemStyle: { color: c.coralDeep } }
            ]
        }), () => ({ head: ['', S.count, S.mean, S.median], rows: pts.map((p, i) => [labels[i], n0.format(p.n), n1.format(p.mean), n1.format(p.median)]) }));

        const quizzes = d.quizzes.slice(0, 12);
        panel.querySelector('[data-chart="boxes"]').style.height = Math.max(220, quizzes.length * 42 + 60) + 'px';
        chart(panel.querySelector('[data-chart="boxes"]'), (c) => ({
            tooltip: { trigger: 'item', formatter: (p) => {
                const q = quizzes[p.dataIndex];
                return esc(q.title) + '<br>n = ' + q.n + '<br>' + S.median + ': ' + n0.format(q.median) + '<br>' + S.mean + ': ' + n1.format(q.mean);
            } },
            grid: { left: 10, right: 20, top: 10, bottom: 30, containLabel: true },
            yAxis: { type: 'category', data: quizzes.map((q) => q.title.length > 32 ? q.title.slice(0, 31) + '…' : q.title), inverse: true, axisLabel: { color: c.body } },
            xAxis: { type: 'value', min: 0, max: 100, splitLine: { lineStyle: { color: c.line } } },
            series: [{
                type: 'boxplot', data: quizzes.map((q) => [q.min, q.q1, q.median, q.q3, q.max]),
                itemStyle: { color: c.coralSoft, borderColor: c.coralDeep, borderWidth: 1.5 }, boxWidth: [8, 26],
                markLine: { silent: true, symbol: 'none', lineStyle: { color: c.ink, type: 'dashed' }, data: [{ xAxis: d.pass }] }
            }]
        }));
        panel.querySelector('[data-table="quizzes"]').innerHTML = tableHtml(
            [lang.startsWith('de') ? 'Prüfung' : 'Sınav', 'n', lang.startsWith('de') ? 'Lernende' : 'Öğrenci', S.mean, S.median, 'SD', lang.startsWith('de') ? 'Bestanden' : 'Geçme', lang.startsWith('de') ? 'Typische Dauer' : 'Tipik süre'],
            d.quizzes.map((q) => [q.title + (q.level ? ' · ' + q.level : ''), n0.format(q.n), n0.format(q.students), n1.format(q.mean), n0.format(q.median), n1.format(q.sd), pct(q.passRate), clock(q.medianDuration)]));
    }

    // ------------------------------------------------------------------
    // Question analysis (one quiz)
    // ------------------------------------------------------------------
    const de = lang.startsWith('de');
    const L = (tr, deText) => de ? deText : tr;
    function light(value, good, ok) {
        if (value == null) return '<span class="rp-light is-none">–</span>';
        const cls = good ? 'is-good' : ok ? 'is-ok' : 'is-bad';
        return '<span class="rp-light ' + cls + '">' + value + '</span>';
    }
    function kr20Words(r) {
        if (r == null) return L('hesaplanamadı', 'nicht berechenbar');
        if (r >= 0.8) return L('iyi: sonuçlar tutarlı', 'gut: Ergebnisse stimmig');
        if (r >= 0.7) return L('kabul edilebilir', 'akzeptabel');
        if (r >= 0.5) return L('düşük: puanlar biraz şansa bağlı', 'niedrig: Punkte teils zufällig');
        return L('çok düşük: sınav seviyeyi ölçmüyor olabilir', 'sehr niedrig: misst das Niveau kaum');
    }
    function renderItems(panel, d) {
        const box = panel.querySelector('[data-items]');
        if (d.needsQuiz) return;
        if (!d.attempts) { box.innerHTML = '<p class="rp-empty">' + esc(S.empty) + '</p>'; return; }
        const few = d.attempts < d.minAttempts;
        let html = '';
        if (few) html += '<p class="adm-alert adm-alert--warn">' + esc(fmt(S.tooFew, d.minAttempts, d.attempts)) + '</p>';
        html += '<div class="adm-stats rp-kpis rp-kpis--small">' +
            stat(n0.format(d.attempts), L('incelenen deneme', 'ausgewertete Versuche') + (d.excluded ? ' · ' + d.excluded + L(' dışarıda (sorular sonradan değişti)', ' ausgelassen (Fragen geändert)') : '')) +
            stat(n1.format(d.meanPercent) + ' / ' + n0.format(d.medianPercent), S.mean + ' / ' + S.median) +
            stat(d.kr20 == null ? '–' : n2.format(d.kr20), L('Güvenilirlik (KR-20): ', 'Zuverlässigkeit (KR-20): ') + kr20Words(d.kr20)) +
            stat(d.semPercent == null ? '–' : '±' + n0.format(d.semPercent), L('Ölçme hatası (puan): bir öğrencinin puanı tekrarda bu kadar oynayabilir', 'Messfehler (Punkte): so viel kann eine Punktzahl schwanken')) +
            (d.lowEffortShare != null ? stat(pct(d.lowEffortShare), L('aceleyle çözülen deneme (cevapların %10’dan fazlası çok hızlı)', 'überflogene Versuche (über 10 % zu schnelle Antworten)')) : '') +
            '</div>';
        html += '<div class="adm-card rp-chart-card"><h3>' + L('Zorluk ve ayırt edicilik', 'Schwierigkeit und Trennschärfe') + '</h3>' +
            '<p class="rp-help">' + L('Her nokta bir soru. Yeşil alan ideal bölge: doğru yapma oranı %30–70 arasında ve iyi öğrenciyi zayıftan ayırıyor (0,25 üstü). Alt kısımdaki sorulara bakmak gerekir.', 'Jeder Punkt ist eine Frage. Die grüne Fläche ist ideal: 30–70 % richtig und trennt starke von schwächeren Lernenden (über 0,25). Fragen unten prüfen.') + '</p>' +
            '<div class="rp-chart" data-chart="scatter"></div></div>';
        html += '<div class="adm-card"><h3>' + L('Soru soru', 'Frage für Frage') + '</h3>' +
            '<details class="rp-glossary"><summary>' + L('Bu sütunlar ne demek?', 'Was bedeuten die Spalten?') + '</summary><dl>' +
            '<dt>' + L('Doğru', 'Richtig') + '</dt><dd>' + L('Soruyu doğru yapanların oranı (zorluk). %30–70 ideal.', 'Anteil richtiger Antworten (Schwierigkeit). 30–70 % ideal.') + '</dd>' +
            '<dt>' + L('Ayırt edicilik', 'Trennschärfe') + '</dt><dd>' + L('Sınavı iyi geçenler bu soruyu daha sık mı doğru yapıyor? 0,25 ve üstü iyi; 0 ve altı: soruyu ya da cevap anahtarını kontrol edin.', 'Lösen Starke diese Frage öfter richtig? Ab 0,25 gut; 0 oder weniger: Frage oder Lösung prüfen.') + '</dd>' +
            '<dt>' + L('Üst–alt farkı', 'Oben–unten') + '</dt><dd>' + L('En iyi %27 ile en zayıf %27 arasındaki doğru yapma farkı.', 'Unterschied zwischen den besten und schwächsten 27 %.') + '</dd>' +
            '<dt>' + L('Boş', 'Leer') + '</dt><dd>' + L('Soruyu boş bırakanlar.', 'Nicht beantwortet.') + '</dd>' +
            '<dt>' + L('Hızlı', 'Schnell') + '</dt><dd>' + L('Okumaya yetmeyecek kadar hızlı verilen cevaplar (en az 3 sn ya da ortalama sürenin %10’u).', 'Zu schnelle Antworten (unter 3 s bzw. 10 % der Durchschnittszeit).') + '</dd>' +
            '</dl></details>' +
            tableHtml([L('Soru', 'Frage'), L('Doğru', 'Richtig'), L('Ayırt edicilik', 'Trennschärfe'), L('Üst–alt', 'Oben–unten'), L('Boş', 'Leer'), L('Süre', 'Zeit'), L('Hızlı', 'Schnell'), L('Dikkat', 'Hinweis')],
                d.items.map((it) => [
                    { html: '<button type="button" class="rp-q" data-q="' + it.questionId + '" aria-expanded="false"><span class="rp-muted">' + it.position + '.</span> ' + esc(it.text) + '</button>' },
                    { html: light(pct(it.p), it.p >= 0.3 && it.p <= 0.7, it.p > 0.7 && it.p <= 0.9) },
                    { html: light(it.pointBiserial == null ? null : n2.format(it.pointBiserial), it.pointBiserial >= 0.25, it.pointBiserial > 0) },
                    { html: it.discrimination == null ? '–' : n2.format(it.discrimination) },
                    pct(it.omitRate), clock(it.medianSeconds), pct(it.rapidRate),
                    { html: '<span class="rp-badges">' + (it.flags.map((fl) => '<span class="adm-badge ' + (fl === 'miskey' || fl === 'negative' ? 'adm-badge--red' : fl === 'few' ? 'adm-badge--plain' : 'adm-badge--amber') + '" title="' + esc(S.flagHelp[fl] || '') + '">' + esc(S.flags[fl] || fl) + '</span>').join(' ') || '<span class="adm-badge adm-badge--green">' + L('İyi', 'Gut') + '</span>') + '</span>' }
                ]), 'rp-items-table') +
            '</div>';
        box.innerHTML = html;

        // Each question opens its answer options: who chose what, overall and in the top/bottom groups
        box.querySelectorAll('.rp-q').forEach((button) => button.addEventListener('click', () => {
            const row = button.closest('tr');
            const next = row.nextElementSibling;
            if (next && next.classList.contains('rp-options-row')) { next.remove(); button.setAttribute('aria-expanded', 'false'); return; }
            const it = d.items.find((x) => String(x.questionId) === button.dataset.q);
            const tr = document.createElement('tr');
            tr.className = 'rp-options-row';
            tr.innerHTML = '<td colspan="8">' + optionsHtml(it) + '</td>';
            row.after(tr);
            button.setAttribute('aria-expanded', 'true');
        }));

        chart(box.querySelector('[data-chart="scatter"]'), (c) => ({
            tooltip: { trigger: 'item', formatter: (p) => {
                const it = d.items[p.dataIndex];
                return esc(it.position + '. ' + it.text.slice(0, 80)) + '<br>' + L('Doğru: ', 'Richtig: ') + pct(it.p) + '<br>' + L('Ayırt edicilik: ', 'Trennschärfe: ') + (it.pointBiserial == null ? '–' : n2.format(it.pointBiserial));
            } },
            xAxis: { type: 'value', min: 0, max: 1, name: L('doğru yapma oranı', 'Anteil richtig'), nameLocation: 'middle', nameGap: 30, axisLabel: { formatter: (v) => n0.format(v * 100) + '%' } },
            yAxis: { type: 'value', min: -0.4, max: 1, name: L('ayırt edicilik', 'Trennschärfe') },
            series: [{
                type: 'scatter', symbolSize: 16,
                data: d.items.map((it) => ({ value: [it.p, it.pointBiserial == null ? 0 : it.pointBiserial],
                    itemStyle: { color: it.flags.includes('miskey') || it.flags.includes('negative') ? c.red : it.flags.length && !it.flags.includes('few') ? c.amber : c.green } })),
                label: { show: true, formatter: (p) => d.items[p.dataIndex].position, color: '#fff', fontSize: 10 },
                markArea: { silent: true, itemStyle: { color: c.green, opacity: 0.08 }, data: [[{ xAxis: 0.3, yAxis: 0.25 }, { xAxis: 0.7, yAxis: 1 }]] },
                markLine: { silent: true, symbol: 'none', lineStyle: { color: c.muted, type: 'dashed' }, label: { show: false }, data: [{ yAxis: 0 }, { yAxis: 0.25 }] }
            }]
        }), () => ({ head: [L('Soru', 'Frage'), L('Doğru', 'Richtig'), L('Ayırt edicilik', 'Trennschärfe')], rows: d.items.map((it) => [it.position + '. ' + it.text, pct(it.p), it.pointBiserial == null ? '–' : n2.format(it.pointBiserial)]) }));
    }
    function stat(num, label) {
        return '<div class="adm-stat"><span><span class="adm-stat__num">' + esc(num) + '</span><span class="adm-stat__label">' + esc(label) + '</span></span></div>';
    }
    function optionsHtml(it) {
        const bar = (v, cls) => '<span class="rp-bar ' + cls + '" style="--v:' + (v == null ? 0 : v * 100) + '"><span></span><b>' + pct(v) + '</b></span>';
        return '<div class="rp-options"><p class="rp-help">' + L('Doğru şık yeşil. İyi bir yanlış şık en çok zayıf öğrencileri çeker (alt grupta yüksek, üst grupta düşük).', 'Die richtige Antwort ist grün. Eine gute falsche Antwort lockt vor allem Schwächere (unten hoch, oben niedrig).') + '</p>' +
            tableHtml([L('Şık', 'Antwort'), L('Herkes', 'Alle'), L('En iyi %27', 'Beste 27 %'), L('En zayıf %27', 'Schwächste 27 %')],
                it.options.map((o) => [
                    { html: (o.isKey ? '<span class="adm-badge adm-badge--green">' + L('doğru', 'richtig') + '</span> ' : '') + esc(o.text) },
                    { html: bar(o.share, o.isKey ? 'is-key' : '') }, { html: bar(o.shareTop, o.isKey ? 'is-key' : '') }, { html: bar(o.shareBottom, o.isKey ? 'is-key' : '') }
                ])) + '</div>';
    }

    // ------------------------------------------------------------------
    // Student × question (same data as the question analysis)
    // ------------------------------------------------------------------
    function renderMatrix(panel, d) {
        const box = panel.querySelector('[data-matrix]');
        if (d.needsQuiz) return;
        if (!d.attempts) { box.innerHTML = '<p class="rp-empty">' + esc(S.empty) + '</p>'; return; }
        const order = d.items.map((it, i) => i).sort((a, b) => d.items[b].p - d.items[a].p);
        const head = '<tr><th scope="col">' + L('Öğrenci', 'Lernende') + '</th><th scope="col" class="is-num">' + S.score + '</th><th scope="col" class="is-num" title="' + esc(L('Kolay soruyu yanlış, zor soruyu doğru yapma sıklığı (0 = tutarlı)', 'Wie oft eine leichte Frage falsch, aber eine schwere richtig ist (0 = stimmig)')) + '">' + L('Tutarsızlık', 'Inkonsistenz') + '</th>' +
            order.map((i) => '<th scope="col" class="rp-heat__q" title="' + esc(d.items[i].text) + '">' + d.items[i].position + '<small>' + pct(d.items[i].p) + '</small></th>').join('') + '</tr>';
        const body = d.rows.map((r) => '<tr><th scope="row">' + esc(r.name) + '</th><td class="is-num">' + r.total + '/' + d.questions + '</td><td class="is-num">' +
            (r.guttmanErrors == null ? '–' : '<span class="rp-light ' + (r.guttmanErrors > 0.3 ? 'is-bad' : r.guttmanErrors > 0.15 ? 'is-ok' : 'is-good') + '">' + n2.format(r.guttmanErrors) + '</span>') + '</td>' +
            order.map((i) => { const v = r.cells[i]; return v === 1 ? '<td class="rp-heat__c is-ok" title="✓">✓</td>' : v === 0 ? '<td class="rp-heat__c is-bad" title="✗">✗</td>' : '<td class="rp-heat__c is-skip" title="–">–</td>'; }).join('') + '</tr>').join('');
        box.innerHTML = '<div class="rp-heat-wrap"><table class="rp-heat"><caption class="adm-sr">' + esc(L('Öğrenci × soru', 'Lernende × Frage')) + '</caption><thead>' + head + '</thead><tbody>' + body + '</tbody></table></div>' +
            (d.rows.length >= 300 ? '<p class="rp-help">' + L('En yüksek puanlı 300 deneme gösteriliyor.', 'Die 300 Versuche mit den meisten Punkten werden gezeigt.') + '</p>' : '');
    }

    // ------------------------------------------------------------------
    // Progress
    // ------------------------------------------------------------------
    function renderProgress(panel, d) {
        if (emptyIf(panel, d.students)) return;
        chart(panel.querySelector('[data-chart="byAttempt"]'), (c) => ({
            xAxis: { type: 'category', data: d.byAttempt.map((a) => a.attempt === 5 ? '5+' : String(a.attempt)), name: L('deneme', 'Versuch'), nameLocation: 'middle', nameGap: 30 },
            yAxis: { type: 'value', min: 0, max: 100 },
            series: [{ type: 'bar', data: d.byAttempt.map((a) => +a.mean.toFixed(1)), itemStyle: { color: c.coral, borderRadius: [6, 6, 0, 0] },
                label: { show: true, position: 'top', color: c.muted, formatter: (p) => 'n=' + d.byAttempt[p.dataIndex].n } }]
        }), () => ({ head: [L('Deneme', 'Versuch'), 'n', S.mean], rows: d.byAttempt.map((a) => [a.attempt === 5 ? '5+' : a.attempt, a.n, n1.format(a.mean)]) }));

        chart(panel.querySelector('[data-chart="toPass"]'), (c) => ({
            xAxis: { type: 'category', data: d.toPass.map((a) => a.attempts === 5 ? '5+' : String(a.attempts)), name: L('kaçıncı denemede geçti', 'bestanden im Versuch'), nameLocation: 'middle', nameGap: 30 },
            yAxis: { type: 'value', minInterval: 1 },
            series: [{ type: 'bar', data: d.toPass.map((a) => a.n), itemStyle: { color: c.green, borderRadius: [6, 6, 0, 0] } }]
        }), () => ({ head: [L('Deneme', 'Versuch'), L('Öğrenci', 'Lernende')], rows: d.toPass.map((a) => [a.attempts === 5 ? '5+' : a.attempts, a.n]) }));
        panel.querySelector('[data-progress-summary]').textContent =
            L(`Öğrencilerin ${pct(d.retakeShare)}'i bir sınavı tekrar çözdü; tekrar edenler ilk denemelerine göre ortalama ${d.meanGain == null ? '–' : n1.format(d.meanGain)} puan daha yüksek aldı. ${d.neverPassed} öğrenci-sınav çifti henüz geçmedi.`,
              `${pct(d.retakeShare)} der Lernenden haben eine Prüfung wiederholt; im Schnitt ${d.meanGain == null ? '–' : n1.format(d.meanGain)} Punkte mehr als beim ersten Versuch. ${d.neverPassed} Paare Lernende–Prüfung noch nicht bestanden.`);

        const top = d.top.slice().reverse();
        const el = panel.querySelector('[data-chart="dumbbell"]');
        if (!top.length) { el.innerHTML = '<p class="rp-empty">' + esc(L('Henüz bir sınavı tekrar çözen yok.', 'Noch hat niemand eine Prüfung wiederholt.')) + '</p>'; return; }
        el.style.height = Math.max(240, top.length * 30 + 60) + 'px';
        // The quiz name is left out when it is the same on every row, and on phones (it is in the tooltip)
        const cut = (s, n) => s.length > n ? s.slice(0, n - 1) + '…' : s;
        const narrow = el.clientWidth < 520;
        const oneQuiz = new Set(top.map((t) => t.quiz)).size === 1;
        const label = (t) => narrow || oneQuiz ? cut(t.name, narrow ? 16 : 30) : t.name + ' · ' + cut(t.quiz, 22);
        chart(el, (c) => ({
            tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' }, formatter: (ps) => {
                const t = top[ps[0].dataIndex];
                return esc(t.name) + ' · ' + esc(t.quiz) + '<br>' + L('İlk', 'Erster') + ': ' + t.first + ' → ' + L('En iyi', 'Bester') + ': ' + t.best + ' (' + t.attempts + ' ' + L('deneme', 'Versuche') + ')';
            } },
            legend: { top: 0, data: [L('İlk deneme', 'Erster Versuch'), L('En iyi deneme', 'Bester Versuch')], textStyle: { color: c.muted } },
            grid: { left: 10, right: 40, top: 34, bottom: 30, containLabel: true },
            yAxis: { type: 'category', data: top.map(label), axisLabel: { color: c.body } },
            xAxis: { type: 'value', min: 0, max: 100 },
            // Dumbbell: a thin bar from the first to the best score, a dot at each end
            series: [
                { type: 'bar', stack: 'g', data: top.map((t) => t.first), itemStyle: { color: 'transparent' }, barWidth: 4, tooltip: { show: false } },
                { type: 'bar', stack: 'g', data: top.map((t) => Math.max(0, t.best - t.first)), itemStyle: { color: c.line }, barWidth: 4,
                  label: { show: true, position: 'right', distance: 12, color: c.muted, formatter: (p) => '+' + (top[p.dataIndex].best - top[p.dataIndex].first) } },
                { name: L('İlk deneme', 'Erster Versuch'), type: 'line', data: top.map((t) => t.first), lineStyle: { width: 0 }, symbol: 'circle', symbolSize: 11, itemStyle: { color: c.coral }, z: 3 },
                { name: L('En iyi deneme', 'Bester Versuch'), type: 'line', data: top.map((t) => t.best), lineStyle: { width: 0 }, symbol: 'circle', symbolSize: 11, itemStyle: { color: c.green }, z: 4 }
            ]
        }), () => ({ head: [L('Öğrenci', 'Lernende'), L('Sınav', 'Prüfung'), L('İlk', 'Erster'), L('En iyi', 'Bester'), L('Son', 'Letzter'), L('Deneme', 'Versuche')],
            rows: d.top.map((t) => [t.name, t.quiz, t.first, t.best, t.last, t.attempts]) }));
    }

    // ------------------------------------------------------------------
    // When (weekday × hour)
    // ------------------------------------------------------------------
    function renderActivity(panel, d) {
        if (emptyIf(panel, d.n)) return;
        const hours = Array.from({ length: 24 }, (_, h) => String(h).padStart(2, '0'));
        const el = panel.querySelector('[data-chart="activity"]');
        // Phones: hours down the side and weekdays across (24 columns would be a few pixels wide each)
        const tall = el.clientWidth < 520;
        el.style.height = tall ? (24 * 20 + 90) + 'px' : '';
        const cells = tall ? d.cells.map((x) => [x[1], x[0], x[2]]) : d.cells;   // d.cells: [hour, weekday, n]
        chart(el, (c) => ({
            tooltip: { trigger: 'item', formatter: (p) => {
                const [hour, day] = tall ? [p.value[1], p.value[0]] : p.value;
                return S.weekdays[day] + ' ' + hours[hour] + ':00 — ' + p.value[2] + ' ' + S.attempts;
            } },
            grid: { left: 10, right: 10, top: 10, bottom: 60, containLabel: true },
            xAxis: { type: 'category', data: tall ? S.weekdays : hours, splitArea: { show: false } },
            yAxis: { type: 'category', data: tall ? hours : S.weekdays, inverse: true, splitLine: { show: false } },
            visualMap: { min: 0, max: Math.max(1, d.max), calculable: false, orient: 'horizontal', left: 'center', bottom: 0, itemWidth: 12,
                inRange: { color: [c.surface, c.coralSoft, c.coral, c.coralDeep] }, textStyle: { color: c.muted } },
            series: [{ type: 'heatmap', data: cells, itemStyle: { borderColor: c.surface, borderWidth: 2, borderRadius: 4 } }]
        }), () => ({ head: [L('Gün', 'Tag'), L('Saat', 'Stunde'), S.count],
            rows: d.cells.slice().sort((a, b) => b[2] - a[2]).slice(0, 30).map((x) => [S.weekdays[x[1]], hours[x[0]] + ':00', x[2]]) }));
    }

    // ------------------------------------------------------------------
    // Integrity
    // ------------------------------------------------------------------
    function renderIntegrity(panel, d) {
        if (emptyIf(panel, d.n)) return;
        const types = d.byType;
        const typesEl = panel.querySelector('[data-chart="eventTypes"]');
        if (!types.length) typesEl.innerHTML = '<p class="rp-empty">' + esc(L('Hiç olay kaydedilmedi.', 'Keine Ereignisse aufgezeichnet.')) + '</p>';
        else chart(typesEl, (c) => ({
            grid: { left: 10, right: 30, top: 10, bottom: 20, containLabel: true },
            yAxis: { type: 'category', data: types.map((t) => S.events[t.type] || t.type), inverse: true, axisLabel: { color: c.body } },
            xAxis: { type: 'value', minInterval: 1 },
            series: [{ type: 'bar', data: types.map((t) => ({ value: t.n, itemStyle: { color: t.violation ? c.red : c.muted, borderRadius: 6 } })), label: { show: true, position: 'right', color: c.muted } }]
        }), () => ({ head: [L('Olay', 'Ereignis'), S.count, L('Uyarı mı?', 'Warnung?')], rows: types.map((t) => [S.events[t.type] || t.type, t.n, t.violation ? L('evet', 'ja') : L('hayır', 'nein')]) }));

        chart(panel.querySelector('[data-chart="warnDistribution"]'), (c) => ({
            xAxis: { type: 'category', data: ['0', '1', '2', '3+'], name: L('uyarı', 'Warnungen'), nameLocation: 'middle', nameGap: 30 },
            yAxis: { type: 'value', minInterval: 1 },
            series: [{ type: 'bar', data: d.distribution.map((v, i) => ({ value: v, itemStyle: { color: i === 0 ? c.green : i === 3 ? c.red : c.amber, borderRadius: [6, 6, 0, 0] } })) }]
        }), () => ({ head: [L('Uyarı', 'Warnungen'), S.attempts], rows: d.distribution.map((v, i) => [['0', '1', '2', '3+'][i], v]) }));
        panel.querySelector('[data-integrity-summary]').textContent =
            L(`Uyarı alanların ortalaması ${d.meanScoreWith == null ? '–' : n1.format(d.meanScoreWith)}, almayanların ${d.meanScoreWithout == null ? '–' : n1.format(d.meanScoreWithout)} puan. ${d.ended} deneme 3 uyarıda otomatik bitti.`,
              `Mit Warnungen im Schnitt ${d.meanScoreWith == null ? '–' : n1.format(d.meanScoreWith)}, ohne ${d.meanScoreWithout == null ? '–' : n1.format(d.meanScoreWithout)} Punkte. ${d.ended} Versuche endeten nach 3 Warnungen.`);
        const table = panel.querySelector('[data-table="flagged"]');
        table.innerHTML = d.attempts.length ? tableHtml([L('Öğrenci', 'Lernende'), L('Sınav', 'Prüfung'), L('Uyarı', 'Warnungen'), S.score, L('Tarih', 'Datum'), ''],
            d.attempts.map((a) => [a.name, a.quiz, a.warnings, a.score,   // a.date is Turkish wall-clock time without an offset
                new Date(a.date).toLocaleString(lang, { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' }),
                { html: '<a class="adm-btn adm-btn--sm" href="' + location.pathname.replace(/\/$/, '') + '/attempt/' + a.id + '" data-report-attempt="' + a.id + '">' + esc(L('Ayrıntı', 'Details')) + '</a>' }]))
            : '<p class="rp-empty">' + esc(L('Bu dönemde uyarı alan deneme yok.', 'Keine Versuche mit Warnungen in diesem Zeitraum.')) + '</p>';
    }

    // ------------------------------------------------------------------
    // Members: growth and the path from sign-up to a passed exam
    // ------------------------------------------------------------------
    function renderGrowth(panel, d) {
        if (emptyIf(panel, d.n)) return;
        const pts = d.points;
        const labels = pts.map((p) => new Date(p.date + 'T00:00:00').toLocaleDateString(lang, d.unit === 'month' ? { month: 'short', year: 'numeric' } : { day: 'numeric', month: 'short' }));
        const signupsEl = panel.querySelector('[data-chart="signups"]');
        chart(signupsEl, (c) => ({
            legend: { top: 0, data: [S.signups, S.firstExams, S.total], textStyle: { color: c.muted } },
            grid: { top: signupsEl.clientWidth < 520 ? 66 : 44 },   // the legend takes two lines on phones
            xAxis: { type: 'category', data: labels },
            yAxis: [{ type: 'value', minInterval: 1 }, { type: 'value', minInterval: 1, splitLine: { show: false } }],
            series: [
                { name: S.signups, type: 'bar', data: pts.map((p) => p.signups), itemStyle: { color: c.coral, borderRadius: [4, 4, 0, 0] }, barMaxWidth: 16 },
                { name: S.firstExams, type: 'bar', data: pts.map((p) => p.firstExams), itemStyle: { color: c.blue, borderRadius: [4, 4, 0, 0] }, barMaxWidth: 16 },
                { name: S.total, type: 'line', yAxisIndex: 1, data: pts.map((p) => p.total), symbol: 'none', smooth: true, lineStyle: { color: c.ink, width: 2 }, itemStyle: { color: c.ink } }
            ]
        }), () => ({ head: ['', S.signups, S.firstExams, S.total], rows: pts.map((p, i) => [labels[i], n0.format(p.signups), n0.format(p.firstExams), n0.format(p.total)]) }));
        panel.querySelector('[data-growth-note]').textContent = d.unknownDates ? fmt(S.unknownDates, d.unknownDates) : '';

        const top = d.funnel[0] || 0;
        const tones = (c) => [c.coralDeep, c.coral, c.amber, c.blue, c.green];
        chart(panel.querySelector('[data-chart="funnel"]'), (c) => ({
            tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
            grid: { left: 10, right: 110, top: 10, bottom: 10, containLabel: true },
            yAxis: { type: 'category', data: S.funnel, inverse: true, axisLabel: { color: c.body } },
            xAxis: { type: 'value', show: false, max: Math.max(1, top) },
            series: [{
                type: 'bar', barWidth: 22,
                data: d.funnel.map((v, i) => ({ value: v, itemStyle: { color: tones(c)[i], borderRadius: 6 } })),
                label: { show: true, position: 'right', color: c.muted, formatter: (p) => n0.format(p.value) + (top ? ' · ' + pct(p.value / top) : '') }
            }]
        }), () => ({ head: ['', S.count, '%'], rows: d.funnel.map((v, i) => [S.funnel[i], n0.format(v), top ? pct(v / top) : '–']) }));
    }

    // Cohorts: sign-up month × months since, share of the group that took an exam that month
    function renderCohort(panel, d) {
        const el = panel.querySelector('[data-chart="cohort"]');
        panel.querySelector('[data-cohort-note]').textContent = d.unknownDates ? fmt(S.unknownDates, d.unknownDates) : '';
        if (emptyIf(panel, d.cohorts.length)) return;
        const months = d.cohorts.map((x) => new Date(x.month + '-01T00:00:00').toLocaleDateString(lang, { month: 'short', year: 'numeric' }));
        const width = Math.max(...d.cohorts.map((x) => x.values.length));
        const cols = Array.from({ length: width }, (_, k) => String(k));
        el.style.height = Math.max(220, d.cohorts.length * 34 + 90) + 'px';
        // On phones the squares are too small for numbers and the row names are shortened (tooltip and table have both)
        const labels = el.clientWidth >= 520;
        const rowNames = labels ? months.map((m, i) => m + ' · ' + d.cohorts[i].size)
            : d.cohorts.map((x) => new Date(x.month + '-01T00:00:00').toLocaleDateString(lang, { month: 'short', year: '2-digit' }));
        chart(el, (c) => ({
            tooltip: { trigger: 'item', formatter: (p) => esc(months[p.value[1]]) + ' · ' + d.cohorts[p.value[1]].size + ' ' + esc(S.cohortSize) + '<br>' + p.value[0] + ' ' + esc(S.monthsLater) + ': ' + p.value[2] + '%' },
            grid: { left: 10, right: 10, top: 10, bottom: 40, containLabel: true },
            xAxis: { type: 'category', data: cols, name: S.monthsLater, nameLocation: 'middle', nameGap: 26, splitArea: { show: false } },
            yAxis: { type: 'category', data: rowNames, inverse: true, splitLine: { show: false } },
            visualMap: { min: 0, max: 100, show: false, inRange: { color: [c.surface, c.coralSoft, c.coral, c.coralDeep] } },
            series: [{
                type: 'heatmap',
                data: d.cohorts.flatMap((x, r) => x.values.map((v, k) => {
                    const value = Math.round(v * 100);
                    return { value: [k, r, value], label: { color: value > 55 ? '#fff' : c.ink } };
                })),
                label: { show: labels, fontSize: 10, formatter: (p) => p.value[2] + '%' },
                itemStyle: { borderColor: c.surface, borderWidth: 2, borderRadius: 4 }
            }]
        }), () => ({
            head: [S.cohortMonth, S.cohortSize].concat(cols.map((k) => k + ' ' + S.monthsLater)),
            rows: d.cohorts.map((x, i) => [months[i], x.size].concat(cols.map((_, k) => x.values[k] == null ? '' : pct(x.values[k]))))
        }));
    }

    // One member's scores over time (Views/Admin/Member.cshtml); points: newest first
    function renderMemberTrend(box, d) {
        const el = box.querySelector('[data-chart="trend"]');
        const pts = d.points.slice().reverse();
        if (!pts.length) { el.innerHTML = '<p class="rp-empty">' + esc(S.empty) + '</p>'; return; }
        const when = (p) => new Date(p.date).toLocaleString(lang, { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
        chart(el, (c) => ({
            tooltip: { trigger: 'item', formatter: (p) => { const x = pts[p.dataIndex]; return esc(x.quiz) + '<br>' + esc(when(x)) + '<br>' + esc(S.score) + ': ' + x.score; } },
            // ECharts names months in English by itself: the labels come from the browser in the page's language
            xAxis: { type: 'time', axisLabel: { hideOverlap: true, formatter: (v) => new Date(v).toLocaleDateString(lang, { day: 'numeric', month: 'short' }) } },
            yAxis: { type: 'value', min: 0, max: 100 },
            series: [{
                type: 'line', symbolSize: 9, lineStyle: { color: c.line, width: 2 },
                data: pts.map((x) => ({ value: [x.date, x.score], itemStyle: { color: x.score >= d.pass ? c.green : c.coral } })),
                markLine: { silent: true, symbol: 'none', lineStyle: { color: c.ink, type: 'dashed' }, label: { formatter: S.passLine, color: c.muted, position: 'insideStartTop' }, data: [{ yAxis: d.pass }] }
            }]
        }), () => ({ head: [S.date, S.quiz, S.score], rows: d.points.map((x) => [when(x), x.quiz, x.score]) }));
    }

    // ------------------------------------------------------------------
    // Attempt side panel
    // ------------------------------------------------------------------
    const drawer = document.getElementById('attemptDrawer');
    const drawerBody = drawer?.querySelector('[data-drawer-body]');
    let opener = null;
    document.addEventListener('click', async (e) => {
        const link = e.target.closest('[data-report-attempt]');
        if (!link || !drawer || !drawer.showModal || e.ctrlKey || e.metaKey || e.shiftKey) return;
        e.preventDefault();
        opener = link;
        const url = link.getAttribute('href');
        drawer.querySelector('[data-drawer-open]').href = url;
        drawerBody.innerHTML = '<p class="rp-empty">' + esc(S.loading) + '</p>';
        drawer.showModal();
        try {
            const response = await fetch(url + (url.includes('?') ? '&' : '?') + 'panel=true', { credentials: 'same-origin' });
            if (!response.ok) throw new Error('HTTP ' + response.status);
            drawerBody.innerHTML = await response.text();
            drawerBody.scrollTop = 0;
        } catch (err) {
            drawerBody.innerHTML = '<p class="adm-alert adm-alert--error">' + esc(S.failed) + '</p>';
        }
    });
    drawer?.querySelector('[data-drawer-close]')?.addEventListener('click', () => drawer.close());
    drawer?.addEventListener('click', (e) => { if (e.target === drawer) drawer.close(); });   // click on the backdrop
    drawer?.addEventListener('close', () => { opener?.focus(); });

    // ------------------------------------------------------------------
    // Live: a new result arrived (ExamService → QuizMonitorHub "ReceiveSubmission")
    // ------------------------------------------------------------------
    const notice = document.querySelector('[data-report-new]');
    if (window.signalR && notice) {
        let count = 0;
        const connection = new signalR.HubConnectionBuilder().withUrl('/quizMonitorHub').withAutomaticReconnect()
            .configureLogging(signalR.LogLevel.None).build();
        connection.on('ReceiveSubmission', (s) => {
            count++;
            notice.hidden = false;
            notice.querySelector('[data-report-new-text]').textContent = S.newResults + (count > 1 ? ' (' + count + ')' : '');
            if (s && s.UserName) notice.title = s.UserName + ' · ' + (s.QuizTitle || '') + ' · ' + s.Score;
        });
        connection.start().catch(() => { /* live updates are a bonus; the page works without them */ });
    }
})();
