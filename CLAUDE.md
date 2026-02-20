# NOIR - Claude Code Instructions

> For universal AI agent instructions, see [AGENTS.md](AGENTS.md). Version 3.0 (2026-02-21).

## SuperClaude Framework

**Just say what you need in natural language.** Available skills: `/sc:help` | `/sc:recommend "your task"`

**Routing hints:** Use `dotnet-backend-patterns` skill for C#/.NET backend work. Use `/ui-ux-pro-max` skill for all UI/UX work.

---

## Critical Rules

### Core Principles

1. **Check existing patterns first** - Look at similar files before writing new code
2. **Use Specifications for all queries** - Never raw `DbSet` queries. Always include `TagWith("MethodName")`.
3. **Run `dotnet build src/NOIR.sln`** after code changes
4. **Soft delete only** - Never hard delete unless explicitly requested for GDPR

### Dependency Injection

5. **No using statements** - Add to `GlobalUsings.cs` in each project
6. **Marker interfaces for DI** - `IScopedService`, `ITransientService`, `ISingletonService`. Auto-registered via Scrutor:
   ```csharp
   public class CustomerService : ICustomerService, IScopedService { }
   ```

### Data Access

7. **IUnitOfWork for persistence** - Repos do NOT auto-save. Always call `SaveChangesAsync()` after mutations. Never inject `ApplicationDbContext` directly.
   ```csharp
   await _repository.AddAsync(entity, ct);
   await _unitOfWork.SaveChangesAsync(ct);  // REQUIRED
   ```
8. **AsTracking for mutations** - Specs default to `AsNoTracking`. Add `.AsTracking()` for entities you'll modify:
   ```csharp
   Query.Where(c => c.Id == id).AsTracking().TagWith("CustomerById");
   ```
9. **AsSplitQuery for multiple collections** - Prevents cartesian explosion on multi-Include queries.

### Architecture

10. **Co-locate Command + Handler + Validator** - All in `Application/Features/{Feature}/Commands/{Action}/` or `Queries/{Action}/`

### Audit & Activity Timeline

11. **Audit logging for user actions** - Mutation commands via frontend MUST implement `IAuditableCommand`. Requires: (a) Command implements `IAuditableCommand<TResult>`, (b) Endpoint sets `UserId`, (c) Frontend calls `usePageContext('PageName')`. See `docs/backend/patterns/hierarchical-audit-logging.md`.
12. **Before-state resolvers for Update commands** - `IAuditableCommand<TDto>` with `OperationType.Update` MUST register a resolver in `DependencyInjection.cs`: `services.AddBeforeStateResolver<YourDto, GetYourEntityQuery>(...)`. Without this, Activity Timeline shows "No handler diff available".

### Serialization

13. **Enums serialize as strings** - Configured in HTTP JSON, SignalR, and Source Generator. See `docs/backend/patterns/json-enum-serialization.md`.

### Security

14. **OTP flow consistency** - All OTP features MUST follow `PasswordResetService.cs` pattern:
    - Cooldown active → return existing session (no new OTP)
    - Cooldown passed, same target → `ResendOtpInternalAsync` (keeps sessionToken)
    - Different target → mark old as used, create new session
    - Frontend: clear OTP input on error via `useEffect`. Use refs for sessionToken.

### Error Handling

15. **Error.Validation parameter order** - `Error.Validation(propertyName, message, code?)`. WRONG: `Error.Validation("message", errorCode)` — shows error codes instead of messages!

### Email System

16. **Email templates are database-driven** - Loaded from `EmailTemplate` table, NOT .cshtml files. NEVER create files in `src/NOIR.Web/EmailTemplates/`. Edit via DB seeder or Admin UI.

### Multi-Tenancy

17. **System users: TenantId = null** - Platform admins MUST have `IsSystemUser = true` and `TenantId = null`. The `TenantIdSetterInterceptor` protects this. See `docs/backend/architecture/tenant-id-interceptor.md`.
18. **Unique constraints MUST include TenantId** - Pattern: `builder.HasIndex(e => new { e.Slug, e.TenantId }).IsUnique()`. Exceptions: security tokens, correlation IDs, system entities, junction tables with tenant-scoped FKs.

### Testing

