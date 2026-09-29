document.addEventListener("DOMContentLoaded", function () {

    // ==============================
    // CUSTOMER SEARCH
    // ==============================

    const searchInput = document.getElementById("customerSearch");
    const customerRows = document.querySelectorAll(".customer-row");

    if (searchInput) {

        searchInput.addEventListener("input", function () {

            const searchValue = this.value.toLowerCase().trim();

            customerRows.forEach(function (row) {

                const customerText =
                    row.textContent.toLowerCase();

                if (customerText.includes(searchValue)) {
                    row.style.display = "";
                } else {
                    row.style.display = "none";
                }

            });

        });

    }


    // ==============================
    // ADD CUSTOMER MODAL
    // ==============================

    const addCustomerModal =
        document.getElementById("addCustomerModal");

    if (addCustomerModal) {

        addCustomerModal.addEventListener(
            "hidden.bs.modal",
            function () {

                const form =
                    addCustomerModal.querySelector("form");

                if (form) {
                    form.reset();
                }

            }
        );

    }

});