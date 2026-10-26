// ============================================================================
// dictionary.js — the dictionary page (Views/Home/Words.cshtml).
// Progressive enhancement only: the search works without it.
//  - ä ö ü ß buttons insert the letter at the cursor (Turkish keyboards lack ä and ß)
//  - "Listen" reads the German word aloud with the browser's own voice (Web Speech API)
// ============================================================================
(function () {
    'use strict';

    var input = document.getElementById('dict-q');

    // ------------------------------------------------------------------------
    // German letters
    // ------------------------------------------------------------------------
    var keys = document.querySelector('[data-dict-keys]');
    if (keys && input) {
        keys.hidden = false;
        // Keep the cursor position: a button press would otherwise move focus first
        keys.addEventListener('mousedown', function (e) { if (e.target.closest('[data-letter]')) e.preventDefault(); });
        keys.addEventListener('click', function (e) {
            var key = e.target.closest('[data-letter]');
            if (!key) return;
            var letter = key.getAttribute('data-letter');
            var start = input.selectionStart == null ? input.value.length : input.selectionStart;
            var end = input.selectionEnd == null ? start : input.selectionEnd;
            input.setRangeText(letter, start, end, 'end');
            input.focus();
            input.dispatchEvent(new Event('input', { bubbles: true }));
        });
    }

    // Put the cursor at the end of a pre-filled search, ready to edit
    if (input && input.value && window.matchMedia('(hover: hover)').matches) {
        input.focus({ preventScroll: true });
        input.setSelectionRange(input.value.length, input.value.length);
    }

    // ------------------------------------------------------------------------
    // Listen
    // ------------------------------------------------------------------------
    if (!('speechSynthesis' in window) || typeof SpeechSynthesisUtterance === 'undefined') return;

    function voiceFor(lang) {
        var voices = window.speechSynthesis.getVoices();
        var prefix = lang.slice(0, 2).toLowerCase();
        return voices.filter(function (v) { return v.lang && v.lang.toLowerCase().indexOf(prefix) === 0; })
            .sort(function (a, b) { return (b.localService ? 1 : 0) - (a.localService ? 1 : 0); })[0] || null;
    }

    function setup() {
        document.querySelectorAll('[data-speak]').forEach(function (button) {
            var lang = button.getAttribute('data-speak-lang') || 'de-DE';
            // Only offer it when the device actually has a voice for the language
            if (!voiceFor(lang)) return;
            button.hidden = false;
            if (button.dataset.ready) return;
            button.dataset.ready = '1';
            button.addEventListener('click', function () {
                window.speechSynthesis.cancel();
                var utterance = new SpeechSynthesisUtterance(button.getAttribute('data-speak'));
                utterance.lang = lang;
                utterance.voice = voiceFor(lang);
                utterance.rate = 0.85; // a little slower, for learners
                utterance.onstart = function () { button.classList.add('is-speaking'); };
                utterance.onend = utterance.onerror = function () { button.classList.remove('is-speaking'); };
                window.speechSynthesis.speak(utterance);
            });
        });
    }

    setup();
    // Chrome loads the voice list asynchronously
    if ('onvoiceschanged' in window.speechSynthesis) window.speechSynthesis.addEventListener('voiceschanged', setup);
})();