19. **All tests must pass** - `dotnet test src/NOIR.sln` after any change. Never leave failing tests.
20. **New features need tests** - Unit tests in `tests/NOIR.Application.UnitTests`, domain tests in `tests/NOIR.Domain.UnitTests`, integration tests in `tests/NOIR.IntegrationTests`.
21. **New repositories need DI verification test** - Create `{Entity}Repository.cs` in `Infrastructure/Persistence/Repositories/` AND a test verifying DI registration.

### Database Migrations

22. **Always specify --context** - `--context ApplicationDbContext` (→ `Migrations/App`) or `--context TenantStoreDbContext` (→ `Migrations/Tenant`). See Quick Reference.

### Pre-Push

23. **Run frontend build before push** - `cd src/NOIR.Web/frontend && pnpm run build`. CI runs strict mode. Pre-push hook at `.git/hooks/pre-push`.

---

## Quick Reference

```bash
# Build & Run
dotnet build src/NOIR.sln
dotnet run --project src/NOIR.Web
dotnet watch --project src/NOIR.Web        # hot reload

# Tests (10,595+)
dotnet test src/NOIR.sln
dotnet test src/NOIR.sln --collect:"XPlat Code Coverage"

# Frontend
cd src/NOIR.Web/frontend && pnpm install && pnpm run dev
pnpm run generate:api                      # Sync types from backend

# Migrations (CRITICAL: always specify --context)
dotnet ef migrations add NAME --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context ApplicationDbContext --output-dir Migrations/App
dotnet ef migrations add NAME --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context TenantStoreDbContext --output-dir Migrations/Tenant
dotnet ef database update --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context ApplicationDbContext
dotnet ef database update --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context TenantStoreDbContext
```

### Admin Credentials

| Account | Email | Password |
|---------|-------|----------|
| **Platform Admin** | `platform@noir.local` | `123qwe` |
| **Tenant Admin** | `admin@noir.local` | `123qwe` |

---

## Running the Website

```bash
./start-dev.sh                             # Recommended: auto-detects OS, frees ports, starts everything
```

**Manual startup** (if script fails):
```bash
cd src/NOIR.Web && dotnet run              # Terminal 1 - Backend
cd src/NOIR.Web/frontend && pnpm install && pnpm run dev  # Terminal 2 - Frontend
```

**Claude Code on Windows** — Frontend MUST use PowerShell to spawn detached (run from project root):
```bash
powershell -Command "Start-Process cmd -ArgumentList '/c cd /d src\NOIR.Web\frontend && pnpm run dev'"
```

| Service | URL |
|---------|-----|
| **Frontend** | http://localhost:3000 |
| **Backend API** | http://localhost:4000 |
| **API Docs** | http://localhost:4000/api/docs |
| **Storybook** | http://localhost:6006 |

Logs: `.backend.log`, `.frontend.log`, `.storybook.log` in project root.

---

## Project Structure

```
src/NOIR.Domain/          # Entities, IRepository, ISpecification
src/NOIR.Application/     # Features (Command + Handler + Validator co-located), DTOs
    └── Features/{Feature}/
        ├── Commands/{Action}/   # {Action}Command.cs + Handler + Validator
        └── Queries/{Action}/    # {Action}Query.cs + Handler
    └── Common/Interfaces/       # Service abstractions
src/NOIR.Infrastructure/  # EF Core, Repositories, Service implementations
src/NOIR.Web/             # Endpoints, Middleware, Program.cs
    └── frontend/         # React 19 SPA (pnpm)
        ├── src/portal-app/      # Domain-driven feature modules
        ├── src/uikit/           # UI components + stories (@uikit)
        ├── src/components/      # Shared app components
        ├── src/hooks/           # Custom hooks
        └── src/services/        # API services
```

---

## Naming Conventions

| Type | Pattern | Example |
|------|---------|---------|
| Specification | `[Entity][Filter]Spec` | `ActiveCustomersSpec` |
| Command | `[Action][Entity]Command` | `CreateOrderCommand` |
| Query | `Get[Entity][Filter]Query` | `GetActiveUsersQuery` |
| Handler | `[Command]Handler` | `CreateOrderCommandHandler` |
| Configuration | `[Entity]Configuration` | `CustomerConfiguration` |

---

## Frontend Rules (React/TypeScript)

### Code Style
- **Arrow functions only**: `export const MyComponent = () => { ... }`. ESLint enforces this.
- **Named exports preferred**: `export const X` over `export default`. Exception: React.lazy pages need both.
- Use `/ui-ux-pro-max` skill for ALL UI/UX work.

