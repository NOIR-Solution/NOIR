# AGENTS.md

> Universal AI agent instructions for NOIR. Compatible with Claude Code, Cursor, Windsurf, GitHub Copilot, and other AI coding assistants.

## Project Overview

**NOIR** is an enterprise .NET 10 + React SaaS foundation using Clean Architecture, CQRS, and DDD patterns.

```
src/
├── NOIR.Domain/           # Core entities, interfaces (no dependencies)
├── NOIR.Application/      # Commands, queries, specifications, DTOs
├── NOIR.Infrastructure/   # EF Core, handlers, external services
└── NOIR.Web/              # API endpoints, middleware
    └── frontend/          # React 19 SPA
```

## Commands

```bash
# Build & Run
dotnet build src/NOIR.sln
dotnet run --project src/NOIR.Web
dotnet watch --project src/NOIR.Web

# Tests (1,808+)
dotnet test src/NOIR.sln

# Database Migrations
dotnet ef migrations add NAME --project src/NOIR.Infrastructure --startup-project src/NOIR.Web
dotnet ef database update --project src/NOIR.Infrastructure --startup-project src/NOIR.Web

# Frontend
cd src/NOIR.Web/frontend
npm install && npm run dev
npm run generate:api          # Sync types from backend
```

## Critical Rules

1. **Use Specifications for all queries** - Never raw `DbSet` queries in services
2. **Tag all specifications** - Include `TagWith("MethodName")` for SQL debugging
3. **Soft delete only** - Never hard delete unless explicitly GDPR-required
4. **No using statements** - Add to `GlobalUsings.cs` in each project
5. **Marker interfaces for DI** - Use `IScopedService`, `ITransientService`, `ISingletonService`
6. **Run tests before committing** - `dotnet test src/NOIR.sln`

## Code Patterns

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

### Wolverine Handlers
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

### DI Registration
```csharp
// Just add marker interface - auto-registered via Scrutor
public class CustomerService : ICustomerService, IScopedService { }
```

## Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| Specification | `[Entity][Filter]Spec` | `ActiveCustomersSpec` |
| Command | `[Action][Entity]Command` | `CreateOrderCommand` |
| Query | `Get[Entity][Filter]Query` | `GetActiveUsersQuery` |
| Handler | `[Command]Handler` | `CreateOrderHandler` |
| Configuration | `[Entity]Configuration` | `CustomerConfiguration` |

## File Boundaries

**Read/Modify Freely:**
- `src/` - All source code
- `tests/` - Test projects
- `docs/` - Documentation

**Avoid Modifying:**
- `*.Designer.cs` - Auto-generated
- `Migrations/` - EF Core auto-generated

## Documentation

| Topic | Location |
|-------|----------|
| Backend patterns | `docs/backend/patterns/` |
| Frontend guide | `docs/frontend/` |
| Architecture decisions | `docs/decisions/` |
| Setup guide | `SETUP.md` |

## Admin Credentials

- **Email:** `admin@noir.local`
- **Password:** `123qwe`
