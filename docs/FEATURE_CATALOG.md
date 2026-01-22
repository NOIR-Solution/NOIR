# NOIR - Feature Catalog

> **Complete reference of all features, commands, queries, and endpoints in the NOIR platform.**

**Last Updated:** 2026-01-22

---

## Table of Contents

- [Overview](#overview)
- [Authentication & Identity](#authentication--identity)
- [User Management](#user-management)
- [Role & Permission Management](#role--permission-management)
- [Multi-Tenancy](#multi-tenancy)
- [Audit Logging](#audit-logging)
- [Notifications](#notifications)
- [Email Templates](#email-templates)
- [Media Management](#media-management)
- [Blog CMS](#blog-cms)
- [Developer Tools](#developer-tools)
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

## Feature Matrix

### Feature Availability by Role

| Feature | PlatformAdmin | TenantAdmin | User |
|---------|---------------|-------------|------|
| **Users** | ✅ All tenants | ✅ Own tenant | ❌ |
| **Roles** | ✅ All tenants | ✅ Own tenant | ❌ |
| **Permissions** | ✅ All tenants | ✅ Own tenant | ❌ |
| **Tenants** | ✅ CRUD | ❌ | ❌ |
| **Audit Logs** | ✅ All tenants | ✅ Own tenant | ❌ |
| **Notifications** | ❌ (system-level) | ✅ | ✅ |
| **Email Templates** | ✅ Platform defaults | ✅ Tenant overrides | ❌ |
| **Media** | ✅ | ✅ | ✅ Own files |
| **Blog** | ✅ All tenants | ✅ Own tenant | ❌ |
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
| `/api/audit` | 3 | Audit logs, entity history, export |
| `/api/notifications` | 4 | Notification CRUD |
| `/api/email-templates` | 4 | Template customization |
| `/api/media` | 3 | File upload, delete, list |
| `/api/blog/posts` | 6 | Blog post CRUD |
| `/api/blog/categories` | 4 | Category CRUD |
| `/api/blog/tags` | 4 | Tag CRUD |
| **Total** | **58** | |

### Commands vs Queries

| Feature | Commands | Queries | Total |
|---------|----------|---------|-------|
| Auth | 7 | 1 | 8 |
| Users | 4 | 2 | 6 |
| Roles | 3 | 2 | 5 |
| Permissions | 2 | 2 | 4 |
| Tenants | 4 | 4 | 8 |
| Audit | 1 | 2 | 3 |
| Notifications | 3 | 2 | 5 |
| Email Templates | 1 | 3 | 4 |
| Media | 2 | 1 | 3 |
| Blog Posts | 4 | 2 | 6 |
| Blog Categories | 3 | 1 | 4 |
| Blog Tags | 3 | 1 | 4 |
| Developer Logs | 0 | 1 | 1 |
| **Total** | **37** | **24** | **61** |

---

## See Also

- [PROJECT_INDEX.md](PROJECT_INDEX.md) - Project structure and navigation
- [KNOWLEDGE_BASE.md](KNOWLEDGE_BASE.md) - Deep-dive codebase reference
- [API_INDEX.md](API_INDEX.md) - REST API documentation
- [Backend Patterns](backend/patterns/) - Implementation patterns
- [Frontend Architecture](frontend/architecture.md) - Frontend structure

---

**Last Updated:** 2026-01-22
**Maintainer:** NOIR Team
