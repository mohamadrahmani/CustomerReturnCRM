using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomerReturnCRM.Infrastructure.Persistence.Migrations;

public partial class AddAvailability : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BusinessWorkingHours",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DayOfWeek = table.Column<int>(type: "int", nullable: false),
                StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                EndTime = table.Column<TimeSpan>(type: "time", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BusinessWorkingHours", x => x.Id);
                table.ForeignKey("FK_BusinessWorkingHours_Businesses_BusinessId", x => x.BusinessId, "Businesses", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "StaffWorkingHours",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                StaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DayOfWeek = table.Column<int>(type: "int", nullable: false),
                StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                EndTime = table.Column<TimeSpan>(type: "time", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StaffWorkingHours", x => x.Id);
                table.ForeignKey("FK_StaffWorkingHours_Staff_StaffId", x => x.StaffId, "Staff", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "StaffTimeOffs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                StaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                StartAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                EndAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StaffTimeOffs", x => x.Id);
                table.ForeignKey("FK_StaffTimeOffs_Staff_StaffId", x => x.StaffId, "Staff", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BusinessWorkingHours_BusinessId_DayOfWeek_StartTime",
            table: "BusinessWorkingHours",
            columns: new[] { "BusinessId", "DayOfWeek", "StartTime" });

        migrationBuilder.CreateIndex(
            name: "IX_StaffWorkingHours_StaffId_DayOfWeek_StartTime",
            table: "StaffWorkingHours",
            columns: new[] { "StaffId", "DayOfWeek", "StartTime" });

        migrationBuilder.CreateIndex(
            name: "IX_StaffTimeOffs_StaffId_StartAt_EndAt",
            table: "StaffTimeOffs",
            columns: new[] { "StaffId", "StartAt", "EndAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "BusinessWorkingHours");
        migrationBuilder.DropTable(name: "StaffTimeOffs");
        migrationBuilder.DropTable(name: "StaffWorkingHours");
    }
}
