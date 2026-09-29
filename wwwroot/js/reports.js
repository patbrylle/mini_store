(function () {
    "use strict";

    function initializeReportsCharts() {

        var data = window.reportsChartData || {};

        var COLOR_SUCCESS = "#3c9461";
        var COLOR_DANGER = "#c0392b";
        var COLOR_GOLD = "#d49a21";
        var COLOR_PRIMARY = "#e4772e";
        var COLOR_TRACK = "#f1f1f1";

        var baseDoughnutOptions = {
            responsive: true,
            maintainAspectRatio: false,
            cutout: "68%",
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    callbacks: {
                        label: function (ctx) {
                            return ctx.label + ": " + ctx.formattedValue;
                        }
                    }
                }
            }
        };

        // PAYMENT STATUS
        var paymentEl = document.getElementById("paymentStatusChart");

        if (paymentEl && data.payment) {

            var paid = Number(data.payment.paid) || 0;
            var outstanding = Number(data.payment.outstanding) || 0;

            new Chart(paymentEl, {
                type: "doughnut",
                data: {
                    labels: ["Paid", "Outstanding"],
                    datasets: [{
                        data: (paid === 0 && outstanding === 0)
                            ? [1, 0]
                            : [paid, outstanding],
                        backgroundColor: [
                            COLOR_SUCCESS,
                            COLOR_DANGER
                        ],
                        borderWidth: 0
                    }]
                },
                options: baseDoughnutOptions
            });
        }

        // INVENTORY STATUS
        var inventoryEl = document.getElementById("inventoryStatusChart");

        if (inventoryEl && data.inventory) {

            var healthy = Number(data.inventory.healthy) || 0;
            var low = Number(data.inventory.lowStock) || 0;
            var out = Number(data.inventory.outOfStock) || 0;

            new Chart(inventoryEl, {
                type: "doughnut",
                data: {
                    labels: [
                        "Healthy",
                        "Low Stock",
                        "Out of Stock"
                    ],
                    datasets: [{
                        data: (healthy === 0 && low === 0 && out === 0)
                            ? [1, 0, 0]
                            : [healthy, low, out],
                        backgroundColor: [
                            COLOR_SUCCESS,
                            COLOR_GOLD,
                            COLOR_DANGER
                        ],
                        borderWidth: 0
                    }]
                },
                options: baseDoughnutOptions
            });
        }

        // BEST-SELLING PRODUCTS
        var bestEl = document.getElementById("bestSellingChart");

        if (
            bestEl &&
            data.bestSelling &&
            data.bestSelling.labels &&
            data.bestSelling.labels.length > 0
        ) {

            new Chart(bestEl, {
                type: "bar",
                data: {
                    labels: data.bestSelling.labels,
                    datasets: [{
                        label: "Quantity Sold",
                        data: data.bestSelling.data,
                        backgroundColor: COLOR_PRIMARY,
                        borderRadius: 6,
                        maxBarThickness: 42
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,

                    plugins: {
                        legend: {
                            display: false
                        },
                        tooltip: {
                            callbacks: {
                                label: function (ctx) {
                                    return "Sold: " + ctx.formattedValue;
                                }
                            }
                        }
                    },

                    scales: {
                        x: {
                            grid: {
                                display: false
                            },
                            ticks: {
                                color: "#777",
                                font: {
                                    size: 12
                                }
                            }
                        },

                        y: {
                            beginAtZero: true,
                            ticks: {
                                precision: 0,
                                color: "#777"
                            },
                            grid: {
                                color: COLOR_TRACK
                            }
                        }
                    }
                }
            });
        }
    }

    // Run immediately if the DOM is already loaded.
    if (document.readyState === "loading") {
        document.addEventListener(
            "DOMContentLoaded",
            initializeReportsCharts
        );
    } else {
        initializeReportsCharts();
    }

})();