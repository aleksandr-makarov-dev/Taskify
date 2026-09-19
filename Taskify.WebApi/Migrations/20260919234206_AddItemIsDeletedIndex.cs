using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taskify.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddItemIsDeletedIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Items_IsDeleted",
                table: "Items",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Items_IsDeleted",
                table: "Items");
        }
    }
}
