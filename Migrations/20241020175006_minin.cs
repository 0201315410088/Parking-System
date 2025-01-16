using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace testsubject.Migrations
{
    /// <inheritdoc />
    public partial class minin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RegAccept",
                table: "CarWashServices",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegAccept",
                table: "CarWashServices");
        }
    }
}
