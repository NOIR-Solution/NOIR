using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NOIR.Infrastructure.Migrations.App
{
    /// <inheritdoc />
    public partial class AddProductFilterIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductAttributes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsFilterable = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsSearchable = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsVariantAttribute = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ShowInProductCard = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ShowInSpecifications = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ValidationRegex = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MinValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    MaxValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    MaxLength = table.Column<int>(type: "int", nullable: true),
                    DefaultValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Placeholder = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HelpText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_ProductAttributes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductFilterIndexes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProductSlug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Sku = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CategoryPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CategoryName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CategorySlug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BrandId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BrandName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BrandSlug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MinPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "VND"),
                    InStock = table.Column<bool>(type: "bit", nullable: false),
                    TotalStock = table.Column<int>(type: "int", nullable: false),
                    AverageRating = table.Column<decimal>(type: "decimal(3,2)", precision: 3, scale: 2, nullable: true),
                    ReviewCount = table.Column<int>(type: "int", nullable: false),
                    AttributesJson = table.Column<string>(type: "nvarchar(max)", maxLength: 500, nullable: false),
                    SearchText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    PrimaryImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProductUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductFilterIndexes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductFilterIndexes_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CategoryAttributes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttributeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryAttributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryAttributes_ProductAttributes_AttributeId",
                        column: x => x.AttributeId,
                        principalTable: "ProductAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CategoryAttributes_ProductCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ProductCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductAttributeValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttributeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DisplayValue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ColorCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SwatchUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IconUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ProductCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttributeValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductAttributeValues_ProductAttributes_AttributeId",
                        column: x => x.AttributeId,
                        principalTable: "ProductAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductAttributeAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttributeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AttributeValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AttributeValueIds = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    TextValue = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    NumberValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 2, nullable: true),
                    BoolValue = table.Column<bool>(type: "bit", nullable: true),
                    DateValue = table.Column<DateTime>(type: "date", nullable: true),
                    DateTimeValue = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ColorValue = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MinRangeValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 2, nullable: true),
                    MaxRangeValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 2, nullable: true),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAttributeAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductAttributeAssignments_ProductAttributeValues_AttributeValueId",
                        column: x => x.AttributeValueId,
                        principalTable: "ProductAttributeValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ProductAttributeAssignments_ProductAttributes_AttributeId",
                        column: x => x.AttributeId,
                        principalTable: "ProductAttributes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductAttributeAssignments_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductAttributeAssignments_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAttributes_AttributeId",
                table: "CategoryAttributes",
                column: "AttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAttributes_CategoryId",
                table: "CategoryAttributes",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAttributes_TenantId_AttributeId",
                table: "CategoryAttributes",
                columns: new[] { "TenantId", "AttributeId" });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAttributes_TenantId_CategoryId_AttributeId",
                table: "CategoryAttributes",
                columns: new[] { "TenantId", "CategoryId", "AttributeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAttributes_TenantId_CategoryId_SortOrder",
                table: "CategoryAttributes",
                columns: new[] { "TenantId", "CategoryId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeAssignments_AttributeId",
                table: "ProductAttributeAssignments",
                column: "AttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeAssignments_AttributeValueId",
                table: "ProductAttributeAssignments",
                column: "AttributeValueId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeAssignments_ProductId",
                table: "ProductAttributeAssignments",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeAssignments_TenantId_AttributeId",
                table: "ProductAttributeAssignments",
                columns: new[] { "TenantId", "AttributeId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeAssignments_TenantId_AttributeValueId",
                table: "ProductAttributeAssignments",
                columns: new[] { "TenantId", "AttributeValueId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeAssignments_TenantId_ProductId",
                table: "ProductAttributeAssignments",
                columns: new[] { "TenantId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeAssignments_TenantId_ProductId_AttributeId_VariantId",
                table: "ProductAttributeAssignments",
                columns: new[] { "TenantId", "ProductId", "AttributeId", "VariantId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL AND [VariantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeAssignments_VariantId",
                table: "ProductAttributeAssignments",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_TenantId",
                table: "ProductAttributes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_TenantId_Code",
                table: "ProductAttributes",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_TenantId_IsActive",
                table: "ProductAttributes",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_TenantId_IsFilterable",
                table: "ProductAttributes",
                columns: new[] { "TenantId", "IsFilterable" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_TenantId_IsVariantAttribute",
                table: "ProductAttributes",
                columns: new[] { "TenantId", "IsVariantAttribute" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_TenantId_SortOrder_Name",
                table: "ProductAttributes",
                columns: new[] { "TenantId", "SortOrder", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_TenantId_Type",
                table: "ProductAttributes",
                columns: new[] { "TenantId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValues_AttributeId",
                table: "ProductAttributeValues",
                column: "AttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValues_TenantId_AttributeId_IsActive",
                table: "ProductAttributeValues",
                columns: new[] { "TenantId", "AttributeId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValues_TenantId_AttributeId_SortOrder",
                table: "ProductAttributeValues",
                columns: new[] { "TenantId", "AttributeId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValues_TenantId_AttributeId_Value",
                table: "ProductAttributeValues",
                columns: new[] { "TenantId", "AttributeId", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductFilterIndexes_ProductId",
                table: "ProductFilterIndexes",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductFilterIndexes_TenantId",
                table: "ProductFilterIndexes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductFilterIndexes_TenantId_BrandId",
                table: "ProductFilterIndexes",
                columns: new[] { "TenantId", "BrandId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductFilterIndexes_TenantId_Category_Status_Sort",
                table: "ProductFilterIndexes",
                columns: new[] { "TenantId", "CategoryId", "Status", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductFilterIndexes_TenantId_CategoryId",
                table: "ProductFilterIndexes",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductFilterIndexes_TenantId_CategoryPath",
                table: "ProductFilterIndexes",
                columns: new[] { "TenantId", "CategoryPath" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductFilterIndexes_TenantId_InStock",
                table: "ProductFilterIndexes",
                columns: new[] { "TenantId", "InStock" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductFilterIndexes_TenantId_LastSynced",
                table: "ProductFilterIndexes",
                columns: new[] { "TenantId", "LastSyncedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductFilterIndexes_TenantId_Price",
                table: "ProductFilterIndexes",
                columns: new[] { "TenantId", "MinPrice", "MaxPrice" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductFilterIndexes_TenantId_ProductId",
                table: "ProductFilterIndexes",
                columns: new[] { "TenantId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductFilterIndexes_TenantId_Rating",
                table: "ProductFilterIndexes",
                columns: new[] { "TenantId", "AverageRating" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductFilterIndexes_TenantId_Status_Sort",
                table: "ProductFilterIndexes",
                columns: new[] { "TenantId", "Status", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryAttributes");

            migrationBuilder.DropTable(
                name: "ProductAttributeAssignments");

            migrationBuilder.DropTable(
                name: "ProductFilterIndexes");

            migrationBuilder.DropTable(
                name: "ProductAttributeValues");

            migrationBuilder.DropTable(
                name: "ProductAttributes");
        }
    }
}
