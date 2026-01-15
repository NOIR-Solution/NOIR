# Role & Permission Management Research Report

> Deep research on best practices for RBAC/ABAC implementations in multi-tenant SaaS applications, with specific recommendations for NOIR.

**Date:** January 14, 2026
**Confidence Level:** High (based on multiple authoritative sources)

---

## Executive Summary

NOIR already has a **solid foundation** for role and permission management. The current implementation uses:
- Database-backed permissions with `resource:action:scope` format
- Many-to-many Role-Permission relationships via `RolePermission` entity
- Multi-tenant roles via `TenantRole` enum (Viewer → Member → Admin → Owner)
- Permission-based authorization with `[HasPermission]` attributes

The research recommends **enhancing rather than replacing** the existing system with:
1. **Role Hierarchy/Inheritance** for reduced redundancy
2. **Permission Templates** for quick role setup
3. **Tenant-scoped Custom Roles** for flexibility
4. **Enhanced UI patterns** for admin dashboard

---

## Part 1: Authorization Model Comparison

### 1.1 RBAC (Role-Based Access Control)

**Best For:** Systems with clear organizational roles

| Pros | Cons |
|------|------|
| Simple to implement | "Role explosion" in complex systems |
| Fast performance (O(1) lookup) | Limited context-awareness |
| Easy to audit | Cannot handle dynamic rules |
| Familiar mental model | Inflexible for edge cases |

**Current NOIR Status:** ✅ Implemented via ASP.NET Identity + custom Permission entity

### 1.2 ABAC (Attribute-Based Access Control)

**Best For:** Complex, context-dependent access rules

| Pros | Cons |
|------|------|
| Extremely fine-grained | Complex to implement |
| Dynamic & context-aware | Performance overhead |
| Policy-driven | Harder to audit |
| Handles edge cases well | Steeper learning curve |

**Current NOIR Status:** ⚠️ Partially implemented via `Scope` field ("own", "team", "all")

### 1.3 ReBAC (Relationship-Based Access Control)

**Best For:** Hierarchical/ownership scenarios (like Google Drive)

| Pros | Cons |
|------|------|
| Natural for hierarchies | Implementation complexity |
| Supports reverse queries | Graph traversal overhead |
| Handles ownership elegantly | Harder to audit |
| Scales well for nested resources | Requires relationship storage |

**Current NOIR Status:** ✅ Implemented via `ResourceShare` entity with `SharePermission`

### 1.4 Recommended Approach: Hybrid Model

