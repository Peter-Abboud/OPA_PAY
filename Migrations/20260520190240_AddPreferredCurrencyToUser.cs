using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OPA_Pay.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferredCurrencyToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PreferredCurrencyId",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredCurrencyId",
                table: "AspNetUsers");
        }
    }
}
