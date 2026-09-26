document.addEventListener("DOMContentLoaded", function () {

    const container =
        document.getElementById("saleProductsContainer");

    const addButton =
        document.getElementById("addSaleProduct");


    if (!container || !addButton) {
        return;
    }


    // ADD PRODUCT ROW
    addButton.addEventListener("click", function () {

        const firstRow =
            container.querySelector(".sale-product-row");

        const newRow =
            firstRow.cloneNode(true);


        // Reset product
        const productSelect =
            newRow.querySelector(".sale-product-select");

        productSelect.value = "";


        // Reset quantity
        const quantityInput =
            newRow.querySelector("input[name='Quantity']");

        quantityInput.value = 1;


        container.appendChild(newRow);

    });


    // REMOVE PRODUCT ROW
    container.addEventListener("click", function (event) {

        const button =
            event.target.closest(".remove-sale-product");

        if (!button) {
            return;
        }


        const rows =
            container.querySelectorAll(".sale-product-row");


        // Keep at least one row
        if (rows.length <= 1) {
            return;
        }


        button.closest(".sale-product-row").remove();

    });


    // CHECK STOCK
    container.addEventListener("change", function (event) {

        if (!event.target.classList.contains("sale-product-select")) {
            return;
        }


        const selectedOption =
            event.target.options[event.target.selectedIndex];

        const stock =
            Number(selectedOption.getAttribute("data-stock"));


        const row =
            event.target.closest(".sale-product-row");

        const quantityInput =
            row.querySelector("input[name='Quantity']");


        if (stock > 0) {

            quantityInput.max = stock;

        } else {

            quantityInput.removeAttribute("max");

        }

    });

});