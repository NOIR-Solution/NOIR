# Employee Tags — DataTable with Category Grouping

## UI Pattern

**Employee Tags** (`/portal/hr/tags`) uses **DataTable with `enableGrouping: true`** on the category column — consistent with all other list pages (Users, Blog Tags, Orders, etc.).

### Why DataTable (not cards)

Employee Tags was migrated from card layout to DataTable to gain: search, column visibility, sorting, audit columns, density control, and grouping — all provided by `useEnterpriseTable`. The 7-category grouping works naturally with TanStack Table's built-in grouping feature.

### Implementation

- `TagsPage.tsx` — DataTable with `enableGrouping: true` on category column
- Client-side search (name, description, category)
- Columns: Actions, Name (with color dot via `ColorPopover`), Category (with `Badge` + `getStatusBadgeClasses`), Description, Employee Count, Sort Order (hidden), Audit columns
- Category grouping: `meta.groupValueFormatter` translates enum values for i18n
- `DeleteEmployeeTagDialog.tsx` — extracted delete confirmation dialog with tag preview
- `TagFormDialog.tsx` — create/edit dialog with category, color picker (12 presets), description, sort order
- `TagSelector.tsx` — tag assignment dialog for employees, grouped by category

### Domain Simplification

- `IsActive` removed from `EmployeeTag` entity (soft delete handles activation/deactivation)
- Consistent with `PostTag` (Blog Tags) which also has no `IsActive`

### MCP Tools

`HrTagTools.cs` — 6 tools: `noir_hr_tags_list`, `noir_hr_tags_get`, `noir_hr_tags_create`, `noir_hr_tags_update`, `noir_hr_tags_delete`, `noir_hr_tags_assign`
