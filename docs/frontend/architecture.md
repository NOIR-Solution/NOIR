# Frontend Architecture

This document describes the architecture and conventions for the NOIR frontend application.

## Technology Stack

| Technology | Purpose |
|------------|---------|
| React 19 | UI library (useDeferredValue, useTransition) |
| TypeScript | Type safety |
| TanStack Query 5 | Server state, caching, optimistic mutations |
| Vite | Build tool & dev server |
| Tailwind CSS 4 | Styling |
| React Router 7 | Client-side routing |
| shadcn/ui | UI component primitives |

## Project Structure

```
frontend/
├── .storybook/              # Storybook configuration
│   ├── main.ts              # Vite + Tailwind CSS 4 setup
│   └── preview.ts           # Global styles
├── src/
│   ├── App.tsx              # App shell with routing
│   ├── main.tsx             # React root entry point
│   ├── index.css            # Global styles + Tailwind
│   │
│   ├── portal-app/          # Domain-driven feature modules
│   │   ├── blogs/           # Blog CMS
│   │   │   ├── features/    #   blog-post-list, blog-post-edit, blog-category-list, blog-tag-list
│   │   │   ├── components/  #   blog-posts/, blog-categories/, blog-tags/
│   │   │   └── states/      #   useBlogPosts.ts, useBlogCategories.ts, useBlogTags.ts
│   │   ├── brands/          # Brand management
│   │   │   └── features/    #   brand-list/BrandsPage.tsx
│   │   ├── customer-groups/ # Customer group management
│   │   │   ├── features/    #   customer-group-list/CustomerGroupsPage.tsx
│   │   │   └── queries/     #   useCustomerGroupQueries.ts, useCustomerGroupMutations.ts
│   │   ├── customers/       # Customer management
│   │   │   ├── features/    #   customer-list, customer-detail
│   │   │   └── queries/     #   useCustomerQueries.ts, useCustomerMutations.ts
│   │   ├── dashboard/       # Dashboard
│   │   │   └── features/    #   dashboard/DashboardPage.tsx
│   │   ├── inventory/       # Inventory receipt management
│   │   │   ├── features/    #   inventory-receipts/InventoryReceiptsPage.tsx
│   │   │   └── queries/     #   useInventoryQueries.ts, useInventoryMutations.ts
│   │   ├── notifications/   # Notifications
│   │   │   ├── features/    #   notification-list, notification-preferences
│   │   │   └── components/  #   notifications/ (Bell, Dropdown, Item, List, Empty)
│   │   ├── orders/          # Order management
│   │   │   ├── features/    #   order-list, order-detail, manual-create, shipment-tracking
│   │   │   └── queries/     #   useOrderQueries.ts, useOrderMutations.ts
│   │   ├── payments/        # Payment management
│   │   │   ├── features/    #   payment-detail/PaymentDetailPage.tsx
│   │   │   └── components/  #   RecordManualPaymentDialog.tsx
│   │   ├── products/        # Product catalog
│   │   │   ├── features/    #   product-list, product-edit, product-category-list, product-attribute-list
│   │   │   ├── components/  #   products/, product-categories/, product-attributes/
│   │   │   └── states/      #   useProducts.ts, useProductCategories.ts
│   │   ├── promotions/      # Promotions & discounts
│   │   ├── reports/         # Reporting & analytics
│   │   ├── reviews/         # Product reviews
│   │   ├── settings/        # All settings (personal, tenant, platform)
│   │   │   ├── features/    #   personal-settings, tenant-settings, platform-settings,
│   │   │   │                #   email-template-edit, legal-page-edit
│   │   │   ├── components/  #   tenant-settings/, platform-settings/, payment-gateways/,
│   │   │   │                #   personal-settings/
│   │   │   └── states/      #   usePaymentGateways.ts
│   │   ├── shipping/        # Shipping management
│   │   ├── systems/         # System monitoring
│   │   │   ├── features/    #   activity-timeline, developer-logs
│   │   │   └── components/  #   activity-timeline/, developer-logs/
│   │   ├── user-access/     # User & access management
│   │   │   ├── features/    #   role-list, user-list, tenant-list
│   │   │   ├── components/  #   roles/, users/, tenants/
│   │   │   └── states/      #   useRoles.ts, useUsers.ts, useTenants.ts
│   │   ├── welcome/         # Public pages
│   │   │   └── features/    #   welcome/WelcomePage, terms/TermsPage, privacy/PrivacyPage
│   │   └── wishlists/       # Customer wishlists
│   │
│   ├── layouts/             # Layout components
│   │   ├── auth/            # Auth pages (each in own folder)
│   │   │   ├── login/LoginPage.tsx
│   │   │   ├── forgot-password/ForgotPasswordPage.tsx
│   │   │   ├── verify-otp/VerifyOtpPage.tsx
│   │   │   ├── reset-password/ResetPasswordPage.tsx
│   │   │   └── auth-success/AuthSuccessPage.tsx
│   │   └── PortalLayout.tsx
│   │
│   ├── uikit/               # UI component library (92 components + stories)
│   │   ├── button/          #   Button.tsx, Button.stories.tsx, index.ts
│   │   ├── dialog/          #   Dialog.tsx, Dialog.stories.tsx, index.ts
│   │   ├── ...              #   Per-component folders (kebab-case)
│   │   └── index.ts         #   Barrel export (@uikit alias)
│   │
│   ├── components/          # Shared app-level components
│   │   ├── PermissionGate.tsx
│   │   ├── ProtectedRoute.tsx
│   │   ├── command-palette/
│   │   └── navigation/
│   │
│   ├── contexts/            # React Context providers
│   │   ├── AuthContext.tsx
│   │   ├── ThemeContext.tsx
│   │   └── ...
│   │
│   ├── hooks/               # Shared custom React hooks
│   │   └── usePermissions.ts
│   │
│   ├── services/            # API service functions
│   │   ├── apiClient.ts
│   │   └── auth.ts, users.ts, ...
│   │
│   ├── lib/                 # Utility libraries
│   │   └── utils.ts
│   │
│   └── types/               # TypeScript type definitions
│       └── index.ts
│
├── package.json
├── pnpm-lock.yaml           # pnpm (disk-optimized)
├── tsconfig.json
├── vite.config.ts
└── eslint.config.js
```

