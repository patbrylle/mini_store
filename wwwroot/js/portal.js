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