# NOIR — Project Index

> Last updated: 2026-04-18 | .NET 10 + React 19 Enterprise SaaS | Multi-Tenant | Clean Architecture

First-time setup: [.claude/ONBOARDING.md](.claude/ONBOARDING.md) · [SETUP.md](SETUP.md)

---

## Stack

| Layer | Tech |
|-------|------|
| Backend | .NET 10, ASP.NET Core, EF Core 10, Wolverine 5, SignalR |
| Frontend | React 19, TypeScript 5.9, Vite 7, Tailwind CSS 4, TanStack Query 5 |
| Database | SQL Server 2022 (LocalDB on Windows, Docker image on macOS/Linux) |
| Testing | xUnit + Vitest 4 + Playwright |
| Package Manager | pnpm 10.28.1 (frontend) |

---

## Project Structure

```
src/
  NOIR.Domain/          # Entities, Value Objects, Domain Events, Enums
  NOIR.Application/     # Features (Commands/Queries/Handlers/Validators), DTOs
  NOIR.Infrastructure/  # EF Core, Repositories, Service Implementations
  NOIR.Web/             # Endpoints, Middleware, SignalR Hubs, MCP server, Program.cs
    frontend/           # React 19 SPA
      src/portal-app/   # 56 pages across feature modules
      src/uikit/        # 101 components + 99 stories (@uikit alias)
      src/hooks/        # 44 custom hooks
      src/services/     # API services (auto-generated from OpenAPI)
      public/locales/   # i18n: EN + VI
tests/
  NOIR.Domain.UnitTests/       # 2,971 tests
  NOIR.Application.UnitTests/  # 8,557 tests
  NOIR.IntegrationTests/       # 1,141 tests
  NOIR.ArchitectureTests/      # 46 tests
docs/
  backend/patterns/    # 20+ backend patterns
  frontend/            # Design standards, architecture, hooks reference
  designs/             # Module design specs (HR, CRM, PM, Calendar, Enterprise DataTable)
  decisions/           # ADRs
.claude/
  rules/               # 20 project rules auto-loaded into every conversation
  skills/              # 9 NOIR-specific skills (noir-qa, noir-feature-add, etc.)
  settings.json        # Declared plugins + marketplaces for team consistency
  ONBOARDING.md        # AI tooling setup guide
```

---

## Quick Start

```bash
./start-dev.sh                            # Auto-start all services
dotnet build src/NOIR.sln                 # Build backend
dotnet test src/NOIR.sln                  # 12,791 tests
cd src/NOIR.Web/frontend && pnpm run dev  # Frontend dev server
```

| Service | URL |
|---------|-----|
| Frontend | http://localhost:3000 |
| Backend API | http://localhost:4000 |
| API Docs | http://localhost:4000/api/docs |
| Storybook | http://localhost:6006 |

**Credentials**: `platform@noir.local` / `admin@noir.local` → `123qwe`

---

## Feature Modules (39)

**E-Commerce**: Products, Brands, Attributes, Cart, Checkout, Orders, Payments, Shipping, Inventory, Reviews, Wishlists, Promotions, Reports, Dashboard, Search

**ERP**: HR (Employee/Department/Tags/OrgChart), CRM (Contact/Company/Lead/Pipeline), PM (Project/Task/Kanban)

**Admin**: Users, Roles, Permissions, Tenants, Settings, EmailTemplates, Webhooks, Media, Blog, LegalPages, FeatureManagement (33 toggleable modules)

---

## Key Patterns

| Pattern | Where |
|---------|-------|
| Command + Handler + Validator co-located | `Features/{F}/Commands/{Action}/` |
| Specification with TagWith() | All queries — never raw DbSet |
| IUnitOfWork.SaveChangesAsync() | Required after every mutation |
| Soft delete only | `IsDeleted` flag on entities |
| Marker interfaces for DI | `IScopedService` / `ITransientService` / `ISingletonService` |
| IAuditableCommand | All user-facing mutations (Activity Timeline) |
| URL-synced dialogs | `useUrlDialog` / `useUrlEditDialog` / `useUrlTab` |
| Real-time updates | SignalR `EntityUpdateSignal` on CRUD |
| Feature gating | `RequireFeature(ModuleNames.X.Y)` |

---

## Migration Commands

```bash
# ApplicationDbContext → Migrations/App
dotnet ef migrations add NAME --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context ApplicationDbContext --output-dir Migrations/App

# TenantStoreDbContext → Migrations/Tenant
dotnet ef migrations add NAME --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context TenantStoreDbContext --output-dir Migrations/Tenant
```

---

## Statistics

| Metric | Value |
|--------|-------|
| Backend tests | **12,715** (2,971 domain + 8,557 application + 46 architecture + 1,141 integration) |
| Frontend tests | 154 (Vitest) + 677 (Storybook browser) |
| UI/UX audit | Playwright-based, 52 pages, 11 rules + axe-core |
| Feature modules | 35 module definitions |
| UIKit components | 99 stories across 100 component directories |
| API endpoint groups | 53 |
| MCP tools | 62 AI-callable tools + 6 prompts + 7 resources |

---

## Docs Index

| Topic | Path |
|-------|------|
| Full instructions | `CLAUDE.md` |
| Agent guide | `AGENTS.md` |
| Backend patterns | `docs/backend/patterns/` |
| Frontend standards | `docs/frontend/design-standards.md` |
| Frontend architecture | `docs/frontend/architecture.md` |
| Documentation index | `docs/DOCUMENTATION_INDEX.md` |
| Roadmap | `docs/roadmap.md` |
