using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOU.Web.Migrations
{
    /// <inheritdoc />
    public partial class creditreportrequeststd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "PaymentCompletionRate",
                table: "CreditReports",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AveragePaymentDelayDays",
                table: "CreditReports",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<string>(
                name: "StudentUserId",
                table: "CreditReportRequests",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CreditReportRequests_StudentUserId",
                table: "CreditReportRequests",
                column: "StudentUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditReportRequests_Student_StudentUserId",
                table: "CreditReportRequests",
                column: "StudentUserId",
                principalTable: "Student",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditReportRequests_Student_StudentUserId",
                table: "CreditReportRequests");

            migrationBuilder.DropIndex(
                name: "IX_CreditReportRequests_StudentUserId",
                table: "CreditReportRequests");

            migrationBuilder.DropColumn(
                name: "StudentUserId",
                table: "CreditReportRequests");

            migrationBuilder.AlterColumn<decimal>(
                name: "PaymentCompletionRate",
                table: "CreditReports",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "AveragePaymentDelayDays",
                table: "CreditReports",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);
        }
    }
}
