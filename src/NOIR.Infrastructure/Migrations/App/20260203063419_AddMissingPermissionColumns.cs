using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NOIR.Infrastructure.Migrations.App
{
    /// <inheritdoc />
    public partial class AddMissingPermissionColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId_IsDeleted",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_ProductOptionValues_Option_Value",
                table: "ProductOptionValues");

            migrationBuilder.DropIndex(
                name: "IX_ProductOptions_Product_Name",
                table: "ProductOptions");

            migrationBuilder.DropIndex(
                name: "IX_ProductImages_Product_Primary",
                table: "ProductImages");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributes_TenantId_IsActive",
                table: "ProductAttributes");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributes_TenantId_IsFilterable",
                table: "ProductAttributes");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributes_TenantId_IsGlobal",
                table: "ProductAttributes");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributes_TenantId_IsVariantAttribute",
                table: "ProductAttributes");

            migrationBuilder.DropIndex(
                name: "IX_PostTags_Slug_TenantId",
                table: "PostTags");

            migrationBuilder.DropIndex(
                name: "IX_PostTagAssignments_PostId_TagId",
                table: "PostTagAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Posts_AuthorId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_Category_Status",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_ScheduledPublish",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_Slug_TenantId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_Status_PublishedAt",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_PostCategories_Parent_Sort",
                table: "PostCategories");

            migrationBuilder.DropIndex(
                name: "IX_PostCategories_Slug_TenantId",
                table: "PostCategories");

            migrationBuilder.DropIndex(
                name: "IX_PasswordResetOtps_Email_IsUsed_IsDeleted",
                table: "PasswordResetOtps");

            migrationBuilder.DropIndex(
                name: "IX_Orders_OrderNumber_TenantId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_PendingDigest",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_NotificationPreferences_UserId_Category",
                table: "NotificationPreferences");

            migrationBuilder.DropIndex(
                name: "IX_MediaFiles_Folder_TenantId",
                table: "MediaFiles");

            migrationBuilder.DropIndex(
                name: "IX_MediaFiles_Slug_TenantId",
                table: "MediaFiles");

            migrationBuilder.DropIndex(
                name: "IX_MediaFiles_UploadedBy",
                table: "MediaFiles");

            migrationBuilder.DropIndex(
                name: "IX_EmailTemplates_Name_TenantId",
                table: "EmailTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Brands_TenantId_IsActive",
                table: "Brands");

            migrationBuilder.DropIndex(
                name: "IX_Brands_TenantId_IsFeatured",
                table: "Brands");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "PermissionTemplateItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "PermissionTemplateItems",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "PermissionTemplateItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PermissionTemplateItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "PermissionTemplateItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Permissions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Permissions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "Permissions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Permissions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "Permissions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ModifiedBy",
                table: "InventoryMovements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                table: "InventoryMovements",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<string>(
                name: "DeletedBy",
                table: "InventoryMovements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "InventoryMovements",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Active",
                table: "RefreshTokens",
                columns: new[] { "TenantId", "UserId", "ExpiresAt" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_TenantId",
                table: "ProductVariants",
                column: "TenantId");

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
                name: "IX_ProductImages_TenantId",
                table: "ProductImages",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_TenantId_Primary",
                table: "ProductImages",
                columns: new[] { "TenantId", "ProductId" },
                filter: "[IsPrimary] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_TenantId_Global",
                table: "ProductAttributes",
                columns: new[] { "TenantId", "SortOrder" },
                filter: "[IsGlobal] = 1 AND [IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_PostTags_TenantId_Slug",
                table: "PostTags",
                columns: new[] { "TenantId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostTagAssignments_PostId",
                table: "PostTagAssignments",
                column: "PostId");

            migrationBuilder.CreateIndex(
                name: "IX_PostTagAssignments_TenantId_Post_Tag",
                table: "PostTagAssignments",
                columns: new[] { "TenantId", "PostId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_CategoryId",
                table: "Posts",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_TenantId_AuthorId",
                table: "Posts",
                columns: new[] { "TenantId", "AuthorId" });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_TenantId_Category_Status",
                table: "Posts",
                columns: new[] { "TenantId", "CategoryId", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_TenantId_ScheduledPublish",
                table: "Posts",
                columns: new[] { "TenantId", "ScheduledPublishAt" },
                filter: "[ScheduledPublishAt] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_TenantId_Slug",
                table: "Posts",
                columns: new[] { "TenantId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_TenantId_Status_PublishedAt",
                table: "Posts",
                columns: new[] { "TenantId", "Status", "PublishedAt", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_PostCategories_ParentId",
                table: "PostCategories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_PostCategories_TenantId_Parent_Sort",
                table: "PostCategories",
                columns: new[] { "TenantId", "ParentId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PostCategories_TenantId_Slug",
                table: "PostCategories",
                columns: new[] { "TenantId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetOtps_Active",
                table: "PasswordResetOtps",
                columns: new[] { "TenantId", "Email", "ExpiresAt" },
                filter: "[IsUsed] = 0 AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TenantId_OrderNumber",
                table: "Orders",
                columns: new[] { "TenantId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UnsentEmail",
                table: "Notifications",
                columns: new[] { "TenantId", "UserId", "CreatedAt" },
                filter: "[EmailSent] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_TenantId_UserId_Category",
                table: "NotificationPreferences",
                columns: new[] { "TenantId", "UserId", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_TenantId_Folder",
                table: "MediaFiles",
                columns: new[] { "TenantId", "Folder" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_TenantId_Slug",
                table: "MediaFiles",
                columns: new[] { "TenantId", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_TenantId_UploadedBy",
                table: "MediaFiles",
                columns: new[] { "TenantId", "UploadedBy" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryMovements_TenantId",
                table: "InventoryMovements",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_TenantId_Name",
                table: "EmailTemplates",
                columns: new[] { "TenantId", "Name" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_TenantId_Featured",
                table: "Brands",
                columns: new[] { "TenantId", "SortOrder" },
                filter: "[IsFeatured] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Active",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariants_TenantId",
                table: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_ProductOptionValues_TenantId_Option_Value",
                table: "ProductOptionValues");

            migrationBuilder.DropIndex(
                name: "IX_ProductOptions_TenantId_Product_Name",
                table: "ProductOptions");

            migrationBuilder.DropIndex(
                name: "IX_ProductImages_TenantId",
                table: "ProductImages");

            migrationBuilder.DropIndex(
                name: "IX_ProductImages_TenantId_Primary",
                table: "ProductImages");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributes_TenantId_Global",
                table: "ProductAttributes");

            migrationBuilder.DropIndex(
                name: "IX_PostTags_TenantId_Slug",
                table: "PostTags");

            migrationBuilder.DropIndex(
                name: "IX_PostTagAssignments_PostId",
                table: "PostTagAssignments");

            migrationBuilder.DropIndex(
                name: "IX_PostTagAssignments_TenantId_Post_Tag",
                table: "PostTagAssignments");

            migrationBuilder.DropIndex(
                name: "IX_Posts_CategoryId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_TenantId_AuthorId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_TenantId_Category_Status",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_TenantId_ScheduledPublish",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_TenantId_Slug",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_TenantId_Status_PublishedAt",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_PostCategories_ParentId",
                table: "PostCategories");

            migrationBuilder.DropIndex(
                name: "IX_PostCategories_TenantId_Parent_Sort",
                table: "PostCategories");

            migrationBuilder.DropIndex(
                name: "IX_PostCategories_TenantId_Slug",
                table: "PostCategories");

            migrationBuilder.DropIndex(
                name: "IX_PasswordResetOtps_Active",
                table: "PasswordResetOtps");

            migrationBuilder.DropIndex(
                name: "IX_Orders_TenantId_OrderNumber",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UnsentEmail",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_NotificationPreferences_TenantId_UserId_Category",
                table: "NotificationPreferences");

            migrationBuilder.DropIndex(
                name: "IX_MediaFiles_TenantId_Folder",
                table: "MediaFiles");

            migrationBuilder.DropIndex(
                name: "IX_MediaFiles_TenantId_Slug",
                table: "MediaFiles");

            migrationBuilder.DropIndex(
                name: "IX_MediaFiles_TenantId_UploadedBy",
                table: "MediaFiles");

            migrationBuilder.DropIndex(
                name: "IX_InventoryMovements_TenantId",
                table: "InventoryMovements");

            migrationBuilder.DropIndex(
                name: "IX_EmailTemplates_TenantId_Name",
                table: "EmailTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Brands_TenantId_Featured",
                table: "Brands");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "PermissionTemplateItems");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PermissionTemplateItems");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "PermissionTemplateItems");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PermissionTemplateItems");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "PermissionTemplateItems");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "Permissions");

            migrationBuilder.AlterColumn<string>(
                name: "ModifiedBy",
                table: "InventoryMovements",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsDeleted",
                table: "InventoryMovements",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "DeletedBy",
                table: "InventoryMovements",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "InventoryMovements",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_IsDeleted",
                table: "RefreshTokens",
                columns: new[] { "UserId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptionValues_Option_Value",
                table: "ProductOptionValues",
                columns: new[] { "OptionId", "Value" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductOptions_Product_Name",
                table: "ProductOptions",
                columns: new[] { "ProductId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_Product_Primary",
                table: "ProductImages",
                columns: new[] { "ProductId", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_TenantId_IsActive",
                table: "ProductAttributes",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_TenantId_IsFilterable",
                table: "ProductAttributes",
                columns: new[] { "TenantId", "IsFilterable" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_TenantId_IsGlobal",
                table: "ProductAttributes",
                columns: new[] { "TenantId", "IsGlobal" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_TenantId_IsVariantAttribute",
                table: "ProductAttributes",
                columns: new[] { "TenantId", "IsVariantAttribute" });

            migrationBuilder.CreateIndex(
                name: "IX_PostTags_Slug_TenantId",
                table: "PostTags",
                columns: new[] { "Slug", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostTagAssignments_PostId_TagId",
                table: "PostTagAssignments",
                columns: new[] { "PostId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_AuthorId",
                table: "Posts",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Category_Status",
                table: "Posts",
                columns: new[] { "CategoryId", "Status", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_ScheduledPublish",
                table: "Posts",
                columns: new[] { "ScheduledPublishAt", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Slug_TenantId",
                table: "Posts",
                columns: new[] { "Slug", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_Status_PublishedAt",
                table: "Posts",
                columns: new[] { "Status", "PublishedAt", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_PostCategories_Parent_Sort",
                table: "PostCategories",
                columns: new[] { "ParentId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PostCategories_Slug_TenantId",
                table: "PostCategories",
                columns: new[] { "Slug", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetOtps_Email_IsUsed_IsDeleted",
                table: "PasswordResetOtps",
                columns: new[] { "Email", "IsUsed", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber_TenantId",
                table: "Orders",
                columns: new[] { "OrderNumber", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_PendingDigest",
                table: "Notifications",
                columns: new[] { "UserId", "IncludedInDigest", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_UserId_Category",
                table: "NotificationPreferences",
                columns: new[] { "UserId", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_Folder_TenantId",
                table: "MediaFiles",
                columns: new[] { "Folder", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_Slug_TenantId",
                table: "MediaFiles",
                columns: new[] { "Slug", "TenantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_UploadedBy",
                table: "MediaFiles",
                column: "UploadedBy");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_Name_TenantId",
                table: "EmailTemplates",
                columns: new[] { "Name", "TenantId" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_TenantId_IsActive",
                table: "Brands",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Brands_TenantId_IsFeatured",
                table: "Brands",
                columns: new[] { "TenantId", "IsFeatured" });
        }
    }
}
