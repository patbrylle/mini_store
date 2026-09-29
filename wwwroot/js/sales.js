/* =========================================================
   SALES JAVASCRIPT
   Single Product Selector + Automatic Product Rows
========================================================= */

document.addEventListener("DOMContentLoaded", function () {

    /* =====================================================
       ELEMENTS
    ===================================================== */

    const saleForm =
        document.getElementById("saleForm");

    const productSelector =
        document.getElementById("productSelector");

    const productsContainer =
        document.getElementById("saleProductsContainer");

    const emptyProductMessage =
        document.getElementById("emptyProductMessage");

    const amountPaidInput =
        document.getElementById("amountPaid");

    const saleTotal =
        document.getElementById("saleTotal");

    const paymentResult =
        document.getElementById("paymentResult");

    const paymentResultLabel =
        document.getElementById("paymentResultLabel");

    const customerSelect =
        document.getElementById("customerSelect");

    const utangPaymentInfo =
        document.getElementById("utangPaymentInfo");

    const submitButton =
        document.querySelector(".transaction-submit-btn");


    /* =====================================================
       CHECK REQUIRED ELEMENTS
    ===================================================== */

    if (!saleForm || !productSelector || !productsContainer) {
        return;
    }


    /* =====================================================
       FORMAT MONEY
    ===================================================== */

    function formatMoney(amount) {

        return new Intl.NumberFormat("en-PH", {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        }).format(amount);

    }


    /* =====================================================
       GET TRANSACTION TYPE
    ===================================================== */

    function getTransactionType() {

        const selected =
            document.querySelector(
                'input[name="TransactionType"]:checked'
            );

        return selected
            ? selected.value
            : "Cash";

    }


    /* =====================================================
       UPDATE EMPTY MESSAGE
    ===================================================== */

    function updateEmptyMessage() {

        const rows =
            productsContainer.querySelectorAll(
                ".sale-product-row"
            );

        if (emptyProductMessage) {

            emptyProductMessage.style.display =
                rows.length === 0
                    ? "block"
                    : "none";

        }

    }


    /* =====================================================
       GET PRODUCT ROW
    ===================================================== */

    function getProductRow(productId) {

        const rows =
            productsContainer.querySelectorAll(
                ".sale-product-row"
            );

        for (const row of rows) {

            if (
                row.dataset.productId ===
                String(productId)
            ) {

                return row;

            }

        }

        return null;

    }


    /* =====================================================
       CREATE PRODUCT ROW
    ===================================================== */

    function createProductRow(
        productId,
        productName,
        price,
        stock
    ) {

        const row =
            document.createElement("div");

        row.className =
            "sale-product-row";

        row.dataset.productId =
            productId;

        row.dataset.price =
            price;

        row.dataset.stock =
            stock;


        /* ================================================
           PRODUCT
        ================================================= */

        const productCell =
            document.createElement("div");

        productCell.className =
            "sale-product-name";


        const productIcon =
            document.createElement("div");

        productIcon.className =
            "sale-product-icon";

        productIcon.innerHTML =
            '<i class="bi bi-box"></i>';


        const productInfo =
            document.createElement("div");


        const productNameElement =
            document.createElement("strong");

        productNameElement.textContent =
            productName;


        const stockText =
            document.createElement("small");

        stockText.textContent =
            stock + " in stock";

        stockText.style.display =
            "block";

        stockText.style.color =
            "#999";

        stockText.style.fontSize =
            "10px";


        productInfo.appendChild(
            productNameElement
        );

        productInfo.appendChild(
            stockText
        );


        productCell.appendChild(
            productIcon
        );

        productCell.appendChild(
            productInfo
        );


        /* ================================================
           HIDDEN PRODUCT ID
        ================================================= */

        const productIdInput =
            document.createElement("input");

        productIdInput.type =
            "hidden";

        productIdInput.name =
            "ProductId";

        productIdInput.value =
            productId;


        /* ================================================
           QUANTITY
        ================================================= */

        const quantityCell =
            document.createElement("div");

        const quantityInput =
            document.createElement("input");

        quantityInput.type =
            "number";

        quantityInput.name =
            "Quantity";

        quantityInput.className =
            "form-control sale-quantity";

        quantityInput.min =
            "1";

        quantityInput.max =
            stock;

        quantityInput.value =
            "1";

        quantityInput.step =
            "1";


        quantityCell.appendChild(
            quantityInput
        );


        /* ================================================
           SUBTOTAL
        ================================================= */

        const subtotalCell =
            document.createElement("div");

        subtotalCell.className =
            "sale-subtotal";

        subtotalCell.textContent =
            "₱" + formatMoney(price);


        /* ================================================
           REMOVE BUTTON
        ================================================= */

        const removeCell =
            document.createElement("div");

        const removeButton =
            document.createElement("button");

        removeButton.type =
            "button";

        removeButton.className =
            "remove-sale-product";

        removeButton.title =
            "Remove product";

        removeButton.innerHTML =
            '<i class="bi bi-x-lg"></i>';


        removeButton.addEventListener(
            "click",
            function () {

                row.remove();

                updateEmptyMessage();

                calculateTotal();

            }
        );


        removeCell.appendChild(
            removeButton
        );


        /* ================================================
           ADD CONTENT TO ROW
        ================================================= */

        row.appendChild(
            productCell
        );

        row.appendChild(
            quantityCell
        );

        row.appendChild(
            subtotalCell
        );

        row.appendChild(
            removeCell
        );

        row.appendChild(
            productIdInput
        );


        /* ================================================
           QUANTITY CHANGE
        ================================================= */

        quantityInput.addEventListener(
            "input",
            function () {

                let quantity =
                    parseInt(
                        this.value,
                        10
                    );

                const maximumStock =
                    parseInt(
                        row.dataset.stock,
                        10
                    );


                if (
                    isNaN(quantity) ||
                    quantity < 1
                ) {

                    quantity = 1;

                }


                if (
                    quantity >
                    maximumStock
                ) {

                    quantity =
                        maximumStock;

                }


                this.value =
                    quantity;


                updateRowSubtotal(row);

                calculateTotal();

            }
        );


        /* ================================================
           ADD ROW
        ================================================= */

        productsContainer.appendChild(
            row
        );


        updateEmptyMessage();

        calculateTotal();

    }


    /* =====================================================
       UPDATE ROW SUBTOTAL
    ===================================================== */

    function updateRowSubtotal(row) {

        const quantityInput =
            row.querySelector(
                ".sale-quantity"
            );

        const subtotalElement =
            row.querySelector(
                ".sale-subtotal"
            );


        if (
            !quantityInput ||
            !subtotalElement
        ) {

            return;

        }


        const quantity =
            parseInt(
                quantityInput.value,
                10
            ) || 0;


        const price =
            parseFloat(
                row.dataset.price
            ) || 0;


        const subtotal =
            quantity * price;


        subtotalElement.textContent =
            "₱" + formatMoney(subtotal);

    }


    /* =====================================================
       CALCULATE TOTAL
    ===================================================== */

    function calculateTotal() {

        let total = 0;


        const rows =
            productsContainer.querySelectorAll(
                ".sale-product-row"
            );


        rows.forEach(function (row) {

            const quantityInput =
                row.querySelector(
                    ".sale-quantity"
                );

            if (!quantityInput) {
                return;
            }


            const quantity =
                parseInt(
                    quantityInput.value,
                    10
                ) || 0;


            const price =
                parseFloat(
                    row.dataset.price
                ) || 0;


            total +=
                quantity * price;


            updateRowSubtotal(row);

        });


        if (saleTotal) {

            saleTotal.textContent =
                "₱" + formatMoney(total);

        }


        updatePaymentResult(total);

        return total;

    }


    /* =====================================================
       UPDATE PAYMENT RESULT
    ===================================================== */

    function updatePaymentResult(total) {

        if (
            !amountPaidInput ||
            !paymentResult
        ) {

            return;

        }


        let amountPaid =
            parseFloat(
                amountPaidInput.value
            ) || 0;


        if (amountPaid < 0) {

            amountPaid = 0;

        }


        const transactionType =
            getTransactionType();


        if (transactionType === "Utang") {

            const balance =
                Math.max(
                    0,
                    total - amountPaid
                );


            if (paymentResultLabel) {

                paymentResultLabel.textContent =
                    "Remaining Balance";

            }


            paymentResult.textContent =
                "₱" + formatMoney(balance);


            paymentResult.classList.add(
                "utang-result"
            );

        }
        else {

            const change =
                Math.max(
                    0,
                    amountPaid - total
                );


            if (paymentResultLabel) {

                paymentResultLabel.textContent =
                    "Change";

            }


            paymentResult.textContent =
                "₱" + formatMoney(change);

        }

    }


    /* =====================================================
       PRODUCT SELECTOR
    ===================================================== */

    productSelector.addEventListener(
        "change",
        function () {

            const selectedOption =
                this.options[
                    this.selectedIndex
                ];


            if (
                !selectedOption ||
                !this.value
            ) {

                return;

            }


            const productId =
                this.value;


            const productName =
                selectedOption.textContent
                    .split("—")[0]
                    .trim();


            const price =
                parseFloat(
                    selectedOption.dataset.price
                ) || 0;


            const stock =
                parseInt(
                    selectedOption.dataset.stock,
                    10
                ) || 0;


            /* =========================================
               NO STOCK
            ========================================== */

            if (stock <= 0) {

                alert(
                    productName +
                    " is currently out of stock."
                );


                this.value = "";

                return;

            }


            /* =========================================
               PRODUCT ALREADY EXISTS
            ========================================== */

            const existingRow =
                getProductRow(productId);


            if (existingRow) {

                const quantityInput =
                    existingRow.querySelector(
                        ".sale-quantity"
                    );


                if (quantityInput) {

                    let currentQuantity =
                        parseInt(
                            quantityInput.value,
                            10
                        ) || 1;


                    const maximumStock =
                        parseInt(
                            existingRow.dataset.stock,
                            10
                        );


                    if (
                        currentQuantity <
                        maximumStock
                    ) {

                        currentQuantity++;

                        quantityInput.value =
                            currentQuantity;

                        updateRowSubtotal(
                            existingRow
                        );

                        calculateTotal();

                    }
                    else {

                        alert(
                            "You already selected the maximum available stock for " +
                            productName +
                            "."
                        );

                    }

                }


                this.value = "";

                return;

            }


            /* =========================================
               ADD NEW PRODUCT
            ========================================== */

            createProductRow(
                productId,
                productName,
                price,
                stock
            );


            /* =========================================
               RESET SELECTOR
            ========================================== */

            this.value = "";

        }
    );


    /* =====================================================
       AMOUNT PAID
    ===================================================== */

    if (amountPaidInput) {

        amountPaidInput.addEventListener(
            "input",
            function () {

                calculateTotal();

            }
        );

    }


    /* =====================================================
       TRANSACTION TYPE
    ===================================================== */

    const transactionTypeInputs =
        document.querySelectorAll(
            'input[name="TransactionType"]'
        );


    transactionTypeInputs.forEach(
        function (input) {

            input.addEventListener(
                "change",
                function () {

                    updateTransactionType();

                }
            );

        }
    );


    /* =====================================================
       UPDATE TRANSACTION TYPE UI
    ===================================================== */

    function updateTransactionType() {

        const transactionType =
            getTransactionType();


        if (transactionType === "Utang") {

            if (utangPaymentInfo) {

                utangPaymentInfo.style.display =
                    "flex";

            }


            if (submitButton) {

                submitButton.innerHTML =
                    '<i class="bi bi-wallet2 me-1"></i>' +
                    'Record Utang';

            }

            if (customerSelect) {

                customerSelect.required =
                    true;

            }

        }
        else {

            if (utangPaymentInfo) {

                utangPaymentInfo.style.display =
                    "none";

            }


            if (submitButton) {

                submitButton.innerHTML =
                    '<i class="bi bi-check-lg me-1"></i>' +
                    'Complete Sale';

            }


            if (customerSelect) {

                customerSelect.required =
                    false;

            }

        }


        calculateTotal();

    }


    /* =====================================================
       FORM SUBMISSION VALIDATION
    ===================================================== */

    saleForm.addEventListener(
        "submit",
        function (event) {

            const rows =
                productsContainer.querySelectorAll(
                    ".sale-product-row"
                );


            /* =========================================
               NO PRODUCTS
            ========================================== */

            if (rows.length === 0) {

                event.preventDefault();

                alert(
                    "Please select at least one product."
                );

                productSelector.focus();

                return;

            }


            /* =========================================
               CALCULATE TOTAL
            ========================================== */

            const total =
                calculateTotal();


            const amountPaid =
                parseFloat(
                    amountPaidInput?.value
                ) || 0;


            const transactionType =
                getTransactionType();


            /* =========================================
               CASH VALIDATION
            ========================================== */

            if (
                transactionType === "Cash" &&
                amountPaid < total
            ) {

                event.preventDefault();

                alert(
                    "The amount paid cannot be less than the total sale amount."
                );

                amountPaidInput.focus();

                return;

            }


            /* =========================================
               UTANG VALIDATION
            ========================================== */

            if (transactionType === "Utang") {

                if (
                    !customerSelect ||
                    !customerSelect.value
                ) {

                    event.preventDefault();

                    alert(
                        "Please select a customer for an utang transaction."
                    );

                    customerSelect?.focus();

                    return;

                }


                if (amountPaid > total) {

                    event.preventDefault();

                    alert(
                        "For an utang transaction, the amount paid cannot be greater than the total."
                    );

                    amountPaidInput.focus();

                    return;

                }

            }

        }
    );


    /* =====================================================
       SEARCH SALES
    ===================================================== */

    const salesSearch =
        document.getElementById("salesSearch");

    const salesTable =
        document.getElementById("salesTable");


    if (
        salesSearch &&
        salesTable
    ) {

        salesSearch.addEventListener(
            "input",
            function () {

                const searchValue =
                    this.value
                        .toLowerCase()
                        .trim();


                const rows =
                    salesTable.querySelectorAll(
                        "tbody tr"
                    );


                rows.forEach(
                    function (row) {

                        const rowText =
                            row.innerText
                                .toLowerCase();


                        row.style.display =
                            rowText.includes(
                                searchValue
                            )
                                ? ""
                                : "none";

                    }
                );

            }
        );

    }


    /* =====================================================
       RESET MODAL WHEN CLOSED
    ===================================================== */

    const newSaleModal =
        document.getElementById(
            "newSaleModal"
        );


    if (newSaleModal) {

        newSaleModal.addEventListener(
            "hidden.bs.modal",
            function () {

                /* Remove selected products */

                const rows =
                    productsContainer.querySelectorAll(
                        ".sale-product-row"
                    );


                rows.forEach(
                    function (row) {

                        row.remove();

                    }
                );


                updateEmptyMessage();


                /* Reset selector */

                productSelector.value =
                    "";


                /* Reset customer */

                if (customerSelect) {

                    customerSelect.value =
                        "";

                    customerSelect.required =
                        false;

                }


                /* Reset transaction type */

                const cashOption =
                    document.querySelector(
                        'input[name="TransactionType"][value="Cash"]'
                    );


                if (cashOption) {

                    cashOption.checked =
                        true;

                }


                /* Reset payment */

                if (amountPaidInput) {

                    amountPaidInput.value =
                        "0";

                }


                /* Reset payment UI */

                updateTransactionType();


                calculateTotal();

            }
        );

    }


    /* =====================================================
       INITIALIZE
    ===================================================== */

    updateEmptyMessage();

    updateTransactionType();

    calculateTotal();

});