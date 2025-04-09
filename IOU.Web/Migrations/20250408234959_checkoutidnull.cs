using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOU.Web.Migrations
{
    /// <inheritdoc />
    public partial class checkoutidnull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_CheckoutRequestID",
                table: "Payments");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CheckoutRequestID",
                table: "Payments",
                column: "CheckoutRequestID",
                unique: true,
                filter: "[CheckoutRequestID] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_CheckoutRequestID",
                table: "Payments");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CheckoutRequestID",
                table: "Payments",
                column: "CheckoutRequestID",
                unique: true);
        }
    }
}
