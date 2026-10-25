// ============================================================================
// admin-texts.js — Admin → Site metinleri. "Düzenle" opens one dialog with the
// Turkish and German text; "Kaydet" posts each changed language to
// /Admin/EditTranslation and updates the row in place (no page reload).
// ============================================================================
(function () {
    'use strict';

    const list = document.querySelector('[data-texts]');
    const dialog = document.getElementById('adm-text-dialog');
    if (!list || !dialog || typeof dialog.showModal !== 'function') return;
    const de = document.body.dataset.lang === 'de';
    const t = (tr, deText) => (de ? deText : tr);
    const form = dialog.querySelector('[data-text-form]');
    const fields = { 'tr-TR': dialog.querySelector('#text-tr'), 'de-DE': dialog.querySelector('#text-de') };
    const save = dialog.querySelector('[data-text-save]');
    const token = form.querySelector('input[name="__RequestVerificationToken"]').value;
    let row = null;
    let original = {};

    list.addEventListener('click', (e) => {
        const button = e.target.closest('[data-edit-text]');
        if (!button) return;
        row = button.closest('tr');
        original = { 'tr-TR': button.dataset.tr || '', 'de-DE': button.dataset.de || '' };
        dialog.querySelector('[data-text-key]').textContent = row.dataset.key;
        Object.keys(fields).forEach((lang) => { fields[lang].value = original[lang]; });
        dialog.showModal();
        fields['tr-TR'].focus();
    });

    dialog.querySelector('[data-text-cancel]').addEventListener('click', () => dialog.close());
    dialog.addEventListener('click', (e) => { if (e.target === dialog) dialog.close(); });

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        const changed = Object.keys(fields).filter((lang) => fields[lang].value !== original[lang]);
        if (!changed.length) { dialog.close(); return; }
        save.classList.add('is-loading');
        save.disabled = true;
        try {
            for (const lang of changed) {
                const body = new URLSearchParams({ name: row.dataset.key, value: fields[lang].value, lang: lang, __RequestVerificationToken: token });
                const response = await fetch(list.dataset.saveUrl, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                    body: body
                });
                const result = await response.json().catch(() => ({ success: false }));
                if (!response.ok || !result.success) throw new Error(result.message || t('Kaydedilemedi.', 'Speichern fehlgeschlagen.'));
                // Reflect the new text in the row and in the next edit
                const value = fields[lang].value;
                row.querySelector('[data-text="' + lang + '"]').textContent = value || '—';
                row.querySelector('[data-edit-text]').dataset[lang === 'tr-TR' ? 'tr' : 'de'] = value;
            }
            row.dataset.search = row.dataset.key + ' ' + fields['tr-TR'].value + ' ' + fields['de-DE'].value;
            dialog.close();
            window.admToast(t('Metin kaydedildi. Sitede hemen görünür.', 'Der Text wurde gespeichert und ist sofort online.'), 'success');
        } catch (err) {
            window.admToast(err.message || t('Kaydedilemedi.', 'Speichern fehlgeschlagen.'), 'error');
        } finally {
            save.classList.remove('is-loading');
            save.disabled = false;
        }
    });
})();
