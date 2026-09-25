using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IID.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDealershipEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Dealerships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dealerships", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_Dealership_Code",
                table: "Dealerships",
                column: "Code",
                unique: true);

            // Insert initial dealerships so existing vehicles can reference a valid DealershipId
            var now = DateTimeOffset.UtcNow;
            migrationBuilder.InsertData(
                table: "Dealerships",
                columns: new[] { "Id", "Name", "Code", "City", "State", "Phone", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111101"), "Apex Motors - Downtown", "DLR-LA-01", "Los Angeles", "CA", "(213) 555-0101", now, now },
                    { new Guid("11111111-1111-1111-1111-111111111102"), "Metro Auto Group - North", "DLR-SEA-02", "Seattle", "WA", "(206) 555-0102", now, now },
                    { new Guid("11111111-1111-1111-1111-111111111103"), "Summit Luxury Cars - Westside", "DLR-DEN-03", "Denver", "CO", "(303) 555-0103", now, now },
                    { new Guid("11111111-1111-1111-1111-111111111104"), "Pinnacle Ford Lincoln", "DLR-DAL-04", "Dallas", "TX", "(214) 555-0104", now, now },
                    { new Guid("11111111-1111-1111-1111-111111111105"), "Grand Horizon Toyota", "DLR-PHX-05", "Phoenix", "AZ", "(602) 555-0105", now, now },
                    { new Guid("11111111-1111-1111-1111-111111111106"), "Velocity Performance Autos", "DLR-MIA-06", "Miami", "FL", "(305) 555-0106", now, now },
                    { new Guid("11111111-1111-1111-1111-111111111107"), "Coastal Bay Auto Gallery", "DLR-SFO-07", "San Francisco", "CA", "(415) 555-0107", now, now },
                    { new Guid("11111111-1111-1111-1111-111111111108"), "Heritage Classic & Pre-Owned", "DLR-CHI-08", "Chicago", "IL", "(312) 555-0108", now, now },
                    { new Guid("11111111-1111-1111-1111-111111111109"), "Frontier Auto Plaza", "DLR-ATX-09", "Austin", "TX", "(512) 555-0109", now, now },
                    { new Guid("11111111-1111-1111-1111-111111111110"), "Silverstone Motor Cars", "DLR-ATL-10", "Atlanta", "GA", "(404) 555-0110", now, now }
                });

            migrationBuilder.AddColumn<Guid>(
                name: "DealershipId",
                table: "Vehicle",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("11111111-1111-1111-1111-111111111101"));

            migrationBuilder.CreateIndex(
                name: "IX_Vehicle_DealershipId",
                table: "Vehicle",
                column: "DealershipId");

            migrationBuilder.AddForeignKey(
                name: "FK_Vehicle_Dealerships_DealershipId",
                table: "Vehicle",
                column: "DealershipId",
                principalTable: "Dealerships",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Vehicle_Dealerships_DealershipId",
                table: "Vehicle");

            migrationBuilder.DropTable(
                name: "Dealerships");

            migrationBuilder.DropIndex(
                name: "IX_Vehicle_DealershipId",
                table: "Vehicle");

            migrationBuilder.DropColumn(
                name: "DealershipId",
                table: "Vehicle");
        }
    }
}
