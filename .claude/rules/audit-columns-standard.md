# Audit Columns Standard

## Rule

ALL table list pages MUST include 4 audit columns as the LAST data columns (rightmost), using `formatDateTime()` from `useRegionalSettings()`.

## Required Columns

| Column | ID | Field | Sortable | Default Visible | Required |
|--------|-----|-------|----------|----------------|----------|
| Created At | `createdAt` | `createdAt` | YES | YES | Every list page |
| Creator | `createdBy` | `createdByName` | YES | YES | If entity is auditable |
| Modified At | `modifiedAt` | `modifiedAt` | YES | NO (hidden) | Every list page |
| Editor | `modifiedBy` | `modifiedByName` | YES | NO (hidden) | If entity is auditable |

## Column Position

```
[Actions] [Select?] [...domain data columns...] [Created At] [Creator] [Modified At] [Editor]
```

## Default Visibility

- **Created At** + **Creator**: visible by default (primary audit info)
- **Modified At** + **Editor**: hidden by default via `meta: { defaultHidden: true }` (power-user info, toggle via Columns dropdown)
- `useEnterpriseTable` reads `meta.defaultHidden` from column definitions and applies it when no stored preference exists in localStorage

## Frontend Implementation

Use `createFullAuditColumns()` from `@/lib/table/columnHelpers`:

```tsx
import { createActionsColumn, createSelectColumn, createFullAuditColumns } from '@/lib/table/columnHelpers'

const { formatDateTime } = useRegionalSettings()

const columns = useMemo((): ColumnDef<ItemType, unknown>[] => [
  createActionsColumn<ItemType>(...),
  createSelectColumn<ItemType>(),
  // ...domain columns...
  ...createFullAuditColumns<ItemType>(t, formatDateTime),
], [t, formatDateTime])
```

For DTOs without user name fields (no `CreatedByName`/`ModifiedByName`), only include the date columns (`createdAt`, `modifiedAt`) in column definitions manually.

## Backend DTO Contract

Every list/summary DTO MUST include:
- `DateTimeOffset CreatedAt`
- `DateTimeOffset? ModifiedAt`
- `string? CreatedByName` (if entity implements `IAuditableEntity`)
- `string? ModifiedByName` (if entity implements `IAuditableEntity`)

User names resolved via `IUserDisplayNameService.GetDisplayNamesAsync()` in query handlers — batch query, not per-row.

## Backend Sort Support

Every list specification MUST include sort cases for `createdBy`/`modifiedBy`:
```csharp
case "createdby":
case "creator":
    if (isDescending) Query.OrderByDescending(x => x.CreatedBy);
    else Query.OrderBy(x => x.CreatedBy);
    break;
case "modifiedby":
case "editor":
    if (isDescending) Query.OrderByDescending(x => x.ModifiedBy);
    else Query.OrderBy(x => x.ModifiedBy);
    break;
```

Sorts by user ID (groups records by same creator/editor). Display name is resolved post-query.

## formatRelativeTime Policy

`formatRelativeTime()` is ONLY for:
- Activity timeline entries
- Comments/notes timestamps
- Dashboard recent activity widgets
- Task detail metadata (PM module)

**NEVER** use `formatRelativeTime()` in DataTable columns. Tables always use `formatDateTime()`.

## Domain Date Columns

Domain-specific dates (e.g. `startDate`/`endDate` on Promotions, `paidAt` on Payments, `joinDate` on Employees) are separate from audit columns. Keep both — domain dates in their natural position, audit columns at the end.

## i18n Keys

| Key | EN | VI |
|-----|----|----|
| `labels.createdAt` | Created At | Ngày tạo |
| `labels.creator` | Creator | Người tạo |
| `labels.modifiedAt` | Modified At | Ngày sửa |
| `labels.editor` | Editor | Người sửa |

## Exceptions

| Page | Exception | Reason |
|------|-----------|--------|
| Employee Tags (card layout) | No table columns | Card-based layout, not DataTable |
| Settings embedded tables | No audit columns | Small config tables, not entity lists |
| Dashboard widgets | No audit columns | Summary cards, not entity lists |

## Bug This Prevents

- BlogPostsPage showing "3 ngày trước" while OrdersPage shows "16/01/2026 14:30"
- Tables missing Modified/Creator/Editor columns
- Inconsistent date formatting across the application
- No visibility into who created or last edited a record
