using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOU.Web.Migrations
{
    /// <inheritdoc />
    public partial class credit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportAccessRequests");

            migrationBuilder.AddColumn<int>(
                name: "DaysLate",
                table: "ScheduledPayment",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsLate",
                table: "ScheduledPayment",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "ScheduledPayment",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "CreditReports",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StudentUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GeneratedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreditScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActiveDebtsCount = table.Column<int>(type: "int", nullable: false),
                    TotalDebtObligations = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentCompletionRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AveragePaymentDelayDays = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RiskCategory = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditReports_Student_StudentUserId",
                        column: x => x.StudentUserId,
                        principalTable: "Student",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CreditReportRequests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LenderUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StudentEmail = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResponseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: true),
                    Purpose = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreditReportId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditReportRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditReportRequests_CreditReports_CreditReportId",
                        column: x => x.CreditReportId,
                        principalTable: "CreditReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditReportRequests_Lender_LenderUserId",
                        column: x => x.LenderUserId,
                        principalTable: "Lender",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditReportRequests_CreditReportId",
                table: "CreditReportRequests",
                column: "CreditReportId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditReportRequests_LenderUserId",
                table: "CreditReportRequests",
                column: "LenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditReportRequests_StudentEmail_LenderUserId",
                table: "CreditReportRequests",
                columns: new[] { "StudentEmail", "LenderUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_CreditReports_StudentUserId",
                table: "CreditReports",
                column: "StudentUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditReportRequests");

            migrationBuilder.DropTable(
                name: "CreditReports");

            migrationBuilder.DropColumn(
                name: "DaysLate",
                table: "ScheduledPayment");

            migrationBuilder.DropColumn(
                name: "IsLate",
                table: "ScheduledPayment");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "ScheduledPayment");

            migrationBuilder.CreateTable(
                name: "ReportAccessRequests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LenderUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StudentUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedReportType = table.Column<int>(type: "int", nullable: false),
                    ResponseDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportAccessRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportAccessRequests_Lender_LenderUserId",
                        column: x => x.LenderUserId,
                        principalTable: "Lender",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportAccessRequests_Student_StudentUserId",
                        column: x => x.StudentUserId,
                        principalTable: "Student",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportAccessRequests_LenderUserId",
                table: "ReportAccessRequests",
                column: "LenderUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportAccessRequests_StudentUserId",
                table: "ReportAccessRequests",
                column: "StudentUserId");
        }
    }
}
