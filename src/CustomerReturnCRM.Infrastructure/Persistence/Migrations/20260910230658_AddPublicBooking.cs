using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomerReturnCRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Businesses",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PublicBookingEnabled",
                table: "Businesses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PublicSlug",
                table: "Businesses",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicCode",
                table: "Appointments",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Businesses_PublicSlug",
                table: "Businesses",
                column: "PublicSlug",
                unique: true,
                filter: "[PublicSlug] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PublicCode",
                table: "Appointments",
                column: "PublicCode",
                unique: true,
                filter: "[PublicCode] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Businesses_PublicSlug",
                table: "Businesses");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_PublicCode",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "PublicBookingEnabled",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "PublicSlug",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "PublicCode",
                table: "Appointments");
        }
    }
}
