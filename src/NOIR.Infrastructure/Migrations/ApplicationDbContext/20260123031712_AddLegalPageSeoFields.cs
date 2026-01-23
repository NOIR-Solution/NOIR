using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NOIR.Infrastructure.Migrations.ApplicationDbContext
{
    /// <inheritdoc />
    public partial class AddLegalPageSeoFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "MetaDescription",
                table: "LegalPages",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AllowIndexing",
                table: "LegalPages",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "CanonicalUrl",
                table: "LegalPages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetaTitle",
                table: "LegalPages",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowIndexing",
                table: "LegalPages");

            migrationBuilder.DropColumn(
                name: "CanonicalUrl",
                table: "LegalPages");

            migrationBuilder.DropColumn(
                name: "MetaTitle",
                table: "LegalPages");

            migrationBuilder.AlterColumn<string>(
                name: "MetaDescription",
                table: "LegalPages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(160)",
                oldMaxLength: 160,
                oldNullable: true);
        }
    }
}
