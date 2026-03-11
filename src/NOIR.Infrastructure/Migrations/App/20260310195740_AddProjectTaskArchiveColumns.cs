using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NOIR.Infrastructure.Migrations.App
{
    /// <inheritdoc />
    public partial class AddProjectTaskArchiveColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ProjectTasks') AND name = 'ArchivedAt')
                BEGIN
                    ALTER TABLE [ProjectTasks] ADD [ArchivedAt] datetimeoffset NULL;
                END
                """);

            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ProjectTasks') AND name = 'IsArchived')
                BEGIN
                    ALTER TABLE [ProjectTasks] ADD [IsArchived] bit NOT NULL DEFAULT 0;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "ProjectTasks");
        }
    }
}
