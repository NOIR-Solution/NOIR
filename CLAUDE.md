# NOIR - Claude Code Instructions

> For universal AI agent instructions, see [AGENTS.md](AGENTS.md). Version 3.9 (2026-03-21).

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
22. **100% API endpoint integration test coverage** - Every endpoint MUST have integration tests in `tests/NOIR.IntegrationTests/Endpoints/{Feature}EndpointsTests.cs`. Required test cases per endpoint: (a) happy path, (b) unauthenticated → 401, (c) invalid input → 400/404. New features MUST include integration tests before merging. Pattern: `[Collection("Integration")] + IClassFixture<CustomWebApplicationFactory>`.

### Database Migrations

23. **Always specify --context** - `--context ApplicationDbContext` (→ `Migrations/App`) or `--context TenantStoreDbContext` (→ `Migrations/Tenant`). See Quick Reference.

### Pre-Push

24. **Run frontend build before push** - `cd src/NOIR.Web/frontend && pnpm run build`. CI runs strict mode. Pre-push hook at `.git/hooks/pre-push`.

### MCP Server

25. **MCP tool naming** — `noir_{domain}_{action}` (e.g. `noir_orders_ship`, `noir_crm_leads_win`). Always set explicit `Name` in `[McpServerTool(Name = "...")]` — never rely on method name default.
26. **Always add `[RequiresModule]` to tool classes** — The filter in `McpServiceRegistration.cs` enforces it automatically; no per-method checks needed.
27. **Accept strings for GUIDs and enums** — AI clients send strings. Parse with `Guid.Parse(id)` and `Enum.TryParse<T>(value, true, out var e)`.
28. **`ListToolsResult` is NOT a record** — Cannot use `result with { Tools = ... }`. Mutate `result.Tools` directly; it's a settable `IList<Tool>`.
29. **Audit commands in MCP tools** — Check whether the command uses `UserId` (Orders, Blog) or `AuditUserId` (CRM, HR, PM, Customers) — they differ by feature.
30. **OpenAPI + MCP consistency** — When modifying a query/command constructor or adding a new capability, `grep -r "new XxxQuery\|new XxxCommand" src/NOIR.Web/Mcp/` to find affected tools and update them. New features need both OpenAPI tags AND MCP tools (see `.claude/rules/feature-registry-sync.md`).

### UI Audit

31. **UI/UX audit automation** — `cd src/NOIR.Web/frontend/e2e && npx playwright test --project=ui-audit --project=ui-audit-platform`. Crawls 52 admin + 4 platform pages (56 total) with 11 custom rules + axe-core. Output in `.ui-audit/` (gitignored). Feed `claude < .ui-audit/prompt.md` for batch fixes.

See `docs/backend/patterns/mcp-server.md` for full guide including prompts, resources, and SDK gotchas.

---

## Quick Reference

