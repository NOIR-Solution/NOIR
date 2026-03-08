# NOIR - Feature Catalog

> **Complete reference of all features, commands, queries, and endpoints in the NOIR platform.**

**Last Updated:** 2026-03-08

---

## Table of Contents

- [Overview](#overview)
- [Authentication & Identity](#authentication--identity)
- [User Management](#user-management)
- [Role & Permission Management](#role--permission-management)
- [Multi-Tenancy](#multi-tenancy)
- [Payment Processing](#payment-processing)
- [Product Catalog](#product-catalog) ⭐
- [Product Attributes](#product-attributes) ⭐
- [Brands](#brands) ⭐
- [Shopping Cart](#shopping-cart) ⭐
- [Checkout](#checkout) ⭐ **NEW**
- [Orders](#orders) ⭐ **NEW**
- [Audit Logging](#audit-logging)
- [Notifications](#notifications)
- [Email Templates](#email-templates)
- [Legal Pages](#legal-pages)
- [Media Management](#media-management)
- [Blog CMS](#blog-cms)
- [Shipping](#shipping) ⭐
- [Inventory](#inventory) ⭐
- [Customers](#customers) ⭐
- [Customer Groups](#customer-groups) ⭐
- [Promotions](#promotions) ⭐
- [Reviews](#reviews) ⭐
- [Wishlists](#wishlists) ⭐
- [Reports](#reports) ⭐
- [Feature Management](#feature-management) ⭐
- [Outbound Webhooks](#outbound-webhooks) ⭐
- [Tenant Settings](#tenant-settings) ⭐
- [Platform Settings](#platform-settings) ⭐
- [Developer Tools](#developer-tools)
- [HR Module](#hr-module) ⭐ **NEW**
- [CRM Module](#crm-module) ⭐ **NEW**
- [Project Management Module](#project-management-module) ⭐ **NEW**
- [Real-Time & Communication](#real-time--communication) ⭐ **NEW**
- [Global Search](#global-search) ⭐ **NEW**
- [Feature Matrix](#feature-matrix)

---

## Overview

NOIR implements **Vertical Slice Architecture** where each feature is self-contained with:
- **Commands** - Write operations (Create, Update, Delete)
- **Queries** - Read operations (Get, List, Search)
- **DTOs** - Data transfer objects
- **Validators** - FluentValidation rules
- **Handlers** - Business logic via Wolverine

**Location:** `src/NOIR.Application/Features/{Feature}/`

---

## Authentication & Identity

**Namespace:** `NOIR.Application.Features.Auth`
**Endpoint:** `/api/auth`

### Features

#### Login
- **Command:** `LoginCommand`
- **Endpoint:** `POST /api/auth/login`
- **Purpose:** Authenticate user with email/password
- **Returns:** JWT access token + refresh token (HTTP-only cookie)
- **Validation:**
  - Email: Required, valid format
  - Password: Required
- **Audit:** HTTP request level only (password not logged)

**Example Request:**
```json
{
  "email": "admin@noir.local",
  "password": "123qwe"
}
```

**Example Response:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "550e8400-e29b-41d4-a716-446655440000",
  "expiresAt": "2026-01-22T12:00:00Z",
  "user": {
    "id": "123",
    "email": "admin@noir.local",
    "fullName": "Admin User",
    "roles": ["Admin"]
  }
}
```

#### Logout
- **Command:** `LogoutCommand`
- **Endpoint:** `POST /api/auth/logout`
- **Purpose:** Revoke refresh token
- **Returns:** Success message
- **Audit:** HTTP request level

#### Refresh Token
- **Command:** `RefreshTokenCommand`
- **Endpoint:** `POST /api/auth/refresh`
- **Purpose:** Get new access token using refresh token
- **Returns:** New JWT + new refresh token (token rotation)
- **Security:** Old refresh token is revoked
- **Audit:** HTTP request level

#### Get Current User
- **Query:** `GetCurrentUserQuery`
- **Endpoint:** `GET /api/auth/me`
- **Purpose:** Get authenticated user's profile
- **Returns:** UserProfileDto
- **Cache:** FusionCache (5 minutes)

#### Update Profile
- **Command:** `UpdateUserProfileCommand`
- **Endpoint:** `PUT /api/auth/profile`
- **Purpose:** Update user's first name, last name, display name, phone
- **Returns:** Updated UserProfileDto
- **Validation:**
  - FirstName: Required, max 100 chars
  - LastName: Required, max 100 chars
- **Audit:** IAuditableCommand (Handler + Entity levels)

#### Upload Avatar
- **Command:** `UploadAvatarCommand`
- **Endpoint:** `POST /api/auth/avatar`
- **Purpose:** Upload user avatar (image)
- **Returns:** Avatar URL
- **Storage:** FluentStorage (Local/Azure/S3)
- **Processing:** Resize to 200x200px (SixLabors.ImageSharp)
- **Validation:**
  - Max size: 5 MB
  - Formats: JPEG, PNG, WebP
- **Audit:** IAuditableCommand

#### Delete Avatar
- **Command:** `DeleteAvatarCommand`
- **Endpoint:** `DELETE /api/auth/avatar`
- **Purpose:** Remove user's avatar
- **Returns:** Success message
- **Audit:** IAuditableCommand

---

### Password Reset Flow

#### Send Password Reset OTP
- **Command:** `SendPasswordResetOtpCommand`
- **Endpoint:** `POST /api/auth/password/send-otp`
- **Purpose:** Generate and email 6-digit OTP
- **Returns:** Session token (for subsequent steps)
- **OTP Lifetime:** 10 minutes
- **Rate Limiting:** 1 OTP per minute per email
- **Email Template:** `PasswordResetOtp` (database-driven)

**Example Request:**
```json
{
  "email": "user@example.com"
}
```

**Example Response:**
```json
{
  "sessionToken": "550e8400-e29b-41d4-a716-446655440000",
  "expiresAt": "2026-01-22T12:10:00Z"
}
```

#### Verify Password Reset OTP
- **Command:** `VerifyPasswordResetOtpCommand`
- **Endpoint:** `POST /api/auth/password/verify-otp`
- **Purpose:** Validate OTP
- **Returns:** Success indicator
- **Max Attempts:** 3 (OTP marked as used after 3 failures)

**Example Request:**
```json
{
  "sessionToken": "550e8400-e29b-41d4-a716-446655440000",
  "otp": "123456"
}
```

#### Reset Password
- **Command:** `ResetPasswordCommand`
- **Endpoint:** `POST /api/auth/password/reset`
- **Purpose:** Set new password after OTP verification
- **Returns:** Success message
- **Validation:**
  - Password: Min 6 chars (or per IdentitySettings)
- **Security:** OTP is marked as used

**Example Request:**
```json
{
  "sessionToken": "550e8400-e29b-41d4-a716-446655440000",
  "otp": "123456",
  "newPassword": "NewSecurePassword123!"
}
```

---

## User Management

**Namespace:** `NOIR.Application.Features.Users`
**Endpoint:** `/api/users`
**Permissions:** `users:*`

### Commands

#### Create User
- **Command:** `CreateUserCommand`
- **Endpoint:** `POST /api/users`
- **Permission:** `users:create`
- **Purpose:** Create new user
- **Returns:** UserProfileDto
- **Validation:**
  - Email: Required, unique within tenant, valid format
  - Password: Required, min length per policy
  - FirstName/LastName: Required
  - RoleNames: Optional, validated against existing roles
- **Multi-Tenancy:** User is created in current tenant
- **Audit:** IAuditableCommand

**Example Request:**
```json
{
  "email": "newuser@example.com",
  "password": "SecurePassword123!",
  "firstName": "John",
  "lastName": "Doe",
  "displayName": "JD",
  "phoneNumber": "+1234567890",
  "roleNames": ["User"]
}
```

#### Update User
- **Command:** `UpdateUserCommand`
- **Endpoint:** `PUT /api/users/{id}`
- **Permission:** `users:update`
- **Purpose:** Update user details
- **Returns:** Updated UserProfileDto
- **Validation:**
  - Email: Unique within tenant if changed
  - FirstName/LastName: Required
- **Audit:** IAuditableCommand

#### Delete User
- **Command:** `DeleteUserCommand`
- **Endpoint:** `DELETE /api/users/{id}`
- **Permission:** `users:delete`
- **Purpose:** Soft delete user
- **Returns:** Success message
- **Security:** Cannot delete self
- **Audit:** IAuditableCommand

#### Assign Roles
- **Command:** `AssignRolesCommand`
- **Endpoint:** `POST /api/users/{id}/roles`
- **Permission:** `users:manage-roles`
- **Purpose:** Assign roles to user
- **Returns:** Success message
- **Validation:**
  - Roles must exist
  - Roles must be in same tenant as user
- **Audit:** IAuditableCommand

### Queries

#### Get Users
- **Query:** `GetUsersQuery`
- **Endpoint:** `GET /api/users`
- **Permission:** `users:read`
- **Purpose:** List users with search, filter, pagination
- **Returns:** PaginatedList<UserDto>
- **Filters:**
  - Search (by email, name)
  - Role filter
  - IsActive filter
- **Sorting:** Email, FirstName, LastName, CreatedAt
- **Cache:** FusionCache (2 minutes)

**Example Request:**
```
GET /api/users?search=john&roleFilter=Admin&pageNumber=1&pageSize=10
```

#### Get User Roles
- **Query:** `GetUserRolesQuery`
- **Endpoint:** `GET /api/users/{id}/roles`
- **Permission:** `users:read`
- **Purpose:** Get user's assigned roles
- **Returns:** List<RoleDto>

---

## Role & Permission Management

**Namespace:** `NOIR.Application.Features.Roles`, `Features.Permissions`
**Endpoint:** `/api/roles`, `/api/permissions`
**Permissions:** `roles:*`, `permissions:*`

### Role Commands

#### Create Role
- **Command:** `CreateRoleCommand`
- **Endpoint:** `POST /api/roles`
- **Permission:** `roles:create`
- **Purpose:** Create new role with permissions
- **Returns:** RoleDto
- **Validation:**
  - Name: Required, unique within tenant, max 256 chars
  - Permissions: Validated against scope (system-only vs tenant-allowed)
- **Multi-Tenancy:** Tenant roles cannot have system-only permissions
- **Audit:** IAuditableCommand

**Example Request:**
```json
{
  "name": "ContentEditor",
  "description": "Can manage blog posts",
  "permissions": [
    "blog-posts:read",
    "blog-posts:create",
    "blog-posts:update"
  ]
}
```

#### Update Role
- **Command:** `UpdateRoleCommand`
- **Endpoint:** `PUT /api/roles/{id}`
- **Permission:** `roles:update`
- **Purpose:** Update role name, description, permissions
- **Returns:** Updated RoleDto
- **Validation:** Same as CreateRole
- **Audit:** IAuditableCommand

#### Delete Role
- **Command:** `DeleteRoleCommand`
- **Endpoint:** `DELETE /api/roles/{id}`
- **Permission:** `roles:delete`
- **Purpose:** Delete role
- **Returns:** Success message
- **Validation:**
  - Cannot delete system roles (Admin, PlatformAdmin)
  - Role must not be assigned to any users
- **Audit:** IAuditableCommand

### Role Queries

#### Get Roles
- **Query:** `GetRolesQuery`
- **Endpoint:** `GET /api/roles`
- **Permission:** `roles:read`
- **Purpose:** List roles with pagination
- **Returns:** PaginatedList<RoleDto>
- **Filters:**
  - Search (by name, description)
  - TenantId filter (for platform admins)
- **Cache:** FusionCache (5 minutes)

#### Get Role By Id
- **Query:** `GetRoleByIdQuery`
- **Endpoint:** `GET /api/roles/{id}`
- **Permission:** `roles:read`
- **Purpose:** Get role details with permissions
- **Returns:** RoleDto
- **Cache:** FusionCache (5 minutes)

### Permission Commands

#### Assign To Role
- **Command:** `AssignToRoleCommand`
- **Endpoint:** `POST /api/permissions/assign`
- **Permission:** `roles:manage-permissions`
- **Purpose:** Add permissions to role
- **Returns:** Success message
- **Validation:** Permissions must be valid for tenant scope
- **Audit:** IAuditableCommand

#### Remove From Role
- **Command:** `RemoveFromRoleCommand`
- **Endpoint:** `POST /api/permissions/remove`
- **Permission:** `roles:manage-permissions`
- **Purpose:** Remove permissions from role
- **Returns:** Success message
- **Audit:** IAuditableCommand

### Permission Queries

#### Get Role Permissions
- **Query:** `GetRolePermissionsQuery`
- **Endpoint:** `GET /api/permissions/roles/{roleId}`
- **Permission:** `roles:read`
- **Purpose:** Get all permissions assigned to a role
- **Returns:** List<string> (permission keys)
- **Cache:** FusionCache (5 minutes)

#### Get User Permissions
- **Query:** `GetUserPermissionsQuery`
- **Endpoint:** `GET /api/permissions/users/{userId}`
- **Permission:** `users:read`
- **Purpose:** Get all permissions for a user (aggregated from roles)
- **Returns:** UserPermissionsDto
- **Cache:** FusionCache (5 minutes)

**Example Response:**
```json
{
  "userId": "123",
  "roles": ["Admin", "ContentEditor"],
  "permissions": [
    "users:read",
    "users:create",
    "blog-posts:read",
    "blog-posts:create",
    "blog-posts:update"
  ]
}
```

---

## Multi-Tenancy

**Namespace:** `NOIR.Application.Features.Tenants`
**Endpoint:** `/api/tenants`
**Permissions:** `tenants:*` (system-only)

### Commands

#### Create Tenant
- **Command:** `CreateTenantCommand`
- **Endpoint:** `POST /api/tenants`
- **Permission:** `tenants:create`
- **Purpose:** Create new tenant
- **Returns:** TenantDto
- **Validation:**
  - Identifier: Required, unique, slug format, max 50 chars
  - Name: Required, max 200 chars
- **Auto-Created:**
  - Admin role with default permissions
  - Admin user (if settings provided)
- **Audit:** IAuditableCommand

**Example Request:**
```json
{
  "identifier": "acme-corp",
  "name": "Acme Corporation",
  "domain": "acme.example.com",
  "description": "Acme Corp tenant",
  "adminEmail": "admin@acme.com",
  "adminPassword": "SecurePassword123!",
  "adminFirstName": "Admin",
  "adminLastName": "User"
}
```

#### Update Tenant
- **Command:** `UpdateTenantCommand`
- **Endpoint:** `PUT /api/tenants/{id}`
- **Permission:** `tenants:update`
- **Purpose:** Update tenant details
- **Returns:** Updated TenantDto
- **Validation:** Name and Domain can be updated
- **Audit:** IAuditableCommand

#### Delete Tenant
- **Command:** `DeleteTenantCommand`
- **Endpoint:** `DELETE /api/tenants/{id}`
- **Permission:** `tenants:delete`
- **Purpose:** Soft delete tenant (archive)
- **Returns:** Success message
- **Effect:** All tenant data is hidden (query filter)
- **Audit:** IAuditableCommand

#### Restore Tenant
- **Command:** `RestoreTenantCommand`
- **Endpoint:** `POST /api/tenants/{id}/restore`
- **Permission:** `tenants:update`
- **Purpose:** Restore archived tenant
- **Returns:** Success message
- **Audit:** IAuditableCommand

### Queries

#### Get Tenants
- **Query:** `GetTenantsQuery`
- **Endpoint:** `GET /api/tenants`
- **Permission:** `tenants:read`
- **Purpose:** List active tenants
- **Returns:** PaginatedList<TenantDto>
- **Filters:** Search (by identifier, name)
- **Cache:** FusionCache (5 minutes)

#### Get Archived Tenants
- **Query:** `GetArchivedTenantsQuery`
- **Endpoint:** `GET /api/tenants/archived`
- **Permission:** `tenants:read`
- **Purpose:** List soft-deleted tenants
- **Returns:** PaginatedList<TenantDto>

#### Get Tenant By Id
- **Query:** `GetTenantByIdQuery`
- **Endpoint:** `GET /api/tenants/{id}`
- **Permission:** `tenants:read`
- **Purpose:** Get tenant details
- **Returns:** TenantDto
- **Cache:** FusionCache (5 minutes)

#### Get Tenant Settings
- **Query:** `GetTenantSettingsQuery`
- **Endpoint:** `GET /api/tenants/{id}/settings`
- **Permission:** `tenants:read`
- **Purpose:** Get tenant configuration
- **Returns:** TenantSettingsDto (from TenantSettings table)

---

## Product Catalog

**Namespace:** `NOIR.Application.Features.Products`
**Endpoint:** `/api/products`, `/api/product-categories`
**Permissions:** `products:*`

### Features

#### Create Product
- **Command:** `CreateProductCommand`
- **Endpoint:** `POST /api/products`
- **Permission:** `products:create`
- **Purpose:** Create new product with variants, images, and categories
- **Returns:** ProductDto
- **Validation:**
  - Name: Required, max 200 chars
  - Slug: Required, unique within tenant
  - At least one variant required
- **Audit:** IAuditableCommand

**Example Request:**
```json
{
  "name": "Premium T-Shirt",
  "slug": "premium-t-shirt",
  "description": "High-quality cotton t-shirt",
  "shortDescription": "Premium cotton tee",
  "categoryId": "123e4567-e89b-12d3-a456-426614174000",
  "variants": [
    {
      "name": "Small / Black",
      "sku": "TSHIRT-S-BLK",
      "price": 29.99,
      "compareAtPrice": 39.99,
      "stockQuantity": 100
    }
  ]
}
```

#### Update Product
- **Command:** `UpdateProductCommand`
- **Endpoint:** `PUT /api/products/{id}`
- **Permission:** `products:update`
- **Purpose:** Update product details
- **Returns:** ProductDto
- **Audit:** IAuditableCommand with before-state resolver

#### Publish Product
- **Command:** `PublishProductCommand`
- **Endpoint:** `POST /api/products/{id}/publish`
- **Permission:** `products:update`
- **Purpose:** Change product status from Draft to Active
- **Returns:** ProductDto

#### Archive Product
- **Command:** `ArchiveProductCommand`
- **Endpoint:** `POST /api/products/{id}/archive`
- **Permission:** `products:update`
- **Purpose:** Mark product as archived (soft archive)
- **Returns:** ProductDto

#### Get Products
- **Query:** `GetProductsQuery`
- **Endpoint:** `GET /api/products`
- **Permission:** `products:read`
- **Purpose:** List products with pagination, filtering, search
- **Returns:** PaginatedList<ProductDto>
- **Query Parameters:**
  - `search`: Full-text search on name/description
  - `categoryId`: Filter by category
  - `status`: Filter by ProductStatus (Draft, Active, Archived)
  - `page`, `pageSize`: Pagination

#### Get Product By Id
- **Query:** `GetProductByIdQuery`
- **Endpoint:** `GET /api/products/{id}`
- **Permission:** `products:read`
- **Purpose:** Get product details with variants and images
- **Returns:** ProductDto

### Product Categories

#### Create Product Category
- **Command:** `CreateProductCategoryCommand`
- **Endpoint:** `POST /api/product-categories`
- **Permission:** `products:create`
- **Purpose:** Create product category (supports hierarchy)
- **Returns:** ProductCategoryDto

#### Update Product Category
- **Command:** `UpdateProductCategoryCommand`
- **Endpoint:** `PUT /api/product-categories/{id}`
- **Permission:** `products:update`
- **Returns:** ProductCategoryDto

#### Delete Product Category
- **Command:** `DeleteProductCategoryCommand`
- **Endpoint:** `DELETE /api/product-categories/{id}`
- **Permission:** `products:delete`
- **Purpose:** Soft delete category
- **Returns:** Success

#### Get Product Categories
- **Query:** `GetProductCategoriesQuery`
- **Endpoint:** `GET /api/product-categories`
- **Permission:** `products:read`
- **Purpose:** List all categories (hierarchical)
- **Returns:** List<ProductCategoryDto>

### Domain Entities

- **Product** - Aggregate root with variants, images, categories
- **ProductVariant** - SKU, price, inventory, attributes
- **ProductImage** - Image URL, alt text, display order
- **ProductCategory** - Hierarchical category structure

### Enums

- **ProductStatus** - Draft, Active, Archived

---

## Product Attributes

**Namespace:** `NOIR.Application.Features.ProductAttributes`
**Endpoint:** `/api/product-attributes`
**Permissions:** `attributes:*`

> Dynamic attribute system supporting 13 attribute types with category linkage, product assignment, and faceted filtering.

### Attribute Management

#### Create Product Attribute
- **Command:** `CreateProductAttributeCommand`
- **Endpoint:** `POST /api/product-attributes`
- **Permission:** `attributes:create`
- **Purpose:** Create a new product attribute definition
- **Returns:** ProductAttributeDto
- **Validation:**
  - Code: Required, unique within tenant
  - Name: Required
  - Type: Required, valid AttributeType
- **Audit:** IAuditableCommand

**Example Request:**
```json
{
  "code": "color",
  "name": "Color",
  "type": "Select",
  "isFilterable": true,
  "isSearchable": true,
  "isVariantAttribute": true,
  "showInProductCard": true,
  "showInSpecifications": true,
  "placeholder": "Select a color"
}
```

#### Update Product Attribute
- **Command:** `UpdateProductAttributeCommand`
- **Endpoint:** `PUT /api/product-attributes/{id}`
- **Permission:** `attributes:update`
- **Purpose:** Update attribute definition (code, name, flags, validation)
- **Returns:** ProductAttributeDto
- **Audit:** IAuditableCommand

#### Delete Product Attribute
- **Command:** `DeleteProductAttributeCommand`
- **Endpoint:** `DELETE /api/product-attributes/{id}`
- **Permission:** `attributes:delete`
- **Purpose:** Soft delete a product attribute
- **Returns:** Success
- **Audit:** IAuditableCommand

#### Get Product Attributes
- **Query:** `GetProductAttributesQuery`
- **Endpoint:** `GET /api/product-attributes`
- **Permission:** `attributes:read`
- **Purpose:** List attributes with pagination and filtering
- **Returns:** PagedResult<ProductAttributeListDto>
- **Query Parameters:**
  - `search`: Search by code or name
  - `isActive`: Filter by active status
  - `isFilterable`: Filter by filterable flag
  - `isVariantAttribute`: Filter by variant attribute flag
  - `type`: Filter by attribute type

#### Get Product Attribute By Id
- **Query:** `GetProductAttributeByIdQuery`
- **Endpoint:** `GET /api/product-attributes/{id}`
- **Permission:** `attributes:read`
- **Purpose:** Get attribute details with all values
- **Returns:** ProductAttributeDto

### Attribute Values

#### Add Attribute Value
- **Command:** `AddProductAttributeValueCommand`
- **Endpoint:** `POST /api/product-attributes/{id}/values`
- **Permission:** `attributes:update`
- **Purpose:** Add a predefined value to a Select/MultiSelect attribute
- **Returns:** ProductAttributeValueDto

**Example Request:**
```json
{
  "value": "red",
  "displayValue": "Red",
  "colorCode": "#FF0000",
  "sortOrder": 1
}
```

#### Update Attribute Value
- **Command:** `UpdateProductAttributeValueCommand`
- **Endpoint:** `PUT /api/product-attributes/{id}/values/{valueId}`
- **Permission:** `attributes:update`
- **Returns:** ProductAttributeValueDto

#### Remove Attribute Value
- **Command:** `RemoveProductAttributeValueCommand`
- **Endpoint:** `DELETE /api/product-attributes/{id}/values/{valueId}`
- **Permission:** `attributes:update`
- **Returns:** Success

### Category-Attribute Linkage

#### Assign Attribute to Category
- **Command:** `AssignCategoryAttributeCommand`
- **Purpose:** Link an attribute to a product category
- **Returns:** CategoryAttributeDto

#### Update Category Attribute
- **Command:** `UpdateCategoryAttributeCommand`
- **Purpose:** Update linkage settings (isRequired, sortOrder)

#### Remove Category Attribute
- **Command:** `RemoveCategoryAttributeCommand`
- **Purpose:** Unlink an attribute from a category

#### Get Category Attributes
- **Query:** `GetCategoryAttributesQuery`
- **Purpose:** Get all attributes linked to a category

### Product-Attribute Assignment

#### Set Product Attribute Value
- **Command:** `SetProductAttributeValueCommand`
- **Purpose:** Set a single attribute value on a product (or variant)

#### Bulk Update Product Attributes
- **Command:** `BulkUpdateProductAttributesCommand`
- **Purpose:** Set multiple attribute values for a product at once

#### Get Product Attribute Assignments
- **Query:** `GetProductAttributeAssignmentsQuery`
- **Purpose:** Get all attribute values assigned to a product

### Form Schema Queries

#### Get Product Attribute Form Schema
- **Query:** `GetProductAttributeFormSchemaQuery`
- **Purpose:** Generate a dynamic form schema for a specific product (with current values)
- **Returns:** ProductAttributeFormSchemaDto (fields, types, options, current values)

#### Get Category Attribute Form Schema
- **Query:** `GetCategoryAttributeFormSchemaQuery`
- **Purpose:** Generate a dynamic form schema for a category (for new product creation)
- **Returns:** CategoryAttributeFormSchemaDto

### Attribute Types

| Type | Description | Use Case |
|------|-------------|----------|
| `Select` | Single selection from predefined options | Color, Size, Material |
| `MultiSelect` | Multiple selections from predefined options | Features, Tags |
| `Text` | Short text input | Brand model number |
| `TextArea` | Long text input | Custom description |
| `Number` | Integer input | Warranty months |
| `Decimal` | Decimal input | Weight (kg) |
| `Boolean` | Yes/No toggle | Is Organic, Is Waterproof |
| `Date` | Date picker | Manufacturing date |
| `DateTime` | Date + time picker | Expiration date |
| `Color` | Color picker with hex value | Primary color |
| `Range` | Min/max range input | Price range |
| `Url` | URL input with validation | Product manual link |
| `File` | File upload | Technical spec PDF |

### Domain Entities

- **ProductAttribute** - Attribute definition (code, name, type, validation rules)
- **ProductAttributeValue** - Predefined options for Select/MultiSelect attributes
- **CategoryAttribute** - Many-to-many link between Category and Attribute
- **ProductAttributeAssignment** - Actual attribute values assigned to products

---

## Brands

**Namespace:** `NOIR.Application.Features.Brands`
**Endpoint:** `/api/brands`
**Permissions:** `brands:*`

> Product brand management with logo, banner, SEO metadata, and featured status.

### Commands

#### Create Brand
- **Command:** `CreateBrandCommand`
- **Endpoint:** `POST /api/brands`
- **Permission:** `brands:create`
- **Purpose:** Create a new product brand
- **Returns:** BrandDto
- **Validation:**
  - Name: Required
  - Slug: Required, unique within tenant
- **Audit:** IAuditableCommand

**Example Request:**
```json
{
  "name": "Nike",
  "slug": "nike",
  "logoUrl": "/media/brands/nike-logo.webp",
  "bannerUrl": "/media/brands/nike-banner.webp",
  "description": "Leading sports apparel brand",
  "website": "https://nike.com",
  "metaTitle": "Nike | Premium Sportswear",
  "metaDescription": "Shop Nike products",
  "isFeatured": true
}
```

#### Update Brand
- **Command:** `UpdateBrandCommand`
- **Endpoint:** `PUT /api/brands/{id}`
- **Permission:** `brands:update`
- **Purpose:** Update brand details
- **Returns:** BrandDto
- **Audit:** IAuditableCommand

#### Delete Brand
- **Command:** `DeleteBrandCommand`
- **Endpoint:** `DELETE /api/brands/{id}`
- **Permission:** `brands:delete`
- **Purpose:** Soft delete brand
- **Returns:** Success
- **Validation:** Brand must not have associated products
- **Audit:** IAuditableCommand

### Queries

#### Get Brands
- **Query:** `GetBrandsQuery`
- **Endpoint:** `GET /api/brands`
- **Permission:** `brands:read`
- **Purpose:** List brands with pagination and filtering
- **Returns:** PagedResult<BrandListDto>
- **Query Parameters:**
  - `search`: Search by name
  - `isActive`: Filter by active status
  - `isFeatured`: Filter by featured status

#### Get Brand By Id
- **Query:** `GetBrandByIdQuery`
- **Endpoint:** `GET /api/brands/{id}`
- **Permission:** `brands:read`
- **Purpose:** Get brand details with product count
- **Returns:** BrandDto

### Domain Entity

- **Brand** - Aggregate root with Name, Slug, LogoUrl, BannerUrl, Description, Website, SEO fields (MetaTitle, MetaDescription), IsActive, IsFeatured, SortOrder

---

## Shopping Cart

**Namespace:** `NOIR.Application.Features.Cart`
**Endpoint:** `/api/cart`
**Permissions:** No auth required (guest support)

### Features

#### Add to Cart
- **Command:** `AddToCartCommand`
- **Endpoint:** `POST /api/cart/items`
- **Auth:** Optional (supports guest users via SessionId)
- **Purpose:** Add product variant to cart
- **Returns:** CartDto
- **Validation:**
  - ProductId: Required, must exist
  - ProductVariantId: Required, must exist
  - Quantity: Min 1
  - Stock check: Must have sufficient inventory
- **Audit:** IAuditableCommand (Create)

**Example Request:**
```json
{
  "productId": "123e4567-e89b-12d3-a456-426614174000",
  "productVariantId": "223e4567-e89b-12d3-a456-426614174000",
  "quantity": 2
}
```

#### Update Cart Item
- **Command:** `UpdateCartItemCommand`
- **Endpoint:** `PUT /api/cart/items/{itemId}`
- **Purpose:** Update quantity of cart item
- **Returns:** CartDto
- **Validation:**
  - Quantity: 0 or more (0 removes item)
  - Stock check on increase
- **Audit:** IAuditableCommand (Update)

#### Remove Cart Item
- **Command:** `RemoveCartItemCommand`
- **Endpoint:** `DELETE /api/cart/items/{itemId}`
- **Purpose:** Remove item from cart
- **Returns:** CartDto
- **Audit:** IAuditableCommand (Delete)

#### Clear Cart
- **Command:** `ClearCartCommand`
- **Endpoint:** `DELETE /api/cart`
- **Purpose:** Remove all items from cart
- **Returns:** CartDto
- **Audit:** IAuditableCommand (Delete)

#### Merge Cart
- **Command:** `MergeCartCommand`
- **Endpoint:** `POST /api/cart/merge`
- **Auth:** Required (merge guest cart on login)
- **Purpose:** Merge guest session cart into authenticated user's cart
- **Returns:** CartDto
- **Behavior:**
  - Combines items from guest and user carts
  - Updates quantities for matching variants
  - Marks guest cart as Merged

#### Get Cart
- **Query:** `GetCartQuery`
- **Endpoint:** `GET /api/cart`
- **Purpose:** Get current cart with all items
- **Returns:** CartDto with full item details

#### Get Cart Summary
- **Query:** `GetCartSummaryQuery`
- **Endpoint:** `GET /api/cart/summary`
- **Purpose:** Get cart totals only (lightweight)
- **Returns:** CartSummaryDto (itemCount, subtotal)

### Guest Cart Support

Shopping cart supports both authenticated and guest users:

1. **Authenticated Users**: Cart linked to UserId
2. **Guest Users**: Cart linked to SessionId (stored in cookie/header)
3. **Cart Merge**: When guest logs in, their cart merges with any existing user cart

### Domain Entities

- **Cart** - Aggregate root (UserId or SessionId, CartStatus)
- **CartItem** - Product variant reference, quantity, unit price

### Enums

- **CartStatus** - Active, Merged, Abandoned, Converted

### Domain Events

- **CartItemAddedEvent** - Fired when item added
- **CartItemUpdatedEvent** - Fired when quantity changed
- **CartItemRemovedEvent** - Fired when item removed
- **CartClearedEvent** - Fired when cart cleared
- **CartMergedEvent** - Fired when guest cart merged

---

## Checkout

**Namespace:** `NOIR.Application.Features.Checkout`
**Endpoint:** `/api/checkout`
**Permissions:** `checkout:*`

> ⭐ **NEW FEATURE:** Complete checkout flow with hybrid accordion pattern supporting address, shipping, payment method selection.

### Features

#### Initiate Checkout
- **Command:** `InitiateCheckoutCommand`
- **Endpoint:** `POST /api/checkout`
- **Auth:** Required
- **Purpose:** Create a checkout session from user's cart
- **Returns:** CheckoutSessionDto
- **Validation:**
  - Cart must have items
  - All items must be in stock
  - Session expires in 30 minutes (configurable)
- **Audit:** IAuditableCommand (Create)

**Example Request:**
```json
{
  "cartId": "123e4567-e89b-12d3-a456-426614174000"
}
```

**Example Response:**
```json
{
  "id": "session-789",
  "status": "Active",
  "expiresAt": "2026-01-26T12:30:00Z",
  "cartId": "cart-123",
  "items": [...],
  "subtotal": 99.99,
  "currentStep": "Address"
}
```

#### Set Checkout Address
- **Command:** `SetCheckoutAddressCommand`
- **Endpoint:** `PUT /api/checkout/{id}/address`
- **Purpose:** Set shipping and billing address
- **Returns:** CheckoutSessionDto
- **Validation:**
  - Session must be active
  - Address fields validated
- **Audit:** IAuditableCommand (Update)

**Example Request:**
```json
{
  "shippingAddress": {
    "fullName": "John Doe",
    "phone": "+84901234567",
    "addressLine1": "123 Main St",
    "ward": "Ward 1",
    "district": "District 1",
    "province": "Ho Chi Minh City",
    "country": "Vietnam"
  },
  "billingAddressSameAsShipping": true
}
```

#### Select Shipping Method
- **Command:** `SelectShippingCommand`
- **Endpoint:** `PUT /api/checkout/{id}/shipping`
- **Purpose:** Select shipping method and calculate cost
- **Returns:** CheckoutSessionDto with updated totals
- **Validation:**
  - Address must be set first
  - Shipping method must be available for address
- **Audit:** IAuditableCommand (Update)

**Example Request:**
```json
{
  "shippingMethodId": "express-delivery",
  "shippingNotes": "Please call before delivery"
}
```

#### Select Payment Method
- **Command:** `SelectPaymentCommand`
- **Endpoint:** `PUT /api/checkout/{id}/payment`
- **Purpose:** Select payment method for order
- **Returns:** CheckoutSessionDto
- **Validation:**
  - Shipping must be selected first
  - Payment gateway must be active
- **Audit:** IAuditableCommand (Update)

**Example Request:**
```json
{
  "paymentGatewayId": "gateway-456",
  "paymentMethod": "CreditCard"
}
```

#### Complete Checkout
- **Command:** `CompleteCheckoutCommand`
- **Endpoint:** `POST /api/checkout/{id}/complete`
- **Purpose:** Create order from checkout session
- **Returns:** OrderDto with payment redirect URL (if applicable)
- **Validation:**
  - All steps must be complete
  - Stock must still be available
  - Session must not be expired
- **Side Effects:**
  - Creates Order entity
  - Reserves inventory
  - Initiates payment (if not COD)
  - Marks cart as Converted
- **Audit:** IAuditableCommand (Create)

#### Get Checkout Session
- **Query:** `GetCheckoutSessionQuery`
- **Endpoint:** `GET /api/checkout/{id}`
- **Purpose:** Get checkout session details
- **Returns:** CheckoutSessionDto with current state

### Domain Entities

- **CheckoutSession** - Aggregate root tracking checkout progress
  - `CartId` - Source cart
  - `UserId` - Authenticated user
  - `Status` - Active, Completed, Expired, Abandoned
  - `ShippingAddress` - Address value object
  - `BillingAddress` - Address value object (optional)
  - `ShippingMethodId` - Selected shipping
  - `PaymentGatewayId` - Selected payment gateway
  - `PaymentMethod` - Selected payment method
  - `ExpiresAt` - Session expiry time
  - `CompletedAt` - When checkout was completed

### Enums

- **CheckoutSessionStatus** - Active, Completed, Expired, Abandoned

### Domain Events

- **CheckoutStartedEvent** - Session created
- **CheckoutAddressSetEvent** - Address saved
- **CheckoutShippingSelectedEvent** - Shipping method selected
- **CheckoutPaymentSelectedEvent** - Payment method selected
- **CheckoutCompletedEvent** - Order created

---

## Orders

**Namespace:** `NOIR.Application.Features.Orders`
**Endpoint:** `/api/orders`
**Permissions:** `orders:*`

> ⭐ **NEW FEATURE:** Complete order lifecycle management with status transitions, inventory management, and fulfillment tracking.

### Features

#### Create Order
- **Command:** `CreateOrderCommand`
- **Endpoint:** `POST /api/orders`
- **Permission:** Internal (created via checkout completion)
- **Purpose:** Create order from completed checkout
- **Returns:** OrderDto
- **Side Effects:**
  - Reserves inventory (InventoryMovement)
  - Generates order number
  - Sets initial status to Pending
- **Audit:** IAuditableCommand (Create)

#### Confirm Order
- **Command:** `ConfirmOrderCommand`
- **Endpoint:** `POST /api/orders/{id}/confirm`
- **Permission:** `orders:update`
- **Purpose:** Confirm order after payment verification
- **Returns:** OrderDto
- **Validation:**
  - Order must be in Pending status
  - Payment must be verified
- **Audit:** IAuditableCommand (Update)

#### Ship Order
- **Command:** `ShipOrderCommand`
- **Endpoint:** `POST /api/orders/{id}/ship`
- **Permission:** `orders:update`
- **Purpose:** Mark order as shipped
- **Returns:** OrderDto
- **Validation:**
  - Order must be in Confirmed or Processing status
  - Tracking number optional
- **Audit:** IAuditableCommand (Update)

**Example Request:**
```json
{
  "trackingNumber": "VN123456789",
  "carrier": "GHTK",
  "estimatedDelivery": "2026-01-28"
}
```

#### Deliver Order
- **Command:** `DeliverOrderCommand`
- **Endpoint:** `POST /api/orders/{id}/deliver`
- **Permission:** `OrdersWrite`
- **Purpose:** Mark order as delivered to customer
- **Returns:** OrderDto
- **Validation:** Order must be in Shipped status
- **Audit:** IAuditableCommand (Update)

#### Complete Order
- **Command:** `CompleteOrderCommand`
- **Endpoint:** `POST /api/orders/{id}/complete`
- **Permission:** `OrdersWrite`
- **Purpose:** Mark order as completed after successful delivery
- **Returns:** OrderDto
- **Validation:** Order must be in Delivered status
- **Audit:** IAuditableCommand (Update)

#### Return Order
- **Command:** `ReturnOrderCommand`
- **Endpoint:** `POST /api/orders/{id}/return`
- **Permission:** `OrdersManage`
- **Purpose:** Mark order as returned, release inventory back to stock
- **Returns:** OrderDto
- **Accepts:** Optional return reason
- **Audit:** IAuditableCommand (Update)

#### Cancel Order
- **Command:** `CancelOrderCommand`
- **Endpoint:** `POST /api/orders/{id}/cancel`
- **Permission:** `OrdersWrite`
- **Purpose:** Cancel order and release inventory
- **Returns:** OrderDto
- **Validation:**
  - Order must be cancellable (not shipped/delivered)
- **Side Effects:**
  - Releases inventory reservation
  - Triggers refund if paid
- **Audit:** IAuditableCommand (Update)

**Example Request:**
```json
{
  "reason": "Customer requested cancellation"
}
```

#### Get Orders
- **Query:** `GetOrdersQuery`
- **Endpoint:** `GET /api/orders`
- **Permission:** `orders:read`
- **Purpose:** List orders with filtering
- **Returns:** PagedResult<OrderListDto>
- **Query Parameters:**
  - `status` - Filter by OrderStatus
  - `dateFrom` / `dateTo` - Date range
  - `search` - Order number, customer name
  - `page`, `pageSize` - Pagination
- **Sorting:** CreatedAt (descending)

#### Get Order By Id
- **Query:** `GetOrderByIdQuery`
- **Endpoint:** `GET /api/orders/{id}`
- **Permission:** `orders:read`
- **Purpose:** Get full order details with items
- **Returns:** OrderDto

**Example Response:**
```json
{
  "id": "order-123",
  "orderNumber": "ORD-2026-000456",
  "status": "Confirmed",
  "createdAt": "2026-01-26T10:00:00Z",
  "shippingAddress": {...},
  "items": [
    {
      "id": "item-1",
      "productId": "prod-123",
      "productName": "Premium T-Shirt",
      "variantId": "var-456",
      "variantName": "Small / Black",
      "sku": "TSHIRT-S-BLK",
      "quantity": 2,
      "unitPrice": 29.99,
      "totalPrice": 59.98
    }
  ],
  "subtotal": 59.98,
  "shippingCost": 5.00,
  "taxAmount": 6.50,
  "total": 71.48,
  "paymentStatus": "Paid",
  "paymentMethod": "CreditCard"
}
```

### Domain Entities

- **Order** - Aggregate root for order lifecycle
  - `OrderNumber` - Human-readable number (ORD-YYYY-NNNNNN)
  - `UserId` - Customer
  - `Status` - OrderStatus enum
  - `ShippingAddress` - Delivery address (snapshot)
  - `BillingAddress` - Billing address (snapshot)
  - `Items` - Collection of OrderItem
  - `Subtotal`, `ShippingCost`, `TaxAmount`, `Total`
  - `PaymentTransactionId` - Linked payment
  - `TrackingNumber`, `Carrier` - Shipping info
  - `CancelledAt`, `CancellationReason`

- **OrderItem** - Product snapshot at time of order
  - `ProductId`, `ProductName` - Product reference + snapshot
  - `VariantId`, `VariantName`, `Sku` - Variant snapshot
  - `Quantity`, `UnitPrice`, `TotalPrice`
  - `ImageUrl` - Product image snapshot

### Enums

- **OrderStatus**
| Value | Description |
|-------|-------------|
| `Pending` | Order created, awaiting payment confirmation |
| `Confirmed` | Payment verified, ready for processing |
| `Processing` | Order being prepared |
| `Shipped` | Order shipped, in transit |
| `Delivered` | Order delivered to customer |
| `Completed` | Order fulfilled, review period ended |
| `Cancelled` | Order cancelled |
| `Refunded` | Order refunded |
| `Returned` | Order returned by customer |

### Order Lifecycle State Machine

```
Pending → Confirmed → Processing → Shipped → Delivered → Completed
   ↓                                  ↓         ↓
Cancelled                          Cancelled  Returned
```

### Domain Events

- **OrderCreatedEvent** - Order created from checkout
- **OrderConfirmedEvent** - Payment verified
- **OrderShippedEvent** - Order dispatched
- **OrderDeliveredEvent** - Order delivered
- **OrderCompletedEvent** - Order fulfilled
- **OrderReturnedEvent** - Order returned by customer
- **OrderCancelledEvent** - Order cancelled

### Inventory Integration

Orders integrate with inventory through:
1. **Reservation on Checkout** - Inventory reserved when checkout initiated
2. **Deduction on Ship** - Actual inventory deducted when shipped
3. **Release on Cancel** - Reservation released if cancelled before ship
4. **Return Restock** - Inventory restored on customer returns

---

## Audit Logging

**Namespace:** `NOIR.Application.Features.Audit`
**Endpoint:** `/api/audit`
**Permissions:** `audit:*`

### 3-Level Audit System

1. **HTTP Request Level** - All API requests (logged via middleware)
2. **Handler Level** - Commands implementing `IAuditableCommand`
3. **Entity Level** - Entity changes (tracked via `EntityAuditLogInterceptor`)

### Commands

#### Bulk Export
- **Command:** `BulkExportAuditLogsCommand`
- **Endpoint:** `POST /api/audit/export`
- **Permission:** `audit:export`
- **Purpose:** Export audit logs to CSV
- **Returns:** File stream (CSV)
- **Filters:** Date range, operation type, entity type
- **Performance:** Uses `EFCore.BulkExtensions` for efficient bulk queries

**Example Request:**
```json
{
  "startDate": "2026-01-01T00:00:00Z",
  "endDate": "2026-01-31T23:59:59Z",
  "operationType": "Update",
  "entityType": "User"
}
```

### Queries

#### Get Audit Logs
- **Query:** `GetAuditLogsQuery`
- **Endpoint:** `GET /api/audit/logs`
- **Permission:** `audit:read`
- **Purpose:** List audit logs with search, filter, pagination
- **Returns:** PaginatedList<EntityAuditLogDto>
- **Filters:**
  - Date range
  - Operation type (Create, Update, Delete)
  - Entity type
  - User ID
  - Search (entity name, changes)
- **Sorting:** Timestamp (descending by default)

**Example Request:**
```
GET /api/audit/logs?startDate=2026-01-01&endDate=2026-01-31&operationType=Update&entityType=User&pageNumber=1&pageSize=20
```

**Example Response:**
```json
{
  "items": [
    {
      "id": "123",
      "entityType": "User",
      "entityId": "user-456",
      "operationType": "Update",
      "userId": "admin-789",
      "userName": "Admin User",
      "timestamp": "2026-01-22T10:30:00Z",
      "changes": "{\"Email\":{\"Old\":\"old@example.com\",\"New\":\"new@example.com\"}}",
      "snapshot": "{...}"
    }
  ],
  "pageNumber": 1,
  "totalPages": 5,
  "totalCount": 100,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

#### Get Entity History
- **Query:** `GetEntityHistoryQuery`
- **Endpoint:** `GET /api/audit/entity-history/{entityType}/{entityId}`
- **Permission:** `audit:entity-history`
- **Purpose:** Get all changes to a specific entity
- **Returns:** List<EntityAuditLogDto> (chronological order)
- **Use Case:** Activity Timeline in frontend

**Example Request:**
```
GET /api/audit/entity-history/User/user-123
```

---

## Notifications

**Namespace:** `NOIR.Application.Features.Notifications`
**Endpoint:** `/api/notifications`
**Permissions:** `notifications:*` (implicit for own notifications)

### Real-Time Architecture

- **SignalR Hub:** `NotificationHub` at `/hubs/notifications`
- **Push Method:** `ReceiveNotification`
- **Connection:** Automatic via `NotificationContext.tsx`

### Commands

#### Mark As Read
- **Command:** `MarkNotificationAsReadCommand`
- **Endpoint:** `POST /api/notifications/{id}/mark-read`
- **Purpose:** Mark single notification as read
- **Returns:** Success message
- **Audit:** Not logged (lightweight action)

#### Mark All As Read
- **Command:** `MarkAllNotificationsAsReadCommand`
- **Endpoint:** `POST /api/notifications/mark-all-read`
- **Purpose:** Mark all user's notifications as read
- **Returns:** Count of updated notifications
- **Audit:** Not logged

#### Delete Notification
- **Command:** `DeleteNotificationCommand`
- **Endpoint:** `DELETE /api/notifications/{id}`
- **Purpose:** Delete notification
- **Returns:** Success message
- **Audit:** Not logged

### Queries

#### Get Notifications
- **Query:** `GetNotificationsQuery`
- **Endpoint:** `GET /api/notifications`
- **Purpose:** List user's notifications
- **Returns:** PaginatedList<NotificationDto>
- **Filters:**
  - IsRead
  - Type (Success, Info, Warning, Error)
- **Sorting:** CreatedAt (descending)

#### Get Unread Count
- **Query:** `GetUnreadCountQuery`
- **Endpoint:** `GET /api/notifications/unread-count`
- **Purpose:** Get count of unread notifications
- **Returns:** int
- **Cache:** FusionCache (1 minute)

### Notification Types

```csharp
public enum NotificationType
{
    Success,    // Green - Success messages
    Info,       // Blue - Informational
    Warning,    // Yellow - Warnings
    Error       // Red - Errors
}
```

### Sending Notifications

```csharp
// Via NotificationService (Infrastructure)
await _notificationService.SendToUserAsync(
    userId: "user-123",
    title: "Profile Updated",
    message: "Your profile was updated successfully.",
    type: NotificationType.Success
);

// Via SignalR directly
await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", notification);
```

---

## Email Templates

**Namespace:** `NOIR.Application.Features.EmailTemplates`
**Endpoint:** `/api/email-templates`
**Permissions:** `email-templates:*`

### Architecture

- **Database-Driven** - Templates stored in `EmailTemplate` table (not .cshtml files)
- **Multi-Tenant** - Copy-on-write pattern:
  - Platform defaults (`TenantId = null`)
  - Tenant overrides (`TenantId = specific`)
- **Variables** - Mustache-style `{{variable}}` syntax
- **Seeding** - Platform templates seeded in `ApplicationDbContextSeeder`

### Template Keys

| Key | Purpose | Variables |
|-----|---------|-----------|
| `PasswordResetOtp` | Password reset OTP email | `{{UserName}}`, `{{OtpCode}}`, `{{ExpiresInMinutes}}` |
| `EmailChangeOtp` | Email change OTP | `{{UserName}}`, `{{NewEmail}}`, `{{OtpCode}}`, `{{ExpiresInMinutes}}` |
| `WelcomeEmail` | User welcome | `{{UserName}}`, `{{TenantName}}`, `{{LoginUrl}}` |
| `TenantCreated` | Tenant creation | `{{TenantName}}`, `{{AdminEmail}}`, `{{LoginUrl}}` |

### Commands

#### Update Email Template
- **Command:** `UpdateEmailTemplateCommand`
- **Endpoint:** `PUT /api/email-templates/{id}`
- **Permission:** `email-templates:update`
- **Purpose:** Customize template (subject, HTML body, variables)
- **Returns:** Updated EmailTemplateDto
- **Multi-Tenancy:**
  - Platform admins edit platform defaults
  - Tenant admins create tenant overrides (copy-on-write)
- **Validation:**
  - Subject: Required, max 500 chars
  - HtmlBody: Required
  - Variables: Validated against template key
- **Audit:** IAuditableCommand

### Queries

#### Get Email Templates
- **Query:** `GetEmailTemplatesQuery`
- **Endpoint:** `GET /api/email-templates`
- **Permission:** `email-templates:read`
- **Purpose:** List templates
- **Returns:** List<EmailTemplateDto>
- **Logic:**
  - Tenant users see tenant overrides + platform defaults
  - Platform admins see all
- **Cache:** FusionCache (10 minutes)

#### Get Email Template By Id
- **Query:** `GetEmailTemplateByIdQuery`
- **Endpoint:** `GET /api/email-templates/{id}`
- **Permission:** `email-templates:read`
- **Purpose:** Get template details
- **Returns:** EmailTemplateDto
- **Cache:** FusionCache (10 minutes)

### Preview Template
- **Query:** `PreviewEmailTemplateQuery`
- **Endpoint:** `POST /api/email-templates/{id}/preview`
- **Permission:** `email-templates:read`
- **Purpose:** Render template with sample data
- **Returns:** Rendered HTML
- **Use Case:** Preview in admin UI before saving

**Example Request:**
```json
{
  "variables": {
    "UserName": "John Doe",
    "OtpCode": "123456",
    "ExpiresInMinutes": "10"
  }
}
```

---

## Legal Pages

**Namespace:** `NOIR.Application.Features.LegalPages`
**Endpoint:** `/api/legal-pages`, `/api/public/legal`
**Permissions:** `legal-pages:*`

### Architecture

- **Copy-on-Write** - Same pattern as Email Templates:
  - Platform defaults (`TenantId = null`) seeded on startup
  - Tenant overrides created on first edit (COW)
  - Revert-to-default deletes tenant copy
- **Entity:** `LegalPage` extends `PlatformTenantAggregateRoot<Guid>`
- **Rich Editor:** TinyMCE (self-hosted) with image upload support
- **SEO Fields:** MetaTitle, MetaDescription, CanonicalUrl, AllowIndexing
- **Seeding:** Platform templates seeded in `ApplicationDbContextSeeder`

### Seeded Pages

| Slug | Title | Purpose |
|------|-------|---------|
| `terms-of-service` | Terms of Service | Platform terms and conditions |
| `privacy-policy` | Privacy Policy | Data privacy and handling policy |

### SEO Fields

| Field | Max Length | Default | Purpose |
|-------|-----------|---------|---------|
| `MetaTitle` | 60 chars | null | Page title for search engines |
| `MetaDescription` | 160 chars | null | Brief description for search results |
| `CanonicalUrl` | 500 chars | null | Preferred URL for deduplication |
| `AllowIndexing` | bool | true | Whether search engines can index |

### Commands

#### Update Legal Page
- **Command:** `UpdateLegalPageCommand`
- **Endpoint:** `PUT /api/legal-pages/{id}`
- **Permission:** `legal-pages:update`
- **Purpose:** Update legal page content and SEO fields
- **Returns:** LegalPageDto
- **Multi-Tenancy:**
  - Platform admin → edits platform default in place
  - Tenant admin editing platform page → creates tenant copy (COW)
  - Tenant admin editing own copy → updates in place
- **Validation:**
  - Title: Required, max 200 chars
  - HtmlContent: Required
  - MetaTitle: Max 60 chars
  - MetaDescription: Max 160 chars
  - CanonicalUrl: Max 500 chars, valid URL format
- **Audit:** IAuditableCommand

**Example Request:**
```json
{
  "title": "Terms of Service",
  "htmlContent": "<h1>Terms of Service</h1><p>...</p>",
  "metaTitle": "Terms of Service | NOIR Platform",
  "metaDescription": "Read our terms and conditions for using the platform.",
  "canonicalUrl": "https://example.com/terms",
  "allowIndexing": true
}
```

#### Revert Legal Page to Default
- **Command:** `RevertLegalPageToDefaultCommand`
- **Endpoint:** `POST /api/legal-pages/{id}/revert`
- **Permission:** `legal-pages:update`
- **Purpose:** Delete tenant's customized version, restore platform default
- **Returns:** LegalPageDto (platform default, with `IsInherited: true`)
- **Validation:** Page must be a tenant override (not platform default)
- **Audit:** IAuditableCommand

### Queries

#### Get Legal Pages
- **Query:** `GetLegalPagesQuery`
- **Endpoint:** `GET /api/legal-pages`
- **Permission:** `legal-pages:read`
- **Purpose:** List all legal pages visible to current user
- **Returns:** List<LegalPageListDto>
- **Logic:**
  - Tenant users see: tenant overrides + platform defaults (not yet customized)
  - Platform admins see: all platform defaults
  - Each page has `IsInherited` flag

#### Get Legal Page
- **Query:** `GetLegalPageQuery`
- **Endpoint:** `GET /api/legal-pages/{id}`
- **Permission:** `legal-pages:read`
- **Purpose:** Get single page with full content for editing
- **Returns:** LegalPageDto

#### Get Public Legal Page
- **Query:** `GetPublicLegalPageQuery`
- **Endpoint:** `GET /api/public/legal/{slug}`
- **Permission:** None (public)
- **Purpose:** Resolve legal page for public display
- **Returns:** PublicLegalPageDto
- **Resolution:** Tenant override → Platform default
- **Use Case:** Frontend `/terms` and `/privacy` routes

**Example Response:**
```json
{
  "slug": "terms-of-service",
  "title": "Terms of Service",
  "htmlContent": "<h1>Terms of Service</h1><p>...</p>",
  "metaTitle": "Terms of Service | NOIR Platform",
  "metaDescription": "Read our terms and conditions.",
  "canonicalUrl": null,
  "allowIndexing": true,
  "lastModified": "2026-01-23T03:00:00Z"
}
```

### Frontend

- **Admin List:** `/portal/legal-pages` — Shows all pages with "Platform Default" or "Customized" badges
- **Admin Editor:** `/portal/legal-pages/{id}` — TinyMCE editor with SEO sidebar
- **Public Routes:** `/terms`, `/privacy` — React pages fetching from public API

---

## Media Management

**Namespace:** `NOIR.Application.Features.Media`
**Endpoint:** `/api/media`
**Permissions:** `media:*` (implicit for file ownership)

### Storage Providers

Configured via `StorageSettings` in `appsettings.json`:
- **Local** - File system storage (default)
- **Azure Blob Storage** - Azure Storage Account
- **AWS S3** - S3-compatible storage

### Commands

#### Upload File
- **Command:** `UploadFileCommand`
- **Endpoint:** `POST /api/media/upload`
- **Purpose:** Upload file to storage
- **Returns:** FileDto (with URL)
- **Validation:**
  - Max size: 10 MB (configurable)
  - Allowed formats: Configurable per use case
- **Storage Path:** `{tenantId}/{userId}/{filename}`
- **Image Processing:** Automatic resize for images (thumbnails)
- **Audit:** IAuditableCommand

#### Delete File
- **Command:** `DeleteFileCommand`
- **Endpoint:** `DELETE /api/media/{fileId}`
- **Purpose:** Delete file from storage
- **Returns:** Success message
- **Security:** Can only delete own files (unless admin)
- **Audit:** IAuditableCommand

### Queries

#### Get Files
- **Query:** `GetFilesQuery`
- **Endpoint:** `GET /api/media`
- **Purpose:** List user's uploaded files
- **Returns:** PaginatedList<FileDto>
- **Filters:** File type, date range

---

## Blog CMS

**Namespace:** `NOIR.Application.Features.Blog`
**Endpoint:** `/api/blog`
**Permissions:** `blog-*:*`

### Features

- **Posts** - Rich text content with featured images
- **Categories** - Hierarchical categorization
- **Tags** - Many-to-many tagging
- **Statuses** - Draft, Published, Archived
- **SEO** - Slug, meta title, meta description
- **Multi-Tenancy** - Tenant-scoped content

### Post Commands

#### Create Post
- **Command:** `CreatePostCommand`
- **Endpoint:** `POST /api/blog/posts`
- **Permission:** `blog-posts:create`
- **Purpose:** Create draft blog post
- **Returns:** PostDto
- **Validation:**
  - Title: Required, max 200 chars
  - Slug: Auto-generated from title, unique within tenant
  - Content: Required
  - Status: Draft by default
- **Audit:** IAuditableCommand

**Example Request:**
```json
{
  "title": "Getting Started with NOIR",
  "slug": "getting-started-with-noir",
  "content": "Rich HTML content...",
  "excerpt": "Brief summary...",
  "featuredImageUrl": "https://example.com/image.jpg",
  "metaTitle": "Getting Started | NOIR Blog",
  "metaDescription": "Learn how to get started with NOIR platform.",
  "categoryId": "cat-123",
  "tagIds": ["tag-1", "tag-2"],
  "status": "Draft"
}
```

#### Update Post
- **Command:** `UpdatePostCommand`
- **Endpoint:** `PUT /api/blog/posts/{id}`
- **Permission:** `blog-posts:update`
- **Purpose:** Update post content
- **Returns:** Updated PostDto
- **Validation:** Same as CreatePost
- **Audit:** IAuditableCommand

#### Delete Post
- **Command:** `DeletePostCommand`
- **Endpoint:** `DELETE /api/blog/posts/{id}`
- **Permission:** `blog-posts:delete`
- **Purpose:** Soft delete post
- **Returns:** Success message
- **Audit:** IAuditableCommand

#### Publish Post
- **Command:** `PublishPostCommand`
- **Endpoint:** `POST /api/blog/posts/{id}/publish`
- **Permission:** `blog-posts:publish`
- **Purpose:** Change status to Published
- **Returns:** Updated PostDto
- **Validation:** Post must be in Draft status
- **Audit:** IAuditableCommand

### Post Queries

#### Get Posts
- **Query:** `GetPostsQuery`
- **Endpoint:** `GET /api/blog/posts`
- **Permission:** `blog-posts:read`
- **Purpose:** List posts with filters
- **Returns:** PaginatedList<PostDto>
- **Filters:**
  - Status (Draft, Published, Archived)
  - Category
  - Tags
  - Search (title, content)
- **Sorting:** CreatedAt, UpdatedAt, Title

#### Get Post
- **Query:** `GetPostQuery`
- **Endpoint:** `GET /api/blog/posts/{id}` or `GET /api/blog/posts/slug/{slug}`
- **Permission:** `blog-posts:read`
- **Purpose:** Get single post by ID or slug
- **Returns:** PostDto
- **Cache:** FusionCache (5 minutes)

### Category Commands

#### Create Category
- **Command:** `CreateCategoryCommand`
- **Endpoint:** `POST /api/blog/categories`
- **Permission:** `blog-categories:create`
- **Purpose:** Create category
- **Returns:** CategoryDto
- **Validation:**
  - Name: Required, max 100 chars
  - Slug: Auto-generated, unique
- **Hierarchy:** Optional ParentId for nested categories
- **Audit:** IAuditableCommand

#### Update Category
- **Command:** `UpdateCategoryCommand`
- **Endpoint:** `PUT /api/blog/categories/{id}`
- **Permission:** `blog-categories:update`
- **Purpose:** Update category
- **Returns:** Updated CategoryDto
- **Audit:** IAuditableCommand

#### Delete Category
- **Command:** `DeleteCategoryCommand`
- **Endpoint:** `DELETE /api/blog/categories/{id}`
- **Permission:** `blog-categories:delete`
- **Purpose:** Delete category
- **Returns:** Success message
- **Validation:** Category must not have posts
- **Audit:** IAuditableCommand

### Category Queries

#### Get Categories
- **Query:** `GetCategoriesQuery`
- **Endpoint:** `GET /api/blog/categories`
- **Permission:** `blog-categories:read`
- **Purpose:** List categories (hierarchical)
- **Returns:** List<CategoryDto>
- **Cache:** FusionCache (10 minutes)

### Tag Commands & Queries

Similar structure to Categories:
- `CreateTagCommand`, `UpdateTagCommand`, `DeleteTagCommand`
- `GetTagsQuery`
- Permission: `blog-tags:*`

---

## Shipping

**Namespace:** `NOIR.Application.Features.Shipping`
**Endpoint:** `/api/shipping`, `/api/shipping-providers`
**Permissions:** `shipping:*`

> Vietnam-focused shipping provider integration with multi-carrier rate calculation, order tracking, and webhook support.

### Shipping Provider Management

#### Configure Shipping Provider
- **Command:** `ConfigureShippingProviderCommand`
- **Endpoint:** `POST /api/shipping-providers`
- **Permission:** `shipping:providers:write`
- **Purpose:** Configure a shipping provider for the tenant
- **Returns:** ShippingProviderDto
- **Validation:**
  - ProviderCode: Required, valid ShippingProviderCode
  - DisplayName: Required
  - Credentials: Required (encrypted storage)
- **Audit:** IAuditableCommand

#### Update Shipping Provider
- **Command:** `UpdateShippingProviderCommand`
- **Endpoint:** `PUT /api/shipping-providers/{id}`
- **Permission:** `shipping:providers:write`
- **Purpose:** Update provider configuration
- **Returns:** ShippingProviderDto
- **Audit:** IAuditableCommand

#### Get Shipping Providers
- **Query:** `GetShippingProvidersQuery`
- **Endpoint:** `GET /api/shipping-providers`
- **Permission:** `shipping:providers:read`
- **Purpose:** List all configured shipping providers
- **Returns:** PagedResult<ShippingProviderListDto>

#### Get Shipping Provider By Id
- **Query:** `GetShippingProviderByIdQuery`
- **Endpoint:** `GET /api/shipping-providers/{id}`
- **Permission:** `shipping:providers:read`
- **Purpose:** Get provider details
- **Returns:** ShippingProviderDto

#### Get Active Shipping Providers
- **Query:** `GetActiveShippingProvidersQuery`
- **Endpoint:** `GET /api/shipping-providers/active`
- **Permission:** `shipping:read`
- **Purpose:** Get active providers for checkout
- **Returns:** List<CheckoutShippingProviderDto>

### Shipping Orders

#### Create Shipping Order
- **Command:** `CreateShippingOrderCommand`
- **Endpoint:** `POST /api/shipping/orders`
- **Permission:** `shipping:orders:write`
- **Purpose:** Create a shipping order with a provider
- **Returns:** ShippingOrderDto
- **Validation:**
  - OrderId: Required
  - ProviderCode: Required, active provider
  - Addresses: Required (pickup + delivery)
  - Items: At least one item
- **Audit:** IAuditableCommand

**Example Request:**
```json
{
  "orderId": "order-123",
  "providerCode": "GHTK",
  "serviceTypeCode": "standard",
  "pickupAddress": {
    "fullName": "NOIR Store",
    "phone": "+84901234567",
    "addressLine1": "123 Nguyen Hue",
    "ward": "Ben Nghe",
    "district": "District 1",
    "province": "Ho Chi Minh City"
  },
  "deliveryAddress": {
    "fullName": "Customer Name",
    "phone": "+84987654321",
    "addressLine1": "456 Le Loi",
    "ward": "Ben Thanh",
    "district": "District 1",
    "province": "Ho Chi Minh City"
  },
  "items": [
    { "name": "T-Shirt", "quantity": 2, "weightGrams": 300, "value": 599000 }
  ],
  "totalWeightGrams": 600,
  "declaredValue": 1198000,
  "codAmount": 1198000
}
```

#### Cancel Shipping Order
- **Command:** `CancelShippingOrderCommand`
- **Endpoint:** `POST /api/shipping/orders/{id}/cancel`
- **Permission:** `shipping:orders:write`
- **Purpose:** Cancel a shipping order before pickup
- **Returns:** ShippingOrderDto
- **Validation:** Order must be in cancellable state (Draft, AwaitingPickup)
- **Audit:** IAuditableCommand

#### Get Shipping Order
- **Query:** `GetShippingOrderQuery`
- **Endpoint:** `GET /api/shipping/orders/{id}`
- **Permission:** `shipping:orders:read`
- **Purpose:** Get shipping order details
- **Returns:** ShippingOrderDto

#### Get Shipping Tracking
- **Query:** `GetShippingTrackingQuery`
- **Endpoint:** `GET /api/shipping/orders/{id}/tracking`
- **Permission:** `shipping:orders:read`
- **Purpose:** Get tracking events for a shipping order
- **Returns:** List<TrackingEventDto>

### Rate Calculation

#### Calculate Shipping Rates
- **Query:** `CalculateShippingRatesQuery`
- **Endpoint:** `POST /api/shipping/rates`
- **Permission:** `shipping:read`
- **Purpose:** Calculate rates across all active providers
- **Returns:** ShippingRatesResponse (rates + recommended rate)

**Example Request:**
```json
{
  "origin": { "province": "Ho Chi Minh City", "district": "District 1" },
  "destination": { "province": "Ha Noi", "district": "Hoan Kiem" },
  "weightGrams": 500,
  "declaredValue": 500000,
  "codAmount": 500000
}
```

**Example Response:**
```json
{
  "rates": [
    {
      "providerCode": "GHTK",
      "providerName": "Giao Hang Tiet Kiem",
      "serviceTypeCode": "standard",
      "serviceTypeName": "Standard Delivery",
      "baseRate": 25000,
      "codFee": 5000,
      "insuranceFee": 0,
      "totalRate": 30000,
      "estimatedDaysMin": 2,
      "estimatedDaysMax": 3,
      "currency": "VND"
    }
  ],
  "recommendedRate": { ... }
}
```

### Supported Providers (Vietnam Market)

| Code | Provider | Description |
|------|----------|-------------|
| `GHTK` | Giao Hang Tiet Kiem | Most popular, fastest delivery (40 hrs avg) |
| `GHN` | Giao Hang Nhanh | Second largest, excellent API |
| `JTExpress` | J&T Express | 100% on-time rate, fresh product support |
| `ViettelPost` | Viettel Post | State-owned, strong rural coverage |
| `NinjaVan` | Ninja Van | Tech-focused, returns management |
| `VNPost` | Vietnam Post | National postal service, widest coverage |
| `BestExpress` | Best Express | Budget-friendly option |
| `Custom` | Custom | For future extensions |

### Shipping Enums

#### ShippingStatus
| Value | Description |
|-------|-------------|
| `Draft` | Order created but not yet submitted |
| `AwaitingPickup` | Submitted to provider, awaiting pickup |
| `PickedUp` | Package picked up from sender |
| `InTransit` | Package in transit |
| `OutForDelivery` | Out for delivery |
| `Delivered` | Delivered successfully |
| `DeliveryFailed` | Delivery failed |
| `Cancelled` | Order cancelled |
| `Returning` | Package being returned |
| `Returned` | Package returned to sender |

#### ShippingProviderHealthStatus
| Value | Description |
|-------|-------------|
| `Unknown` | Not yet checked |
| `Healthy` | API responding normally |
| `Degraded` | Partial issues |
| `Unhealthy` | API unavailable |

### Domain Entities

- **ShippingProvider** - Provider configuration (credentials, settings, health)
- **ShippingOrder** - Shipping order tracking (status, tracking number, fees)
- **ShippingTrackingEvent** - Individual tracking events with timestamps
- **ShippingWebhookLog** - Webhook audit trail

---

## Inventory

**Namespace:** `NOIR.Application.Features.Inventory`
**Endpoint:** `/api/inventory`
**Permissions:** `inventory:*`

> Stock management tracked at ProductVariant level with movement history and reservation support.

### Queries

#### Get Stock History
- **Query:** `GetStockHistoryQuery`
- **Endpoint:** `GET /api/inventory/history`
- **Permission:** `inventory:read`
- **Purpose:** Get inventory movement history for a product variant
- **Returns:** PagedResult<InventoryMovementDto>
- **Query Parameters:**
  - `productVariantId`: Required
  - `movementType`: Optional filter
  - `dateFrom` / `dateTo`: Optional date range

**Example Response:**
```json
{
  "items": [
    {
      "id": "mov-123",
      "productVariantId": "var-456",
      "productId": "prod-789",
      "movementType": "StockIn",
      "quantityBefore": 50,
      "quantityMoved": 100,
      "quantityAfter": 150,
      "reference": "PO-2026-001",
      "notes": "Supplier delivery",
      "createdAt": "2026-01-28T10:00:00Z"
    }
  ]
}
```

### Commands

#### Create Stock Movement
- **Command:** `CreateStockMovementCommand`
- **Endpoint:** `POST /api/inventory/movements`
- **Permission:** `OrdersManage`
- **Purpose:** Create a manual stock movement (StockIn, StockOut, or Adjustment)
- **Returns:** `InventoryMovementDto`
- **Audit:** Implements `IAuditableCommand`

#### Create Inventory Receipt
- **Command:** `CreateInventoryReceiptCommand`
- **Endpoint:** `POST /api/inventory/receipts`
- **Permission:** `OrdersManage`
- **Purpose:** Create a batch stock movement receipt (draft) with items
- **Returns:** `InventoryReceiptDto`
- **Receipt Number:** Auto-generated as `RCV-YYYYMMDD-NNNN` (StockIn) or `SHP-YYYYMMDD-NNNN` (StockOut)

#### Confirm Inventory Receipt
- **Command:** `ConfirmInventoryReceiptCommand`
- **Endpoint:** `POST /api/inventory/receipts/{id}/confirm`
- **Permission:** `OrdersManage`
- **Purpose:** Confirm receipt and adjust stock for all items
- **Business Rules:**
  - Receipt must be in Draft status
  - Receipt must have at least one item
  - StockIn: Increases variant stock via `ReleaseStock()`
  - StockOut: Decreases variant stock via `ReserveStock()` (validates sufficient stock)
  - Each item logs an `InventoryMovement` record

#### Cancel Inventory Receipt
- **Command:** `CancelInventoryReceiptCommand`
- **Endpoint:** `POST /api/inventory/receipts/{id}/cancel`
- **Permission:** `OrdersManage`
- **Purpose:** Cancel a draft receipt (no stock adjustment)
- **Accepts:** Optional cancellation reason

### Queries

#### Get Stock History
- **Query:** `GetStockHistoryQuery`
- **Endpoint:** `GET /api/inventory/products/{productId}/variants/{variantId}/history`
- **Permission:** `OrdersRead`
- **Purpose:** Get paginated inventory movement history for a product variant
- **Returns:** `PagedResult<InventoryMovementDto>`

#### Get Inventory Receipts
- **Query:** `GetInventoryReceiptsQuery`
- **Endpoint:** `GET /api/inventory/receipts`
- **Permission:** `OrdersRead`
- **Purpose:** Get paginated receipts with optional type/status filter
- **Returns:** `PagedResult<InventoryReceiptSummaryDto>`

#### Get Inventory Receipt By ID
- **Query:** `GetInventoryReceiptByIdQuery`
- **Endpoint:** `GET /api/inventory/receipts/{id}`
- **Permission:** `OrdersRead`
- **Purpose:** Get full receipt details with items
- **Returns:** `InventoryReceiptDto`

### Inventory Receipt Workflow

```
  ┌─────────┐     Confirm()     ┌───────────┐
  │  Draft  │ ────────────────> │ Confirmed │
  └────┬────┘                   └───────────┘
       │                          (stock adjusted)
       │
       │ Cancel()
       ▼
  ┌───────────┐
  │ Cancelled │
  └───────────┘
    (no stock change)
```

**Receipt Types:**
- `StockIn` - Inbound receipt (phieu nhap kho), prefix `RCV`
- `StockOut` - Outbound receipt (phieu xuat kho), prefix `SHP`

### Inventory Movement Types

| Type | Description |
|------|-------------|
| `StockIn` | Stock received from supplier |
| `StockOut` | Stock shipped to customer |
| `Adjustment` | Manual stock adjustment (audit, correction) |
| `Return` | Customer return restocking |
| `Reserved` | Stock reserved for checkout session |
| `Released` | Reserved stock released (cancelled/expired) |

### Integration with Orders

Orders integrate with inventory through:
1. **Reservation on Checkout** - Inventory reserved when checkout initiated
2. **Deduction on Ship** - Actual inventory deducted when shipped
3. **Release on Cancel** - Reservation released if cancelled before ship
4. **Return Restock** - Inventory restored on customer returns

### Domain Entities

- **InventoryReceipt** (`TenantAggregateRoot<Guid>`) - Batch stock movement receipt with status workflow
- **InventoryReceiptItem** (`TenantEntity<Guid>`) - Line items with product snapshots (name, variant, SKU)
- **InventoryMovement** - Records each stock change with before/after quantities
- **ProductVariant.StockQuantity** - Current available stock
- **ProductVariant.ReservedQuantity** - Currently reserved stock

**Pattern Documentation:** See `docs/backend/patterns/inventory-receipt-pattern.md` for detailed architecture.

---

## Dashboard

**Namespace:** `NOIR.Application.Features.Dashboard`
**Endpoint:** `/api/dashboard`
**Permission:** `OrdersRead`

> Aggregated metrics for the admin dashboard with revenue, order counts, top products, and more.

### Queries

#### Get Dashboard Metrics
- **Query:** `GetDashboardMetricsQuery`
- **Endpoint:** `GET /api/dashboard/metrics`
- **Purpose:** Get aggregated dashboard metrics
- **Returns:** `DashboardMetricsDto`
- **Query Parameters:**
  - `topProducts` - Number of top selling products (default: 5)
  - `lowStockThreshold` - Stock quantity alert threshold (default: 10)
  - `recentOrders` - Number of recent orders to show (default: 10)
  - `salesDays` - Number of days for sales chart (default: 30)

### Metrics Included

| Metric | DTO | Description |
|--------|-----|-------------|
| Revenue | `RevenueMetricsDto` | Total, this month, last month, today, average order value |
| Order Counts | `OrderStatusCountsDto` | Count per status (all 9 statuses) |
| Top Products | `TopSellingProductDto[]` | By quantity sold with revenue |
| Low Stock | `LowStockProductDto[]` | Variants below threshold |
| Recent Orders | `RecentOrderDto[]` | Latest orders with status |
| Sales Over Time | `SalesOverTimeDto[]` | Daily revenue + order count for charts |
| Product Distribution | `ProductStatusDistributionDto` | Draft/Active/Archived counts |

### Architecture

- Handler delegates to `IDashboardQueryService` interface (Application layer)
- Implementation in `DashboardQueryService` (Infrastructure layer) uses direct `DbContext`
- 7 independent aggregation queries run in parallel via `Task.WhenAll()` for performance
- Revenue metrics exclude Cancelled/Refunded orders
- All queries use `TagWith()` for SQL debugging

---

## Customers

**Namespace:** `NOIR.Application.Features.Customers`
**Endpoint:** `/api/customers`
**Permissions:** `customers:*`

> Customer lifecycle management with address book, loyalty points, segmentation, and order history.

### Commands

#### Create Customer
- **Command:** `CreateCustomerCommand`
- **Endpoint:** `POST /api/customers`
- **Permission:** `customers:create`
- **Purpose:** Create a new customer profile (linked to a user account via optional UserId)
- **Returns:** `CustomerDto`
- **Validation:**
  - Email: Required, unique within tenant
  - FirstName / LastName: Required
- **Audit:** IAuditableCommand (Create)

**Example Request:**
```json
{
  "email": "john@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "phone": "+84901234567",
  "tags": "vip,wholesale",
  "notes": "Prefers email contact"
}
```

#### Update Customer
- **Command:** `UpdateCustomerCommand`
- **Endpoint:** `PUT /api/customers/{id}`
- **Permission:** `customers:update`
- **Purpose:** Update customer details, tags, notes, active status
- **Returns:** `CustomerDto`
- **Audit:** IAuditableCommand (Update)

#### Delete Customer
- **Command:** `DeleteCustomerCommand`
- **Endpoint:** `DELETE /api/customers/{id}`
- **Permission:** `customers:delete`
- **Purpose:** Soft delete customer
- **Returns:** Success message
- **Audit:** IAuditableCommand (Delete)

#### Update Customer Segment
- **Command:** `UpdateCustomerSegmentCommand`
- **Endpoint:** `PUT /api/customers/{id}/segment`
- **Permission:** `customers:update`
- **Purpose:** Manually override customer segment (New, Regular, VIP, Wholesale)
- **Returns:** `CustomerDto`
- **Audit:** IAuditableCommand (Update)

#### Add Loyalty Points
- **Command:** `AddLoyaltyPointsCommand`
- **Endpoint:** `POST /api/customers/{id}/loyalty/add`
- **Permission:** `customers:update`
- **Purpose:** Credit loyalty points to a customer with optional reason
- **Returns:** `CustomerDto`
- **Audit:** IAuditableCommand (Update)

#### Redeem Loyalty Points
- **Command:** `RedeemLoyaltyPointsCommand`
- **Endpoint:** `POST /api/customers/{id}/loyalty/redeem`
- **Permission:** `customers:update`
- **Purpose:** Deduct loyalty points (order redemption)
- **Returns:** `CustomerDto`
- **Audit:** IAuditableCommand (Update)

### Address Management

#### Add Customer Address
- **Command:** `AddCustomerAddressCommand`
- **Endpoint:** `POST /api/customers/{id}/addresses`
- **Permission:** `customers:update`
- **Purpose:** Add a new address to a customer's address book
- **Returns:** `CustomerAddressDto`
- **Validation:** FullName, Phone, AddressLine1, Province required

#### Update Customer Address
- **Command:** `UpdateCustomerAddressCommand`
- **Endpoint:** `PUT /api/customers/{id}/addresses/{addressId}`
- **Permission:** `customers:update`
- **Purpose:** Update an address and optionally mark as default
- **Returns:** `CustomerAddressDto`

#### Delete Customer Address
- **Command:** `DeleteCustomerAddressCommand`
- **Endpoint:** `DELETE /api/customers/{id}/addresses/{addressId}`
- **Permission:** `customers:update`
- **Purpose:** Remove an address from the address book
- **Returns:** Success message

### Queries

#### Get Customers
- **Query:** `GetCustomersQuery`
- **Endpoint:** `GET /api/customers`
- **Permission:** `customers:read`
- **Purpose:** List customers with search, filtering, and pagination
- **Returns:** `PagedResult<CustomerSummaryDto>`
- **Query Parameters:**
  - `search` - Search by email, name, phone
  - `segment` - Filter by CustomerSegment
  - `tier` - Filter by CustomerTier
  - `isActive` - Filter by active status

#### Get Customer By Id
- **Query:** `GetCustomerByIdQuery`
- **Endpoint:** `GET /api/customers/{id}`
- **Permission:** `customers:read`
- **Purpose:** Get full customer profile including addresses
- **Returns:** `CustomerDto`

#### Get Customer Orders
- **Query:** `GetCustomerOrdersQuery`
- **Endpoint:** `GET /api/customers/{id}/orders`
- **Permission:** `customers:read`
- **Purpose:** Get paginated order history for a customer
- **Returns:** `PagedResult<OrderListDto>`

#### Get Customer Stats
- **Query:** `GetCustomerStatsQuery`
- **Endpoint:** `GET /api/customers/stats`
- **Permission:** `customers:read`
- **Purpose:** Aggregated statistics for dashboard (totals, segment/tier distribution, top spenders)
- **Returns:** `CustomerStatsDto`

### Domain Entities

- **Customer** - Aggregate root (Email, FirstName, LastName, Phone, Segment, Tier, LoyaltyPoints, LifetimeLoyaltyPoints, TotalOrders, TotalSpent, AverageOrderValue, Tags, Notes, IsActive)
- **CustomerAddress** - Address value object (AddressType, FullName, Phone, AddressLine1, Ward, District, Province, PostalCode, IsDefault)

### Enums

| Enum | Values |
|------|--------|
| `CustomerSegment` | New, Regular, VIP, Wholesale |
| `CustomerTier` | Bronze, Silver, Gold, Platinum |
| `AddressType` | Shipping, Billing |

### Domain Events

- **CustomerCreatedEvent** - Customer profile created
- **CustomerUpdatedEvent** - Customer profile updated
- **CustomerDeactivatedEvent** - Customer deactivated
- **CustomerTierChangedEvent** - Customer tier upgraded/downgraded
- **CustomerSegmentChangedEvent** - Customer segment changed

---

## Customer Groups

**Namespace:** `NOIR.Application.Features.CustomerGroups`
**Endpoint:** `/api/customer-groups`
**Permissions:** `customers:*`

> Segment customers into named groups for targeted promotions, pricing rules, and analytics.

### Commands

#### Create Customer Group
- **Command:** `CreateCustomerGroupCommand`
- **Endpoint:** `POST /api/customer-groups`
- **Permission:** `customers:create`
- **Purpose:** Create a new customer group
- **Returns:** `CustomerGroupDto`
- **Validation:**
  - Name: Required, max 200 chars, unique within tenant
  - Slug: Auto-generated from name

#### Update Customer Group
- **Command:** `UpdateCustomerGroupCommand`
- **Endpoint:** `PUT /api/customer-groups/{id}`
- **Permission:** `customers:update`
- **Purpose:** Update group name, description, active status
- **Returns:** `CustomerGroupDto`

#### Delete Customer Group
- **Command:** `DeleteCustomerGroupCommand`
- **Endpoint:** `DELETE /api/customer-groups/{id}`
- **Permission:** `customers:delete`
- **Purpose:** Soft delete customer group
- **Returns:** Success message

#### Assign Customers to Group
- **Command:** `AssignCustomersToGroupCommand`
- **Endpoint:** `POST /api/customer-groups/{id}/members`
- **Permission:** `customers:update`
- **Purpose:** Add one or more customers to a group (bulk)
- **Returns:** Updated member count

#### Remove Customers from Group
- **Command:** `RemoveCustomersFromGroupCommand`
- **Endpoint:** `DELETE /api/customer-groups/{id}/members`
- **Permission:** `customers:update`
- **Purpose:** Remove customers from a group (bulk)
- **Returns:** Updated member count

### Queries

#### Get Customer Groups
- **Query:** `GetCustomerGroupsQuery`
- **Endpoint:** `GET /api/customer-groups`
- **Permission:** `customers:read`
- **Purpose:** List all customer groups with member counts
- **Returns:** `PagedResult<CustomerGroupListDto>`
- **Filters:** Search (by name, slug), IsActive

#### Get Customer Group By Id
- **Query:** `GetCustomerGroupByIdQuery`
- **Endpoint:** `GET /api/customer-groups/{id}`
- **Permission:** `customers:read`
- **Purpose:** Get group details
- **Returns:** `CustomerGroupDto`

### Domain Entity

- **CustomerGroup** - Aggregate root (Name, Description, Slug, IsActive, MemberCount)

---

## Promotions

**Namespace:** `NOIR.Application.Features.Promotions`
**Endpoint:** `/api/promotions`
**Permissions:** `promotions:*`

> Discount and promotion engine supporting percentage/fixed discounts, usage limits, date ranges, and product/category targeting.

### Commands

#### Create Promotion
- **Command:** `CreatePromotionCommand`
- **Endpoint:** `POST /api/promotions`
- **Permission:** `promotions:create`
- **Purpose:** Create a promotion with a discount code
- **Returns:** `PromotionDto`
- **Validation:**
  - Name: Required
  - Code: Required, unique within tenant
  - DiscountValue: Required, positive
  - StartDate must be before EndDate
- **Audit:** IAuditableCommand (Create)

**Example Request:**
```json
{
  "name": "Summer Sale 20%",
  "code": "SUMMER20",
  "description": "20% off all orders over 500,000 VND",
  "promotionType": "Discount",
  "discountType": "Percentage",
  "discountValue": 20.0,
  "maxDiscountAmount": 200000,
  "minOrderValue": 500000,
  "usageLimitTotal": 1000,
  "usageLimitPerUser": 1,
  "startDate": "2026-06-01T00:00:00Z",
  "endDate": "2026-08-31T23:59:59Z",
  "applyLevel": "Cart"
}
```

#### Update Promotion
- **Command:** `UpdatePromotionCommand`
- **Endpoint:** `PUT /api/promotions/{id}`
- **Permission:** `promotions:update`
- **Purpose:** Update promotion details (cannot change code)
- **Returns:** `PromotionDto`
- **Audit:** IAuditableCommand (Update)

#### Delete Promotion
- **Command:** `DeletePromotionCommand`
- **Endpoint:** `DELETE /api/promotions/{id}`
- **Permission:** `promotions:delete`
- **Purpose:** Soft delete a promotion
- **Returns:** Success message
- **Audit:** IAuditableCommand (Delete)

#### Activate Promotion
- **Command:** `ActivatePromotionCommand`
- **Endpoint:** `POST /api/promotions/{id}/activate`
- **Permission:** `promotions:update`
- **Purpose:** Set promotion to active status
- **Returns:** `PromotionDto`

#### Deactivate Promotion
- **Command:** `DeactivatePromotionCommand`
- **Endpoint:** `POST /api/promotions/{id}/deactivate`
- **Permission:** `promotions:update`
- **Purpose:** Set promotion to inactive status
- **Returns:** `PromotionDto`

#### Apply Promotion
- **Command:** `ApplyPromotionCommand`
- **Endpoint:** Internal (invoked during checkout)
- **Purpose:** Apply a promotion code to a cart/order and record usage
- **Returns:** Discount amount applied

### Queries

#### Get Promotions
- **Query:** `GetPromotionsQuery`
- **Endpoint:** `GET /api/promotions`
- **Permission:** `promotions:read`
- **Purpose:** List promotions with filtering
- **Returns:** `PagedResult<PromotionDto>`
- **Filters:** Search (name, code), Status, IsActive, date range

#### Get Promotion By Id
- **Query:** `GetPromotionByIdQuery`
- **Endpoint:** `GET /api/promotions/{id}`
- **Permission:** `promotions:read`
- **Purpose:** Get promotion details with usage stats and recent usages
- **Returns:** `PromotionDto`

#### Validate Promo Code
- **Query:** `ValidatePromoCodeQuery`
- **Endpoint:** `POST /api/promotions/validate`
- **Permission:** Authenticated users
- **Purpose:** Validate a promo code against the current cart (used at checkout)
- **Returns:** `PromoCodeValidationDto` (IsValid, DiscountAmount, message)

### Domain Entities

- **Promotion** - Aggregate root (Name, Code, PromotionType, DiscountType, DiscountValue, MaxDiscountAmount, MinOrderValue, UsageLimitTotal, UsageLimitPerUser, CurrentUsageCount, StartDate, EndDate, IsActive, Status, ApplyLevel, ProductIds, CategoryIds)
- **PromotionUsage** - Tracks per-user usage (PromotionId, UserId, OrderId, DiscountAmount, UsedAt)

### Enums

| Enum | Values |
|------|--------|
| `PromotionType` | Discount, FreeShipping, BuyXGetY |
| `DiscountType` | Percentage, FixedAmount |
| `PromotionApplyLevel` | Cart, Product, Category |
| `PromotionStatus` | Draft, Active, Inactive, Expired |

---

## Reviews

**Namespace:** `NOIR.Application.Features.Reviews`
**Endpoint:** `/api/reviews`
**Permissions:** `reviews:*`

> Product review system with star ratings, media attachments, moderation workflow, admin responses, and helpfulness voting.

### Commands

#### Create Review
- **Command:** `CreateReviewCommand`
- **Endpoint:** `POST /api/reviews`
- **Permission:** Authenticated users
- **Purpose:** Submit a product review with rating, title, content, and optional media
- **Returns:** `ReviewDto`
- **Validation:**
  - ProductId: Required, must exist
  - Rating: 1–5
  - Content: Required
  - IsVerifiedPurchase: Checked against order history

#### Approve Review
- **Command:** `ApproveReviewCommand`
- **Endpoint:** `POST /api/reviews/{id}/approve`
- **Permission:** `reviews:moderate`
- **Purpose:** Approve a pending review for public display
- **Returns:** `ReviewDto`
- **Audit:** IAuditableCommand (Update)

#### Reject Review
- **Command:** `RejectReviewCommand`
- **Endpoint:** `POST /api/reviews/{id}/reject`
- **Permission:** `reviews:moderate`
- **Purpose:** Reject a review (hidden from public)
- **Returns:** `ReviewDto`
- **Audit:** IAuditableCommand (Update)

#### Bulk Approve Reviews
- **Command:** `BulkApproveReviewsCommand`
- **Endpoint:** `POST /api/reviews/bulk-approve`
- **Permission:** `reviews:moderate`
- **Purpose:** Approve multiple pending reviews at once
- **Returns:** Count of approved reviews

#### Bulk Reject Reviews
- **Command:** `BulkRejectReviewsCommand`
- **Endpoint:** `POST /api/reviews/bulk-reject`
- **Permission:** `reviews:moderate`
- **Purpose:** Reject multiple pending reviews at once
- **Returns:** Count of rejected reviews

#### Add Admin Response
- **Command:** `AddAdminResponseCommand`
- **Endpoint:** `POST /api/reviews/{id}/response`
- **Permission:** `reviews:moderate`
- **Purpose:** Add or update the merchant's official response to a review
- **Returns:** `ReviewDto`

#### Vote Review
- **Command:** `VoteReviewCommand`
- **Endpoint:** `POST /api/reviews/{id}/vote`
- **Permission:** Authenticated users
- **Purpose:** Mark a review as helpful or not helpful
- **Returns:** Updated vote counts

### Queries

#### Get Reviews
- **Query:** `GetReviewsQuery`
- **Endpoint:** `GET /api/reviews`
- **Permission:** `reviews:read`
- **Purpose:** List all reviews with filtering (admin moderation queue)
- **Returns:** `PagedResult<ReviewDto>`
- **Filters:** Status (Pending, Approved, Rejected), ProductId, Rating, DateRange

#### Get Product Reviews
- **Query:** `GetProductReviewsQuery`
- **Endpoint:** `GET /api/reviews/products/{productId}`
- **Permission:** Public (approved reviews only) / `reviews:read` (all statuses)
- **Purpose:** Get paginated approved reviews for a specific product
- **Returns:** `PagedResult<ReviewDto>`

#### Get Review By Id
- **Query:** `GetReviewByIdQuery`
- **Endpoint:** `GET /api/reviews/{id}`
- **Permission:** `reviews:read`
- **Purpose:** Get full review details including media
- **Returns:** `ReviewDetailDto`

#### Get Review Stats
- **Query:** `GetReviewStatsQuery`
- **Endpoint:** `GET /api/reviews/products/{productId}/stats`
- **Permission:** Public
- **Purpose:** Get aggregated rating statistics for a product (average, distribution)
- **Returns:** `ReviewStatsDto`

### Domain Entities

- **ProductReview** - Aggregate root (ProductId, UserId, OrderId, Rating, Title, Content, Status, IsVerifiedPurchase, HelpfulVotes, NotHelpfulVotes, AdminResponse, AdminRespondedAt)
- **ReviewMedia** - Media attachment (MediaUrl, MediaType, DisplayOrder)

### Enums

| Enum | Values |
|------|--------|
| `ReviewStatus` | Pending, Approved, Rejected |
| `ReviewMediaType` | Image, Video |

---

## Wishlists

**Namespace:** `NOIR.Application.Features.Wishlists`
**Endpoint:** `/api/wishlists`
**Permissions:** Authenticated users

> Multi-wishlist management with sharing, item priorities, cart migration, and analytics tracking.

### Commands

#### Create Wishlist
- **Command:** `CreateWishlistCommand`
- **Endpoint:** `POST /api/wishlists`
- **Permission:** Authenticated users
- **Purpose:** Create a new named wishlist (users can have multiple)
- **Returns:** `WishlistDto`
- **Validation:** Name: Required, max 100 chars

#### Update Wishlist
- **Command:** `UpdateWishlistCommand`
- **Endpoint:** `PUT /api/wishlists/{id}`
- **Permission:** Owner
- **Purpose:** Update wishlist name and public/private setting
- **Returns:** `WishlistDto`

#### Delete Wishlist
- **Command:** `DeleteWishlistCommand`
- **Endpoint:** `DELETE /api/wishlists/{id}`
- **Permission:** Owner
- **Purpose:** Delete a wishlist
- **Returns:** Success message

#### Add to Wishlist
- **Command:** `AddToWishlistCommand`
- **Endpoint:** `POST /api/wishlists/{id}/items`
- **Permission:** Owner
- **Purpose:** Add a product (with optional variant) to a wishlist
- **Returns:** `WishlistDetailDto`
- **Validation:** ProductId required; ProductVariantId optional; Note optional

#### Remove From Wishlist
- **Command:** `RemoveFromWishlistCommand`
- **Endpoint:** `DELETE /api/wishlists/{id}/items/{itemId}`
- **Permission:** Owner
- **Purpose:** Remove an item from a wishlist
- **Returns:** Success message

#### Update Wishlist Item Priority
- **Command:** `UpdateWishlistItemPriorityCommand`
- **Endpoint:** `PUT /api/wishlists/{id}/items/{itemId}/priority`
- **Permission:** Owner
- **Purpose:** Set priority (Low, Normal, High, MustHave) for a wishlist item
- **Returns:** `WishlistDetailDto`

#### Move to Cart
- **Command:** `MoveToCartCommand`
- **Endpoint:** `POST /api/wishlists/{id}/items/{itemId}/move-to-cart`
- **Permission:** Owner
- **Purpose:** Transfer a wishlist item to the shopping cart
- **Returns:** `CartDto`

#### Share Wishlist
- **Command:** `ShareWishlistCommand`
- **Endpoint:** `POST /api/wishlists/{id}/share`
- **Permission:** Owner
- **Purpose:** Generate a public share URL for a wishlist
- **Returns:** Share URL

### Queries

#### Get Wishlists
- **Query:** `GetWishlistsQuery`
- **Endpoint:** `GET /api/wishlists`
- **Permission:** Authenticated users (own wishlists only)
- **Purpose:** List all wishlists owned by the current user
- **Returns:** `List<WishlistDto>`

#### Get Wishlist By Id
- **Query:** `GetWishlistByIdQuery`
- **Endpoint:** `GET /api/wishlists/{id}`
- **Permission:** Owner (or public if IsPublic = true)
- **Purpose:** Get wishlist with all items and product details
- **Returns:** `WishlistDetailDto`

#### Get Shared Wishlist
- **Query:** `GetSharedWishlistQuery`
- **Endpoint:** `GET /api/wishlists/shared/{shareToken}`
- **Permission:** Public (if wishlist is public)
- **Purpose:** View a shared wishlist via share token
- **Returns:** `WishlistDetailDto`

#### Get Wishlist Analytics
- **Query:** `GetWishlistAnalyticsQuery`
- **Endpoint:** `GET /api/wishlists/analytics`
- **Permission:** `reports:read`
- **Purpose:** Most wishlisted products, wishlist-to-cart conversion rates
- **Returns:** Analytics data

### Domain Entities

- **Wishlist** - Aggregate root (UserId, Name, IsDefault, IsPublic, ShareToken)
- **WishlistItem** - Product reference (ProductId, ProductVariantId, Note, Priority, IsInStock, AddedAt)

### Enums

| Enum | Values |
|------|--------|
| `WishlistItemPriority` | Low, Normal, High, MustHave |

---

## Reports

**Namespace:** `NOIR.Application.Features.Reports`
**Endpoint:** `/api/reports`
**Permissions:** `reports:*`

> Business analytics and reporting covering revenue, best sellers, inventory health, customer metrics, and CSV export.

### Queries

#### Get Revenue Report
- **Query:** `GetRevenueReportQuery`
- **Endpoint:** `GET /api/reports/revenue`
- **Permission:** `reports:read`
- **Purpose:** Revenue analytics for a period with daily breakdown, category breakdown, and payment method breakdown
- **Returns:** `RevenueReportDto`
- **Query Parameters:**
  - `period` - daily, weekly, monthly, yearly (default: monthly)
  - `startDate` / `endDate` - Custom date range (overrides period)
- **Includes:**
  - Total revenue, total orders, average order value
  - Revenue by day (`DailyRevenueDto[]`)
  - Revenue by category (`CategoryRevenueDto[]`)
  - Revenue by payment method (`PaymentMethodRevenueDto[]`)
  - Comparison to previous period (`RevenueComparisonDto`)

#### Get Best Sellers Report
- **Query:** `GetBestSellersReportQuery`
- **Endpoint:** `GET /api/reports/best-sellers`
- **Permission:** `reports:read`
- **Purpose:** Top-selling products ranked by units sold with revenue contribution
- **Returns:** `BestSellersReportDto`
- **Query Parameters:** `period`, `startDate`, `endDate`, `limit` (top N products)

#### Get Inventory Report
- **Query:** `GetInventoryReportQuery`
- **Endpoint:** `GET /api/reports/inventory`
- **Permission:** `reports:read`
- **Purpose:** Inventory health: low-stock alerts, stock valuation, dead-stock identification
- **Returns:** `InventoryReportDto`
- **Includes:**
  - Low-stock products (`LowStockDto[]`)
  - Total product and variant counts
  - Total stock value

#### Get Customer Report
- **Query:** `GetCustomerReportQuery`
- **Endpoint:** `GET /api/reports/customers`
- **Permission:** `reports:read`
- **Purpose:** Customer metrics: new vs returning, segment distribution, top spenders, churn rate
- **Returns:** Customer analytics data

#### Export Report
- **Command:** `ExportReportCommand`
- **Endpoint:** `POST /api/reports/export`
- **Permission:** `reports:export`
- **Purpose:** Export a report as CSV download
- **Returns:** File stream (CSV)
- **Supported Reports:** Revenue, BestSellers, Inventory, Customers

### Domain

- **Service:** `IReportQueryService` (Application interface) — implemented in Infrastructure
- **Architecture:** Delegates to service interface; no EF Core dependency in Application layer

---

## Feature Management

**Namespace:** `NOIR.Application.Features.FeatureManagement`
**Endpoint:** `/api/features`
**Permissions:** `system:admin` (platform operations), `settings:read` (tenant view)

> Two-layer module gating system: platform controls availability, tenants control enablement. 31 module definitions (8 core + 23 toggleable) with endpoint-level and command-level enforcement.

### Architecture

- **Module Registry:** Defined in `Application/Modules/` — 31 module definitions as `IModuleDefinition` implementations
- **Two-Layer Override:**
  - Platform admin: `IsAvailable` — controls whether a tenant CAN use the module
  - Tenant admin: `IsEnabled` — controls whether the tenant HAS turned it on
  - Computed: `IsEffective = IsAvailable && IsEnabled`
- **Enforcement:**
  - Endpoints: `.RequireFeature(ModuleNames.X.Y)` on endpoint groups
  - Commands: `[RequiresFeature]` attribute + `FeatureCheckMiddleware` (Wolverine middleware, cached via `ConcurrentDictionary`)
- **Caching:** FusionCache (5-minute TTL) + per-request dictionary cache via `IFeatureChecker`
- **Fail-closed:** Unknown features → disabled
- **Background Jobs:** `ITenantJobRunner` iterates active tenants, sets Finbuckle context, respects feature flags

### Module Categories

| Category | Core? | Modules |
|----------|-------|---------|
| Core Platform | Yes (8) | UserManagement, RoleManagement, AuditLog, Notifications, Media, EmailTemplates, LegalPages, DeveloperLogs |
| E-commerce | No (23) | Products, ProductAttributes, Brands, Cart, Checkout, Orders, Payments, Shipping, Inventory, Reviews, Wishlists, Customers, CustomerGroups, Promotions, Reports, Dashboard, Blog, Webhooks, FilterAnalytics, etc. |

### Commands

#### Set Module Availability (Platform Admin)
- **Command:** `SetModuleAvailabilityCommand`
- **Endpoint:** `PUT /api/features/tenants/{tenantId}/availability`
- **Permission:** `system:admin`
- **Purpose:** Platform admin enables or disables a module for a specific tenant
- **Returns:** `TenantFeatureStateDto`
- **Audit:** IAuditableCommand (Update)

#### Toggle Module (Tenant Admin)
- **Command:** `ToggleModuleCommand`
- **Endpoint:** `PUT /api/features/modules/{featureName}/toggle`
- **Permission:** `settings:update`
- **Purpose:** Tenant admin enables or disables a module (subject to platform availability)
- **Returns:** `TenantFeatureStateDto`
- **Validation:** Module must be available (IsAvailable = true) before enabling

### Queries

#### Get Module Catalog
- **Query:** `GetModuleCatalogQuery`
- **Endpoint:** `GET /api/features/catalog`
- **Permission:** `system:admin`
- **Purpose:** List all 31 module definitions with descriptions, icons, and default state
- **Returns:** `ModuleCatalogDto`

#### Get Tenant Feature States
- **Query:** `GetTenantFeatureStatesQuery`
- **Endpoint:** `GET /api/features/tenants/{tenantId}/states`
- **Permission:** `system:admin`
- **Purpose:** Get per-module IsAvailable/IsEnabled/IsEffective state for a tenant
- **Returns:** `List<TenantFeatureStateDto>`

#### Get Current Tenant Features
- **Query:** `GetCurrentTenantFeaturesQuery`
- **Endpoint:** `GET /api/features/current`
- **Permission:** Authenticated users
- **Purpose:** Get the current tenant's effective feature flags (used by frontend `useFeatures()`)
- **Returns:** `List<TenantFeatureStateDto>`

### Domain Entities

- **TenantModuleState** (`TenantEntity`) - Stores per-tenant overrides (FeatureName, IsAvailable, IsEnabled); extends `TenantEntity` (not `AggregateRoot`)

### Frontend Integration

- **Hook:** `useFeatures()` — fetches current tenant feature states, cached
- **Component:** `<FeatureGuard featureName="..." />` — conditionally renders children
- **Sidebar:** Filters navigation items based on effective module state
- **Admin UI:** Modules tab in Tenant Settings (toggle per module), Modules tab in Platform Admin

---

## Outbound Webhooks

**Namespace:** `NOIR.Application.Features.Webhooks`
**Endpoint:** `/api/webhooks`
**Permissions:** `webhooks:*`

> Tenant-configurable outbound webhook subscriptions with HMAC-SHA256 signature, event pattern filtering, delivery tracking, retry logic, and secret rotation.

### Architecture

- **Event Source:** `WebhookEventTypeRegistry` maps 40+ domain events to webhook-friendly string names (e.g. `order.created`, `payment.succeeded`)
- **Dispatcher:** `WebhookDispatcher` — Wolverine handler listening to `IDomainEvent`; queries active subscriptions matching event pattern; enqueues `DeliverWebhookCommand` per subscription
- **Delivery:** `DeliverWebhookCommand` — HTTP POST with HMAC-SHA256 signature header (`X-NOIR-Signature`), configurable timeout and retry count
- **Retry Logic:** Exponential backoff; `NextRetryAt` computed on failure; `AttemptNumber` tracked per log entry
- **Signature:** `HMAC-SHA256(secret, payload)` — consumers verify using stored secret

### Registered Event Categories

| Category | Events |
|----------|--------|
| Products | `product.created`, `product.published`, `product.archived`, `product.updated`, `product.stock_changed` |
| Product Categories | `product_category.created`, `product_category.updated`, `product_category.deleted` |
| Brands | `brand.created`, `brand.updated`, `brand.deleted` |
| Orders | `order.created`, `order.confirmed`, `order.shipped`, `order.delivered`, `order.completed`, `order.cancelled`, `order.refunded`, `order.returned` |
| Payments | `payment.created`, `payment.succeeded`, `payment.failed`, `payment.refund_completed`, `payment.cod_collected` |
| Cart | `cart.abandoned`, `cart.converted` |
| Checkout | `checkout.completed` |
| Customers | `customer.created`, `customer.updated`, `customer.deactivated`, `customer.tier_changed`, `customer.segment_changed` |
| Inventory | `inventory.receipt_confirmed` |
| Reviews | `review.created`, `review.approved`, `review.rejected` |

### Commands

#### Create Webhook Subscription
- **Command:** `CreateWebhookSubscriptionCommand`
- **Endpoint:** `POST /api/webhooks/subscriptions`
- **Permission:** `webhooks:create`
- **Purpose:** Register a new outbound webhook endpoint
- **Returns:** `WebhookSubscriptionDto`
- **Validation:**
  - Name: Required
  - Url: Required, valid HTTPS URL
  - EventPatterns: Required (comma-separated event type strings or wildcard `*`)
  - MaxRetries: 0–10 (default 5)
  - TimeoutSeconds: 5–120 (default 30)
- **Audit:** IAuditableCommand (Create)

**Example Request:**
```json
{
  "name": "ERP Order Sync",
  "url": "https://erp.example.com/webhooks/noir",
  "eventPatterns": "order.created,order.confirmed,order.shipped",
  "description": "Sync orders to ERP system",
  "maxRetries": 5,
  "timeoutSeconds": 30
}
```

#### Update Webhook Subscription
- **Command:** `UpdateWebhookSubscriptionCommand`
- **Endpoint:** `PUT /api/webhooks/subscriptions/{id}`
- **Permission:** `webhooks:update`
- **Purpose:** Update subscription URL, event patterns, or settings
- **Returns:** `WebhookSubscriptionDto`
- **Audit:** IAuditableCommand (Update)

#### Delete Webhook Subscription
- **Command:** `DeleteWebhookSubscriptionCommand`
- **Endpoint:** `DELETE /api/webhooks/subscriptions/{id}`
- **Permission:** `webhooks:delete`
- **Purpose:** Soft delete a webhook subscription
- **Returns:** Success message
- **Audit:** IAuditableCommand (Delete)

#### Activate Webhook Subscription
- **Command:** `ActivateWebhookSubscriptionCommand`
- **Endpoint:** `POST /api/webhooks/subscriptions/{id}/activate`
- **Permission:** `webhooks:update`
- **Purpose:** Enable a paused subscription
- **Returns:** `WebhookSubscriptionDto`

#### Deactivate Webhook Subscription
- **Command:** `DeactivateWebhookSubscriptionCommand`
- **Endpoint:** `POST /api/webhooks/subscriptions/{id}/deactivate`
- **Permission:** `webhooks:update`
- **Purpose:** Pause a subscription without deleting it
- **Returns:** `WebhookSubscriptionDto`

#### Rotate Webhook Secret
- **Command:** `RotateWebhookSecretCommand`
- **Endpoint:** `POST /api/webhooks/subscriptions/{id}/rotate-secret`
- **Permission:** `webhooks:update`
- **Purpose:** Generate a new HMAC secret (one-time display only)
- **Returns:** `WebhookSecretDto` (new secret — shown once)
- **Security:** Old secret is invalidated immediately

#### Test Webhook Subscription
- **Command:** `TestWebhookSubscriptionCommand`
- **Endpoint:** `POST /api/webhooks/subscriptions/{id}/test`
- **Permission:** `webhooks:update`
- **Purpose:** Send a test payload to verify the endpoint is reachable
- **Returns:** Delivery log entry

#### Deliver Webhook (Internal)
- **Command:** `DeliverWebhookCommand`
- **Purpose:** Internal Wolverine handler — performs the actual HTTP delivery and logs result
- **Triggered by:** `WebhookDispatcher` upon domain event

### Queries

#### Get Webhook Subscriptions
- **Query:** `GetWebhookSubscriptionsQuery`
- **Endpoint:** `GET /api/webhooks/subscriptions`
- **Permission:** `webhooks:read`
- **Purpose:** List all subscriptions with delivery statistics
- **Returns:** `PagedResult<WebhookSubscriptionSummaryDto>`

#### Get Webhook Subscription By Id
- **Query:** `GetWebhookSubscriptionByIdQuery`
- **Endpoint:** `GET /api/webhooks/subscriptions/{id}`
- **Permission:** `webhooks:read`
- **Purpose:** Get subscription details with aggregated delivery counts
- **Returns:** `WebhookSubscriptionDto`

#### Get Webhook Delivery Logs
- **Query:** `GetWebhookDeliveryLogsQuery`
- **Endpoint:** `GET /api/webhooks/subscriptions/{id}/logs`
- **Permission:** `webhooks:read`
- **Purpose:** Paginated delivery history for a subscription
- **Returns:** `PagedResult<WebhookDeliveryLogDto>`
- **Includes:** Request URL, HTTP status, attempt number, duration, error message, NextRetryAt

#### Get Webhook Event Types
- **Query:** `GetWebhookEventTypesQuery`
- **Endpoint:** `GET /api/webhooks/event-types`
- **Permission:** `webhooks:read`
- **Purpose:** List all available event types from the registry (grouped by category)
- **Returns:** `List<WebhookEventTypeDto>`

### Domain Entities

- **WebhookSubscription** - Aggregate root (Name, Url, EventPatterns, IsActive, MaxRetries, TimeoutSeconds, Status, LastDeliveryAt, Secret)
- **WebhookDeliveryLog** - Delivery attempt record (EventType, EventId, ResponseStatusCode, Status, AttemptNumber, NextRetryAt, DurationMs, ErrorMessage)

### Enums

| Enum | Values |
|------|--------|
| `WebhookSubscriptionStatus` | Active, Paused, Failed |
| `WebhookDeliveryStatus` | Pending, Delivered, Failed, Retrying |

---

## Tenant Settings

**Namespace:** `NOIR.Application.Features.TenantSettings`
**Endpoint:** `/api/tenant-settings`
**Permissions:** `settings:*`

> Per-tenant configuration for branding, contact info, regional preferences, and SMTP email. Accessed via the Tenant Settings portal page with tabbed navigation.

### Commands

#### Update Branding Settings
- **Command:** `UpdateBrandingSettingsCommand`
- **Endpoint:** `PUT /api/tenant-settings/branding`
- **Permission:** `settings:update`
- **Purpose:** Set tenant logo, favicon, primary/secondary brand colors, and dark mode default
- **Returns:** `BrandingSettingsDto`

#### Update Contact Settings
- **Command:** `UpdateContactSettingsCommand`
- **Endpoint:** `PUT /api/tenant-settings/contact`
- **Permission:** `settings:update`
- **Purpose:** Set tenant contact email, phone, and address
- **Returns:** `ContactSettingsDto`

#### Update Regional Settings
- **Command:** `UpdateRegionalSettingsCommand`
- **Endpoint:** `PUT /api/tenant-settings/regional`
- **Permission:** `settings:update`
- **Purpose:** Set timezone, language, and date format
- **Returns:** `RegionalSettingsDto`

#### Update Tenant SMTP Settings
- **Command:** `UpdateTenantSmtpSettingsCommand`
- **Endpoint:** `PUT /api/tenant-settings/smtp`
- **Permission:** `settings:update`
- **Purpose:** Configure tenant-specific outbound SMTP server (overrides platform defaults)
- **Returns:** `TenantSmtpSettingsDto`

#### Revert Tenant SMTP Settings
- **Command:** `RevertTenantSmtpSettingsCommand`
- **Endpoint:** `DELETE /api/tenant-settings/smtp`
- **Permission:** `settings:update`
- **Purpose:** Delete tenant SMTP override and fall back to platform SMTP settings
- **Returns:** Success message

#### Test Tenant SMTP Connection
- **Command:** `TestTenantSmtpConnectionCommand`
- **Endpoint:** `POST /api/tenant-settings/smtp/test`
- **Permission:** `settings:update`
- **Purpose:** Send a test email using the current SMTP configuration
- **Returns:** Success or error detail

### Queries

#### Get Branding Settings
- **Query:** `GetBrandingSettingsQuery`
- **Endpoint:** `GET /api/tenant-settings/branding`
- **Permission:** `settings:read`
- **Returns:** `BrandingSettingsDto` (LogoUrl, FaviconUrl, PrimaryColor, SecondaryColor, DarkModeDefault)

#### Get Contact Settings
- **Query:** `GetContactSettingsQuery`
- **Endpoint:** `GET /api/tenant-settings/contact`
- **Permission:** `settings:read`
- **Returns:** `ContactSettingsDto` (Email, Phone, Address)

#### Get Regional Settings
- **Query:** `GetRegionalSettingsQuery`
- **Endpoint:** `GET /api/tenant-settings/regional`
- **Permission:** `settings:read`
- **Returns:** `RegionalSettingsDto` (Timezone, Language, DateFormat)

#### Get Tenant SMTP Settings
- **Query:** `GetTenantSmtpSettingsQuery`
- **Endpoint:** `GET /api/tenant-settings/smtp`
- **Permission:** `settings:read`
- **Returns:** `TenantSmtpSettingsDto` (Host, Port, Username, IsInherited flag)

### Frontend Tabs

The Tenant Settings portal page (`/portal/settings`) is organized into tabs:

| Tab | Description |
|-----|-------------|
| **General** | Branding + contact settings |
| **Regional** | Timezone, language, date format |
| **Email** | SMTP configuration + test connection |
| **Payment Gateways** | Configure Stripe, MoMo, VNPay, COD, etc. (via Payments feature) |
| **Shipping Providers** | Configure GHTK, GHN, J&T, etc. (via Shipping feature) |
| **Email Templates** | Template customization (via Email Templates feature) |
| **Legal Pages** | Terms / Privacy edit (via Legal Pages feature) |
| **Modules** | Toggle enabled feature modules (via Feature Management) |

---

## Platform Settings

**Namespace:** `NOIR.Application.Features.PlatformSettings`
**Endpoint:** `/api/platform-settings`
**Permissions:** `system:admin`

> System-wide configuration for the NOIR platform, accessible only to platform administrators. Covers global SMTP settings.

### Commands

#### Update SMTP Settings
- **Command:** `UpdateSmtpSettingsCommand`
- **Endpoint:** `PUT /api/platform-settings/smtp`
- **Permission:** `system:admin`
- **Purpose:** Configure the platform-wide outbound SMTP server (used by all tenants unless overridden)
- **Returns:** `SmtpSettingsDto`
- **Settings:** Host, Port, Username, Password, EnableSsl, FromName, FromEmail

#### Test SMTP Connection
- **Command:** `TestSmtpConnectionCommand`
- **Endpoint:** `POST /api/platform-settings/smtp/test`
- **Permission:** `system:admin`
- **Purpose:** Send a test email using the current platform SMTP settings
- **Returns:** Success or error detail

### Queries

#### Get SMTP Settings
- **Query:** `GetSmtpSettingsQuery`
- **Endpoint:** `GET /api/platform-settings/smtp`
- **Permission:** `system:admin`
- **Purpose:** Retrieve current platform SMTP configuration (password masked)
- **Returns:** `SmtpSettingsDto`

### Multi-Tenancy

- Platform SMTP settings act as the default for all tenants
- Individual tenants can override via **Tenant Settings → Email** (see `TenantSmtpSettings`)
- When a tenant has no override, emails are sent via the platform SMTP server

---

## Developer Tools

**Namespace:** `NOIR.Application.Features.DeveloperLogs`
**Endpoint:** `/api/developer-logs`
**Permissions:** `system:admin`

### Real-Time Log Streaming

- **SignalR Hub:** `DeveloperLogHub` at `/hubs/developer-logs`
- **Architecture:** Serilog → `DeferredSignalRLogSink` → SignalR → Frontend
- **Dynamic Level:** Change log level at runtime without restart

### Query

#### Stream Logs
- **Query:** `StreamLogsQuery`
- **Endpoint:** WebSocket connection via SignalR
- **Permission:** `system:admin`
- **Purpose:** Real-time log streaming
- **Filters:**
  - Log level (Debug, Info, Warning, Error, Fatal)
  - Source context
  - Message pattern
- **Returns:** Continuous stream of log events

**Log Event Format:**
```json
{
  "timestamp": "2026-01-22T10:30:00.123Z",
  "level": "Information",
  "messageTemplate": "User {UserId} logged in from {IpAddress}",
  "message": "User user-123 logged in from 192.168.1.1",
  "properties": {
    "UserId": "user-123",
    "IpAddress": "192.168.1.1",
    "SourceContext": "NOIR.Web.Endpoints.AuthEndpoints"
  },
  "exception": null
}
```

---

## Payment Processing

**Namespace:** `NOIR.Application.Features.Payments`
**Endpoint:** `/api/payments`
**Permissions:** `payments:*`

> ⭐ **NEW FEATURE:** Complete payment gateway integration supporting multiple providers, transaction tracking, refund management, and webhook processing.

### Real-Time Architecture

- **SignalR Hub:** `PaymentHub` at `/hubs/payments`
- **Client Interface:** `IPaymentClient` (strongly-typed)
- **Abstraction:** `IPaymentHubContext` (injectable service)
- **Connection:** Auto-join user/tenant groups on connect

**Notification Types:**
| Method | Event | Target Groups |
|--------|-------|---------------|
| `PaymentStatusChanged` | Payment status transition | `payment_{id}`, `order_{orderId}` |
| `CodCollected` | COD cash collected | `cod_updates_{tenantId}`, `payment_{id}` |
| `RefundStatusChanged` | Refund approved/rejected | `payment_{id}` |
| `WebhookProcessed` | Webhook callback handled | `webhooks_{tenantId}` |

**Client Groups:**
- `user_{userId}` - Personal payment updates
- `tenant_{tenantId}` - Tenant-wide payment events
- `payment_{transactionId}` - Specific transaction tracking
- `order_{orderId}` - Order payment tracking
- `cod_updates_{tenantId}` - COD collection monitoring
- `webhooks_{tenantId}` - Webhook processing monitoring

---

### Gateway Management

#### Configure Gateway
- **Command:** `ConfigureGatewayCommand`
- **Endpoint:** `POST /api/payments/gateways`
- **Permission:** `payments:gateways:write`
- **Purpose:** Configure a payment gateway for the tenant
- **Returns:** PaymentGatewayDto
- **Validation:**
  - Provider: Required, valid provider type
  - DisplayName: Required, max 100 chars
  - Credentials: Required, encrypted storage
- **Audit:** IAuditableCommand (Handler + Entity levels)

**Example Request:**
```json
{
  "provider": "Stripe",
  "displayName": "Stripe Credit Card",
  "environment": "Sandbox",
  "credentials": {
    "apiKey": "sk_test_xxx",
    "webhookSecret": "whsec_xxx"
  },
  "supportedCurrencies": ["USD", "EUR", "VND"],
  "minAmount": 1.00,
  "maxAmount": 10000.00
}
```

#### Update Gateway
- **Command:** `UpdateGatewayCommand`
- **Endpoint:** `PUT /api/payments/gateways/{id}`
- **Permission:** `payments:gateways:write`
- **Purpose:** Update gateway configuration
- **Returns:** Updated PaymentGatewayDto
- **Audit:** IAuditableCommand

#### Get Gateway
- **Query:** `GetPaymentGatewayQuery`
- **Endpoint:** `GET /api/payments/gateways/{id}`
- **Permission:** `payments:gateways:read`
- **Purpose:** Get specific gateway configuration
- **Returns:** PaymentGatewayDto (credentials masked)

#### Get Gateways
- **Query:** `GetPaymentGatewaysQuery`
- **Endpoint:** `GET /api/payments/gateways`
- **Permission:** `payments:gateways:read`
- **Purpose:** List all configured gateways
- **Returns:** PagedResult<PaymentGatewayDto>

#### Get Active Gateways
- **Query:** `GetActiveGatewaysQuery`
- **Endpoint:** `GET /api/payments/gateways/active`
- **Permission:** `payments:read`
- **Purpose:** Get active gateways for checkout
- **Returns:** List<PaymentGatewayDto> (public info only)

---

### Payment Transactions

#### Create Payment
- **Command:** `CreatePaymentCommand`
- **Endpoint:** `POST /api/payments/transactions`
- **Permission:** `payments:write`
- **Purpose:** Initiate a payment transaction
- **Returns:** PaymentTransactionDto with redirect URL (if applicable)
- **Validation:**
  - OrderId: Required
  - Amount: Required, positive
  - Currency: Required, valid ISO code
  - PaymentGatewayId: Required, active gateway
  - PaymentMethod: Required
- **Audit:** IAuditableCommand

**Example Request:**
```json
{
  "orderId": "order-123",
  "amount": 99.99,
  "currency": "USD",
  "paymentGatewayId": "gateway-456",
  "paymentMethod": "CreditCard",
  "returnUrl": "https://example.com/checkout/complete",
  "metadata": {
    "customerEmail": "customer@example.com",
    "orderReference": "ORD-2026-001"
  }
}
```

**Example Response:**
```json
{
  "id": "txn-789",
  "transactionNumber": "TXN-2026-000123",
  "status": "Pending",
  "amount": 99.99,
  "currency": "USD",
  "paymentMethod": "CreditCard",
  "redirectUrl": "https://gateway.example.com/pay/xyz",
  "expiresAt": "2026-01-25T12:30:00Z"
}
```

#### Cancel Payment
- **Command:** `CancelPaymentCommand`
- **Endpoint:** `POST /api/payments/transactions/{id}/cancel`
- **Permission:** `payments:write`
- **Purpose:** Cancel a pending payment
- **Returns:** Updated PaymentTransactionDto
- **Validation:** Payment must be in cancellable state (Pending, RequiresAction)
- **Audit:** IAuditableCommand

#### Get Transaction
- **Query:** `GetPaymentTransactionQuery`
- **Endpoint:** `GET /api/payments/transactions/{id}`
- **Permission:** `payments:read`
- **Purpose:** Get specific transaction details
- **Returns:** PaymentTransactionDto

#### Get Transactions
- **Query:** `GetPaymentTransactionsQuery`
- **Endpoint:** `GET /api/payments/transactions`
- **Permission:** `payments:read`
- **Purpose:** List transactions with filtering
- **Query Params:**
  - `status` - Filter by PaymentStatus
  - `paymentMethod` - Filter by PaymentMethod
  - `dateFrom` / `dateTo` - Date range
  - `orderId` - Filter by order
- **Returns:** PagedResult<PaymentTransactionDto>

#### Get Order Payments
- **Query:** `GetOrderPaymentsQuery`
- **Endpoint:** `GET /api/payments/orders/{orderId}/payments`
- **Permission:** `payments:read`
- **Purpose:** Get all payments for a specific order
- **Returns:** List<PaymentTransactionDto>

---

### Cash-on-Delivery (COD)

#### Get Pending COD Payments
- **Query:** `GetPendingCodPaymentsQuery`
- **Endpoint:** `GET /api/payments/cod/pending`
- **Permission:** `payments:cod:read`
- **Purpose:** List pending COD payments awaiting collection
- **Returns:** PagedResult<PaymentTransactionDto>

#### Confirm COD Collection
- **Command:** `ConfirmCodCollectionCommand`
- **Endpoint:** `POST /api/payments/transactions/{id}/cod-collected`
- **Permission:** `payments:cod:write`
- **Purpose:** Mark COD payment as collected by courier
- **Returns:** Updated PaymentTransactionDto
- **Validation:** Payment must be COD method with CodPending status
- **Audit:** IAuditableCommand

**Example Request:**
```json
{
  "collectorName": "John Courier",
  "collectedAmount": 99.99
}
```

---

### Refunds

#### Request Refund
- **Command:** `RequestRefundCommand`
- **Endpoint:** `POST /api/payments/transactions/{id}/refund`
- **Permission:** `payments:refunds:write`
- **Purpose:** Request a refund for a paid transaction
- **Returns:** RefundDto
- **Validation:**
  - Amount: Required, positive, <= original amount
  - Reason: Required
- **Audit:** IAuditableCommand

**Example Request:**
```json
{
  "amount": 49.99,
  "reason": "CustomerRequest",
  "reasonDetail": "Customer changed their mind about the product"
}
```

#### Approve Refund
- **Command:** `ApproveRefundCommand`
- **Endpoint:** `POST /api/payments/refunds/{id}/approve`
- **Permission:** `payments:refunds:approve`
- **Purpose:** Approve a pending refund request
- **Returns:** Updated RefundDto
- **Audit:** IAuditableCommand

#### Reject Refund
- **Command:** `RejectRefundCommand`
- **Endpoint:** `POST /api/payments/refunds/{id}/reject`
- **Permission:** `payments:refunds:approve`
- **Purpose:** Reject a pending refund request
- **Returns:** Updated RefundDto
- **Audit:** IAuditableCommand

**Example Request:**
```json
{
  "rejectionReason": "Outside return window"
}
```

#### Get Refunds
- **Query:** `GetRefundsQuery`
- **Endpoint:** `GET /api/payments/refunds`
- **Permission:** `payments:refunds:read`
- **Purpose:** List refunds with filtering
- **Query Params:**
  - `status` - Filter by RefundStatus
  - `transactionId` - Filter by transaction
- **Returns:** PagedResult<RefundDto>

---

### Webhooks

#### Process Webhook
- **Command:** `ProcessWebhookCommand`
- **Endpoint:** `POST /api/payments/webhooks/{provider}`
- **Permission:** Public endpoint (signature verified)
- **Purpose:** Handle payment provider webhook callbacks
- **Returns:** HTTP 200 OK on success
- **Security:**
  - Signature verification per provider
  - Idempotency via GatewayEventId
  - Automatic retry handling
- **Audit:** Webhook log created

#### Get Webhook Logs
- **Query:** `GetWebhookLogsQuery`
- **Endpoint:** `GET /api/payments/webhooks/logs`
- **Permission:** `payments:webhooks:read`
- **Purpose:** View webhook processing history
- **Query Params:**
  - `provider` - Filter by provider
  - `status` - Filter by ProcessingStatus
  - `paymentTransactionId` - Filter by transaction
- **Returns:** PagedResult<WebhookLogDto>

---

### Payment Enums

#### PaymentStatus
| Value | Description |
|-------|-------------|
| `Pending` | Payment created, awaiting user action |
| `Processing` | Payment being processed by gateway |
| `RequiresAction` | Additional action needed (3DS, OTP) |
| `Authorized` | Payment authorized, not yet captured |
| `Paid` | Payment successfully completed |
| `Failed` | Payment failed |
| `Cancelled` | Payment cancelled by user/system |
| `Expired` | Payment expired before completion |
| `Refunded` | Fully refunded |
| `PartialRefund` | Partially refunded |
| `CodPending` | COD payment awaiting collection |
| `CodCollected` | COD payment collected |

#### PaymentMethod
| Value | Description |
|-------|-------------|
| `CreditCard` | Credit card payment |
| `DebitCard` | Debit card payment |
| `EWallet` | Digital wallet (MoMo, ZaloPay, etc.) |
| `QRCode` | QR code payment |
| `BankTransfer` | Bank transfer |
| `Installment` | Installment payment |
| `COD` | Cash on delivery |
| `BuyNowPayLater` | BNPL services |

#### RefundStatus
| Value | Description |
|-------|-------------|
| `Pending` | Refund requested, awaiting approval |
| `Approved` | Refund approved, awaiting processing |
| `Processing` | Refund being processed by gateway |
| `Completed` | Refund completed |
| `Rejected` | Refund rejected |
| `Failed` | Refund processing failed |

---

## Feature Matrix

### Feature Availability by Role

| Feature | PlatformAdmin | TenantAdmin | User |
|---------|---------------|-------------|------|
| **Users** | ✅ All tenants | ✅ Own tenant | ❌ |
| **Roles** | ✅ All tenants | ✅ Own tenant | ❌ |
| **Permissions** | ✅ All tenants | ✅ Own tenant | ❌ |
| **Tenants** | ✅ CRUD | ❌ | ❌ |
| **Product Attributes** | ✅ All tenants | ✅ Own tenant | ❌ |
| **Brands** | ✅ All tenants | ✅ Own tenant | ❌ |
| **Payments** | ✅ All tenants | ✅ Own tenant | ✅ Own orders |
| **Checkout** | ✅ All tenants | ✅ Own tenant | ✅ Own sessions |
| **Orders** | ✅ All tenants | ✅ Own tenant | ✅ Own orders |
| **Audit Logs** | ✅ All tenants | ✅ Own tenant | ❌ |
| **Notifications** | ❌ (system-level) | ✅ | ✅ |
| **Email Templates** | ✅ Platform defaults | ✅ Tenant overrides | ❌ |
| **Legal Pages** | ✅ Platform defaults | ✅ Tenant overrides | ❌ |
| **Media** | ✅ | ✅ | ✅ Own files |
| **Blog** | ✅ All tenants | ✅ Own tenant | ❌ |
| **Shipping** | ✅ All tenants | ✅ Own tenant | ✅ Own orders |
| **Inventory** | ✅ All tenants | ✅ Own tenant | ❌ |
| **Dashboard** | ✅ All tenants | ✅ Own tenant | ❌ |
| **Customers** | ✅ All tenants | ✅ Own tenant | ❌ |
| **Customer Groups** | ✅ All tenants | ✅ Own tenant | ❌ |
| **Promotions** | ✅ All tenants | ✅ Own tenant | ❌ |
| **Reviews** | ✅ All tenants | ✅ Own tenant | ✅ Own reviews |
| **Wishlists** | ✅ All tenants | ✅ Own tenant | ✅ Own wishlists |
| **Reports** | ✅ All tenants | ✅ Own tenant | ❌ |
| **Feature Management** | ✅ Platform + tenant control | ✅ Tenant toggle | ❌ |
| **Outbound Webhooks** | ✅ All tenants | ✅ Own tenant | ❌ |
| **Tenant Settings** | ✅ All tenants | ✅ Own tenant | ❌ |
| **Platform Settings** | ✅ | ❌ | ❌ |
| **Developer Logs** | ✅ | ❌ | ❌ |
| **Hangfire Dashboard** | ✅ | ❌ | ❌ |

### API Endpoints Summary

| Endpoint Group | Count | Description |
|----------------|-------|-------------|
| `/api/auth` | 10 | Authentication, profile, password reset |
| `/api/users` | 6 | User CRUD, roles |
| `/api/roles` | 4 | Role CRUD |
| `/api/permissions` | 4 | Permission assignment |
| `/api/tenants` | 6 | Tenant management |
| `/api/payments` | 15 | Transactions, gateways, refunds, webhooks, COD |
| `/api/checkout` | 6 | ⭐ **NEW:** initiate, address, shipping, payment, complete |
| `/api/orders` | 9 | ⭐ **NEW:** create, confirm, ship, deliver, complete, return, cancel, list, details |
| `/api/audit` | 3 | Audit logs, entity history, export |
| `/api/notifications` | 4 | Notification CRUD |
| `/api/email-templates` | 4 | Template customization |
| `/api/legal-pages` | 4 | Legal page CRUD (COW) |
| `/api/public/legal` | 1 | Public legal page access |
| `/api/media` | 3 | File upload, delete, list |
| `/api/blog/posts` | 6 | Blog post CRUD |
| `/api/blog/categories` | 4 | Category CRUD |
| `/api/product-attributes` | 7 | ⭐ **NEW:** Attribute CRUD + value management |
| `/api/brands` | 5 | ⭐ **NEW:** Brand CRUD |
| `/api/shipping` | 4 | ⭐ **NEW:** Shipping orders, rates, tracking |
| `/api/shipping-providers` | 4 | ⭐ **NEW:** Provider configuration |
| `/api/inventory` | 7 | ⭐ **NEW:** Stock movements, receipts CRUD, confirm, cancel |
| `/api/blog/tags` | 4 | Tag CRUD |
| `/api/dashboard` | 1 | Aggregated metrics |
| `/api/customers` | 12 | Customer CRUD, addresses, loyalty points, order history, stats |
| `/api/customer-groups` | 7 | Group CRUD, member assignment/removal |
| `/api/promotions` | 8 | Promotion CRUD, activate/deactivate, validate promo code |
| `/api/reviews` | 10 | Review CRUD, approve/reject, bulk moderation, admin response, voting |
| `/api/wishlists` | 10 | Wishlist CRUD, item management, sharing, analytics, move-to-cart |
| `/api/reports` | 5 | Revenue, best sellers, inventory, customers, export |
| `/api/features` | 5 | Module catalog, tenant states, toggle, set availability |
| `/api/webhooks/subscriptions` | 8 | Subscription CRUD, activate/deactivate, rotate secret, test, delivery logs |
| `/api/webhooks/event-types` | 1 | Registered event type catalog |
| `/api/tenant-settings` | 10 | Branding, contact, regional, SMTP settings |
| `/api/platform-settings` | 3 | Platform SMTP settings |
| **Total** | **200** | |

### Commands vs Queries

| Feature | Commands | Queries | Total |
|---------|----------|---------|-------|
| Auth | 7 | 1 | 8 |
| Users | 4 | 2 | 6 |
| Roles | 3 | 2 | 5 |
| Permissions | 2 | 2 | 4 |
| Tenants | 4 | 4 | 8 |
| **Payments** | **9** | **9** | **18** |
| **Checkout** | **5** | **1** | **6** |
| **Orders** | **7** | **2** | **9** |
| Audit | 1 | 2 | 3 |
| Notifications | 3 | 2 | 5 |
| Email Templates | 1 | 3 | 4 |
| Legal Pages | 2 | 3 | 5 |
| Media | 2 | 1 | 3 |
| Blog Posts | 4 | 2 | 6 |
| Blog Categories | 3 | 1 | 4 |
| Blog Tags | 3 | 1 | 4 |
| **Product Attributes** | **10** | **7** | **17** |
| **Brands** | **3** | **2** | **5** |
| **Shipping** | **3** | **5** | **8** |
| **Inventory** | **4** | **3** | **7** |
| **Dashboard** | **0** | **1** | **1** |
| **Customers** | **8** | **4** | **12** |
| **Customer Groups** | **5** | **2** | **7** |
| **Promotions** | **6** | **3** | **9** |
| **Reviews** | **7** | **4** | **11** |
| **Wishlists** | **8** | **4** | **12** |
| **Reports** | **1** | **4** | **5** |
| **Feature Management** | **2** | **3** | **5** |
| **Outbound Webhooks** | **7** | **4** | **11** |
| **Tenant Settings** | **5** | **4** | **9** |
| **Platform Settings** | **2** | **1** | **3** |
| Developer Logs | 0 | 1 | 1 |
| **Total** | **131** | **92** | **223** |

---

## HR Module

**Location:** `Features/Hr/`
**Module:** `Erp.Hr` (toggleable)
**Permissions:** 4 employee + 4 department = 8 total

### Employee Management

| Type | Name | Description |
|------|------|-------------|
| **Command** | `CreateEmployeeCommand` | Create employee (auto-code EMP-) |
| **Command** | `UpdateEmployeeCommand` | Update employee details |
| **Command** | `DeactivateEmployeeCommand` | Deactivate with cascade (reassign subordinates) |
| **Command** | `ReactivateEmployeeCommand` | Reactivate employee |
| **Command** | `BulkAssignTagsCommand` | Bulk assign tags to employees |
| **Command** | `BulkChangeDepartmentCommand` | Bulk change department |
| **Command** | `ImportEmployeesCommand` | Import from CSV (row-level errors) |
| **Query** | `GetEmployeeByIdQuery` | Employee detail with tags |
| **Query** | `GetEmployeesQuery` | Paged list with filters |
| **Query** | `ExportEmployeesQuery` | Export to Excel/CSV |
| **Query** | `GetOrgChartQuery` | Hierarchical org chart data |
| **Query** | `GetHrReportsQuery` | HR analytics (headcount, department, status) |

### Department Management

| Type | Name | Description |
|------|------|-------------|
| **Command** | `CreateDepartmentCommand` | Create department (tree structure) |
| **Command** | `UpdateDepartmentCommand` | Update department |
| **Command** | `DeleteDepartmentCommand` | Delete department |
| **Query** | `GetDepartmentsQuery` | Hierarchical department list |
| **Query** | `GetDepartmentByIdQuery` | Department detail |

### Employee Tags

| Type | Name | Description |
|------|------|-------------|
| **Command** | `CreateEmployeeTagCommand` | Create tag (7 categories) |
| **Command** | `UpdateEmployeeTagCommand` | Update tag |
| **Command** | `DeleteEmployeeTagCommand` | Delete tag |
| **Query** | `GetEmployeeTagsQuery` | List tags |

**Key Patterns:**
- SequenceCounter-based code generation (EMP-001)
- Recursive CTE for org chart hierarchy
- Self-referencing FK with `DeleteBehavior.Restrict`
- d3-org-chart on frontend

---

## CRM Module

**Location:** `Features/Crm/`
**Module:** `Erp.Crm` (toggleable)
**Permissions:** 17 total (4 per entity + CrmLeadsManage + CrmPipelineManage)

### CRM Contacts

| Type | Name | Description |
|------|------|-------------|
| **Command** | `CreateCrmContactCommand` | Create contact |
| **Command** | `UpdateCrmContactCommand` | Update contact |
| **Command** | `DeleteCrmContactCommand` | Delete (blocked if active leads) |
| **Query** | `GetCrmContactsQuery` | Paged contact list |
| **Query** | `GetCrmContactByIdQuery` | Contact detail |

### CRM Companies

| Type | Name | Description |
|------|------|-------------|
| **Command** | `CreateCrmCompanyCommand` | Create company |
| **Command** | `UpdateCrmCompanyCommand` | Update company |
| **Command** | `DeleteCrmCompanyCommand` | Delete (blocked if has contacts) |
| **Query** | `GetCrmCompaniesQuery` | Paged company list |
| **Query** | `GetCrmCompanyByIdQuery` | Company detail |

### CRM Leads & Pipeline

| Type | Name | Description |
|------|------|-------------|
| **Command** | `CreateLeadCommand` | Create lead in pipeline |
| **Command** | `UpdateLeadCommand` | Update lead |
| **Command** | `DeleteLeadCommand` | Delete lead |
| **Command** | `MoveLeadCommand` | Move to stage (Kanban drag, float SortOrder) |
| **Command** | `WinLeadCommand` | Win lead → auto-create Customer |
| **Command** | `LoseLeadCommand` | Mark lead as lost |
| **Command** | `ReopenLeadCommand` | Reopen closed lead |
| **Query** | `GetLeadsQuery` | Paged lead list |
| **Query** | `GetLeadByIdQuery` | Lead detail |
| **Query** | `GetCrmDashboardQuery` | CRM dashboard metrics |

### CRM Pipelines

| Type | Name | Description |
|------|------|-------------|
| **Command** | `CreatePipelineCommand` | Create pipeline |
| **Command** | `UpdatePipelineCommand` | Update pipeline & stages |
| **Command** | `DeletePipelineCommand` | Delete (blocked if default or active leads) |
| **Query** | `GetPipelinesQuery` | List pipelines |
| **Query** | `GetPipelineByIdQuery` | Pipeline with stages |

### CRM Activities

| Type | Name | Description |
|------|------|-------------|
| **Command** | `CreateCrmActivityCommand` | Create activity (call, email, meeting, note) |
| **Command** | `UpdateCrmActivityCommand` | Update activity |
| **Command** | `DeleteCrmActivityCommand` | Delete activity |
| **Query** | `GetCrmActivitiesQuery` | Paged activity list |

**Key Patterns:**
- @dnd-kit Kanban board with float-based SortOrder
- Won/Lost as virtual columns
- Lead → Customer auto-conversion on Win
- Delete guards (contact blocked if active leads)

---

## Project Management Module

**Location:** `Features/Pm/`
**Module:** `Erp.Pm` (toggleable)

### Projects

| Type | Name | Description |
|------|------|-------------|
| **Command** | `CreateProjectCommand` | Create project (auto-code PRJ-) |
| **Command** | `UpdateProjectCommand` | Update project |
| **Command** | `DeleteProjectCommand` | Delete project |
| **Query** | `GetProjectsQuery` | Paged project list |
| **Query** | `GetProjectByIdQuery` | Project with columns & tasks |

### Tasks

| Type | Name | Description |
|------|------|-------------|
| **Command** | `CreateProjectTaskCommand` | Create task |
| **Command** | `UpdateProjectTaskCommand` | Update task |
| **Command** | `DeleteProjectTaskCommand` | Delete task |
| **Command** | `MoveProjectTaskCommand` | Move task (Kanban drag) |
| **Command** | `CompleteProjectTaskCommand` | Complete task |
| **Query** | `GetProjectTasksQuery` | Paged task list |
| **Query** | `GetProjectTaskByIdQuery` | Task detail with subtasks |

**Key Patterns:**
- SequenceCounter-based code generation (PRJ-001, TSK-001)
- Kanban board with @dnd-kit
- Subtask hierarchy

---

## Real-Time & Communication

### SignalR Entity Update Signals

**145 command handlers** publish `EntityUpdateSignal` after mutations via `IEntityUpdateHubContext`.

| Component | Location | Purpose |
|-----------|----------|---------|
| `EntityUpdateSignal` | `Application/Common/Models/` | Signal record (EntityType, EntityId, Operation) |
| `IEntityUpdateHubContext` | `Application/Common/Interfaces/` | Publishing interface |
| `NotificationHub` | `Infrastructure/Hubs/` | Hub with JoinEntity/LeaveEntity methods |
| `useEntityUpdateSignal` | `frontend/src/hooks/` | Frontend hook for real-time subscriptions |

**Group naming:** `entity_list_{EntityType}_{tenantId}` | `entity_{EntityType}_{id}_{tenantId}`

### Server-Sent Events (SSE)

| Component | Location | Purpose |
|-----------|----------|---------|
| `ISseEventPublisher` | `Application/Common/Interfaces/` | Event publishing |
| `SseEventPublisher` | `Infrastructure/Sse/` | Channel-based implementation |
| `useSse` / `useJobProgress` | `frontend/src/hooks/` | Frontend consumers |

**Docs:** [SignalR Pattern](backend/patterns/signalr-real-time.md) | [SSE Pattern](backend/patterns/sse-background-jobs.md)

---

## Global Search

| Type | Name | Description |
|------|------|-------------|
| **Query** | `GlobalSearchQuery` | Search across all entities |

**Frontend:** Enhanced Command Palette (Cmd+K) with content search results.

---

## See Also

- [PROJECT_INDEX.md](PROJECT_INDEX.md) - Project structure and navigation
- [KNOWLEDGE_BASE.md](KNOWLEDGE_BASE.md) - Deep-dive codebase reference
- [API_INDEX.md](API_INDEX.md) - REST API documentation
- [Backend Patterns](backend/patterns/) - Implementation patterns
- [Frontend Architecture](frontend/architecture.md) - Frontend structure

---

**Last Updated:** 2026-03-08
**Version:** 4.0
**Maintainer:** NOIR Team

---

## Changelog

### Version 4.0 (2026-03-08)
- Added **HR Module** feature section (12+ commands/queries — employees, departments, tags, org chart, import/export, reports)
- Added **CRM Module** feature section (20+ commands/queries — contacts, companies, leads, pipelines, activities, Kanban)
- Added **PM Module** feature section (12+ commands/queries — projects, tasks, Kanban, subtasks)
- Added **Real-Time & Communication** section (SignalR entity signals, SSE, job progress)
- Added **Global Search** section
- Updated totals: 280+ endpoints across 52 groups
- Updated to Version 4.0

### Version 3.0 (2026-02-27)
- Added **Customers** feature section (8 commands, 4 queries — CRUD, addresses, loyalty points, order history, stats)
- Added **Customer Groups** feature section (5 commands, 2 queries — CRUD, bulk member management)
- Added **Promotions** feature section (6 commands, 3 queries — discount codes, percentage/fixed, usage limits)
- Added **Reviews** feature section (7 commands, 4 queries — star ratings, moderation, bulk approve/reject, voting)
- Added **Wishlists** feature section (8 commands, 4 queries — multi-wishlist, sharing, priorities, cart migration, analytics)
- Added **Reports** feature section (1 command, 4 queries — revenue, best sellers, inventory, customers, CSV export)
- Added **Feature Management** feature section (2 commands, 3 queries — 31 modules, two-layer override, endpoint/command gating)
- Added **Outbound Webhooks** feature section (7 commands, 4 queries — 40+ event types, HMAC-SHA256, retry logic)
- Added **Tenant Settings** feature section (5 commands, 4 queries — branding, contact, regional, SMTP)
- Added **Platform Settings** feature section (2 commands, 1 query — platform SMTP)
- Updated Feature Matrix with all new features
- Updated API Endpoints Summary: 121 → 200 total endpoints
- Updated Commands vs Queries: 137 → 223 total

### Version 2.6 (2026-02-18)
- Added **Dashboard** feature section (1 query, 7 metric categories, parallel aggregation)
- Added Dashboard to Feature Matrix and endpoint/query counts
- Updated totals: 121 endpoints, 137 commands/queries

### Version 2.5 (2026-02-18)
- Expanded **Inventory** feature with receipt system (4 commands, 3 queries, 7 endpoints)
- Documented inventory receipt workflow: Draft -> Confirmed/Cancelled
- Updated **Orders** with full lifecycle (7 commands: create, confirm, ship, deliver, complete, return, cancel)
- Updated endpoint counts (111 -> 120) and command/query totals (127 -> 136)
- Added pattern reference: `docs/backend/patterns/inventory-receipt-pattern.md`

### Version 2.4 (2026-02-18)
- Added **Product Attributes** feature section (10 commands, 7 queries, 13 attribute types)
- Added **Brands** feature section (3 commands, 2 queries)
- Added **Shipping** feature section (3 commands, 5 queries, 8 Vietnam providers)
- Added **Inventory** feature section (1 query, 6 movement types)
- Updated Feature Matrix with new feature permissions
- Updated API Endpoints Summary (now 111 total endpoints)
- Updated Commands vs Queries (now 127 total)
- Added attribute types reference table
- Added shipping provider reference table with Vietnam market providers
- Added inventory movement types reference table

### Version 2.3 (2026-01-26)
- Added **Checkout** feature section with 5 commands, 1 query (hybrid accordion pattern)
- Added **Orders** feature section with 4 commands, 2 queries (full lifecycle management)
- Updated Feature Matrix with Checkout and Orders permissions
- Updated API Endpoints Summary (now 90 total endpoints)
- Updated Commands vs Queries (now 96 total)

### Version 2.2 (2026-01-25)
- Added **Payment Processing** feature section with 9 commands, 9 queries
- Added Payment gateway management documentation
- Added Payment transaction lifecycle documentation
- Added Refund workflow documentation
- Added COD (Cash-on-Delivery) documentation
- Added Webhook processing documentation
- Updated Feature Matrix with Payments role access
- Updated API Endpoints Summary (now 78 total endpoints)
- Updated Commands vs Queries (now 84 total)
