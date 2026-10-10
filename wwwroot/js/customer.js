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