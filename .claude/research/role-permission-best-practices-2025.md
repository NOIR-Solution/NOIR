# Role & Permission System Best Practices Research

> Research Date: 2025-01-02
> Purpose: Deep analysis of permission system best practices for project-based multi-tenant SaaS

---

## Executive Summary

NOIR's current permission system implements **basic RBAC** with tenant-level isolation. While functional, it lacks the hierarchical and relationship-based capabilities needed for project-based SaaS applications. This research recommends evolving to a **Hybrid RBAC + ReBAC** model that can support organizational hierarchies (Tenant → Projects → Teams → Resources).

---

## Part 1: Industry Best Practices (2024-2025)

### 1.1 Authorization Model Comparison

| Model | Description | Use Case | Complexity |
|-------|-------------|----------|------------|
| **RBAC** | Roles → Permissions | Simple applications with stable roles | Low |
| **ABAC** | Attributes drive access decisions | Dynamic context-dependent access | High |
| **ReBAC** | Relationships define permissions | Hierarchical, collaborative apps | Medium-High |
| **Hybrid** | RBAC + ReBAC/ABAC combinations | Enterprise SaaS | Medium-High |

### 1.2 Key Industry Patterns

#### Google Zanzibar (ReBAC Foundation)
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
Alice owns the document, the document is in the finance folder, and accounting team members can view the finance folder (and thus the document).

#### Multi-Tenant RBAC (AWS/Azure Pattern)
- **Tenant isolation** is primary concern
- Roles scoped to tenants, not global
- Same role can have different permissions per tenant
- Template-based role creation across tenants

**Architecture Components:**
1. **Policy Administration Point (PAP)**: Where policies are stored/managed
2. **Policy Decision Point (PDP)**: Where policies are evaluated
3. **Policy Enforcement Point (PEP)**: Where decisions are enforced

### 1.3 Modern SaaS Permission Requirements

| Requirement | Description |
|-------------|-------------|
| **Tenant Isolation** | Data strictly separated by tenant |
| **Resource Hierarchies** | Org → Project → Team → Resource |
| **Permission Inheritance** | Parent permissions flow to children |
| **Scoped Roles** | Same user, different roles per context |
| **Ownership-Based Access** | Creator/owner gets special permissions |
| **Collaborative Access** | Share resources with specific users/teams |
| **Cross-Tenant Access** | External users with limited access |
| **Audit Trail** | Who granted what, when |

---

## Part 2: Current NOIR Implementation Analysis

### 2.1 Current Architecture

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

### 2.2 Existing Components

| Component | Location | Status |
|-----------|----------|--------|
| `Permission` entity | `Domain/Entities/Permission.cs` | ✅ Well-designed |
| `RolePermission` join | `Domain/Entities/Permission.cs` | ✅ Basic implementation |
| `Permissions` constants | `Domain/Common/Permissions.cs` | ⚠️ Global only |
| `Roles` constants | `Domain/Common/Roles.cs` | ⚠️ Only Admin/User |
| `HasPermissionAttribute` | `Infrastructure/Identity/Authorization/` | ✅ Works for global |
| `PermissionAuthorizationHandler` | `Infrastructure/Identity/Authorization/` | ⚠️ No resource scoping |
| `ITenantEntity` | `Domain/Common/ITenantEntity.cs` | ✅ Good foundation |
| `TenantIdSetterInterceptor` | `Infrastructure/Persistence/Interceptors/` | ✅ Auto tenant tagging |

### 2.3 What NOIR Does Well

1. **Tenant-level data isolation** via Finbuckle + `ITenantEntity`
2. **Permission entity design** with Resource:Action:Scope structure
3. **Database-backed permissions** (not hardcoded in code)
4. **Caching** of user permissions (5-minute sliding expiration)
5. **Audit tracking** on `RolePermission` changes
6. **Permission groups** for UI organization

### 2.4 Critical Gaps Identified

| Gap | Impact | Priority |
|-----|--------|----------|
| **No Project/Team entities** | Cannot scope permissions to projects | Critical |
| **Global roles only** | User has same role across all contexts | Critical |
| **No resource-based checks** | Cannot verify ownership/membership | High |
| **No permission inheritance** | Cannot inherit from folder → file | High |
| **No tenant-scoped roles** | Roles are shared across tenants | Medium |
| **No dynamic role templates** | Each tenant must define roles manually | Medium |
| **No relationship tuples** | Cannot express "user owns resource" | Medium |
| **No external user sharing** | Cannot share with users outside tenant | Low |

---

## Part 3: Recommended Changes

