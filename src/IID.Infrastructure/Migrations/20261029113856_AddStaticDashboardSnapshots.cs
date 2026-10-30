using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IID.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStaticDashboardSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DashboardSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DealershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SummaryJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuickStatsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChartsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionCenterJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AiInsightsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LowInventoryAlertsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RealtimeAlertsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ComputedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardSnapshots_DealershipId",
                table: "DashboardSnapshots",
                column: "DealershipId");

            migrationBuilder.CreateIndex(
                name: "UX_DashboardSnapshots_ScopeKey",
                table: "DashboardSnapshots",
                column: "ScopeKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DashboardSnapshots");
        }
    }
}
