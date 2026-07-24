using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OPA_Pay.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferPickupCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PickupCode",
                table: "Transfers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PickupCode",
                table: "Transfers");
        }
    }
}
