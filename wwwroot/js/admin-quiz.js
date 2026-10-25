// ============================================================================
// admin-quiz.js — the quiz form (Views/Admin/_QuizForm.cshtml).
// Add / remove / reorder questions and answers, keep the posted names
// (Questions[i].Answers[j].…) and the A, B, C letters in order, and check
// every question before saving: text, at least two answers, one marked
// correct. Completely empty question cards are dropped instead of refused.
// ============================================================================
(function () {
    'use strict';

    const form = document.getElementById('quizForm');
    if (!form) return;
    const de = document.body.dataset.lang === 'de';
    const t = (tr, deText) => (de ? deText : tr);
    const list = form.querySelector('[data-questions]');
    const questionTemplate = document.getElementById('quiz-question-template');
    const answerTemplate = document.getElementById('quiz-answer-template');
    const letters = 'ABCDEFGHIJ';
    const dirty = () => window.admMarkDirty?.(form);

    function renumber() {
        const cards = Array.from(list.querySelectorAll('[data-question]'));
        cards.forEach((card, q) => {
            card.querySelector('[data-qnum]').textContent = q + 1;
            card.querySelectorAll('[data-qfield]').forEach((el) => { el.name = 'Questions[' + q + '].' + el.dataset.qfield; });
            card.querySelectorAll('[data-answer]').forEach((row, a) => {
                const letter = letters[Math.min(a, letters.length - 1)];
                row.querySelector('[data-letter]').textContent = letter;
                row.querySelectorAll('[data-afield]').forEach((el) => { el.name = 'Questions[' + q + '].Answers[' + a + '].' + el.dataset.afield; });
                const text = row.querySelector('input[type="text"]');
                text.placeholder = t('Cevap ', 'Antwort ') + letter;
                text.setAttribute('aria-label', t('Soru ', 'Frage ') + (q + 1) + ', ' + t('cevap ', 'Antwort ') + letter);
            });
            card.querySelector('[data-q-up]').disabled = q === 0;
            card.querySelector('[data-q-down]').disabled = q === cards.length - 1;
        });
        const count = form.querySelector('[data-question-count]');
        if (count) count.textContent = cards.length;
    }

    function addAnswer(card, focus) {
        const answers = card.querySelector('[data-answers]');
        if (answers.children.length >= letters.length) return;
        answers.appendChild(answerTemplate.content.firstElementChild.cloneNode(true));
        renumber();
        if (focus) answers.lastElementChild.querySelector('input[type="text"]').focus();
    }

    function addQuestion() {
        const card = questionTemplate.content.firstElementChild.cloneNode(true);
        list.appendChild(card);
        for (let i = 0; i < 4; i++) addAnswer(card, false);
        renumber();
        card.scrollIntoView({ behavior: 'smooth', block: 'center' });
        card.querySelector('textarea').focus({ preventScroll: true });
        dirty();
    }

    const isBlank = (card) =>
        !card.querySelector('[data-qfield="QuestionText"]').value.trim() &&
        Array.from(card.querySelectorAll('[data-afield="AnswerText"]')).every((i) => !i.value.trim()) &&
        Array.from(card.querySelectorAll('input[type="file"]')).every((i) => !i.files.length) &&
        !card.querySelector('[data-qfield="VideoUrl"]').value.trim();

    form.querySelector('[data-q-add]').addEventListener('click', addQuestion);

    list.addEventListener('click', (e) => {
        const card = e.target.closest('[data-question]');
        if (!card) return;
        if (e.target.closest('[data-a-add]')) { addAnswer(card, true); dirty(); return; }
        const removeAnswer = e.target.closest('[data-a-remove]');
        if (removeAnswer) {
            const answers = card.querySelector('[data-answers]');
            const row = removeAnswer.closest('[data-answer]');
            if (answers.children.length <= 2) {
                row.querySelector('input[type="text"]').value = '';
                row.querySelector('[data-correct]').checked = false;
                row.classList.remove('is-correct');
            } else {
                row.remove();
                renumber();
            }
            dirty();
            return;
        }
        if (e.target.closest('[data-q-up]') && card.previousElementSibling) { list.insertBefore(card, card.previousElementSibling); renumber(); dirty(); return; }
        if (e.target.closest('[data-q-down]') && card.nextElementSibling) { list.insertBefore(card.nextElementSibling, card); renumber(); dirty(); return; }
        if (e.target.closest('[data-q-remove]')) {
            const remove = () => {
                card.remove();
                if (!list.children.length) addQuestion();
                renumber();
                dirty();
            };
            if (isBlank(card)) { remove(); return; }
            const saved = parseInt(card.querySelector('[data-qfield="QuestionId"]').value || '0', 10) > 0;
            window.admConfirm({
                title: t('Bu soru kaldırılsın mı?', 'Diese Frage entfernen?'),
                text: t('Soru ' + card.querySelector('[data-qnum]').textContent + ' ve cevapları listeden kaldırılacak.', 'Frage ' + card.querySelector('[data-qnum]').textContent + ' und ihre Antworten werden entfernt.'),
                note: saved ? t('Soru, siz sınavı kaydettiğinizde kalıcı olarak silinir. Vazgeçerseniz sayfadan kaydetmeden çıkın.', 'Die Frage wird beim Speichern der Prüfung endgültig gelöscht. Verlassen Sie sonst die Seite ohne zu speichern.') : '',
                ok: t('Evet, kaldır', 'Ja, entfernen')
            }).then(remove);
        }
    });

    list.addEventListener('change', (e) => {
        if (e.target.matches('[data-correct]')) e.target.closest('[data-answer]').classList.toggle('is-correct', e.target.checked);
    });

    // Teacher: the list is off while "no teacher name" is on
    const anonymous = form.querySelector('[data-anonymous]');
    const teacher = form.querySelector('[data-teacher-select]');
    const syncTeacher = () => { if (anonymous && teacher) teacher.disabled = anonymous.checked; };
    anonymous?.addEventListener('change', syncTeacher);
    syncTeacher();

    // Word chips get a filter box once there are many
    const words = form.querySelector('[data-word-choices]');
    if (words && words.children.length > 15) {
        const filter = document.createElement('input');
        filter.type = 'search';
        filter.className = 'adm-input';
        filter.placeholder = t('Kelime ara…', 'Wort suchen…');
        filter.setAttribute('aria-label', filter.placeholder);
        words.before(filter);
        filter.addEventListener('input', () => {
            const term = filter.value.trim().toLocaleLowerCase();
            Array.from(words.children).forEach((chip) => { chip.hidden = term && !chip.textContent.toLocaleLowerCase().includes(term); });
        });
    }

    // Before saving: drop empty cards and empty answer rows, then check what is left
    form.addEventListener('submit', (e) => {
        list.querySelectorAll('[data-question]').forEach((card) => { if (isBlank(card) && list.children.length > 1) card.remove(); });
        let firstBad = null;
        let message = '';
        list.querySelectorAll('[data-question]').forEach((card) => {
            card.classList.remove('has-error');
            if (isBlank(card)) return;
            const rows = Array.from(card.querySelectorAll('[data-answer]'));
            const filled = rows.filter((r) => r.querySelector('input[type="text"]').value.trim());
            let problem = '';
            if (!card.querySelector('[data-qfield="QuestionText"]').value.trim()) problem = t('soru metni boş', 'der Fragetext fehlt');
            else if (filled.length < 2) problem = t('en az iki cevap gerekli', 'mindestens zwei Antworten nötig');
            else if (!filled.some((r) => r.querySelector('[data-correct]').checked)) problem = t('doğru cevap işaretlenmemiş', 'keine richtige Antwort markiert');
            if (problem) {
                card.classList.add('has-error');
                if (!firstBad) {
                    firstBad = card;
                    message = t('Soru ' + card.querySelector('[data-qnum]').textContent + ': ' + problem + '.', 'Frage ' + card.querySelector('[data-qnum]').textContent + ': ' + problem + '.');
                }
            } else {
                // Empty answer rows are not sent (the server would skip them anyway)
                rows.filter((r) => !r.querySelector('input[type="text"]').value.trim() && rows.length > 2).forEach((r) => r.remove());
            }
        });
        renumber();
        if (firstBad) {
            e.preventDefault();
            e.stopImmediatePropagation();
            window.admToast(message, 'error');
            firstBad.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }
    }, true);

    renumber();
})();
