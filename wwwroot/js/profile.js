(function () {
    'use strict';

    // ---------- show / hide password ----------
    document.querySelectorAll('[data-toggle-password]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var input = btn.closest('.field-wrap').querySelector('input');
            var show = input.type === 'password';

            input.type = show ? 'text' : 'password';
            btn.classList.toggle('active', show);
            btn.setAttribute('aria-label', show ? 'Hide password' : 'Show password');
            btn.querySelector('i').className = show ? 'bi bi-eye-slash' : 'bi bi-eye';
        });
    });

    // ---------- strength meter + rules + match ----------
    var pw = document.querySelector('[data-pw-new]');
    var confirm = document.querySelector('[data-pw-confirm]');
    if (!pw || !confirm) return;

    var meter = document.getElementById('pwMeter');
    var meterText = document.getElementById('pwMeterText');
    var matchText = document.getElementById('pwMatch');
    var rules = document.querySelectorAll('#pwRules li');

    // same rules as Program.cs (length 8, digit, lower, upper, symbol)
    var tests = {
        length: function (v) { return v.length >= 8; },
        upper:  function (v) { return /[A-Z]/.test(v); },
        lower:  function (v) { return /[a-z]/.test(v); },
        digit:  function (v) { return /\d/.test(v); },
        symbol: function (v) { return /[^A-Za-z0-9]/.test(v); }
    };

    var labels = ['Enter a password', 'Weak', 'Fair', 'Good', 'Strong'];

    function update() {
        var v = pw.value;
        var passed = 0;

        rules.forEach(function (li) {
            var ok = tests[li.getAttribute('data-rule')](v);
            li.classList.toggle('ok', ok);
            li.querySelector('i').className = ok ? 'bi bi-check-circle-fill' : 'bi bi-circle';
            if (ok) passed++;
        });

        var level = v.length === 0 ? 0 : passed <= 2 ? 1 : passed === 3 ? 2 : passed === 4 ? 3 : 4;
        meter.setAttribute('data-level', level);
        meterText.textContent = labels[level];

        if (confirm.value.length === 0) {
            matchText.textContent = '';
            matchText.className = 'match-text';
        } else if (confirm.value === v) {
            matchText.textContent = 'Passwords match';
            matchText.className = 'match-text ok';
        } else {
            matchText.textContent = 'Passwords do not match';
            matchText.className = 'match-text bad';
        }
    }

    pw.addEventListener('input', update);
    confirm.addEventListener('input', update);
    update();
})();