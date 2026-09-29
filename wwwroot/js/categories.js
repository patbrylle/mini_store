document.addEventListener("DOMContentLoaded", function () {

    // =========================================================
    // CATEGORY SEARCH
    // =========================================================

    const searchInput =
        document.getElementById("categorySearch");

    const categoryRows =
        document.querySelectorAll(".category-row");


    if (searchInput) {

        searchInput.addEventListener("input", function () {

            const searchValue =
                this.value.toLowerCase().trim();


            categoryRows.forEach(function (row) {

                const categoryText =
                    row.textContent.toLowerCase();


                if (categoryText.includes(searchValue)) {

                    row.style.display = "";

                } else {

                    row.style.display = "none";

                }

            });

        });

    }


    // =========================================================
    // ADD CATEGORY MODAL
    // =========================================================

    const addCategoryModal =
        document.getElementById("addCategoryModal");


    if (addCategoryModal) {

        addCategoryModal.addEventListener(
            "hidden.bs.modal",
            function () {

                const form =
                    addCategoryModal.querySelector("form");


                if (form) {

                    form.reset();

                }

            }
        );

    }

});