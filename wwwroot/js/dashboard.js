document.addEventListener("DOMContentLoaded", function () {

    // ==========================================
    // SALES OVERVIEW CHART
    // ==========================================

    const salesCanvas = document.getElementById("salesTrendChart");

    if (salesCanvas) {

        const salesLabels =
            window.dashboardSalesLabels || [];

        const salesData =
            window.dashboardSalesData || [];

        new Chart(salesCanvas, {

            type: "line",

            data: {
                labels: salesLabels,

                datasets: [{
                    label: "Sales",

                    data: salesData,

                    borderColor: "#E4772E",

                    backgroundColor:
                        "rgba(228, 119, 46, 0.12)",

                    fill: true,

                    tension: 0.3,

                    pointRadius: 4,

                    pointHoverRadius: 6,

                    pointBackgroundColor: "#E4772E",

                    borderWidth: 2
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

                            label: function (context) {

                                return " ₱" +
                                    Number(context.raw)
                                        .toLocaleString(
                                            "en-PH",
                                            {
                                                minimumFractionDigits: 2
                                            }
                                        );
                            }
                        }
                    }
                },

                scales: {

                    y: {

                        beginAtZero: true,

                        ticks: {

                            callback: function (value) {

                                return "₱" +
                                    Number(value)
                                        .toLocaleString(
                                            "en-PH"
                                        );
                            }
                        }
                    },

                    x: {

                        grid: {
                            display: false
                        }
                    }
                }
            }
        });
    }


    // ==========================================
    // CATEGORY BREAKDOWN CHART
    // ==========================================

    const categoryCanvas =
        document.getElementById("categoryChart");

    if (categoryCanvas) {

        const categoryLabels =
            window.dashboardCategoryLabels || [];

        const categoryData =
            window.dashboardCategoryData || [];

        new Chart(categoryCanvas, {

            type: "doughnut",

            data: {

                labels: categoryLabels,

                datasets: [{

                    data: categoryData,

                    backgroundColor: [
                        "#E4772E",
                        "#E0A63A",
                        "#B5451B",
                        "#4C9A6A",
                        "#8A7A6D",
                        "#5B7FA3",
                        "#A66B9C"
                    ],

                    borderWidth: 0
                }]
            },

            options: {

                responsive: true,

                maintainAspectRatio: false,

                cutout: "65%",

                plugins: {

                    legend: {

                        position: "bottom",

                        labels: {

                            boxWidth: 12,

                            padding: 15
                        }
                    }
                }
            }
        });
    }

});