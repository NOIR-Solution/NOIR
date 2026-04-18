---
name: noir-feature-add
description: Scaffold a new NOIR feature/module with correct registration across all 5 registries (Sidebar, Permissions, OpenAPI, Modules, MCP Tools). Use when the user asks to add a new feature, module, domain area, or a new top-level sidebar entry. Prevents the common "added sidebar item but forgot OpenAPI tag / permission / MCP tool" class of bug covered in `.claude/rules/feature-registry-sync.md`.
---

# noir-feature-add — 5-Registry Feature Scaffold

NOIR requires **every new feature** to be registered in 5 places. Skipping any one of them produces a silent bug (API docs miss the group, role UI can't assign permission, AI agents can't call the feature, etc.). This skill walks the registration with a mandatory checklist.

## Inputs to collect upfront

Before editing any file, confirm with the user:

1. **Feature name** (PascalCase, singular): e.g. `Promotion`, `Invoice`, `Warehouse`
2. **Domain category** (determines sidebar section + module category):
   - `Dashboard` | `Marketing` | `Orders & Fulfillment` | `Customers` | `Catalog` | `Human Resources` | `Project Management` | `CRM` | `Content` | `Users & Access` | `Settings` | `System`
3. **Is this a toggleable module?** (27 toggleable vs 8 core — default: yes, toggleable)
4. **Permissions needed** (default: `Read`, `Create`, `Update`, `Delete`, `Manage`)
5. **Initial MCP tools** (default: `list`, `get` — `create`/`update`/`delete` are optional)
6. **Frontend path** (default: `/portal/{category-kebab}/{feature-kebab}s`)

If any of the above is ambiguous, ASK. Do not guess.

## The 5 registries — must ALL be updated

| # | File | What to add |
|---|---|---|
| 1 | `src/NOIR.Application/Modules/{Feature}ModuleDefinition.cs` + `ModuleNames.cs` + `ModuleCatalog.cs` | Module definition, `ModuleNames.{Category}.{Name}` constant, register in catalog |
| 2 | `src/NOIR.Domain/Common/Permissions.cs` | `{Feature}Read/Create/Update/Delete/Manage` constants, group entry, add all to `All` array in the correct section |
| 3 | `src/NOIR.Web/OpenApi/SecuritySchemeDocumentTransformer.cs` | `OpenApiTag` in `document.Tags` **AND** `x-tagGroups` JSON entry (both must match the domain category) |
| 4 | `src/NOIR.Web/frontend/src/components/portal/Sidebar.tsx` | Item in correct `navSection` with `titleKey`, `icon`, `path`, `permission`, `feature` |
| 5 | `src/NOIR.Web/Mcp/Tools/{Feature}Tools.cs` | `[McpServerToolType]` class with `[RequiresModule(ModuleNames.{Cat}.{Name})]`. Tool names follow `noir_{domain}_{action}`. At minimum: list + get. |

Plus:
- **i18n keys** in both `public/locales/en/common.json` AND `public/locales/vi/common.json` for `nav.*`, `modules.*`, `permissions.*` — pure Vietnamese (no mixed-language labels per `sidebar-naming-convention.md`)
- **Module test count** in `tests/NOIR.Application.UnitTests/Modules/ModuleCatalogTests.cs` — bump the expected count
- **Integration tests** in `tests/NOIR.IntegrationTests/Endpoints/{Feature}EndpointsTests.cs` — at minimum: happy path, 401 unauthenticated, 400/404 invalid input (Rule 22)

## Workflow (strict order)

### Phase 1 — Read patterns first (Rule 1)

Before writing anything, read **2–3 recent similar features** to match the current pattern. Good examples:
- Simple CRUD: `Brands` (`Features/Brands/`, `Web/Mcp/Tools/BrandTools.cs`, `ProductCategoryModuleDefinition.cs`)
- With workflow: `Promotions`
- CRM/HR/PM: pick the matching category's latest feature

Commands:
```bash
# Latest Module definitions
ls -t src/NOIR.Application/Modules/*.cs | head -5

# Latest MCP tool classes
ls -t src/NOIR.Web/Mcp/Tools/*.cs | head -5
```

### Phase 2 — Backend scaffolding (Domain → Application → Infrastructure → Web)

1. Entity in `src/NOIR.Domain/Entities/{Feature}.cs` (implement `IAuditableEntity` + `ITenantEntity` if scoped)
2. `{Feature}Configuration` in `src/NOIR.Infrastructure/Persistence/Configurations/` (Rule 18: unique indexes include `TenantId`)
3. Repository in `src/NOIR.Infrastructure/Persistence/Repositories/{Feature}Repository.cs` + DI verification test (Rule 21)
4. Commands + Queries co-located in `src/NOIR.Application/Features/{Feature}/{Commands|Queries}/{Action}/` (Rule 10)
5. Mutation commands that go through frontend MUST implement `IAuditableCommand<TResult>` (Rule 11)
6. `Update` commands MUST register a before-state resolver in `DependencyInjection.cs` (Rule 12)
7. Endpoints under `src/NOIR.Web/Endpoints/{Feature}Endpoints.cs`, tag with `.WithTags("...")` and gate with `.RequireFeature(ModuleNames...)`
8. Migration: `dotnet ef migrations add Add{Feature} --context ApplicationDbContext --output-dir Migrations/App` (Rule 23)

