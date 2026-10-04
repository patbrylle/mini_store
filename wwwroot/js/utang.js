(function () {

    function init() {

        // =========================================================
        // SEARCH + STATUS FILTER
        // One function applies both, so they never overwrite each other.
        // =========================================================

        var table = document.getElementById("utangTable");
        var rows = table
            ? Array.prototype.slice.call(table.querySelectorAll("tbody tr[data-status]"))
            : [];

        var chips = document.querySelectorAll("#utangFilters .ut-chip");
        var search = document.getElementById("utangSearch");
        var noResults = document.getElementById("utangNoResults");
        var shown = document.getElementById("utangShown");
        var clearBtn = document.getElementById("utangClear");

        var activeFilter = "all";

        function matchesFilter(row) {
            if (activeFilter === "all") return true;
            if (activeFilter === "overdue") return row.dataset.overdue === "true";
            return row.dataset.status === activeFilter;
        }

        function apply() {
            var query = search ? search.value.toLowerCase().trim() : "";
            var visible = 0;

            rows.forEach(function (row) {
                var show =
                    matchesFilter(row) &&
                    (!query || (row.dataset.search || "").indexOf(query) !== -1);

                row.hidden = !show;
                if (show) visible++;
            });

            if (noResults) noResults.hidden = rows.length === 0 || visible !== 0;
            if (shown) shown.textContent = "Showing " + visible + " of " + rows.length;
        }

        function setFilter(filter) {
            activeFilter = filter;

            chips.forEach(function (chip) {
                var on = chip.dataset.filter === filter;
                chip.classList.toggle("active", on);
                chip.setAttribute("aria-pressed", on ? "true" : "false");
            });

            apply();
        }

        chips.forEach(function (chip) {
            chip.addEventListener("click", function () {
                setFilter(chip.dataset.filter);
            });
        });

        if (search) {
            search.addEventListener("input", apply);
            search.addEventListener("search", apply);
        }

        if (clearBtn) {
            clearBtn.addEventListener("click", function () {
                if (search) search.value = "";
                setFilter("all");
            });
        }


        // =========================================================
        // DELETE MODAL: fill it with the clicked record
        // =========================================================

        var deleteModal = document.getElementById("deleteUtangModal");

        if (deleteModal) {
            deleteModal.addEventListener("show.bs.modal", function (event) {
                var btn = event.relatedTarget;
                if (!btn) return;

                document.getElementById("deleteUtangId").value = btn.getAttribute("data-id");
                document.getElementById("deleteUtangName").textContent = btn.getAttribute("data-name") + "'s";
                document.getElementById("deleteUtangAmount").textContent = btn.getAttribute("data-amount");
            });
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }

})();