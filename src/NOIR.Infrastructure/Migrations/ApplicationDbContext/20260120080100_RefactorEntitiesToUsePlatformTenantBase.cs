using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NOIR.Infrastructure.Migrations.ApplicationDbContext
{
    /// <inheritdoc />
    public partial class RefactorEntitiesToUsePlatformTenantBase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TenantId",
                table: "PermissionTemplates",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_Platform_Lookup",
                table: "EmailTemplates",
                columns: new[] { "Name", "IsActive" },
                filter: "[TenantId] IS NULL AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailTemplates_Platform_Lookup",
                table: "EmailTemplates");

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                table: "PermissionTemplates",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
