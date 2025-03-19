using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOU.Web.Migrations
{
    /// <inheritdoc />
    public partial class PaymentsDbSet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payment_Debt_DebtId",
                table: "Payment");

            migrationBuilder.DropForeignKey(
                name: "FK_Payment_ScheduledPayment_ScheduledPaymentId",
                table: "Payment");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Payment",
                table: "Payment");

            migrationBuilder.RenameTable(
                name: "Payment",
                newName: "Payments");

            migrationBuilder.RenameIndex(
                name: "IX_Payment_ScheduledPaymentId",
                table: "Payments",
                newName: "IX_Payments_ScheduledPaymentId");

            migrationBuilder.RenameIndex(
                name: "IX_Payment_DebtId",
                table: "Payments",
                newName: "IX_Payments_DebtId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Payments",
                table: "Payments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Debt_DebtId",
                table: "Payments",
                column: "DebtId",
                principalTable: "Debt",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_ScheduledPayment_ScheduledPaymentId",
                table: "Payments",
                column: "ScheduledPaymentId",
                principalTable: "ScheduledPayment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Debt_DebtId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_ScheduledPayment_ScheduledPaymentId",
                table: "Payments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Payments",
                table: "Payments");

            migrationBuilder.RenameTable(
                name: "Payments",
                newName: "Payment");

            migrationBuilder.RenameIndex(
                name: "IX_Payments_ScheduledPaymentId",
                table: "Payment",
                newName: "IX_Payment_ScheduledPaymentId");

            migrationBuilder.RenameIndex(
                name: "IX_Payments_DebtId",
                table: "Payment",
                newName: "IX_Payment_DebtId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Payment",
                table: "Payment",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Payment_Debt_DebtId",
                table: "Payment",
                column: "DebtId",
                principalTable: "Debt",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Payment_ScheduledPayment_ScheduledPaymentId",
                table: "Payment",
                column: "ScheduledPaymentId",
                principalTable: "ScheduledPayment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
