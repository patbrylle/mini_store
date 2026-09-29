
document.addEventListener("DOMContentLoaded", function () {


    /* ===================================================== */
    /* PRODUCT SEARCH */
    /* ===================================================== */

    const searchInput =
        document.getElementById("productSearch");

    const productRows =
        document.querySelectorAll(".product-row");


    if (searchInput) {

        searchInput.addEventListener("input", function () {

            const searchValue =
                this.value.toLowerCase().trim();


            productRows.forEach(function (row) {

                const productText =
                    row.textContent.toLowerCase();


                if (productText.includes(searchValue)) {

                    row.style.display = "";

                } else {

                    row.style.display = "none";

                }

            });

        });

    }


    /* ===================================================== */
    /* EDIT PRODUCT MODAL */
    /* ===================================================== */

    const editButtons =
        document.querySelectorAll(".edit-product-btn");


    editButtons.forEach(function (button) {

        button.addEventListener("click", function () {

            const productId =
                this.getAttribute("data-product-id");

            const productName =
                this.getAttribute("data-product-name");

            const categoryId =
                this.getAttribute("data-category-id");

            const price =
                this.getAttribute("data-price");

            const quantity =
                this.getAttribute("data-quantity");


            const idInput =
                document.getElementById("editProductId");

            const nameInput =
                document.getElementById("editProductName");

            const categoryInput =
                document.getElementById("editCategoryId");

            const priceInput =
                document.getElementById("editPrice");

            const quantityInput =
                document.getElementById("editQuantity");


            if (idInput) {
                idInput.value = productId || "";
            }

            if (nameInput) {
                nameInput.value = productName || "";
            }

            if (categoryInput) {
                categoryInput.value = categoryId || "";
            }

            if (priceInput) {
                priceInput.value = price || "";
            }

            if (quantityInput) {
                quantityInput.value = quantity || "";
            }

        });

    });


    /* ===================================================== */
    /* VIEW PRODUCT MODAL */
    /* ===================================================== */

    const viewButtons =
        document.querySelectorAll(".view-product-btn");


    viewButtons.forEach(function (button) {

        button.addEventListener("click", function () {

            /* --------------------------------------------- */
            /* GET PRODUCT DATA FROM BUTTON */
            /* --------------------------------------------- */

            const productId =
                this.getAttribute("data-product-id");

            const productName =
                this.getAttribute("data-product-name");

            const category =
                this.getAttribute("data-category");

            const price =
                this.getAttribute("data-price");

            const quantity =
                this.getAttribute("data-quantity");


            /* --------------------------------------------- */
            /* GET MODAL ELEMENTS */
            /* --------------------------------------------- */

            const productNameText =
                document.getElementById(
                    "viewProductName"
                );

            const productNameInfo =
                document.getElementById(
                    "viewProductNameInfo"
                );

            const categoryText =
                document.getElementById(
                    "viewProductCategory"
                );

            const categoryInfo =
                document.getElementById(
                    "viewProductCategoryInfo"
                );

            const priceText =
                document.getElementById(
                    "viewProductPrice"
                );

            const quantityText =
                document.getElementById(
                    "viewProductQuantity"
                );

            const stockQuantity =
                document.getElementById(
                    "viewStockQuantity"
                );

            const stockStatus =
                document.getElementById(
                    "viewStockStatus"
                );


            /* --------------------------------------------- */
            /* PRODUCT NAME */
            /* --------------------------------------------- */

            if (productNameText) {

                productNameText.textContent =
                    productName || "Unnamed Product";

            }


            if (productNameInfo) {

                productNameInfo.textContent =
                    productName || "Unnamed Product";

            }


            /* --------------------------------------------- */
            /* CATEGORY */
            /* --------------------------------------------- */

            if (categoryText) {

                categoryText.textContent =
                    category || "Uncategorized";

            }


            if (categoryInfo) {

                categoryInfo.textContent =
                    category || "Uncategorized";

            }


            /* --------------------------------------------- */
            /* PRICE */
            /* --------------------------------------------- */

            if (priceText) {

                priceText.textContent =
                    "₱" + (price || "0.00");

            }


            /* --------------------------------------------- */
            /* QUANTITY */
            /* --------------------------------------------- */

            if (quantityText) {

                quantityText.textContent =
                    (quantity || "0") + " units";

            }


            if (stockQuantity) {

                stockQuantity.textContent =
                    (quantity || "0") + " units";

            }


            /* --------------------------------------------- */
            /* STOCK STATUS */
            /* --------------------------------------------- */

            const stockValue =
                parseInt(quantity) || 0;


            if (stockStatus) {

                /* Remove old status classes */

                stockStatus.classList.remove(
                    "view-stock-good",
                    "view-stock-low",
                    "view-stock-out"
                );


                /* OUT OF STOCK */

                if (stockValue <= 0) {

                    stockStatus.textContent =
                        "Out of Stock";

                    stockStatus.classList.add(
                        "view-stock-out"
                    );

                }


                /* LOW STOCK */

                else if (stockValue <= 10) {

                    stockStatus.textContent =
                        "Low Stock";

                    stockStatus.classList.add(
                        "view-stock-low"
                    );

                }


                /* IN STOCK */

                else {

                    stockStatus.textContent =
                        "In Stock";

                    stockStatus.classList.add(
                        "view-stock-good"
                    );

                }

            }

        });

    });


    /* ===================================================== */
    /* DELETE PRODUCT MODAL */
    /* ===================================================== */

    const deleteButtons =
        document.querySelectorAll(".delete-product-btn");


    deleteButtons.forEach(function (button) {

        button.addEventListener("click", function () {

            const productId =
                this.getAttribute("data-product-id");

            const productName =
                this.getAttribute("data-product-name");

            const category =
                this.getAttribute("data-category");

            const price =
                this.getAttribute("data-price");

            const stock =
                this.getAttribute("data-stock");


            const idInput =
                document.getElementById(
                    "deleteProductId"
                );

            const nameText =
                document.getElementById(
                    "deleteProductName"
                );

            const nameInfo =
                document.getElementById(
                    "deleteProductNameInfo"
                );

            const categoryText =
                document.getElementById(
                    "deleteProductCategory"
                );

            const priceText =
                document.getElementById(
                    "deleteProductPrice"
                );

            const stockText =
                document.getElementById(
                    "deleteProductStock"
                );


            if (idInput) {

                idInput.value =
                    productId || "";

            }


            if (nameText) {

                nameText.textContent =
                    productName || "";

            }


            if (nameInfo) {

                nameInfo.textContent =
                    productName || "";

            }


            if (categoryText) {

                categoryText.textContent =
                    category || "Uncategorized";

            }


            if (priceText) {

                priceText.textContent =
                    "₱" + (price || "0.00");

            }


            if (stockText) {

                stockText.textContent =
                    stock || "0";

            }

        });

    });


    /* ===================================================== */
    /* RESET ADD PRODUCT MODAL */
    /* ===================================================== */

    const addProductModal =
        document.getElementById("addProductModal");


    if (addProductModal) {

        addProductModal.addEventListener(
            "hidden.bs.modal",
            function () {

                const form =
                    addProductModal.querySelector("form");


                if (form) {

                    form.reset();

                }

            }
        );

    }


    /* ===================================================== */
    /* RESET EDIT PRODUCT MODAL */
    /* ===================================================== */

    const editProductModal =
        document.getElementById("editProductModal");


    if (editProductModal) {

        editProductModal.addEventListener(
            "hidden.bs.modal",
            function () {

                const form =
                    editProductModal.querySelector("form");


                if (form) {

                    form.reset();

                }

            }
        );

    }


    /* ===================================================== */
    /* RESET DELETE PRODUCT MODAL */
    /* ===================================================== */

    const deleteProductModal =
        document.getElementById(
            "deleteProductModal"
        );


    if (deleteProductModal) {

        deleteProductModal.addEventListener(
            "hidden.bs.modal",
            function () {

                const form =
                    deleteProductModal.querySelector("form");


                if (form) {

                    form.reset();

                }


                const nameText =
                    document.getElementById(
                        "deleteProductName"
                    );

                const nameInfo =
                    document.getElementById(
                        "deleteProductNameInfo"
                    );


                if (nameText) {

                    nameText.textContent = "";

                }


                if (nameInfo) {

                    nameInfo.textContent = "";

                }

            }
        );

    }


    /* ===================================================== */
    /* RESET VIEW PRODUCT MODAL */
    /* ===================================================== */

    const viewProductModal =
        document.getElementById(
            "viewProductModal"
        );


    if (viewProductModal) {

        viewProductModal.addEventListener(
            "hidden.bs.modal",
            function () {

                const productNameText =
                    document.getElementById(
                        "viewProductName"
                    );

                const productNameInfo =
                    document.getElementById(
                        "viewProductNameInfo"
                    );

                const categoryText =
                    document.getElementById(
                        "viewProductCategory"
                    );

                const categoryInfo =
                    document.getElementById(
                        "viewProductCategoryInfo"
                    );

                const priceText =
                    document.getElementById(
                        "viewProductPrice"
                    );

                const quantityText =
                    document.getElementById(
                        "viewProductQuantity"
                    );

                const stockQuantity =
                    document.getElementById(
                        "viewStockQuantity"
                    );

                const stockStatus =
                    document.getElementById(
                        "viewStockStatus"
                    );


                if (productNameText) {

                    productNameText.textContent =
                        "Product Name";

                }


                if (productNameInfo) {

                    productNameInfo.textContent =
                        "-";

                }


                if (categoryText) {

                    categoryText.textContent =
                        "Category";

                }


                if (categoryInfo) {

                    categoryInfo.textContent =
                        "-";

                }


                if (priceText) {

                    priceText.textContent =
                        "₱0.00";

                }


                if (quantityText) {

                    quantityText.textContent =
                        "0 units";

                }


                if (stockQuantity) {

                    stockQuantity.textContent =
                        "0 units";

                }


                if (stockStatus) {

                    stockStatus.textContent =
                        "Out of Stock";


                    stockStatus.classList.remove(
                        "view-stock-good",
                        "view-stock-low"
                    );


                    stockStatus.classList.add(
                        "view-stock-out"
                    );

                }

            }
        );

    }

});

