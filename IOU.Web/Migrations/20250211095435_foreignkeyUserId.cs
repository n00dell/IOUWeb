using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IOU.Web.Migrations
{
    /// <inheritdoc />
    public partial class foreignkeyUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Guardian_AspNetUsers_UserId1",
                table: "Guardian");

            migrationBuilder.DropForeignKey(
                name: "FK_Lender_AspNetUsers_UserId1",
                table: "Lender");

            migrationBuilder.DropForeignKey(
                name: "FK_Student_AspNetUsers_UserId1",
                table: "Student");

            migrationBuilder.DropIndex(
                name: "IX_Student_UserId1",
                table: "Student");

            migrationBuilder.DropIndex(
                name: "IX_Lender_UserId1",
                table: "Lender");

            migrationBuilder.DropIndex(
                name: "IX_Guardian_UserId1",
                table: "Guardian");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Lender");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "Guardian");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpectedGraduationDate",
                table: "Student",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AddForeignKey(
                name: "FK_Guardian_AspNetUsers_UserId",
                table: "Guardian",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Lender_AspNetUsers_UserId",
                table: "Lender",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Student_AspNetUsers_UserId",
                table: "Student",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Guardian_AspNetUsers_UserId",
                table: "Guardian");

            migrationBuilder.DropForeignKey(
                name: "FK_Lender_AspNetUsers_UserId",
                table: "Lender");

            migrationBuilder.DropForeignKey(
                name: "FK_Student_AspNetUsers_UserId",
                table: "Student");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "ExpectedGraduationDate",
                table: "Student",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<string>(
                name: "UserId1",
                table: "Student",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId1",
                table: "Lender",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId1",
                table: "Guardian",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Student_UserId1",
                table: "Student",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_Lender_UserId1",
                table: "Lender",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_Guardian_UserId1",
                table: "Guardian",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Guardian_AspNetUsers_UserId1",
                table: "Guardian",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Lender_AspNetUsers_UserId1",
                table: "Lender",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Student_AspNetUsers_UserId1",
                table: "Student",
                column: "UserId1",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