Based on research from [Permit.io](https://www.permit.io/blog/rbac-vs-abac-vs-rebac), [Oso](https://www.osohq.com/learn/rbac-vs-abac-vs-rebac-what-is-the-best-access-policy-paradigm), and [Aserto](https://www.aserto.com/blog/rbac-abac-and-rebac-differences-and-scenarios):

```
┌─────────────────────────────────────────────────────────────────┐
│                     NOIR Authorization Stack                      │
├─────────────────────────────────────────────────────────────────┤
│  Layer 1: RBAC Foundation                                        │
│  - System roles (Admin, User) with permission sets               │
│  - Tenant roles (Owner, Admin, Member, Viewer)                   │
├─────────────────────────────────────────────────────────────────┤
│  Layer 2: Permission-Based (Fine-Grained RBAC)                   │
│  - resource:action format for granular control                   │
│  - Custom roles with specific permission combinations            │
├─────────────────────────────────────────────────────────────────┤
│  Layer 3: Scope/ABAC (Context-Aware)                             │
│  - resource:action:scope (own/team/all)                          │
│  - Attribute-based rules for special cases                       │
├─────────────────────────────────────────────────────────────────┤
│  Layer 4: ReBAC (Resource Relationships)                         │
│  - ResourceShare for document/resource sharing                   │
│  - Ownership inheritance for nested resources                    │
└─────────────────────────────────────────────────────────────────┘
```

---

## Part 2: Key Libraries & Tools Evaluated

### 2.1 .NET Authorization Libraries

| Library | Stars | Description | Recommendation |
|---------|-------|-------------|----------------|
| [Casbin.NET](https://github.com/casbin/Casbin.NET) | 1.2k+ | ACL/RBAC/ABAC via policy files | Consider for complex policies |
| [Gatekeeper](https://github.com/jchristn/Gatekeeper) | 100+ | Lightweight RBAC | Too simple for NOIR |
| [OpenFGA (.NET SDK)](https://openfga.dev/) | 3k+ | Zanzibar-inspired ReBAC | Consider for advanced ReBAC |
| ASP.NET Core Policies | Built-in | Native policy-based auth | ✅ Already using |

**Verdict:** NOIR's current approach (custom Permission entity + ASP.NET policies) is appropriate. Consider OpenFGA only if ReBAC requirements grow significantly.

### 2.2 Multi-Tenant Patterns

From [AuthPermissions](https://www.thereformedprogrammer.net/building-asp-net-core-and-ef-core-multi-tenant-apps-part1-the-database/) research:

| Pattern | Description | NOIR Status |
|---------|-------------|-------------|
| Tenant-Scoped Roles | Roles belong to specific tenants | ❌ Not implemented |
| Role Inheritance | Roles can inherit from parent roles | ❌ Not implemented |
| Permission Templates | Pre-defined permission sets | ⚠️ Partial (Groups class) |
| User-Tenant Membership | Many-to-many user-tenant with roles | ✅ Implemented |

---

## Part 3: Current NOIR Architecture Analysis

### 3.1 Strengths

1. **Well-structured Permission entity** with Resource/Action/Scope/Category
2. **Audit tracking** on RolePermission join entity
3. **System permission protection** via `IsSystem` flag
4. **Clean API design** with proper endpoint separation
5. **Multi-tenant foundation** via `TenantRole` and `UserTenantMembership`

### 3.2 Gaps Identified

| Gap | Impact | Priority |
|-----|--------|----------|
| No role inheritance/hierarchy | Role redundancy, harder maintenance | High |
| Roles are global (not tenant-scoped) | Less flexibility for tenants | Medium |
| No permission templates/presets | Slower role creation | Medium |
| Missing Permission CRUD endpoints | Cannot add custom permissions | High |
| No UI for permission matrix | Admin experience limitation | Medium |

### 3.3 Current Entity Relationships

```
┌──────────────┐     ┌─────────────────┐     ┌────────────┐
│   User       │────▶│  RolePermission │◀────│    Role    │
│ (Identity)   │     │   (Join Table)  │     │ (Identity) │
└──────────────┘     └────────┬────────┘     └────────────┘
                              │
                              ▼
                     ┌────────────────┐
                     │   Permission   │
                     │ (Domain Entity)│
                     └────────────────┘

┌──────────────┐     ┌─────────────────────┐     ┌────────────┐
│   User       │────▶│ UserTenantMembership│◀────│   Tenant   │
│ (Identity)   │     │    (TenantRole)     │     │  (Domain)  │
└──────────────┘     └─────────────────────┘     └────────────┘
```

---

## Part 4: Recommendations

### 4.1 High Priority: Role Hierarchy & Inheritance

**Problem:** Without inheritance, "Admin" must explicitly list all permissions that "Member" also has.

**Solution:** Add `ParentRoleId` to enable role inheritance.

```csharp
// New: Role entity (if moving away from Identity's IdentityRole)
public class ApplicationRole : IdentityRole
{
    public string? ParentRoleId { get; set; }
    public ApplicationRole? ParentRole { get; set; }
    public ICollection<ApplicationRole> ChildRoles { get; set; }

    // For tenant-scoped roles
    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }

    public bool IsSystemRole { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}
```

**Permission Resolution Algorithm:**
```csharp
public async Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(string roleId)
{
    var permissions = new HashSet<string>();
    var visited = new HashSet<string>();

    await CollectPermissionsRecursive(roleId, permissions, visited);

    return permissions;
}

private async Task CollectPermissionsRecursive(
    string roleId,
    HashSet<string> permissions,
    HashSet<string> visited)
{
    if (!visited.Add(roleId)) return; // Prevent cycles

    var role = await _roleManager.FindByIdAsync(roleId);
    if (role == null) return;

    // Get direct permissions
    var directPermissions = await GetRolePermissionsAsync(roleId);
    permissions.UnionWith(directPermissions);

    // Recurse to parent
    if (role.ParentRoleId != null)
    {
        await CollectPermissionsRecursive(role.ParentRoleId, permissions, visited);
    }
}
```

### 4.2 High Priority: Permission CRUD Operations

**Add new endpoints and commands:**

```
POST   /api/permissions              - Create custom permission
GET    /api/permissions              - List all permissions (paginated, filterable)
GET    /api/permissions/{id}         - Get permission by ID
PUT    /api/permissions/{id}         - Update permission metadata
DELETE /api/permissions/{id}         - Delete non-system permission
GET    /api/permissions/categories   - Get grouped by category
```

**Permission entity enhancements:**
```csharp
public class Permission : Entity<Guid>
{
    // Existing fields...

    // New: Tenant scoping for custom permissions
    public Guid? TenantId { get; set; }  // null = global/system permission

    // New: For UI display
    public string? IconName { get; set; }
    public string? HelpText { get; set; }

    // New: Dependencies
    public ICollection<PermissionDependency> Dependencies { get; set; }
}

// New: Permission dependencies (e.g., "delete" requires "read")
public class PermissionDependency
{
    public Guid PermissionId { get; set; }
    public Guid RequiredPermissionId { get; set; }
}
```

### 4.3 Medium Priority: Permission Templates

**Create pre-defined permission sets for quick role creation:**

```csharp
public class PermissionTemplate : Entity<Guid>
{
    public string Name { get; set; }  // e.g., "Content Manager", "Read-Only User"
    public string Description { get; set; }
    public Guid? TenantId { get; set; }  // null = system template
    public bool IsSystem { get; set; }

    public ICollection<PermissionTemplateItem> Items { get; set; }
}

public class PermissionTemplateItem
{
    public Guid TemplateId { get; set; }
    public Guid PermissionId { get; set; }
}
```

**Default Templates:**
| Template | Description | Permissions |
|----------|-------------|-------------|
| Administrator | Full system access | All permissions |
| Content Manager | Manage content resources | CRUD on content entities |
| Read-Only | View-only access | All `:read` permissions |
| User Manager | Manage users only | users:*, roles:read |

### 4.4 Medium Priority: Tenant-Scoped Custom Roles

**Allow tenants to create their own roles:**

```csharp
// Enhanced Role DTO
public record RoleDto(
    string Id,
    string Name,
    string? Description,
    string? ParentRoleId,
    Guid? TenantId,           // null = system role
    bool IsSystemRole,
    int UserCount,
    IReadOnlyList<string> DirectPermissions,
    IReadOnlyList<string> InheritedPermissions,
    IReadOnlyList<string> EffectivePermissions  // Combined
);
```

**Business Rules:**
1. System roles (TenantId = null) can only be modified by system admins
2. Tenant roles can only be managed by tenant admins/owners
3. Tenant roles cannot exceed parent tenant's permission scope
4. Child tenants cannot have more permissions than parent tenant

### 4.5 UI/UX Recommendations

Based on [React-admin RBAC](https://marmelab.com/react-admin/AuthRBAC.html) and [Permit.io](https://www.permit.io/blog/implementing-react-rbac-authorization) patterns:

#### Permission Matrix View
```
                 │ users │ roles │ orders │ reports │
─────────────────┼───────┼───────┼────────┼─────────┤
Admin            │ CRUD  │ CRUD  │ CRUD   │ CRUD    │
Manager          │ RU    │ R     │ CRUD   │ R       │
User             │ R(own)│ -     │ CRU    │ R       │
Viewer           │ -     │ -     │ R      │ R       │
```

#### Component Patterns
1. **Permission Picker** - Multi-select grouped by category
2. **Role Builder** - Drag-drop permissions with template starting points
3. **Effective Permissions View** - Shows inherited + direct
4. **Permission Diff** - Compare two roles side-by-side
5. **User Permission Preview** - Show what a specific user can do

#### Conditional Rendering Hook
```typescript
// Frontend pattern
const { hasPermission, hasAnyPermission, hasAllPermissions } = usePermissions();

// Usage
{hasPermission('users:delete') && <DeleteButton />}
{hasAnyPermission(['reports:read', 'reports:export']) && <ReportsLink />}
```

---

## Part 5: Database Schema Enhancements

### 5.1 Proposed Schema Changes

```sql
-- Enhance existing Role (or create ApplicationRole)
ALTER TABLE AspNetRoles ADD COLUMN ParentRoleId NVARCHAR(450) NULL;
ALTER TABLE AspNetRoles ADD COLUMN TenantId UNIQUEIDENTIFIER NULL;
ALTER TABLE AspNetRoles ADD COLUMN Description NVARCHAR(500) NULL;
ALTER TABLE AspNetRoles ADD COLUMN IsSystemRole BIT NOT NULL DEFAULT 0;
ALTER TABLE AspNetRoles ADD COLUMN SortOrder INT NOT NULL DEFAULT 0;

-- Add FK
ALTER TABLE AspNetRoles
ADD CONSTRAINT FK_Roles_ParentRole
FOREIGN KEY (ParentRoleId) REFERENCES AspNetRoles(Id);

-- Permission Templates
CREATE TABLE PermissionTemplates (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    TenantId UNIQUEIDENTIFIER NULL,
    IsSystem BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIMEOFFSET NOT NULL,
    ModifiedAt DATETIMEOFFSET NOT NULL
);

CREATE TABLE PermissionTemplateItems (
    TemplateId UNIQUEIDENTIFIER NOT NULL,
    PermissionId UNIQUEIDENTIFIER NOT NULL,
    PRIMARY KEY (TemplateId, PermissionId),
    FOREIGN KEY (TemplateId) REFERENCES PermissionTemplates(Id),
    FOREIGN KEY (PermissionId) REFERENCES Permissions(Id)
);

-- Permission Dependencies
CREATE TABLE PermissionDependencies (
    PermissionId UNIQUEIDENTIFIER NOT NULL,
    RequiredPermissionId UNIQUEIDENTIFIER NOT NULL,
    PRIMARY KEY (PermissionId, RequiredPermissionId),
    FOREIGN KEY (PermissionId) REFERENCES Permissions(Id),
    FOREIGN KEY (RequiredPermissionId) REFERENCES Permissions(Id)
);
```

### 5.2 Indexing Strategy

```sql
-- For permission lookups
CREATE INDEX IX_Permissions_Resource_Action ON Permissions(Resource, Action);
CREATE INDEX IX_Permissions_Category ON Permissions(Category);
CREATE INDEX IX_Permissions_TenantId ON Permissions(TenantId);

-- For role hierarchy traversal
CREATE INDEX IX_Roles_ParentRoleId ON AspNetRoles(ParentRoleId);
CREATE INDEX IX_Roles_TenantId ON AspNetRoles(TenantId);

-- For efficient permission caching
CREATE INDEX IX_RolePermissions_RoleId ON RolePermissions(RoleId) INCLUDE (PermissionId);
```

---

## Part 6: Implementation Roadmap

### Phase 1: Core Enhancements (1-2 sprints)
- [ ] Add Permission CRUD endpoints
- [ ] Add role hierarchy (ParentRoleId)
- [ ] Update permission resolution to include inheritance
- [ ] Add permission caching with hierarchy support

### Phase 2: Templates & UI (1 sprint)
- [ ] Implement PermissionTemplate entity
- [ ] Create default templates (Admin, Manager, User, Viewer)
- [ ] Build React permission matrix component
- [ ] Add role builder with template selection

### Phase 3: Tenant Customization (1 sprint)
- [ ] Add TenantId to roles for tenant-scoped roles
- [ ] Implement permission scope validation
- [ ] Add tenant admin UI for custom role creation
- [ ] Add permission inheritance limits per tenant tier

### Phase 4: Advanced Features (Future)
- [ ] Permission dependencies (auto-grant required permissions)
- [ ] Time-based permission grants
- [ ] Permission request/approval workflow
- [ ] Audit log for permission changes

---

## Sources

### Authorization Models
- [RBAC vs ABAC vs ReBAC - Permit.io](https://www.permit.io/blog/rbac-vs-abac-vs-rebac)
- [Authorization Policy Comparison - Oso](https://www.osohq.com/learn/rbac-vs-abac-vs-rebac-what-is-the-best-access-policy-paradigm)
- [RBAC vs ReBAC - Aserto](https://www.aserto.com/blog/rbac-vs-rebac)
- [Choosing Authorization Models - Pangea](https://pangea.cloud/blog/rbac-vs-rebac-vs-abac/)

### .NET Implementation
- [Casbin.NET - GitHub](https://github.com/casbin/Casbin.NET)
- [OpenFGA - Auth0](https://openfga.dev/)
- [ASP.NET Core Policy-Based Auth - Microsoft](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies)
- [AuthPermissions Library - Jon P Smith](https://www.thereformedprogrammer.net/a-better-way-to-handle-authorization-in-asp-net-core/)

### Multi-Tenant Patterns
- [Multi-Tenant SaaS RBAC - Medium](https://medium.com/@my_journey_to_be_an_architect/building-role-based-access-control-for-a-multi-tenant-saas-startup-26b89d603fdb)
- [ASP.NET Identity Multi-Tenant - DEV](https://dev.to/luqman_bolajoko/implementing-aspnet-identity-for-a-multi-tenant-application-best-practices-4an6)

### Database Design
- [Permission Schema Design - ZigPoll](https://www.zigpoll.com/content/what-database-schema-do-you-recommend-using-to-efficiently-manage-owner-permissions-and-roles-across-multiple-projects)
- [RBAC Fundamentals - CelerData](https://celerdata.com/glossary/role-based-access-control-rbac)

### UI/UX Patterns
- [React RBAC Implementation - Permit.io](https://www.permit.io/blog/implementing-react-rbac-authorization)
- [React-admin Authorization - Marmelab](https://marmelab.com/react-admin/AuthRBAC.html)
- [React Permission Rendering - Medium](https://medium.com/geekculture/how-to-conditionally-render-react-ui-based-on-user-permissions-7b9a1c73ffe2)
