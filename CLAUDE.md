# NOIR — Claude Code Instructions

> Universal AI instructions: [AGENTS.md](AGENTS.md). First-time setup: [.claude/ONBOARDING.md](.claude/ONBOARDING.md). Version 4.2 (2026-04-18).
>
> Deep guidance for every rule below lives in `.claude/rules/*.md` (20 files, auto-loaded). Rules here are the terse source-of-truth; rule files have examples + background. Never duplicate rule-file content here.

## SuperClaude routing

Natural language → skill. Available: `/sc:help` · `/sc:recommend "task"`.

| Task | Skill |
|---|---|
| New feature (Sidebar+Permissions+OpenAPI+Modules+MCP) | `noir-feature-add` |
| EF migration (needs correct `--context`) | `noir-migration` |
| Form (dialog/page) | `noir-form-scaffold` |
| List page with DataTable | `noir-datatable-page` |
| Expose feature to AI | `noir-mcp-tool-add` |
| SEO audit / meta tags / structured data | `noir-seo-check` + `seo-audit` (marketing-skills) |
| UI/UX design work | `/ui-ux-pro-max` |
| .NET patterns (EF, Aspire, perf, testing) | `dotnet-skills:*` |
| Marketing (CRO, copywriting, email, pricing) | `marketing-skills:*` |

---

## The 32 rules

### Core

1. **Read 2–3 similar files before writing new code.**
2. **All queries via Specifications.** Never raw `DbSet`. Every spec: `.TagWith("MethodName")`.
3. **`dotnet build src/NOIR.sln` after any C# change.**
4. **Soft delete only.** Hard delete only on explicit GDPR request.

### DI

5. **No `using` in files.** Add to `GlobalUsings.cs` per project.
6. **Marker interfaces for DI:** `IScopedService`, `ITransientService`, `ISingletonService`. Scrutor auto-registers.

### Data access

7. **`IUnitOfWork.SaveChangesAsync()` after every mutation.** Repos do not auto-save. Never inject `ApplicationDbContext`.
8. **Specs default to `AsNoTracking`.** Add `.AsTracking()` on queries whose results you'll modify.
9. **`.AsSplitQuery()` when Include-ing multiple collections** — prevents cartesian explosion.

### Architecture

10. **Co-locate Command + Handler + Validator** in `Application/Features/{Feature}/Commands|Queries/{Action}/`.

### Audit

11. **User-facing mutations implement `IAuditableCommand<T>`.** Endpoint sets `UserId` (ecommerce) or `AuditUserId` (CRM/HR/PM/Customers); frontend calls `usePageContext('PageName')`. See `docs/backend/patterns/hierarchical-audit-logging.md`.
12. **Update commands register a before-state resolver** in `DependencyInjection.cs` via `AddBeforeStateResolver<TDto, TQuery>(...)`. Missing → Activity Timeline shows "No handler diff available".

### Serialization

13. **Enums as strings** across HTTP JSON, SignalR, source generator. See `docs/backend/patterns/json-enum-serialization.md`.

### Security

14. **OTP follows `PasswordResetService.cs`:** cooldown → return existing session; same target post-cooldown → `ResendOtpInternalAsync`; different target → mark old used + new session. Frontend clears OTP input on error via `useEffect`; use refs for sessionToken.

### Errors

15. **`Error.Validation(propertyName, message, code?)`** — property first. Reversed args show error code instead of message.

### Email

16. **Email templates are DB-driven.** `EmailTemplate` table, not .cshtml. Never create files in `NOIR.Web/EmailTemplates/`.

### Multi-tenancy

17. **Platform admins: `IsSystemUser = true` + `TenantId = null`.** `TenantIdSetterInterceptor` enforces.
18. **Unique indexes include `TenantId`:** `HasIndex(e => new { e.Slug, e.TenantId }).IsUnique()`. Exceptions: security tokens, correlation IDs, system entities, junction tables.

### Testing

19. **All tests must pass.** `dotnet test src/NOIR.sln` after every change.
20. **New features need tests** — unit (`Application|Domain.UnitTests`) + integration (`IntegrationTests`).
21. **New repo → DI verification test.** `Infrastructure/Persistence/Repositories/{Entity}Repository.cs` + assertion in `RepositoryRegistrationTests.cs`.
22. **100% endpoint integration coverage.** `tests/NOIR.IntegrationTests/Endpoints/{Feature}EndpointsTests.cs`: happy path + 401 + 400/404. Pattern: `[Collection("Integration")] + IClassFixture<CustomWebApplicationFactory>`.

### Migrations

23. **Always pass `--context`.** `ApplicationDbContext` → `Migrations/App`; `TenantStoreDbContext` → `Migrations/Tenant`. Use `noir-migration` skill.

### Pre-push

24. **Frontend build must pass strict mode** before push. `cd src/NOIR.Web/frontend && pnpm run build`. Pre-push hook: `.git/hooks/pre-push`.

### MCP server

