# NOIR Domain Entities

## Location
`src/NOIR.Domain/Entities/`

## Core Entities

### Permission
Database-backed authorization with format `resource:action:scope`:
```csharp
public class Permission : Entity<Guid>
{
    public string Resource { get; private set; }  // e.g., "orders", "users"
    public string Action { get; private set; }     // e.g., "create", "read", "delete"
    public string? Scope { get; private set; }     // e.g., "own", "team", "all"
    public string DisplayName { get; private set; }
    public string? Description { get; private set; }
    public string? Category { get; private set; }
    public bool IsSystem { get; private set; }     // Cannot be deleted
    public string Name => $"{Resource}:{Action}:{Scope}";
}
```

### RolePermission
Join entity for Role-Permission many-to-many:
```csharp
public class RolePermission : Entity<Guid>, IAuditableEntity
{
    public string RoleId { get; private set; }
    public Guid PermissionId { get; private set; }
    public Permission Permission { get; private set; }
}
```

### RefreshToken
JWT refresh token tracking:
- Token hash storage (not plain text)
- Family-based rotation detection
- Device/IP tracking

### Audit Entities (3-Level Audit)

1. **HttpRequestAuditLog** - HTTP request/response logging
2. **HandlerAuditLog** - CQRS command/query execution
3. **EntityAuditLog** - EF Core entity changes (before/after diff)

### ResourceShare
Multi-user resource sharing support.

### Notification
User notification system:
- Title, message, type (info, success, warning, error)
- Read/unread status tracking
- Optional action URL for navigation
- Soft delete support

### NotificationPreference
User notification preferences by category:
- Email and in-app channel toggles
- Category-based configuration

### EmailTemplate
Customizable email templates:
- Subject and HTML body with placeholders
- Template key for lookup
- System flag for protected templates

### Tenant
Multi-tenant configuration (Finbuckle):
- Identifier and name
- Connection string (optional)
- Active/inactive status

### Multi-Tenancy Entities

#### UserTenantMembership
Platform-level entity enabling users to belong to multiple tenants:
```csharp
public class UserTenantMembership : Entity<Guid>
{
    public Guid UserId { get; }         // User reference
    public Guid TenantId { get; }       // Tenant reference
    public TenantRole Role { get; }     // Owner, Admin, Member, Viewer
    public bool IsDefault { get; }      // User's default tenant
    public DateTimeOffset JoinedAt { get; }
}
```

| Role | Permissions |
|------|-------------|
| Owner | Full control, can delete tenant |
| Admin | Manage users and settings |
| Member | Standard access |
| Viewer | Read-only access |

#### TenantBranding
Tenant customization (logo, colors, etc.)

#### TenantDomain
Custom domains for tenant access.

#### TenantSetting
Tenant-specific configuration settings.

### Authentication Entities

#### EmailChangeOtp
OTP verification for email change requests.

#### PasswordResetOtp
OTP verification for password reset flow.

## Entity Base Classes

### Entity<TId>
```csharp
public abstract class Entity<TId>
{
    public TId Id { get; protected set; }
    public DateTimeOffset CreatedAt { get; protected set; }
    public DateTimeOffset? ModifiedAt { get; protected set; }
}
```

### AggregateRoot<TId>
Extends Entity with IAuditableEntity (full audit + soft delete).

## Entity Creation Pattern
Use static factory methods:
```csharp
public static Permission Create(
    string resource,
    string action,
    string displayName,
    string? scope = null)
{
    return new Permission
    {
        Id = Guid.NewGuid(),
        Resource = resource.ToLowerInvariant(),
        // ...
    };
}
```

## Naming Convention
- Private setters (encapsulation)
- Private constructor for EF Core
- Public static `Create()` factory method
- Public `Update()` methods for modifications
