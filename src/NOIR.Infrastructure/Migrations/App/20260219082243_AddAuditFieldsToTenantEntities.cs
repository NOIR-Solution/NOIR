using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NOIR.Infrastructure.Migrations.App
{
    /// <inheritdoc />
    public partial class AddAuditFieldsToTenantEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WishlistItems_WishlistId_ProductId_VariantId",
                table: "WishlistItems");

            migrationBuilder.DropIndex(
                name: "IX_TenantSettings_TenantId_Key",
                table: "TenantSettings");

            migrationBuilder.DropIndex(
                name: "IX_ResourceShares_Unique",
                table: "ResourceShares");

            migrationBuilder.DropIndex(
                name: "IX_PromotionProducts_PromotionId_ProductId",
                table: "PromotionProducts");

            migrationBuilder.DropIndex(
                name: "IX_PromotionCategories_PromotionId_CategoryId",
                table: "PromotionCategories");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_TenantId_Sku",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductOptionValues_TenantId_Option_Value",
                table: "ProductOptionValues");

            migrationBuilder.DropIndex(
                name: "IX_ProductOptions_TenantId_Product_Name",
                table: "ProductOptions");

            migrationBuilder.DropIndex(
                name: "IX_ProductFilterIndexes_TenantId_ProductId",
                table: "ProductFilterIndexes");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributeValues_TenantId_AttributeId_Value",
                table: "ProductAttributeValues");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributeAssignments_TenantId_ProductId_AttributeId_VariantId",
                table: "ProductAttributeAssignments");

            migrationBuilder.DropIndex(
                name: "IX_PermissionTemplates_Name_TenantId",
                table: "PermissionTemplates");

            migrationBuilder.DropIndex(
                name: "IX_PermissionTemplateItems_TemplateId_PermissionId",
                table: "PermissionTemplateItems");

            migrationBuilder.DropIndex(
                name: "IX_PaymentInstallments_Transaction_Number",
                table: "PaymentInstallments");

            migrationBuilder.DropIndex(
                name: "IX_CustomerGroupMemberships_GroupId_CustomerId_TenantId",
                table: "CustomerGroupMemberships");

            migrationBuilder.DropIndex(
                name: "IX_CategoryAttributes_TenantId_CategoryId_AttributeId",
                table: "CategoryAttributes");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "WishlistItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "WishlistItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "WishlistItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "WishlistItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "WishlistItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ShippingTrackingEvents",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "ShippingTrackingEvents",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ShippingTrackingEvents",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ShippingTrackingEvents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "ShippingTrackingEvents",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ReviewMedia",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "ReviewMedia",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ReviewMedia",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ReviewMedia",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "ReviewMedia",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "PromotionUsages",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "PromotionUsages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "PromotionUsages",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PromotionUsages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "PromotionUsages",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "PromotionProducts",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "PromotionProducts",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "PromotionProducts",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PromotionProducts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "PromotionProducts",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "PromotionCategories",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "PromotionCategories",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "PromotionCategories",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PromotionCategories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "PromotionCategories",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ProductVariants",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "ProductVariants",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ProductVariants",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProductVariants",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "ProductVariants",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ProductOptionValues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "ProductOptionValues",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ProductOptionValues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProductOptionValues",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "ProductOptionValues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ProductOptions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "ProductOptions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ProductOptions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProductOptions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "ProductOptions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ProductImages",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "ProductImages",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ProductImages",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProductImages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "ProductImages",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ProductFilterIndexes",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "ProductFilterIndexes",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ProductFilterIndexes",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProductFilterIndexes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "ProductFilterIndexes",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ProductAttributeValues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "ProductAttributeValues",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ProductAttributeValues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProductAttributeValues",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "ProductAttributeValues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "ProductAttributeAssignments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "ProductAttributeAssignments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ProductAttributeAssignments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ProductAttributeAssignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "ProductAttributeAssignments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "PaymentInstallments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "PaymentInstallments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "PaymentInstallments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PaymentInstallments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "PaymentInstallments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "OrderNotes",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "OrderNotes",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "OrderNotes",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "OrderNotes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "OrderNotes",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "OrderItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "OrderItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "OrderItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "OrderItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "OrderItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "InventoryReceiptItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "InventoryReceiptItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "InventoryReceiptItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "InventoryReceiptItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "InventoryReceiptItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "FilterAnalyticsEvents",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "FilterAnalyticsEvents",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "FilterAnalyticsEvents",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "FilterAnalyticsEvents",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "FilterAnalyticsEvents",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "CustomerGroupMemberships",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "CustomerGroupMemberships",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "CustomerGroupMemberships",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CustomerGroupMemberships",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "CustomerGroupMemberships",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "CustomerAddresses",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "CustomerAddresses",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "CustomerAddresses",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CustomerAddresses",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "CustomerAddresses",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "CategoryAttributes",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "CategoryAttributes",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "CategoryAttributes",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CategoryAttributes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "CategoryAttributes",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "CartItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "CartItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "CartItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CartItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "CartItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_IsDeleted",
                table: "WishlistItems",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_TenantId",
                table: "WishlistItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_WishlistId_ProductId_VariantId",
                table: "WishlistItems",
                columns: new[] { "WishlistId", "ProductId", "ProductVariantId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TenantSettings_TenantId_Key",
                table: "TenantSettings",
                columns: new[] { "TenantId", "Key" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingTrackingEvents_IsDeleted",
                table: "ShippingTrackingEvents",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingTrackingEvents_TenantId",
                table: "ShippingTrackingEvents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewMedia_IsDeleted",
                table: "ReviewMedia",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceShares_Unique",
                table: "ResourceShares",
                columns: new[] { "ResourceType", "ResourceId", "SharedWithUserId", "TenantId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionUsages_IsDeleted",
                table: "PromotionUsages",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionUsages_TenantId",
                table: "PromotionUsages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionProducts_IsDeleted",
                table: "PromotionProducts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionProducts_PromotionId_ProductId",
                table: "PromotionProducts",
                columns: new[] { "PromotionId", "ProductId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionProducts_TenantId",
                table: "PromotionProducts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCategories_IsDeleted",
                table: "PromotionCategories",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCategories_PromotionId_CategoryId",
                table: "PromotionCategories",
                columns: new[] { "PromotionId", "CategoryId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCategories_TenantId",
                table: "PromotionCategories",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_IsDeleted",
                table: "ProductVariants",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_TenantId_Sku",
                table: "ProductVariants",
                columns: new[] { "TenantId", "Sku" },
                unique: true,
                filter: "[Sku] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptionValues_IsDeleted",
                table: "ProductOptionValues",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptionValues_TenantId_Option_Value",
                table: "ProductOptionValues",
                columns: new[] { "TenantId", "OptionId", "Value" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptions_IsDeleted",
                table: "ProductOptions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptions_TenantId_Product_Name",
                table: "ProductOptions",
                columns: new[] { "TenantId", "ProductId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_IsDeleted",
                table: "ProductImages",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductFilterIndexes_IsDeleted",
                table: "ProductFilterIndexes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductFilterIndexes_TenantId_ProductId",
                table: "ProductFilterIndexes",
                columns: new[] { "TenantId", "ProductId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValues_IsDeleted",
                table: "ProductAttributeValues",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValues_TenantId",
                table: "ProductAttributeValues",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValues_TenantId_AttributeId_Value",
                table: "ProductAttributeValues",
                columns: new[] { "TenantId", "AttributeId", "Value" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeAssignments_IsDeleted",
                table: "ProductAttributeAssignments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeAssignments_TenantId",
                table: "ProductAttributeAssignments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeAssignments_TenantId_ProductId_AttributeId_VariantId",
                table: "ProductAttributeAssignments",
                columns: new[] { "TenantId", "ProductId", "AttributeId", "VariantId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionTemplates_Name_TenantId",
                table: "PermissionTemplates",
                columns: new[] { "Name", "TenantId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionTemplateItems_TemplateId_PermissionId",
                table: "PermissionTemplateItems",
                columns: new[] { "TemplateId", "PermissionId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentInstallments_IsDeleted",
                table: "PaymentInstallments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentInstallments_Transaction_Number",
                table: "PaymentInstallments",
                columns: new[] { "PaymentTransactionId", "InstallmentNumber" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OrderNotes_IsDeleted",
                table: "OrderNotes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_OrderNotes_TenantId",
                table: "OrderNotes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_IsDeleted",
                table: "OrderItems",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_TenantId",
                table: "OrderItems",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryReceiptItems_IsDeleted",
                table: "InventoryReceiptItems",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_FilterAnalyticsEvents_IsDeleted",
                table: "FilterAnalyticsEvents",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerGroupMemberships_GroupId_CustomerId_TenantId",
                table: "CustomerGroupMemberships",
                columns: new[] { "CustomerGroupId", "CustomerId", "TenantId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerGroupMemberships_IsDeleted",
                table: "CustomerGroupMemberships",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerGroupMemberships_TenantId",
                table: "CustomerGroupMemberships",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerAddresses_IsDeleted",
                table: "CustomerAddresses",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAttributes_IsDeleted",
                table: "CategoryAttributes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAttributes_TenantId",
                table: "CategoryAttributes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAttributes_TenantId_CategoryId_AttributeId",
                table: "CategoryAttributes",
                columns: new[] { "TenantId", "CategoryId", "AttributeId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_IsDeleted",
                table: "CartItems",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_TenantId",
                table: "CartItems",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WishlistItems_IsDeleted",
                table: "WishlistItems");

            migrationBuilder.DropIndex(
                name: "IX_WishlistItems_TenantId",
                table: "WishlistItems");

            migrationBuilder.DropIndex(
                name: "IX_WishlistItems_WishlistId_ProductId_VariantId",
                table: "WishlistItems");

            migrationBuilder.DropIndex(
                name: "IX_TenantSettings_TenantId_Key",
                table: "TenantSettings");

            migrationBuilder.DropIndex(
                name: "IX_ShippingTrackingEvents_IsDeleted",
                table: "ShippingTrackingEvents");

            migrationBuilder.DropIndex(
                name: "IX_ShippingTrackingEvents_TenantId",
                table: "ShippingTrackingEvents");

            migrationBuilder.DropIndex(
                name: "IX_ReviewMedia_IsDeleted",
                table: "ReviewMedia");

            migrationBuilder.DropIndex(
                name: "IX_ResourceShares_Unique",
                table: "ResourceShares");

            migrationBuilder.DropIndex(
                name: "IX_PromotionUsages_IsDeleted",
                table: "PromotionUsages");

            migrationBuilder.DropIndex(
                name: "IX_PromotionUsages_TenantId",
                table: "PromotionUsages");

            migrationBuilder.DropIndex(
                name: "IX_PromotionProducts_IsDeleted",
                table: "PromotionProducts");

            migrationBuilder.DropIndex(
                name: "IX_PromotionProducts_PromotionId_ProductId",
                table: "PromotionProducts");

            migrationBuilder.DropIndex(
                name: "IX_PromotionProducts_TenantId",
                table: "PromotionProducts");

            migrationBuilder.DropIndex(
                name: "IX_PromotionCategories_IsDeleted",
                table: "PromotionCategories");

            migrationBuilder.DropIndex(
                name: "IX_PromotionCategories_PromotionId_CategoryId",
                table: "PromotionCategories");

            migrationBuilder.DropIndex(
                name: "IX_PromotionCategories_TenantId",
                table: "PromotionCategories");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_IsDeleted",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_TenantId_Sku",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductOptionValues_IsDeleted",
                table: "ProductOptionValues");

            migrationBuilder.DropIndex(
                name: "IX_ProductOptionValues_TenantId_Option_Value",
                table: "ProductOptionValues");

            migrationBuilder.DropIndex(
                name: "IX_ProductOptions_IsDeleted",
                table: "ProductOptions");

            migrationBuilder.DropIndex(
                name: "IX_ProductOptions_TenantId_Product_Name",
                table: "ProductOptions");

            migrationBuilder.DropIndex(
                name: "IX_ProductImages_IsDeleted",
                table: "ProductImages");

            migrationBuilder.DropIndex(
                name: "IX_ProductFilterIndexes_IsDeleted",
                table: "ProductFilterIndexes");

            migrationBuilder.DropIndex(
                name: "IX_ProductFilterIndexes_TenantId_ProductId",
                table: "ProductFilterIndexes");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributeValues_IsDeleted",
                table: "ProductAttributeValues");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributeValues_TenantId",
                table: "ProductAttributeValues");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributeValues_TenantId_AttributeId_Value",
                table: "ProductAttributeValues");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributeAssignments_IsDeleted",
                table: "ProductAttributeAssignments");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributeAssignments_TenantId",
                table: "ProductAttributeAssignments");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributeAssignments_TenantId_ProductId_AttributeId_VariantId",
                table: "ProductAttributeAssignments");

            migrationBuilder.DropIndex(
                name: "IX_PermissionTemplates_Name_TenantId",
                table: "PermissionTemplates");

            migrationBuilder.DropIndex(
                name: "IX_PermissionTemplateItems_TemplateId_PermissionId",
                table: "PermissionTemplateItems");

            migrationBuilder.DropIndex(
                name: "IX_PaymentInstallments_IsDeleted",
                table: "PaymentInstallments");

            migrationBuilder.DropIndex(
                name: "IX_PaymentInstallments_Transaction_Number",
                table: "PaymentInstallments");

            migrationBuilder.DropIndex(
                name: "IX_OrderNotes_IsDeleted",
                table: "OrderNotes");

            migrationBuilder.DropIndex(
                name: "IX_OrderNotes_TenantId",
                table: "OrderNotes");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_IsDeleted",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_TenantId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryReceiptItems_IsDeleted",
                table: "InventoryReceiptItems");

            migrationBuilder.DropIndex(
                name: "IX_FilterAnalyticsEvents_IsDeleted",
                table: "FilterAnalyticsEvents");

            migrationBuilder.DropIndex(
                name: "IX_CustomerGroupMemberships_GroupId_CustomerId_TenantId",
                table: "CustomerGroupMemberships");

            migrationBuilder.DropIndex(
                name: "IX_CustomerGroupMemberships_IsDeleted",
                table: "CustomerGroupMemberships");

            migrationBuilder.DropIndex(
                name: "IX_CustomerGroupMemberships_TenantId",
                table: "CustomerGroupMemberships");

            migrationBuilder.DropIndex(
                name: "IX_CustomerAddresses_IsDeleted",
                table: "CustomerAddresses");

            migrationBuilder.DropIndex(
                name: "IX_CategoryAttributes_IsDeleted",
                table: "CategoryAttributes");

            migrationBuilder.DropIndex(
                name: "IX_CategoryAttributes_TenantId",
                table: "CategoryAttributes");

            migrationBuilder.DropIndex(
                name: "IX_CategoryAttributes_TenantId_CategoryId_AttributeId",
                table: "CategoryAttributes");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_IsDeleted",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_TenantId",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "WishlistItems");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "WishlistItems");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "WishlistItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "WishlistItems");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "WishlistItems");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ShippingTrackingEvents");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ShippingTrackingEvents");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ShippingTrackingEvents");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ShippingTrackingEvents");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "ShippingTrackingEvents");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ReviewMedia");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ReviewMedia");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ReviewMedia");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ReviewMedia");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "ReviewMedia");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PromotionUsages");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PromotionUsages");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PromotionUsages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PromotionUsages");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "PromotionUsages");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PromotionProducts");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PromotionProducts");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PromotionProducts");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PromotionProducts");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "PromotionProducts");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PromotionCategories");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PromotionCategories");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PromotionCategories");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PromotionCategories");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "PromotionCategories");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProductOptionValues");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ProductOptionValues");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ProductOptionValues");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProductOptionValues");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "ProductOptionValues");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProductOptions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ProductOptions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ProductOptions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProductOptions");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "ProductOptions");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProductFilterIndexes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ProductFilterIndexes");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ProductFilterIndexes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProductFilterIndexes");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "ProductFilterIndexes");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProductAttributeValues");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ProductAttributeValues");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ProductAttributeValues");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProductAttributeValues");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "ProductAttributeValues");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ProductAttributeAssignments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ProductAttributeAssignments");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ProductAttributeAssignments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ProductAttributeAssignments");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "ProductAttributeAssignments");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PaymentInstallments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PaymentInstallments");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PaymentInstallments");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PaymentInstallments");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "PaymentInstallments");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "OrderNotes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "OrderNotes");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "OrderNotes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "OrderNotes");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "OrderNotes");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "InventoryReceiptItems");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "InventoryReceiptItems");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "InventoryReceiptItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "InventoryReceiptItems");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "InventoryReceiptItems");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "FilterAnalyticsEvents");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "FilterAnalyticsEvents");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "FilterAnalyticsEvents");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "FilterAnalyticsEvents");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "FilterAnalyticsEvents");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "CustomerGroupMemberships");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CustomerGroupMemberships");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "CustomerGroupMemberships");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CustomerGroupMemberships");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "CustomerGroupMemberships");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "CustomerAddresses");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CustomerAddresses");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "CustomerAddresses");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CustomerAddresses");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "CustomerAddresses");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "CategoryAttributes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CategoryAttributes");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "CategoryAttributes");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CategoryAttributes");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "CategoryAttributes");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "CartItems");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_WishlistId_ProductId_VariantId",
                table: "WishlistItems",
                columns: new[] { "WishlistId", "ProductId", "ProductVariantId" },
                unique: true,
                filter: "[ProductVariantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TenantSettings_TenantId_Key",
                table: "TenantSettings",
                columns: new[] { "TenantId", "Key" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceShares_Unique",
                table: "ResourceShares",
                columns: new[] { "ResourceType", "ResourceId", "SharedWithUserId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromotionProducts_PromotionId_ProductId",
                table: "PromotionProducts",
                columns: new[] { "PromotionId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PromotionCategories_PromotionId_CategoryId",
                table: "PromotionCategories",
                columns: new[] { "PromotionId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_TenantId_Sku",
                table: "ProductVariants",
                columns: new[] { "TenantId", "Sku" },
                unique: true,
                filter: "[Sku] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptionValues_TenantId_Option_Value",
                table: "ProductOptionValues",
                columns: new[] { "TenantId", "OptionId", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptions_TenantId_Product_Name",
                table: "ProductOptions",
                columns: new[] { "TenantId", "ProductId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductFilterIndexes_TenantId_ProductId",
                table: "ProductFilterIndexes",
                columns: new[] { "TenantId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeValues_TenantId_AttributeId_Value",
                table: "ProductAttributeValues",
                columns: new[] { "TenantId", "AttributeId", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributeAssignments_TenantId_ProductId_AttributeId_VariantId",
                table: "ProductAttributeAssignments",
                columns: new[] { "TenantId", "ProductId", "AttributeId", "VariantId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL AND [VariantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionTemplates_Name_TenantId",
                table: "PermissionTemplates",
                columns: new[] { "Name", "TenantId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionTemplateItems_TemplateId_PermissionId",
                table: "PermissionTemplateItems",
                columns: new[] { "TemplateId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentInstallments_Transaction_Number",
                table: "PaymentInstallments",
                columns: new[] { "PaymentTransactionId", "InstallmentNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerGroupMemberships_GroupId_CustomerId_TenantId",
                table: "CustomerGroupMemberships",
                columns: new[] { "CustomerGroupId", "CustomerId", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CategoryAttributes_TenantId_CategoryId_AttributeId",
                table: "CategoryAttributes",
                columns: new[] { "TenantId", "CategoryId", "AttributeId" },
                unique: true);
        }
    }
}
