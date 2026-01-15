# Permission Enforcement & Welcome Email Design

**Date:** 2025-01-15
**Status:** Approved

## Overview

This design addresses security gaps in the permission system and implements welcome email functionality for new users.

## Problems Identified

1. **Permission cache not invalidated** - When user roles change, cached permissions persist for 5-30 minutes
2. **Frontend only checks authentication** - No permission-based UI hiding
3. **Admin user not protected** - Can remove all roles from admin, breaking access
4. **No welcome email** - New users don't receive credentials

## Solutions

### 1. Permission Cache Invalidation Fix

**Root cause:** `IPermissionCacheInvalidator` exists but is never called.

**Fix:** Inject and call `IPermissionCacheInvalidator.InvalidateUser()` in:
- `AssignRolesToUserCommandHandler` - after roles assigned
- `AssignPermissionToRoleCommandHandler` - invalidate all users in role via `InvalidateRoleAsync()`
- `RemovePermissionFromRoleCommandHandler` - invalidate all users in role

### 2. System User Protection

**Approach:** Add `IsSystemUser` boolean flag to `ApplicationUser` entity.

**Implementation:**
- Add EF migration for `IsSystemUser` column (default: false)
- Update seed data to set `admin@noir.local` as `IsSystemUser = true`
- Add `IsSystemUser` to `UserDto`
- Backend handlers check flag before modifying user:
  - Cannot delete system user
  - Cannot remove all roles from system user
  - Cannot lock system user
- Return error: "Cannot modify system user"

**UI Indicator:**
- Show shield icon + "System" badge on protected users
- Disable/hide action buttons for system users
- Tooltip: "System user cannot be modified"

### 3. Frontend Permission Enforcement (Full Granular)

**New API endpoint:** `GET /api/users/me/permissions`
- Returns array of effective permission strings for current user
- e.g., `["users:read", "users:create", "emailtemplates:read"]`

**New hook:** `usePermissions()`
```typescript
const { permissions, hasPermission, isLoading } = usePermissions()

// Check single permission
if (hasPermission('users:create')) { ... }

// Check multiple (any)
if (hasAnyPermission(['users:update', 'users:delete'])) { ... }
```

**New component:** `<PermissionGate>`
```typescript
<PermissionGate permission="users:create">
  <Button>Create User</Button>
</PermissionGate>

<PermissionGate permissions={["users:update", "users:delete"]} requireAll={false}>
  <ActionMenu />
</PermissionGate>
```

**Updated ProtectedRoute:**
```typescript
<ProtectedRoute requiredPermission="emailtemplates:read">
  <EmailTemplatesPage />
</ProtectedRoute>
```

**Sidebar filtering:**
- Menu items include `requiredPermission` property
- Items without permission are hidden (not just disabled)

### 4. Welcome Email

**Flow:**
1. Admin creates user via `CreateUserCommand` with password
2. Handler creates user via `IUserIdentityService.CreateUserAsync()`
3. Handler sends welcome email via `IEmailService.SendTemplatedEmailAsync()`
4. User receives email with login URL and credentials

**Email Template:** `user-welcome`
- Subject: `Welcome to NOIR - Your Account Has Been Created`
- Variables: `{{FirstName}}`, `{{Email}}`, `{{Password}}`, `{{LoginUrl}}`

**Base URL Service:** `IBaseUrlService`
```csharp
public interface IBaseUrlService
{
    string GetBaseUrl();
}

public class BaseUrlService : IBaseUrlService
{
    // Priority: HttpContext.Request.GetDisplayUrl() > AppSettings:BaseUrl
}
```

**AppSettings:**
```json
{
  "AppSettings": {
    "BaseUrl": "https://noir.example.com"
  }
}
```

## Files to Create/Modify

### Backend - Bug Fixes
- `src/NOIR.Application/Features/Users/Commands/AssignRolesToUser/AssignRolesToUserCommandHandler.cs`
- `src/NOIR.Application/Features/Roles/Commands/AssignPermissionToRole/AssignPermissionToRoleCommandHandler.cs`
- `src/NOIR.Application/Features/Roles/Commands/RemovePermissionFromRole/RemovePermissionFromRoleCommandHandler.cs`

### Backend - System User
- `src/NOIR.Infrastructure/Identity/ApplicationUser.cs` - add IsSystemUser
- `src/NOIR.Infrastructure/Persistence/Migrations/` - new migration
- `src/NOIR.Infrastructure/Persistence/Seeders/UserSeeder.cs` - update seed
- `src/NOIR.Application/Features/Users/DTOs/UserDtos.cs` - add IsSystemUser
- `src/NOIR.Application/Features/Users/Commands/*/` - add protection checks

### Backend - Permissions Endpoint
- `src/NOIR.Application/Features/Users/Queries/GetCurrentUserPermissions/`
- `src/NOIR.Web/Endpoints/UserEndpoints.cs`

### Backend - Welcome Email
- `src/NOIR.Application/Common/Interfaces/IBaseUrlService.cs`
- `src/NOIR.Infrastructure/Services/BaseUrlService.cs`
- `src/NOIR.Infrastructure/Persistence/Seeders/EmailTemplateSeeder.cs` - add template
- `src/NOIR.Application/Features/Users/Commands/CreateUser/CreateUserCommandHandler.cs`

### Frontend
- `src/NOIR.Web/frontend/src/hooks/usePermissions.ts`
- `src/NOIR.Web/frontend/src/components/PermissionGate.tsx`
- `src/NOIR.Web/frontend/src/components/ProtectedRoute.tsx` - update
- `src/NOIR.Web/frontend/src/components/portal/Sidebar.tsx` - update
- `src/NOIR.Web/frontend/src/pages/admin/UsersPage.tsx` - update

## Testing Checklist

- [ ] Remove all roles from non-system user → loses access immediately
- [ ] Try to remove roles from system user → error returned
- [ ] User without `emailtemplates:read` → page hidden in sidebar, 403 on direct URL
- [ ] Create new user → welcome email sent with credentials
- [ ] Login with credentials from email → works
