(function () {
    const form      = document.getElementById("saleForm");
    const selector  = document.getElementById("productSelector");
    const container = document.getElementById("saleProductsContainer");
    const emptyMsg  = document.getElementById("emptyProductMessage");
    const totalEl   = document.getElementById("saleTotal");
    const paidInput = document.getElementById("amountPaid");
    const resultEl  = document.getElementById("paymentResult");
    const resultLbl = document.getElementById("paymentResultLabel");
    const utangInfo = document.getElementById("utangPaymentInfo");
    const modal     = document.getElementById("newSaleModal");

    if (!form || !selector || !container) return;

    // never initialise twice
    if (form.dataset.txInit) return;
    form.dataset.txInit = "1";

    let items = [];

    const peso = n => "₱" + Number(n).toLocaleString("en-PH", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });

    const isUtang = () => {
        const checked = form.querySelector('input[name="TransactionType"]:checked');
        return checked && checked.value === "Utang";
    };

    function updateTotals() {
        const total = items.reduce((s, i) => s + i.price * i.qty, 0);
        const paid  = parseFloat(paidInput.value) || 0;

        totalEl.textContent = peso(total);

        if (utangInfo) utangInfo.style.display = isUtang() ? "" : "none";

        if (isUtang()) {
            resultLbl.textContent = "Remaining Balance";
            resultEl.textContent  = peso(Math.max(total - paid, 0));
        } else if (paid >= total) {
            resultLbl.textContent = "Change";
            resultEl.textContent  = peso(paid - total);
        } else {
            resultLbl.textContent = "Still Needed";
            resultEl.textContent  = peso(total - paid);
        }
    }

    function render() {
        container.querySelectorAll(".product-row").forEach(r => r.remove());
        emptyMsg.style.display = items.length ? "none" : "";

        items.forEach(item => {
            const row = document.createElement("div");
            row.className = "product-row";

            row.innerHTML = `
                <div class="product-row-name">
                    <span></span>
                    <small>${peso(item.price)} each · ${item.stock} in stock</small>
                    <input type="hidden" name="ProductId" value="${item.id}" />
                </div>

                <div class="qty-stepper">
                    <button type="button" class="qty-btn" data-step="-1" aria-label="Decrease">−</button>
                    <input type="number" class="product-row-qty" name="Quantity"
                           min="1" max="${item.stock}" value="${item.qty}" />
                    <button type="button" class="qty-btn" data-step="1" aria-label="Increase">+</button>
                </div>

                <div class="product-row-subtotal">${peso(item.price * item.qty)}</div>

                <button type="button" class="product-row-remove" aria-label="Remove">
                    <i class="bi bi-x-lg"></i>
                </button>
            `;

            row.querySelector(".product-row-name span").textContent = item.name;

            const qtyInput = row.querySelector(".product-row-qty");
            const subtotal = row.querySelector(".product-row-subtotal");

            function setQty(value) {
                let q = parseInt(value, 10) || 1;
                q = Math.min(Math.max(q, 1), item.stock);
                item.qty = q;
                qtyInput.value = q;
                subtotal.textContent = peso(item.price * q);
                updateTotals();
            }

            qtyInput.addEventListener("input", () => setQty(qtyInput.value));
            qtyInput.addEventListener("blur",  () => { qtyInput.value = item.qty; });

            row.querySelectorAll(".qty-btn").forEach(btn => {
                btn.addEventListener("click", () =>
                    setQty(item.qty + parseInt(btn.dataset.step, 10)));
            });

            row.querySelector(".product-row-remove").addEventListener("click", () => {
                items = items.filter(i => i.id !== item.id);
                render();
            });

            container.appendChild(row);
        });

        updateTotals();
    }

    selector.addEventListener("change", function () {
        const option = selector.selectedOptions[0];
        if (!option || !option.value) return;

        const id    = option.value;
        const stock = parseInt(option.dataset.stock, 10) || 0;
        const price = parseFloat(String(option.dataset.price).replace(",", ".")) || 0;
        const name  = option.textContent.split("—")[0].trim();

        if (stock <= 0) {
            alert(name + " is out of stock.");
            selector.value = "";
            return;
        }

        const existing = items.find(i => i.id === id);

        if (existing) {
            if (existing.qty < stock) existing.qty++;
            else alert("Maximum available stock for " + name + " is " + stock + ".");
        } else {
            items.push({ id, name, price, stock, qty: 1 });
        }

        render();
        selector.value = "";
    });

    paidInput.addEventListener("input", updateTotals);

    form.querySelectorAll('input[name="TransactionType"]').forEach(r =>
        r.addEventListener("change", updateTotals));

    // =========================================================
    // FORM SUBMIT
    // =========================================================
    form.addEventListener("submit", function (event) {
        if (form.querySelectorAll('input[name="ProductId"]').length === 0) {
            event.preventDefault();
            alert("Please add at least one product.");
        }
    });

    // =========================================================
    // RESET WHEN MODAL CLOSES
    // =========================================================
    if (modal) {
        modal.addEventListener("hidden.bs.modal", function () {
            items = [];
            paidInput.value = "0";
            selector.value = "";
            render();
        });
    }

    render();
})();