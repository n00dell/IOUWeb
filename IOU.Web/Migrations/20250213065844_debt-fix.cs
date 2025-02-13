using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOU.Web.Migrations
{
    /// <inheritdoc />
    public partial class debtfix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Debt_Lender_LenderId",
                table: "Debt");

            migrationBuilder.DropForeignKey(
                name: "FK_Debt_Student_StudentId",
                table: "Debt");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Debt",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_Debt_Lender_LenderId",
                table: "Debt",
                column: "LenderId",
                principalTable: "Lender",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Debt_Student_StudentId",
                table: "Debt",
                column: "StudentId",
                principalTable: "Student",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Debt");

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
    }
}
