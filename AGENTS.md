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

# Tests (6,750+)
dotnet test src/NOIR.sln

# Database Migrations (CRITICAL: always specify --context)
dotnet ef migrations add NAME --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context ApplicationDbContext --output-dir Migrations/App
dotnet ef migrations add NAME --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context TenantStoreDbContext --output-dir Migrations/Tenant
dotnet ef database update --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context ApplicationDbContext
dotnet ef database update --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context TenantStoreDbContext

# Frontend
cd src/NOIR.Web/frontend
pnpm install && pnpm run dev
pnpm run generate:api          # Sync types from backend
```

## Critical Rules

1. **Use Specifications for all queries** - Never raw `DbSet` queries in services
2. **Tag all specifications** - Include `TagWith("MethodName")` for SQL debugging
3. **Use IUnitOfWork for persistence** - Repository methods do NOT auto-save. Call `SaveChangesAsync()` after mutations
4. **Use AsTracking for mutations** - Specifications default to `AsNoTracking`. Add `.AsTracking()` for entities you'll modify
5. **Co-locate Command + Handler + Validator** - All CQRS components in `Application/Features/{Feature}/Commands/{Action}/`
6. **Soft delete only** - Never hard delete unless explicitly GDPR-required
7. **No using statements** - Add to `GlobalUsings.cs` in each project
8. **Marker interfaces for DI** - Use `IScopedService`, `ITransientService`, `ISingletonService`
9. **Run tests before committing** - `dotnet test src/NOIR.sln`

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

### Handlers (Co-located with Commands)
```csharp
// Application/Features/Orders/Commands/Create/CreateOrderCommandHandler.cs
public class CreateOrderCommandHandler
{
    private readonly IRepository<Order, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderCommandHandler(
        IRepository<Order, Guid> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OrderDto>> Handle(
        CreateOrderCommand cmd,
        CancellationToken ct)
    {
        var order = Order.Create(cmd.CustomerId, cmd.Items);
        await _repository.AddAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);  // REQUIRED
        return Result.Success(order.ToDto());
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
| Handler | `[Command]Handler` | `CreateOrderCommandHandler` |
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
| Knowledge base | `docs/KNOWLEDGE_BASE.md` |

## Admin Credentials

| Account | Email | Password |
|---------|-------|----------|
| **Platform Admin** | `platform@noir.local` | `123qwe` |
| **Tenant Admin** | `admin@noir.local` | `123qwe` |
