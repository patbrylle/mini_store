document.addEventListener("DOMContentLoaded", function () {

    // ==============================
    // INVENTORY SEARCH
    // ==============================

    const searchInput = document.getElementById("inventorySearch");
    const table = document.getElementById("inventoryTable");

    if (searchInput && table) {

        const tbody = table.querySelector("tbody");
        const rows = Array.from(tbody.querySelectorAll("tr"));

        // "No matching products" row, shown when nothing matches
        const noResultsRow = document.createElement("tr");
        noResultsRow.hidden = true;
        noResultsRow.innerHTML =
            '<td colspan="6" class="text-center text-muted py-4">' +
            '<i class="bi bi-search me-1"></i> No matching products.' +
            '</td>';
        tbody.appendChild(noResultsRow);

        function filterInventory() {
            const value = searchInput.value.toLowerCase().trim();
            let visible = 0;

            rows.forEach(function (row) {
                // Search only Product (col 1) and Category (col 2)
                const product = row.cells[0]?.textContent.toLowerCase() ?? "";
                const category = row.cells[1]?.textContent.toLowerCase() ?? "";

                const match =
                    product.includes(value) || category.includes(value);

                row.style.display = match ? "" : "none";

                if (match) visible++;
            });

            noResultsRow.hidden = visible !== 0;
        }

        ["input", "keyup", "search"].forEach(function (evt) {
            searchInput.addEventListener(evt, filterInventory);
        });
    }


    // ==============================
    // ADD STOCK MODAL
    // ==============================

    const addStockModal = document.getElementById("addStockModal");

    if (addStockModal) {
        addStockModal.addEventListener("show.bs.modal", function (event) {

            const button = event.relatedTarget;

            // Modal was opened without a button (e.g. from a URL), so skip filling it
            if (!button) return;

            document.getElementById("modalProductId").value =
                button.getAttribute("data-product-id");

            document.getElementById("modalProductName").value =
                button.getAttribute("data-product-name");

            document.getElementById("modalCurrentStock").value =
                button.getAttribute("data-current-stock");
        });
    }

});