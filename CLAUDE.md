# NOIR - Claude Code Instructions

## CRITICAL RULES

**IMPORTANT:** Follow these rules in every task:

1. **Always check existing patterns** before writing new code - look at similar files first
2. **Use Specifications** for all database queries - never raw `DbSet` queries in services
3. **Tag all specifications** with `TagWith("MethodName")` for SQL debugging
4. **Run `dotnet build src/NOIR.sln`** after code changes to verify compilation
5. **Soft delete only** - never use hard delete unless explicitly requested for GDPR
6. **No using statements in files** - add new usings to `GlobalUsings.cs` in each project instead
7. **Use marker interfaces** for DI - add `IScopedService` to new services

## Project Overview

.NET 10 + React SaaS with multi-tenancy. Clean Architecture + CQRS + DDD.

**Tech:** EF Core, Wolverine (not MediatR), FluentValidation, Serilog, Finbuckle, Scrutor, Hangfire

## Commands

```bash
dotnet build src/NOIR.sln                    # Build
dotnet run --project src/NOIR.Web            # Run (localhost:5000)
dotnet watch --project src/NOIR.Web          # Run with hot reload
dotnet test src/NOIR.sln                     # Test (1,739 tests)
dotnet test src/NOIR.sln --collect:"XPlat Code Coverage"  # Test with coverage
dotnet ef migrations add NAME --project src/NOIR.Infrastructure --startup-project src/NOIR.Web
```

## Key Directories

```
src/NOIR.Domain/          -> Entities, IRepository, ISpecification
src/NOIR.Application/     -> Commands, Queries, Specifications, DTOs, ServiceLifetimes
src/NOIR.Infrastructure/  -> EF Core, Repositories, Handlers, Configurations
src/NOIR.Web/             -> Endpoints, Middleware, Program.cs
tests/                    -> Unit, Integration, Architecture tests
```

## Code Patterns

### Service Registration (Auto via Scrutor)

```csharp
// Just add marker interface - auto-registered!
public class CustomerService : ICustomerService, IScopedService
{
    // Implementation
}

// Available markers: IScopedService, ITransientService, ISingletonService
```

### Specifications (ALWAYS use for queries)

```csharp
public class ActiveCustomersSpec : Specification<Customer>
{
    public ActiveCustomersSpec(string? search = null)
    {
        Query.Where(c => c.IsActive)
             .TagWith("GetActiveCustomers");  // REQUIRED for debugging
        if (!string.IsNullOrEmpty(search))
            Query.Where(c => c.Name.Contains(search));
    }
}
// Usage: await _repo.ListAsync(new ActiveCustomersSpec());
```

### Entity Configuration (Auto-discovered)

```csharp
// Create in src/NOIR.Infrastructure/Persistence/Configurations/
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
    }
}
// Auto-discovered via ApplyConfigurationsFromAssembly
```

### Handlers (Wolverine - no interface needed)

```csharp
public static class CreateOrderHandler
{
    public static async Task<OrderResponse> Handle(
        CreateOrderCommand cmd,
        IRepository<Order, Guid> repo,
        CancellationToken ct)
    {
        // Implementation
    }
}
```

## Performance Rules

| Scenario | Use |
|----------|-----|
| Read-only queries | `AsNoTracking` (default) |
| Multiple collections | `.AsSplitQuery()` |
| Count/Any operations | Auto-optimized by repo |

## Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| Specification | `[Entity][Filter]Spec` | `ActiveCustomersSpec` |
| Command | `[Action][Entity]Command` | `CreateOrderCommand` |
| Handler | `[Command]Handler` | `CreateOrderHandler` |
| Configuration | `[Entity]Configuration` | `CustomerConfiguration` |

## Security Features

| Feature | Status | Description |
|---------|--------|-------------|
| JWT + Refresh Tokens | Implemented | Token rotation with theft detection |
| Audit Logging | Implemented | Automatic entity change tracking |
| Permission System | Implemented | Database-backed RBAC |
| Device Fingerprinting | Implemented | Optional token binding |
| Multi-Tenancy | Implemented | Finbuckle with auto-filtering |
| Rate Limiting | Implemented | Fixed window (100/min) + Sliding window for auth (5/min) |
| Security Headers | Implemented | X-Frame-Options, X-Content-Type-Options, etc. |