25. **Tool name: `noir_{domain}_{action}`.** Always explicit `[McpServerTool(Name = "...")]`, never method-name default.
26. **`[RequiresModule]` on the tool class**, not per method. `McpServiceRegistration.cs` enforces.
27. **GUIDs and enums as strings in signature** — AI clients send strings. Parse inside.
28. **`ListToolsResult` is NOT a record.** Mutate `result.Tools` directly; `with { ... }` fails.
29. **Check audit ID field per feature** — `UserId` (Orders/Blog) vs `AuditUserId` (CRM/HR/PM/Customers). Silent compile with wrong one = NULL audit.
30. **When modifying a query/command, grep MCP tools:** `grep -r "new XxxQuery\|new XxxCommand" src/NOIR.Web/Mcp/`. New features need OpenAPI tag **AND** MCP tool — see `.claude/rules/feature-registry-sync.md`.

### UI audit

31. **UI audit:** `cd src/NOIR.Web/frontend/e2e && npx playwright test --project=ui-audit --project=ui-audit-platform`. 52 admin + 4 platform pages, 11 custom rules + axe-core. Output in `.ui-audit/` (gitignored). `claude < .ui-audit/prompt.md` for batch fixes.

### QA

32. **`/noir-qa`** runs 5-phase pipeline (git diff → test cases → flows → browser exec → fix-retest). Targeted: `/noir-qa test <feature>`. Fix mode: `/noir-qa fix`. See `.qa/README.md`.

---

## Commands

```bash
# Setup & build
./setup-claude.sh                                  # First clone: env check + restore
dotnet build src/NOIR.sln
dotnet watch --project src/NOIR.Web                # hot reload backend

# Start everything
./start-dev.sh                                     # Auto-detects OS, frees ports

# Tests  (13,546 total · 12,715 backend)
dotnet test src/NOIR.sln
cd src/NOIR.Web/frontend && pnpm test:coverage

# Frontend
cd src/NOIR.Web/frontend && pnpm install && pnpm run dev
pnpm run generate:api                              # Sync types from backend
pnpm storybook                                     # http://localhost:6006

# UI audit
cd src/NOIR.Web/frontend/e2e && npx playwright test --project=ui-audit --project=ui-audit-platform

# Migrations (ALWAYS pass --context — see rule 23 or use noir-migration skill)
dotnet ef migrations add NAME --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context ApplicationDbContext --output-dir Migrations/App
dotnet ef migrations add NAME --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context TenantStoreDbContext --output-dir Migrations/Tenant
```

**URLs:** Frontend `:3000` · API `:4000` · API Docs `:4000/api/docs` · MCP `:4000/api/mcp` · Storybook `:6006`
**Logs:** `.backend.log`, `.frontend.log`, `.storybook.log` (project root)
**Dev accounts:** `platform@noir.local` / `admin@noir.local` — password `123qwe`

**Windows native:** spawn frontend detached: `powershell -Command "Start-Process cmd -ArgumentList '/c cd /d src\NOIR.Web\frontend && pnpm run dev'"`

---

## Project structure

```
src/
  NOIR.Domain/          Entities, ISpecification, repository interfaces
  NOIR.Application/     Features/{Feature}/{Commands|Queries}/{Action}/ · DTOs · validators
  NOIR.Infrastructure/  EF Core, repos, service implementations
  NOIR.Web/             Endpoints · Middleware · Program.cs
    Mcp/                  Tools · Resources · Prompts · Filters
    frontend/             React 19 SPA (pnpm)
      src/portal-app/       56 pages, domain-driven modules
      src/uikit/            @uikit — 101 components + 99 stories
      src/hooks/            44 custom hooks
      src/services/         API clients (generated + manual)
      src/contexts/         Auth, Regional, Theme
      src/validation/       Zod schemas (generated from FluentValidation)
      src/i18n/             i18next — EN + VI
tests/                  Domain · Application · Integration · Architecture
.claude/
  rules/                20 auto-loaded rule files (source of truth)
  skills/               10 NOIR-specific skills
  settings.json         Declared plugins + marketplaces
  ONBOARDING.md         AI tooling setup guide
```

**Read/Modify:** `src/`, `tests/`, `docs/`, `.claude/`. **Avoid:** `*.Designer.cs`, `Migrations/` (auto-generated).

---

## Naming

| Type | Pattern | Example |
|---|---|---|
| Spec | `[Entity][Filter]Spec` | `ActiveCustomersSpec` |
| Command | `[Action][Entity]Command` | `CreateOrderCommand` |
| Query | `Get[Entity][Filter]Query` | `GetActiveUsersQuery` |
| Handler | `[Command]Handler` | `CreateOrderCommandHandler` |
| EF Config | `[Entity]Configuration` | `CustomerConfiguration` |
| MCP Tool | `noir_{domain}_{action}` | `noir_orders_ship` |

---

## Frontend rules (React 19 + TypeScript)

**Code style:** arrow functions only (`export const X = () => {}`); named exports preferred (exception: React.lazy pages).

**Non-negotiable — every interactive element:** `cursor-pointer`. Every icon-only button: contextual `aria-label`. Every destructive action: confirmation dialog.

