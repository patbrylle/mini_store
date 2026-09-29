document.addEventListener("DOMContentLoaded", function () {


// =========================================================
// UTANG SEARCH
// =========================================================

const searchInput =
    document.getElementById("utangSearch");

const utangTable =
    document.getElementById("utangTable");

if (searchInput && utangTable) {

    searchInput.addEventListener("input", function () {

        const searchValue =
            this.value.toLowerCase().trim();

        const rows =
            utangTable.querySelectorAll("tbody tr");

        rows.forEach(function (row) {

            const rowText =
                row.innerText.toLowerCase();

            row.style.display =
                rowText.includes(searchValue)
                    ? ""
                    : "none";
        });
    });
}


});
