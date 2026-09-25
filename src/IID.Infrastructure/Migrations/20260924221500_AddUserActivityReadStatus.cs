using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IID.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserActivityReadStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserActivityReadStatus",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ActivityId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserActivityReadStatus", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityReadStatus_UserId_ActivityId",
                table: "UserActivityReadStatus",
                columns: new[] { "UserId", "ActivityId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserActivityReadStatus");
        }
    }
}
