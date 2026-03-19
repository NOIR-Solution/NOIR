using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NOIR.Infrastructure.Migrations.App
{
    /// <inheritdoc />
    public partial class CRM_PipelineStage_AddStageTypeAndIsSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StageType",
                table: "PipelineStages",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Active");

            migrationBuilder.AddColumn<bool>(
                name: "IsSystem",
                table: "PipelineStages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Seed Won and Lost system stages for every existing pipeline.
            migrationBuilder.Sql(@"
                DECLARE @Pipelines TABLE (Id UNIQUEIDENTIFIER, TenantId NVARCHAR(128), MaxOrder INT);

                INSERT INTO @Pipelines (Id, TenantId, MaxOrder)
                SELECT p.Id, p.TenantId, ISNULL(MAX(s.SortOrder), -1)
                FROM Pipelines p
                LEFT JOIN PipelineStages s ON s.PipelineId = p.Id AND s.IsDeleted = 0
                WHERE p.IsDeleted = 0
                GROUP BY p.Id, p.TenantId;

                INSERT INTO PipelineStages (Id, PipelineId, Name, SortOrder, Color, StageType, IsSystem, TenantId, CreatedAt, IsDeleted)
                SELECT NEWID(), pl.Id, 'Won', pl.MaxOrder + 1, '#22c55e', 'Won', 1, pl.TenantId, GETUTCDATE(), 0
                FROM @Pipelines pl;

                INSERT INTO PipelineStages (Id, PipelineId, Name, SortOrder, Color, StageType, IsSystem, TenantId, CreatedAt, IsDeleted)
                SELECT NEWID(), pl.Id, 'Lost', pl.MaxOrder + 2, '#ef4444', 'Lost', 1, pl.TenantId, GETUTCDATE(), 0
                FROM @Pipelines pl;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM PipelineStages WHERE IsSystem = 1;");

            migrationBuilder.DropColumn(
                name: "IsSystem",
                table: "PipelineStages");

            migrationBuilder.DropColumn(
                name: "StageType",
                table: "PipelineStages");
        }
    }
}
