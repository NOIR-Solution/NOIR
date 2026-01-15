using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NOIR.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityContextToHandlerAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailTemplates_Name_Language_IsActive_IsDeleted",
                table: "EmailTemplates");

            migrationBuilder.DropIndex(
                name: "IX_EmailTemplates_Name_Language_TenantId",
                table: "EmailTemplates");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "EmailTemplates");

            migrationBuilder.AddColumn<string>(
                name: "ActionDescription",
                table: "HandlerAuditLogs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PageContext",
                table: "HandlerAuditLogs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetDisplayName",
                table: "HandlerAuditLogs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HandlerAuditLogs_PageContext",
                table: "HandlerAuditLogs",
                column: "PageContext");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_Name_IsActive_IsDeleted",
                table: "EmailTemplates",
                columns: new[] { "Name", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_Name_TenantId",
                table: "EmailTemplates",
                columns: new[] { "Name", "TenantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HandlerAuditLogs_PageContext",
                table: "HandlerAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_EmailTemplates_Name_IsActive_IsDeleted",
                table: "EmailTemplates");

            migrationBuilder.DropIndex(
                name: "IX_EmailTemplates_Name_TenantId",
                table: "EmailTemplates");

            migrationBuilder.DropColumn(
                name: "ActionDescription",
                table: "HandlerAuditLogs");

            migrationBuilder.DropColumn(
                name: "PageContext",
                table: "HandlerAuditLogs");

            migrationBuilder.DropColumn(
                name: "TargetDisplayName",
                table: "HandlerAuditLogs");

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "EmailTemplates",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_Name_Language_IsActive_IsDeleted",
                table: "EmailTemplates",
                columns: new[] { "Name", "Language", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_Name_Language_TenantId",
                table: "EmailTemplates",
                columns: new[] { "Name", "Language", "TenantId" },
                unique: true);
        }
    }
}