## Folder Conventions

### `/portal-app` (Domain Modules)

**Purpose:** Domain-driven feature modules for the portal application. Each domain has its own folder with a consistent internal structure.

**Module Structure:**
```
portal-app/{domain}/
├── features/           # Page-level components (one per route)
│   └── {feature-name}/ #   e.g., product-list/
│       └── {PageName}Page.tsx
├── components/         # Domain-specific reusable components
│   └── {group-name}/   #   e.g., products/
│       ├── CreateProductDialog.tsx
│       └── index.ts    #   Barrel export
├── queries/            # TanStack Query hooks (preferred)
│   ├── queryKeys.ts        # Query key factories
│   ├── use{Domain}Queries.ts  # Query hooks (GET)
│   ├── use{Domain}Mutations.ts # Mutation hooks (POST/PUT/DELETE)
│   └── index.ts            # Barrel export
└── states/             # Legacy domain-specific hooks
    └── useProducts.ts
```

**Guidelines:**
- Page components always have `Page` suffix (e.g., `DashboardPage`, `ProductsPage`)
- Each page lives in its own kebab-case folder
- Components are prefixed with domain context (e.g., `EmailPreviewDialog`, not `PreviewDialog`)
- States contain TanStack Query hooks for API data fetching
- Cross-module imports use `@/portal-app/{domain}/` absolute paths
- Intra-module imports use relative paths

### `/layouts/auth`

**Purpose:** Authentication-related pages outside the portal layout.

**Guidelines:**
- Each auth page gets its own folder (e.g., `login/`, `forgot-password/`)
- Pages are eagerly loaded (not lazy) for fast auth flow

### `/uikit` (UI Component Library)

