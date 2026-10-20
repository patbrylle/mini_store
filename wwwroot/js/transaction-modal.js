
(function () {
    "use strict";

    const form = document.getElementById("saleForm");
    const selector = document.getElementById("productSelector");
    const container = document.getElementById("saleProductsContainer");

    if (!form || !selector || !container) return;

    // Prevent duplicate initialization.
    if (form.dataset.txInit === "1") return;
    form.dataset.txInit = "1";

    const emptyMsg = document.getElementById("emptyProductMessage");
    const totalEl = document.getElementById("saleTotal");
    const paidInput = document.getElementById("amountPaid");
    const resultEl = document.getElementById("paymentResult");
    const resultLbl = document.getElementById("paymentResultLabel");
    const utangInfo = document.getElementById("utangPaymentInfo");
    const customerSelect = document.getElementById("customerSelect");
    const submitButton = form.querySelector(".transaction-submit-btn");
    const modal = document.getElementById("newSaleModal");

    let items = [];

    function peso(value) {
        return "₱" + Number(value || 0).toLocaleString("en-PH", {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    function getTransactionType() {
        const checked = form.querySelector(
            'input[name="TransactionType"]:checked'
        );

        return checked ? checked.value : "Cash";
    }

    function getTotal() {
        return items.reduce(function (sum, item) {
            return sum + item.price * item.qty;
        }, 0);
    }

    function updateTotals() {
        const total = getTotal();
        const paid = Math.max(
            0,
            parseFloat(paidInput ? paidInput.value : "0") || 0
        );

        const utang = getTransactionType() === "Utang";

        if (totalEl) {
            totalEl.textContent = peso(total);
        }

        if (utangInfo) {
            utangInfo.style.display = utang ? "flex" : "none";
        }

        if (resultLbl && resultEl) {
            if (utang) {
                resultLbl.textContent = "Remaining Balance";
                resultEl.textContent = peso(Math.max(total - paid, 0));
                resultEl.classList.add("utang-result");
            } else if (paid >= total) {
                resultLbl.textContent = "Change";
                resultEl.textContent = peso(paid - total);
                resultEl.classList.remove("utang-result");
            } else {
                resultLbl.textContent = "Still Needed";
                resultEl.textContent = peso(total - paid);
                resultEl.classList.remove("utang-result");
            }
        }

        if (customerSelect) {
            customerSelect.required = utang;
        }

        if (submitButton) {
            submitButton.innerHTML = utang
                ? '<i class="bi bi-wallet2 me-1"></i> Record Utang'
                : '<i class="bi bi-check-lg me-1"></i> Complete Sale';
        }
    }

    function render() {
        // Remove only rows managed by this script.
        container.querySelectorAll(".product-row").forEach(function (row) {
            row.remove();
        });

        if (emptyMsg) {
            emptyMsg.style.display = items.length ? "none" : "";
        }

        items.forEach(function (item) {
            const row = document.createElement("div");
            row.className = "product-row";

            const nameCell = document.createElement("div");
            nameCell.className = "product-row-name";

            const name = document.createElement("span");
            name.textContent = item.name;

            const details = document.createElement("small");
            details.textContent =
                peso(item.price) + " each · " + item.stock + " in stock";

            const productId = document.createElement("input");
            productId.type = "hidden";
            productId.name = "ProductId";
            productId.value = item.id;

            nameCell.append(name, details, productId);

            const qtyCell = document.createElement("div");
            qtyCell.className = "qty-stepper";

            const decrease = document.createElement("button");
            decrease.type = "button";
            decrease.className = "qty-btn";
            decrease.dataset.step = "-1";
            decrease.setAttribute("aria-label", "Decrease quantity");
            decrease.textContent = "−";

            const qtyInput = document.createElement("input");
            qtyInput.type = "number";
            qtyInput.className = "product-row-qty";
            qtyInput.name = "Quantity";
            qtyInput.min = "1";
            qtyInput.max = String(item.stock);
            qtyInput.step = "1";
            qtyInput.value = String(item.qty);
            qtyInput.required = true;

            const increase = document.createElement("button");
            increase.type = "button";
            increase.className = "qty-btn";
            increase.dataset.step = "1";
            increase.setAttribute("aria-label", "Increase quantity");
            increase.textContent = "+";

            qtyCell.append(decrease, qtyInput, increase);

            const subtotal = document.createElement("div");
            subtotal.className = "product-row-subtotal";
            subtotal.textContent = peso(item.price * item.qty);

            const remove = document.createElement("button");
            remove.type = "button";
            remove.className = "product-row-remove";
            remove.setAttribute("aria-label", "Remove " + item.name);
            remove.innerHTML = '<i class="bi bi-x-lg"></i>';

            function setQuantity(value) {
                let quantity = parseInt(value, 10);

                if (!Number.isFinite(quantity)) {
                    quantity = 1;
                }

                quantity = Math.max(1, Math.min(quantity, item.stock));

                item.qty = quantity;
                qtyInput.value = String(quantity);
                subtotal.textContent = peso(item.price * quantity);

                updateTotals();
            }

            qtyInput.addEventListener("change", function () {
                setQuantity(qtyInput.value);
            });

            qtyInput.addEventListener("blur", function () {
                qtyInput.value = String(item.qty);
            });

            decrease.addEventListener("click", function () {
                setQuantity(item.qty - 1);
            });

            increase.addEventListener("click", function () {
                if (item.qty >= item.stock) {
                    alert("Maximum available stock for " + item.name +
                        " is " + item.stock + ".");
                    return;
                }

                setQuantity(item.qty + 1);
            });

            remove.addEventListener("click", function () {
                items = items.filter(function (existing) {
                    return existing.id !== item.id;
                });

                render();
            });

            row.append(nameCell, qtyCell, subtotal, remove);
            container.appendChild(row);
        });

        updateTotals();
    }

    // =========================================================
    // PRODUCT SELECTOR
    // =========================================================

    selector.addEventListener("change", function () {
        const option = selector.selectedOptions[0];

        if (!option || !option.value) return;

        const id = option.value;
        const stock = parseInt(option.dataset.stock, 10) || 0;
        const price = parseFloat(option.dataset.price) || 0;
        const name = option.textContent.split("—")[0].trim();

        if (stock <= 0) {
            alert(name + " is out of stock.");
            selector.value = "";
            return;
        }

        if (!Number.isFinite(price) || price < 0) {
            alert("The selected product has an invalid price.");
            selector.value = "";
            return;
        }

        const existing = items.find(function (item) {
            return item.id === id;
        });

        if (existing) {
            if (existing.qty < existing.stock) {
                existing.qty++;
            } else {
                alert("Maximum available stock for " + name +
                    " is " + existing.stock + ".");
            }
        } else {
            items.push({
                id: id,
                name: name,
                price: price,
                stock: stock,
                qty: 1
            });
        }

        render();
        selector.value = "";
    });

    // =========================================================
    // PAYMENT + TRANSACTION TYPE
    // =========================================================

    if (paidInput) {
        paidInput.addEventListener("input", updateTotals);
    }

    form.querySelectorAll('input[name="TransactionType"]').forEach(
        function (radio) {
            radio.addEventListener("change", updateTotals);
        }
    );

    // =========================================================
    // FORM VALIDATION
    // =========================================================

    form.addEventListener("submit", function (event) {
        if (items.length === 0) {
            event.preventDefault();
            alert("Please select at least one product.");
            selector.focus();
            return;
        }

        // Ensure submitted product IDs and quantities match rendered rows.
        const renderedRows = container.querySelectorAll(".product-row");

        if (renderedRows.length !== items.length) {
            event.preventDefault();
            alert("Please check the selected products and try again.");
            return;
        }

        const total = getTotal();
        const paid = Math.max(
            0,
            parseFloat(paidInput ? paidInput.value : "0") || 0
        );

        if (total <= 0) {
            event.preventDefault();
            alert("The transaction total must be greater than zero.");
            return;
        }

        if (getTransactionType() === "Cash" && paid < total) {
            event.preventDefault();
            alert("The amount paid cannot be less than the total sale amount.");

            if (paidInput) paidInput.focus();
            return;
        }

        if (getTransactionType() === "Utang") {
            if (!customerSelect || !customerSelect.value) {
                event.preventDefault();
                alert("Please select a customer for an utang transaction.");

                if (customerSelect) customerSelect.focus();
                return;
            }

            if (paid > total) {
                event.preventDefault();
                alert("For an utang transaction, the amount paid cannot be greater than the total.");

                if (paidInput) paidInput.focus();
                return;
            }
        }

        // Validate quantities again before sending the form.
        for (const item of items) {
            if (
                !Number.isInteger(item.qty) ||
                item.qty < 1 ||
                item.qty > item.stock
            ) {
                event.preventDefault();
                alert("Please check the quantity for " + item.name + ".");
                return;
            }
        }
    });

    // =========================================================
    // RESET WHEN MODAL CLOSES
    // =========================================================

    if (modal) {
        modal.addEventListener("hidden.bs.modal", function () {
            items = [];

            if (paidInput) {
                paidInput.value = "0";
            }

            selector.value = "";

            // Restore the page's configured default transaction type.
            const defaultType =
                modal.dataset.defaultTransactionType || "Cash";

            const defaultRadio = form.querySelector(
                'input[name="TransactionType"][value="' +
                defaultType + '"]'
            );

            if (defaultRadio) {
                defaultRadio.checked = true;
            }

            if (customerSelect) {
                customerSelect.value = "";
            }

            render();
        });
    }

    // Initial setup.
    render();
})();
