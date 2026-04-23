using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpkeepAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddColorRemoveIsActiveFromRoutineEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "RoutineEvents");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "RoutineEvents",
                type: "character varying(7)",
                maxLength: 7,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "RoutineEvents");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "RoutineEvents",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
