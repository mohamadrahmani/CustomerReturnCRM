using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomerReturnCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SmsTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmsTemplates_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SmsCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmsCampaigns_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SmsCampaigns_SmsTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "SmsTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SmsRecipients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SmsCampaignId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mobile = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RenderedMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProviderMessageId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmsRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SmsRecipients_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SmsRecipients_SmsCampaigns_SmsCampaignId",
                        column: x => x.SmsCampaignId,
                        principalTable: "SmsCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SmsCampaigns_BusinessId_CreatedByUserId",
                table: "SmsCampaigns",
                columns: new[] { "BusinessId", "CreatedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_SmsCampaigns_BusinessId_Status_ScheduledAt",
                table: "SmsCampaigns",
                columns: new[] { "BusinessId", "Status", "ScheduledAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SmsCampaigns_TemplateId",
                table: "SmsCampaigns",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SmsRecipients_CustomerId",
                table: "SmsRecipients",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_SmsRecipients_ProviderMessageId",
                table: "SmsRecipients",
                column: "ProviderMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_SmsRecipients_SmsCampaignId_CustomerId",
                table: "SmsRecipients",
                columns: new[] { "SmsCampaignId", "CustomerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SmsRecipients_SmsCampaignId_Status",
                table: "SmsRecipients",
                columns: new[] { "SmsCampaignId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SmsTemplates_BusinessId_IsActive",
                table: "SmsTemplates",
                columns: new[] { "BusinessId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SmsTemplates_BusinessId_Name",
                table: "SmsTemplates",
                columns: new[] { "BusinessId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SmsRecipients");

            migrationBuilder.DropTable(
                name: "SmsCampaigns");

            migrationBuilder.DropTable(
                name: "SmsTemplates");
        }
    }
}
