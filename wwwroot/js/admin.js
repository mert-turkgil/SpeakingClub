// ============================================================================
// admin.js — behaviour shared by every admin page (Views/Shared/_AdminLayout).
// Sidebar drawer, user menu, theme, toasts, the "Are you sure?" dialog for
// deletes, list search/sort/"show more", file previews, character counters,
// password generator, form tabs, small rich-text editors and a warning when
// leaving a form with unsaved changes. Everything is opt-in through data-*
// attributes, so pages only describe what they need.
// ============================================================================
(function () {
    'use strict';

    const html = document.documentElement;
    const body = document.body;
    const shell = document.querySelector('[data-admin]');
    if (!shell) return;
    const de = (body.dataset.lang || 'tr') === 'de';
    const t = (tr, deText) => (de ? deText : tr);
    const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const store = {
        get(key) { try { return localStorage.getItem(key); } catch (e) { return null; } },
        set(key, value) { try { localStorage.setItem(key, value); } catch (e) { /* private mode */ } }
    };

    // ------------------------------------------------------------------
    // Sidebar drawer (tablets and phones)
    // ------------------------------------------------------------------
    const openSide = () => { shell.classList.add('is-side-open'); document.querySelector('.adm-side a')?.focus({ preventScroll: true }); };
    const closeSide = () => shell.classList.remove('is-side-open');
    document.querySelectorAll('[data-side-open]').forEach((b) => b.addEventListener('click', openSide));
    document.querySelectorAll('[data-side-close]').forEach((b) => b.addEventListener('click', closeSide));
    document.addEventListener('keydown', (e) => { if (e.key === 'Escape') { closeSide(); closeMenus(); } });

    // Top bar shadow once the page scrolls
    const top = document.querySelector('.adm-top');
    if (top) {
        const onScroll = () => top.classList.toggle('is-scrolled', window.scrollY > 4);
        window.addEventListener('scroll', onScroll, { passive: true });
        onScroll();
    }

    // ------------------------------------------------------------------
    // User menu
    // ------------------------------------------------------------------
    function closeMenus() {
        document.querySelectorAll('[data-menu-button][aria-expanded="true"]').forEach((b) => {
            b.setAttribute('aria-expanded', 'false');
            document.getElementById(b.getAttribute('aria-controls'))?.classList.remove('is-open');
        });
    }
    document.querySelectorAll('[data-menu-button]').forEach((button) => {
        const menu = document.getElementById(button.getAttribute('aria-controls'));
        button.addEventListener('click', (e) => {
            e.stopPropagation();
            const open = button.getAttribute('aria-expanded') !== 'true';
            closeMenus();
            button.setAttribute('aria-expanded', String(open));
            menu?.classList.toggle('is-open', open);
        });
    });
    document.addEventListener('click', (e) => { if (!e.target.closest('.adm-menu')) closeMenus(); });

    // ------------------------------------------------------------------
    // Light / dark theme (same storage key as the public site)
    // ------------------------------------------------------------------
    document.querySelectorAll('[data-theme-toggle]').forEach((button) => {
        const sync = () => {
            const dark = html.getAttribute('data-bs-theme') === 'dark';
            button.setAttribute('aria-pressed', String(dark));
            button.querySelector('i').className = dark ? 'fa-solid fa-sun' : 'fa-solid fa-moon';
            button.setAttribute('aria-label', dark ? t('Açık temaya geç', 'Helles Design') : t('Koyu temaya geç', 'Dunkles Design'));
        };
        button.addEventListener('click', () => {
            const next = html.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
            html.setAttribute('data-bs-theme', next);
            store.set('theme', next);
            sync();
        });
        sync();
    });

    // ------------------------------------------------------------------
    // Toasts: success and warnings fade on their own, errors stay until closed
    // ------------------------------------------------------------------
    function dismiss(toast) {
        if (!toast || toast.classList.contains('is-leaving')) return;
        toast.classList.add('is-leaving');
        setTimeout(() => toast.remove(), reduceMotion ? 0 : 260);
    }
    window.admToast = function (message, kind) {
        const box = document.querySelector('.adm-toasts');
        if (!box) return;
        const icons = { success: 'fa-circle-check', error: 'fa-circle-exclamation', warning: 'fa-triangle-exclamation' };
        const toast = document.createElement('div');
        toast.className = 'adm-toast adm-toast--' + (kind || 'success');
        toast.setAttribute('role', kind === 'error' ? 'alert' : 'status');
        toast.innerHTML = '<i class="fa-solid ' + (icons[kind] || icons.success) + '" aria-hidden="true"></i><p></p><button type="button" data-toast-close aria-label="' + t('Kapat', 'Schließen') + '"><i class="fa-solid fa-xmark"></i></button>';
        toast.querySelector('p').textContent = message;
        box.appendChild(toast);
        arm(toast);
    };
    function arm(toast) {
        toast.querySelector('[data-toast-close]')?.addEventListener('click', () => dismiss(toast));
        if (!toast.classList.contains('adm-toast--error')) {
            setTimeout(() => dismiss(toast), toast.classList.contains('adm-toast--warning') ? 10000 : 6000);
        }
    }
    document.querySelectorAll('.adm-toast').forEach(arm);

    // ------------------------------------------------------------------
    // "Are you sure?" dialog. Put data-confirm on a <form> (or a submit button / link):
    //   data-confirm="Title"  data-confirm-text="Explanation"  data-confirm-note="Extra warning"
    //   data-confirm-ok="Button label"
    // ------------------------------------------------------------------
    const dialog = document.getElementById('adm-confirm');
    let pending = null;
    function ask(source, proceed) {
        if (!dialog || typeof dialog.showModal !== 'function') {
            if (window.confirm(source.dataset.confirm || t('Emin misiniz?', 'Sind Sie sicher?'))) proceed();
            return;
        }
        dialog.querySelector('[data-confirm-title]').textContent = source.dataset.confirm || t('Emin misiniz?', 'Sind Sie sicher?');
        dialog.querySelector('[data-confirm-text]').textContent = source.dataset.confirmText || t('Bu işlem geri alınamaz.', 'Das lässt sich nicht rückgängig machen.');
        dialog.querySelector('[data-confirm-note]').textContent = source.dataset.confirmNote || '';
        dialog.querySelector('[data-confirm-ok-label]').textContent = source.dataset.confirmOk || t('Evet, sil', 'Ja, löschen');
        pending = proceed;
        dialog.showModal();
        dialog.querySelector('[data-confirm-cancel]').focus();
    }
    // For page scripts: admConfirm({ title, text, note, ok }).then(() => …) runs only when confirmed
    window.admConfirm = (options) => new Promise((resolve) => {
        ask({ dataset: { confirm: options.title || '', confirmText: options.text || '', confirmNote: options.note || '', confirmOk: options.ok || '' } }, () => resolve(true));
    });
    if (dialog) {
        dialog.querySelector('[data-confirm-cancel]').addEventListener('click', () => { pending = null; dialog.close(); });
        dialog.querySelector('[data-confirm-ok]').addEventListener('click', () => {
            const go = pending;
            pending = null;
            dialog.close();
            if (go) go();
        });
        dialog.addEventListener('click', (e) => { if (e.target === dialog) { pending = null; dialog.close(); } });
    }
    document.addEventListener('submit', (e) => {
        const form = e.target;
        if (!(form instanceof HTMLFormElement) || !form.dataset.confirm || form.dataset.confirmed === '1') return;
        e.preventDefault();
        ask(form, () => {
            form.dataset.confirmed = '1';
            dirty.clear(form);
            HTMLFormElement.prototype.submit.call(form);
        });
    }, true);
    document.addEventListener('click', (e) => {
        const link = e.target.closest('a[data-confirm]');
        if (!link) return;
        e.preventDefault();
        ask(link, () => { window.location.href = link.href; });
    });

    // ------------------------------------------------------------------
    // Guides ("How do I…?") remember whether they were closed
    // ------------------------------------------------------------------
    document.querySelectorAll('details[data-guide]').forEach((guide) => {
        const key = 'adm-guide:' + guide.dataset.guide;
        if (store.get(key) === 'closed') guide.open = false;
        guide.addEventListener('toggle', () => store.set(key, guide.open ? 'open' : 'closed'));
    });

    // ------------------------------------------------------------------
    // Lists: search, column sort, filters and "show more"
    //   <div data-list data-page-size="25"> … <input data-list-search> … <table> …
    //   <span data-list-count></span> … <button data-list-more></button> … <p class="adm-noresult">
    // ------------------------------------------------------------------
    const fold = (s) => String(s || '').toLocaleLowerCase(de ? 'de' : 'tr').replace(/ı/g, 'i').normalize('NFD').replace(/[̀-ͯ]/g, '').replace(/ß/g, 'ss');
    document.querySelectorAll('[data-list]').forEach((list) => {
        const tbody = list.querySelector('tbody');
        if (!tbody) return;
        const rows = Array.from(tbody.rows);
        rows.forEach((row, i) => row.style.setProperty('--i', i));
        // Search text, re-read when a page updates a row's data-search (e.g. after editing a site text)
        const textOf = (row) => {
            const source = row.dataset.search || row.textContent;
            if (row._source !== source) { row._source = source; row._text = fold(source); }
            return row._text;
        };
        const search = list.querySelector('[data-list-search]');
        const count = list.querySelector('[data-list-count]');
        const more = list.querySelector('[data-list-more]');
        const none = list.querySelector('.adm-noresult');
        const filters = Array.from(list.querySelectorAll('[data-list-filter]'));
        const pageSize = parseInt(list.dataset.pageSize || '25', 10);
        let limit = pageSize;

        function render() {
            const words = fold(search ? search.value : '').split(/\s+/).filter(Boolean);
            const matching = rows.filter((row) => words.every((w) => textOf(row).includes(w)) &&
                filters.every((f) => !f.value || (row.dataset[f.dataset.listFilter] || '').split(' ').includes(f.value)));
            rows.forEach((row) => row.classList.add('is-hidden'));
            matching.slice(0, limit).forEach((row) => row.classList.remove('is-hidden'));
            if (count) {
                count.textContent = matching.length === rows.length
                    ? t(rows.length + ' kayıt', rows.length + ' Einträge')
                    : t(matching.length + ' / ' + rows.length + ' kayıt gösteriliyor', matching.length + ' von ' + rows.length + ' Einträgen');
            }
            if (more) {
                const rest = matching.length - limit;
                more.hidden = rest <= 0;
                more.querySelector('span').textContent = t('Daha fazla göster (' + rest + ' kaldı)', 'Mehr anzeigen (noch ' + rest + ')');
            }
            if (none) none.classList.toggle('is-visible', matching.length === 0);
            list.querySelector('.adm-search')?.classList.toggle('has-value', !!(search && search.value));
        }
        let timer = 0;
        search?.addEventListener('input', () => { clearTimeout(timer); timer = setTimeout(() => { limit = pageSize; render(); }, 120); });
        list.querySelector('.adm-search button')?.addEventListener('click', () => { search.value = ''; limit = pageSize; render(); search.focus(); });
        filters.forEach((f) => f.addEventListener('change', () => { limit = pageSize; render(); }));
        more?.addEventListener('click', () => { limit += pageSize; render(); });

        list.querySelectorAll('th[data-sort]').forEach((th) => {
            th.tabIndex = 0;
            const sort = () => {
                const index = Array.from(th.parentElement.children).indexOf(th);
                const asc = th.getAttribute('aria-sort') !== 'ascending';
                list.querySelectorAll('th[data-sort]').forEach((o) => o.removeAttribute('aria-sort'));
                th.setAttribute('aria-sort', asc ? 'ascending' : 'descending');
                const value = (row) => {
                    const cell = row.cells[index];
                    const raw = cell ? (cell.dataset.value ?? cell.textContent.trim()) : '';
                    return th.dataset.sort === 'num' ? parseFloat(raw) || 0 : fold(raw);
                };
                rows.sort((a, b) => {
                    const x = value(a); const y = value(b);
                    return (x > y ? 1 : x < y ? -1 : 0) * (asc ? 1 : -1);
                });
                rows.forEach((row) => tbody.appendChild(row));
                render();
            };
            th.addEventListener('click', sort);
            th.addEventListener('keydown', (e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); sort(); } });
        });
        render();
    });

    // ------------------------------------------------------------------
    // Character counters: <span class="adm-count" data-count-for="FieldId" data-max="160">
    // ------------------------------------------------------------------
    document.querySelectorAll('[data-count-for]').forEach((counter) => {
        const field = document.getElementById(counter.dataset.countFor);
        if (!field) return;
        const max = parseInt(counter.dataset.max || field.getAttribute('maxlength') || '0', 10);
        const update = () => {
            const n = field.value.length;
            counter.textContent = max ? n + ' / ' + max : String(n);
            counter.classList.toggle('is-near', max && n > max * 0.85 && n <= max);
            counter.classList.toggle('is-over', max && n > max);
        };
        field.addEventListener('input', update);
        update();
    });

    // ------------------------------------------------------------------
    // File pickers with a preview: <label class="adm-drop"> <input type="file"> … <div class="adm-drop__preview">
    // ------------------------------------------------------------------
    document.querySelectorAll('.adm-drop').forEach((drop) => {
        const input = drop.querySelector('input[type="file"]');
        const preview = drop.querySelector('.adm-drop__preview');
        if (!input || !preview) return;
        ['dragenter', 'dragover'].forEach((ev) => drop.addEventListener(ev, () => drop.classList.add('is-over')));
        ['dragleave', 'drop'].forEach((ev) => drop.addEventListener(ev, () => drop.classList.remove('is-over')));
        input.addEventListener('change', () => {
            preview.innerHTML = '';
            const files = Array.from(input.files || []);
            drop.classList.toggle('has-file', files.length > 0);
            if (!files.length) return;
            const file = files[0];
            if (files.length === 1 && file.type.startsWith('image/')) {
                const img = document.createElement('img');
                img.alt = '';
                img.src = URL.createObjectURL(file);
                preview.appendChild(img);
            } else if (files.length === 1 && file.type.startsWith('audio/')) {
                const audio = document.createElement('audio');
                audio.controls = true;
                audio.src = URL.createObjectURL(file);
                preview.appendChild(audio);
            }
            const name = document.createElement('span');
            name.className = 'adm-drop__name';
            name.textContent = files.length === 1
                ? file.name + ' · ' + Math.max(1, Math.round(file.size / 1024)) + ' KB'
                : t(files.length + ' dosya seçildi: ', files.length + ' Dateien gewählt: ') + files.map((f) => f.name).join(', ');
            preview.appendChild(name);
            const max = parseInt(input.dataset.maxMb || '0', 10);
            if (max && files.some((f) => f.size > max * 1024 * 1024)) {
                window.admToast(t('Dosya çok büyük. En fazla ' + max + ' MB olabilir.', 'Die Datei ist zu groß (höchstens ' + max + ' MB).'), 'warning');
            }
        });
    });

    // ------------------------------------------------------------------
    // Password generator / reveal / copy
    // ------------------------------------------------------------------
    function makePassword() {
        const sets = ['ABCDEFGHJKLMNPQRSTUVWXYZ', 'abcdefghijkmnpqrstuvwxyz', '23456789', '!#%+=?@'];
        const all = sets.join('');
        const pick = (chars) => chars[crypto.getRandomValues(new Uint32Array(1))[0] % chars.length];
        const chars = sets.map(pick);
        while (chars.length < 14) chars.push(pick(all));
        for (let i = chars.length - 1; i > 0; i--) {
            const j = crypto.getRandomValues(new Uint32Array(1))[0] % (i + 1);
            [chars[i], chars[j]] = [chars[j], chars[i]];
        }
        return chars.join('');
    }
    document.querySelectorAll('[data-generate-password]').forEach((button) => {
        button.addEventListener('click', () => {
            const value = makePassword();
            button.dataset.generatePassword.split(',').forEach((id) => {
                const field = document.getElementById(id.trim());
                if (!field) return;
                field.value = value;
                field.type = 'text';
                field.dispatchEvent(new Event('input', { bubbles: true }));
            });
            window.admToast(t('Yeni şifre oluşturuldu. Kişiye iletmek için “Kopyala”ya basın.', 'Neues Passwort erzeugt. Mit „Kopieren“ können Sie es weitergeben.'), 'success');
        });
    });
    document.querySelectorAll('[data-copy]').forEach((button) => {
        button.addEventListener('click', async () => {
            const field = document.getElementById(button.dataset.copy);
            if (!field || !field.value) { window.admToast(t('Kopyalanacak bir şey yok.', 'Nichts zum Kopieren.'), 'warning'); return; }
            try {
                await navigator.clipboard.writeText(field.value);
                window.admToast(t('Panoya kopyalandı.', 'In die Zwischenablage kopiert.'), 'success');
            } catch (e) {
                field.type = 'text';
                field.select();
            }
        });
    });
    document.querySelectorAll('[data-reveal]').forEach((button) => {
        button.addEventListener('click', () => {
            const field = document.getElementById(button.dataset.reveal);
            if (!field) return;
            const show = field.type === 'password';
            field.type = show ? 'text' : 'password';
            button.querySelector('i').className = show ? 'fa-regular fa-eye-slash' : 'fa-regular fa-eye';
            button.setAttribute('aria-pressed', String(show));
        });
    });

    // ------------------------------------------------------------------
    // Tabs inside long forms: <div data-tabs> <button role="tab" aria-controls="panel-id"> …
    // A tab whose panel holds a validation error gets a red dot and opens first.
    // ------------------------------------------------------------------
    document.querySelectorAll('[data-tabs]').forEach((tabs) => {
        const buttons = Array.from(tabs.querySelectorAll('[role="tab"]'));
        const panel = (b) => document.getElementById(b.getAttribute('aria-controls'));
        const select = (button, focus) => {
            buttons.forEach((b) => {
                const on = b === button;
                b.setAttribute('aria-selected', String(on));
                b.tabIndex = on ? 0 : -1;
                panel(b)?.classList.toggle('is-active', on);
            });
            if (focus) button.focus();
            store.set('adm-tab:' + location.pathname, button.id);
        };
        buttons.forEach((b, i) => {
            b.addEventListener('click', () => select(b));
            b.addEventListener('keydown', (e) => {
                const step = e.key === 'ArrowRight' ? 1 : e.key === 'ArrowLeft' ? -1 : 0;
                if (step) { e.preventDefault(); select(buttons[(i + step + buttons.length) % buttons.length], true); }
            });
        });
        const hasError = (b) => !!panel(b)?.querySelector('.input-validation-error, .field-validation-error:not(:empty)');
        buttons.forEach((b) => b.classList.toggle('has-error', hasError(b)));
        const firstError = buttons.find(hasError);
        select(firstError || buttons[0]);
        // Client-side validation errors appearing later also mark their tab
        document.addEventListener('invalid-form.validate', () => setTimeout(() => {
            buttons.forEach((b) => b.classList.toggle('has-error', hasError(b)));
            const bad = buttons.find(hasError);
            if (bad) select(bad);
        }, 0));
        tabs.querySelectorAll('[data-tab-next]').forEach((n) => n.addEventListener('click', () => {
            const current = buttons.findIndex((b) => b.getAttribute('aria-selected') === 'true');
            select(buttons[Math.min(current + 1, buttons.length - 1)]);
            window.scrollTo({ top: tabs.getBoundingClientRect().top + window.scrollY - 90, behavior: reduceMotion ? 'auto' : 'smooth' });
        }));
    });
    document.querySelectorAll('[data-tab-go]').forEach((link) => link.addEventListener('click', (e) => {
        const tab = document.getElementById(link.dataset.tabGo);
        if (!tab) return;
        e.preventDefault();
        tab.click();
        tab.scrollIntoView({ behavior: reduceMotion ? 'auto' : 'smooth', block: 'center' });
    }));

    // ------------------------------------------------------------------
    // Small rich-text editors: <textarea data-editor="mini"> (CKEditor 5 must be on the page)
    // ------------------------------------------------------------------
    const miniEditors = [];
    // The editor hides its textarea; let jQuery Validation still check it (it skips hidden fields by default).
    // Runs before jquery.validate.unobtrusive parses the forms on DOM ready.
    if (window.jQuery && window.jQuery.validator) window.jQuery.validator.setDefaults({ ignore: ':hidden:not([data-editor])' });
    if (window.CKEDITOR && document.querySelector('textarea[data-editor="mini"]')) {
        const { ClassicEditor, Essentials, Paragraph, Bold, Italic, Underline, Link, List, Undo } = window.CKEDITOR;
        const licenseKey = document.querySelector('meta[name="ck-license-key"]')?.content || 'GPL';
        document.querySelectorAll('textarea[data-editor="mini"]').forEach((area) => {
            ClassicEditor.create(area, {
                licenseKey,
                plugins: [Essentials, Paragraph, Bold, Italic, Underline, Link, List, Undo],
                toolbar: ['bold', 'italic', 'underline', '|', 'link', 'bulletedList', 'numberedList', '|', 'undo', 'redo'],
                placeholder: area.getAttribute('placeholder') || '',
                language: de ? 'de' : 'tr'
            }).then((editor) => {
                miniEditors.push({ area, editor });
                editor.model.document.on('change:data', () => { area.value = editor.getData(); dirty.mark(area.form); });
            }).catch((err) => console.error('Editor could not start:', err));
        });
        document.addEventListener('submit', (e) => {
            miniEditors.forEach(({ area, editor }) => { if (area.form === e.target) area.value = editor.getData(); });
        }, true);
    }

    // ------------------------------------------------------------------
    // Forms: unsaved-changes warning (data-dirty-guard) and a busy save button (data-loading)
    // ------------------------------------------------------------------
    const dirtyForms = new Set();
    const dirty = {
        mark(form) {
            if (!form || !form.hasAttribute('data-dirty-guard') || dirtyForms.has(form)) return;
            dirtyForms.add(form);
            form.querySelectorAll('.adm-savebar__hint').forEach((h) => {
                h.classList.add('is-dirty');
                h.textContent = t('Kaydedilmemiş değişiklikler var', 'Ungespeicherte Änderungen');
            });
        },
        clear(form) { dirtyForms.delete(form); }
    };
    window.admMarkDirty = (form) => dirty.mark(form);
    document.querySelectorAll('form[data-dirty-guard]').forEach((form) => {
        form.addEventListener('input', () => dirty.mark(form));
        form.addEventListener('change', () => dirty.mark(form));
    });
    window.addEventListener('beforeunload', (e) => {
        if (dirtyForms.size) { e.preventDefault(); e.returnValue = ''; }
    });
    document.addEventListener('submit', (e) => {
        const form = e.target;
        if (!(form instanceof HTMLFormElement) || e.defaultPrevented) return;
        // jQuery Validation stops invalid forms; let it decide before showing the spinner
        const $ = window.jQuery;
        if ($ && $.fn.valid && $(form).data('validator') && !$(form).valid()) return;
        dirty.clear(form);
        if (form.hasAttribute('data-loading')) {
            const button = form.querySelector('button[type="submit"]:not([name]), .adm-savebar button[type="submit"]');
            if (button) {
                button.classList.add('is-loading');
                setTimeout(() => { button.disabled = true; }, 0);
            }
        }
    });
    window.addEventListener('pageshow', (e) => {
        if (e.persisted) document.querySelectorAll('.is-loading').forEach((b) => { b.classList.remove('is-loading'); b.disabled = false; });
    });
})();
