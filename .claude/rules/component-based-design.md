# Component-Based Design (Frontend)

## Rule

ALL frontend UI MUST use shared components from `@uikit` or `@/components`. Never use raw HTML elements when a shared component exists.

## Component Map (Required Substitutions)

| Raw HTML / Custom | Use Instead (from `@uikit`) |
|---|---|
| `<input type="text">` | `<Input>` |
| `<input type="date">` | `<DatePicker>` (forms) or `<Input type="date">` (compact contexts) |
| `<input type="color">` | `<ColorPicker>` or `<CompactColorPicker>` |
| `<textarea>` | `<Textarea>` |
| `<select>` | `<Select>` / `<Combobox>` |
| `<button>` (action) | `<Button>` |
| `<table>` (list page) | `<DataTable>` + `useEnterpriseTable` |
| Custom modal/dialog | `<Credenza>` (responsive) or `<Dialog>` |
| Custom tooltip | `<Tooltip>` (Radix) — never `title=` attribute |
| Custom dropdown | `<DropdownMenu>` |
| Custom badge/tag | `<Badge>` with `getStatusBadgeClasses()` |
| Custom empty state | `<EmptyState>` |
| `animate-pulse` divs | `<Skeleton>` or `<TableSkeleton>` / `<DetailPageSkeleton>` |
| Custom loading | `<Loading>` or `<PageLoader>` |
| Custom file preview | `<FilePreviewTrigger>` (tables) or `<FilePreviewModal>` (cards) |
| Custom import/export | `<ImportExportDropdown>` |

## Shared Hook Map

| Custom Pattern | Use Instead |
|---|---|
| `useState` + `useEffect` for API data | TanStack Query (`useQuery`) |
| Custom table state | `useEnterpriseTable` + `useTableParams` |
| Custom tab URL sync | `useUrlTab()` |
| Custom dialog URL sync | `useUrlDialog()` / `useUrlEditDialog()` |
| Custom form state | `react-hook-form` + `zodResolver` |

## Accepted Exceptions

| Pattern | Why Allowed |
|---|---|
| **PM inline-edit/search** `<input>` (KanbanBoard, TaskDetailModal, TaskDetailPage) | Minimal unstyled input that looks like text until focused — `<Input>` adds unwanted borders/padding |
| **Hidden** `<input type="file">` | Browser file API — never visible, no @uikit equivalent needed |
| **PM TaskFilterPopover** CheckRow/RadioRow | Compact filter UI with specific interaction pattern; Radix Checkbox Presence causes perf issues at scale |
| **Embedded settings tables** (ProviderList, WebhooksSettingsTab) | Small client-side tables without pagination — DataTable is overkill |
| **Color-dynamic toggle chips** (TagSelector) | Dynamic `style` for tag colors — no Button variant supports inline `borderColor`/`color` |
| **Avatar overflow** `<button>` (ProjectMemberAvatars) | Avatar-like circular element — Button adds no value when all styling is overridden |
| **Selectable option cards** (AppearanceSettings) | Card-like selection elements — Button variant doesn't fit card layout |
| **Kanban quick-add** `<textarea>` (KanbanBoard) | Inline quick-add context, same reasoning as inline-edit `<input>` |
| **Status indicator** `animate-pulse` (notification bell, saving indicator) | Functional animation on icons/badges, not a loading skeleton |

## Button Sizing Convention

| Context | Size Prop | Height | Example |
|---------|-----------|--------|---------|
| Primary actions (Create, Save) | `default` | h-9 | PageHeader, Dialog footer |
| Toolbar buttons (filter, export, density) | `size="sm"` + `h-9` | h-9 | DataTableToolbar (sm for tighter padding, h-9 for input alignment) |
| Compact secondary buttons | `size="sm"` | h-8 | Inline actions, card buttons |
| Icon-only buttons | `size="icon"` | size-9 | Standard icon buttons |
| Compact icon buttons (cards/rows) | custom `h-7 w-7` | h-7 | Tag edit/delete, card actions |

**Rules:**
- Never use `h-7` on TEXT buttons — minimum is `h-8` (`size="sm"`)
- Never override `size="icon"` with `h-8 w-8` — either use native size-9 or use `size="sm"` + `w-8 p-0`
- Dialog footer buttons: always default size (h-9), Cancel and Submit must match
- Adjacent buttons must be the same height

## Hover & Transition Standard

| Element | Hover | Transition |
|---------|-------|-----------|
| Cards / card-like containers | `shadow-sm hover:shadow-lg` | `transition-all duration-300` |
| Kanban cards (drag context) | `shadow-sm hover:shadow-md` | `transition-all duration-200` |
| Opacity pending states | `opacity-70` | `transition-opacity duration-200` |
| Button hover | Handled by Button variant — never override `hover:bg-*` | Built-in |

**Never** use `hover:shadow-md` on non-Kanban cards — always `hover:shadow-lg`.

## Before Creating a New Component

1. **Check `@uikit`** — 99+ components. Run: `grep -r "export" src/uikit/index.ts`
2. **Check `@/components`** — shared app components (PermissionPicker, BulkActionToolbar, etc.)
3. **Check Storybook** — `pnpm storybook` at http://localhost:6006
4. If no match exists AND the component is used in 2+ places → add to `@uikit` with a story.
5. If single-use → keep in the feature directory but use @uikit primitives internally.
