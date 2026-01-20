# Platform Admin Tenant ID Bug Fix

**Date:** 2026-01-20
**Status:** ✅ Fixed
**Severity:** Medium
**Category:** Authentication/Authorization

## Problem

Platform admin users were showing a tenant GUID in the dashboard instead of "Platform" (null tenant). This broke the expected behavior where system users (platform admins) should have `TenantId = null` for cross-tenant access.

**Symptoms:**
- Dashboard showed: `Tenant: 98f6120e-44eb-4c87-83f9-59f90a1e3f46`
- Expected: `Tenant: Platform`
- Database correctly had `TenantId = NULL` for platform admin
- JWT token correctly had no `tenant_id` claim

## Root Cause

In `src/NOIR.Application/Features/Auth/Queries/GetCurrentUser/GetCurrentUserQueryHandler.cs`, line 53 was using `_currentUser.TenantId` (from HTTP context/tenant resolution middleware) instead of `user.TenantId` (from database):

```csharp
// ❌ WRONG - Line 53
var userDto = new CurrentUserDto(
    ...
    _currentUser.TenantId,  // Uses tenant from HTTP context
    ...
);
```

Even though the JWT token had no `tenant_id` claim, the tenant resolution middleware was setting a default tenant in the HTTP context, which was then incorrectly returned by the `/api/auth/me` endpoint.

## Solution

Changed line 53 to use `user.TenantId` from the database:

```csharp
// ✅ CORRECT
var userDto = new CurrentUserDto(
    ...
    user.TenantId,  // Use TenantId from database, not HTTP context
    ...
);
```

**File Changed:**
- `src/NOIR.Application/Features/Auth/Queries/GetCurrentUser/GetCurrentUserQueryHandler.cs`

## Verification

Created Playwright E2E test (`src/NOIR.Web/frontend/e2e/tests/platform-admin-tenant-test.spec.ts`) that verifies:

1. ✅ Platform admin login succeeds
2. ✅ Dashboard displays "Platform" not a GUID
3. ✅ `/api/auth/me` response omits `tenantId` field (null value)
4. ✅ JWT token has no `tenant_id` claim

**Test Results:**
```
✅ CORRECT: /api/auth/me returns tenantId = null (omitted from JSON)
  ✓  2 [chromium] › e2e/tests/platform-admin-tenant-test.spec.ts:24:1 › platform admin should have no tenant ID (3.4s)

  2 passed (7.1s)
```

## Investigation Notes

### Data Flow Analysis

1. **Database:** ✅ Platform admin has `TenantId = NULL`
2. **Login Flow:** ✅ `LoginCommandHandler` correctly passes `user.TenantId` (null) to token generation
3. **JWT Token:** ✅ Generated without `tenant_id` claim (verified in logs)
4. **Bug Location:** ❌ `GetCurrentUserQueryHandler` was using HTTP context tenant instead of database value

### Debug Logging Added (Then Removed)

Temporarily added logging to:
- `LoginCommandHandler.CompleteLogin` - Track TenantId in token generation
- `UserIdentityService.FindByEmailAsync` - Track user loading
- `UserIdentityService.FindTenantsByEmailAsync` - Track tenant discovery

Logs confirmed:
```
[18:26:53 INF] [LoginHandler] Generating token for user: Email=platform@noir.local, TenantId=NULL, IsSystemUser=True
```

## Related Files

**Modified:**
- `src/NOIR.Application/Features/Auth/Queries/GetCurrentUser/GetCurrentUserQueryHandler.cs` (Line 53)

**Tests Added:**
- `src/NOIR.Web/frontend/e2e/tests/platform-admin-tenant-test.spec.ts`

**Tests Updated:**
- `tests/NOIR.Application.UnitTests/Infrastructure/TenantIdSetterInterceptorTests.cs` (Added mock logger)

## Lessons Learned

1. **Always use database values for user-specific data** - Don't rely on HTTP context for user properties that come from the database
2. **HTTP context vs Database** - Tenant resolution middleware may set default tenants in HTTP context, which can override null database values if not careful
3. **JSON serialization of null** - When C# returns `null`, JSON serialization omits the field entirely (becomes `undefined` in JavaScript, not `null`)
4. **E2E tests are critical** - The bug was only caught through end-to-end testing that verified the actual UI display

### Understanding _currentUser.TenantId vs user.TenantId

**Why did `_currentUser.TenantId` have a value when it should be null?**

`_currentUser.TenantId` is the **REQUEST tenant context** (from HTTP middleware), NOT the user's actual tenant property. For platform admins:

- **JWT token**: No `tenant_id` claim ✅ (correct)
- **Database**: `TenantId = NULL` ✅ (correct)
- **HTTP context**: `_currentUser.TenantId = "default"` (fallback from `WithStaticStrategy("default")`)

The middleware sets a fallback tenant because:
1. Platform admins may want to scope requests to specific tenants
2. Non-HTTP contexts (like database seeding) need a default tenant

**This is NOT a bug in the middleware** - it's working as designed. The bug was in **handlers using the wrong source**:

| Use Case | Correct Source | Wrong Source |
|----------|---------------|--------------|
| Filter query by tenant | `_currentUser.TenantId` | N/A |
| Check tenant access | `_currentUser.TenantId` | N/A |
| **Return user's tenant** | `user.TenantId` | ❌ `_currentUser.TenantId` |
| **Generate user token** | `user.TenantId` | ❌ `_currentUser.TenantId` |

**Key Principle**: Request context (`_currentUser`) is for **scoping/filtering**, database entity is for **user properties**.

## Prevention

To prevent similar bugs:
1. In handlers that return user data, always use the entity/DTO values from the database
2. Use `_currentUser` (HTTP context) only for **identifying** the current user, not for returning their **properties**
3. Add E2E tests for critical authentication flows
4. Review any code that mixes HTTP context data with database data

## Additional Context

- Platform admins are "system users" with `IsSystemUser = true` and `TenantId = null`
- They have cross-tenant access for platform administration
- Tenant resolution middleware is designed for regular tenant-scoped users
- System users should bypass tenant context for their profile data