**Purpose:** 72 shadcn/ui-based primitives with per-component folders and a single barrel export.

**Guidelines:**
- Import via `@uikit` barrel alias: `import { Button, Dialog } from '@uikit'`
- Internal cross-references use relative paths (`../button/Button`), NOT `@uikit`
- Each component folder: `{kebab-case}/{PascalCase}.tsx` + `index.ts`
- Stories colocated: `{Component}.stories.tsx`

### `/components`

**Purpose:** Shared app-level components (not domain-specific).

**Examples:** `ProtectedRoute`, `PermissionGate`, `CommandPalette`, `ViewTransitionLink`

### `/services`

**Purpose:** API communication layer.

**Guidelines:**
- One file per domain (auth.ts, users.ts, etc.)
- Export functions, not classes
- Use type imports from `/types`
- Include JSDoc comments for documentation

**Example:**
```tsx
// services/auth.ts
import type { LoginRequest, AuthResponse } from '@/types'

export async function login(request: LoginRequest): Promise<AuthResponse> { ... }
```

### `/types`

**Purpose:** TypeScript type definitions.

**Guidelines:**
- Separate files by domain (auth.ts, api.ts)
- Use barrel export in index.ts
- Types should mirror backend DTOs where applicable
- Import from `@/types` not individual files

**Example:**
```tsx
// Correct
import type { CurrentUser, ApiError } from '@/types'

// Avoid
import type { CurrentUser } from '@/types/auth'
```

### `/contexts`

**Purpose:** React Context providers for global state.

**Guidelines:**
- Export both Provider and useContext hook
- Keep contexts focused on a single concern
- Name hook with `use` prefix + context name

### `/hooks`

**Purpose:** Custom React hooks for shared logic.

**Key Hooks:**
- `usePermissions` - Permission checking with `hasPermission()`, `hasAllPermissions()`, `hasAnyPermission()`
- `useOptimisticMutation` - Shared optimistic update helpers for TanStack Query (`optimisticListDelete`, `optimisticListPatch`, `optimisticArrayDelete`)

**Guidelines:**
- Export typed permission constants from `usePermissions.ts`
- Use hooks to check permissions in components before rendering actions

**Example:**
```tsx
// Using permission hooks
import { usePermissions, Permissions } from '@/hooks/usePermissions'

function UserActions() {
  const { hasPermission } = usePermissions()
  const canEdit = hasPermission(Permissions.UsersUpdate)

  return (
    <>
      {canEdit && <Button onClick={handleEdit}>Edit</Button>}
    </>
  )
}
```

### `/lib`

**Purpose:** Utility functions and helpers.

**Guidelines:**
- Generic utilities only (not domain-specific)
- Keep utilities pure and testable

## Import Aliases

| Alias | Maps To | Usage |
|-------|---------|-------|
| `@/` | `src/` | General source imports |
| `@uikit` | `src/uikit/index.ts` | UI component barrel |

```tsx
// UI components - use @uikit barrel
import { Button, Dialog, Card } from '@uikit'

// App-level imports - use @/ alias
import { usePermissions } from '@/hooks/usePermissions'
import type { CurrentUser } from '@/types'

// Cross-module imports - use @/ with full path
import { DashboardPage } from '@/portal-app/dashboard/features/dashboard/DashboardPage'

// Intra-module imports - use relative paths
import { ProductTable } from '../../components/products/ProductTable'

// Avoid bare relative paths from deep nesting
import { Button } from '../../../../uikit/button/Button' // ❌ Use @uikit instead
```

## Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Components | PascalCase | `LoginPage.tsx`, `Button.tsx` |
| Hooks | camelCase with `use` prefix | `useAuth.ts` |
| Services | camelCase | `auth.ts` |
| Types | PascalCase | `CurrentUser`, `ApiError` |
| Constants | SCREAMING_SNAKE_CASE | `API_BASE`, `MAX_RETRY` |

## Domain Module Pattern

The frontend uses a **domain-driven module pattern** under `portal-app/`. Each domain module is self-contained with features, components, and state management.

