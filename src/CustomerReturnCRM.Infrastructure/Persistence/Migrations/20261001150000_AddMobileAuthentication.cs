using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CustomerReturnCRM.Infrastructure.Persistence.Migrations;

[Microsoft.EntityFrameworkCore.Infrastructure.DbContext(typeof(CustomerReturnCRM.Infrastructure.Persistence.ApplicationDbContext))]
[Migration("20261001150000_AddMobileAuthentication")]
public partial class AddMobileAuthentication : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "PhoneNumber",
            table: "AspNetUsers",
            type: "nvarchar(30)",
            maxLength: 30,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(max)",
            oldNullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "PasswordResetOtpExpiresAtUtc",
            table: "AspNetUsers",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PasswordResetOtpHash",
            table: "AspNetUsers",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "PasswordResetOtpAttemptCount",
            table: "AspNetUsers",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<DateTime>(
            name: "PasswordResetOtpRequestedAtUtc",
            table: "AspNetUsers",
            type: "datetime2",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUsers_PhoneNumber",
            table: "AspNetUsers",
            column: "PhoneNumber",
            unique: true,
            filter: "[PhoneNumber] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_AspNetUsers_PhoneNumber",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "PasswordResetOtpExpiresAtUtc",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "PasswordResetOtpHash",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "PasswordResetOtpAttemptCount",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "PasswordResetOtpRequestedAtUtc",
            table: "AspNetUsers");

        migrationBuilder.AlterColumn<string>(
            name: "PhoneNumber",
            table: "AspNetUsers",
            type: "nvarchar(max)",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(30)",
            oldMaxLength: 30,
            oldNullable: true);
    }
}
