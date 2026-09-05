using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomerReturnCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSmartListDismissals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SmartListDismissals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SmartListType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    LastVisitAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpectedReturnDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DismissedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmartListDismissals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmartListDismissals_AspNetUsers_DismissedByUserId",
                        column: x => x.DismissedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SmartListDismissals_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SmartListDismissals_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SmartListDismissals_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SmartListDismissals_BusinessId_CustomerId_ServiceId_SmartListType",
                table: "SmartListDismissals",
                columns: new[] { "BusinessId", "CustomerId", "ServiceId", "SmartListType" },
                unique: true,
                filter: "[ServiceId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SmartListDismissals_BusinessId_SmartListType",
                table: "SmartListDismissals",
                columns: new[] { "BusinessId", "SmartListType" });

            migrationBuilder.CreateIndex(
                name: "IX_SmartListDismissals_CustomerId",
                table: "SmartListDismissals",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartListDismissals_DismissedByUserId",
                table: "SmartListDismissals",
                column: "DismissedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SmartListDismissals_ServiceId",
                table: "SmartListDismissals",
                column: "ServiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SmartListDismissals");
        }
    }
}
