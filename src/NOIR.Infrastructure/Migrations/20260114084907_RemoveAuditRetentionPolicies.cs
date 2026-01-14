using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NOIR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAuditRetentionPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditRetentionPolicies");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditRetentionPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ColdStorageDays = table.Column<int>(type: "int", nullable: false, defaultValue: 365),
                    CompliancePreset = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeleteAfterDays = table.Column<int>(type: "int", nullable: false, defaultValue: 2555),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EntityTypesJson = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: true),
                    ExportBeforeArchive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ExportBeforeDelete = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    HotStorageDays = table.Column<int>(type: "int", nullable: false, defaultValue: 30),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    WarmStorageDays = table.Column<int>(type: "int", nullable: false, defaultValue: 90)
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
    }
}
