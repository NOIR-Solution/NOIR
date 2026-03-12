# Feature Registry Sync Rule

## The 5 Registries

When adding a new feature/module or renaming an existing one, **all 5 registries** must stay in sync:

| # | Registry | File | What to update |
|---|----------|------|----------------|
| 1 | **Sidebar** | `frontend/src/components/portal/Sidebar.tsx` | `navSections[]` — add item with `titleKey`, `icon`, `path`, `permission`, `feature` |
| 2 | **Permissions** | `Domain/Common/Permissions.cs` | Add constants (`XxxRead`, `XxxCreate`, etc.), group, and add to `All` array in correct section |
| 3 | **OpenAPI** | `Web/OpenApi/SecuritySchemeDocumentTransformer.cs` | Add `OpenApiTag` in `document.Tags` AND update `x-tagGroups` JSON — both must match |
| 4 | **Modules** | `Application/Modules/` | Add `ModuleNames` constant + `{Name}ModuleDefinition.cs` + register in `ModuleCatalog` |
| 5 | **MCP Tools** | `Web/Mcp/Tools/{Name}Tools.cs` | Add `[McpServerToolType]` + `[RequiresModule(ModuleNames.X.Y)]` class with at least list/get tools |

## Group Ordering (must match across all 5)

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
- [ ] MCP tool class created in `Web/Mcp/Tools/{Name}Tools.cs` with `[RequiresModule]`
- [ ] MCP tool naming follows `noir_{domain}_{action}` convention
- [ ] i18n keys added for both EN and VI (`nav.*`, `modules.*`, `permissions.*`)
- [ ] `ModuleCatalogTests.cs` expected count updated

## Common Mistakes

- Adding a sidebar item but forgetting the OpenAPI tag (API docs won't show the group)
- Adding permissions but not updating the `All` array (role assignment UI won't show them)
- Adding a module definition but not adding it to `x-tagGroups` (Scalar sidebar won't group it)
- Mismatched group names between sidebar `labelKey` translations and OpenAPI `x-tagGroups` `"name"`
- Adding endpoints but forgetting MCP tools — AI agents won't be able to access the new feature

## API & MCP Consistency (Ongoing — Not Just New Features)

When **modifying** an existing query, command, or endpoint, check if a corresponding MCP tool exists and needs updating:

| Change | MCP Impact | Action |
|--------|-----------|--------|
| Add/remove/rename a command/query constructor parameter | MCP tool creates the command — constructor change breaks it | Update `new XxxCommand(...)` call in the tool |
| Rename a DTO property | MCP returns the DTO — property rename changes AI-visible field names | Verify the tool still references correct properties if used |
| Add a new required parameter to an endpoint | MCP tool may not pass it | Update the tool's method signature and invocation |
| Remove an endpoint (feature deprecated) | MCP tool will fail at runtime | Remove or disable the corresponding MCP tool |
| Add a major new capability (new command) | AI agents can't use it | Add a new `[McpServerTool]` method to the relevant tool class |

**Quick consistency check before committing any API change:**
```
grep -r "new XxxQuery\|new XxxCommand" src/NOIR.Web/Mcp/
```
If results appear, the MCP tool uses that query/command — review it for compatibility.
