using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NOIR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceShareParentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParentResourceId",
                table: "ResourceShares",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentResourceType",
                table: "ResourceShares",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditRetentionPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HotStorageDays = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    WarmStorageDays = table.Column<int>(type: "int", nullable: false, defaultValue: 90),
                    ColdStorageDays = table.Column<int>(type: "int", nullable: false, defaultValue: 365),
                    DeleteAfterDays = table.Column<int>(type: "int", nullable: false, defaultValue: 2555),
                    EntityTypesJson = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    CompliancePreset = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ExportBeforeArchive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ExportBeforeDelete = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditRetentionPolicies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditRetentionPolicies_ActivePriority",
                table: "AuditRetentionPolicies",
                columns: new[] { "IsActive", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditRetentionPolicies_TenantActive",
                table: "AuditRetentionPolicies",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditRetentionPolicies_TenantId",
                table: "AuditRetentionPolicies",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditRetentionPolicies_UniqueActivePreset",
                table: "AuditRetentionPolicies",
                columns: new[] { "TenantId", "CompliancePreset", "IsActive" },
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditRetentionPolicies");

            migrationBuilder.DropColumn(
                name: "ParentResourceId",
                table: "ResourceShares");

            migrationBuilder.DropColumn(
                name: "ParentResourceType",
                table: "ResourceShares");
        }
    }
}
