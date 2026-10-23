(function () {
    'use strict';

    // ---------- search + filter ----------
    var rows = Array.prototype.slice.call(document.querySelectorAll('#userTable tbody tr[data-role]'));
    var chips = document.querySelectorAll('#userFilters .um-chip');
    var search = document.getElementById('userSearch');
    var noResults = document.getElementById('userNoResults');
    var filter = 'all';

    function apply() {
        var q = search ? search.value.trim().toLowerCase() : '';
        var visible = 0;

        rows.forEach(function (row) {
            var okFilter = filter === 'all' ||
                (filter === 'disabled' ? row.dataset.status === 'disabled' : row.dataset.role === filter);
            var show = okFilter && (!q || row.dataset.search.indexOf(q) !== -1);

            row.hidden = !show;
            if (show) visible++;
        });

        if (noResults) noResults.hidden = !rows.length || visible !== 0;
    }

    chips.forEach(function (chip) {
        chip.addEventListener('click', function () {
            filter = chip.dataset.filter;
            chips.forEach(function (c) {
                c.classList.toggle('active', c === chip);
                c.setAttribute('aria-pressed', c === chip ? 'true' : 'false');
            });
            apply();
        });
    });

    if (search) search.addEventListener('input', apply);

    // ---------- delete modal ----------
    var del = document.getElementById('deleteUserModal');
    if (del) {
        del.addEventListener('show.bs.modal', function (e) {
            var b = e.relatedTarget;
            if (!b) return;

            var customer = b.dataset.kind === 'customer';
            document.getElementById('deleteUserId').value = b.dataset.id;
            document.getElementById('deleteUserName').textContent = b.dataset.name;
            document.getElementById('deleteUserLabel').textContent =
                customer ? 'Delete customer login?' : 'Delete staff account?';
            document.getElementById('deleteUserNote').textContent = customer
                ? 'Only the login is removed. Their customer record, purchases and credit history are kept.'
                : 'To only block access, use Disable instead.';
        });
    }
})();