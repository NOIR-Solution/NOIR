# NOIR

> Enterprise-ready .NET 10 + React SaaS foundation with multi-tenancy, Clean Architecture, and comprehensive testing.

## Quick Start

```bash
# Build
dotnet build src/NOIR.sln

# Run
dotnet run --project src/NOIR.Web

# Test
dotnet test src/NOIR.sln

# Admin login: admin@noir.local / 123qwe
```

## Project Structure

```
NOIR/
├── src/
│   ├── NOIR.Domain/           # Entities, interfaces, domain logic
│   ├── NOIR.Application/      # Commands, queries, DTOs, specifications
│   ├── NOIR.Infrastructure/   # EF Core, handlers, external services
│   └── NOIR.Web/              # API endpoints, middleware
│       └── frontend/          # React SPA
├── tests/                     # 1,739+ tests
├── docs/                      # Documentation
│   ├── backend/               # Backend patterns & guides
│   ├── frontend/              # Frontend architecture & guides
│   └── decisions/             # Architecture Decision Records
└── .claude/                   # Claude Code commands
```

## Key Patterns

### Backend (.NET)

1. **Specifications for queries** - Never use raw `DbSet` queries
   ```csharp
   var spec = new ActiveCustomersSpec();
   return await _repository.ListAsync(spec);
   ```

2. **Tag all specifications** for SQL debugging
   ```csharp
   Query.Where(x => x.IsActive)
        .TagWith("GetActiveCustomers");
   ```

3. **Marker interfaces for DI** - Auto-registered via Scrutor
   ```csharp
   public class CustomerService : ICustomerService, IScopedService { }
   ```

4. **Wolverine handlers** - Static classes, no interfaces
   ```csharp
   public static class CreateOrderHandler
   {
       public static async Task<OrderResponse> Handle(
           CreateOrderCommand cmd, IRepository<Order, Guid> repo, CancellationToken ct)
       { ... }
   }
   ```

5. **Soft delete only** - Never hard delete unless GDPR-required

### Frontend (React)

1. **Import alias** - Use `@/` for src/ imports
   ```tsx
   import { Button } from '@/components/ui/button'
   ```

2. **Type sync** - Generate types from backend OpenAPI
   ```bash
   npm run generate:api
   ```

3. **21st.dev for components** - AI-assisted component generation
   ```
   /ui create a login form with validation
   ```

## Documentation

| Topic | Location |
|-------|----------|
| Backend Overview | `docs/backend/README.md` |
| Frontend Overview | `docs/frontend/README.md` |
| Architecture Decisions | `docs/decisions/` |
| Setup Guide | `SETUP.md` |
| AI Instructions | `CLAUDE.md` |

## Commands

```bash
# Database
dotnet ef migrations add NAME --project src/NOIR.Infrastructure --startup-project src/NOIR.Web
dotnet ef database update --project src/NOIR.Infrastructure --startup-project src/NOIR.Web

# Frontend
cd src/NOIR.Web/frontend
npm run dev        # Development
npm run build      # Production build
npm run lint       # Lint check
```

## Critical Rules

1. **Check existing patterns first** - Look at similar files before writing new code
2. **Use Specifications** - Never raw DbSet queries in services
3. **Tag specifications** - Always include `TagWith("MethodName")`
4. **Soft delete only** - Unless explicitly GDPR-required
5. **No using statements** - Add to GlobalUsings.cs in each project
6. **Run tests** - `dotnet test src/NOIR.sln` before committing

## Tech Stack

### Backend
- .NET 10 LTS, EF Core 10, SQL Server
- Wolverine (CQRS), FluentValidation, Mapperly
- Finbuckle.MultiTenant, Hangfire, Serilog

### Frontend
- React 19, TypeScript, Vite
- Tailwind CSS 4, shadcn/ui, React Router 7
