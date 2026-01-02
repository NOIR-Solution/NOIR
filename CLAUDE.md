# NOIR - Claude Code Instructions

## Critical Rules

1. **Check existing patterns first** - Look at similar files before writing new code
2. **Use Specifications** for all database queries - Never raw `DbSet` queries in services
3. **Tag all specifications** with `TagWith("MethodName")` for SQL debugging
4. **Run `dotnet build src/NOIR.sln`** after code changes
5. **Soft delete only** - Never hard delete unless explicitly requested for GDPR
6. **No using statements in files** - Add to `GlobalUsings.cs` in each project
7. **Use marker interfaces** for DI - Add `IScopedService`, `ITransientService`, or `ISingletonService`

## Quick Reference

```bash
# Build & Run
dotnet build src/NOIR.sln
dotnet run --project src/NOIR.Web
dotnet watch --project src/NOIR.Web

# Tests (1,739 tests)
dotnet test src/NOIR.sln

# Migrations
dotnet ef migrations add NAME --project src/NOIR.Infrastructure --startup-project src/NOIR.Web
```

**Admin Login:** `admin@noir.local` / `123qwe`

## Project Structure

```
src/NOIR.Domain/          # Entities, IRepository, ISpecification
src/NOIR.Application/     # Commands, Queries, Specifications, DTOs
src/NOIR.Infrastructure/  # EF Core, Repositories, Handlers
src/NOIR.Web/             # Endpoints, Middleware, Program.cs
```

## Code Patterns

### Service Registration
```csharp
// Just add marker interface - auto-registered!
public class CustomerService : ICustomerService, IScopedService { }
```

### Specifications (Required for queries)
```csharp
public class ActiveCustomersSpec : Specification<Customer>
{
    public ActiveCustomersSpec(string? search = null)
    {
        Query.Where(c => c.IsActive)
             .TagWith("GetActiveCustomers");  // REQUIRED
    }
}
```

### Handlers (Wolverine)
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

### Entity Configuration
```csharp
// Auto-discovered via ApplyConfigurationsFromAssembly
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(e => e.Id);
    }
}
```

## Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| Specification | `[Entity][Filter]Spec` | `ActiveCustomersSpec` |
| Command | `[Action][Entity]Command` | `CreateOrderCommand` |
| Handler | `[Command]Handler` | `CreateOrderHandler` |
| Configuration | `[Entity]Configuration` | `CustomerConfiguration` |

## Performance Rules

| Scenario | Use |
|----------|-----|
| Read-only queries | `AsNoTracking` (default) |
| Multiple collections | `.AsSplitQuery()` |

## Detailed Documentation

| Topic | Location |
|-------|----------|
| **Setup guide** | `SETUP.md` |
| **Project overview** | `README.md` |
| **Tech decisions** | `.claude/decisions/001-tech-stack.md` |
| **Frontend stack** | `.claude/decisions/002-frontend-ui-stack.md` |
| **Repository pattern** | `.claude/patterns/repository-specification.md` |
| **DI registration** | `.claude/patterns/di-auto-registration.md` |
| **Entity configuration** | `.claude/patterns/entity-configuration.md` |
| **JWT refresh tokens** | `.claude/patterns/jwt-refresh-token.md` |
| **Audit logging** | `.claude/patterns/hierarchical-audit-logging.md` |

## File Boundaries

**Read freely:** `src/`, `tests/`, `.claude/`

**Avoid modifying:** `*.Designer.cs`, `Migrations/` (auto-generated)
