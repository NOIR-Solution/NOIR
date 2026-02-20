# AGENTS.md

> Universal AI agent instructions for NOIR. Compatible with Claude Code, Cursor, Windsurf, GitHub Copilot, and other AI coding assistants.
> For detailed rules and patterns, see [CLAUDE.md](CLAUDE.md).

## Project Overview

**NOIR** is an enterprise .NET 10 + React SaaS foundation using Clean Architecture, CQRS, and DDD patterns.

```
src/
├── NOIR.Domain/           # Core entities, interfaces (no dependencies)
├── NOIR.Application/      # Commands, queries, specifications, DTOs
├── NOIR.Infrastructure/   # EF Core, handlers, external services
└── NOIR.Web/              # API endpoints, middleware
    └── frontend/          # React 19 SPA (pnpm)
```

## Commands

```bash
# Build & Run
dotnet build src/NOIR.sln
dotnet run --project src/NOIR.Web

# Tests (10,595+)
dotnet test src/NOIR.sln

# Database Migrations (CRITICAL: always specify --context)
dotnet ef migrations add NAME --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context ApplicationDbContext --output-dir Migrations/App
dotnet ef migrations add NAME --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context TenantStoreDbContext --output-dir Migrations/Tenant

# Frontend
cd src/NOIR.Web/frontend && pnpm install && pnpm run dev
pnpm run generate:api    # Sync types from backend
```

## Critical Rules (Summary — see CLAUDE.md for full 23 rules)

1. **Specifications for all queries** — Never raw `DbSet` queries. Always `TagWith("MethodName")`.
2. **IUnitOfWork for persistence** — Repos don't auto-save. Call `SaveChangesAsync()` after mutations.
3. **AsTracking for mutations** — Specs default to `AsNoTracking`. Add `.AsTracking()` for modification.
4. **Co-locate CQRS** — Command + Handler + Validator in `Application/Features/{Feature}/Commands/{Action}/`.
5. **Soft delete only** — Never hard delete unless explicitly GDPR-required.
6. **Marker interfaces for DI** — `IScopedService`, `ITransientService`, `ISingletonService`. No `using` statements — use `GlobalUsings.cs`.
7. **Run tests before committing** — `dotnet test src/NOIR.sln`

## Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| Specification | `[Entity][Filter]Spec` | `ActiveCustomersSpec` |
| Command | `[Action][Entity]Command` | `CreateOrderCommand` |
| Query | `Get[Entity][Filter]Query` | `GetActiveUsersQuery` |
| Handler | `[Command]Handler` | `CreateOrderCommandHandler` |
| Configuration | `[Entity]Configuration` | `CustomerConfiguration` |

## File Boundaries

- **Read/Modify:** `src/`, `tests/`, `docs/`, `.claude/`
- **Avoid:** `*.Designer.cs`, `Migrations/` (auto-generated)

## Admin Credentials

| Account | Email | Password |
|---------|-------|----------|
| **Platform Admin** | `platform@noir.local` | `123qwe` |
| **Tenant Admin** | `admin@noir.local` | `123qwe` |
