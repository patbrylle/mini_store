(function () {
    const modal = document.getElementById('umResetModal');
    if (!modal) return;

    const form = document.getElementById('umResetForm');
    const idInput = document.getElementById('umResetId');
    const emailLabel = document.getElementById('umResetEmail');
    const pw = document.getElementById('umNewPassword');
    const cf = document.getElementById('umConfirmPassword');
    const errBox = document.getElementById('umResetError');
    const submitBtn = document.getElementById('umResetSubmit');
    const minLen = parseInt(form.dataset.min || '6', 10);
    let lastFocus = null;

    function showError(msg, input) {
        errBox.textContent = msg;
        errBox.hidden = false;
        [pw, cf].forEach(i => i.classList.remove('is-invalid'));
        if (input) { input.classList.add('is-invalid'); input.focus(); }
    }

    function clearError() {
        errBox.hidden = true;
        [pw, cf].forEach(i => i.classList.remove('is-invalid'));
    }

    function resetToggles() {
        modal.querySelectorAll('.um-pw-toggle').forEach(btn => {
            btn.classList.remove('is-visible');
            btn.setAttribute('aria-pressed', 'false');
            btn.setAttribute('aria-label', 'Show password');
            btn.parentElement.querySelector('input').type = 'password';
        });
    }

    function openModal(id, email) {
        lastFocus = document.activeElement;
        idInput.value = id;
        emailLabel.textContent = email;
        form.reset();
        idInput.value = id;
        clearError();
        resetToggles();
        submitBtn.disabled = false;
        submitBtn.textContent = 'Reset password';
        modal.classList.add('is-open');
        modal.setAttribute('aria-hidden', 'false');
        document.body.style.overflow = 'hidden';
        setTimeout(() => pw.focus(), 50);
    }

    function closeModal() {
        modal.classList.remove('is-open');
        modal.setAttribute('aria-hidden', 'true');
        document.body.style.overflow = '';
        if (lastFocus) lastFocus.focus();
    }

    document.addEventListener('click', function (e) {
        const trigger = e.target.closest('[data-um-reset]');
        if (trigger) {
            openModal(trigger.dataset.userId, trigger.dataset.userEmail);
            return;
        }

        if (e.target.closest('[data-um-close]')) {
            closeModal();
            return;
        }

        const toggle = e.target.closest('.um-pw-toggle');
        if (toggle && modal.contains(toggle)) {
            const input = toggle.parentElement.querySelector('input');
            const show = input.type === 'password';
            input.type = show ? 'text' : 'password';
            toggle.classList.toggle('is-visible', show);
            toggle.setAttribute('aria-pressed', String(show));
            toggle.setAttribute('aria-label', show ? 'Hide password' : 'Show password');
            input.focus();
        }
    });

    // Close when clicking the dark backdrop (not when dragging from inside the card)
    modal.addEventListener('mousedown', function (e) {
        if (e.target === modal) closeModal();
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && modal.classList.contains('is-open')) closeModal();
    });

    [pw, cf].forEach(i => i.addEventListener('input', clearError));

    form.addEventListener('submit', function (e) {
        if (!pw.value) { e.preventDefault(); return showError('Please enter a new password.', pw); }
        if (pw.value.length < minLen) { e.preventDefault(); return showError('Password must be at least ' + minLen + ' characters.', pw); }
        if (pw.value !== cf.value) { e.preventDefault(); return showError('Passwords do not match.', cf); }

        submitBtn.disabled = true;
        submitBtn.textContent = 'Resetting...';
    });
})();