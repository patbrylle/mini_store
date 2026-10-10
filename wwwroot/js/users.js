(function () {
    'use strict';

    // =====================================================
    // SEARCH + ROLE/STATUS FILTER (one function, so they never fight)
    // =====================================================
    var rows = Array.prototype.slice.call(document.querySelectorAll('#userTable tbody tr[data-role]'));
    var chips = document.querySelectorAll('#userFilters .um-chip');
    var search = document.getElementById('userSearch');
    var noResults = document.getElementById('userNoResults');
    var filter = 'all';

    function apply() {
        var q = search ? search.value.trim().toLowerCase() : '';
        var visible = 0;

        rows.forEach(function (row) {
            var okFilter =
                filter === 'all' ||
                (filter === 'disabled' ? row.dataset.active === 'false' : row.dataset.role === filter);

            var okSearch = !q || row.dataset.search.indexOf(q) !== -1;
            var show = okFilter && okSearch;

            row.hidden = !show;
            if (show) visible++;
        });

        if (noResults) noResults.hidden = !rows.length || visible !== 0;
    }

    chips.forEach(function (chip) {
        chip.addEventListener('click', function () {
            filter = chip.dataset.filter;

            chips.forEach(function (c) {
                var on = c === chip;
                c.classList.toggle('active', on);
                c.setAttribute('aria-pressed', on ? 'true' : 'false');
            });

            apply();
        });
    });

    if (search) search.addEventListener('input', apply);


    // =====================================================
    // DELETE MODAL
    // =====================================================
    var deleteModal = document.getElementById('deleteUserModal');
    if (deleteModal) {
        deleteModal.addEventListener('show.bs.modal', function (e) {
            var btn = e.relatedTarget;
            if (!btn) return;

            document.getElementById('deleteUserId').value = btn.getAttribute('data-id');
            document.getElementById('deleteUserEmail').textContent = btn.getAttribute('data-email');
        });
    }


    // =====================================================
    // RESET PASSWORD MODAL
    // =====================================================
    var resetModal = document.getElementById('resetUserModal');
    if (resetModal) {
        var resetForm = document.getElementById('resetUserForm');
        var newPw = document.getElementById('resetNewPassword');
        var confirmPw = document.getElementById('resetConfirmPassword');
        var resetErr = document.getElementById('resetError');
        var resetSubmit = document.getElementById('resetSubmit');
        var minLen = parseInt(resetForm.dataset.min || '6', 10);

        var clearResetError = function () {
            resetErr.hidden = true;
            newPw.classList.remove('is-invalid');
            confirmPw.classList.remove('is-invalid');
        };

        var showResetError = function (msg, input) {
            clearResetError();
            resetErr.textContent = msg;
            resetErr.hidden = false;
            input.classList.add('is-invalid');
            input.focus();
        };

        var resetToggles = function () {
            resetModal.querySelectorAll('.um-pw-toggle').forEach(function (btn) {
                var input = btn.parentElement.querySelector('input');
                input.type = 'password';
                btn.setAttribute('aria-pressed', 'false');
                btn.setAttribute('aria-label', 'Show password');
                btn.querySelector('i').className = 'bi bi-eye';
            });
        };

        resetModal.addEventListener('show.bs.modal', function (e) {
            var btn = e.relatedTarget;
            if (!btn) return;

            resetForm.reset();
            document.getElementById('resetUserId').value = btn.getAttribute('data-id');
            document.getElementById('resetUserEmail').textContent = btn.getAttribute('data-email');
            clearResetError();
            resetToggles();
            resetSubmit.disabled = false;
        });

        resetModal.addEventListener('shown.bs.modal', function () {
            newPw.focus();
        });

        // show / hide password
        resetModal.addEventListener('click', function (e) {
            var toggle = e.target.closest('.um-pw-toggle');
            if (!toggle) return;

            var input = toggle.parentElement.querySelector('input');
            var show = input.type === 'password';

            input.type = show ? 'text' : 'password';
            toggle.setAttribute('aria-pressed', show ? 'true' : 'false');
            toggle.setAttribute('aria-label', show ? 'Hide password' : 'Show password');
            toggle.querySelector('i').className = show ? 'bi bi-eye-slash' : 'bi bi-eye';
            input.focus();
        });

        newPw.addEventListener('input', clearResetError);
        confirmPw.addEventListener('input', clearResetError);

        resetForm.addEventListener('submit', function (e) {
            if (!newPw.value) { e.preventDefault(); return showResetError('Please enter a new password.', newPw); }
            if (newPw.value.length < minLen) { e.preventDefault(); return showResetError('Password must be at least ' + minLen + ' characters.', newPw); }
            if (newPw.value !== confirmPw.value) { e.preventDefault(); return showResetError('Passwords do not match.', confirmPw); }

            resetSubmit.disabled = true;
        });
    }


    // =====================================================
    // EDIT USER MODAL
    // =====================================================
    var editModal = document.getElementById('editUserModal');
    if (editModal) {
        var editForm = document.getElementById('editUserForm');
        var editEmail = document.getElementById('editEmail');
        var editErr = document.getElementById('editError');
        var editSubmit = document.getElementById('editSubmit');
        var roleStaff = document.getElementById('editRoleStaff');
        var roleAdmin = document.getElementById('editRoleAdmin');
        var editAdminNote = document.getElementById('editAdminNote');

        var clearEditError = function () {
            editErr.hidden = true;
        };

        var showEditError = function (msg, input) {
            editErr.textContent = msg;
            editErr.hidden = false;
            if (input) input.focus();
        };

        var syncAdminNote = function () {
            editAdminNote.hidden = !roleAdmin.checked;
        };

        editModal.addEventListener('show.bs.modal', function (e) {
            var btn = e.relatedTarget;
            if (!btn) return;

            var isAdmin = (btn.getAttribute('data-role') || '').toLowerCase() === 'admin';

            document.getElementById('editUserId').value = btn.getAttribute('data-id');
            editEmail.value = btn.getAttribute('data-email') || '';
            roleAdmin.checked = isAdmin;
            roleStaff.checked = !isAdmin;

            clearEditError();
            syncAdminNote();
            editSubmit.disabled = false;
        });

        editModal.addEventListener('shown.bs.modal', function () {
            editEmail.focus();
        });

        roleStaff.addEventListener('change', syncAdminNote);
        roleAdmin.addEventListener('change', syncAdminNote);
        editEmail.addEventListener('input', clearEditError);

        editForm.addEventListener('submit', function (e) {
            var email = editEmail.value.trim();

            if (!email) { e.preventDefault(); return showEditError('Please enter an email address.', editEmail); }
            if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) { e.preventDefault(); return showEditError('Please enter a valid email address.', editEmail); }

            editEmail.value = email;
            editSubmit.disabled = true;
        });
    }
})();