**Forms:** use `useValidatedForm` (bundles `mode: 'onBlur'` + `reValidateMode: 'onChange'` + `requiredFields` + `handleFormError` + `FormErrorBanner`). Never `toast.error` for form submits. See `.claude/rules/form-validation-standard.md`.

**Table list pages:** `useEnterpriseTable` + `DataTable`. Card `gap-0`, CardHeader `pb-3`, CardContent `space-y-3`. Actions column first (44px), Select second (40px), audit columns last. See `.claude/rules/datatable-standard.md` + `table-list-standard.md` + `audit-columns-standard.md`.

**Dialogs:** Use `Credenza` (not `AlertDialog`). No built-in X — every dialog needs `CredenzaFooter` with Close/Cancel. Destructive: `border-destructive/30`.

**Status badges:** `variant="outline"` + `getStatusBadgeClasses('green'|'gray'|'red'|...)`. Never `variant="default"/"secondary"`.

**Empty states:** `<EmptyState icon title description />` from `@uikit`. Never plain `text-muted-foreground` div.

**Dates:** `useRegionalSettings().formatDateTime` in tables. `formatRelativeTime` only in timelines/comments. Never `toLocaleString`. See `.claude/rules/date-formatting.md`.

**i18n:** Every user-facing string via `t()`. Add keys to BOTH `en/common.json` AND `vi/common.json`. VI is sentence case, pure Vietnamese (no "Bài viết Blog"). See `.claude/rules/localization-check.md` + `sidebar-naming-convention.md`.

**Component-based design:** Never raw HTML input/table/button when `@uikit` has it. See `.claude/rules/component-based-design.md`.

**Tight gotchas (bug-specific, not covered elsewhere):**
- Multi-select dropdown stays open: `onSelect={(e) => e.preventDefault()}` on `DropdownMenuCheckboxItem`
- Dialog focus ring clipping: never wrap form inputs in `overflow-hidden` / `ScrollArea` / `overflow-y-auto`
- Gradient text: must include `text-transparent` with `bg-clip-text`
- Mermaid labels: `<br/>`, never `\n` (GitHub renders as HTML)
- Radix Checkbox bulk ops (60+): use `LightCheckbox` pattern from `PermissionPicker.tsx` — Radix Presence causes infinite re-renders

**Branding:** Orbital logo = 3 concentric SVG circles with `.orbital-animated`. Light bg: `stroke="currentColor" text-primary`. Dark: `stroke="white" strokeOpacity="0.9"`. Sidebar: `text-sidebar-primary`. Always `aria-hidden="true"` on decorative logos.

**PWA** already configured (`public/manifest.json` + `index.html` meta). Do not duplicate.

**References:** [docs/frontend/design-standards.md](docs/frontend/design-standards.md) · [architecture.md](docs/frontend/architecture.md) · [hooks-reference.md](docs/frontend/hooks-reference.md).

---

## Domain map (all Complete unless marked)

### E-commerce — `Features/`

Products (variants, 13 attribute types, faceted search), ProductAttributes, Cart (guest + auth merge on login), Checkout (30min session), Orders (10-step lifecycle, cancel/return), Payments, Shipping, Inventory (receipts: StockIn RCV- / StockOut SHP-), Reviews (moderation), Wishlists, Customers, CustomerGroups, Promotions (code/fixed, usage limits, date range), Reports, Webhooks. Dashboard: 7 metrics via `Task.WhenAll`.

### ERP — `Features/`

HR (Employee EMP-, Department tree, 7 tag categories, org chart via xyflow), CRM (Contact, Company, Lead, Pipeline, Activity — Kanban), PM (Project PRJ-, Kanban, subtasks/labels/comments). **Calendar:** Design Ready only (see `docs/designs/module-calendar.md`).

### Platform

35 feature modules (8 core + 27 toggleable) · SSE for job progress · SignalR for real-time entity signals (`IEntityUpdateHubContext` in 145 handlers).

---

## Documentation

| Topic | Location |
|---|---|
| Index | `docs/DOCUMENTATION_INDEX.md` |
| Knowledge base | `docs/KNOWLEDGE_BASE.md` |
| Backend patterns | `docs/backend/patterns/` (20+ files) |
| MCP server (62 tools · 6 prompts · 7 resources) | `docs/backend/patterns/mcp-server.md` |
| Frontend | `docs/frontend/` |
| Hooks reference (44 hooks) | `docs/frontend/hooks-reference.md` |
| ADRs | `docs/decisions/` |
| Module designs | `docs/designs/` (HR ✅ · CRM ✅ · PM ✅ · Calendar 📋) |
| Research / future work | `docs/backend/research/` |
| QA lessons | `docs/qa/` |
| Roadmap | `docs/roadmap.md` |

---

> Changelog: [CHANGELOG.md](CHANGELOG.md) · current version 4.2 (2026-04-18). Rules philosophy: every rule prevents a documented bug (see `.claude/rules/cto-team.md`). Kill test for new rules: if you can't name the bug, don't add it.
