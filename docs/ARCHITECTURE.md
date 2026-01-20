# NOIR Architecture Overview

**Last Updated:** 2026-01-20
**Version:** 1.0

A comprehensive guide to the NOIR platform architecture, design patterns, and technical decisions.

---

## Table of Contents

1. [High-Level Architecture](#high-level-architecture)
2. [Clean Architecture Layers](#clean-architecture-layers)
3. [CQRS with Wolverine](#cqrs-with-wolverine)
4. [Multi-Tenancy Architecture](#multi-tenancy-architecture)
5. [Authentication & Authorization](#authentication--authorization)
6. [Data Access Patterns](#data-access-patterns)
7. [Audit System](#audit-system)
8. [Frontend Architecture](#frontend-architecture)
9. [Deployment Architecture](#deployment-architecture)
10. [Technology Stack](#technology-stack)

---

## High-Level Architecture

NOIR is a multi-tenant SaaS platform built with **.NET 10** and **React 19**, following **Clean Architecture** and **Domain-Driven Design (DDD)** principles.

```
┌─────────────────────────────────────────────────────────────────┐
│                     NOIR Platform                               │
│  ┌───────────────┐  ┌───────────────┐  ┌─────────────────────┐ │
│  │   React SPA   │  │  REST API     │  │  Background Jobs    │ │
│  │   (Vite)      │  │  (ASP.NET)    │  │  (Hangfire)         │ │
│  └───────────────┘  └───────────────┘  └─────────────────────┘ │
│                              │                                   │
│  ┌───────────────────────────┴───────────────────────────────┐  │
│  │            Application Layer (CQRS/Wolverine)            │  │
│  └───────────────────────────┬───────────────────────────────┘  │
│                              │                                   │
│  ┌───────────────────────────┴───────────────────────────────┐  │
│  │         Infrastructure Layer (EF Core, Identity)         │  │
│  └───────────────────────────┬───────────────────────────────┘  │
│                              │                                   │
│  ┌───────────────────────────┴───────────────────────────────┐  │
│  │              Domain Layer (Entities, Rules)              │  │
│  └──────────────────────────────────────────────────────────┘  │
│                              │                                   │
│  ┌───────────────────────────┴───────────────────────────────┐  │
│  │                   SQL Server Database                     │  │
│  └──────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

### Core Principles

1. **Separation of Concerns** - Each layer has a distinct responsibility
2. **Dependency Inversion** - Layers depend on abstractions, not implementations
3. **Testability** - Business logic isolated from infrastructure
4. **Maintainability** - Vertical slice organization for features
5. **Scalability** - Multi-tenant by design, horizontal scaling ready

---

## Clean Architecture Layers

NOIR follows a strict layered architecture with one-way dependencies:

```
┌──────────────────────────────────────────────────────────┐
│                    Web Layer                             │
│  • API Endpoints (Minimal API)                           │
│  • Middleware (Auth, Audit, Exception)                   │
│  • Frontend SPA (React)                                  │
└─────────────────────┬────────────────────────────────────┘
                      │ depends on
┌─────────────────────┴────────────────────────────────────┐
│               Infrastructure Layer                       │
│  • EF Core DbContext & Repositories                      │
│  • ASP.NET Identity (UserManager, RoleManager)           │
│  • External Services (Email, Storage, Cache)             │
│  • Audit Interceptors                                    │
└─────────────────────┬────────────────────────────────────┘
                      │ implements
┌─────────────────────┴────────────────────────────────────┐
│                Application Layer                         │
│  • Features (Commands, Queries, Handlers)                │
│  • Specifications (Query criteria)                       │
│  • DTOs (Data Transfer Objects)                          │
│  • Validators (FluentValidation)                         │
│  • Behaviors (Logging, Performance)                      │
└─────────────────────┬────────────────────────────────────┘
                      │ depends on
┌─────────────────────┴────────────────────────────────────┐
│                  Domain Layer                            │
│  • Entities (User, Tenant, Permission)                   │
│  • Value Objects (Email, PhoneNumber)                    │
│  • Interfaces (IRepository, ISpecification)              │
│  • Domain Events                                         │
│  • Business Rules                                        │
└──────────────────────────────────────────────────────────┘
```

### Dependency Rule

**↑ Layers can only depend on layers below them ↑**

- **Domain**: Zero dependencies (pure C#)
- **Application**: Depends only on Domain
- **Infrastructure**: Implements Application interfaces, depends on Domain + Application
- **Web**: Orchestrates all layers, depends on Infrastructure + Application

### Project Structure

```
src/
├── NOIR.Domain/           # 60 files - Core business entities
│   ├── Common/            # Base classes, value objects, result types
│   ├── Entities/          # Domain entities (User, Tenant, Permission)
│   └── Interfaces/        # Repository, specification contracts
│
├── NOIR.Application/      # 280 files - Business logic
│   ├── Features/          # Vertical slices (Auth, Users, Roles)
│   │   └── {Feature}/
│   │       ├── Commands/{Action}/
│   │       │   ├── {Action}Command.cs
│   │       │   ├── {Action}CommandHandler.cs
│   │       │   └── {Action}CommandValidator.cs
│   │       └── Queries/{Action}/
│   │           ├── {Action}Query.cs
│   │           └── {Action}QueryHandler.cs
│   ├── Specifications/    # Query specifications
│   ├── Common/            # Shared interfaces, DTOs
│   └── Behaviors/         # Pipeline middleware
│
├── NOIR.Infrastructure/   # 180 files - External integrations
│   ├── Persistence/       # EF Core, repositories
│   │   ├── ApplicationDbContext.cs
│   │   ├── Configurations/  # Entity configurations
│   │   └── Interceptors/    # Audit, tenant, domain events
│   ├── Identity/          # ASP.NET Identity
│   │   ├── ApplicationUser.cs
│   │   ├── TokenService.cs
│   │   └── Authorization/
│   ├── Services/          # External services
│   │   ├── EmailService.cs
│   │   ├── FileStorageService.cs
│   │   └── ImageProcessorService.cs
│   └── Audit/             # Audit middleware
│
└── NOIR.Web/              # 47 files - API + Frontend host
    ├── Program.cs         # Application entry point
    ├── Endpoints/         # Minimal API endpoints
    ├── Middleware/        # HTTP middleware
    └── frontend/          # 162 files - React SPA
        └── src/
            ├── pages/
            ├── components/
            ├── services/
            └── types/
```

---

## CQRS with Wolverine

NOIR uses **Vertical Slice Architecture** with **CQRS** (Command Query Responsibility Segregation) powered by **Wolverine**.

### Vertical Slice Pattern

Each feature is self-contained in a single folder:

```
Application/Features/Users/
├── Commands/
│   ├── CreateUser/
│   │   ├── CreateUserCommand.cs       # Command (request)
│   │   ├── CreateUserCommandHandler.cs  # Handler (logic)
│   │   └── CreateUserCommandValidator.cs  # Validator (rules)
│   └── UpdateUser/
│       ├── UpdateUserCommand.cs
│       ├── UpdateUserCommandHandler.cs
│       └── UpdateUserCommandValidator.cs
└── Queries/
    ├── GetUsers/
    │   ├── GetUsersQuery.cs
    │   └── GetUsersQueryHandler.cs
    └── GetUserById/
        ├── GetUserByIdQuery.cs
        └── GetUserByIdQueryHandler.cs
```

**Benefits:**
- **Cohesion** - All feature code in one place
- **Discoverability** - Easy to navigate and understand
- **Deletion** - Remove a feature by deleting one folder
- **Testing** - Test entire feature slice in isolation

### Command Pattern

Commands represent **write operations** (Create, Update, Delete):

```csharp
// Command - What to do
public sealed record CreateUserCommand(
    string Email,
    string DisplayName,
    string Password,
    List<string> RoleNames) : ICommand<Result<UserDto>>;

// Handler - How to do it
public class CreateUserCommandHandler
{
    private readonly IUserIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;

    public async Task<Result<UserDto>> Handle(
        CreateUserCommand cmd,
        CancellationToken ct)
    {
        // 1. Validate business rules
        // 2. Create entity
        // 3. Persist changes
        // 4. Return result
    }
}

// Validator - Rules
public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MinimumLength(6);
    }
}
```

### Query Pattern

Queries represent **read operations**:

```csharp
// Query - What to fetch
public sealed record GetUsersQuery(
    string? Search,
    int PageNumber,
    int PageSize) : IQuery<Result<PaginatedList<UserDto>>>;

// Handler - How to fetch it
public class GetUsersQueryHandler
{
    private readonly IReadRepository<ApplicationUser, string> _repository;

    public async Task<Result<PaginatedList<UserDto>>> Handle(
        GetUsersQuery query,
        CancellationToken ct)
    {
        var spec = new ActiveUsersSpec(query.Search);
        var users = await _repository.ListAsync(spec, ct);
        return Result.Success(users.ToPaginatedList());
    }
}
```

### Wolverine Message Bus

Wolverine handles command/query routing and execution:

```csharp
// In endpoint
app.MapPost("/api/users", async (
    CreateUserCommand command,
    IMessageBus bus) =>
{
    var result = await bus.InvokeAsync<Result<UserDto>>(command);
    return result.ToHttpResult();
});
```

**Wolverine Features Used:**
- **Handler Discovery** - Auto-discovers handlers by convention
- **Validation Pipeline** - Runs FluentValidation before handlers
- **Error Handling** - Translates exceptions to error responses
- **Logging** - Built-in request/response logging

**ADR:** See [003-vertical-slice-cqrs.md](decisions/003-vertical-slice-cqrs.md)

---

## Multi-Tenancy Architecture

NOIR implements **multi-tenancy** using **Finbuckle.MultiTenant** with database-per-schema isolation.

### Tenant Isolation Model

```
┌─────────────────────────────────────────────────────────┐
│                    SQL Server                           │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │   tenant_a   │  │   tenant_b   │  │  platform    │  │
│  │   (schema)   │  │   (schema)   │  │  (TenantId=  │  │
│  │              │  │              │  │   NULL)      │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
└─────────────────────────────────────────────────────────┘
```

### Tenant Resolution Strategy

Finbuckle resolves tenants using this priority order:

1. **HTTP Header** - `X-Tenant: tenant-identifier`
2. **JWT Claim** - `tenant_id` claim in access token
3. **No Fallback** - Null for platform admins (removed static strategy)

### Platform vs Tenant Data

NOIR supports a **two-level data model**:

| Level | TenantId | Scope | Example |
|-------|----------|-------|---------|
| **Platform** | `null` | Shared across all tenants | Default email templates |
| **Tenant** | `Guid` | Specific to one tenant | Custom email templates |

#### Platform/Tenant Pattern

Base classes for entities that support platform defaults with tenant overrides:

```csharp
// For entities WITHOUT domain events
public abstract class PlatformTenantEntity<TId> : Entity<TId>, IAuditableEntity
{
    public string? TenantId { get; protected set; }  // Null = platform
    public bool IsPlatformDefault => TenantId is null;
    public bool IsTenantOverride => TenantId is not null;
}

// For entities WITH domain events
public abstract class PlatformTenantAggregateRoot<TId> : AggregateRoot<TId>
{
    public string? TenantId { get; protected set; }
    // ... (same properties)
}
```

**Example Usage:**

```csharp
// EmailTemplate uses PlatformTenantAggregateRoot
public class EmailTemplate : PlatformTenantAggregateRoot<Guid>
{
    // Platform factory method
    public static EmailTemplate CreatePlatformDefault(
        string name, string subject, string htmlBody)
    {
        return new EmailTemplate
        {
            TenantId = null,  // Platform-level
            Name = name,
            Subject = subject,
            HtmlBody = htmlBody
        };
    }

    // Tenant factory method
    public static EmailTemplate CreateTenantOverride(
        Guid tenantId, string name, string subject, string htmlBody)
    {
        return new EmailTemplate
        {
            TenantId = tenantId.ToString(),  // Tenant-specific
            Name = name,
            Subject = subject,
            HtmlBody = htmlBody
        };
    }
}
```

#### Copy-on-Write Pattern

When tenants edit platform defaults, a **copy-on-write** creates a tenant-specific override:

```csharp
// Query pattern with fallback
var templates = await _dbContext.Set<EmailTemplate>()
    .IgnoreQueryFilters()  // Bypass tenant filter
    .Where(t => t.Name == templateName && t.IsActive && !t.IsDeleted)
    .ToListAsync();

// Prefer tenant-specific, fallback to platform
var template = templates.FirstOrDefault(t => t.TenantId == currentTenantId)
    ?? templates.FirstOrDefault(t => t.TenantId == null);
```

### User-Tenant Membership

Users can belong to **multiple tenants**:

```csharp
public class UserTenantMembership : Entity<Guid>
{
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public TenantRole Role { get; set; }  // Owner, Admin, Member, Viewer
    public bool IsDefault { get; set; }   // User's default tenant
    public DateTimeOffset JoinedAt { get; set; }
}
```

**Tenant Roles:**
- **Owner** - Full control, can delete tenant
- **Admin** - Manage users and settings
- **Member** - Standard access
- **Viewer** - Read-only access

### Global Query Filters

EF Core automatically filters queries by `TenantId`:

```csharp
// In ApplicationDbContext.OnModelCreating
foreach (var entityType in builder.Model.GetEntityTypes())
{
    if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
    {
        entityType.AddQueryFilter<ITenantEntity>(
            e => e.TenantId == _currentUser.TenantId);
    }
}
```

**Bypass filters when needed:**
```csharp
// Platform admin queries
var allTenants = await _dbContext.Tenants
    .IgnoreQueryFilters()
    .ToListAsync();
```

---

## Authentication & Authorization

### JWT + Refresh Token Flow

```
┌──────────┐                 ┌──────────┐                 ┌──────────┐
│  Client  │                 │   API    │                 │ Database │
└────┬─────┘                 └────┬─────┘                 └────┬─────┘
     │                            │                            │
     │  POST /auth/login          │                            │
     ├───────────────────────────>│                            │
     │  { email, password }       │                            │
     │                            │  Validate credentials      │
     │                            ├───────────────────────────>│
     │                            │                            │
     │  200 OK                    │  Return user + tenant      │
     │  { accessToken, user }     │<───────────────────────────┤
     │<───────────────────────────┤                            │
     │  Set-Cookie: refresh_token │  Store refresh token       │
     │     (HTTP-only)            ├───────────────────────────>│
     │                            │                            │
     │  GET /api/users            │                            │
     │  Authorization: Bearer ... │                            │
     ├───────────────────────────>│                            │
     │                            │  Validate JWT              │
     │  200 OK { users }          │  Check permissions         │
     │<───────────────────────────┤                            │
     │                            │                            │
     │  (access token expires)    │                            │
     │                            │                            │
     │  POST /auth/refresh        │                            │
     ├───────────────────────────>│  Validate refresh token    │
     │  Cookie: refresh_token     ├───────────────────────────>│
     │                            │                            │
     │  200 OK                    │  Rotate token              │
     │  { accessToken }           │<───────────────────────────┤
     │<───────────────────────────┤                            │
     │  Set-Cookie: new_token     ├───────────────────────────>│
     │                            │                            │
```

### JWT Token Structure

```json
{
  "header": {
    "alg": "HS256",
    "typ": "JWT"
  },
  "payload": {
    "sub": "user-guid",
    "email": "admin@noir.local",
    "name": "Platform Admin",
    "tenant_id": null,  // Omitted for platform admins
    "role": ["PlatformAdmin"],
    "permissions": ["*:*:*"],
    "exp": 1640000000,
    "iat": 1639996400
  },
  "signature": "..."
}
```

### Permission-Based Authorization

**Permission Format:** `{resource}:{action}:{scope}`

**Examples:**
- `users:read:all` - View all users
- `users:write:own` - Create/update own tenant's users
- `audit:view:all` - View all audit logs

**Usage in Endpoints:**

```csharp
app.MapGet("/api/users", [HasPermission("users:read:all")] async (
    GetUsersQuery query,
    IMessageBus bus) =>
{
    var result = await bus.InvokeAsync<Result<PaginatedList<UserDto>>>(query);
    return result.ToHttpResult();
});
```

**Usage in Frontend:**

```tsx
import { usePermissions, Permissions } from '@/hooks/usePermissions'

function UserActions() {
  const { hasPermission } = usePermissions()
  const canEdit = hasPermission(Permissions.UsersUpdate)

  return (
    <>
      {canEdit && <Button onClick={handleEdit}>Edit</Button>}
    </>
  )
}
```

### Role Hierarchy

Roles support inheritance via `ParentRoleId`:

```
Admin (parent)
├── Manager (inherits Admin permissions)
└── Editor (inherits Admin permissions)
    └── Author (inherits Editor permissions)
```

**Effective Permissions = Direct + Inherited**

---

## Data Access Patterns

### Repository + Specification Pattern

NOIR uses the **Repository Pattern** with **Specification Pattern** for all database queries.

**Rule:** Never use raw `DbSet` queries in handlers. Always use specifications.

```csharp
// ❌ WRONG - Direct DbSet query
var users = await _dbContext.Users
    .Where(u => u.IsActive)
    .ToListAsync();

// ✅ CORRECT - Specification query
var spec = new ActiveUsersSpec();
var users = await _repository.ListAsync(spec, ct);
```

### Specification Example

```csharp
public class ActiveUsersSpec : Specification<ApplicationUser>
{
    public ActiveUsersSpec(string? search = null)
    {
        Query.Where(u => u.IsActive && !u.IsDeleted)
             .TagWith("GetActiveUsers");  // REQUIRED for SQL debugging

        if (!string.IsNullOrWhiteSpace(search))
        {
            Query.Where(u =>
                u.Email.Contains(search) ||
                u.DisplayName.Contains(search));
        }
    }
}
```

**Specification Benefits:**
- **Reusability** - Use same spec in multiple handlers
- **Testability** - Test query logic independently
- **Composability** - Combine multiple specs
- **SQL Debugging** - `TagWith()` adds SQL comments

### Unit of Work Pattern

**CRITICAL:** Repository methods do NOT auto-save! Always use `IUnitOfWork`.

```csharp
public class CreateOrderCommandHandler
{
    private readonly IRepository<Order, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;  // REQUIRED!

    public async Task<Result<OrderDto>> Handle(
        CreateOrderCommand cmd,
        CancellationToken ct)
    {
        var order = Order.Create(cmd.CustomerId, cmd.Items);
        await _repository.AddAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);  // REQUIRED!
        return Result.Success(order.ToDto());
    }
}
```

### Tracking Strategy

Specifications default to `AsNoTracking` (read-only). For mutations, use `AsTracking`:

```csharp
public class UserByIdForUpdateSpec : Specification<ApplicationUser>
{
    public UserByIdForUpdateSpec(Guid id)
    {
        Query.Where(u => u.Id == id.ToString())
             .AsTracking()  // REQUIRED for modification!
             .TagWith("GetUserForUpdate");
    }
}
```

**Pattern Doc:** [repository-specification.md](backend/patterns/repository-specification.md)

---

## Audit System

NOIR implements **3-level hierarchical audit logging**:

```
┌─────────────────────────────────────────────────────────────┐
│  HTTP Request Audit                                         │
│  • Tracks: Method, Path, Status, Duration                   │
│  • CorrelationId: Links to child logs                       │
└─────────────────────────┬───────────────────────────────────┘
                          │
           ┌──────────────┴──────────────┐
           │                             │
┌──────────┴──────────────┐  ┌───────────┴──────────────────┐
│  Handler Audit          │  │  Handler Audit               │
│  • Tracks: Command name │  │  • Multiple handlers can run │
│  • Duration, Result     │  │    in one HTTP request       │
└──────────┬──────────────┘  └───────────┬──────────────────┘
           │                             │
    ┌──────┴──────┐              ┌───────┴────────┐
    │             │              │                │
┌───┴────┐  ┌────┴────┐  ┌──────┴───┐  ┌─────────┴────┐
│ Entity │  │ Entity  │  │ Entity   │  │  Entity      │
│ Change │  │ Change  │  │ Change   │  │  Change      │
│ (User) │  │ (Role)  │  │ (Tenant) │  │ (Settings)   │
└────────┘  └─────────┘  └──────────┘  └──────────────┘
```

### Correlation Tracking

Every request gets a `CorrelationId` that links all related audit logs:

```
HTTP Request: correlation-123
  └─> Handler: CreateUserCommandHandler (correlation-123)
      └─> Entity: User created (correlation-123)
```

### Entity Change Tracking

The `EntityAuditLogInterceptor` automatically captures before/after state:

```json
{
  "entityId": "user-guid",
  "entityType": "ApplicationUser",
  "operationType": "Update",
  "before": {
    "email": "old@example.com",
    "displayName": "Old Name"
  },
  "after": {
    "email": "new@example.com",
    "displayName": "New Name"
  },
  "changedFields": ["Email", "DisplayName"],
  "userId": "admin-guid",
  "timestamp": "2026-01-20T10:00:00Z"
}
```

### Auditable Commands

Commands that mutate data via frontend MUST implement `IAuditableCommand`:

```csharp
public sealed record UpdateUserCommand(
    Guid Id,
    string DisplayName) : IAuditableCommand<UserDto>
{
    [JsonIgnore]
    public string? UserId { get; init; }  // Set by endpoint

    public object? GetTargetId() => Id;
    public AuditOperationType OperationType => AuditOperationType.Update;
    public string? GetActionDescription() => $"Updated user '{DisplayName}'";
}
```

**Requirements:**
1. Command implements `IAuditableCommand<TResult>`
2. Endpoint sets `UserId` from `ICurrentUser`
3. Frontend calls `usePageContext('PageName')`

**Pattern Doc:** [hierarchical-audit-logging.md](backend/patterns/hierarchical-audit-logging.md)

---

## Frontend Architecture

### React SPA Structure

```
frontend/src/
├── pages/                 # Route components
│   ├── Landing.tsx
│   ├── Login.tsx
│   └── portal/            # Authenticated pages
│       ├── Dashboard.tsx
│       └── admin/         # Admin pages
│           ├── users/
│           ├── roles/
│           └── tenants/
├── components/            # Reusable components
│   ├── ui/                # shadcn/ui + 21st.dev
│   ├── PermissionGate.tsx
│   └── ProtectedRoute.tsx
├── services/              # API client
│   ├── apiClient.ts       # Base client with auth
│   ├── auth.ts
│   └── users.ts
├── hooks/                 # Custom React hooks
│   └── usePermissions.ts
├── contexts/              # React contexts
│   ├── AuthContext.tsx
│   └── ThemeContext.tsx
├── types/                 # TypeScript types
│   ├── api.ts
│   └── auth.ts
└── lib/                   # Utilities
    └── utils.ts
```

### API Client Pattern

Centralized API client with authentication and error handling:

```typescript
// apiClient.ts
import axios from 'axios'

export const apiClient = axios.create({
  baseURL: '/api',
  withCredentials: true,  // Include HTTP-only cookies
})

// Request interceptor - Add JWT token
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// Response interceptor - Handle token refresh
apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      // Try refresh
      try {
        const { data } = await axios.post('/api/auth/refresh')
        localStorage.setItem('accessToken', data.accessToken)
        // Retry original request
        error.config.headers.Authorization = `Bearer ${data.accessToken}`
        return axios(error.config)
      } catch {
        // Refresh failed, logout
        window.location.href = '/login'
      }
    }
    return Promise.reject(error)
  }
)
```

### Permission-Based Rendering

```tsx
import { usePermissions, Permissions } from '@/hooks/usePermissions'

function UserTable() {
  const { hasPermission } = usePermissions()
  const canEdit = hasPermission(Permissions.UsersUpdate)
  const canDelete = hasPermission(Permissions.UsersDelete)

  return (
    <Table>
      {users.map(user => (
        <TableRow key={user.id}>
          <TableCell>{user.email}</TableCell>
          <TableCell>
            {canEdit && <Button onClick={() => edit(user)}>Edit</Button>}
            {canDelete && <Button onClick={() => del(user)}>Delete</Button>}
          </TableCell>
        </TableRow>
      ))}
    </Table>
  )
}
```

### Type Safety with TypeScript

Types are auto-generated from backend DTOs:

```bash
# In frontend directory
npm run generate:api
```

Generates `src/types/generated.ts` with all API types.

**Doc:** [frontend/architecture.md](frontend/architecture.md)

---

## Deployment Architecture

### Development Environment

```
┌───────────────────────────────────────────────────────┐
│  Developer Machine                                    │
│  ┌─────────────┐      ┌──────────────┐               │
│  │  Vite Dev   │      │  .NET Watch  │               │
│  │  :3000      │─────>│  :4000       │               │
│  │  (React)    │ proxy│  (API)       │               │
│  └─────────────┘      └──────┬───────┘               │
│                              │                        │
│                       ┌──────┴────────┐               │
│                       │  SQL Server   │               │
│                       │  LocalDB/     │               │
│                       │  Docker       │               │
│                       └───────────────┘               │
└───────────────────────────────────────────────────────┘
```

**URLs:**
- Frontend: `http://localhost:3000`
- Backend: `http://localhost:4000`
- API Docs: `http://localhost:3000/api/docs`

### Production Environment

```
┌────────────────────────────────────────────────────────┐
│  Azure App Service / Docker Container                 │
│  ┌──────────────────────────────────────────────────┐ │
│  │  ASP.NET Core App (:80)                          │ │
│  │  ┌────────────┐      ┌─────────────────────┐    │ │
│  │  │  React SPA │      │  REST API           │    │ │
│  │  │  (wwwroot) │<─────│  (/api/*)           │    │ │
│  │  └────────────┘      └──────┬──────────────┘    │ │
│  │                             │                    │ │
│  │                      ┌──────┴──────┐             │ │
│  │                      │  Hangfire   │             │ │
│  │                      │  Dashboard  │             │ │
│  │                      └─────────────┘             │ │
│  └──────────────────────────────────────────────────┘ │
└───────────┬────────────────────────────────────────────┘
            │
     ┌──────┴───────┐
     │  Azure SQL   │
     │  Database    │
     └──────────────┘
```

**Build Process:**
1. `dotnet build -c Release` → Builds .NET projects
2. `npm run build` (in frontend/) → Vite builds to `wwwroot/`
3. .NET serves React SPA + API from single host

---

## Technology Stack

### Backend

| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 10 LTS | Framework (support until 2028) |
| ASP.NET Core | 10 | Web API host |
| Entity Framework Core | 10 | ORM |
| SQL Server | 2022 | Primary database |
| Wolverine | 3.x | CQRS messaging |
| FluentValidation | 11.x | Request validation |
| Mapperly | 3.x | DTO mapping (source gen) |
| Finbuckle.MultiTenant | 10.x | Multi-tenancy |
| ASP.NET Identity | 10 | Authentication |
| Hangfire | 1.8.x | Background jobs |
| Serilog | 10.x | Structured logging |
| FluentEmail | 3.x | Email sending |
| SixLabors.ImageSharp | 3.1.x | Image processing |

### Frontend

| Technology | Version | Purpose |
|------------|---------|---------|
| React | 19 | UI library |
| TypeScript | 5.x | Type safety |
| Vite | Latest | Build tool & dev server |
| Tailwind CSS | 4 | Styling |
| React Router | 7 | Client-side routing |
| shadcn/ui | Latest | UI component primitives |
| i18next | Latest | Internationalization |
| axios | Latest | HTTP client |
| zod | Latest | Schema validation |

### Infrastructure

| Technology | Purpose |
|------------|---------|
| Docker | SQL Server (macOS/Linux dev) |
| LocalDB | SQL Server (Windows dev) |
| MailHog | Email testing (dev) |
| Azure App Service | Hosting (prod) |
| Azure SQL | Database (prod) |

---

## Design Patterns Used

| Pattern | Where | Purpose |
|---------|-------|---------|
| **Clean Architecture** | Solution structure | Separation of concerns |
| **CQRS** | Application layer | Command/Query separation |
| **Vertical Slice** | Feature organization | Co-locate related code |
| **Repository** | Data access | Abstract persistence |
| **Specification** | Queries | Encapsulate query logic |
| **Unit of Work** | Transactions | Manage DB transactions |
| **Factory Method** | Entity creation | Controlled instantiation |
| **Domain Events** | Business logic | Decouple side effects |
| **Result Pattern** | Error handling | Railway-oriented programming |
| **Strategy** | Multi-tenancy | Tenant resolution |
| **Decorator** | Audit logging | Cross-cutting concerns |
| **Interceptor** | EF Core | Hook into persistence |

---

## Key Architectural Decisions

| ADR | Title | Status |
|-----|-------|--------|
| [001](decisions/001-tech-stack.md) | Technology Stack Selection | Accepted |
| [002](decisions/002-frontend-ui-stack.md) | Frontend UI Stack | Accepted |
| [003](decisions/003-vertical-slice-cqrs.md) | Vertical Slice Architecture for CQRS | Accepted |

---

## Performance Considerations

### Database Optimization

- **Filtered Indexes** for platform defaults: 2-3x faster queries
- **AsNoTracking** by default: Reduce memory overhead
- **AsSplitQuery** for multiple collections: Prevent cartesian explosion
- **Bulk Operations** for 1000+ records: Use EFCore.BulkExtensions

### Caching Strategy

- **In-Memory Cache**: Permissions, roles (short TTL)
- **HTTP Cache**: Static files (1 year), avatars (immutable)
- **CDN**: Frontend assets in production

### API Performance

- **Pagination**: Max 100 items per page
- **Projection**: Select only needed fields
- **Lazy Loading**: Disabled (explicit includes only)

---

## Security Architecture

### Defense in Depth

1. **Network Layer**: HTTPS, HSTS, rate limiting
2. **Application Layer**: Authentication, authorization
3. **Data Layer**: SQL injection prevention, encrypted fields
4. **Audit Layer**: Full activity tracking

### Security Headers

```
Strict-Transport-Security: max-age=31536000; includeSubDomains
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
X-XSS-Protection: 1; mode=block
Content-Security-Policy: default-src 'self'
```

### Secrets Management

- **Development**: User Secrets (`dotnet user-secrets`)
- **Production**: Azure Key Vault / Environment Variables

---

## Related Documentation

| Document | Purpose |
|----------|---------|
| [KNOWLEDGE_BASE.md](KNOWLEDGE_BASE.md) | Complete codebase reference |
| [API_INDEX.md](API_INDEX.md) | API endpoint documentation |
| [Backend Patterns](backend/README.md) | Implementation patterns |
| [Frontend Guide](frontend/README.md) | Frontend architecture |

---

*Last Updated: 2026-01-20 | Architecture Version: 1.0 | Clean Architecture + CQRS + Multi-Tenancy*
