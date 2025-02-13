using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOU.Web.Migrations
{
    /// <inheritdoc />
    public partial class initialdebt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Debt_Lender_LenderUserId",
                table: "Debt");

            migrationBuilder.DropForeignKey(
                name: "FK_Debt_Student_StudentUserId",
                table: "Debt");

            migrationBuilder.DropIndex(
                name: "IX_Debt_LenderUserId",
                table: "Debt");

            migrationBuilder.DropIndex(
                name: "IX_Debt_StudentUserId",
                table: "Debt");

            migrationBuilder.DropColumn(
                name: "LenderUserId",
                table: "Debt");

            migrationBuilder.DropColumn(
                name: "StudentUserId",
                table: "Debt");

            migrationBuilder.AddColumn<decimal>(
                name: "AccumulatedInterest",
                table: "Debt",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Debt",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentBalance",
                table: "Debt",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateIssued",
                table: "Debt",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DueDate",
                table: "Debt",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "GracePeriodDays",
                table: "Debt",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "InterestRate",
                table: "Debt",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LateFeeAmount",
                table: "Debt",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "LenderId",
                table: "Debt",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PrincipalAmount",
                table: "Debt",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "Debt",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Debt",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StudentId",
                table: "Debt",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Debt_LenderId",
                table: "Debt",
                column: "LenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Debt_StudentId",
                table: "Debt",
                column: "StudentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Debt_Lender_LenderId",
                table: "Debt",
                column: "LenderId",
                principalTable: "Lender",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Debt_Student_StudentId",
                table: "Debt",
                column: "StudentId",
                principalTable: "Student",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Debt_Lender_LenderId",
                table: "Debt");

            migrationBuilder.DropForeignKey(
                name: "FK_Debt_Student_StudentId",
                table: "Debt");

            migrationBuilder.DropIndex(
                name: "IX_Debt_LenderId",
                table: "Debt");

            migrationBuilder.DropIndex(
                name: "IX_Debt_StudentId",
                table: "Debt");

            migrationBuilder.DropColumn(
                name: "AccumulatedInterest",
                table: "Debt");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Debt");

            migrationBuilder.DropColumn(
                name: "CurrentBalance",
                table: "Debt");

            migrationBuilder.DropColumn(
                name: "DateIssued",
                table: "Debt");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "Debt");

            migrationBuilder.DropColumn(
                name: "GracePeriodDays",
                table: "Debt");

            migrationBuilder.DropColumn(
                name: "InterestRate",
                table: "Debt");

            migrationBuilder.DropColumn(
                name: "LateFeeAmount",
                table: "Debt");

            migrationBuilder.DropColumn(
                name: "LenderId",
                table: "Debt");

            migrationBuilder.DropColumn(
                name: "PrincipalAmount",
                table: "Debt");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "Debt");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Debt");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "Debt");

            migrationBuilder.AddColumn<string>(
                name: "LenderUserId",
                table: "Debt",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentUserId",
                table: "Debt",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Debt_LenderUserId",
                table: "Debt",
                column: "LenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Debt_StudentUserId",
                table: "Debt",
                column: "StudentUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Debt_Lender_LenderUserId",
                table: "Debt",
                column: "LenderUserId",
                principalTable: "Lender",
                principalColumn: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Debt_Student_StudentUserId",
                table: "Debt",
                column: "StudentUserId",
                principalTable: "Student",
                principalColumn: "UserId");
        }
    }
}