**Adding a new domain module:**
```
portal-app/{new-domain}/
├── features/
│   └── {feature-name}/
│       └── {FeatureName}Page.tsx   # Default export for lazy loading
├── components/
│   └── {group-name}/
│       ├── SomeDialog.tsx
│       └── index.ts
└── states/
    └── use{Domain}.ts              # TanStack Query hooks
```

**Register in App.tsx:**
```tsx
const NewPage = lazy(() => import('@/portal-app/{new-domain}/features/{feature}/NewPage'))
// ...
<Route path="new-feature" element={<Suspense fallback={<LazyFallback />}><NewPage /></Suspense>} />
```

## Integration with Backend

This frontend is embedded within the .NET NOIR.Web project:

```
NOIR/
├── src/
│   ├── NOIR.Domain/           # C# Domain entities
│   ├── NOIR.Application/      # C# Business logic
│   ├── NOIR.Infrastructure/   # C# Data access
│   └── NOIR.Web/              # C# Web API
│       └── frontend/          # ← This React app
```

**Key Points:**

1. **API Base:** All API calls use `/api` prefix (proxied in dev, same-origin in prod)
2. **Authentication:** Uses HTTP-only cookies for security
3. **Build Output:** Vite builds to `../wwwroot/` which .NET serves as static files
4. **Type Sync:** Use `pnpm run generate:api` to sync types from backend (see [api-types.md](api-types.md))

## Adding New Features

1. **New portal page:** Create `portal-app/{domain}/features/{feature-name}/{PageName}Page.tsx`, add lazy route in `App.tsx`
2. **New domain component:** Add to `portal-app/{domain}/components/{group-name}/`
3. **New API hook:** Add to `portal-app/{domain}/states/use{Domain}.ts` (TanStack Query)
4. **New API service:** Add to `/services/{domain}.ts`
5. **Shared component:** Add to `/components/`
6. **UI primitive:** Add to `/uikit/{name}/`, export from barrel
7. **Global state:** Add context in `/contexts`

## UI/UX Standardization Patterns

**Last Updated:** 2026-02-13

### Interactive Elements - cursor-pointer

All clickable/interactive elements MUST have `cursor-pointer` class:

```tsx
// Buttons in dialogs
<AlertDialogCancel className="cursor-pointer">Cancel</AlertDialogCancel>
<AlertDialogAction className="cursor-pointer">Confirm</AlertDialogAction>

// Icon buttons
<Button variant="ghost" size="icon" className="cursor-pointer">
  <Trash2 className="h-4 w-4" />
</Button>
```

### Accessibility - aria-labels

All icon-only buttons MUST have descriptive `aria-label`:

```tsx
// Good - describes the action and context
<Button
  variant="ghost"
  size="icon"
  className="cursor-pointer"
  aria-label={`View ${product.name} details`}
>
  <Eye className="h-4 w-4" />
</Button>

// Good - back navigation
<Button
  variant="ghost"
  size="icon"
  aria-label="Go back to products list"
>
  <ArrowLeft className="h-5 w-5" />
</Button>
```

### AlertDialog Pattern

Standard destructive dialog pattern:

```tsx
<AlertDialog open={open} onOpenChange={onOpenChange}>
  <AlertDialogContent className="border-destructive/30">
    <AlertDialogHeader>
      <div className="flex items-center gap-3">
        <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
          <AlertTriangle className="h-5 w-5 text-destructive" />
        </div>
        <div>
          <AlertDialogTitle>Delete Item</AlertDialogTitle>
          <AlertDialogDescription>
            Are you sure? This action cannot be undone.
          </AlertDialogDescription>
        </div>
      </div>
    </AlertDialogHeader>
    <AlertDialogFooter>
      <AlertDialogCancel disabled={loading} className="cursor-pointer">
        Cancel
      </AlertDialogCancel>
      <AlertDialogAction
        onClick={handleConfirm}
        disabled={loading}
        className="bg-destructive text-destructive-foreground hover:bg-destructive/90 cursor-pointer"
      >
        {loading ? 'Deleting...' : 'Delete'}
      </AlertDialogAction>
    </AlertDialogFooter>
  </AlertDialogContent>
</AlertDialog>
```

