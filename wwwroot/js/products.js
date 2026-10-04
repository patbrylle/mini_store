document.addEventListener("DOMContentLoaded", function () {

    /* ===================================================== */
    /* HELPERS                                               */
    /* ===================================================== */

    const $ = (id) => document.getElementById(id);

    function setText(id, value) {
        const el = $(id);
        if (el) el.textContent = value;
    }

    function setValue(id, value) {
        const el = $(id);
        if (el) el.value = value;
    }

    function resetFormOnClose(modalId, extraReset) {
        const modal = $(modalId);
        if (!modal) return;

        modal.addEventListener("hidden.bs.modal", function () {
            const form = modal.querySelector("form");
            if (form) form.reset();
            if (extraReset) extraReset();
        });
    }


    /* ===================================================== */
    /* PRODUCT SEARCH                                        */
    /* ===================================================== */

    const searchInput = $("productSearch");
    const tableBody = $("productsTableBody");
    const productRows = document.querySelectorAll(".product-row");

    let noResultRow = null;

    function getNoResultRow() {
        if (noResultRow) return noResultRow;

        noResultRow = document.createElement("tr");
        noResultRow.innerHTML =
            '<td colspan="5">' +
                '<div class="products-empty">' +
                    '<div class="empty-icon"><i class="bi bi-search"></i></div>' +
                    '<h5>No Products Found</h5>' +
                    '<p>No products match your search.</p>' +
                '</div>' +
            '</td>';

        return noResultRow;
    }

    if (searchInput && tableBody && productRows.length > 0) {

        searchInput.addEventListener("input", function () {

            const term = this.value.toLowerCase().trim();
            let visible = 0;

            productRows.forEach(function (row) {
                const match = row.textContent.toLowerCase().includes(term);
                row.style.display = match ? "" : "none";
                if (match) visible++;
            });

            const empty = getNoResultRow();

            if (visible === 0) {
                if (!empty.isConnected) tableBody.appendChild(empty);
            } else if (empty.isConnected) {
                empty.remove();
            }
        });
    }

/* ===================================================== */
/* EDIT PRODUCT MODAL                                    */
/* ===================================================== */

document.querySelectorAll(".edit-product-btn").forEach(function (button) {

    button.addEventListener("click", function () {

        const productId = this.dataset.productId || "";

        setValue("editProductId", productId);
        setValue("editProductIdParameter", productId);

        setValue(
            "editProductName",
            this.dataset.productName || ""
        );

        setValue(
            "editCategoryId",
            this.dataset.categoryId || ""
        );

        setValue(
            "editPrice",
            this.dataset.price || ""
        );

        setValue(
            "editQuantity",
            this.dataset.quantity || ""
        );

    });

});
    /* ===================================================== */
    /* VIEW PRODUCT MODAL                                    */
    /* ===================================================== */

    function setViewStatus(quantity) {

        const badge = $("viewStockStatus");
        if (!badge) return;

        badge.classList.remove("view-stock-good", "view-stock-low", "view-stock-out");

        if (quantity <= 0) {
            badge.textContent = "Out of Stock";
            badge.classList.add("view-stock-out");
        } else if (quantity <= 10) {
            badge.textContent = "Low Stock";
            badge.classList.add("view-stock-low");
        } else {
            badge.textContent = "In Stock";
            badge.classList.add("view-stock-good");
        }
    }

    document.querySelectorAll(".view-product-btn").forEach(function (button) {

        button.addEventListener("click", function () {

            const name = this.dataset.productName || "Unnamed Product";
            const category = this.dataset.category || "Uncategorized";
            const price = "₱" + (this.dataset.price || "0.00");
            const quantity = this.dataset.quantity || "0";
            const units = quantity + " units";

            setText("viewProductName", name);
            setText("viewProductNameInfo", name);
            setText("viewProductCategory", category);
            setText("viewProductCategoryInfo", category);
            setText("viewProductPrice", price);
            setText("viewProductQuantity", units);
            setText("viewStockQuantity", units);

            setViewStatus(parseInt(quantity, 10) || 0);

        });

    });


    /* ===================================================== */
    /* DELETE PRODUCT MODAL                                  */
    /* ===================================================== */

    document.querySelectorAll(".delete-product-btn").forEach(function (button) {

        button.addEventListener("click", function () {

            const name = this.dataset.productName || "";

            setValue("deleteProductId", this.dataset.productId || "");
            setText("deleteProductName", name);
            setText("deleteProductNameInfo", name);
            setText("deleteProductCategory", this.dataset.category || "Uncategorized");
            setText("deleteProductPrice", "₱" + (this.dataset.price || "0.00"));
            setText("deleteProductStock", this.dataset.stock || "0");

        });

    });


    /* ===================================================== */
    /* RESET MODALS ON CLOSE                                 */
    /* ===================================================== */

    resetFormOnClose("addProductModal");
    resetFormOnClose("editProductModal");

    resetFormOnClose("deleteProductModal", function () {
        setText("deleteProductName", "");
        setText("deleteProductNameInfo", "");
    });

    const viewModal = $("viewProductModal");

    if (viewModal) {

        viewModal.addEventListener("hidden.bs.modal", function () {

            setText("viewProductName", "Product Name");
            setText("viewProductNameInfo", "-");
            setText("viewProductCategory", "Category");
            setText("viewProductCategoryInfo", "-");
            setText("viewProductPrice", "₱0.00");
            setText("viewProductQuantity", "0 units");
            setText("viewStockQuantity", "0 units");

            setViewStatus(0);

        });

    }

});