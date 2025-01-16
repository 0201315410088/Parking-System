using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace testsubject.Migrations
{
    /// <inheritdoc />
    public partial class fghj : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmissionDate",
                table: "CarWashServices",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "CarWashServices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "CarWashServices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "CarWashServices",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "CarWashServices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "CarWashServices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "CarWashServices",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "CarWashServices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Username",
                table: "CarWashServices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProfileEmail",
                table: "CarWashBookings",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CarWashBookings_ProfileEmail",
                table: "CarWashBookings",
                column: "ProfileEmail");

            migrationBuilder.AddForeignKey(
                name: "FK_CarWashBookings_Profiles_ProfileEmail",
                table: "CarWashBookings",
                column: "ProfileEmail",
                principalTable: "Profiles",
                principalColumn: "Email",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarWashBookings_Profiles_ProfileEmail",
                table: "CarWashBookings");

            migrationBuilder.DropIndex(
                name: "IX_CarWashBookings_ProfileEmail",
                table: "CarWashBookings");

            migrationBuilder.DropColumn(
                name: "City",
                table: "CarWashServices");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "CarWashServices");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "CarWashServices");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "CarWashServices");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "CarWashServices");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "CarWashServices");

            migrationBuilder.DropColumn(
                name: "State",
                table: "CarWashServices");

            migrationBuilder.DropColumn(
                name: "Username",
                table: "CarWashServices");

            migrationBuilder.DropColumn(
                name: "ProfileEmail",
                table: "CarWashBookings");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SubmissionDate",
                table: "CarWashServices",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");
        }
    }
}
