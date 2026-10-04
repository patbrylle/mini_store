(function () {
    'use strict';

    var reduceMotion = window.matchMedia &&
        window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    /* =====================================================
       KPI COUNT-UP  (server already renders the final value,
       so the page is still correct if JS fails)
       ===================================================== */
    document.querySelectorAll('.count-up').forEach(function (el) {
        var target   = parseFloat(el.dataset.value) || 0;
        var decimals = parseInt(el.dataset.decimals || '0', 10);
        var prefix   = el.dataset.prefix || '';

        if (reduceMotion || target === 0) return;

        function format(n) {
            return prefix + n.toLocaleString('en-US', {
                minimumFractionDigits: decimals,
                maximumFractionDigits: decimals
            });
        }

        var duration = 1000;
        var start = null;

        function tick(ts) {
            if (start === null) start = ts;
            var p = Math.min((ts - start) / duration, 1);
            var eased = 1 - Math.pow(1 - p, 3);          // easeOutCubic
            el.textContent = format(target * eased);
            if (p < 1) {
                requestAnimationFrame(tick);
            } else {
                el.textContent = format(target);
            }
        }

        el.textContent = format(0);
        requestAnimationFrame(tick);
    });

    /* =====================================================
       CHARTS
       ===================================================== */
    if (typeof Chart === 'undefined') return;

    var styles = getComputedStyle(document.documentElement);
    function token(name, fallback) {
        var v = styles.getPropertyValue(name).trim();
        return v || fallback;
    }

    var C = {
        primary:   token('--color-primary',   '#E4772E'),
        dark:      token('--color-primary-dark', '#C9611E'),
        gold:      token('--color-gold',      '#E0A63A'),
        secondary: token('--color-secondary', '#B5451B'),
        success:   token('--color-success',   '#4C9A6A'),
        muted:     token('--color-text-muted', '#8A7A6D'),
        border:    token('--color-border',    '#EDE1D4'),
        tooltipBg: token('--color-sidebar-bg', '#2B211A')
    };

    Chart.defaults.font.family = "'Plus Jakarta Sans', -apple-system, 'Segoe UI', sans-serif";
    Chart.defaults.font.size = 12;
    Chart.defaults.color = C.muted;

    function peso(n) {
        return '₱' + Number(n).toLocaleString('en-PH', {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    function hasData(arr) {
        return Array.isArray(arr) && arr.some(function (x) { return Number(x) > 0; });
    }

    function escapeHtml(s) {
        return String(s).replace(/[&<>"']/g, function (ch) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[ch];
        });
    }

    function showEmpty(canvas, icon, message) {
        canvas.parentElement.innerHTML =
            '<div class="empty-state"><i class="bi ' + icon + '"></i><p>' + message + '</p></div>';
    }

    var tooltipBase = {
        backgroundColor: C.tooltipBg,
        padding: 12,
        cornerRadius: 10,
        titleFont: { weight: '700' },
        bodyFont: { weight: '600' }
    };

    /* ---------- SALES TREND (line) ---------- */
    var salesCanvas = document.getElementById('salesTrendChart');

    if (salesCanvas) {
        var salesLabels = window.dashboardSalesLabels || [];
        var salesData   = window.dashboardSalesData || [];

        if (!hasData(salesData)) {
            showEmpty(salesCanvas, 'bi-graph-up', 'No sales recorded this month yet.');
        } else {
            var sctx = salesCanvas.getContext('2d');
            var gradient = sctx.createLinearGradient(0, 0, 0, salesCanvas.parentElement.clientHeight || 320);
            gradient.addColorStop(0, C.primary + '50');
            gradient.addColorStop(1, C.primary + '00');

            new Chart(sctx, {
                type: 'line',
                data: {
                    labels: salesLabels,
                    datasets: [{
                        label: 'Sales',
                        data: salesData,
                        borderColor: C.primary,
                        backgroundColor: gradient,
                        borderWidth: 3,
                        fill: true,
                        tension: 0.4,
                        pointRadius: 0,
                        pointHoverRadius: 6,
                        pointHoverBackgroundColor: '#fff',
                        pointHoverBorderColor: C.primary,
                        pointHoverBorderWidth: 3
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    interaction: { mode: 'index', intersect: false },
                    animation: { duration: reduceMotion ? 0 : 1100, easing: 'easeOutQuart' },
                    plugins: {
                        legend: { display: false },
                        tooltip: Object.assign({}, tooltipBase, {
                            displayColors: false,
                            callbacks: {
                                label: function (c) { return peso(c.parsed.y); }
                            }
                        })
                    },
                    scales: {
                        x: {
                            grid: { display: false },
                            border: { display: false },
                            ticks: { maxRotation: 0, autoSkipPadding: 14 }
                        },
                        y: {
                            beginAtZero: true,
                            border: { display: false },
                            grid: { color: C.border, borderDash: [4, 4] },
                            ticks: {
                                callback: function (v) {
                                    return v >= 1000 ? '₱' + (v / 1000) + 'k' : '₱' + v;
                                }
                            }
                        }
                    }
                }
            });
        }
    }

    /* ---------- CATEGORY BREAKDOWN (doughnut) ---------- */
    var catCanvas = document.getElementById('categoryChart');

    if (catCanvas) {
        var catLabels = window.dashboardCategoryLabels || [];
        var catData   = window.dashboardCategoryData || [];
        var legend    = document.getElementById('categoryLegend');

        if (!hasData(catData)) {
            showEmpty(catCanvas, 'bi-pie-chart', 'No category data yet.');
            if (legend) legend.remove();
        } else {
            var palette = [C.primary, C.gold, C.secondary, '#F4A15F', C.success, '#8A7A6D', C.dark, '#F2C879'];

            function colorAt(i) { return palette[i % palette.length]; }

            var total = catData.reduce(function (a, b) { return a + Number(b); }, 0);

            new Chart(catCanvas.getContext('2d'), {
                type: 'doughnut',
                data: {
                    labels: catLabels,
                    datasets: [{
                        data: catData,
                        backgroundColor: catLabels.map(function (_, i) { return colorAt(i); }),
                        borderColor: '#fff',
                        borderWidth: 3,
                        hoverOffset: 8
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    cutout: '68%',
                    animation: { duration: reduceMotion ? 0 : 1000, easing: 'easeOutQuart' },
                    plugins: {
                        legend: { display: false },
                        tooltip: Object.assign({}, tooltipBase, {
                            callbacks: {
                                label: function (c) {
                                    var pct = total ? (c.parsed / total * 100).toFixed(1) : 0;
                                    return ' ' + c.label + ': ' + c.parsed.toLocaleString() + ' (' + pct + '%)';
                                }
                            }
                        })
                    }
                }
            });

            if (legend) {
                legend.innerHTML = catLabels.map(function (label, i) {
                    var pct = total ? Math.round(Number(catData[i]) / total * 100) : 0;
                    return '<div class="legend-item">' +
                        '<span class="legend-dot" style="background:' + colorAt(i) + '"></span>' +
                        '<span class="legend-name" title="' + escapeHtml(label) + '">' + escapeHtml(label) + '</span>' +
                        '<span class="legend-pct">' + pct + '%</span>' +
                        '</div>';
                }).join('');
            }
        }
    }
})();