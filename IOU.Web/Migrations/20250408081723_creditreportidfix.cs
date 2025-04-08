using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOU.Web.Migrations
{
    /// <inheritdoc />
    public partial class creditreportidfix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditReportRequests_CreditReports_CreditReportId",
                table: "CreditReportRequests");

            migrationBuilder.AlterColumn<string>(
                name: "CreditReportId",
                table: "CreditReportRequests",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditReportRequests_CreditReports_CreditReportId",
                table: "CreditReportRequests",
                column: "CreditReportId",
                principalTable: "CreditReports",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditReportRequests_CreditReports_CreditReportId",
                table: "CreditReportRequests");

            migrationBuilder.AlterColumn<string>(
                name: "CreditReportId",
                table: "CreditReportRequests",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CreditReportRequests_CreditReports_CreditReportId",
                table: "CreditReportRequests",
                column: "CreditReportId",
                principalTable: "CreditReports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
