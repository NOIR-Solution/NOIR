# URL State Conventions

## Tabs → `useUrlTab()`

All tabbed pages MUST use `useUrlTab()` hook from `@/hooks/useUrlTab` for URL-synced tab state.

```tsx
const { activeTab, handleTabChange, isPending } = useUrlTab({ defaultTab: 'overview' })
```

- Enables direct linking, bookmarking, and sharing of specific tab views
- Default tab is omitted from URL for cleaner URLs
- Uses `replace: true` to avoid browser history pollution
- Preserves existing search params (functional updater)
- Built-in `useTransition` for pending state (use `isPending` for opacity transition)

**Applies to:** Pages with `<Tabs>`. Does NOT apply to dialog/popup tabs (transient UI).

## Create Dialogs → `useUrlDialog()`

Create dialogs MUST use `useUrlDialog()` hook from `@/hooks/useUrlDialog` for URL-synced open state.

```tsx
const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = useUrlDialog({ paramValue: 'create-product-category' })

<Button onClick={() => openCreate()}>Create</Button>
<Credenza open={isCreateOpen} onOpenChange={onCreateOpenChange}>...</Credenza>
```

- Uses `?dialog=paramValue` URL pattern
- `paramValue` must be unique across all pages (e.g. `create-product-category`, NOT `create-category`)
- Standard destructuring: `isCreateOpen` / `openCreate` / `onCreateOpenChange`

## Edit Dialogs → `useUrlEditDialog()`

Edit dialogs MUST use `useUrlEditDialog()` hook from `@/hooks/useUrlEditDialog` for URL-synced edit state.

```tsx
const { editItem, openEdit, closeEdit, onEditOpenChange } = useUrlEditDialog<EntityType>(items)

<Button onClick={() => openEdit(entity)}>Edit</Button>
```

- Uses `?edit=entityId` URL pattern
- Resolves full entity from items array on page load
- Uses `replace: true` to avoid browser history pollution

### Combined create+edit dialog (single dialog component)

**CRITICAL**: Use conditional close — calling both `setSearchParams` hooks in the same tick causes the second to overwrite the first.

```tsx
<ProductCategoryDialog
  open={isCreateOpen || !!editItem}
  onOpenChange={(open) => {
    if (!open) {
      if (isCreateOpen) onCreateOpenChange(false)
      if (editItem) closeEdit()
    }
  }}
  category={editItem}
/>
```

### Separate create and edit dialogs

```tsx
<CreateRoleDialog open={isCreateOpen} onOpenChange={onCreateOpenChange} />
<EditRoleDialog open={!!editItem} onOpenChange={onEditOpenChange} role={editItem} />
```

**Applies to:** Create/edit dialogs on list pages. Does NOT apply to: delete confirmations, detail views, filter dialogs (transient UI).
