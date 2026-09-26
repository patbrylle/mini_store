document.addEventListener("DOMContentLoaded", function () {

    // ================================
    // EDIT CATEGORY
    // ================================

    const editCategoryModal =
        document.getElementById("editCategoryModal");

    if (editCategoryModal) {

        editCategoryModal.addEventListener(
            "show.bs.modal",
            function (event) {

                const button = event.relatedTarget;

                const categoryId =
                    button.getAttribute("data-category-id");

                const categoryName =
                    button.getAttribute("data-category-name");

                document.getElementById("editCategoryId").value =
                    categoryId;

                document.getElementById("editCategoryName").value =
                    categoryName;
            }
        );
    }


    // ================================
    // DELETE CATEGORY
    // ================================

    const deleteCategoryModal =
        document.getElementById("deleteCategoryModal");

    if (deleteCategoryModal) {

        deleteCategoryModal.addEventListener(
            "show.bs.modal",
            function (event) {

                const button = event.relatedTarget;

                const categoryId =
                    button.getAttribute("data-category-id");

                const categoryName =
                    button.getAttribute("data-category-name");

                document.getElementById("deleteCategoryId").value =
                    categoryId;

                document.getElementById("deleteCategoryName").textContent =
                    categoryName;

                document.getElementById("deleteCategoryNameInfo").textContent =
                    categoryName;
            }
        );
    }

});