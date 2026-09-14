using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomerReturnCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBaleIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BaleConnectToken",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaleConnectToken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaleConnectToken_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BaleConnectToken_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BaleCustomerIdentity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BaleUserId = table.Column<long>(type: "bigint", nullable: false),
                    BaleChatId = table.Column<long>(type: "bigint", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConnectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaleCustomerIdentity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaleCustomerIdentity_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BaleCustomerIdentity_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BaleConnectToken_BusinessId_CustomerId_ExpiresAtUtc",
                table: "BaleConnectToken",
                columns: new[] { "BusinessId", "CustomerId", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BaleConnectToken_CustomerId",
                table: "BaleConnectToken",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_BaleConnectToken_TokenHash",
                table: "BaleConnectToken",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BaleCustomerIdentity_BusinessId_BaleUserId",
                table: "BaleCustomerIdentity",
                columns: new[] { "BusinessId", "BaleUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BaleCustomerIdentity_BusinessId_CustomerId",
                table: "BaleCustomerIdentity",
                columns: new[] { "BusinessId", "CustomerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BaleCustomerIdentity_CustomerId",
                table: "BaleCustomerIdentity",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BaleConnectToken");

            migrationBuilder.DropTable(
                name: "BaleCustomerIdentity");
        }
    }
}
