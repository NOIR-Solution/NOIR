using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NOIR.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AuditImprovements : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ============================================
        // 1. Create AuditRetentionPolicies table
        // ============================================
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
                EntityTypesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CompliancePreset = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                ExportBeforeArchive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                ExportBeforeDelete = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                Priority = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                LastModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                LastModifiedBy = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AuditRetentionPolicies", x => x.Id);
            });

        // Create indexes for AuditRetentionPolicies
        migrationBuilder.CreateIndex(
            name: "IX_AuditRetentionPolicies_TenantId",
            table: "AuditRetentionPolicies",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_AuditRetentionPolicies_TenantActive",
            table: "AuditRetentionPolicies",
            columns: new[] { "TenantId", "IsActive" });

        migrationBuilder.CreateIndex(
            name: "IX_AuditRetentionPolicies_ActivePriority",
            table: "AuditRetentionPolicies",
            columns: new[] { "IsActive", "Priority" });

        migrationBuilder.CreateIndex(
            name: "IX_AuditRetentionPolicies_UniqueActivePreset",
            table: "AuditRetentionPolicies",
            columns: new[] { "TenantId", "CompliancePreset", "IsActive" },
            unique: true,
            filter: "[IsActive] = 1");

        // ============================================
        // 2. Create Full-Text Catalog and Indexes
        // ============================================

        // Create the Full-Text Catalog
        migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'AuditLogsCatalog')
            BEGIN
                CREATE FULLTEXT CATALOG AuditLogsCatalog AS DEFAULT;
            END
        ");

        // Create Full-Text Index on EntityAuditLogs
        // Note: Requires a unique index - using PK_EntityAuditLogs
        migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('EntityAuditLogs'))
            BEGIN
                CREATE FULLTEXT INDEX ON EntityAuditLogs (
                    EntityType LANGUAGE 1033,
                    EntityId LANGUAGE 1033,
                    Operation LANGUAGE 1033,
                    EntityDiff LANGUAGE 1033
                ) KEY INDEX PK_EntityAuditLogs ON AuditLogsCatalog
                WITH (CHANGE_TRACKING = AUTO, STOPLIST = OFF);
            END
        ");

        // Create Full-Text Index on HandlerAuditLogs
        migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('HandlerAuditLogs'))
            BEGIN
                CREATE FULLTEXT INDEX ON HandlerAuditLogs (
                    HandlerName LANGUAGE 1033,
                    OperationType LANGUAGE 1033,
                    TargetDtoType LANGUAGE 1033,
                    InputParameters LANGUAGE 1033,
                    OutputResult LANGUAGE 1033,
                    DtoDiff LANGUAGE 1033,
                    ErrorMessage LANGUAGE 1033
                ) KEY INDEX PK_HandlerAuditLogs ON AuditLogsCatalog
                WITH (CHANGE_TRACKING = AUTO, STOPLIST = OFF);
            END
        ");

        // Create Full-Text Index on HttpRequestAuditLogs
        migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('HttpRequestAuditLogs'))
            BEGIN
                CREATE FULLTEXT INDEX ON HttpRequestAuditLogs (
                    Url LANGUAGE 1033,
                    HttpMethod LANGUAGE 1033,
                    QueryString LANGUAGE 1033,
                    RequestBody LANGUAGE 1033,
                    ResponseBody LANGUAGE 1033,
                    UserEmail LANGUAGE 1033,
                    IpAddress LANGUAGE 1033
                ) KEY INDEX PK_HttpRequestAuditLogs ON AuditLogsCatalog
                WITH (CHANGE_TRACKING = AUTO, STOPLIST = OFF);
            END
        ");

        // ============================================
        // 3. Seed default retention policy
        // ============================================
        migrationBuilder.Sql($@"
            IF NOT EXISTS (SELECT 1 FROM AuditRetentionPolicies WHERE TenantId IS NULL AND Name = 'System Default')
            BEGIN
                INSERT INTO AuditRetentionPolicies (
                    Id, TenantId, Name, Description,
                    HotStorageDays, WarmStorageDays, ColdStorageDays, DeleteAfterDays,
                    CompliancePreset, ExportBeforeArchive, ExportBeforeDelete, IsActive, Priority,
                    CreatedAt, CreatedBy
                ) VALUES (
                    NEWID(), NULL, 'System Default', 'Default retention policy for all tenants. Override with tenant-specific policies.',
                    30, 90, 365, 2555,
                    'CUSTOM', 1, 1, 1, 0,
                    GETUTCDATE(), 'System'
                );
            END
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop Full-Text Indexes first
        migrationBuilder.Sql(@"
            IF EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('HttpRequestAuditLogs'))
                DROP FULLTEXT INDEX ON HttpRequestAuditLogs;

            IF EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('HandlerAuditLogs'))
                DROP FULLTEXT INDEX ON HandlerAuditLogs;

            IF EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('EntityAuditLogs'))
                DROP FULLTEXT INDEX ON EntityAuditLogs;
        ");

        // Drop Full-Text Catalog
        migrationBuilder.Sql(@"
            IF EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'AuditLogsCatalog')
                DROP FULLTEXT CATALOG AuditLogsCatalog;
        ");

        // Drop indexes
        migrationBuilder.DropIndex(
            name: "IX_AuditRetentionPolicies_UniqueActivePreset",
            table: "AuditRetentionPolicies");

        migrationBuilder.DropIndex(
            name: "IX_AuditRetentionPolicies_ActivePriority",
            table: "AuditRetentionPolicies");

        migrationBuilder.DropIndex(
            name: "IX_AuditRetentionPolicies_TenantActive",
            table: "AuditRetentionPolicies");

        migrationBuilder.DropIndex(
            name: "IX_AuditRetentionPolicies_TenantId",
            table: "AuditRetentionPolicies");

        // Drop table
        migrationBuilder.DropTable(name: "AuditRetentionPolicies");
    }
}
