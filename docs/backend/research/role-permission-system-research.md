# Role & Permission System Research

> **Comprehensive research on authorization patterns for multi-tenant SaaS applications.**

**Last Updated:** 2026-02-01
**Version:** 1.0 (Consolidated)
**Status:** Research Complete

---

## Executive Summary

NOIR's current permission system implements **database-backed RBAC** with tenant-level isolation via Finbuckle. This research analyzes industry best practices and provides two implementation paths:

1. **Path A: Quick Enhancement** - Enhance existing RBAC with role hierarchy, permission templates, and tenant-scoped custom roles (Lower effort, faster delivery)
2. **Path B: Full Evolution** - Evolve to Hybrid RBAC + ReBAC for project-based SaaS with hierarchical resources (Higher effort, future-proof)

**Recommendation:** Start with Path A for immediate improvements, design architecture to support Path B migration if complexity grows.

---

## Table of Contents

- [Part 1: Authorization Model Comparison](#part-1-authorization-model-comparison)
- [Part 2: Industry Patterns](#part-2-industry-patterns)
- [Part 3: Current NOIR Analysis](#part-3-current-noir-analysis)
- [Part 4: Path A - Quick Enhancement](#part-4-path-a---quick-enhancement)
- [Part 5: Path B - Full Evolution](#part-5-path-b---full-evolution)
- [Part 6: Implementation Roadmap](#part-6-implementation-roadmap)
- [Part 7: Sources](#part-7-sources)

---

## Part 1: Authorization Model Comparison

### 1.1 Model Overview

| Model | Description | Use Case | Complexity |
|-------|-------------|----------|------------|
| **RBAC** | Roles → Permissions | Simple apps with stable roles | Low |
| **ABAC** | Attributes drive access decisions | Dynamic context-dependent access | High |
| **ReBAC** | Relationships define permissions | Hierarchical, collaborative apps | Medium-High |
| **Hybrid** | RBAC + ReBAC/ABAC combinations | Enterprise SaaS | Medium-High |

### 1.2 RBAC (Role-Based Access Control)

**Best For:** Systems with clear organizational roles

| Pros | Cons |
|------|------|
| Simple to implement | "Role explosion" in complex systems |
| Fast performance (O(1) lookup) | Limited context-awareness |
| Easy to audit | Cannot handle dynamic rules |
| Familiar mental model | Inflexible for edge cases |

**Current NOIR Status:** ✅ Implemented via ASP.NET Identity + custom Permission entity

### 1.3 ABAC (Attribute-Based Access Control)

**Best For:** Complex, context-dependent access rules

| Pros | Cons |
|------|------|
| Extremely fine-grained | Complex to implement |
| Dynamic & context-aware | Performance overhead |
| Policy-driven | Harder to audit |
| Handles edge cases well | Steeper learning curve |

**Current NOIR Status:** ⚠️ Partially implemented via `Scope` field ("own", "team", "all")

### 1.4 ReBAC (Relationship-Based Access Control)

**Best For:** Hierarchical/ownership scenarios (like Google Drive)

| Pros | Cons |
|------|------|
| Natural for hierarchies | Implementation complexity |
| Supports reverse queries | Graph traversal overhead |
| Handles ownership elegantly | Harder to audit |
| Scales well for nested resources | Requires relationship storage |

**Current NOIR Status:** ✅ Implemented via `ResourceShare` entity with `SharePermission`

### 1.5 Recommended: Layered Hybrid Model

```
┌─────────────────────────────────────────────────────────────────┐
│                     NOIR Authorization Stack                     │
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
│  Layer 4: ReBAC (Resource Relationships) [Optional]              │
│  - ResourceShare for document/resource sharing                   │
│  - Ownership inheritance for nested resources                    │
└─────────────────────────────────────────────────────────────────┘
```

---

## Part 2: Industry Patterns

### 2.1 Google Zanzibar (ReBAC Foundation)

- Stores relationships as tuples: `(user, relation, object)`
- Supports hierarchical permission inheritance
- Graph traversal for permission checks
- Used by: Google Drive, YouTube, Gmail, Google Cloud IAM

**Example:**
```
document:budget.xlsx#owner@user:alice
folder:finance#parent@document:budget.xlsx
folder:finance#viewer@team:accounting#member
```

### 2.2 Multi-Tenant RBAC (AWS/Azure Pattern)

- **Tenant isolation** is primary concern
- Roles scoped to tenants, not global
- Same role can have different permissions per tenant
- Template-based role creation across tenants

**Architecture Components:**
1. **Policy Administration Point (PAP)**: Where policies are stored/managed
2. **Policy Decision Point (PDP)**: Where policies are evaluated
3. **Policy Enforcement Point (PEP)**: Where decisions are enforced

### 2.3 Modern SaaS Permission Requirements

| Requirement | Description | NOIR Status |
|-------------|-------------|-------------|
| **Tenant Isolation** | Data strictly separated by tenant | ✅ Finbuckle |
| **Resource Hierarchies** | Org → Project → Team → Resource | ❌ Not implemented |
| **Permission Inheritance** | Parent permissions flow to children | ❌ Not implemented |
| **Scoped Roles** | Same user, different roles per context | ⚠️ Partial |
| **Ownership-Based Access** | Creator/owner gets special permissions | ✅ ResourceShare |
| **Collaborative Access** | Share resources with specific users | ✅ ResourceShare |
| **Audit Trail** | Who granted what, when | ✅ RolePermission audit |

### 2.4 .NET Authorization Libraries Evaluated

| Library | Stars | Description | Recommendation |
|---------|-------|-------------|----------------|
| [Casbin.NET](https://github.com/casbin/Casbin.NET) | 1.2k+ | ACL/RBAC/ABAC via policy files | Consider for complex policies |
| [OpenFGA](https://openfga.dev/) | 3k+ | Zanzibar-inspired ReBAC | Consider for advanced ReBAC |
| ASP.NET Core Policies | Built-in | Native policy-based auth | ✅ Already using |

**Verdict:** NOIR's current approach (custom Permission entity + ASP.NET policies) is appropriate. Consider OpenFGA only if ReBAC requirements grow significantly.

---

## Part 3: Current NOIR Analysis

### 3.1 Current Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        NOIR Current                         │
├─────────────────────────────────────────────────────────────┤
│  Tenant (via Finbuckle)                                     │
│  └── Users (ApplicationUser with TenantId)                  │
│      └── Roles (ASP.NET Identity IdentityRole - GLOBAL)    │
│          └── Permissions (RolePermission → Permission)      │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 Existing Components

| Component | Location | Status |
|-----------|----------|--------|
| `Permission` entity | `Domain/Entities/Permission.cs` | ✅ Well-designed |
| `RolePermission` join | `Domain/Entities/Permission.cs` | ✅ Basic implementation |
| `Permissions` constants | `Domain/Common/Permissions.cs` | ⚠️ Global only |
| `HasPermissionAttribute` | `Infrastructure/Identity/Authorization/` | ✅ Works |
| `PermissionAuthorizationHandler` | `Infrastructure/Identity/Authorization/` | ⚠️ No resource scoping |
| `TenantIdSetterInterceptor` | `Infrastructure/Persistence/Interceptors/` | ✅ Auto tenant tagging |

### 3.3 Strengths

1. **Well-structured Permission entity** with Resource/Action/Scope/Category
2. **Audit tracking** on RolePermission join entity
3. **System permission protection** via `IsSystem` flag
4. **Multi-tenant foundation** via `TenantRole` and `UserTenantMembership`
5. **Database-backed permissions** (not hardcoded)
6. **Permission caching** (5-minute sliding expiration)

### 3.4 Gaps Identified

| Gap | Impact | Path A | Path B |
|-----|--------|--------|--------|
| No role inheritance/hierarchy | Role redundancy | ✅ Addresses | ✅ Addresses |
| Roles are global (not tenant-scoped) | Less flexibility | ✅ Addresses | ✅ Addresses |
| No permission templates | Slower role creation | ✅ Addresses | ✅ Addresses |
| No Project/Team entities | Cannot scope to projects | ❌ | ✅ Addresses |
| No resource-based checks | Cannot verify ownership | ❌ | ✅ Addresses |
| No permission inheritance | No folder → file inherit | ❌ | ✅ Addresses |

---

## Part 4: Path A - Quick Enhancement

**Goal:** Enhance existing RBAC without major architectural changes.

### 4.1 Role Hierarchy & Inheritance

Add `ParentRoleId` to enable role inheritance:

```csharp
public class ApplicationRole : IdentityRole
{
    public string? ParentRoleId { get; set; }
    public ApplicationRole? ParentRole { get; set; }
    public ICollection<ApplicationRole> ChildRoles { get; set; }

    public Guid? TenantId { get; set; }  // For tenant-scoped roles
    public bool IsSystemRole { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}
```

**Permission Resolution:**
```csharp
public async Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(string roleId)
{
    var permissions = new HashSet<string>();
    var visited = new HashSet<string>();
    await CollectPermissionsRecursive(roleId, permissions, visited);
    return permissions;
}

private async Task CollectPermissionsRecursive(
    string roleId, HashSet<string> permissions, HashSet<string> visited)
{
    if (!visited.Add(roleId)) return; // Prevent cycles

    var role = await _roleManager.FindByIdAsync(roleId);
    if (role == null) return;

    var directPermissions = await GetRolePermissionsAsync(roleId);
    permissions.UnionWith(directPermissions);

    if (role.ParentRoleId != null)
        await CollectPermissionsRecursive(role.ParentRoleId, permissions, visited);
}
```

### 4.2 Permission Templates

Pre-defined permission sets for quick role creation:

```csharp
public class PermissionTemplate : Entity<Guid>
{
    public string Name { get; set; }  // e.g., "Content Manager"
    public string Description { get; set; }
    public Guid? TenantId { get; set; }  // null = system template
    public bool IsSystem { get; set; }
    public ICollection<PermissionTemplateItem> Items { get; set; }
}
```

**Default Templates:**

| Template | Description | Permissions |
|----------|-------------|-------------|
| Administrator | Full system access | All permissions |
| Content Manager | Manage content | CRUD on content entities |
| Read-Only | View-only access | All `:read` permissions |
| User Manager | Manage users only | users:*, roles:read |

### 4.3 Tenant-Scoped Custom Roles

Allow tenants to create their own roles:

```csharp
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
    IReadOnlyList<string> EffectivePermissions
);
```

**Business Rules:**
1. System roles (TenantId = null) can only be modified by system admins
2. Tenant roles can only be managed by tenant admins/owners
3. Tenant roles cannot exceed parent tenant's permission scope

### 4.4 Database Schema (Path A)

```sql
-- Enhance existing Role
ALTER TABLE AspNetRoles ADD ParentRoleId NVARCHAR(450) NULL;
ALTER TABLE AspNetRoles ADD TenantId UNIQUEIDENTIFIER NULL;
ALTER TABLE AspNetRoles ADD Description NVARCHAR(500) NULL;
ALTER TABLE AspNetRoles ADD IsSystemRole BIT NOT NULL DEFAULT 0;
ALTER TABLE AspNetRoles ADD SortOrder INT NOT NULL DEFAULT 0;

-- Permission Templates
CREATE TABLE PermissionTemplates (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    TenantId UNIQUEIDENTIFIER NULL,
    IsSystem BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIMEOFFSET NOT NULL
);

CREATE TABLE PermissionTemplateItems (
    TemplateId UNIQUEIDENTIFIER NOT NULL,
    PermissionId UNIQUEIDENTIFIER NOT NULL,
    PRIMARY KEY (TemplateId, PermissionId)
);

-- Indexes
CREATE INDEX IX_Roles_ParentRoleId ON AspNetRoles(ParentRoleId);
CREATE INDEX IX_Roles_TenantId ON AspNetRoles(TenantId);
```

---

## Part 5: Path B - Full Evolution

**Goal:** Evolve to Hybrid RBAC + ReBAC for project-based SaaS.

### 5.1 New Domain Entities

```csharp
public class Project : TenantAggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public ICollection<ProjectMember> Members { get; private set; }
    public ICollection<Team> Teams { get; private set; }
}

public class Team : TenantEntity<Guid>
{
    public Guid ProjectId { get; private set; }
    public string Name { get; private set; }
    public ICollection<TeamMember> Members { get; private set; }
}

public class ProjectMember : Entity<Guid>
{
    public Guid ProjectId { get; private set; }
    public string UserId { get; private set; }
    public Guid ProjectRoleId { get; private set; }  // Role within this project
}
```

### 5.2 Scoped Role System

```csharp
public class ScopedRole : TenantEntity<Guid>
{
    public string Name { get; private set; }
    public RoleScope Scope { get; private set; }  // Global, Tenant, Project, Team
    public Guid? ScopeEntityId { get; private set; }  // ProjectId or TeamId
    public bool IsDefault { get; private set; }
    public ICollection<ScopedRolePermission> Permissions { get; private set; }
}

public enum RoleScope
{
    Global,   // System-wide (super admin)
    Tenant,   // Tenant-wide roles
    Project,  // Project-specific roles
    Team      // Team-specific roles
}
```

### 5.3 Relationship Tuples (ReBAC)

Google Zanzibar-style relationship tuples:

```csharp
public class RelationshipTuple : Entity<long>
{
    public string SubjectType { get; private set; }  // "user", "team", "role"
    public string SubjectId { get; private set; }
    public string Relation { get; private set; }     // "owner", "editor", "viewer"
    public string ObjectType { get; private set; }   // "project", "document"
    public string ObjectId { get; private set; }
    public string? TenantId { get; private set; }
}
```

### 5.4 Resource-Based Authorization Handler

```csharp
public class ResourceAuthorizationHandler : AuthorizationHandler<PermissionRequirement, IResource>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement,
        IResource resource)
    {
        var userId = context.User.GetUserId();

        // Check direct permission
        if (await HasDirectPermission(userId, requirement.Permission, resource))
        {
            context.Succeed(requirement);
            return;
        }

        // Check inherited permission (from parent resources)
        if (await HasInheritedPermission(userId, requirement.Permission, resource))
        {
            context.Succeed(requirement);
            return;
        }

        // Check team/project membership
        if (await HasMembershipPermission(userId, requirement.Permission, resource))
        {
            context.Succeed(requirement);
            return;
        }
    }
}
```

### 5.5 Integration Options

| Option | Pros | Cons | Best For |
|--------|------|------|----------|
| **Build In-House** | Full control, no dependencies | More effort, custom maintenance | Specific requirements |
| **OpenFGA** | Production-proven, .NET SDK | External dependency, learning curve | Complex hierarchies |
| **SpiceDB** | True Zanzibar, strong consistency | Golang service, gRPC integration | Google-scale apps |

**Recommendation:** Build in-house for Phase 1-2, design for optional OpenFGA migration.

---

## Part 6: Implementation Roadmap

### Phase 1: Core Enhancements (Path A) - 2 sprints

| Task | Effort | Description |
|------|--------|-------------|
| Add role hierarchy (ParentRoleId) | 2 days | Enable role inheritance |
| Update permission resolution | 2 days | Include inheritance in lookups |
| Add PermissionTemplate entity | 2 days | Pre-defined permission sets |
| Create default templates | 1 day | Admin, Manager, User, Viewer |
| Add TenantId to roles | 1 day | Tenant-scoped custom roles |

### Phase 2: UI/UX Improvements - 1 sprint

| Task | Effort | Description |
|------|--------|-------------|
| Permission matrix component | 2 days | Visual role/permission grid |
| Role builder with templates | 2 days | Drag-drop permission assignment |
| Effective permissions view | 1 day | Show inherited + direct |

### Phase 3: Project-Based Access (Path B) - 2 sprints

| Task | Effort | Description |
|------|--------|-------------|
| Add Project/Team entities | 3 days | Core hierarchical entities |
| Add ScopedRole entity | 2 days | Context-scoped roles |
| Resource-based handler | 2 days | Check ownership/membership |
| Permission inheritance service | 2 days | Parent → child permission flow |

### Phase 4: Advanced Features (Future)

| Task | Effort | Description |
|------|--------|-------------|
| RelationshipTuple (ReBAC) | 3 days | Zanzibar-style tuples |
| Permission dependencies | 2 days | Auto-grant required permissions |
| External user sharing | 3 days | Cross-tenant access |
| Consider OpenFGA integration | Evaluate | External service option |

---

## Part 7: Sources

### Authorization Models
- [RBAC vs ABAC vs ReBAC - Permit.io](https://www.permit.io/blog/rbac-vs-abac-vs-rebac)
- [Authorization Policy Comparison - Oso](https://www.osohq.com/learn/rbac-vs-abac-vs-rebac-what-is-the-best-access-policy-paradigm)
- [RBAC vs ReBAC - Aserto](https://www.aserto.com/blog/rbac-vs-rebac)
- [Choosing Authorization Models - Pangea](https://pangea.cloud/blog/rbac-vs-rebac-vs-abac/)

### Google Zanzibar & ReBAC
- [Google Zanzibar Paper](https://research.google/pubs/zanzibar-googles-consistent-global-authorization-system/) - Google Research
- [How Google Drive Models Authorization](https://www.aserto.com/blog/google-zanzibar-drive-rebac-authorization-model) - Aserto
- [What is Google Zanzibar?](https://www.permit.io/blog/what-is-google-zanzibar) - Permit.io
- [Top 5 Zanzibar Implementations 2024](https://workos.com/blog/top-5-google-zanzibar-open-source-implementations-in-2024) - WorkOS

### .NET Implementation
- [Casbin.NET - GitHub](https://github.com/casbin/Casbin.NET)
- [OpenFGA - Auth0](https://openfga.dev/)
- [Fga.Net.AspNetCore](https://github.com/Hawxy/Fga.Net) - ASP.NET Core integration
- [ASP.NET Core Policy-Based Auth - Microsoft](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies)
- [Resource-Based Authorization - Microsoft](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resourcebased)
- [AuthPermissions Library - Jon P Smith](https://www.thereformedprogrammer.net/a-better-way-to-handle-authorization-in-asp-net-core/)

### Multi-Tenant SaaS
- [Best Practices for Multi-Tenant Authorization](https://www.permit.io/blog/best-practices-for-multi-tenant-authorization) - Permit.io
- [Multi-tenant RBAC](https://www.aserto.com/use-cases/multi-tenant-saas-rbac) - Aserto
- [AWS Multi-tenant Authorization Guide](https://docs.aws.amazon.com/prescriptive-guidance/latest/saas-multitenant-api-access-authorization/introduction.html)
- [Build Multi-Tenant SaaS Application](https://blog.logto.io/build-multi-tenant-saas-application) - Logto
- [B2B SaaS Identity Challenges](https://auth0.com/blog/b2b-saas-identity-challenges-granular-access-control/) - Auth0

### UI/UX Patterns
- [React RBAC Implementation - Permit.io](https://www.permit.io/blog/implementing-react-rbac-authorization)
- [React-admin Authorization - Marmelab](https://marmelab.com/react-admin/AuthRBAC.html)

---

## Changelog

### Version 1.0 (2026-02-01)
- Consolidated from two separate documents:
  - `role-permission-best-practices-2025.md` (2025-01-02)
  - `role-permission-management-research.md` (2026-01-14)
- Combined industry analysis with practical enhancement recommendations
- Presented two implementation paths (Quick Enhancement vs Full Evolution)
- Unified sources and references