### 3.1 Domain Model Additions

#### 3.1.1 Core Entities Needed

```csharp
// New entities to add
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

public class TeamMember : Entity<Guid>
{
    public Guid TeamId { get; private set; }
    public string UserId { get; private set; }
    public Guid TeamRoleId { get; private set; }  // Role within this team
}
```

#### 3.1.2 Scoped Role System

```csharp
// Roles scoped to context
public class ScopedRole : TenantEntity<Guid>
{
    public string Name { get; private set; }
    public RoleScope Scope { get; private set; }  // Global, Tenant, Project, Team
    public Guid? ScopeEntityId { get; private set; }  // ProjectId or TeamId if scoped
    public bool IsDefault { get; private set; }  // Template role
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

#### 3.1.3 Relationship Tuples (ReBAC Foundation)

```csharp
// Google Zanzibar-style relationship tuples
public class RelationshipTuple : Entity<long>
{
    public string SubjectType { get; private set; }  // "user", "team", "role"
    public string SubjectId { get; private set; }
    public string Relation { get; private set; }     // "owner", "editor", "viewer", "member"
    public string ObjectType { get; private set; }   // "project", "document", "folder"
    public string ObjectId { get; private set; }
    public string? TenantId { get; private set; }

    // Example tuples:
    // (user:123, owner, project:456)
    // (team:789, viewer, document:abc)
    // (project:456, parent, document:abc)  -- for inheritance
}
```

### 3.2 Authorization Handler Changes

#### 3.2.1 Resource-Based Authorization Handler

```csharp
public class ResourceAuthorizationHandler : AuthorizationHandler<PermissionRequirement, IResource>
{
    private readonly IRelationshipService _relationships;

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

### 3.3 Permission Scope Extension

Update the existing `Permission` entity to support scoped checks:

```csharp
// Extended permission format: resource:action:scope
// Examples:
// - projects:read:own      → Can read projects they own
// - projects:read:team     → Can read projects their team is assigned to
// - projects:read:all      → Can read all projects (in tenant)
// - documents:edit:own     → Can edit documents they created
// - documents:edit:project → Can edit documents in projects they have access to
```

### 3.4 Database Schema Changes

```sql
-- New tables needed
CREATE TABLE Projects (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId NVARCHAR(64) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(2000),
    CreatedAt DATETIMEOFFSET NOT NULL,
    -- audit fields...
);

CREATE TABLE Teams (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId NVARCHAR(64) NOT NULL,
    ProjectId UNIQUEIDENTIFIER NOT NULL REFERENCES Projects(Id),
    Name NVARCHAR(200) NOT NULL,
    -- audit fields...
);

CREATE TABLE ProjectMembers (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    ProjectId UNIQUEIDENTIFIER NOT NULL REFERENCES Projects(Id),
    UserId NVARCHAR(450) NOT NULL,
    RoleId UNIQUEIDENTIFIER NOT NULL,  -- Scoped role
    -- audit fields...
);

CREATE TABLE ScopedRoles (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TenantId NVARCHAR(64),  -- NULL for global/system roles
    Name NVARCHAR(100) NOT NULL,
    Scope INT NOT NULL,  -- 0=Global, 1=Tenant, 2=Project, 3=Team
    ScopeEntityId UNIQUEIDENTIFIER,  -- ProjectId/TeamId for scoped roles
    IsDefault BIT NOT NULL DEFAULT 0,
    -- audit fields...
);

CREATE TABLE RelationshipTuples (
    Id BIGINT IDENTITY PRIMARY KEY,
    TenantId NVARCHAR(64),
    SubjectType NVARCHAR(50) NOT NULL,
    SubjectId NVARCHAR(450) NOT NULL,
    Relation NVARCHAR(50) NOT NULL,
    ObjectType NVARCHAR(50) NOT NULL,
    ObjectId NVARCHAR(450) NOT NULL,
    CreatedAt DATETIMEOFFSET NOT NULL,
    -- Composite index for lookups
    INDEX IX_RelTuple_Subject (SubjectType, SubjectId, Relation),
    INDEX IX_RelTuple_Object (ObjectType, ObjectId, Relation)
);
```

---

## Part 4: Implementation Roadmap

### Phase 1: Foundation (Recommended First)

| Task | Effort | Description |
|------|--------|-------------|
| Add `Project` entity | 2 days | Core entity with tenant scoping |
| Add `Team` entity | 1 day | Nested under Project |
| Add `ProjectMember` | 1 day | Join entity with scoped role |
| Add `ScopedRole` entity | 2 days | Replace global roles with scoped |
| Migrate existing roles | 1 day | Convert to tenant-scoped roles |

### Phase 2: Authorization Layer

| Task | Effort | Description |
|------|--------|-------------|
| Resource-based handler | 2 days | Check ownership/membership |
| Update `HasPermissionAttribute` | 1 day | Support resource parameter |
| Scope permission checks | 2 days | Check role within context |
| Permission inheritance service | 2 days | Parent → child permission flow |

### Phase 3: ReBAC Support (Optional)

| Task | Effort | Description |
|------|--------|-------------|
| Add `RelationshipTuple` | 2 days | Zanzibar-style tuples |
| Relationship service | 3 days | CRUD + traversal |
| Graph-based checks | 2 days | Traverse for permissions |
| Consider OpenFGA/SpiceDB | Evaluate | External service option |

### Phase 4: Advanced Features

| Task | Effort | Description |
|------|--------|-------------|
| Role templates | 2 days | Default roles per tenant |
| External user sharing | 3 days | Cross-tenant access |
| Permission UI components | 3 days | Role/permission management |
| Audit improvements | 1 day | Track permission grants |

---

## Part 5: Integration Options

### Option A: Build In-House (Recommended for Now)

**Pros:**
- Full control over implementation
- No external dependencies
- Integrates naturally with existing EF Core/ASP.NET Core
- Matches existing codebase patterns

**Cons:**
- More development effort
- Must maintain custom code
- May lack advanced features

**Best for:** Projects with specific requirements, full control needed

### Option B: OpenFGA Integration

**Pros:**
- Production-proven (CNCF Sandbox project)
- .NET SDK available (`Fga.Net.AspNetCore`)
- Handles complex ReBAC scenarios
- Scales to millions of relationships

**Cons:**
- External service dependency
- Data sync complexity
- Learning curve for DSL
- Operational overhead

**Best for:** Large-scale applications with complex hierarchies

### Option C: SpiceDB Integration

**Pros:**
- True Zanzibar implementation
- Strong consistency guarantees
- Built-in caching (Leopard-style)
- Supports caveats for ABAC-like features

**Cons:**
- Golang service (requires gRPC integration)
- More operational complexity
- Overkill for smaller applications

**Best for:** Google-scale applications, strict consistency requirements

### Recommended Approach

**Start with Option A** (in-house) for Phase 1-2, with the architecture designed to optionally migrate to Option B (OpenFGA) for Phase 3+ if complexity grows.

---

## Part 6: Code Examples

### 6.1 Scoped Permission Check

```csharp
// Current (global check)
[HasPermission(Permissions.ProjectsRead)]
public async Task<IResult> GetProject(Guid id) { ... }

// New (resource-based check)
[HasPermission(Permissions.ProjectsRead)]
public async Task<IResult> GetProject(Guid id, IAuthorizationService auth)
{
    var project = await _repo.GetByIdAsync(id);
    if (project == null) return Results.NotFound();

    // Resource-based authorization check
    var result = await auth.AuthorizeAsync(User, project, "projects:read");
    if (!result.Succeeded) return Results.Forbid();

    return Results.Ok(project);
}
```

### 6.2 Project Membership Service

```csharp
public class ProjectAuthorizationService : IProjectAuthorizationService
{
    public async Task<bool> CanAccessProjectAsync(
        string userId,
        Guid projectId,
        string requiredPermission)
    {
        // 1. Check if user is direct project member with permission
        var membership = await _projectMembers
            .FirstOrDefaultAsync(pm => pm.UserId == userId && pm.ProjectId == projectId);

        if (membership != null)
        {
            var rolePermissions = await GetRolePermissions(membership.RoleId);
            if (rolePermissions.Contains(requiredPermission))
                return true;
        }

        // 2. Check if user's team has access
        var teamMemberships = await _teamMembers
            .Where(tm => tm.UserId == userId)
            .Select(tm => tm.Team)
            .Where(t => t.ProjectId == projectId)
            .ToListAsync();

        foreach (var team in teamMemberships)
        {
            var teamMember = await _teamMembers
                .FirstOrDefaultAsync(tm => tm.UserId == userId && tm.TeamId == team.Id);

            if (teamMember != null)
            {
                var rolePermissions = await GetRolePermissions(teamMember.RoleId);
                if (rolePermissions.Contains(requiredPermission))
                    return true;
            }
        }

        // 3. Check tenant-level role (fallback)
        var tenantRole = await GetUserTenantRole(userId);
        if (tenantRole != null)
        {
            var rolePermissions = await GetRolePermissions(tenantRole.Id);
            if (rolePermissions.Contains(requiredPermission))
                return true;
        }

        return false;
    }
}
```

### 6.3 Role Template System

```csharp
// Default project roles (seeded per tenant)
public static class ProjectRoleTemplates
{
    public static readonly RoleTemplate Owner = new()
    {
        Name = "Owner",
        Scope = RoleScope.Project,
        Permissions = [
            "projects:*:own",       // Full project control
            "teams:*:own",          // Manage teams
            "members:*:own",        // Manage members
            "documents:*:own",      // All document access
        ]
    };

    public static readonly RoleTemplate Admin = new()
    {
        Name = "Admin",
        Scope = RoleScope.Project,
        Permissions = [
            "projects:read:own",
            "projects:update:own",
            "teams:*:own",
            "members:read:own",
            "members:update:own",
            "documents:*:own",
        ]
    };

    public static readonly RoleTemplate Member = new()
    {
        Name = "Member",
        Scope = RoleScope.Project,
        Permissions = [
            "projects:read:own",
            "documents:read:own",
            "documents:create:own",
        ]
    };

    public static readonly RoleTemplate Viewer = new()
    {
        Name = "Viewer",
        Scope = RoleScope.Project,
        Permissions = [
            "projects:read:own",
            "documents:read:own",
        ]
    };
}
```

---

## Part 7: Sources & References

### Primary Sources

- [Authorization Policy Showdown: RBAC vs. ABAC vs. ReBAC](https://www.permit.io/blog/rbac-vs-abac-vs-rebac) - Permit.io
- [RBAC vs ABAC vs ReBAC: Choosing the Right Permission System](https://blog.webdevsimplified.com/2025-11/rbac-vs-abac-vs-rebac/) - Web Dev Simplified
- [Best Practices for Multi-Tenant Authorization](https://www.permit.io/blog/best-practices-for-multi-tenant-authorization) - Permit.io
- [Multi-tenant RBAC](https://www.aserto.com/use-cases/multi-tenant-saas-rbac) - Aserto
- [AWS Multi-tenant Authorization Guide](https://docs.aws.amazon.com/prescriptive-guidance/latest/saas-multitenant-api-access-authorization/introduction.html) - AWS

### Zanzibar & ReBAC

- [Google Zanzibar Paper](https://research.google/pubs/zanzibar-googles-consistent-global-authorization-system/) - Google Research
- [How Google Drive Models Authorization](https://www.aserto.com/blog/google-zanzibar-drive-rebac-authorization-model) - Aserto
- [What is Google Zanzibar?](https://www.permit.io/blog/what-is-google-zanzibar) - Permit.io
- [Top 5 Zanzibar Implementations 2024](https://workos.com/blog/top-5-google-zanzibar-open-source-implementations-in-2024) - WorkOS

### .NET Integration

- [OpenFGA .NET SDK](https://github.com/openfga/dotnet-sdk) - OpenFGA
- [Fga.Net.AspNetCore](https://github.com/Hawxy/Fga.Net) - ASP.NET Core integration
- [Getting Started with SpiceDB in .NET](https://medium.com/kpmg-uk-engineering/getting-started-with-spicedb-in-net-741e353a4d83) - KPMG Engineering
- [Fine-Grained Authorization in ASP.NET Core](https://auth0.com/blog/fine-grained-authorization-in-aspnet-core-with-auth0-fga/) - Auth0
- [Resource-Based Authorization in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resourcebased) - Microsoft

### Multi-Tenant SaaS

- [Build Multi-Tenant SaaS Application](https://blog.logto.io/build-multi-tenant-saas-application) - Logto
- [How to Design Multi-Tenant SaaS Architecture](https://clerk.com/blog/how-to-design-multitenant-saas-architecture) - Clerk
- [B2B SaaS Identity Challenges: Granular Access Control](https://auth0.com/blog/b2b-saas-identity-challenges-granular-access-control/) - Auth0

---

## Conclusion

NOIR has a solid foundation with its tenant isolation and database-backed permissions. To support project-based SaaS, the key changes are:

1. **Add hierarchical entities** (Project, Team, ProjectMember)
2. **Scope roles to context** (tenant, project, team level)
3. **Implement resource-based checks** (verify ownership/membership)
4. **Design for extensibility** (optional ReBAC/OpenFGA migration path)

The recommended approach is to build Phase 1-2 in-house using ASP.NET Core's native authorization system, keeping the door open for OpenFGA integration if complexity grows.
