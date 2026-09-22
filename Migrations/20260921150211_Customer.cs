using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mini_store.Migrations
{
    /// <inheritdoc />
    public partial class Customer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Productname",
                table: "Products",
                newName: "ProductName");

            migrationBuilder.RenameColumn(
                name: "productId",
                table: "Products",
                newName: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ProductName",
                table: "Products",
                newName: "Productname");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "Products",
                newName: "productId");
        }
    }
}
