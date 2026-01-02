using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NOIR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ResourceBasedAuthorization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResourceShares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SharedWithUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Permission = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SharedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceShares", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceShares_ExpiresAt",
                table: "ResourceShares",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceShares_IsDeleted",
                table: "ResourceShares",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceShares_Resource",
                table: "ResourceShares",
                columns: new[] { "ResourceType", "ResourceId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceShares_TenantId",
                table: "ResourceShares",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceShares_Unique",
                table: "ResourceShares",
                columns: new[] { "ResourceType", "ResourceId", "SharedWithUserId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResourceShares_User",
                table: "ResourceShares",
                columns: new[] { "SharedWithUserId", "TenantId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResourceShares");
        }
    }
}
