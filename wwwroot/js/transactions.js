(function () {
    "use strict";

    var root = document.getElementById("txRoot");
    if (!root || !window.bootstrap) return;

    var URL = {
        create: root.dataset.createUrl,
        pay: root.dataset.payUrl,
        details: root.dataset.detailsUrl,
        receipt: root.dataset.receiptUrl
    };
    var RETURN_TO = root.dataset.return || "Sales";

    // ---------- helpers ----------
    var $ = function (id) { return document.getElementById(id); };
    var r2 = function (n) { return Math.round(n * 100) / 100; };

    function peso(n) {
        return "₱" + Number(n || 0).toLocaleString("en-PH", {
            minimumFractionDigits: 2, maximumFractionDigits: 2
        });
    }

    function esc(s) {
        return String(s == null ? "" : s).replace(/[&<>"']/g, function (c) {
            return { "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c];
        });
    }

    function ymd(d) {
        return d.getFullYear() + "-" +
            String(d.getMonth() + 1).padStart(2, "0") + "-" +
            String(d.getDate()).padStart(2, "0");
    }

    function inDays(n) {
        var d = new Date();
        d.setDate(d.getDate() + n);
        return ymd(d);
    }

    function newKey() {
        var raw = (window.crypto && crypto.randomUUID)
            ? crypto.randomUUID()
            : Date.now().toString(16) + Math.random().toString(16).slice(2);
        return raw.replace(/-/g, "").slice(0, 32);
    }

    function modal(id) { return bootstrap.Modal.getOrCreateInstance($(id)); }

    // Hide one modal, then show another (avoids Bootstrap backdrop glitches).
    function swap(fromId, toId) {
        var from = $(fromId);
        if (!from || !from.classList.contains("show")) { modal(toId).show(); return; }
        from.addEventListener("hidden.bs.modal", function () { modal(toId).show(); }, { once: true });
        modal(fromId).hide();
    }

    async function post(url, formData) {
        var res = await fetch(url, {
            method: "POST",
            body: formData,
            headers: { "X-Requested-With": "XMLHttpRequest" }
        });

        var data = null;
        try { data = await res.json(); } catch (e) { }

        if (!res.ok || !data || !data.success) {
            throw new Error((data && data.error) || "Something went wrong. Please try again.");
        }
        return data;
    }

    async function loadHtml(url) {
        var res = await fetch(url, { headers: { "X-Requested-With": "XMLHttpRequest" } });
        if (!res.ok) throw new Error("Could not load the details. Please try again.");
        return res.text();
    }

    var reloadOnClose = false;

    // ---------- receipt + details ----------
    async function openReceipt(saleId, paymentId, fromId) {
        var html = await loadHtml(URL.receipt + "/" + saleId + (paymentId ? "?paymentId=" + paymentId : ""));
        $("txReceiptBody").innerHTML = html;
        if (fromId) swap(fromId, "txReceiptModal"); else modal("txReceiptModal").show();
    }

    async function openDetails(saleId) {
        var body = $("txDetailsBody");
        body.innerHTML = '<div class="tx-loading"><span class="spinner-border spinner-border-sm"></span> Loading…</div>';
        modal("txDetailsModal").show();

        try {
            body.innerHTML = await loadHtml(URL.details + "/" + saleId + "?returnTo=" + encodeURIComponent(RETURN_TO));
        } catch (e) {
            body.innerHTML = '<div class="tx-loading">' + esc(e.message) + "</div>";
        }
    }

    $("txReceiptModal").addEventListener("hidden.bs.modal", function () {
        if (reloadOnClose) window.location.reload();
    });

    // ---------- pay flow (inside the details modal) ----------
    function payError(msg) {
        var box = $("txPayError");
        if (!box) return;
        box.textContent = msg || "";
        box.hidden = !msg;
    }

    function payStep(n) {
        $("txPayStep1").hidden = n !== 1;
        $("txPayStep2").hidden = n !== 2;
    }

    function payReview() {
        var f = $("txPayForm");
        var max = parseFloat(f.dataset.max) || 0;
        var amount = r2(parseFloat(f.elements.amount.value) || 0);

        if (!(amount > 0)) return payError("Enter a payment amount.");
        if (amount > max + 0.001) return payError("Payment can't be more than the balance (" + peso(max) + ").");

        payError("");
        var left = Math.max(0, r2(max - amount));
        $("txPayConfirmText").innerHTML =
            '<div class="tx-row"><span>Payment</span><strong>' + peso(amount) + "</strong></div>" +
            '<div class="tx-row"><span>Method</span><strong>Cash</strong></div>' +
            '<div class="tx-row tx-row-result"><span>Balance after</span><strong>' + peso(left) + "</strong></div>" +
            '<p class="tx-muted mb-0 mt-2">' + (left === 0
                ? "This will fully settle the utang."
                : "A receipt will be generated after you confirm.") + "</p>";
        payStep(2);
    }

    async function payConfirm(btn) {
        var f = $("txPayForm");
        var old = btn.innerHTML;
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Saving…';

        try {
            var data = await post(URL.pay, new FormData(f));
            reloadOnClose = true;
            await openReceipt(data.saleId, data.paymentId, "txDetailsModal");
        } catch (e) {
            payStep(1);
            payError(e.message);
        } finally {
            btn.disabled = false;
            btn.innerHTML = old;
        }
    }

    function onAction(e) {
        var t = e.target.closest("[data-tx-action]");
        if (!t) return;

        switch (t.dataset.txAction) {
            case "receipt":
                openReceipt(t.dataset.sale, t.dataset.payment, "txDetailsModal")
                    .catch(function (err) { alert(err.message); });
                break;
            case "print": window.print(); break;
            case "pay-full":
                var f = $("txPayForm");
                f.elements.amount.value = f.dataset.max;
                break;
            case "pay-review": payReview(); break;
            case "pay-back": payStep(1); break;
            case "pay-confirm": payConfirm(t); break;
            case "del-start":
            case "del-cancel":
                var wrap = t.closest(".tx-del");
                var starting = t.dataset.txAction === "del-start";
                wrap.querySelector('[data-tx-action="del-start"]').hidden = starting;
                wrap.querySelector(".tx-del-ask").hidden = !starting;
                break;
        }
    }

    $("txDetailsBody").addEventListener("click", onAction);
    $("txReceiptBody").addEventListener("click", onAction);

    document.addEventListener("click", function (e) {
        var b = e.target.closest("[data-tx-view]");
        if (!b) return;
        e.preventDefault();
        openDetails(b.dataset.txView);
    });

    // /Utang?highlight=ID (from overdue notifications)
    var hl = new URLSearchParams(window.location.search).get("highlight");
    if (hl) {
        var hlBtn = document.querySelector('[data-utang-id="' + CSS.escape(hl) + '"] [data-tx-view]');
        if (hlBtn) hlBtn.click();
    }

    // ---------- new transaction ----------
    var nm = $("newSaleModal");
    if (!nm) return;

    var form = $("saleForm");
    var cart = $("txCart");
    var cartEmpty = $("txCartEmpty");
    var errBox = $("txError");
    var stepEdit = $("txStepEdit");
    var stepReview = $("txStepReview");
    var btnReview = $("txReview");
    var btnBack = $("txBack");
    var btnConfirm = $("txConfirm");
    var btnCancel = $("txCancel");
    var search = $("txSearch");
    var productBtns = Array.prototype.slice.call(document.querySelectorAll(".tx-product"));
    var noProducts = $("txNoProducts");
   var el = {
    total: $("txTotal"), paid: $("txPaid"), resLabel: $("txResultLabel"), result: $("txResult"),
    due: $("txDue"), dueWrap: $("txDueWrap"), cust: $("txCustomer"), custHint: $("txCustomerHint"),
    note: $("txUtangNote"), quick: $("txQuick"),
    walkIn: $("txWalkIn"), paidLabel: $("txPaidLabel"), paidHint: $("txPaidHint")
};

    var items = [];
    var category = "all";
    var step = "edit";
    var busy = false;

    function type() {
        var c = form.querySelector('input[name="TransactionType"]:checked');
        return c ? c.value : "Cash";
    }
    function total() { return r2(items.reduce(function (s, i) { return s + i.price * i.qty; }, 0)); }
    function paid() { return Math.max(0, r2(parseFloat(el.paid.value) || 0)); }

    function showError(msg) {
        errBox.textContent = msg || "";
        errBox.hidden = !msg;
        if (msg) errBox.scrollIntoView({ block: "nearest", behavior: "smooth" });
    }

    // ----- catalog -----
    function filterProducts() {
        var q = search.value.trim().toLowerCase();
        var shown = 0;
        productBtns.forEach(function (b) {
            var ok = (category === "all" || b.dataset.cat === category) &&
                     (!q || b.dataset.search.indexOf(q) !== -1);
            b.hidden = !ok;
            if (ok) shown++;
        });
        noProducts.hidden = shown !== 0;
    }

    function addProduct(btn) {
        var id = btn.dataset.id;
        var stock = parseInt(btn.dataset.stock, 10) || 0;
        var existing = items.find(function (i) { return i.id === id; });

        if (existing) {
            if (existing.qty >= existing.stock) return showError("Only " + existing.stock + " of " + existing.name + " in stock.");
            existing.qty++;
        } else {
            items.push({
                id: id, name: btn.dataset.name,
                price: parseFloat(btn.dataset.price) || 0,
                stock: stock, qty: 1
            });
        }
        showError("");
        renderCart();
    }

    $("txProducts").addEventListener("click", function (e) {
        var b = e.target.closest(".tx-product");
        if (b) addProduct(b);
    });

    $("txCats").addEventListener("click", function (e) {
        var chip = e.target.closest(".tx-chip");
        if (!chip) return;
        category = chip.dataset.cat;
        $("txCats").querySelectorAll(".tx-chip").forEach(function (c) { c.classList.toggle("active", c === chip); });
        filterProducts();
    });

    search.addEventListener("input", filterProducts);
    search.addEventListener("keydown", function (e) {
        if (e.key !== "Enter") return;
        e.preventDefault();
        var first = productBtns.find(function (b) { return !b.hidden; });
        if (first) { addProduct(first); search.select(); }
    });

    // ----- cart -----
    function setQty(item, value) {
        var q = parseInt(value, 10);
        if (!isFinite(q) || q < 1) q = 1;
        if (q > item.stock) { q = item.stock; showError("Only " + item.stock + " of " + item.name + " in stock."); }
        item.qty = q;
        renderCart();
    }

    function renderCart() {
        cart.querySelectorAll(".tx-line").forEach(function (n) { n.remove(); });
        cartEmpty.hidden = items.length > 0;

        items.forEach(function (it) {
            var row = document.createElement("div");
            row.className = "tx-line";
            row._item = it;
            row.innerHTML =
                '<div class="tx-line-name"><strong>' + esc(it.name) + "</strong><small>" + peso(it.price) + " each</small></div>" +
                '<div class="tx-qty">' +
                '<button type="button" data-step="-1" aria-label="Decrease">−</button>' +
                '<input type="number" name="Quantity" min="1" max="' + it.stock + '" value="' + it.qty + '" inputmode="numeric" />' +
                '<button type="button" data-step="1" aria-label="Increase">+</button></div>' +
                '<div class="tx-line-sub">' + peso(it.price * it.qty) + "</div>" +
                '<button type="button" class="tx-remove" aria-label="Remove ' + esc(it.name) + '"><i class="bi bi-x-lg"></i></button>' +
                '<input type="hidden" name="ProductId" value="' + it.id + '" />';
            cart.appendChild(row);
        });

        updateTotals();
    }

    cart.addEventListener("click", function (e) {
        var row = e.target.closest(".tx-line");
        if (!row) return;
        var it = row._item;

        if (e.target.closest(".tx-remove")) {
            items = items.filter(function (x) { return x !== it; });
            return renderCart();
        }
        var s = e.target.closest("[data-step]");
        if (s) setQty(it, it.qty + Number(s.dataset.step));
    });

    cart.addEventListener("change", function (e) {
        var row = e.target.closest(".tx-line");
        if (row && e.target.name === "Quantity") setQty(row._item, e.target.value);
    });

    // ----- totals / type -----
    function updateTotals() {
        var utang = type() === "Utang";
        var t = total(), p = paid();

        el.total.textContent = peso(t);
        el.dueWrap.hidden = !utang;
        el.note.hidden = !utang;
        el.quick.hidden = utang;
el.custHint.textContent = utang ? "Required for utang." : "Optional for cash sales.";

// Utang needs a real customer: the walk-in choice becomes a disabled placeholder.
el.walkIn.textContent = utang ? "Select a customer…" : "Walk-in customer";
el.walkIn.disabled = utang;

// Utang payment is a partial payment, not the full amount.
el.paidLabel.textContent = utang ? "Partial payment (cash)" : "Amount paid (cash)";
el.paidHint.hidden = !utang;
        if (utang) {
            el.resLabel.textContent = "Remaining balance";
            el.result.textContent = peso(Math.max(0, t - p));
        } else if (p >= t) {
            el.resLabel.textContent = "Change";
            el.result.textContent = peso(p - t);
        } else {
            el.resLabel.textContent = "Still needed";
            el.result.textContent = peso(t - p);
        }

        btnReview.disabled = items.length === 0;
    }

    form.querySelectorAll('input[name="TransactionType"]').forEach(function (r) {
        r.addEventListener("change", function () {
            if (type() === "Utang" && !el.due.value) el.due.value = inDays(7);
            showError("");
            updateTotals();
        });
    });

    el.paid.addEventListener("input", updateTotals);

    el.quick.addEventListener("click", function (e) {
        var b = e.target.closest("[data-quick]");
        if (!b) return;
        el.paid.value = b.dataset.quick === "exact" ? total().toFixed(2) : b.dataset.quick;
        updateTotals();
    });

    nm.querySelector("#txDueWrap").addEventListener("click", function (e) {
        var b = e.target.closest("[data-days]");
        if (b) el.due.value = inDays(parseInt(b.dataset.days, 10));
    });

    // ----- validation + review -----
    function validate() {
        var utang = type() === "Utang", t = total(), p = paid();

        if (!items.length) return "Add at least one product.";

        if (utang) {
            if (!el.cust.value) return "Select a customer for utang.";
            if (!el.due.value) return "Choose a due date.";
            if (el.due.value < ymd(new Date())) return "Due date can't be in the past.";
            if (p >= t) return "The amount paid covers the total. Record it as a Cash Sale instead.";
        } else if (p + 0.005 < t) {
            return "Amount paid must cover the total.";
        }

        return null;
    }

    function setStep(next) {
        step = next;
        var review = next === "review";
        stepEdit.hidden = review;
        stepReview.hidden = !review;
        btnReview.hidden = review;
        btnBack.hidden = !review;
        btnConfirm.hidden = !review;
        btnCancel.hidden = review;
        $("txStepLabel").textContent = review ? "Step 2 of 2 · Check and confirm" : "Step 1 of 2 · Build the order";
    }

    function renderReview() {
        var utang = type() === "Utang", t = total(), p = paid();
        var custName = el.cust.value ? el.cust.selectedOptions[0].textContent.trim() : "Walk-in customer";
        var applied = utang ? p : t;

        var rows = items.map(function (i) {
            return "<tr><td>" + esc(i.name) + '</td><td class="text-center">' + i.qty +
                '</td><td class="text-end">' + peso(i.price) + '</td><td class="text-end"><strong>' +
                peso(i.price * i.qty) + "</strong></td></tr>";
        }).join("");

        stepReview.innerHTML =
            '<div class="tx-note"><i class="bi bi-shield-check"></i><span>Check the details below. Confirming saves the record, deducts stock and creates a receipt.</span></div>' +
            '<div class="tx-info-grid">' +
            '<div class="tx-info"><span>Type</span><strong>' + (utang ? "Utang (credit)" : "Cash sale") + "</strong></div>" +
            '<div class="tx-info"><span>Customer</span><strong>' + esc(custName) + "</strong></div>" +
            (utang ? '<div class="tx-info"><span>Due date</span><strong>' + esc(el.due.value) + "</strong></div>" : "") +
            '<div class="tx-info"><span>Payment</span><strong>Cash</strong></div></div>' +
            '<table class="tx-table"><thead><tr><th>Product</th><th class="text-center">Qty</th><th class="text-end">Price</th><th class="text-end">Subtotal</th></tr></thead><tbody>' + rows + "</tbody></table>" +
            '<div class="tx-summary">' +
            '<div class="tx-row"><span>Total</span><strong>' + peso(t) + "</strong></div>" +
'<div class="tx-row"><span>' + (utang ? "Partial payment" : "Paid now") + '</span><strong class="text-success">' + peso(applied) + "</strong></div>" +            '<div class="tx-row tx-row-result"><span>' + (utang ? "Balance (utang)" : "Change") + "</span><strong>" +
            peso(utang ? t - p : Math.max(0, p - t)) + "</strong></div></div>";
    }

    btnReview.addEventListener("click", function () {
        var msg = validate();
        if (msg) return showError(msg);
        showError("");
        renderReview();
        setStep("review");
    });

    btnBack.addEventListener("click", function () { setStep("edit"); });

    form.addEventListener("submit", function (e) {
        e.preventDefault();
        if (step === "edit") btnReview.click(); else btnConfirm.click();
    });

    // ----- submit -----
    btnConfirm.addEventListener("click", async function () {
        if (busy) return;

        var msg = validate();
        if (msg) { setStep("edit"); return showError(msg); }

        busy = true;
        var old = btnConfirm.innerHTML;
        btnConfirm.disabled = true;
        btnBack.disabled = true;
        btnConfirm.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Saving…';

        try {
            var data = await post(URL.create, new FormData(form));
            reloadOnClose = true;
            await openReceipt(data.saleId, data.paymentId, "newSaleModal");
        } catch (e) {
            setStep("edit");
            showError(e.message);
        } finally {
            busy = false;
            btnConfirm.disabled = false;
            btnBack.disabled = false;
            btnConfirm.innerHTML = old;
        }
    });

    // ----- reset -----
    function reset() {
        items = [];
        category = "all";
        search.value = "";
        el.paid.value = "0";
        el.cust.value = "";
        el.due.value = "";
        el.due.min = ymd(new Date());
        $("txKey").value = newKey();

        var def = nm.dataset.defaultType || "Cash";
        var radio = form.querySelector('input[name="TransactionType"][value="' + def + '"]');
        if (radio) radio.checked = true;
        if (def === "Utang") el.due.value = inDays(7);

        $("txCats").querySelectorAll(".tx-chip").forEach(function (c) {
            c.classList.toggle("active", c.dataset.cat === "all");
        });

        showError("");
        setStep("edit");
        filterProducts();
        renderCart();
    }

    nm.addEventListener("hidden.bs.modal", reset);
    nm.addEventListener("shown.bs.modal", function () { search.focus(); });

    reset();
})();