**Key Elements:**
- `border-destructive/30` on `AlertDialogContent`
- Icon container: `p-2 rounded-xl bg-destructive/10 border border-destructive/20`
- `cursor-pointer` on both Cancel and Action buttons
- Disabled state during async operations

### Card Shadow Standardization

Consistent card hover effect pattern:

```tsx
<Card className="shadow-sm hover:shadow-lg transition-all duration-300 border-border/50 backdrop-blur-sm bg-card/95">
  {/* content */}
</Card>
```

### Gradient Text

Gradient text requires `text-transparent` class:

```tsx
// Correct
<h1 className="text-3xl font-bold tracking-tight bg-gradient-to-r from-foreground to-foreground/70 bg-clip-text text-transparent">
  Page Title
</h1>

// Wrong - gradient won't show
<h1 className="text-3xl font-bold bg-gradient-to-r from-foreground to-foreground/70 bg-clip-text">
  Page Title
</h1>
```

### Confirmation Dialogs for Destructive Actions

All destructive actions (delete, remove) should use confirmation dialogs:

```tsx
// State for confirmation dialog
const [itemToDelete, setItemToDelete] = useState<ItemType | null>(null)
const [isDeleting, setIsDeleting] = useState(false)

// Trigger confirmation
<Button onClick={() => setItemToDelete(item)}>
  <Trash2 className="h-4 w-4" />
</Button>

// Confirmation dialog
<AlertDialog
  open={!!itemToDelete}
  onOpenChange={(open) => !open && setItemToDelete(null)}
>
  {/* Standard AlertDialog pattern above */}
</AlertDialog>
```

## Form Validation Standards

**Last Updated:** 2026-01-26

### Validation Timing: `mode: 'onBlur'`

All forms MUST use `mode: 'onBlur'` for consistent real-time validation. This validates fields when the user leaves (blurs) each input, providing immediate feedback without interrupting typing.

**Why `onBlur` is the standard:**
- **Better UX**: Validates after user finishes typing, not during
- **Immediate feedback**: Shows errors before form submission
- **Less intrusive**: Doesn't show errors while user is still typing
- **Consistent behavior**: All forms behave the same way

**Available modes (for reference):**
| Mode | When it validates | Use case |
|------|-------------------|----------|
| `onBlur` | When field loses focus | **Standard - use this** |
| `onChange` | Every keystroke | Too aggressive, poor UX |
| `onSubmit` | Only on form submit | Too late, poor feedback |
| `onTouched` | After first blur, then onChange | Alternative option |
| `all` | All of the above | Too aggressive |

### Standard Pattern: react-hook-form + Zod + FormField

All forms MUST use react-hook-form with Zod validation and shadcn/ui Form components:

```tsx
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import * as z from 'zod'
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@uikit'

// 1. Define Zod schema
const formSchema = z.object({
  email: z.string().min(1, 'Email is required').email('Invalid email'),
  name: z.string().min(1, 'Name is required').max(100, 'Max 100 characters'),
})

type FormValues = z.infer<typeof formSchema>

// 2. Initialize form with mode: 'onBlur'
const form = useForm<FormValues>({
  resolver: zodResolver(formSchema),
  mode: 'onBlur',  // REQUIRED - validates on blur
  defaultValues: {
    email: '',
    name: '',
  },
})

// 3. Use FormField components (labels auto-turn red on error)
<Form {...form}>
  <form onSubmit={form.handleSubmit(onSubmit)} noValidate>
    <FormField
      control={form.control}
      name="email"
      render={({ field }) => (
        <FormItem>
          <FormLabel>Email *</FormLabel>
          <FormControl>
            <Input type="text" placeholder="user@example.com" {...field} />
          </FormControl>
          <FormMessage /> {/* Auto-displays validation errors */}
        </FormItem>
      )}
    />
  </form>
</Form>
```

