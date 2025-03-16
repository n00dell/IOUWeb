using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOU.Web.Migrations
{
    /// <inheritdoc />
    public partial class rejectlender : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Debt_Lender_LenderUserId",
                table: "Debt");

            migrationBuilder.AddForeignKey(
                name: "FK_Debt_Lender_LenderUserId",
                table: "Debt",
                column: "LenderUserId",
                principalTable: "Lender",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Debt_Lender_LenderUserId",
                table: "Debt");

            migrationBuilder.AddForeignKey(
                name: "FK_Debt_Lender_LenderUserId",
                table: "Debt",
                column: "LenderUserId",
                principalTable: "Lender",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
