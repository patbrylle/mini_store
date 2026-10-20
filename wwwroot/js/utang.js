
(function () {
    "use strict";

    function init() {
        // =========================================================
        // SEARCH + STATUS FILTER
        // =========================================================

        var table = document.getElementById("utangTable");

        var rows = table
            ? Array.from(
                table.querySelectorAll("tbody tr[data-status]")
            )
            : [];

        var chips = document.querySelectorAll(
            "#utangFilters .ut-chip"
        );

        var search = document.getElementById("utangSearch");
        var noResults = document.getElementById("utangNoResults");
        var shown = document.getElementById("utangShown");
        var clearBtn = document.getElementById("utangClear");

        var activeFilter = "all";

        function matchesFilter(row) {
            if (activeFilter === "all") {
                return true;
            }

            if (activeFilter === "overdue") {
                return row.dataset.overdue === "true";
            }

            return row.dataset.status === activeFilter;
        }

        function applyFilters() {
            var query = search
                ? search.value.toLowerCase().trim()
                : "";

            var visible = 0;

            rows.forEach(function (row) {
                var searchText = (
                    row.dataset.search || row.textContent || ""
                ).toLowerCase();

                var matchesSearch =
                    query === "" || searchText.includes(query);

                var shouldShow =
                    matchesFilter(row) && matchesSearch;

                row.hidden = !shouldShow;

                if (shouldShow) {
                    visible++;
                }
            });

            if (shown) {
                shown.textContent =
                    "Showing " + visible + " of " + rows.length;
            }

            if (noResults) {
                noResults.hidden =
                    rows.length === 0 || visible > 0;
            }
        }

        function setFilter(filter) {
            activeFilter = filter || "all";

            chips.forEach(function (chip) {
                var isActive =
                    chip.dataset.filter === activeFilter;

                chip.classList.toggle("active", isActive);

                chip.setAttribute(
                    "aria-pressed",
                    isActive ? "true" : "false"
                );
            });

            applyFilters();
        }

        chips.forEach(function (chip) {
            chip.addEventListener("click", function () {
                setFilter(chip.dataset.filter);
            });
        });

        if (search) {
            search.addEventListener("input", applyFilters);
            search.addEventListener("search", applyFilters);
        }

        if (clearBtn) {
            clearBtn.addEventListener("click", function () {
                if (search) {
                    search.value = "";
                }

                setFilter("all");

                if (search) {
                    search.focus();
                }
            });
        }

        // =========================================================
        // DELETE MODAL
        // =========================================================

        var deleteModal =
            document.getElementById("deleteUtangModal");

        if (deleteModal) {
            deleteModal.addEventListener(
                "show.bs.modal",
                function (event) {
                    var button = event.relatedTarget;

                    if (!button) {
                        return;
                    }

                    var idInput =
                        document.getElementById("deleteUtangId");

                    var nameElement =
                        document.getElementById("deleteUtangName");

                    var amountElement =
                        document.getElementById("deleteUtangAmount");

                    if (idInput) {
                        idInput.value =
                            button.getAttribute("data-id") || "";
                    }

                    if (nameElement) {
                        var name =
                            button.getAttribute("data-name") || "Customer";

                        nameElement.textContent = name + "'s";
                    }

                    if (amountElement) {
                        amountElement.textContent =
                            button.getAttribute("data-amount") || "";
                    }
                }
            );
        }

        // Initial display
        applyFilters();
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }
})();