## API Endpoints

### Authentication
| Endpoint | Auth | Rate Limit | Purpose |
|----------|------|------------|---------|
| `/api/auth/login` | No | auth (5/min) | JWT login |
| `/api/auth/register` | No | auth (5/min) | Register |
| `/api/auth/refresh` | No | auth (5/min) | Rotate tokens |
| `/api/auth/me` | Yes | fixed (100/min) | Current user |
| `/api/auth/me` (PUT) | Yes | fixed (100/min) | Update profile |

### User Management (Admin)
| Endpoint | Auth | Purpose |
|----------|------|---------|
| `/api/users` | Admin | List users (paginated) |
| `/api/users/{id}` | Admin | Get user by ID |
| `/api/users/{id}` (PUT) | Admin | Update user |
| `/api/users/{id}` (DELETE) | Admin | Soft delete user |
| `/api/users/{id}/roles` | Admin | Assign roles |

### Role Management (Admin)
| Endpoint | Auth | Purpose |
|----------|------|---------|
| `/api/roles` | Admin | List roles |
| `/api/roles/{id}` | Admin | Get role with permissions |
| `/api/roles` (POST) | Admin | Create role |
| `/api/roles/{id}` (DELETE) | Admin | Delete role |
| `/api/roles/{id}/permissions` | Admin | Manage role permissions |

### Permissions (Admin)
| Endpoint | Auth | Purpose |
|----------|------|---------|
| `/api/permissions` | Admin | List all permissions |

### System
| Endpoint | Auth | Purpose |
|----------|------|---------|
| `/api/docs` | No | Scalar API docs |
| `/api/health` | No | Health check |
| `/hangfire` | Admin | Job dashboard |

**Admin:** `admin@noir.local` / `123qwe`

## Test Projects

| Project | Tests | Purpose |
|---------|-------|---------|
| NOIR.Domain.UnitTests | 469 | Entity behavior, Result pattern, Value objects |
| NOIR.Application.UnitTests | 962 | Handlers, Services, Validators, Specifications |
| NOIR.IntegrationTests | 283 | End-to-end API tests with SQL Server |
| NOIR.ArchitectureTests | 25 | Layer dependency validation |

**Cross-Platform Testing Strategy:**
- **Windows**: Uses SQL Server LocalDB automatically
- **macOS/Linux**: Uses Docker SQL Server (localhost:1433) automatically
- **Override**: Set `NOIR_TEST_SQL_CONNECTION` environment variable for custom SQL Server
- **Force LocalDB**: Set `NOIR_USE_LOCALDB=true` (e.g., WSL with LocalDB available)

**Database Strategy:** SQL Server for both testing and production. No SQLite/InMemory - ensures identical behavior between test and production environments.

## Audit Diff Format

Entity and DTO diffs use a simple field-level format for readability and easy querying:

```json
{
  "name": { "from": "Old Name", "to": "New Name" },
  "email": { "from": "old@x.com", "to": "new@x.com" },
  "phone": { "from": null, "to": "+1-555-0123" }
}
```

Nested paths use dot notation: `"address.city": { "from": "NYC", "to": "LA" }`

## Detailed Documentation

| Topic | Location |
|-------|----------|
| Tech stack decisions | `.claude/decisions/001-tech-stack.md` |
| Repository pattern | `.claude/patterns/repository-specification.md` |
| DI auto-registration | `.claude/patterns/di-auto-registration.md` |
| Entity configuration | `.claude/patterns/entity-configuration.md` |
| JWT refresh tokens | `.claude/patterns/jwt-refresh-token.md` |
| Audit logging | `.claude/patterns/hierarchical-audit-logging.md` |

## Research Archive (Implemented)

Research notes for implemented features are in `.claude/brainstorming/`:
- Rate limiting research (implemented in Program.cs)
- Middleware ordering (implemented in Program.cs)
- CSP best practices (research complete)

## File Boundaries

**Read freely:** `src/`, `tests/`, `.claude/`

**Avoid modifying:** `*.Designer.cs`, `Migrations/` (auto-generated)
