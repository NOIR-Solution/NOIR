# NOIR API Documentation

**Last Updated:** 2026-02-08
**Version:** 2.0
**Base URL (Dev):** `http://localhost:4000/api`

A comprehensive guide to all REST API endpoints in the NOIR platform.

---

## Quick Reference

| Category | Endpoints | Auth Required | Key Permissions |
|----------|-----------|---------------|-----------------|
| [Authentication](#authentication-api) | 5 | Mixed | Public + Authenticated |
| [Users](#users-api) | 8 | ✅ | `users:*` |
| [Roles](#roles-api) | 8 | ✅ | `roles:*` |
| [Permissions](#permissions-api) | 2 | ✅ | `permissions:read` |
| [Tenants](#tenants-api) | 5 | ✅ | `tenants:*` |
| [Products](#products-api) | 14+ | ✅ | `products:*` |
| [Product Categories](#product-categories-api) | 10 | ✅ | `products:*` |
| [Product Attributes](#product-attributes-api) | 8 | ✅ | `products:*` |
| [Product Filters](#product-filters-api) | 2 | Mixed | Public filtering |
| [Brands](#brands-api) | 5 | ✅ | `brands:*` |
| [Cart](#cart-api) | 7 | Mixed | User-scoped / SessionId |
| [Checkout](#checkout-api) | 7 | ✅ | User-scoped |
| [Orders](#orders-api) | 6 | ✅ | `orders:*` |
| [Payments](#payments-api) | 9+ | ✅ | `payments:*` |
| [Payment Gateways](#payment-gateways-api) | 7 | ✅ | Admin |
| [Shipping](#shipping-api) | 5 | ✅ | `shipping:*` |
| [Shipping Providers](#shipping-providers-api) | 5 | ✅ | Admin |
| [Notifications](#notifications-api) | 5 | ✅ | User-scoped |
| [Email Templates](#email-templates-api) | 4 | ✅ | `email-templates:*` |
| [Audit](#audit-api) | 3 | ✅ | `audit:view` |
| [Blog](#blog-api) | 12 | Mixed | `blog:write` (admin) |
| [Feeds](#feeds-api) | 3 | ❌ | Public |
| [Media](#media-api) | 2 | ✅ | `media:*` |
| [Legal Pages](#legal-pages-api) | 4 | Mixed | Admin + Public |
| [Platform Settings](#platform-settings-api) | 3 | ✅ | Platform Admin |
| [Tenant Settings](#tenant-settings-api) | 8 | ✅ | Tenant Admin |
| [Filter Analytics](#filter-analytics-api) | 2 | Mixed | Admin (read) / Public (write) |
| [Developer Logs](#developer-logs-api) | 1 | ✅ | Dev only |

**API Documentation UI:** `http://localhost:4000/api/docs` (Scalar)

---

## Authentication & Authorization

### Authentication Flow

1. **Login** → Returns JWT access token + refresh token (HTTP-only cookie)
2. **Access API** → Include JWT in `Authorization: Bearer {token}` header
3. **Token Expires** → Call `/auth/refresh` to get new access token
4. **Logout** → Invalidates refresh token

### Multi-Tenancy

NOIR supports multi-tenant access:

- **Regular Users:** Scoped to their tenant via JWT `tenant_id` claim
- **Platform Admins:** No tenant scope (`TenantId = null`), cross-tenant access

**Tenant Resolution Order:**
1. `X-Tenant` HTTP header
2. `tenant_id` claim in JWT
3. No fallback (null for platform admins)

### Permission Format

Permissions follow the pattern: `{resource}:{action}:{scope}`

**Examples:**
- `users:read:all` - View all users
- `users:create:own` - Create users in own tenant
- `audit:view:all` - View all audit logs

---

## Authentication API

**Base Path:** `/api/auth`

### POST /auth/login

Authenticate user and receive tokens.

**Request:**
```json
{
  "email": "admin@noir.local",
  "password": "123qwe",
  "rememberMe": true,
  "deviceFingerprint": "optional-device-id"
}
```

**Response (200 OK):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-01-20T12:00:00Z",
  "user": {
    "id": "guid",
    "email": "admin@noir.local",
    "displayName": "Platform Admin",
    "tenantId": null,
    "roles": ["PlatformAdmin"],
    "permissions": ["*:*:*"]
  }
}
```

**Errors:**
- `401 Unauthorized` - Invalid credentials
- `403 Forbidden` - Account locked
- `400 Bad Request` - Validation error

**Notes:**
- Refresh token set as HTTP-only cookie
- `rememberMe: true` → 30-day refresh token, `false` → 7-day

---

### POST /auth/refresh

Refresh access token using refresh token cookie.

**Request:** *(No body, uses HTTP-only cookie)*

**Response (200 OK):**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-01-20T12:00:00Z"
}
```

**Errors:**
- `401 Unauthorized` - Invalid/expired refresh token
- `403 Forbidden` - Token family mismatch (potential theft)

**Token Rotation:**
- Each refresh invalidates the old token and issues a new one
- Protects against token theft via rotation detection

---

### POST /auth/logout

Invalidate refresh token and clear cookies.

**Request:** *(No body)*

**Response (204 No Content)**

**Notes:**
- Clears refresh token cookie
- Invalidates token in database

---

### GET /auth/me

Get current authenticated user details.

**Response (200 OK):**
```json
{
  "id": "guid",
  "email": "admin@noir.local",
  "displayName": "Platform Admin",
  "tenantId": null,
  "roles": ["PlatformAdmin"],
  "permissions": ["*:*:*"],
  "avatarUrl": "https://..."
}
```

**Errors:**
- `401 Unauthorized` - Not authenticated

---

### PUT /auth/profile

Update current user's profile.

**Request:**
```json
{
  "displayName": "New Name",
  "email": "newemail@example.com",
  "currentPassword": "required-for-email-change",
  "avatarUrl": "https://..."
}
```

**Response (200 OK):**
```json
{
  "id": "guid",
  "email": "newemail@example.com",
  "displayName": "New Name",
  "avatarUrl": "https://..."
}
```

**Errors:**
- `400 Bad Request` - Validation error
- `409 Conflict` - Email already in use

---

## Users API

**Base Path:** `/api/users`
**Required Permission:** `users:read`, `users:write`

### GET /users

List users with filtering and pagination.

**Query Parameters:**
- `search` - Search by name/email
- `role` - Filter by role name
- `isActive` - Filter by active status
- `pageNumber` - Page number (default: 1)
- `pageSize` - Items per page (default: 10, max: 100)

**Response (200 OK):**
```json
{
  "items": [
    {
      "id": "guid",
      "email": "user@example.com",
      "displayName": "John Doe",
      "isActive": true,
      "roles": ["User"],
      "createdAt": "2026-01-20T10:00:00Z"
    }
  ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 50,
  "totalPages": 5
}
```

---

### POST /users

Create a new user.

**Request:**
```json
{
  "email": "newuser@example.com",
  "displayName": "Jane Smith",
  "password": "SecurePass123!",
  "roleNames": ["User"],
  "sendWelcomeEmail": true
}
```

**Response (201 Created):**
```json
{
  "id": "guid",
  "email": "newuser@example.com",
  "displayName": "Jane Smith",
  "isActive": true,
  "roles": ["User"],
  "createdAt": "2026-01-20T10:00:00Z"
}
```

**Errors:**
- `409 Conflict` - Email already exists
- `400 Bad Request` - Validation error

---

### GET /users/{id}

Get user details by ID.

**Response (200 OK):**
```json
{
  "id": "guid",
  "email": "user@example.com",
  "displayName": "John Doe",
  "isActive": true,
  "roles": ["User", "Editor"],
  "tenantId": "tenant-guid",
  "createdAt": "2026-01-20T10:00:00Z",
  "modifiedAt": "2026-01-20T11:00:00Z"
}
```

**Errors:**
- `404 Not Found` - User doesn't exist

---

### PUT /users/{id}

Update user details.

**Request:**
```json
{
  "displayName": "Updated Name",
  "email": "updated@example.com",
  "isActive": true
}
```

**Response (200 OK):**
```json
{
  "id": "guid",
  "email": "updated@example.com",
  "displayName": "Updated Name",
  "isActive": true,
  "modifiedAt": "2026-01-20T12:00:00Z"
}
```

---

### DELETE /users/{id}

Soft delete a user.

**Response (204 No Content)**

**Notes:**
- Soft delete only (sets `IsDeleted = true`)
- Hard delete requires explicit GDPR request

---

### POST /users/{id}/lock

Lock user account.

**Request:**
```json
{
  "reason": "Security violation"
}
```

**Response (200 OK):**
```json
{
  "id": "guid",
  "isLocked": true,
  "lockedAt": "2026-01-20T12:00:00Z",
  "lockedBy": "admin-user-id"
}
```

---

### POST /users/{id}/unlock

Unlock user account.

**Response (200 OK):**
```json
{
  "id": "guid",
  "isLocked": false
}
```

---

### POST /users/{id}/roles

Assign roles to user.

**Request:**
```json
{
  "roleNames": ["User", "Editor"]
}
```

**Response (200 OK):**
```json
{
  "id": "guid",
  "roles": ["User", "Editor"]
}
```

---

## Roles API

**Base Path:** `/api/roles`
**Required Permission:** `roles:read`, `roles:write`

### GET /roles

List all roles.

**Response (200 OK):**
```json
{
  "items": [
    {
      "id": "guid",
      "name": "Admin",
      "description": "Administrator role",
      "color": "#3B82F6",
      "iconName": "shield",
      "parentRoleId": null,
      "userCount": 5
    }
  ]
}
```

---

### POST /roles

Create a new role.

**Request:**
```json
{
  "name": "Editor",
  "description": "Content editor",
  "color": "#10B981",
  "iconName": "edit",
  "parentRoleId": null
}
```

**Response (201 Created):**
```json
{
  "id": "guid",
  "name": "Editor",
  "description": "Content editor",
  "color": "#10B981",
  "iconName": "edit",
  "createdAt": "2026-01-20T10:00:00Z"
}
```

---

### GET /roles/{id}

Get role details.

**Response (200 OK):**
```json
{
  "id": "guid",
  "name": "Admin",
  "description": "Administrator role",
  "permissions": ["users:read:all", "users:write:all"],
  "userCount": 5
}
```

---

### PUT /roles/{id}

Update role.

**Request:**
```json
{
  "name": "Senior Editor",
  "description": "Updated description",
  "color": "#6366F1"
}
```

**Response (200 OK):**
```json
{
  "id": "guid",
  "name": "Senior Editor",
  "modifiedAt": "2026-01-20T12:00:00Z"
}
```

---

### DELETE /roles/{id}

Delete role (if no users assigned).

**Response (204 No Content)**

**Errors:**
- `409 Conflict` - Role has assigned users

---

### GET /roles/{id}/permissions

Get role permissions.

**Response (200 OK):**
```json
{
  "direct": ["users:read:all", "blog:write:own"],
  "inherited": ["users:write:all"]
}
```

---

### GET /roles/{id}/effective-permissions

Get all effective permissions (direct + inherited).

**Response (200 OK):**
```json
{
  "permissions": ["users:read:all", "users:write:all", "blog:write:own"]
}
```

---

### PUT /roles/{id}/permissions

Assign permissions to role.

**Request:**
```json
{
  "permissionIds": ["perm-guid-1", "perm-guid-2"]
}
```

**Response (200 OK):**
```json
{
  "permissions": ["users:read:all", "blog:write:own"]
}
```

---

### DELETE /roles/{id}/permissions/{permissionId}

Remove permission from role.

**Response (204 No Content)**

---

## Permissions API

**Base Path:** `/api/permissions`
**Required Permission:** `permissions:read`

### GET /permissions

List all available permissions.

**Response (200 OK):**
```json
{
  "items": [
    {
      "id": "guid",
      "name": "users:read:all",
      "resource": "users",
      "action": "read",
      "scope": "all",
      "description": "View all users"
    }
  ]
}
```

---

### GET /permissions/templates

Get permission templates (predefined sets).

**Response (200 OK):**
```json
{
  "items": [
    {
      "id": "guid",
      "name": "UserManagement",
      "permissions": ["users:read:all", "users:write:all", "roles:read:all"]
    }
  ]
}
```

---

## Tenants API

**Base Path:** `/api/tenants`
**Required Permission:** `tenants:read`, `tenants:write`

### GET /tenants

List tenants.

**Response (200 OK):**
```json
{
  "items": [
    {
      "id": "guid",
      "identifier": "acme-corp",
      "name": "Acme Corporation",
      "isActive": true,
      "createdAt": "2026-01-20T10:00:00Z"
    }
  ]
}
```

---

### POST /tenants

Create tenant.

**Request:**
```json
{
  "identifier": "new-tenant",
  "name": "New Tenant Inc.",
  "isActive": true
}
```

**Response (201 Created):**
```json
{
  "id": "guid",
  "identifier": "new-tenant",
  "name": "New Tenant Inc.",
  "isActive": true,
  "createdAt": "2026-01-20T10:00:00Z"
}
```

---

### GET /tenants/{id}

Get tenant details.

**Response (200 OK):**
```json
{
  "id": "guid",
  "identifier": "acme-corp",
  "name": "Acme Corporation",
  "settings": {
    "theme": "dark",
    "features": ["blog", "notifications"]
  }
}
```

---

### PUT /tenants/{id}

Update tenant.

**Request:**
```json
{
  "name": "Updated Name",
  "isActive": false
}
```

**Response (200 OK)**

---

### DELETE /tenants/{id}

Delete tenant.

**Response (204 No Content)**

**Notes:**
- Soft delete by default
- Hard delete available for GDPR compliance

---

## Notifications API

**Base Path:** `/api/notifications`
**Auth:** Required (user-scoped)

### GET /notifications

Get user notifications.

**Query Parameters:**
- `unreadOnly` - Filter unread only
- `pageNumber`, `pageSize` - Pagination

**Response (200 OK):**
```json
{
  "items": [
    {
      "id": "guid",
      "title": "New message",
      "message": "You have a new message",
      "type": "info",
      "isRead": false,
      "createdAt": "2026-01-20T10:00:00Z"
    }
  ],
  "totalCount": 10,
  "unreadCount": 3
}
```

---

### GET /notifications/unread-count

Get unread notification count.

**Response (200 OK):**
```json
{
  "count": 3
}
```

---

### POST /notifications/{id}/read

Mark notification as read.

**Response (204 No Content)**

---

### POST /notifications/read-all

Mark all notifications as read.

**Response (204 No Content)**

---

### DELETE /notifications/{id}

Delete notification.

**Response (204 No Content)**

---

## Email Templates API

**Base Path:** `/api/email-templates`
**Required Permission:** `email-templates:read`, `email-templates:write`

### GET /email-templates

List email templates.

**Response (200 OK):**
```json
{
  "items": [
    {
      "id": "guid",
      "name": "WelcomeEmail",
      "subject": "Welcome to {{AppName}}",
      "isPlatformDefault": true,
      "isTenantOverride": false
    }
  ]
}
```

---

### GET /email-templates/{id}

Get template details.

**Response (200 OK):**
```json
{
  "id": "guid",
  "name": "WelcomeEmail",
  "subject": "Welcome to {{AppName}}",
  "htmlBody": "<html>...</html>",
  "variables": ["DisplayName", "Email", "AppName"]
}
```

---

### PUT /email-templates/{id}

Update template (creates tenant override if platform default).

**Request:**
```json
{
  "subject": "Custom welcome",
  "htmlBody": "<html>...</html>"
}
```

**Response (200 OK)**

**Notes:**
- Platform templates: Shared across all tenants
- Tenant overrides: Created on first edit (copy-on-write)

---

### POST /email-templates/{id}/test

Send test email.

**Request:**
```json
{
  "recipientEmail": "test@example.com",
  "variables": {
    "DisplayName": "Test User",
    "Email": "test@example.com"
  }
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "preview": "<html>...</html>"
}
```

---

## Audit API

**Base Path:** `/api/audit`
**Required Permission:** `audit:view`

### GET /audit/logs

Get audit logs with filtering.

**Query Parameters:**
- `startDate`, `endDate` - Date range
- `userId` - Filter by user
- `entityId` - Filter by entity
- `search` - Search across multiple fields
- `pageNumber`, `pageSize` - Pagination

**Response (200 OK):**
```json
{
  "items": [
    {
      "id": "guid",
      "correlationId": "correlation-guid",
      "httpRequestId": "http-guid",
      "handlerName": "CreateUserCommandHandler",
      "userId": "user-guid",
      "userEmail": "admin@example.com",
      "entityId": "entity-guid",
      "entityType": "User",
      "operationType": "Create",
      "before": null,
      "after": { "email": "new@example.com" },
      "timestamp": "2026-01-20T10:00:00Z"
    }
  ],
  "totalCount": 100
}
```

**Hierarchy:**
- **HTTP Request** (top level)
  - **Handler Execution** (CQRS handler)
    - **Entity Changes** (database mutations)

---

### GET /audit/user/{userId}/activity

Get user activity timeline.

**Response (200 OK):**
```json
{
  "userId": "user-guid",
  "actions": [
    {
      "timestamp": "2026-01-20T10:00:00Z",
      "action": "Created user",
      "details": { "email": "new@example.com" }
    }
  ]
}
```

---

### GET /audit/entity/{entityId}

Get entity change history.

**Response (200 OK):**
```json
{
  "entityId": "entity-guid",
  "entityType": "User",
  "changes": [
    {
      "timestamp": "2026-01-20T10:00:00Z",
      "userId": "admin-guid",
      "field": "Email",
      "oldValue": "old@example.com",
      "newValue": "new@example.com"
    }
  ]
}
```

---

## Blog API

**Base Path:** `/api/blog`
**Auth:** Public read, `blog:write` for mutations

### GET /blog/posts

List published posts.

**Query Parameters:**
- `search` - Search title/content
- `categoryId` - Filter by category
- `tagId` - Filter by tag
- `status` - Filter by status (draft/published)
- `pageNumber`, `pageSize` - Pagination

**Response (200 OK):**
```json
{
  "items": [
    {
      "id": "guid",
      "title": "Post Title",
      "slug": "post-title",
      "excerpt": "Short description...",
      "featuredImageUrl": "https://...",
      "author": "John Doe",
      "publishedAt": "2026-01-20T10:00:00Z",
      "categoryName": "Technology",
      "tags": ["React", "TypeScript"]
    }
  ],
  "totalCount": 50
}
```

---

### GET /blog/posts/{id}

Get post details.

**Response (200 OK):**
```json
{
  "id": "guid",
  "title": "Post Title",
  "slug": "post-title",
  "content": "Full content...",
  "excerpt": "Short description...",
  "featuredImageUrl": "https://...",
  "author": "John Doe",
  "publishedAt": "2026-01-20T10:00:00Z",
  "category": { "id": "guid", "name": "Technology" },
  "tags": [{ "id": "guid", "name": "React" }],
  "seoTitle": "SEO Title",
  "seoDescription": "SEO description",
  "seoKeywords": "react, typescript"
}
```

---

### POST /blog/posts

Create post (draft).

**Request:**
```json
{
  "title": "New Post",
  "content": "Content...",
  "excerpt": "Summary...",
  "categoryId": "category-guid",
  "tagIds": ["tag-guid-1", "tag-guid-2"],
  "featuredImageUrl": "https://...",
  "seoTitle": "SEO Title",
  "seoDescription": "SEO description"
}
```

**Response (201 Created)**

---

### PUT /blog/posts/{id}

Update post.

**Request:** (Same as POST)

**Response (200 OK)**

---

### DELETE /blog/posts/{id}

Delete post.

**Response (204 No Content)**

---

### POST /blog/posts/{id}/publish

Publish draft post.

**Response (200 OK):**
```json
{
  "id": "guid",
  "status": "Published",
  "publishedAt": "2026-01-20T10:00:00Z"
}
```

---

### GET /blog/categories

List categories.

**Response (200 OK):**
```json
{
  "items": [
    {
      "id": "guid",
      "name": "Technology",
      "slug": "technology",
      "postCount": 25
    }
  ]
}
```

---

### POST /blog/categories

Create category.

**Request:**
```json
{
  "name": "New Category",
  "parentCategoryId": null
}
```

**Response (201 Created)**

---

### PUT /blog/categories/{id}

Update category.

**Response (200 OK)**

---

### DELETE /blog/categories/{id}

Delete category.

**Response (204 No Content)**

---

### GET /blog/tags

List tags.

**Response (200 OK):**
```json
{
  "items": [
    {
      "id": "guid",
      "name": "React",
      "slug": "react",
      "color": "#61DAFB",
      "postCount": 15
    }
  ]
}
```

---

### POST /blog/tags

Create tag.

**Request:**
```json
{
  "name": "Vue.js",
  "color": "#42B883"
}
```

**Response (201 Created)**

---

### PUT /blog/tags/{id}

Update tag.

**Response (200 OK)**

---

### DELETE /blog/tags/{id}

Delete tag.

**Response (204 No Content)**

---

## Media API

**Base Path:** `/api/media`
**Required Permission:** `media:write`

### POST /media/upload

Upload and process image.

**Request:**
- **Content-Type:** `multipart/form-data`
- **Form Fields:**
  - `file` - Image file (JPEG, PNG, WebP, AVIF, HEIC)
  - `folder` - Target folder (blog, content, avatars)
  - `entityId` - Optional entity ID

**Response (200 OK):**
```json
{
  "id": "guid",
  "originalUrl": "https://.../original.webp",
  "variants": {
    "thumb": "https://.../thumb.webp",
    "extraLarge": "https://.../xl.webp"
  },
  "thumbHash": "base64-encoded-hash",
  "dominantColor": "#3B82F6",
  "width": 1920,
  "height": 1080
}
```

**Processing Pipeline:**
1. Validate format
2. Auto-rotate based on EXIF
3. Generate variants (Thumb 150px, XL 1920px)
4. Encode to WebP
5. Generate ThumbHash for blur placeholder
6. Save and return URLs

---

### GET /media/{*path}

Serve uploaded media files.

**Examples:**
- `GET /media/avatars/user-123.webp`
- `GET /media/blog/post-thumbnail.webp`

**Caching:** 1 year (immutable, unique slugs/GUIDs)

---

## Developer Logs API

**Base Path:** `/api/developer-logs`
**Auth:** Development environment only

### GET /developer-logs/stream

Real-time log streaming via SignalR.

**Connection:**
```typescript
import { HubConnectionBuilder } from '@microsoft/signalr'

const connection = new HubConnectionBuilder()
  .withUrl('/api/developer-logs/stream')
  .build()

connection.on('LogEntry', (log) => {
  console.log(log)
})

await connection.start()
```

**Log Entry:**
```json
{
  "timestamp": "2026-01-20T10:00:00Z",
  "level": "Information",
  "message": "User logged in",
  "properties": {
    "UserId": "user-guid",
    "IP": "127.0.0.1"
  }
}
```

**Filters:**
- Level: Debug, Information, Warning, Error
- Search: Message/property search

---

## Error Response Format

All API errors follow the RFC 7807 ProblemDetails format:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Validation failed",
  "errors": {
    "Email": ["Email address is required", "Email format is invalid"],
    "Password": ["Password must be at least 6 characters"]
  },
  "traceId": "correlation-guid"
}
```

### Common Status Codes

| Code | Meaning | When |
|------|---------|------|
| 200 | OK | Success |
| 201 | Created | Resource created |
| 204 | No Content | Success, no response body |
| 400 | Bad Request | Validation error |
| 401 | Unauthorized | Not authenticated |
| 403 | Forbidden | Not authorized |
| 404 | Not Found | Resource doesn't exist |
| 409 | Conflict | Resource conflict |
| 422 | Unprocessable Entity | Business rule violation |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Server error |

---

## Rate Limiting

**Default Limits:**
- Anonymous: 100 requests/minute
- Authenticated: 1000 requests/minute

**Headers:**
```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1640000000
```

**Exceeded Response (429):**
```json
{
  "type": "https://httpstatuses.com/429",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Rate limit exceeded. Try again in 60 seconds."
}
```

---

## Pagination

All list endpoints support pagination:

**Query Parameters:**
- `pageNumber` - Page number (1-based, default: 1)
- `pageSize` - Items per page (default: 10, max: 100)

**Response Format:**
```json
{
  "items": [...],
  "pageNumber": 1,
  "pageSize": 10,
  "totalCount": 100,
  "totalPages": 10,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

---

## Products API

**Base Path:** `/api/products`

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | `/products` | List products (paged, filterable) | ✅ |
| GET | `/products/{id}` | Get product by ID | ✅ |
| POST | `/products` | Create product | ✅ |
| PUT | `/products/{id}` | Update product | ✅ |
| DELETE | `/products/{id}` | Delete product (soft) | ✅ |
| POST | `/products/{id}/publish` | Publish product (Draft → Active) | ✅ |
| POST | `/products/{id}/archive` | Archive product | ✅ |
| POST | `/products/{id}/duplicate` | Duplicate product | ✅ |
| POST | `/products/bulk/publish` | Bulk publish products | ✅ |
| POST | `/products/bulk/archive` | Bulk archive products | ✅ |
| POST | `/products/bulk/delete` | Bulk delete products | ✅ |
| POST | `/products/bulk/import` | Bulk import products | ✅ |
| GET | `/products/export` | Export products to CSV | ✅ |
| GET | `/products/stats` | Get product statistics | ✅ |

### Product Images (`/api/products/{productId}/images`)

| Method | Path | Description |
|--------|------|-------------|
| POST | `/images` | Add product image |
| POST | `/images/upload` | Upload product image file |
| PUT | `/images/{id}` | Update product image |
| DELETE | `/images/{id}` | Delete product image |
| POST | `/images/reorder` | Reorder product images |
| POST | `/images/{id}/set-primary` | Set primary image |

### Product Variants (`/api/products/{productId}/variants`)

| Method | Path | Description |
|--------|------|-------------|
| POST | `/variants` | Add product variant |
| PUT | `/variants/{id}` | Update product variant |
| DELETE | `/variants/{id}` | Delete product variant |

### Product Options (`/api/products/{productId}/options`)

| Method | Path | Description |
|--------|------|-------------|
| POST | `/options` | Add product option |
| PUT | `/options/{id}` | Update product option |
| DELETE | `/options/{id}` | Delete product option |
| POST | `/options/{optionId}/values` | Add option value |
| PUT | `/options/{optionId}/values/{id}` | Update option value |
| DELETE | `/options/{optionId}/values/{id}` | Delete option value |

### Product Attribute Assignments (`/api/products/{productId}/attributes`)

| Method | Path | Description |
|--------|------|-------------|
| GET | `/attributes` | Get product attribute assignments |
| GET | `/attributes/form-schema` | Get attribute form schema |
| POST | `/attributes` | Set product attribute value |
| POST | `/attributes/bulk` | Bulk update attributes |

---

## Product Categories API

**Base Path:** `/api/products/categories`

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | `/categories` | List categories (hierarchical) | ✅ |
| GET | `/categories/{id}` | Get category by ID | ✅ |
| POST | `/categories` | Create category | ✅ |
| PUT | `/categories/{id}` | Update category | ✅ |
| DELETE | `/categories/{id}` | Delete category (soft) | ✅ |

### Category Attributes (`/api/products/categories/{categoryId}/attributes`)

| Method | Path | Description |
|--------|------|-------------|
| GET | `/attributes` | Get category attributes |
| GET | `/attributes/form-schema` | Get attribute form schema |
| POST | `/attributes` | Assign attribute to category |
| PUT | `/attributes/{id}` | Update category attribute |
| DELETE | `/attributes/{id}` | Remove attribute from category |

---

## Product Attributes API

**Base Path:** `/api/product-attributes`

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | `/product-attributes` | List attributes (paged) | ✅ |
| GET | `/product-attributes/{id}` | Get attribute by ID | ✅ |
| POST | `/product-attributes` | Create attribute | ✅ |
| PUT | `/product-attributes/{id}` | Update attribute | ✅ |
| DELETE | `/product-attributes/{id}` | Delete attribute | ✅ |

### Attribute Values (`/api/product-attributes/{attributeId}/values`)

| Method | Path | Description |
|--------|------|-------------|
| POST | `/values` | Add predefined value |
| PUT | `/values/{id}` | Update value |
| DELETE | `/values/{id}` | Remove value |

**Attribute Types:** Select, MultiSelect, Text, TextArea, Number, Decimal, Boolean, Date, DateTime, Color, Range, Url, File

---

## Product Filters API

**Base Path:** `/api/products/filter`

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | `/filter` | Filter products with facets (public) | ❌ |
| GET | `/filter/category/{slug}` | Get category-specific filters | ❌ |

Supports: category slug, brand filter, price range, attribute filters, search query, sorting.

---

## Brands API

**Base Path:** `/api/brands`

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | `/brands` | List brands (paged) | ✅ |
| GET | `/brands/{id}` | Get brand by ID | ✅ |
| POST | `/brands` | Create brand | ✅ |
| PUT | `/brands/{id}` | Update brand | ✅ |
| DELETE | `/brands/{id}` | Delete brand (soft) | ✅ |

---

## Cart API

**Base Path:** `/api/cart`

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | `/cart` | Get current cart | Mixed (User/SessionId) |
| GET | `/cart/summary` | Get cart summary (mini-cart) | Mixed |
| POST | `/cart/items` | Add item to cart | Mixed |
| PUT | `/cart/items/{id}` | Update cart item quantity | Mixed |
| DELETE | `/cart/items/{id}` | Remove item from cart | Mixed |
| DELETE | `/cart` | Clear entire cart | Mixed |
| POST | `/cart/merge` | Merge guest cart on login | ✅ |

Guest carts use `SessionId` (cookie/header). On login, call merge to combine.

---

## Checkout API

**Base Path:** `/api/checkout`

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | `/checkout/{sessionId}` | Get checkout session | ✅ |
| POST | `/checkout/initiate` | Initiate checkout from cart | ✅ |
| POST | `/checkout/{sessionId}/shipping-address` | Set shipping address | ✅ |
| POST | `/checkout/{sessionId}/billing-address` | Set billing address | ✅ |
| POST | `/checkout/{sessionId}/shipping-method` | Select shipping method | ✅ |
| POST | `/checkout/{sessionId}/payment-method` | Select payment method | ✅ |
| POST | `/checkout/{sessionId}/complete` | Complete checkout → create order | ✅ |

Session expires after 30 minutes (configurable).

---

## Orders API

**Base Path:** `/api/orders`

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | `/orders` | List orders (paged, filterable) | ✅ |
| GET | `/orders/{id}` | Get order by ID | ✅ |
| POST | `/orders` | Create order | ✅ |
| POST | `/orders/{id}/confirm` | Confirm order | ✅ |
| POST | `/orders/{id}/ship` | Ship order | ✅ |
| POST | `/orders/{id}/cancel` | Cancel order (releases inventory) | ✅ |

**Order Status Flow:** Pending → Confirmed → Processing → Shipped → Delivered → Completed (or → Cancelled)

---

## Payments API

**Base Path:** `/api/payments`

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | `/payments` | List payment transactions | ✅ |
| GET | `/payments/{id}` | Get payment transaction | ✅ |
| POST | `/payments` | Create payment | ✅ |
| POST | `/payments/{id}/cancel` | Cancel payment | ✅ |
| POST | `/payments/refunds/request` | Request refund | ✅ |
| POST | `/payments/refunds/{id}/approve` | Approve refund | ✅ |
| POST | `/payments/refunds/{id}/reject` | Reject refund | ✅ |
| GET | `/payments/cod/pending` | Get pending COD payments | ✅ |
| POST | `/payments/cod/{id}/confirm` | Confirm COD collection | ✅ |

## Payment Gateways API

**Base Path:** `/api/payment-gateways`

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | `/payment-gateways` | List payment gateways | ✅ Admin |
| GET | `/payment-gateways/{id}` | Get gateway details | ✅ Admin |
| POST | `/payment-gateways` | Configure new gateway | ✅ Admin |
| PUT | `/payment-gateways/{id}` | Update gateway config | ✅ Admin |
| GET | `/payment-gateways/active` | Get active gateways (checkout) | ✅ |
| POST | `/payment-gateways/test` | Test gateway connection | ✅ Admin |
| GET | `/payment-gateways/schemas` | Get gateway config schemas | ✅ Admin |

---

## Shipping API

**Base Path:** `/api/shipping`

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| POST | `/shipping/rates/calculate` | Calculate shipping rates | ✅ |
| POST | `/shipping/orders` | Create shipping order | ✅ |
| GET | `/shipping/orders/{trackingNumber}` | Get shipping order | ✅ |
| POST | `/shipping/orders/{trackingNumber}/cancel` | Cancel shipping order | ✅ |
| GET | `/shipping/tracking/{trackingNumber}` | Get tracking events | ✅ |

## Shipping Providers API

**Base Path:** `/api/shipping-providers`

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | `/shipping-providers` | List all providers (admin) | ✅ Admin |
| GET | `/shipping-providers/active` | Get active providers | ✅ |
| GET | `/shipping-providers/{id}` | Get provider details | ✅ Admin |
| POST | `/shipping-providers` | Configure new provider | ✅ Admin |
| PUT | `/shipping-providers/{id}` | Update provider config | ✅ Admin |

---

## Legal Pages API

**Base Path:** `/api/legal-pages` (Admin) | `/api/public/legal` (Public)

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | `/legal-pages` | List legal pages | ✅ Admin |
| GET | `/legal-pages/{id}` | Get legal page by ID | ✅ Admin |
| PUT | `/legal-pages/{id}` | Update legal page content | ✅ Admin |
| POST | `/legal-pages/{id}/revert` | Revert to platform default | ✅ Admin |
| GET | `/public/legal/{slug}` | Get public legal page | ❌ Public |

---

## Platform Settings API

**Base Path:** `/api/platform-settings`

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | `/platform-settings/smtp` | Get SMTP settings | ✅ Platform Admin |
| PUT | `/platform-settings/smtp` | Update SMTP settings | ✅ Platform Admin |
| POST | `/platform-settings/smtp/test` | Test SMTP connection | ✅ Platform Admin |

---

## Tenant Settings API

**Base Path:** `/api/tenant-settings`

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | `/tenant-settings/branding` | Get branding settings | ✅ Tenant Admin |
| PUT | `/tenant-settings/branding` | Update branding | ✅ Tenant Admin |
| GET | `/tenant-settings/contact` | Get contact settings | ✅ Tenant Admin |
| PUT | `/tenant-settings/contact` | Update contact settings | ✅ Tenant Admin |
| GET | `/tenant-settings/regional` | Get regional settings | ✅ Tenant Admin |
| PUT | `/tenant-settings/regional` | Update regional settings | ✅ Tenant Admin |
| GET | `/tenant-settings/smtp` | Get tenant SMTP settings | ✅ Tenant Admin |
| PUT | `/tenant-settings/smtp` | Update tenant SMTP | ✅ Tenant Admin |
| POST | `/tenant-settings/smtp/test` | Test tenant SMTP | ✅ Tenant Admin |
| POST | `/tenant-settings/smtp/revert` | Revert to platform SMTP | ✅ Tenant Admin |

---

## Feeds API

**Public RSS and Sitemap endpoints (no authentication required)**

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | `/blog/feed.xml` | RSS 2.0 feed of published posts | ❌ |
| GET | `/rss.xml` | RSS feed (alternative path) | ❌ |
| GET | `/sitemap.xml` | XML sitemap for SEO | ❌ |

---

## Filter Analytics API

**Base Path:** `/api/analytics/filter-events`

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| POST | `/analytics/filter-events` | Track filter usage event | ❌ Public |
| GET | `/analytics/filter-events/popular` | Get popular filters | ✅ Admin |

---

## Versioning

NOIR uses URL path versioning:

**Current Version:** v1 (implicit, no version in URL)
**Future Versions:** `/api/v2/...`

---

## Related Documentation

| Document | Purpose |
|----------|---------|
| [KNOWLEDGE_BASE.md](KNOWLEDGE_BASE.md) | Complete codebase reference |
| [Backend Patterns](backend/README.md) | Implementation patterns |
| [Frontend Architecture](frontend/architecture.md) | Frontend integration |

---

*Last Updated: 2026-02-08 | Total Endpoints: 160+ across 30 groups | Supports: OpenAPI 3.0*
