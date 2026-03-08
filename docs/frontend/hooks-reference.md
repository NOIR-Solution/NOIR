# Frontend Hooks Reference

> Quick reference for all 39 custom React hooks in `src/NOIR.Web/frontend/src/hooks/`

**Last Updated:** 2026-03-08

---

## Quick Reference Table

| Hook | Category | Purpose |
|------|----------|---------|
| `useOptimisticMutation` | Data Fetching | Optimistic update helpers for TanStack Query |
| `useStockHistoryQuery` | Data Fetching | Stock history with TanStack Query |
| `useGlobalSearch` | Data Fetching | Global search via Cmd+K |
| `useMediaFiles` | Data Fetching | Media library files |
| `useDashboard` | Data Fetching | Dashboard metrics |
| `useWebhooks` | Data Fetching | Webhook subscriptions |
| `useLogin` | Auth | Login mutation + redirect |
| `usePermissions` | Auth | Permission checking (`hasPermission`, `hasAllPermissions`) |
| `useFeatures` | Auth | Feature flag checking |
| `useUrlTab` | URL State | Tab state synced to `?tab=` |
| `useUrlDialog` | URL State | Create dialog state synced to `?dialog=` |
| `useUrlEditDialog` | URL State | Edit dialog state synced to `?edit=` |
| `useSignalR` | Real-Time | SignalR connection management |
| `useEntityUpdateSignal` | Real-Time | CRUD change subscriptions |
| `useSse` | Real-Time | Server-Sent Events consumer |
| `useJobProgress` | Real-Time | Background job progress |
| `useLogStream` | Real-Time | Developer log streaming |
| `useBroadcastChannel` | Real-Time | Cross-tab session sync |
| `useSelection` | UI State | Multi-select for lists/tables |
| `useMediaQuery` | UI State | CSS media query watcher |
| `useMobile` | UI State | Mobile breakpoint detection |
| `useTabVisibility` | UI State | Page visibility API |
| `useKeyboardShortcuts` | UI State | Global keyboard shortcuts |
| `useUnsavedChanges` | UI State | Unsaved changes guard |
| `useAutoSave` | UI State | Auto-save with debounce |
| `useBreadcrumbs` | Navigation | Breadcrumb trail management |
| `useViewTransition` | Navigation | View Transitions API wrapper |
| `useValidatedForm` | Forms | react-hook-form + Zod wrapper |
| `useSmartDefaults` | Forms | Populate form from last used values |
| `useVariantAutoSave` | Forms | Product variant auto-save |
| `useImageUpload` | Forms | Image upload with preview |
| `useOnboarding` | Platform | Onboarding flow state |
| `usePageContext` | Platform | Sets page name for audit logging |
| `usePWA` | Platform | PWA install prompt |
| `useNetworkStatus` | Platform | Online/offline detection |
| `useServerHealth` | Platform | Backend health + deploy recovery |
| `useSoftDelete` | Platform | Soft delete confirmation pattern |

---

## 1. Data Fetching

### `useOptimisticMutation`
**File:** `hooks/useOptimisticMutation.ts`

Shared optimistic update helpers for TanStack Query mutations. Prevents stale UI on delete/patch.

```typescript
import { optimisticListDelete, optimisticListPatch, optimisticArrayDelete } from '@/hooks/useOptimisticMutation'

// Paginated list delete (items + totalCount shape)
const useDeleteProduct = () => useMutation({
  mutationFn: deleteProduct,
  ...optimisticListDelete(queryClient, productKeys.lists(), productKeys.all),
})

// Paginated list patch
const usePublishProduct = () => useMutation({
  mutationFn: publishProduct,
  ...optimisticListPatch(queryClient, productKeys.lists(), productKeys.all, { status: 'Active' }),
})

// Flat array delete
const useDeleteTag = () => useMutation({
  mutationFn: deleteTag,
  ...optimisticArrayDelete(queryClient, blogKeys.tags(), blogKeys.all),
})
```

**Returns:** `{ onMutate, onError, onSettled }` spread into `useMutation`

---

### `useStockHistoryQuery`
**File:** `hooks/useStockHistoryQuery.ts`

Fetches stock movement history for a product variant.

```typescript
const { data, isLoading } = useStockHistoryQuery(variantId)
```

---

### `useGlobalSearch`
**File:** `hooks/useGlobalSearch.ts`

Powers the Command Palette (Cmd+K) global search.

```typescript
const { results, isSearching, search } = useGlobalSearch()
search('customer name')
```

---

### `useMediaFiles`
**File:** `hooks/useMediaFiles.ts`

Media library with folder filtering and search.

```typescript
const { files, isLoading, uploadFile, deleteFile } = useMediaFiles({ folder: 'blog' })
```

---

### `useDashboard`
**File:** `hooks/useDashboard.ts`

Dashboard metrics with feature-gated widget groups.

```typescript
const { metrics, isLoading } = useDashboard()
```

---

### `useWebhooks`
**File:** `hooks/useWebhooks.ts`

