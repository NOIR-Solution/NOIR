using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NOIR.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarUrl",
                table: "AspNetUsers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmailChangeOtps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CurrentEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NewEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OtpHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SessionToken = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    ResendCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    LastResendAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
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
                    table.PrimaryKey("PK_EmailChangeOtps", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailChangeOtps_NewEmail",
                table: "EmailChangeOtps",
                column: "NewEmail");

            migrationBuilder.CreateIndex(
                name: "IX_EmailChangeOtps_SessionToken",
                table: "EmailChangeOtps",
                column: "SessionToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailChangeOtps_SessionToken_IsUsed",
                table: "EmailChangeOtps",
                columns: new[] { "SessionToken", "IsUsed" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailChangeOtps_TenantId",
                table: "EmailChangeOtps",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailChangeOtps_UserId",
                table: "EmailChangeOtps",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailChangeOtps_UserId_IsUsed_IsDeleted",
                table: "EmailChangeOtps",
                columns: new[] { "UserId", "IsUsed", "IsDeleted" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailChangeOtps");

            migrationBuilder.DropColumn(
                name: "AvatarUrl",
                table: "AspNetUsers");
        }
    }
}
