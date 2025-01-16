using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace testsubject.Migrations
{
    /// <inheritdoc />
    public partial class tvybuni : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarWashBookings_CarWashServices_CarWashServiceId",
                table: "CarWashBookings");

            migrationBuilder.DropForeignKey(
                name: "FK_CarWashBookings_Profiles_ProfileEmail",
                table: "CarWashBookings");

            migrationBuilder.DropIndex(
                name: "IX_CarWashBookings_CarWashServiceId",
                table: "CarWashBookings");

            migrationBuilder.DropIndex(
                name: "IX_CarWashBookings_ProfileEmail",
                table: "CarWashBookings");

            migrationBuilder.DropColumn(
                name: "BookingDuration",
                table: "CarWashBookings");

            migrationBuilder.DropColumn(
                name: "CarWashServiceId",
                table: "CarWashBookings");

            migrationBuilder.DropColumn(
                name: "ProfileEmail",
                table: "CarWashBookings");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "CarWashServices",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "ServiceName",
                table: "CarWashBookings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServiceName",
                table: "CarWashBookings");

            migrationBuilder.AlterColumn<string>(
                name: "Username",
                table: "CarWashServices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "BookingDuration",
                table: "CarWashBookings",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "CarWashServiceId",
                table: "CarWashBookings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProfileEmail",
                table: "CarWashBookings",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CarWashBookings_CarWashServiceId",
                table: "CarWashBookings",
                column: "CarWashServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_CarWashBookings_ProfileEmail",
                table: "CarWashBookings",
                column: "ProfileEmail");

            migrationBuilder.AddForeignKey(
                name: "FK_CarWashBookings_CarWashServices_CarWashServiceId",
                table: "CarWashBookings",
                column: "CarWashServiceId",
                principalTable: "CarWashServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CarWashBookings_Profiles_ProfileEmail",
                table: "CarWashBookings",
                column: "ProfileEmail",
                principalTable: "Profiles",
                principalColumn: "Email",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
