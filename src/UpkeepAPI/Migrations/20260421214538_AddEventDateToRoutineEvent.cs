using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UpkeepAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddEventDateToRoutineEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int[]>(
                name: "DaysOfWeek",
                table: "RoutineEvents",
                type: "integer[]",
                nullable: true,
                oldClrType: typeof(int[]),
                oldType: "integer[]");

            migrationBuilder.AddColumn<DateOnly>(
                name: "EventDate",
                table: "RoutineEvents",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventDate",
                table: "RoutineEvents");

            migrationBuilder.AlterColumn<int[]>(
                name: "DaysOfWeek",
                table: "RoutineEvents",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0],
                oldClrType: typeof(int[]),
                oldType: "integer[]",
                oldNullable: true);
        }
    }
}