```bash
# Build & Run
dotnet build src/NOIR.sln
dotnet run --project src/NOIR.Web
dotnet watch --project src/NOIR.Web        # hot reload

# Tests (12,715 backend · 13,546 total)
dotnet test src/NOIR.sln
dotnet test src/NOIR.sln --collect:"XPlat Code Coverage"

# Frontend
cd src/NOIR.Web/frontend && pnpm install && pnpm run dev
pnpm run generate:api                      # Sync types from backend
cd e2e && npx playwright test --project=ui-audit --project=ui-audit-platform  # UI/UX consistency audit

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
| **MCP Server** | http://localhost:4000/api/mcp |
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
    └── Mcp/             # MCP server — Tools/, Resources/, Prompts/, Filters/
    └── frontend/         # React 19 SPA (pnpm)
        ├── src/portal-app/      # Domain-driven feature modules (56 pages)
        ├── src/uikit/           # UI components + stories (@uikit, 98 components)
        ├── src/components/      # Shared app components
        ├── src/hooks/           # Custom hooks (41+)
        ├── src/services/        # API service clients (generated + manual)
        ├── src/contexts/        # React Context providers (Auth, Regional, Theme)
        ├── src/lib/             # Utility functions
        ├── src/layouts/         # Page layouts
        ├── src/types/           # TypeScript type definitions
        ├── src/validation/      # Zod schemas (generated from backend)
        └── src/i18n/            # i18next configuration (EN + VI)
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
| MCP Tool | `noir_{domain}_{action}` | `noir_orders_ship` |

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

**Dialog footer spacing**: `CredenzaFooter` has `mt-4` built-in. Do NOT add `space-y-4` to `<form>` just to get footer spacing — it's already handled. If using raw `DialogFooter` (not Credenza), wrap the `<form>` with `className="space-y-4"` or add `mt-4` to the footer manually.

**Dialog close convention**: No built-in X button. Every dialog MUST have `CredenzaFooter`/`DialogFooter` with Close/Cancel button. Users close via footer button, click-outside, or ESC. See `.claude/rules/dialog-header-spacing.md`.

**No native `title=` tooltips**: Use Radix `<Tooltip>` from `@uikit` instead of HTML `title` attribute. Add `aria-label` for accessibility.

**Gradient text**: MUST include `text-transparent` with `bg-clip-text`.

**Card shadows**: `shadow-sm hover:shadow-lg transition-all duration-300`.

**Radix Checkbox bulk operations**: 60+ Radix Checkboxes changing state simultaneously causes "Maximum update depth exceeded" (Radix Presence `setNode` in ref callback). Use `LightCheckbox` from `PermissionPicker.tsx` pattern — a plain `<button role="checkbox">` with conditional icon, no Radix Presence.

**Mermaid node labels**: Use `<br/>` for line breaks, NEVER `\n`. GitHub renders node text as HTML — `\n` shows as literal text. Wrong: `["Title\nSubtitle"]`. Right: `["Title<br/>Subtitle"]`.

**Pagination spacing**: `CardContent` wrapping DataTable + DataTablePagination MUST have `space-y-3`. When using ternary for opacity transitions, include `space-y-3` in BOTH branches.

**Checkbox centering**: DataTable handles `meta.align === 'center'` columns automatically (zero td padding + flex wrapper). Never add manual padding to actions or select column cells.

### Validation

**Stack**: react-hook-form + Zod + `mode: 'onBlur'` + `reValidateMode: 'onChange'`. Every `useForm()` MUST have both. Full pattern: `.claude/rules/form-validation-standard.md`.

**Required field asterisk**: Auto-detected — pass `requiredFields={useMemo(() => getRequiredFields(schema), [schema])}` to `<Form>`. Never hardcode `required` prop on labels.

**Server errors**: `handleFormError(err, form, setServerErrors, t)` in catch block + `<FormErrorBanner>` at top of form. Never `toast.error()` for form submit failures.

**Zod `.issues`** (not `.errors`): `result.error.issues.forEach(...)` — `.errors` does not exist and throws.

**i18n schema factories**: `zodResolver(createSchema(t)) as unknown as Resolver<FormData>` — `z.default()` causes type mismatch. Never use `as any`.

**Convenience**: `useValidatedForm` from `@/hooks/useValidatedForm` bundles all 4 rules (mode, reValidateMode, requiredFields, handleFormError).

### Design Language

**ONE design language** — ALL UI must follow [docs/frontend/design-standards.md](docs/frontend/design-standards.md). Key rules:

- **Dialogs**: Use `Credenza` (not `AlertDialog`). Destructive dialogs: `border-destructive/30` on `CredenzaContent`.
- **Destructive buttons**: `variant="destructive"` + `className="cursor-pointer bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors"`.
- **Status badges**: `variant="outline"` + `getStatusBadgeClasses('green'|'gray'|'red'|...)` from `@/utils/statusBadge`. Never `variant="default"/"secondary"` for status.
- **Empty states**: Use `<EmptyState icon={X} title={t('...')} description={t('...')} />` from `@uikit`. Never plain `<div className="text-center py-8 text-muted-foreground">`.
- **Create buttons**: No `shadow-lg hover:shadow-xl`. Use `className="group transition-all duration-300"`.
- **Table list pages**: MUST use `useEnterpriseTable` + `DataTable` (TanStack Table) — no custom tables. Card `gap-0` + CardHeader `pb-3` + CardContent `space-y-3`. Always show `CardDescription` with "Showing X of Y items". Pass `defaultPageSize` to `DataTablePagination`. See `.claude/rules/table-list-standard.md`, `.claude/rules/datatable-standard.md`.
- **Form spacing**: `space-y-4` in dialog bodies. Never `space-y-5`.
- **No gradient buttons**: Never use `bg-gradient-to-r` on standard action buttons. Use default Button variants.

### Branding & Logo

**Orbital logo mark**: 3 concentric SVG circles. Always use `.orbital-animated` CSS class for animation.
- Light backgrounds: `stroke="currentColor"` + `className="... text-primary"` (auto-adapts to light/dark `--primary`)
- Dark/gradient panels: `stroke="white"` with `strokeOpacity="0.9"`
- Sidebar: `stroke="currentColor"` + `className="text-sidebar-primary"`
- Always include `aria-hidden="true"` on decorative logo SVGs

**PWA**: `public/manifest.json` + `index.html` meta tags already configured. Do not duplicate or override.

### Patterns (Reference)

For TanStack Query hooks, `useEnterpriseTable` (unified table hook), React 19 performance patterns (useDeferredValue, useTransition, optimistic mutations), and UI standardization patterns, see [docs/frontend/architecture.md](docs/frontend/architecture.md). Hook reference: [docs/frontend/hooks-reference.md](docs/frontend/hooks-reference.md). Check existing code per Rule 1.

---

## Storybook & UIKit

**98 component stories** in `src/uikit/{component}/`. Config: `.storybook/main.ts` (React + Vite + Tailwind CSS 4).

```bash
cd src/NOIR.Web/frontend && pnpm storybook       # Dev: http://localhost:6006
cd src/NOIR.Web/frontend && pnpm build-storybook  # Build check
```

- `@uikit` alias → `src/uikit/` (tsconfig.app.json)
- Stories excluded from prod build
- Components and stories co-located in `src/uikit/{component}/`

---

## E-commerce Domain Map

| Domain | Location | Key Entities | Status |
|--------|----------|-------------|--------|
| **Products** | `Features/Products/` | Product → Variant (SKU, price, inventory) → Image. Status: Draft → Active → Archived | Complete |
| **Attributes** | `Features/ProductAttributes/` | 13 types. ProductFilterIndex for faceted search. FilterAnalyticsEvent for tracking. | Complete |
| **Cart** | `Features/Cart/` | Guest (SessionId) + Auth user. MergeCartCommand on login. Status: Active → Converted/Abandoned | Complete |
| **Checkout** | `Features/Checkout/` | Accordion: Address → Shipping → Payment → Complete. 30min session expiry. | Complete |
| **Orders** | `Features/Orders/` | Pending → Confirmed → Processing → Shipped → Delivered → Completed. Cancel/Return with inventory. 10 lifecycle commands. | Complete |
| **Payments** | `Features/Payments/` | PaymentTransaction tracking, status timeline, order payments query. Embedded in Order Detail. | Complete |
| **Shipping** | `Features/Shipping/` | Provider integrations, tracking timeline, carrier management. Embedded in Order Detail. | Complete |
| **Inventory** | `Features/Inventory/` | Receipt system (phieu nhap/xuat). Draft → Confirmed/Cancelled. Types: StockIn (RCV-), StockOut (SHP-). | Complete |
| **Reviews** | `Features/Reviews/` | Product reviews with moderation. Approve/Reject workflow. | Complete |
| **Wishlists** | `Features/Wishlists/` | User wishlists with analytics tracking. | Complete |
| **Dashboard** | `Features/Dashboard/` | 7 metrics via Task.WhenAll(). Revenue excludes Cancelled/Refunded. 4 widget groups (E-commerce, CRM, feature-gated). | Complete |
| **Customers** | `Features/Customers/` | Customer profiles with addresses, order history. Detail page with timeline. | Complete |
| **Customer Groups** | `Features/CustomerGroups/` | Segmentation groups with rule-based membership. | Complete |
| **Promotions** | `Features/Promotions/` | Discount codes, percentage/fixed, usage limits, date ranges. | Complete |
| **Reports** | `Features/Reports/` | Revenue, orders, inventory, product performance analytics. | Complete |
| **Webhooks** | `Features/Webhooks/` | Outbound webhook subscriptions with event filtering and delivery tracking. | Complete |
| **Feature Mgmt** | `Application/Modules/` | 35 modules (8 core + 27 toggleable). Platform availability + tenant enable. | Complete |
| **SSE** | `Infrastructure/Sse/` | Server-Sent Events for real-time job progress and operation updates. | Complete |

---

## ERP Module Map

| Module | Location | Key Entities | Status |
|--------|----------|-------------|--------|
| **HR** | `Features/Hr/` | Employee (auto-code EMP-), Department (tree), EmployeeTag (7 categories). Org chart, bulk ops, import/export, reports. | Complete |
| **CRM** | `Features/Crm/` | Contact, Company, Lead, Pipeline, Stage, Activity. Kanban board, dashboard widgets, domain events. | Complete |
| **PM** | `Features/Pm/` | Project (auto-code PRJ-), ProjectColumn, ProjectTask, Label, Comment, Attachment. Kanban, task list, subtasks. | Complete |
| **Calendar** | — | CalendarEvent, Attendee, RecurrenceRule. Shared calendars, resource booking. | Design Ready |

---

## Documentation

| Topic | Location |
|-------|----------|
| **Index** | `docs/DOCUMENTATION_INDEX.md` |
| **Knowledge Base** | `docs/KNOWLEDGE_BASE.md` |
| **Backend Patterns** | `docs/backend/patterns/` |
| **MCP Server** | `docs/backend/patterns/mcp-server.md` (55 tools, 6 prompts, 7 resources) |
| **Frontend Guide** | `docs/frontend/` |
| **Hooks Reference** | `docs/frontend/hooks-reference.md` (41+ hooks) |
| **Architecture Decisions** | `docs/decisions/` |
| **Module Designs** | `docs/designs/` (HR ✅, CRM ✅, PM ✅, Calendar 📋) |
| **QA Lessons** | `docs/qa/` |
| **Roadmap** | `docs/roadmap.md` |

Research reports → `docs/backend/research/`.

## File Boundaries

- **Read/Modify:** `src/`, `tests/`, `docs/`, `.claude/`
- **Avoid:** `*.Designer.cs`, `Migrations/` (auto-generated)

---

> Changelog: [CHANGELOG.md](CHANGELOG.md). Current version: 3.9 (2026-03-21).
