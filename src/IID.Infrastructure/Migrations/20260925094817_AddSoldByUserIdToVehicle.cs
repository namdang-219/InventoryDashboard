using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IID.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSoldByUserIdToVehicle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SoldByUserId",
                table: "Vehicle",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SoldByUserId",
                table: "Vehicle");
        }
    }
}
