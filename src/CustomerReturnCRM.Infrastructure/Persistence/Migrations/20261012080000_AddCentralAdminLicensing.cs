using CustomerReturnCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomerReturnCRM.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261012080000_AddCentralAdminLicensing")]
public sealed class AddCentralAdminLicensing : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsCentralAdmin",
            table: "AspNetUsers",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<bool>(
            name: "LicenseIsActive",
            table: "Businesses",
            type: "bit",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LicenseStartDate",
            table: "Businesses",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LicenseEndDate",
            table: "Businesses",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LicenseAdminNote",
            table: "Businesses",
            type: "nvarchar(1000)",
            maxLength: 1000,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "IsCentralAdmin", table: "AspNetUsers");
        migrationBuilder.DropColumn(name: "LicenseIsActive", table: "Businesses");
        migrationBuilder.DropColumn(name: "LicenseStartDate", table: "Businesses");
        migrationBuilder.DropColumn(name: "LicenseEndDate", table: "Businesses");
        migrationBuilder.DropColumn(name: "LicenseAdminNote", table: "Businesses");
    }
}
