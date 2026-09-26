const addStockModal = document.getElementById("addStockModal");

if (addStockModal) {
    addStockModal.addEventListener("show.bs.modal", function (event) {

        const button = event.relatedTarget;

        const productId = button.getAttribute("data-product-id");
        const productName = button.getAttribute("data-product-name");
        const currentStock = button.getAttribute("data-current-stock");

        document.getElementById("modalProductId").value = productId;
        document.getElementById("modalProductName").value = productName;
        document.getElementById("modalCurrentStock").value = currentStock;
    });
}