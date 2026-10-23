document.addEventListener("DOMContentLoaded", function () {

    // ==============================
    // CUSTOMER SEARCH
    // ==============================

    const searchInput = document.getElementById("customerSearch");
    const table = document.getElementById("customerTable");

    if (searchInput && table) {

        // Real customer rows only (skip the "No Customers" empty-state row)
        const customerRows = Array.from(
            table.querySelectorAll("tbody tr")
        ).filter(function (row) {
            return !row.querySelector("td[colspan]");
        });

        function filterCustomers() {
            const searchValue = searchInput.value.toLowerCase().trim();

            customerRows.forEach(function (row) {
                const customerText = row.textContent.toLowerCase();

                row.style.display = customerText.includes(searchValue)
                    ? ""
                    : "none";
            });
        }

        ["input", "keyup", "search"].forEach(function (evt) {
            searchInput.addEventListener(evt, filterCustomers);
        });
    }


    // ==============================
    // ADD CUSTOMER MODAL
    // ==============================

    const addCustomerModal = document.getElementById("addCustomerModal");

    if (addCustomerModal) {
        addCustomerModal.addEventListener("hidden.bs.modal", function () {
            const form = addCustomerModal.querySelector("form");

            if (form) {
                form.reset();
            }
        });
    }

});
    // ==============================
    // CUSTOMER HISTORY MODAL
    // ==============================

    const historyEl = document.getElementById("customerHistoryModal");

    if (historyEl) {
        const body = document.getElementById("customerHistoryBody");
        const title = document.getElementById("customerHistoryTitle");

        // load the history when a row's History button opens the modal
        historyEl.addEventListener("show.bs.modal", async function (e) {
            const btn = e.relatedTarget;
            if (!btn || !btn.dataset.url) return;

            title.textContent = (btn.dataset.name || "Customer") + " — History";
            body.innerHTML =
                '<div class="text-center py-5"><div class="spinner-border" role="status"></div></div>';

            try {
                const res = await fetch(btn.dataset.url, {
                    headers: { "X-Requested-With": "XMLHttpRequest" }
                });
                if (!res.ok) throw new Error("HTTP " + res.status);
                body.innerHTML = await res.text();
            } catch (err) {
                body.innerHTML =
                    '<p class="text-danger text-center py-4">Could not load the history. Please try again.</p>';
            }
        });

        // Bootstrap can't stack modals: close history first, then let the
        // shared transaction modal (transactions.js) open on the relayed click.
        historyEl.addEventListener("click", function (e) {
            const btn = e.target.closest("[data-tx-view]");
            if (!btn) return;

            if (btn.dataset.relay) {        // relayed click: let it bubble
                delete btn.dataset.relay;
                return;
            }

            e.stopPropagation();

            historyEl.addEventListener("hidden.bs.modal", function () {
                btn.dataset.relay = "1";
                btn.click();
            }, { once: true });

            bootstrap.Modal.getInstance(historyEl).hide();
        });
    }