### Gotchas (Bug Prevention)

**cursor-pointer**: ALL interactive elements (Tabs, Checkboxes, Select, DropdownMenu, Switch) MUST have `cursor-pointer`.

**aria-label**: ALL icon-only buttons must have contextual `aria-label={`Delete ${item.name}`}`.

**Confirmation dialogs**: Required for ALL destructive actions.

**Multi-select dropdown stays open**: Add `onSelect={(e) => e.preventDefault()}` to `DropdownMenuCheckboxItem`.

**Dialog focus ring clipping**: NEVER wrap form inputs in `overflow-hidden`, `ScrollArea`, or `overflow-y-auto`. Let dialogs grow naturally.

**Gradient text**: MUST include `text-transparent` with `bg-clip-text`.

**Card shadows**: `shadow-sm hover:shadow-lg transition-all duration-300`.

### Validation

**Stack**: react-hook-form + Zod + `mode: 'onBlur'`. FluentValidation rules must match Zod schemas.

**Zod `.issues`** (not `.errors`): `result.error.issues.forEach(...)` — `.errors` does not exist and throws.

**i18n schema factories**: Use `zodResolver(createSchema(t)) as unknown as Resolver<FormData>` — `z.default()` causes type mismatch. Never use `as any`. See [docs/frontend/architecture.md#form-validation-standards](docs/frontend/architecture.md#form-validation-standards).

### Patterns (Reference)

For TanStack Query hooks, React 19 performance patterns (useDeferredValue, useTransition, optimistic mutations), and UI standardization patterns, see [docs/frontend/architecture.md](docs/frontend/architecture.md). Check existing code per Rule 1.

---

## Storybook & UIKit

**72 component stories** in `src/uikit/{component}/`. Config: `.storybook/main.ts` (React + Vite + Tailwind CSS 4).

```bash
cd src/NOIR.Web/frontend && pnpm storybook       # Dev: http://localhost:6006
cd src/NOIR.Web/frontend && pnpm build-storybook  # Build check
```

- `@uikit` alias → `src/uikit/` (tsconfig.app.json)
- Stories excluded from prod build
- Components live in `src/components/ui/`, stories in `src/uikit/`

---

## E-commerce Domain Map

| Domain | Location | Key Entities | Status |
|--------|----------|-------------|--------|
| **Products** | `Features/Products/` | Product → Variant (SKU, price, inventory) → Image. Status: Draft → Active → Archived | Complete |
| **Attributes** | `Features/ProductAttributes/` | 13 types. ProductFilterIndex for faceted search. FilterAnalyticsEvent for tracking. | Complete |
| **Cart** | `Features/Cart/` | Guest (SessionId) + Auth user. MergeCartCommand on login. Status: Active → Converted/Abandoned | Complete |
| **Checkout** | `Features/Checkout/` | Accordion: Address → Shipping → Payment → Complete. 30min session expiry. | Complete |
| **Orders** | `Features/Orders/` | Pending → Confirmed → Processing → Shipped → Delivered → Completed. Cancel/Return with inventory. 7 lifecycle commands. | Complete |
| **Inventory** | `Features/Inventory/` | Receipt system (phieu nhap/xuat). Draft → Confirmed/Cancelled. Types: StockIn (RCV-), StockOut (SHP-). | Complete |
| **Dashboard** | `Features/Dashboard/` | 7 metrics via Task.WhenAll(). Revenue excludes Cancelled/Refunded. | Complete |

---

## Documentation

| Topic | Location |
|-------|----------|
| **Index** | `docs/DOCUMENTATION_INDEX.md` |
| **Knowledge Base** | `docs/KNOWLEDGE_BASE.md` |
| **Backend Patterns** | `docs/backend/patterns/` |
| **Frontend Guide** | `docs/frontend/` |
| **Architecture Decisions** | `docs/decisions/` |

Research reports → `docs/backend/research/` or `docs/frontend/research/`.

## File Boundaries

- **Read/Modify:** `src/`, `tests/`, `docs/`, `.claude/`
- **Avoid:** `*.Designer.cs`, `Migrations/` (auto-generated)

---

> Changelog: [CHANGELOG.md](CHANGELOG.md). Current version: 3.0 (2026-02-21).
