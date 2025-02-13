using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOU.Web.Migrations
{
    /// <inheritdoc />
    public partial class debtimprovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AccumulatedLateFees",
                table: "Debt",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CalculationPeriod",
                table: "Debt",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DebtType",
                table: "Debt",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InterestType",
                table: "Debt",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastInterestCalculationDate",
                table: "Debt",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccumulatedLateFees",
                table: "Debt");

            migrationBuilder.DropColumn(
                name: "CalculationPeriod",
                table: "Debt");

            migrationBuilder.DropColumn(
                name: "DebtType",
                table: "Debt");

            migrationBuilder.DropColumn(
                name: "InterestType",
                table: "Debt");

            migrationBuilder.DropColumn(
                name: "LastInterestCalculationDate",
                table: "Debt");
        }
    }
}
