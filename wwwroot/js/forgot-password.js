(function () {
    'use strict';

    var modal = document.getElementById('forgotPasswordModal');
    if (!modal) return;

    var token = modal.querySelector('input[name="__RequestVerificationToken"]').value;
    var step1 = document.getElementById('fpStep1');
    var step2 = document.getElementById('fpStep2');
    var done = document.getElementById('fpDone');
    var errBox = document.getElementById('fpError');
    var demoBox = document.getElementById('fpDemo');
    var sub = document.getElementById('fpSub');
    var email = document.getElementById('fpEmail');
    var code = document.getElementById('fpCode');
    var pw = document.getElementById('fpPw');
    var pw2 = document.getElementById('fpPw2');
    var resend = document.getElementById('fpResend');
    var timer = null;

    function post(url, data) {
        var body = new URLSearchParams(data);
        body.append('__RequestVerificationToken', token);

        return fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'X-Requested-With': 'XMLHttpRequest' },
            body: body
        }).then(function (res) {
            return res.json().catch(function () { return {}; }).then(function (json) {
                return { ok: res.ok, data: json };
            });
        });
    }

    function showError(msg) { errBox.textContent = msg; errBox.hidden = false; }
    function clearError() { errBox.hidden = true; }

    function busy(form, on) {
        var b = form.querySelector('button[type="submit"]');
        if (b) b.disabled = on;
    }

    function startCountdown() {
        var left = 60;
        clearInterval(timer);
        resend.disabled = true;
        resend.textContent = 'Resend code in ' + left + 's';

        timer = setInterval(function () {
            left--;
            if (left <= 0) {
                clearInterval(timer);
                resend.disabled = false;
                resend.textContent = 'Resend code';
            } else {
                resend.textContent = 'Resend code in ' + left + 's';
            }
        }, 1000);
    }

    function sendCode(isResend) {
        clearError();

        var value = email.value.trim();
        if (!value) { showError('Enter your email address.'); email.focus(); return Promise.resolve(); }

        busy(step1, true);
        return post(modal.dataset.sendUrl, { Email: value }).then(function (r) {
            busy(step1, false);
            if (!r.ok) { showError(r.data.error || 'Something went wrong. Please try again.'); return; }

            // demo mode (no SMTP, Development only): the server hands the code back
            if (r.data.demoCode) {
                demoBox.textContent = 'Demo mode, email is not configured. Your code is ' + r.data.demoCode + '.';
                demoBox.hidden = false;
            } else {
                demoBox.hidden = true;
            }

            document.getElementById('fpSentTo').textContent = value;
            step1.hidden = true;
            step2.hidden = false;
            sub.textContent = 'Enter the code and choose a new password.';
            startCountdown();
            if (!isResend) code.focus();
        }).catch(function () { busy(step1, false); showError('Network error. Please try again.'); });
    }

    step1.addEventListener('submit', function (e) { e.preventDefault(); sendCode(false); });
    resend.addEventListener('click', function () { sendCode(true); });

    step2.addEventListener('submit', function (e) {
        e.preventDefault();
        clearError();

        if (!/^\d{6}$/.test(code.value.trim())) { showError('Enter the 6-digit code.'); code.focus(); return; }
        if (pw.value.length < 8) { showError('Password must be at least 8 characters.'); pw.focus(); return; }
        if (pw.value !== pw2.value) { showError('Passwords do not match.'); pw2.focus(); return; }

        busy(step2, true);
        post(modal.dataset.resetUrl, {
            Email: email.value.trim(),
            Code: code.value.trim(),
            NewPassword: pw.value,
            ConfirmPassword: pw2.value
        }).then(function (r) {
            busy(step2, false);
            if (!r.ok) { showError(r.data.error || 'Could not reset the password.'); return; }

            step2.hidden = true;
            demoBox.hidden = true;
            done.hidden = false;
            sub.textContent = '';
            clearInterval(timer);
        }).catch(function () { busy(step2, false); showError('Network error. Please try again.'); });
    });

    // show / hide password
    modal.addEventListener('click', function (e) {
        var eye = e.target.closest('.fp-eye');
        if (!eye) return;
        var input = eye.parentElement.querySelector('input');
        var show = input.type === 'password';
        input.type = show ? 'text' : 'password';
        eye.querySelector('i').className = show ? 'bi bi-eye-slash' : 'bi bi-eye';
        eye.setAttribute('aria-label', show ? 'Hide password' : 'Show password');
    });

    // always start clean
    modal.addEventListener('hidden.bs.modal', function () {
        clearInterval(timer);
        [email, code, pw, pw2].forEach(function (i) { i.value = ''; });
        step1.hidden = false; step2.hidden = true; done.hidden = true;
        demoBox.hidden = true; clearError();
        sub.textContent = "We'll email you a 6-digit code to confirm it's you.";
    });
})();