# NOIR Knowledge Base

**Last Updated:** 2026-01-15
**Version:** 1.4

A comprehensive cross-referenced guide to the NOIR codebase, patterns, and architecture.

---

## Quick Navigation

| Section | Description |
|---------|-------------|
| [Architecture Overview](#architecture-overview) | Clean Architecture layers and dependencies |
| [Domain Layer](#domain-layer) | Entities, interfaces, value objects |
| [Application Layer](#application-layer) | Features, commands, queries, specifications |
| [Infrastructure Layer](#infrastructure-layer) | Persistence, identity, services |
| [Web Layer](#web-layer) | API endpoints, middleware, frontend |
| [Cross-Cutting Concerns](#cross-cutting-concerns) | Audit, auth, validation, multi-tenancy |
| [Development Guide](#development-guide) | Common tasks and patterns |
| [Testing](#testing) | Test structure and patterns |
| [Documentation Map](#documentation-map) | All docs with descriptions |

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        NOIR.Web                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │  Endpoints   │  │  Middleware  │  │  frontend/ (React)   │   │
│  └──────────────┘  └──────────────┘  └──────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    NOIR.Infrastructure                           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │  Persistence │  │   Identity   │  │      Services        │   │
│  │  (EF Core)   │  │  (Auth/JWT)  │  │  (Email, Storage)    │   │
│  └──────────────┘  └──────────────┘  └──────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                     NOIR.Application                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │   Features   │  │ Specifications│  │     Behaviors        │   │
│  │(Commands/Queries)│ │              │  │   (Middleware)       │   │
│  └──────────────┘  └──────────────┘  └──────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                       NOIR.Domain                                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐   │
│  │   Entities   │  │  Interfaces  │  │   Common/ValueObjects │   │
│  └──────────────┘  └──────────────┘  └──────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
```

### Dependency Flow

```
Domain ← Application ← Infrastructure ← Web
```

- **Domain**: No external dependencies (pure C#)
- **Application**: Depends only on Domain
- **Infrastructure**: Implements Application interfaces
- **Web**: Composes all layers

---

## Domain Layer

**Path:** `src/NOIR.Domain/`

### Base Classes

| Class | Path | Purpose |
|-------|------|---------|
| `Entity<TId>` | `Common/Entity.cs` | Base entity with Id, CreatedAt, ModifiedAt |
| `AuditableEntity<TId>` | `Common/AuditableEntity.cs` | Entity with full audit fields |
| `AggregateRoot<TId>` | `Common/AggregateRoot.cs` | DDD aggregate root with domain events |
| `ValueObject` | `Common/ValueObject.cs` | Immutable value object base |
| `Result<T>` | `Common/Result.cs` | Railway-oriented error handling |

### Entities

#### Core Entities

| Entity | Path | Related To |
|--------|------|------------|
| `Permission` | `Entities/Permission.cs` | [RBAC Authorization](#authorization) |
| `PermissionTemplate` | `Entities/PermissionTemplate.cs` | Role permission presets |
| `RefreshToken` | `Entities/RefreshToken.cs` | [JWT Pattern](backend/patterns/jwt-refresh-token.md) |
| `ResourceShare` | `Entities/ResourceShare.cs` | Multi-user sharing |

#### Audit Entities

| Entity | Path | Related To |
|--------|------|------------|
| `EntityAuditLog` | `Entities/EntityAuditLog.cs` | [Audit Logging](backend/patterns/hierarchical-audit-logging.md) |
| `HandlerAuditLog` | `Entities/HandlerAuditLog.cs` | [Audit Logging](backend/patterns/hierarchical-audit-logging.md) |
| `HttpRequestAuditLog` | `Entities/HttpRequestAuditLog.cs` | [Audit Logging](backend/patterns/hierarchical-audit-logging.md) |

#### Multi-Tenancy Entities

| Entity | Path | Related To |
|--------|------|------------|
| `Tenant` | `Entities/Tenant.cs` | [Multi-Tenancy](#multi-tenancy) |
| `TenantBranding` | `Entities/TenantBranding.cs` | Tenant customization |
| `TenantDomain` | `Entities/TenantDomain.cs` | Custom tenant domains |
| `TenantSetting` | `Entities/TenantSetting.cs` | Tenant configuration |
| `UserTenantMembership` | `Entities/UserTenantMembership.cs` | [Multi-Tenant User Access](#multi-tenancy) |

#### Notification Entities

| Entity | Path | Related To |
|--------|------|------------|
| `Notification` | `Entities/Notification.cs` | [Notifications Feature](#notifications-feature) |
| `NotificationPreference` | `Entities/NotificationPreference.cs` | User notification settings |
| `EmailTemplate` | `Entities/EmailTemplate.cs` | [Email Templates Feature](#emailtemplates-feature) |

#### Authentication Entities

| Entity | Path | Related To |
|--------|------|------------|
| `EmailChangeOtp` | `Entities/EmailChangeOtp.cs` | Email change verification |
| `PasswordResetOtp` | `Entities/PasswordResetOtp.cs` | Password reset flow |

#### Identity Entities (Infrastructure Layer)

| Entity | Path | Related To |
|--------|------|------------|
| `ApplicationUser` | `Infrastructure/Identity/ApplicationUser.cs` | User with extended properties (LockedAt, LockedBy) |
| `ApplicationRole` | `Infrastructure/Identity/ApplicationRole.cs` | Role with hierarchy (ParentRoleId, IconName, Color) |

### Interfaces

| Interface | Path | Implementation |
|-----------|------|----------------|
| `IRepository<TEntity, TId>` | `Interfaces/IRepository.cs` | `Repository<>` in Infrastructure |
| `IReadRepository<TEntity, TId>` | `Interfaces/IReadRepository.cs` | Read-only queries |
| `ISpecification<T>` | `Interfaces/ISpecification.cs` | Query specifications |
| `IUnitOfWork` | `Interfaces/IUnitOfWork.cs` | Transaction boundary |

### Constants

| Constant | Path | Purpose |
|----------|------|---------|
| `Permissions` | `Common/Permissions.cs` | Permission string constants |
| `Roles` | `Common/Roles.cs` | System role constants |

---

## Application Layer

**Path:** `src/NOIR.Application/`

### Feature Modules

#### Auth Feature
**Path:** `Features/Auth/`

| Type | Name | Path |
|------|------|------|
| Command | `LoginCommand` | `Commands/Login/` |
| Command | `RefreshTokenCommand` | `Commands/RefreshToken/` |
| Command | `LogoutCommand` | `Commands/Logout/` |
| Command | `UpdateUserProfileCommand` | `Commands/UpdateUserProfile/` |
| Query | `GetCurrentUserQuery` | `Queries/GetCurrentUser/` |
| Query | `GetUserByIdQuery` | `Queries/GetUserById/` |
| DTO | `AuthResponse` | `DTOs/AuthResponse.cs` |

**Related:** [AuthEndpoints](#auth-endpoints), [TokenService](#identity-services)

#### Users Feature
**Path:** `Features/Users/`

| Type | Name | Path |
|------|------|------|
| Command | `CreateUserCommand` | `Commands/CreateUser/` |
| Command | `UpdateUserCommand` | `Commands/UpdateUser/` |
| Command | `DeleteUserCommand` | `Commands/DeleteUser/` |
| Command | `AssignRolesToUserCommand` | `Commands/AssignRoles/` |
| Command | `LockUserCommand` | `Commands/LockUser/` |
| Query | `GetUsersQuery` | `Queries/GetUsers/` |
| Query | `GetUserRolesQuery` | `Queries/GetUserRoles/` |
| DTO | `UserDtos` | `DTOs/UserDtos.cs` |

**Related:** [UserEndpoints](#user-endpoints)

#### Roles Feature
**Path:** `Features/Roles/`

| Type | Name | Path |
|------|------|------|
| Command | `CreateRoleCommand` | `Commands/CreateRole/` |
| Command | `UpdateRoleCommand` | `Commands/UpdateRole/` |
| Command | `DeleteRoleCommand` | `Commands/DeleteRole/` |
| Query | `GetRolesQuery` | `Queries/GetRoles/` |
| Query | `GetRoleByIdQuery` | `Queries/GetRoleById/` |
| DTO | `RoleDtos` | `DTOs/RoleDtos.cs` |

**Related:** [RoleEndpoints](#role-endpoints)

#### Permissions Feature
**Path:** `Features/Permissions/`

| Type | Name | Path |
|------|------|------|
| Command | `AssignPermissionToRoleCommand` | `Commands/AssignToRole/` |
| Command | `RemovePermissionFromRoleCommand` | `Commands/RemoveFromRole/` |
| Query | `GetRolePermissionsQuery` | `Queries/GetRolePermissions/` |
| Query | `GetUserPermissionsQuery` | `Queries/GetUserPermissions/` |
| Query | `GetAllPermissionsQuery` | `Queries/GetAllPermissions/` |
| Query | `GetPermissionTemplatesQuery` | `Queries/GetPermissionTemplates/` |

**Related:** [Authorization](#authorization), [PermissionEndpoints](#permission-endpoints)

#### EmailTemplates Feature
**Path:** `Features/EmailTemplates/`

| Type | Name | Path |
|------|------|------|
| Command | `UpdateEmailTemplateCommand` | `Commands/Update/` |
| Query | `GetEmailTemplatesQuery` | `Queries/GetAll/` |
| Query | `GetEmailTemplateByIdQuery` | `Queries/GetById/` |

**Service:** `EmailService` (`Infrastructure/Services/EmailService.cs`)
- Uses **platform-level fallback**: tenant-specific template → platform template (TenantId = null)
- Templates seeded as platform-level by default via `ApplicationDbContextSeeder`
- Supports variable replacement: `{{DisplayName}}`, `{{Email}}`, `{{Password}}`, etc.

**Related:** [EmailTemplateEndpoints](#email-template-endpoints), [Platform-Level Data](#platform-level-vs-tenant-level-data)

#### Notifications Feature
**Path:** `Features/Notifications/`

| Type | Name | Path |
|------|------|------|
| Command | `MarkAsReadCommand` | `Commands/MarkAsRead/` |
| Command | `MarkAllAsReadCommand` | `Commands/MarkAllAsRead/` |
| Command | `DeleteNotificationCommand` | `Commands/DeleteNotification/` |
| Command | `UpdatePreferencesCommand` | `Commands/UpdatePreferences/` |
| Query | `GetNotificationsQuery` | `Queries/GetNotifications/` |
| Query | `GetUnreadCountQuery` | `Queries/GetUnreadCount/` |
| Query | `GetPreferencesQuery` | `Queries/GetPreferences/` |

**Related:** [NotificationEndpoints](#notification-endpoints)

#### Tenants Feature
**Path:** `Features/Tenants/`

| Type | Name | Path |
|------|------|------|
| Command | `CreateTenantCommand` | `Commands/Create/` |
| Command | `UpdateTenantCommand` | `Commands/Update/` |
| Command | `DeleteTenantCommand` | `Commands/Delete/` |
| Query | `GetTenantsQuery` | `Queries/GetAll/` |
| Query | `GetTenantByIdQuery` | `Queries/GetById/` |

**Related:** [TenantEndpoints](#tenant-endpoints)

### Specifications

**Path:** `Specifications/`
**Pattern Doc:** [Repository & Specification](backend/patterns/repository-specification.md)

All database queries MUST use specifications:
```csharp
public class ActiveUsersSpec : Specification<User>
{
    public ActiveUsersSpec()
    {
        Query.Where(u => u.IsActive)
             .TagWith("GetActiveUsers");  // REQUIRED
    }
}
```

### Behaviors (Pipeline)

**Path:** `Behaviors/`

| Behavior | Purpose |
|----------|---------|
| `LoggingMiddleware` | Request/response logging |
| `PerformanceMiddleware` | Performance metrics |

### Common Interfaces

**Path:** `Common/Interfaces/`

| Interface | Implementation | Purpose |
|-----------|----------------|---------|
| `ICurrentUser` | `CurrentUserService` | Current user context |
| `ITokenService` | `TokenService` | JWT generation |
| `IEmailService` | `EmailService` | Email sending |
| `IFileStorage` | `FileStorageService` | File operations |
| `IDateTime` | `DateTimeService` | Testable date/time |
| `IDiffService` | `JsonDiffService` | Entity change diffs |

---

## Infrastructure Layer

**Path:** `src/NOIR.Infrastructure/`

### Persistence

**Path:** `Persistence/`
**Pattern Doc:** [Repository & Specification](backend/patterns/repository-specification.md)

| Component | Path | Purpose |
|-----------|------|---------|
| `ApplicationDbContext` | `Persistence/ApplicationDbContext.cs` | EF Core DbContext |
| `Repository<TEntity, TId>` | `Persistence/Repository.cs` | Generic repository |
| `SpecificationEvaluator` | `Persistence/SpecificationEvaluator.cs` | Specification evaluation |

#### Entity Configurations

**Path:** `Persistence/Configurations/`
**Pattern Doc:** [Entity Configuration](backend/patterns/entity-configuration.md)

Auto-discovered via `ApplyConfigurationsFromAssembly`.

#### Interceptors

**Path:** `Persistence/Interceptors/`

| Interceptor | Purpose |
|-------------|---------|
| `AuditableEntityInterceptor` | Sets audit fields (CreatedBy, ModifiedBy) |
| `DomainEventInterceptor` | Dispatches domain events |
| `TenantIdSetterInterceptor` | Multi-tenant isolation |
| `EntityAuditLogInterceptor` | Entity change logging |

### Identity Services

**Path:** `Identity/`

| Service | Path | Purpose |
|---------|------|---------|
| `ApplicationUser` | `Identity/ApplicationUser.cs` | Extended IdentityUser |
| `TokenService` | `Identity/TokenService.cs` | JWT generation/validation |
| `RefreshTokenService` | `Identity/RefreshTokenService.cs` | Refresh token management |
| `CookieAuthService` | `Identity/CookieAuthService.cs` | Cookie-based auth |

#### Handlers (Co-located with Commands)

**Path:** `Application/Features/{Feature}/Commands/{Action}/` or `Queries/{Action}/`

Handlers are co-located with their Commands/Queries and use constructor injection:
```csharp
// Application/Features/Auth/Commands/Login/LoginCommandHandler.cs
public class LoginCommandHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthResponse>> Handle(
        LoginCommand cmd,
        CancellationToken ct) { ... }
}
```

### Authorization

**Path:** `Identity/Authorization/`

| Component | Purpose |
|-----------|---------|
| `PermissionHandler` | Permission-based authorization |
| `ResourceAuthorizationHandler` | Resource-level authorization |

### Audit System

**Path:** `Audit/`
**Pattern Doc:** [Hierarchical Audit Logging](backend/patterns/hierarchical-audit-logging.md)

| Component | Purpose |
|-----------|---------|
| `HandlerAuditMiddleware` | CQRS handler execution logging |
| `HttpRequestAuditMiddleware` | HTTP request/response logging |
| `AuditRetentionJob` | Cleanup and archival |

### Services

**Path:** `Services/`
**Pattern Doc:** [DI Auto-Registration](backend/patterns/di-auto-registration.md)

| Service | Interface | Lifetime |
|---------|-----------|----------|
| `EmailService` | `IEmailService` | Scoped |
| `FileStorageService` | `IFileStorage` | Scoped |
| `DateTimeService` | `IDateTime` | Scoped |
| `CurrentUserService` | `ICurrentUser` | Scoped |
| `JsonDiffService` | `IDiffService` | Scoped |
| `DeviceFingerprintService` | `IDeviceFingerprintService` | Scoped |
| `BackgroundJobsService` | `IBackgroundJobsService` | Scoped |

---

## Web Layer

**Path:** `src/NOIR.Web/`

### API Endpoints

#### Auth Endpoints
**Path:** `Endpoints/AuthEndpoints.cs`
**Prefix:** `/api/auth`

| Method | Route | Handler |
|--------|-------|---------|
| POST | `/login` | `LoginCommand` |
| POST | `/refresh` | `RefreshTokenCommand` |
| POST | `/logout` | `LogoutCommand` |
| GET | `/me` | `GetCurrentUserQuery` |
| PUT | `/profile` | `UpdateUserProfileCommand` |

#### User Endpoints
**Path:** `Endpoints/UserEndpoints.cs`
**Prefix:** `/api/users`

| Method | Route | Handler |
|--------|-------|---------|
| GET | `/` | `GetUsersQuery` |
| POST | `/` | `CreateUserCommand` |
| GET | `/{id}` | `GetUserByIdQuery` |
| PUT | `/{id}` | `UpdateUserCommand` |
| DELETE | `/{id}` | `DeleteUserCommand` |
| GET | `/{id}/roles` | `GetUserRolesQuery` |
| POST | `/{id}/lock` | `LockUserCommand` |
| POST | `/{id}/unlock` | Unlocks user |
| POST | `/{id}/roles` | `AssignRolesToUserCommand` |

#### Role Endpoints
**Path:** `Endpoints/RoleEndpoints.cs`
**Prefix:** `/api/roles`

| Method | Route | Handler |
|--------|-------|---------|
| GET | `/` | `GetRolesQuery` |
| GET | `/{id}` | `GetRoleByIdQuery` |
| POST | `/` | `CreateRoleCommand` |
| PUT | `/{id}` | `UpdateRoleCommand` |
| DELETE | `/{id}` | `DeleteRoleCommand` |
| GET | `/{id}/permissions` | `GetRolePermissionsQuery` |
| GET | `/{id}/effective-permissions` | Gets inherited permissions |
| PUT | `/{id}/permissions` | `AssignPermissionToRoleCommand` |
| DELETE | `/{id}/permissions/{permissionId}` | `RemovePermissionFromRoleCommand` |

#### Permission Endpoints
**Path:** `Endpoints/PermissionEndpoints.cs`
**Prefix:** `/api/permissions`

| Method | Route | Handler |
|--------|-------|---------|
| GET | `/` | `GetAllPermissionsQuery` |
| GET | `/templates` | `GetPermissionTemplatesQuery` |

#### File Endpoints
**Path:** `Endpoints/FileEndpoints.cs`
**Prefix:** `/api/files`

| Method | Route | Purpose |
|--------|-------|---------|
| POST | `/upload` | Upload file |
| GET | `/{id}` | Download file |
| DELETE | `/{id}` | Delete file |

### Middleware

**Path:** `Middleware/`

| Middleware | Purpose |
|------------|---------|
| `ExceptionHandlingMiddleware` | Global exception handling, ProblemDetails |
| `SecurityHeadersMiddleware` | Security headers (CSP, HSTS, etc.) |

### Frontend

**Path:** `frontend/`
**Docs:** [Frontend Architecture](frontend/architecture.md)

| Directory | Purpose |
|-----------|---------|
| `src/components/` | Reusable UI components (shadcn/ui + 21st.dev) |
| `src/pages/` | Route pages |
| `src/hooks/` | Custom React hooks (usePermissions, etc.) |
| `src/services/` | API client and services |
| `src/contexts/` | React contexts (Auth, Theme) |
| `src/i18n/` | Internationalization |
| `src/lib/` | Utilities |
| `src/types/` | TypeScript types (auto-generated) |

#### Custom 21st.dev Components

| Component | Path | Usage |
|-----------|------|-------|
| `EmptyState` | `components/ui/empty-state.tsx` | Tables with no data |
| `Pagination` | `components/ui/pagination.tsx` | Data table pagination |
| `ColorPicker` | `components/ui/color-picker.tsx` | Role color selection |

#### Permission-Based UI Rendering

The frontend uses `usePermissions` hook to conditionally render UI elements based on user permissions:

```tsx
import { usePermissions, Permissions } from '@/hooks/usePermissions'

function UserActions() {
  const { hasPermission } = usePermissions()
  const canEdit = hasPermission(Permissions.UsersUpdate)
  const canDelete = hasPermission(Permissions.UsersDelete)

  return (
    <>
      {canEdit && <Button onClick={handleEdit}>Edit</Button>}
      {canDelete && <Button onClick={handleDelete}>Delete</Button>}
    </>
  )
}
```

**Key components using permission-based rendering:**
- `UsersPage` - Create, Edit, Delete, Assign Roles buttons
- `UserTable` - Action menu items per permission
- `EmailTemplatesPage` - Edit button visibility
- `RolesPage` - CRUD actions based on role permissions

#### API Error Handling

The `apiClient.ts` provides user-friendly error messages for HTTP status codes:
- **403 Forbidden**: Shows "You don't have permission to perform this action." (i18n: `messages.permissionDenied`)
- **401 Unauthorized**: Shows "Your session has expired. Please sign in again." (i18n: `messages.sessionExpired`)

---

## Cross-Cutting Concerns

### Authorization

**Location:** `Infrastructure/Identity/Authorization/`

NOIR uses a hybrid authorization model:
- **Role-based**: Traditional roles (Admin, User, etc.)
- **Permission-based**: Fine-grained `resource:action:scope` permissions

Permission format: `{resource}:{action}:{scope}`
- Example: `users:create:all`, `orders:read:own`

### Audit Trail

**Docs:** [Hierarchical Audit Logging](backend/patterns/hierarchical-audit-logging.md)

Three-level audit system:
1. **HTTP Level** - Request/response logging
2. **Handler Level** - Command/query execution
3. **Entity Level** - Database changes (before/after diff)

### Multi-Tenancy

**Package:** Finbuckle.MultiTenant
**Interceptor:** `TenantIdSetterInterceptor`

NOIR implements a multi-tenant architecture where:
- Users can belong to **multiple tenants** via `UserTenantMembership`
- Each membership has a **role** (Owner, Admin, Member, Viewer)
- One membership can be marked as **default** for the user
- Tenant-specific entities automatically get `TenantId` set via interceptor

#### User-Tenant Membership Model

```csharp
// UserTenantMembership - Platform-level entity (NOT tenant-scoped)
public class UserTenantMembership : Entity<Guid>
{
    public Guid UserId { get; }         // User reference
    public Guid TenantId { get; }       // Tenant reference
    public TenantRole Role { get; }     // Owner, Admin, Member, Viewer
    public bool IsDefault { get; }      // User's default tenant
    public DateTimeOffset JoinedAt { get; }
}
```

#### Tenant Roles

| Role | Permissions |
|------|-------------|
| Owner | Full control, can delete tenant |
| Admin | Manage users and settings |
| Member | Standard access |
| Viewer | Read-only access |

#### Platform-Level vs Tenant-Level Data

NOIR supports a **fallback pattern** where data can exist at two levels:

| Level | TenantId Value | Scope | Example |
|-------|----------------|-------|---------|
| Platform | `null` | Shared across all tenants | Default email templates |
| Tenant | `Guid` | Specific to one tenant | Custom email templates |

**Query Pattern for Fallback:**
```csharp
// First check tenant-specific, then fallback to platform
var templates = await _dbContext.Set<EmailTemplate>()
    .IgnoreQueryFilters()  // Bypass tenant filter
    .Where(t => t.Name == templateName && t.IsActive && !t.IsDeleted)
    .ToListAsync();

// Prefer tenant-specific, fallback to platform (TenantId = null)
var template = templates.FirstOrDefault(t => t.TenantId == currentTenantId)
    ?? templates.FirstOrDefault(t => t.TenantId == null);
```

**Key Points:**
- Platform-level data serves as **defaults** for all tenants
- Tenants can **override** platform defaults with their own data
- Use `IgnoreQueryFilters()` when querying both levels
- Seeder creates platform-level data with `TenantId = null`

### Validation

**Package:** FluentValidation
**Location:** Each command has a corresponding `*Validator.cs` file

```csharp
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MinimumLength(6);
    }
}
```

---

## Development Guide

### Common Tasks

| Task | Files to Modify | Pattern Doc |
|------|-----------------|-------------|
| Add Entity | `Domain/Entities/`, `Infrastructure/Persistence/Configurations/` | [Entity Configuration](backend/patterns/entity-configuration.md) |
| Add Command | `Application/Features/<Feature>/Commands/` | Feature structure |
| Add Query | `Application/Features/<Feature>/Queries/` | Feature structure |
| Add Handler | `Infrastructure/Identity/Handlers/` | CQRS pattern |
| Add Endpoint | `Web/Endpoints/` | Minimal API |
| Add Specification | `Application/Specifications/` | [Repository & Specification](backend/patterns/repository-specification.md) |
| Add Service | Add marker interface (`IScopedService`, etc.) | [DI Auto-Registration](backend/patterns/di-auto-registration.md) |

### Critical Rules

1. **Use Specifications** for all database queries - never raw `DbSet`
2. **Tag all specifications** with `TagWith("MethodName")` for SQL debugging
3. **Use IUnitOfWork** for persistence - repository methods do NOT auto-save
4. **Use AsTracking** for mutation specs - default is `AsNoTracking`
5. **Co-locate Command + Handler + Validator** - all in `Application/Features/{Feature}/Commands/{Action}/`
6. **Soft delete only** - never hard delete (except GDPR)
7. **Marker interfaces** for DI auto-registration
8. **No using statements** in files - add to `GlobalUsings.cs`

### Performance Rules

| Scenario | Use |
|----------|-----|
| Read-only queries | `AsNoTracking` (default) |
| Multiple collections | `.AsSplitQuery()` |
| Bulk operations (1000+) | [Bulk extension methods](backend/patterns/bulk-operations.md) |

---

## Testing

**Path:** `tests/`

### Test Projects

| Project | Tests | Purpose |
|---------|-------|---------|
| `NOIR.Domain.UnitTests` | 400+ | Domain entity tests |
| `NOIR.Application.UnitTests` | 900+ | Handler, specification tests |
| `NOIR.ArchitectureTests` | 30+ | Dependency constraints |
| `NOIR.IntegrationTests` | 400+ | API integration tests |
| **Total** | **1,800+** | |

### Test Patterns

```csharp
// Handler test
[Fact]
public async Task Handle_ValidCommand_ReturnsSuccess()
{
    // Arrange
    var command = new CreateUserCommand("test@example.com", "password");

    // Act
    var result = await CreateUserHandler.Handle(command, _mockRepo.Object, CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
}
```

### Running Tests

```bash
# All tests
dotnet test src/NOIR.sln

# Specific project
dotnet test tests/NOIR.Domain.UnitTests

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

---

## Documentation Map

### Backend Documentation

| Document | Path | Description |
|----------|------|-------------|
| Backend README | `docs/backend/README.md` | Overview and quick start |
| Repository & Specification | `docs/backend/patterns/repository-specification.md` | Data access patterns |
| DI Auto-Registration | `docs/backend/patterns/di-auto-registration.md` | Service registration |
| Entity Configuration | `docs/backend/patterns/entity-configuration.md` | EF Core setup |
| JWT Refresh Tokens | `docs/backend/patterns/jwt-refresh-token.md` | Token rotation |
| Audit Logging | `docs/backend/patterns/hierarchical-audit-logging.md` | 3-level audit |
| Bulk Operations | `docs/backend/patterns/bulk-operations.md` | High-volume data |

### Research Documents

| Document | Path | Description |
|----------|------|-------------|
| Audit Logging Comparison | `docs/backend/research/hierarchical-audit-logging-comparison-2025.md` | Technology comparison |
| Role & Permission Systems | `docs/backend/research/role-permission-best-practices-2025.md` | Best practices |
| IUnitOfWork & EF Core | `docs/backend/research/research_iunitofwork_efcore_best_practices_20260103.md` | Persistence patterns |

### Frontend Documentation

| Document | Path | Description |
|----------|------|-------------|
| Frontend README | `docs/frontend/README.md` | Overview and setup |
| Architecture | `docs/frontend/architecture.md` | Project structure |
| Theme | `docs/frontend/theme.md` | Theming guide |
| API Types | `docs/frontend/api-types.md` | Type generation |
| Localization | `docs/frontend/localization-guide.md` | i18n setup |
| Color Schema | `docs/frontend/COLOR_SCHEMA_GUIDE.md` | Color guidelines |

### Architecture Decisions

| ADR | Path | Description |
|-----|------|-------------|
| 001 | `docs/decisions/001-tech-stack.md` | Technology selection |
| 002 | `docs/decisions/002-frontend-ui-stack.md` | Frontend UI choices |

### AI Instructions

| Document | Path | Description |
|----------|------|-------------|
| CLAUDE.md | `CLAUDE.md` | Claude Code specific instructions |
| AGENTS.md | `AGENTS.md` | Universal AI agent guidelines |

---

## Quick Reference

### Commands

```bash
# Build & Run
dotnet build src/NOIR.sln
dotnet run --project src/NOIR.Web
dotnet watch --project src/NOIR.Web

# Tests
dotnet test src/NOIR.sln

# Frontend
cd src/NOIR.Web/frontend
npm install && npm run dev
npm run generate:api  # Sync types from backend

# Database
dotnet ef migrations add NAME --project src/NOIR.Infrastructure --startup-project src/NOIR.Web

# Docker
docker-compose up -d  # Start SQL Server + MailHog
```

### URLs

| URL | Purpose |
|-----|---------|
| `http://localhost:3000` | Application (frontend + API via proxy) |
| `http://localhost:3000/api/docs` | API documentation (Scalar) |
| `http://localhost:3000/hangfire` | Background jobs dashboard |
| `http://localhost:4000` | Backend only (production-like) |

### Default Credentials

- **Email:** `admin@noir.local`
- **Password:** `123qwe`

---

*Updated: 2026-01-15 | Total Tests: 1,800+ | Features: 8 | Endpoints: 11 | Entities: 16*
