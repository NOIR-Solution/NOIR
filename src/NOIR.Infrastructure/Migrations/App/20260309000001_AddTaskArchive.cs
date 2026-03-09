using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NOIR.Infrastructure.Migrations.App
{
    /// <inheritdoc />
    public partial class AddTaskArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "ProjectTasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArchivedAt",
                table: "ProjectTasks",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_ProjectId_IsArchived",
                table: "ProjectTasks",
                columns: new[] { "ProjectId", "IsArchived" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectTasks_ProjectId_IsArchived",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "ProjectTasks");
        }
    }
}