### Benefits of This Pattern

1. **Auto red labels**: `FormLabel` automatically adds `text-destructive` class on error
2. **Auto error messages**: `FormMessage` displays validation errors automatically
3. **Type safety**: Form values inferred from Zod schema
4. **Consistent validation**: Same behavior across all forms
5. **Less boilerplate**: No manual error/touched state management

### Anti-Pattern: Manual Error State (DEPRECATED)

Do NOT use manual `useState` for errors/touched state:

```tsx
// ❌ WRONG - Don't do this
const [errors, setErrors] = useState({})
const [touched, setTouched] = useState({})

const handleBlur = (field, value) => {
  setTouched(prev => ({ ...prev, [field]: true }))
  const error = validateField(field, value)
  setErrors(prev => ({ ...prev, [field]: error }))
}

<Label className={touched.email && errors.email ? 'text-destructive' : ''}>
  Email
</Label>
```

This pattern requires:
- 100+ lines of boilerplate code
- Manual label color management
- Manual error state tracking
- More bugs and inconsistencies

### Custom Hook: useValidatedForm

For complex forms, use the `useValidatedForm` hook which defaults to `mode: 'onBlur'`:

```tsx
import { useValidatedForm } from '@/hooks/useValidatedForm'
import { createTenantSchema } from '@/validation/schemas.generated'

const { form, handleSubmit, isSubmitting, serverError } = useValidatedForm({
  schema: createTenantSchema,
  defaultValues: { identifier: '', name: '' },
  // mode: 'onBlur' is the default
  onSubmit: async (data) => {
    await createTenant(data)
  },
})
```

### Checklist for New Forms

- [ ] Uses `react-hook-form` with `zodResolver`
- [ ] Has `mode: 'onBlur'` in useForm options
- [ ] Uses `Form`, `FormField`, `FormLabel`, `FormControl`, `FormMessage` components
- [ ] Has `noValidate` on form element (disables browser validation)
- [ ] Uses `type="text"` for email fields (avoids browser tooltip)
- [ ] Zod schema matches backend FluentValidation rules

### Migration Guide for Manual Forms

If you encounter a form using manual error state, migrate it to the standard pattern:

1. Replace `useState` for errors/touched with `useForm`
2. Add `mode: 'onBlur'` to useForm options
3. Replace `<Label>` with `<FormLabel>` inside `<FormField>`
4. Replace manual error display with `<FormMessage />`
5. Remove manual `handleBlur` and validation functions
6. Remove className conditionals for error styling

## React 19 + TanStack Query Performance Patterns

**Last Updated:** 2026-02-15

React 19 hooks (`useDeferredValue`, `useTransition`) are combined with TanStack Query v5 for smooth, lag-free UX. These patterns replace older debounce + form-submit search and manual loading states.

**Key principle:** `useOptimistic` is intentionally avoided due to known conflicts with React Query's `useSyncExternalStore` (TanStack GitHub #9742).

### Live Search with `useDeferredValue`

Replaces `useDebouncedCallback` + form submit. The input stays responsive while the query updates on lower priority:

```typescript
import { useState, useDeferredValue, useMemo } from 'react'

const [searchInput, setSearchInput] = useState('')
const deferredSearch = useDeferredValue(searchInput)
const isSearchStale = searchInput !== deferredSearch

// Derive query params via useMemo (NOT useEffect — React 19 flags useEffect setState as cascading render)
const queryParams = useMemo(() => ({ ...params, search: deferredSearch || undefined }), [params, deferredSearch])
const { data } = useProductsQuery(queryParams)

// For paginated pages, reset page in the onChange handler
<Input
  value={searchInput}
  onChange={(e) => { setSearchInput(e.target.value); setParams(prev => ({ ...prev, page: 1 })) }}
  placeholder="Search..."
/>

// Visual feedback while search catches up
<CardContent className={isSearchStale
  ? 'opacity-70 transition-opacity duration-200'
  : 'transition-opacity duration-200'}>
```

