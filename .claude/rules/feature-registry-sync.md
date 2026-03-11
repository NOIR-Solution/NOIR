# Feature Registry Sync Rule

## The 4 Registries

When adding a new feature/module or renaming an existing one, **all 4 registries** must stay in sync:

| # | Registry | File | What to update |
|---|----------|------|----------------|
| 1 | **Sidebar** | `frontend/src/components/portal/Sidebar.tsx` | `navSections[]` — add item with `titleKey`, `icon`, `path`, `permission`, `feature` |
| 2 | **Permissions** | `Domain/Common/Permissions.cs` | Add constants (`XxxRead`, `XxxCreate`, etc.), group, and add to `All` array in correct section |
| 3 | **OpenAPI** | `Web/OpenApi/SecuritySchemeDocumentTransformer.cs` | Add `OpenApiTag` in `document.Tags` AND update `x-tagGroups` JSON — both must match |
| 4 | **Modules** | `Application/Modules/` | Add `ModuleNames` constant + `{Name}ModuleDefinition.cs` + register in `ModuleCatalog` |

## Group Ordering (must match across all 4)

```
Dashboard → Marketing → Orders & Fulfillment → Customers → Catalog →
Human Resources → Project Management → CRM → Content →
Users & Access → Settings → System
```

## Checklist for New Feature

- [ ] `ModuleNames.{Category}.{Name}` constant added
- [ ] `{Name}ModuleDefinition.cs` created with correct `SortOrder` within its category
- [ ] Permission constants added to `Permissions.cs` (constants + group + `All` array)
- [ ] Sidebar item added to correct `navSection` with `permission` and `feature` props
- [ ] OpenAPI tag added to `document.Tags` with description
- [ ] OpenAPI `x-tagGroups` JSON updated to include the new tag in the correct group
- [ ] Endpoint group tagged with `.WithTags("Tag Name")` and gated with `.RequireFeature()`
- [ ] i18n keys added for both EN and VI (`nav.*`, `modules.*`, `permissions.*`)
- [ ] `ModuleCatalogTests.cs` expected count updated

## Common Mistakes

- Adding a sidebar item but forgetting the OpenAPI tag (API docs won't show the group)
- Adding permissions but not updating the `All` array (role assignment UI won't show them)
- Adding a module definition but not adding it to `x-tagGroups` (Scalar sidebar won't group it)
- Mismatched group names between sidebar `labelKey` translations and OpenAPI `x-tagGroups` `"name"`
