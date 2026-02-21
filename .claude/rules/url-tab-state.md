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

## Dialogs → `useUrlDialog()`

Create/edit dialogs SHOULD use `useUrlDialog()` hook from `@/hooks/useUrlDialog` for URL-synced open state.

```tsx
const { isOpen, open, onOpenChange } = useUrlDialog({ paramValue: 'create-category' })

<Button onClick={open}>Create</Button>
<Credenza open={isOpen} onOpenChange={onOpenChange}>...</Credenza>
```

- Enables bookmarking and sharing of "create new X" states
- Uses `?dialog=paramValue` URL pattern
- Uses `replace: true` to avoid browser history pollution
- Preserves existing search params

**Applies to:** Create/Edit dialogs on list pages. Does NOT apply to: delete confirmations, detail views, filter dialogs (transient UI).