### `useTransition` for Filters, Pagination & Tabs

Wraps state updates that trigger re-renders with heavy content. Provides `isPending` for visual feedback:

```typescript
import { useTransition } from 'react'

// Filters & Pagination
const [isFilterPending, startFilterTransition] = useTransition()

const setPage = (page: number) => startFilterTransition(() =>
  setParams(prev => ({ ...prev, page }))
)
const setStatus = (status: string) => startFilterTransition(() =>
  setParams(prev => ({ ...prev, status, page: 1 }))
)

// Tab switching (settings pages)
const [isTabPending, startTabTransition] = useTransition()
const handleTabChange = (tab: string) => {
  startTabTransition(() => setActiveTab(tab))
}

// Combined visual feedback
<CardContent className={(isSearchStale || isFilterPending)
  ? 'opacity-70 transition-opacity duration-200'
  : 'transition-opacity duration-200'}>
```

### Optimistic Mutations with `useOptimisticMutation`

Shared utility at `src/hooks/useOptimisticMutation.ts` provides type-safe optimistic update helpers. Three functions cover all data shapes:

| Helper | Cache Shape | Use Case |
|--------|------------|----------|
| `optimisticListDelete` | `{ items[], totalCount }` | Delete from paginated list |
| `optimisticListPatch` | `{ items[], totalCount }` | Patch fields on paginated list item |
| `optimisticArrayDelete` | `T[]` | Delete from flat array |

**How it works:** `onMutate` snapshots + cancels queries + updates cache instantly. `onError` rolls back to snapshot. `onSettled` invalidates for server truth.

```typescript
import { optimisticListDelete, optimisticListPatch } from '@/hooks/useOptimisticMutation'

// Instant delete - row disappears immediately
export const useDeleteProduct = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deleteProduct(id),
    ...optimisticListDelete(queryClient, productKeys.lists(), productKeys.all),
  })
}

// Instant status change - badge updates immediately
export const usePublishProduct = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => publishProduct(id),
    ...optimisticListPatch(queryClient, productKeys.lists(), productKeys.all, { status: 'Active' }),
  })
}

// Flat array delete (e.g., blog categories, tags)
export const useDeleteBlogTag = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deleteBlogTag(id),
    ...optimisticArrayDelete(queryClient, blogKeys.tags(), blogKeys.all),
  })
}
```

**Type safety:** Uses structural `PagedData` interface (`{ items: { id: string }[], totalCount }`) that all paginated response types satisfy via TypeScript structural typing. No `any` types or `eslint-disable` comments.

### Query Key Pattern

Each domain module uses a factory pattern for query keys:

```typescript
// src/portal-app/products/queries/queryKeys.ts
export const productKeys = {
  all: ['products'] as const,
  lists: () => [...productKeys.all, 'list'] as const,
  list: (params: GetProductsParams) => [...productKeys.lists(), params] as const,
  detail: (id: string) => [...productKeys.all, 'detail', id] as const,
}
```

### Coverage

| Page | `useDeferredValue` | `useTransition` | Optimistic Mutations |
|------|-------------------|-----------------|---------------------|
| **Products** | search | filters, pagination, bulk ops | delete, publish, archive |
| **Product Categories** | search | — | — |
| **Product Attributes** | search | — | delete |
| **Brands** | search | — | delete |
| **Blog Posts** | search | filters, pagination | delete |
| **Blog Categories** | search | — | delete (array) |
| **Blog Tags** | search | — | delete (array) |
| **Roles** | search | pagination | delete |
| **Users** | search | filters, pagination | delete, lock, unlock |
| **Tenants** | search | pagination | delete |
| **Activity Timeline** | search | — | — |
| **Developer Logs** | search (local) | tabs | — |
| **Tenant Settings** | — | tabs | — |
| **Platform Settings** | — | tabs | — |
| **Personal Settings** | — | section nav | — |

## Code Quality

- Run `pnpm run lint` before committing
- TypeScript strict mode is enabled
- Follow existing patterns in the codebase
