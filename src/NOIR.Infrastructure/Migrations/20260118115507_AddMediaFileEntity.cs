using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NOIR.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaFileEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FeaturedImageId",
                table: "Posts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MediaFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShortId = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Folder = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DefaultUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ThumbHash = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DominantColor = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Width = table.Column<int>(type: "int", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    Format = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    HasTransparency = table.Column<bool>(type: "bit", nullable: false),
                    VariantsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: false),
                    SrcsetsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: false),
                    AltText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UploadedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaFiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_FeaturedImageId",
                table: "Posts",
                column: "FeaturedImageId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_Folder_TenantId",
                table: "MediaFiles",
                columns: new[] { "Folder", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_ShortId",
                table: "MediaFiles",
                column: "ShortId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_Slug_TenantId",
                table: "MediaFiles",
                columns: new[] { "Slug", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_TenantId",
                table: "MediaFiles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_UploadedBy",
                table: "MediaFiles",
                column: "UploadedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_MediaFiles_FeaturedImageId",
                table: "Posts",
                column: "FeaturedImageId",
                principalTable: "MediaFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_MediaFiles_FeaturedImageId",
                table: "Posts");

            migrationBuilder.DropTable(
                name: "MediaFiles");

            migrationBuilder.DropIndex(
                name: "IX_Posts_FeaturedImageId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "FeaturedImageId",
                table: "Posts");
        }
    }
}
