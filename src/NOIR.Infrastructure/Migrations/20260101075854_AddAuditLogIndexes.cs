using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NOIR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArchivedAt",
                table: "HttpRequestAuditLogs",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "HttpRequestAuditLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArchivedAt",
                table: "HandlerAuditLogs",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "HandlerAuditLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArchivedAt",
                table: "EntityAuditLogs",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "EntityAuditLogs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_HttpRequestAuditLogs_IsArchived",
                table: "HttpRequestAuditLogs",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_HttpRequestAuditLogs_IsArchived_ArchivedAt",
                table: "HttpRequestAuditLogs",
                columns: new[] { "IsArchived", "ArchivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HttpRequestAuditLogs_Tenant_StartTime",
                table: "HttpRequestAuditLogs",
                columns: new[] { "TenantId", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_HttpRequestAuditLogs_TenantId",
                table: "HttpRequestAuditLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_HandlerAuditLogs_IsArchived",
                table: "HandlerAuditLogs",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_HandlerAuditLogs_IsArchived_ArchivedAt",
                table: "HandlerAuditLogs",
                columns: new[] { "IsArchived", "ArchivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HandlerAuditLogs_TenantId",
                table: "HandlerAuditLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EntityAuditLogs_EntityHistory",
                table: "EntityAuditLogs",
                columns: new[] { "EntityType", "EntityId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_EntityAuditLogs_IsArchived",
                table: "EntityAuditLogs",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_EntityAuditLogs_IsArchived_ArchivedAt",
                table: "EntityAuditLogs",
                columns: new[] { "IsArchived", "ArchivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EntityAuditLogs_TenantId",
                table: "EntityAuditLogs",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HttpRequestAuditLogs_IsArchived",
                table: "HttpRequestAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_HttpRequestAuditLogs_IsArchived_ArchivedAt",
                table: "HttpRequestAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_HttpRequestAuditLogs_Tenant_StartTime",
                table: "HttpRequestAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_HttpRequestAuditLogs_TenantId",
                table: "HttpRequestAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_HandlerAuditLogs_IsArchived",
                table: "HandlerAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_HandlerAuditLogs_IsArchived_ArchivedAt",
                table: "HandlerAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_HandlerAuditLogs_TenantId",
                table: "HandlerAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_EntityAuditLogs_EntityHistory",
                table: "EntityAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_EntityAuditLogs_IsArchived",
                table: "EntityAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_EntityAuditLogs_IsArchived_ArchivedAt",
                table: "EntityAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_EntityAuditLogs_TenantId",
                table: "EntityAuditLogs");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "HttpRequestAuditLogs");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "HttpRequestAuditLogs");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "HandlerAuditLogs");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "HandlerAuditLogs");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "EntityAuditLogs");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "EntityAuditLogs");
        }
    }
}