Webhook subscription management.

```typescript
const { subscriptions, isLoading, createWebhook, deleteWebhook } = useWebhooks()
```

---

## 2. Authentication & Authorization

### `useLogin`
**File:** `hooks/useLogin.ts`

Login mutation with redirect on success.

```typescript
const { login, isLoading, error } = useLogin()
login({ email, password, rememberMe })
```

---

### `usePermissions`
**File:** `hooks/usePermissions.ts`

Permission checking from JWT claims.

```typescript
const { hasPermission, hasAllPermissions, hasAnyPermission } = usePermissions()
if (hasPermission('products:create')) { ... }
```

---

### `useFeatures`
**File:** `hooks/useFeatures.ts`

Feature flag checking (module availability).

```typescript
const { isEnabled } = useFeatures()
if (isEnabled('Ecommerce.Products')) { ... }
```

---

## 3. URL State

> **Rule:** All tabbed pages MUST use `useUrlTab`. All create dialogs MUST use `useUrlDialog`. All edit dialogs MUST use `useUrlEditDialog`. See [url-tab-state.md](../../.claude/rules/url-tab-state.md).

### `useUrlTab`
**File:** `hooks/useUrlTab.ts`

Syncs active tab to URL `?tab=value`. Default tab omitted from URL for clean URLs.

```typescript
const { activeTab, handleTabChange, isPending } = useUrlTab({ defaultTab: 'overview' })
```

**Returns:** `{ activeTab, handleTabChange, isPending }`

---

### `useUrlDialog`
**File:** `hooks/useUrlDialog.ts`

Syncs create dialog open state to `?dialog=paramValue`.

```typescript
const { isOpen, open, onOpenChange } = useUrlDialog({ paramValue: 'create-product' })
// Standard destructuring:
const { isOpen: isCreateOpen, open: openCreate, onOpenChange: onCreateOpenChange } = ...
```

---

### `useUrlEditDialog`
**File:** `hooks/useUrlEditDialog.ts`

Syncs edit dialog to `?edit=entityId`. Resolves full entity from items array.

```typescript
const { editItem, openEdit, closeEdit, onEditOpenChange } = useUrlEditDialog<Product>(products)
openEdit(product) // sets ?edit=product.id
```

> **CRITICAL:** Calling both `useUrlDialog` and `useUrlEditDialog` close in the same tick causes the second to overwrite the first. Use conditional close:
> ```tsx
> onOpenChange={(open) => {
>   if (!open) {
>     if (isCreateOpen) onCreateOpenChange(false)
>     if (editItem) closeEdit()
>   }
> }}
> ```

---

## 4. Real-Time

### `useSignalR`
**File:** `hooks/useSignalR.ts`

Base SignalR connection for notifications. Disconnects when tab is hidden (via `useTabVisibility`).

```typescript
const { connection, isConnected } = useSignalR()
```

---

### `useEntityUpdateSignal`
**File:** `hooks/useEntityUpdateSignal.ts`

Subscribe to entity CRUD changes via SignalR groups.

```typescript
useEntityUpdateSignal({
  entityType: 'Product',
  onCollectionUpdate: refetch,       // list pages
  entityId: product.id,              // edit pages
  onEntityUpdate: handleUpdate,
  onEntityDeleted: handleDeleted,
})
```

**Behavior matrix:**
| Scenario | `formState.isDirty` | Action |
|----------|---------------------|--------|
| List update | — | `refetch()` |
| Edit update | false | Reload form silently |
| Edit update | true | Show `EntityConflictDialog` |
| Entity deleted | any | Show `EntityDeletedDialog` (non-dismissible) + navigate away |

---

### `useSse`
**File:** `hooks/useSse.ts`

Generic Server-Sent Events consumer with exponential backoff reconnect.

```typescript
const { lastEvent, isConnected } = useSse<JobProgressEvent>('/api/sse/events', {
  eventType: 'job:progress',
})
```

---

### `useJobProgress`
**File:** `hooks/useJobProgress.ts`

Convenience wrapper for tracking background job progress.

```typescript
const { progress, status, isComplete } = useJobProgress(jobId)
```

---

### `useLogStream`
**File:** `hooks/useLogStream.ts`

Real-time developer log streaming via SignalR `LogStreamHub`.

```typescript
const { logs, level, setLevel, clearLogs } = useLogStream()
```

---

### `useBroadcastChannel`
**File:** `hooks/useBroadcastChannel.ts`

Cross-tab session synchronization (auth state, theme, language).

```typescript
const { postMessage } = useBroadcastChannel('auth', (event) => {
  if (event.type === 'logout') signOut()
})
```

---

## 5. UI State

### `useSelection`
**File:** `hooks/useSelection.ts`

Multi-select state for lists and tables. Used for bulk operations.

```typescript
const { selectedIds, handleSelectAll, handleSelectNone, handleToggleSelect, isAllSelected } = useSelection(items)
```

> **Rule:** Call `useSelection(data)` AFTER the `data` variable declaration.

---

