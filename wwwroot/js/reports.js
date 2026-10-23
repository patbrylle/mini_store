(function () {
    "use strict";

    // ---------- print buttons ----------
    document.querySelectorAll("[data-rp-print]").forEach(function (btn) {
        btn.addEventListener("click", function () { window.print(); });
    });

    // ---------- custom date range panel ----------
    var toggle = document.getElementById("rpCustomToggle");
    var panel = document.getElementById("rpCustom");

    if (toggle && panel) {
        toggle.addEventListener("click", function () {
            panel.hidden = !panel.hidden;
            toggle.classList.toggle("active", !panel.hidden);
        });
    }

    // ---------- search inside the table ----------
    var input = document.getElementById("rpSearch");
    var table = document.getElementById("rpTable");

    if (input && table) {
        var rows = Array.from(table.tBodies[0].rows);
        var texts = rows.map(function (r) { return r.textContent.toLowerCase(); });
        var total = rows.length;
        var count = document.getElementById("rpCount");
        var none = document.getElementById("rpNoResults");
        var foot = table.tFoot;

        input.addEventListener("input", function () {
            var q = input.value.toLowerCase().trim();
            var shown = 0;

            rows.forEach(function (row, i) {
                var ok = !q || texts[i].indexOf(q) !== -1;
                row.hidden = !ok;
                if (ok) shown++;
            });

            if (count) {
                count.textContent = (shown === total
                    ? total.toLocaleString()
                    : shown.toLocaleString() + " of " + total.toLocaleString()) + " records";
            }

            if (none) none.hidden = shown > 0;

            // the totals row covers every record, so hide it while searching
            if (foot) foot.hidden = !!q;
        });
    }

    // ---------- overview charts ----------
    var dataEl = document.getElementById("rpData");
    if (!dataEl || typeof Chart === "undefined") return;

    var data = JSON.parse(dataEl.textContent);
    var css = getComputedStyle(document.documentElement);

    function token(name, fallback) {
        return css.getPropertyValue(name).trim() || fallback;
    }

    var C = {
        success: token("--color-success", "#3c9461"),
        danger: token("--color-danger", "#c0392b"),
        gold: token("--color-gold", "#d49a21"),
        primary: token("--color-primary", "#e4772e"),
        muted: token("--color-text-muted", "#777777"),
        grid: token("--color-border", "#eeeeee")
    };

    Chart.defaults.font.family = "'Plus Jakarta Sans', system-ui, sans-serif";
    Chart.defaults.color = C.muted;

    function peso(v) {
        return "\u20B1" + Number(v).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function doughnut(id, labels, values, colors, format) {
        var el = document.getElementById(id);
        if (!el) return;

        // nothing to show: draw a grey ring instead of a fake "100% paid"
        var empty = values.every(function (v) { return !v; });
        if (empty) el.parentElement.classList.add("is-empty");

        new Chart(el, {
            type: "doughnut",
            data: {
                labels: empty ? ["No data"] : labels,
                datasets: [{
                    data: empty ? [1] : values,
                    backgroundColor: empty ? [C.grid] : colors,
                    borderWidth: 0
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: "68%",
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        enabled: !empty,
                        callbacks: {
                            label: function (ctx) { return ctx.label + ": " + format(ctx.parsed); }
                        }
                    }
                }
            }
        });
    }

    // sales, last 7 days
    var salesEl = document.getElementById("rpSalesChart");
    if (salesEl) {
        new Chart(salesEl, {
            type: "bar",
            data: {
                labels: data.last7.labels,
                datasets: [{
                    data: data.last7.values,
                    backgroundColor: C.primary,
                    borderRadius: 6,
                    maxBarThickness: 46
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: { callbacks: { label: function (ctx) { return "Sales: " + peso(ctx.parsed.y); } } }
                },
                scales: {
                    x: { grid: { display: false } },
                    y: {
                        beginAtZero: true,
                        grid: { color: C.grid },
                        ticks: { callback: function (v) { return "\u20B1" + Number(v).toLocaleString(); } }
                    }
                }
            }
        });
    }

    doughnut("rpPaymentChart",
        ["Paid", "Outstanding"],
        [data.payment.paid, data.payment.outstanding],
        [C.success, C.danger],
        peso);

    doughnut("rpInventoryChart",
        ["Healthy", "Low stock", "Out of stock"],
        [data.inventory.healthy, data.inventory.low, data.inventory.outOfStock],
        [C.success, C.gold, C.danger],
        function (v) { return v + " products"; });
})();