using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NOIR.Infrastructure.Migrations.App
{
    /// <inheritdoc />
    public partial class GapFixesHrCrmPm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProjectCode",
                table: "Projects",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StatusMapping",
                table: "ProjectColumns",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_ProjectCode_TenantId",
                table: "Projects",
                columns: new[] { "ProjectCode", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectColumns_TenantId_ProjectId_Name",
                table: "ProjectColumns",
                columns: new[] { "TenantId", "ProjectId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Name_Parent_Tenant",
                table: "Departments",
                columns: new[] { "Name", "ParentDepartmentId", "TenantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Projects_ProjectCode_TenantId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_ProjectColumns_TenantId_ProjectId_Name",
                table: "ProjectColumns");

            migrationBuilder.DropIndex(
                name: "IX_Departments_Name_Parent_Tenant",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "ProjectCode",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "StatusMapping",
                table: "ProjectColumns");
        }
    }
}
