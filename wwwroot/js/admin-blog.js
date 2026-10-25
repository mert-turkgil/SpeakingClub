// ============================================================================
// admin-blog.js — the blog post form (Views/Admin/_BlogForm.cshtml).
// Live checklist + score in the side panel, the address preview, loading a
// quiz's questions, a name box per new attachment, deleting attachments
// that are already saved. The editors themselves: ckeditor.js.
// ============================================================================
(function () {
    'use strict';

    const form = document.getElementById('blogForm');
    if (!form) return;
    const de = document.body.dataset.lang === 'de';
    const t = (tr, deText) => (de ? deText : tr);
    const field = (name) => form.querySelector('[name="' + name + '"]');
    const lower = (s) => String(s || '').toLocaleLowerCase(de ? 'de' : 'tr');
    const token = () => form.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

    // ------------------------------------------------------------------
    // Address (slug) preview — same rules as AdminController.GenerateSlug
    // ------------------------------------------------------------------
    const slugify = (text) => lower(text)
        .replace(/ç/g, 'c').replace(/ğ/g, 'g').replace(/ı/g, 'i').replace(/ö/g, 'o').replace(/ş/g, 's').replace(/ü/g, 'u')
        .replace(/ä/g, 'a').replace(/ß/g, 'ss')
        .normalize('NFD').replace(/[̀-ͯ]/g, '')
        .replace(/\s+/g, '-').replace(/[^a-z0-9-]/g, '').replace(/-+/g, '-').replace(/^-+|-+$/g, '').slice(0, 100);
    const preview = form.querySelector('[data-slug-preview]');
    const title = field('Title');
    const slug = field('Slug');
    function updatePreview() {
        if (!preview) return;
        const value = slugify(slug.value) || slugify(title.value) || '…';
        preview.innerHTML = '';
        preview.append(document.createTextNode(t('Yazının adresi: ', 'Adresse: ') + form.dataset.siteUrl + '/yazilar/'));
        const b = document.createElement('b');
        b.textContent = value;
        preview.append(b);
    }
    [title, slug].forEach((input) => input?.addEventListener('input', updatePreview));
    updatePreview();

    // ------------------------------------------------------------------
    // Checklist + score (side panel)
    // ------------------------------------------------------------------
    const meter = form.querySelector('[data-seo-meter]');
    const coverInput = form.querySelector('[data-cover-input]');
    const wordCount = (html) => String(html || '').replace(/<[^>]+>/g, ' ').replace(/&nbsp;/g, ' ').trim().split(/\s+/).filter(Boolean).length;
    const checks = {
        title: { weight: 20, ok: () => title.value.trim().length >= 10 },
        author: { weight: 10, ok: () => field('Author').value.trim().length > 0 },
        summary: { weight: 15, ok: () => field('Description').value.trim().length >= 50 },
        content: { weight: 25, ok: () => wordCount(field('Content').value) >= 300 },
        cover: { weight: 15, ok: () => form.dataset.hasCover === 'true' || (coverInput && coverInput.files.length > 0) },
        keyword: {
            weight: 15,
            ok: () => {
                const kw = lower(field('FocusKeyphrase')?.value).trim();
                return kw.length > 0 && lower(title.value).includes(kw);
            }
        }
    };
    function updateMeter() {
        if (!meter) return;
        let score = 0;
        Object.keys(checks).forEach((key) => {
            const ok = checks[key].ok();
            if (ok) score += checks[key].weight;
            meter.querySelector('[data-check="' + key + '"]')?.classList.toggle('is-ok', ok);
        });
        const ring = meter.querySelector('.adm-meter__ring');
        ring.style.setProperty('--p', score);
        ring.style.setProperty('--meter', score >= 80 ? 'var(--a-green)' : score >= 50 ? 'var(--a-amber)' : 'var(--a-red)');
        meter.querySelector('[data-seo-score]').textContent = score;
    }
    form.addEventListener('input', updateMeter);
    form.addEventListener('change', updateMeter);
    // The editors write into their textareas without firing input events
    setInterval(updateMeter, 1500);
    updateMeter();

    // ------------------------------------------------------------------
    // Quiz → its questions
    // ------------------------------------------------------------------
    const quizSelect = form.querySelector('[data-quiz-select]');
    const questionSelect = form.querySelector('[data-question-select]');
    quizSelect?.addEventListener('change', async () => {
        const none = questionSelect.options[0].cloneNode(true);
        questionSelect.innerHTML = '';
        questionSelect.append(none);
        if (!quizSelect.value) return;
        questionSelect.disabled = true;
        try {
            const response = await fetch(form.dataset.questionsUrl + '?quizId=' + encodeURIComponent(quizSelect.value), { headers: { Accept: 'application/json' } });
            const questions = await response.json();
            questions.forEach((q) => questionSelect.append(new Option(q.questionText || ('#' + q.questionId), q.questionId)));
            if (!questions.length) window.admToast(t('Bu sınavda henüz soru yok.', 'Diese Prüfung hat noch keine Fragen.'), 'warning');
        } catch (e) {
            window.admToast(t('Sorular yüklenemedi.', 'Die Fragen konnten nicht geladen werden.'), 'error');
        } finally {
            questionSelect.disabled = false;
        }
    });

    // ------------------------------------------------------------------
    // New attachments: one name box per file, joined into AttachmentDisplayNames on save
    // ------------------------------------------------------------------
    const attachInput = form.querySelector('[data-attachments-input]');
    const namesBox = form.querySelector('[data-attachment-names]');
    const namesField = form.querySelector('[data-attachment-names-field]');
    attachInput?.addEventListener('change', () => {
        namesBox.querySelectorAll('.adm-field').forEach((n) => n.remove());
        const files = Array.from(attachInput.files || []);
        namesBox.hidden = files.length === 0;
        files.forEach((file, i) => {
            const wrap = document.createElement('div');
            wrap.className = 'adm-field';
            const label = document.createElement('label');
            label.htmlFor = 'attach-name-' + i;
            label.textContent = file.name;
            const input = document.createElement('input');
            input.type = 'text';
            input.id = 'attach-name-' + i;
            input.className = 'adm-input';
            input.placeholder = file.name.replace(/\.[^.]+$/, '');
            input.dataset.attachName = '';
            wrap.append(label, input);
            namesBox.append(wrap);
        });
    });
    form.addEventListener('submit', () => {
        if (!namesField || !namesBox) return;
        const names = Array.from(namesBox.querySelectorAll('[data-attach-name]')).map((i) => i.value.replace(/,/g, ' ').trim());
        namesField.value = names.some(Boolean) ? names.join(',') : '';
    });

    // ------------------------------------------------------------------
    // Delete an attachment that is already saved (right away, after confirming)
    // ------------------------------------------------------------------
    form.addEventListener('click', (e) => {
        const button = e.target.closest('[data-delete-file]');
        if (!button) return;
        window.admConfirm({
            title: t('Bu dosya silinsin mi?', 'Diese Datei löschen?'),
            text: t('“' + button.dataset.fileName + '” yazıdan kaldırılacak ve sunucudan silinecek. Bu hemen olur, kaydetmenize gerek yok.', '„' + button.dataset.fileName + '“ wird aus dem Beitrag entfernt und gelöscht. Das passiert sofort, ohne Speichern.'),
            ok: t('Evet, dosyayı sil', 'Ja, Datei löschen')
        }).then(async () => {
            button.classList.add('is-loading');
            try {
                const body = new URLSearchParams({ fileId: button.dataset.deleteFile, blogId: button.dataset.blogId, __RequestVerificationToken: token() });
                const response = await fetch(form.dataset.fileDeleteUrl, { method: 'POST', headers: { 'Content-Type': 'application/x-www-form-urlencoded' }, body });
                const result = await response.json();
                if (!result.success) throw new Error(result.message);
                button.closest('li').remove();
                window.admToast(t('Dosya silindi.', 'Die Datei wurde gelöscht.'), 'success');
            } catch (err) {
                button.classList.remove('is-loading');
                window.admToast(t('Dosya silinemedi.', 'Die Datei konnte nicht gelöscht werden.'), 'error');
            }
        });
    });
})();
