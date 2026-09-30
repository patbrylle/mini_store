using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mini_store.Migrations
{
    /// <inheritdoc />
    public partial class AddUtangDueDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Utangs",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Utangs");
        }
    }
}
