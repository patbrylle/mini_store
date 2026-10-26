(function () {
    'use strict';

    document.querySelectorAll('[data-portal-filter]').forEach(function (root) {
        var q = root.querySelector('[data-filter-q]');
        var cat = root.querySelector('[data-filter-cat]');
        var empty = root.querySelector('[data-filter-empty]');
        var items = Array.prototype.slice.call(root.querySelectorAll('[data-filter-item]'));
        if (!items.length) return;

        function apply() {
            var term = q ? q.value.trim().toLowerCase() : '';
            var c = cat ? cat.value.toLowerCase() : '';
            var shown = 0;

            items.forEach(function (el) {
                var ok = (!term || (el.dataset.search || '').indexOf(term) !== -1) &&
                         (!c || (el.dataset.category || '').toLowerCase() === c);
                el.hidden = !ok;
                if (ok) shown++;
            });

            if (empty) empty.hidden = shown !== 0;
        }

        if (q) q.addEventListener('input', apply);
        if (cat) cat.addEventListener('change', apply);
    });
})();
(function () {
    'use strict';

    var modalEl = document.getElementById('ptReceiptModal');
    if (!modalEl || !window.bootstrap) return;

    var body = document.getElementById('ptReceiptBody');
    var baseUrl = modalEl.dataset.receiptUrl;
    var modal = bootstrap.Modal.getOrCreateInstance(modalEl);

    document.addEventListener('click', async function (e) {
        // print button inside the receipt
        var print = e.target.closest('[data-tx-action="print"]');
        if (print && modalEl.contains(print)) { window.print(); return; }

        var btn = e.target.closest('[data-receipt-sale]');
        if (!btn) return;
        e.preventDefault();

        body.innerHTML = '<div class="tx-loading"><span class="spinner-border spinner-border-sm"></span> Loading…</div>';
        modal.show();

        var qs = '?id=' + encodeURIComponent(btn.dataset.receiptSale);
        if (btn.dataset.receiptPayment) qs += '&paymentId=' + encodeURIComponent(btn.dataset.receiptPayment);

        try {
            var res = await fetch(baseUrl + qs, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            if (!res.ok) throw new Error('Could not load the receipt.');
            body.innerHTML = await res.text();
        } catch (err) {
            body.innerHTML =
                '<div class="tx-loading">' + err.message + '</div>' +
                '<div class="modal-footer tx-footer"><button type="button" class="tx-btn tx-btn-primary" data-bs-dismiss="modal">Close</button></div>';
        }
    });
})();