### `useMediaQuery`
**File:** `hooks/useMediaQuery.ts`

Reactive CSS media query watcher.

```typescript
const isLargeScreen = useMediaQuery('(min-width: 1024px)')
```

---

### `useMobile`
**File:** `hooks/useMobile.tsx`

Mobile breakpoint detection (`< 768px`).

```typescript
const isMobile = useMobile()
```

---

### `useTabVisibility`
**File:** `hooks/useTabVisibility.ts`

Page Visibility API — detects when user switches browser tabs.

```typescript
const isVisible = useTabVisibility()
```

---

### `useKeyboardShortcuts`
**File:** `hooks/useKeyboardShortcuts.ts`

Global keyboard shortcut registration.

```typescript
useKeyboardShortcuts({
  'mod+k': openCommandPalette,
  'Escape': closeDialog,
})
```

---

### `useUnsavedChanges`
**File:** `hooks/useUnsavedChanges.ts`

Prevents navigation when form has unsaved changes.

```typescript
useUnsavedChanges(isDirty, 'You have unsaved changes.')
```

---

### `useAutoSave`
**File:** `hooks/useAutoSave.ts`

Debounced auto-save for form fields.

```typescript
const { lastSaved, isSaving } = useAutoSave(formValues, saveFunction, { debounceMs: 1000 })
```

---

## 6. Navigation

### `useBreadcrumbs`
**File:** `hooks/useBreadcrumbs.ts`

Breadcrumb trail management for the portal layout.

```typescript
useBreadcrumbs([
  { label: t('nav.products'), href: '/products' },
  { label: product.name },
])
```

---

### `useViewTransition`
**File:** `hooks/useViewTransition.ts`

View Transitions API wrapper for smooth page transitions.

```typescript
const { startTransition } = useViewTransition()
startTransition(() => navigate('/products'))
```

---

## 7. Forms

### `useValidatedForm`
**File:** `hooks/useValidatedForm.ts` *(or `useSmartDefaults.ts`)*

react-hook-form + Zod wrapper with standard `mode: 'onBlur'`.

```typescript
const form = useValidatedForm(schema, defaultValues)
```

---

### `useSmartDefaults`
**File:** `hooks/useSmartDefaults.ts`

Populate form fields from last used values (localStorage).

```typescript
useSmartDefaults(form, 'product-create')
```

---

### `useVariantAutoSave`
**File:** `hooks/useVariantAutoSave.ts`

Product variant form with auto-save on change.

```typescript
const { isSaving } = useVariantAutoSave(variantId, formValues)
```

---

### `useImageUpload`
**File:** `hooks/useImageUpload.ts`

Image upload with preview, validation, and progress tracking.

```typescript
const { upload, preview, isUploading, error } = useImageUpload({ maxSizeMb: 5 })
```

---

## 8. Platform

### `useOnboarding`
**File:** `hooks/useOnboarding.ts`

Onboarding flow state management.

```typescript
const { step, nextStep, isComplete } = useOnboarding()
```

---

### `usePageContext`
**File:** `hooks/usePageContext.ts`

Sets page name for audit logging (required for `IAuditableCommand`). See CLAUDE.md rule #11.

```typescript
// In every page component that has audit-logged mutations:
usePageContext('ProductsPage')
```

---

### `usePWA`
**File:** `hooks/usePWA.ts`

PWA install prompt management.

```typescript
const { canInstall, install } = usePWA()
```

---

### `useNetworkStatus`
**File:** `hooks/useNetworkStatus.ts`

Online/offline detection with `OfflineBanner` integration.

```typescript
const { isOnline } = useNetworkStatus()
```

---

### `useServerHealth`
**File:** `hooks/useServerHealth.ts`

Backend health monitoring. Shows `ServerRecoveryBanner` during deploys.

```typescript
const { isServerDown, isRecovering } = useServerHealth()
```

---

### `useSoftDelete`
**File:** `hooks/useSoftDelete.ts`

Soft delete with confirmation dialog pattern.

```typescript
const { confirmDelete, DeleteConfirmDialog } = useSoftDelete({
  onDelete: (id) => deleteProduct(id),
})
```

---

## Test Coverage

| Hook | Test File |
|------|-----------|
| `useEntityUpdateSignal` | `useEntityUpdateSignal.test.tsx` |
| `useMediaQuery` | `useMediaQuery.test.ts` |
| `useSelection` | `useSelection.test.ts` |
| `useTabVisibility` | `useTabVisibility.test.ts` |
| `useUrlDialog` | `useUrlDialog.test.tsx` |
| `useUrlEditDialog` | `useUrlEditDialog.test.tsx` |
| `useUrlTab` | `useUrlTab.test.tsx` |

---

## Cross-References

- [architecture.md](architecture.md) — TanStack Query patterns, React 19 performance hooks
- [url-tab-state.md](../../.claude/rules/url-tab-state.md) — URL state conventions (REQUIRED reading)
- [CLAUDE.md](../../CLAUDE.md) — Rule #11: `usePageContext` for audit logging
