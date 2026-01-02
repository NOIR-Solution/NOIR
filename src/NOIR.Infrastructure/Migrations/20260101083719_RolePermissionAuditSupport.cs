using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NOIR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RolePermissionAuditSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_RolePermissions",
                table: "RolePermissions");

            // Add Id column with NEWID() default to generate unique GUIDs for existing rows
            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "RolePermissions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValueSql: "NEWID()");

            // Add CreatedAt with SYSDATETIMEOFFSET() default for existing rows
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "RolePermissions",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "SYSDATETIMEOFFSET()");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "RolePermissions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "RolePermissions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "RolePermissions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "RolePermissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModifiedAt",
                table: "RolePermissions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "RolePermissions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_RolePermissions",
                table: "RolePermissions",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_IsDeleted",
                table: "RolePermissions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_RolePermissions",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_IsDeleted",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                table: "RolePermissions");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "RolePermissions");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RolePermissions",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" });
        }
    }
}