### Phase 3 — The 5 registries

Now update all 5. Refer to the table above. Use a TodoWrite list to track — one task per registry — and don't mark a registry done until you've grep'd the file to confirm the edit stuck.

### Phase 4 — Frontend

1. Run `cd src/NOIR.Web/frontend && pnpm run generate:api` to sync types + Zod schemas
2. Page components under `src/portal-app/{category}/{feature}/` — follow `table-list-standard.md` + `datatable-standard.md` + audit columns standard
3. Forms use `useValidatedForm` (Rule: form-validation-standard)
4. `usePageContext('FeatureName')` in the page component (audit logging — Rule 11)
5. URL-synced state: `useUrlTab()` / `useUrlDialog()` / `useUrlEditDialog()` per `url-tab-state.md`

### Phase 5 — Tests

1. Unit: `tests/NOIR.Application.UnitTests/Features/{Feature}/` — handler tests with mocked `IEntityUpdateHubContext` for any handler that publishes signals
2. Domain: `tests/NOIR.Domain.UnitTests/` if new value objects / invariants
3. Integration: `tests/NOIR.IntegrationTests/Endpoints/{Feature}EndpointsTests.cs` — 100% endpoint coverage (Rule 22)
4. Repository DI: `tests/NOIR.Infrastructure.UnitTests/Persistence/RepositoryRegistrationTests.cs` — add assertion for new repo
5. Module catalog: bump expected count in `ModuleCatalogTests.cs`

### Phase 6 — Quality gates (MUST pass before reporting done)

```bash
dotnet build src/NOIR.sln                          # 0 errors
dotnet test src/NOIR.sln                           # ALL pass, zero skipped
cd src/NOIR.Web/frontend && pnpm run build         # 0 errors, 0 warnings (strict)
cd src/NOIR.Web/frontend && pnpm build-storybook   # 0 errors (if UIKit touched)
```

Plus manual verification via browser (Rule: UI changes need browser test):
- New feature appears in sidebar (check both EN and VI)
- Permission shows up in Role editor
- API docs show tag in correct `x-tagGroups` section
- Feature toggle visible in Settings > Features
- MCP tool callable (check `curl http://localhost:4000/api/mcp/tools/list` or via MCP Inspector)

## Final checklist (read aloud before reporting complete)

Group ordering everywhere — MUST match:
`Dashboard → Marketing → Orders & Fulfillment → Customers → Catalog → Human Resources → Project Management → CRM → Content → Users & Access → Settings → System`

- [ ] `ModuleNames.{Category}.{Name}` constant added
- [ ] `{Name}ModuleDefinition.cs` created with correct `SortOrder` within its category
- [ ] Registered in `ModuleCatalog.cs`
- [ ] `ModuleCatalogTests.cs` expected count updated — tests pass
- [ ] Permission constants in `Permissions.cs` (constants + group + appended to `All` array in correct section)
- [ ] Sidebar item in correct `navSection` with `permission` + `feature` props
- [ ] OpenAPI tag in `document.Tags` with description
- [ ] OpenAPI `x-tagGroups` JSON updated — tag in correct group
- [ ] Endpoint group tagged `.WithTags("Tag Name")` AND gated `.RequireFeature(ModuleNames.X.Y)`
- [ ] MCP tool class at `Web/Mcp/Tools/{Feature}Tools.cs` with `[RequiresModule]`
- [ ] MCP tool names follow `noir_{domain}_{action}` — explicit `[McpServerTool(Name = "...")]`
- [ ] i18n keys in BOTH `en/common.json` AND `vi/common.json` (`nav.*`, `modules.*`, `permissions.*`)
- [ ] VI labels follow sentence case + pure Vietnamese (no "Bài viết Blog" style mixing)
- [ ] Integration tests cover happy path + 401 + 400/404
- [ ] Repository DI registration test added
- [ ] Migration added with `--context ApplicationDbContext` (or Tenant)
- [ ] `dotnet build`, `dotnet test`, `pnpm run build` all green

## Common mistakes this skill prevents

- Adding sidebar item but forgetting the OpenAPI tag (API docs won't show the group)
- Adding permissions but not updating the `All` array (role assignment UI won't show them)
- Adding endpoint but forgetting `.RequireFeature()` gate (module toggle does nothing)
- Adding MCP tool without `[RequiresModule]` (bypasses feature gate)
- MCP tool naming like `GetProducts` instead of `noir_products_list` (breaks AI discoverability)
- Accepting `Guid` in MCP tool signature instead of `string` (AI clients send strings — Rule 27)
- Using `.With("Name")` single tag when a feature needs grouping in OpenAPI sidebar
- Module test count in `ModuleCatalogTests.cs` out of sync after adding a module
- i18n labels mixing EN + VI (e.g. "Bài viết Blog" — violates `sidebar-naming-convention.md`)
