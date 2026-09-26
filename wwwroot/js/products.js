document.addEventListener("DOMContentLoaded", function () {

    const addProductModal =
        document.getElementById("addProductModal");

    if (addProductModal) {

        addProductModal.addEventListener(
            "shown.bs.modal",
            function () {

                const productName =
                    addProductModal.querySelector(
                        'input[name="ProductName"]'
                    );

                if (productName) {
                    productName.focus();
                }

            }
        );

    }

});