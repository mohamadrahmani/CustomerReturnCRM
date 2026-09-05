using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CustomerReturnCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DefaultDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    SuggestedReturnDays = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceTemplates", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ServiceTemplates",
                columns: new[] { "Id", "BusinessType", "CreatedAt", "DefaultDurationMinutes", "IsActive", "SuggestedReturnDays", "Title", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("a1000000-0000-0000-0000-000000000001"), "General", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 60, true, 30, "General consultation", null },
                    { new Guid("a1000000-0000-0000-0000-000000000002"), "BeautySalon", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 120, true, 60, "Hair coloring", null },
                    { new Guid("a1000000-0000-0000-0000-000000000003"), "BeautySalon", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 90, true, 21, "Nail service", null },
                    { new Guid("a1000000-0000-0000-0000-000000000004"), "BeautySalon", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 60, true, 30, "Facial", null },
                    { new Guid("a1000000-0000-0000-0000-000000000005"), "BeautySalon", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 45, true, 45, "Haircut", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTemplates_BusinessType_Title",
                table: "ServiceTemplates",
                columns: new[] { "BusinessType", "Title" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceTemplates");
        }
    }
}
