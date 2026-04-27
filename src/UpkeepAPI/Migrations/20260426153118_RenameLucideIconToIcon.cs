using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpkeepAPI.Migrations
{
    /// <inheritdoc />
    public partial class RenameLucideIconToIcon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LucideIcon",
                table: "Habits",
                newName: "Icon");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Icon",
                table: "Habits",
                newName: "LucideIcon");
        }
    }
}
