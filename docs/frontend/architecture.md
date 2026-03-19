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

**Last Updated:** 2026-03-19

### UX Contract — "Reward Early, Punish Late"

The core principle: **never show an error while the user is actively editing a field**.

| User action | Error shown? | Reason |
|---|---|---|
| Focus + blur without typing | ❌ | `isDirty=false` — untouched |
| Edit → blur with invalid value | ✅ | `isDirty=true`, unfocused |
| **Focus a field that has an error** | ❌ | `isFocused=true` — let them fix it |
| **Type in a focused field** | ❌ | Still focused — no interruption |
| Blur with now-valid value | ❌ | `reValidateMode: 'onChange'` cleared it |
| Blur with still-invalid value | ✅ | `isDirty=true`, unfocused |
| Click Submit | ✅ all except focused field | `isSubmitted=true` |
| Server error (`type="server"`) | ✅ when unfocused | Always validated |

### Two-Gate Architecture

All error display (`FormLabel` color, `FormControl` border, `FormMessage` text) uses **identical gates**:

```
Gate 1 — hasBeenValidated: isDirty OR isSubmitted OR isServerError
Gate 2 — !isFocused

showError = hasBeenValidated && !isFocused
```

`FormItem` tracks `isFocused` via `onFocusCapture`/`onBlurCapture` (with `setTimeout(0)` debounce to handle Radix popover focus shifts). All three visual indicators must always stay in sync — if any one diverges, the UX looks broken.

### Required Form Config

Every `useForm()` call **MUST** have both:

```tsx
const form = useForm<FormValues>({
  resolver: zodResolver(schema) as unknown as Resolver<FormValues>,
  mode: 'onBlur',           // validate on blur (not every keystroke)
  reValidateMode: 'onChange', // ← REQUIRED: clears error as soon as user fixes it
  defaultValues: { ... },
})
```

**Never use `reValidateMode: 'onBlur'`** — old errors persist while typing (re-validation only fires on blur), making the UX worse than `onChange`.

### Standard Pattern: react-hook-form + Zod + Form Components

```tsx
import { useForm, type Resolver } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMemo, useState } from 'react'
import {
  Form, FormControl, FormField, FormItem, FormLabel, FormMessage,
  FormErrorBanner, Input,
} from '@uikit'
import { getRequiredFields, handleFormError } from '@/lib/form'

const createSchema = (t: TFunction) => z.object({
  email: z.string().min(1, t('validation.required')).email(t('validation.invalidEmail')),
  name:  z.string().min(1, t('validation.required')).max(100, t('validation.maxLength', { count: 100 })),
})

type FormValues = z.infer<ReturnType<typeof createSchema>>

// In component:
const { t } = useTranslation('common')
const schema = useMemo(() => createSchema(t), [t])
const requiredFields = useMemo(() => getRequiredFields(schema), [schema])
const [serverErrors, setServerErrors] = useState<string[]>([])

const form = useForm<FormValues>({
  resolver: zodResolver(schema) as unknown as Resolver<FormValues>,
  mode: 'onBlur',
  reValidateMode: 'onChange',
  defaultValues: { email: '', name: '' },
})

const onSubmit = async (data: FormValues) => {
  try {
    await mutation.mutateAsync(data)
    onOpenChange(false)
  } catch (err) {
    handleFormError(err, form, setServerErrors, t)
  }
}

// JSX:
<Form {...form} requiredFields={requiredFields}>
  <form onSubmit={form.handleSubmit(onSubmit)}>
    <FormErrorBanner
      errors={serverErrors}
      onDismiss={() => setServerErrors([])}
      title={t('validation.unableToSave')}
    />
    <FormField
      control={form.control}
      name="email"
      render={({ field }) => (
        <FormItem>
          <FormLabel>{t('labels.email')}</FormLabel>  {/* asterisk auto-added */}
          <FormControl>
            <Input {...field} type="email" />
          </FormControl>
          <FormMessage />
        </FormItem>
      )}
    />
  </form>
</Form>
```

### Key Utilities

| Utility | Import | Purpose |
|---|---|---|
| `getRequiredFields(schema)` | `@/lib/form` | Introspects Zod schema → `Set<string>` of required field names for auto-asterisk |
| `handleFormError(err, form, setServerErrors, t)` | `@/lib/form` | Maps API errors to field errors or server error banner |
| `useValidatedForm(options)` | `@/hooks/useValidatedForm` | Bundles all 4 rules: `mode`, `reValidateMode`, `requiredFields`, `handleFormError` |
| `FormErrorBanner` | `@uikit` | Shows form-level server errors at top of form with dismiss |

### Required Asterisk — Auto-Detection from Schema

`FormLabel` reads `requiredFields` from `FormSchemaContext` and auto-adds a red `*` — no manual `required` prop:

```tsx
const schema = useMemo(() => createSchema(t), [t])
const requiredFields = useMemo(() => getRequiredFields(schema), [schema])

<Form {...form} requiredFields={requiredFields}>
  {/* FormLabel will auto-show * for fields not wrapped in .optional()/.nullable().optional() */}
```

### Server Error Routing

| Error type | Where to show | How |
|---|---|---|
| Field-specific (email taken, slug exists) | Inline under field | `form.setError('field', { type: 'server', message })` |
| Form-level business rule violation | Banner at top of form | `FormErrorBanner` component |
| Non-form action (delete, bulk op) | Toast | `toast.error()` — only for non-form contexts |

`handleFormError` handles both cases automatically — it maps `ValidationProblemDetails.Errors` (PascalCase) to `form.setError()` (camelCase) and puts remaining errors in the banner.

### Convenience Hook: useValidatedForm

Bundles all 4 rules for simple forms:

```tsx
import { useValidatedForm } from '@/hooks/useValidatedForm'

const { form, handleSubmit, isSubmitting, serverErrors, dismissServerErrors, requiredFields } =
  useValidatedForm({
    schema: createSchema(t),
    defaultValues: { name: '', email: '' },
    onSubmit: async (data) => {
      await mutation.mutateAsync(data)
      toast.success(t('xxx.created'))
      onOpenChange(false)
    },
  })
```

### Checklist for New Forms

- [ ] `mode: 'onBlur'` + `reValidateMode: 'onChange'` in every `useForm()`
- [ ] `requiredFields` passed to `<Form requiredFields={requiredFields}>`
- [ ] `FormErrorBanner` as first child in form body
- [ ] `handleFormError` in catch block (not `toast.error`)
- [ ] `setServerErrors([])` on dialog open / form reset
- [ ] Zod schema factories use `(t)` parameter — never hardcode strings
- [ ] `zodResolver(schema) as unknown as Resolver<T>` (not `as any`)

### Reference Implementations

| Pattern | File |
|---|---|
| Dialog form (canonical) | `BrandDialog.tsx` |
| Page-level form | `ProductFormPage.tsx` |
| Settings inline form | `SmtpSettingsTab.tsx` |
| useValidatedForm | `TenantFormValidated.tsx` |

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

## CategoryTreeView — Virtual Scroll & Drag-and-Drop

`CategoryTreeView` (`src/uikit/category-tree-view/`) uses `@headless-tree/react` + `@tanstack/react-virtual` for large hierarchical lists.

### Virtualization threshold

```tsx
const VIRTUALIZE_THRESHOLD = 40
// Use categories.length (STABLE), NOT treeItems.length (volatile during expand/collapse)
const shouldVirtualize = categories.length > VIRTUALIZE_THRESHOLD
```

`treeItems.length` fluctuates as nodes expand/collapse — this would flip between virtual/standard paths mid-interaction causing full DOM rebuilds and lag.

### Container layout (virtual path)

The scroll container and tree container MUST be **separate elements**:

```tsx
{/* Scroll container — outer, has fixed height */}
<div ref={scrollParentRef} className="overflow-auto" style={{ height: virtualHeight }}>
  {/* Tree container — inner, getContainerProps here, NOT on scroll container */}
  <div
    {...tree.getContainerProps('Category Tree')}
    style={{ height: `${rowVirtualizer.getTotalSize()}px`, position: 'relative' }}
  >
    {/* virtual items, drag line */}
  </div>
</div>
```

**Why they must be separate**: headless-tree calculates drag-line position as `item.getBoundingClientRect().top - treeContainer.getBoundingClientRect().top`. When the user scrolls, the inner container's viewport top changes (it moves up), keeping this calculation correct. If `getContainerProps` is on the scroll container instead, `container.viewport.top` stays fixed and drag-line position is wrong by `scrollTop` pixels.

### Viewport height measurement (`fillParent` mode)

`AnimatedOutlet` wraps all pages in `div.vt-main-content` with no explicit height, so `h-full` on the page resolves to `auto`. Instead of relying on CSS height chains, measure the available space with JS:

```tsx
const fillParent = maxHeight === '100%'
const toolbarRef = useRef<HTMLDivElement>(null)
const virtualHeightRef = useRef(400)
const [virtualHeight, setVirtualHeight] = useState(400)

useLayoutEffect(() => {
  if (!fillParent || !toolbarRef.current) return
  const bottom = toolbarRef.current.getBoundingClientRect().bottom
  // 72 = space-y-2 gap (8) + tree-wrapper p-4 (16) + Card py-6 (24) + main p-6 (24)
  const available = Math.max(100, Math.floor(window.innerHeight - bottom - 72))
  if (Math.abs(available - virtualHeightRef.current) > 2) {
    virtualHeightRef.current = available
    setVirtualHeight(available)
  }
}) // no deps — runs after every render; ~0.1ms; guard prevents cascading re-renders
```

Pass `maxHeight="100%"` from the page to activate this mode. The toolbar ref is placed on the element just above the scroll container.

### Drag-and-drop: use `seperateDragHandle: false`

headless-tree uses **HTML5 drag events** (`draggable`, `onDragStart`, `onDragOver`, `onDrop`).

```tsx
// ✅ CORRECT — entire row is draggable
seperateDragHandle: false,
```

```tsx
// ❌ WRONG — only the tiny grip icon is draggable; users cannot discover it
seperateDragHandle: true,  // + getDragHandleProps() on grip div
```

With `seperateDragHandle: false`, `item.getProps()` includes `draggable: true` on the row itself. The grip icon becomes a **visual-only indicator** (`pointer-events-none`):

```tsx
// Row: canDrag && 'cursor-grab active:cursor-grabbing' added to className
// Grip div: pointer-events-none, no getDragHandleProps()
<div className="flex items-center shrink-0 pointer-events-none">
  <GripVertical className="opacity-0 group-hover:opacity-70" />
</div>
```

### Performance during drag

Use `transition-colors duration-150` on tree rows, **not** `transition-all`:

```tsx
// ✅ Fast — only color transitions, avoids GPU compositing overhead on 15 visible items
'transition-colors duration-150'

// ❌ Slow — animates ring, shadow, transform on every drag event → visible lag
'transition-all duration-200 ease-out'
```

### Page integration

Pages using `CategoryTreeView` in tree mode must use `space-y-6` layout (no flex height chain):

```tsx
// ✅ Simple layout — CategoryTreeView handles its own height internally
<div className="space-y-6">
  <PageHeader ... />
  <Card ...>
    <CardContent ...>
      <div className="rounded-xl border border-border/50 p-4">
        <CategoryTreeView maxHeight="100%" categories={...} onReorder={handleReorder} />
      </div>
    </CardContent>
  </Card>
</div>
```

Never use `h-full flex flex-col` on the page wrapper or `flex-1 min-h-0` on Card/CardContent — these depend on the `h-full` chain which `AnimatedOutlet` breaks.

### Resize handling

`CategoryTreeView` uses **two** `useLayoutEffect` calls for height:

1. **No-deps** (runs every render) — captures sidebar toggles, breadcrumb changes, any React state update that affects layout
2. **`[fillParent]`-dep with resize listener** — captures raw browser window resize which doesn't trigger a React re-render

```tsx
// Dedicated resize listener — plain window resize doesn't trigger a re-render
useLayoutEffect(() => {
  if (!fillParent) return
  const handleResize = () => { /* same measurement logic */ }
  window.addEventListener('resize', handleResize)
  return () => window.removeEventListener('resize', handleResize)
}, [fillParent])
```

---

## DataTable (TanStack Table) for List Pages

**All paginated table list pages** MUST use TanStack Table via `DataTable`, `DataTableToolbar`, `DataTablePagination`, `useEnterpriseTable`, and `useTableParams`. This ensures consistent column visibility, sorting, enterprise features (density, pinning, drag-reorder, resize), and UX.

**Reference rules:**
- `.claude/rules/datatable-standard.md` — Column order, actions, visibility, image columns
- `.claude/rules/table-list-standard.md` — Card layout, search, verification
- `.claude/rules/datatable-migration.md` — Migration checklist, 100/100 verification

**Pattern:**
```tsx
const ch = createColumnHelper<ItemType>()
const columns = useMemo(() => [
  createActionsColumn<ItemType>(...),
  createSelectColumn<ItemType>(),  // if row selection
  ch.accessor('name', { header: ..., meta: { label: t('...') } }),
], [deps])

const { params, searchInput, setSearchInput, isSearchStale, setFilter, ... } = useTableParams<Filters>({ defaultPageSize })
const { data } = useQuery(params)
const { table, settings, isCustomized, resetToDefault, setDensity } = useEnterpriseTable({
  data: data?.items ?? [], columns, tableKey: 'page-key', rowCount: data?.totalCount ?? 0,
  state: { pagination: { pageIndex: params.pageIndex, pageSize: params.pageSize }, sorting: params.sorting },
  onPaginationChange, onSortingChange, enableRowSelection: true, getRowId: (row) => row.id,
})

<DataTableToolbar table={table} searchInput={...} onSearchChange={...}
  columnOrder={settings.columnOrder} onColumnsReorder={(newOrder) => table.setColumnOrder(newOrder)}
  isCustomized={isCustomized} onResetSettings={resetToDefault}
  density={settings.density} onDensityChange={setDensity} filterSlot={...} />
<DataTable table={table} density={settings.density} isLoading={...} emptyState={<EmptyState ... />} />
<DataTablePagination table={table} />
```

**Key points:**
- `useEnterpriseTable` is the unified hook — manages both server-side state (pagination, sorting) and enterprise UI state (visibility, order, sizing, pinning, density) with localStorage persistence
- Filter values: use `params.filters.role` (not `params.role`) — TypeScript `TableParams` nests filters
- Image columns: use `FilePreviewTrigger` per `.claude/rules/image-preview-in-lists.md`
- Post-migration: run UI audit — target 0 CRITICAL, 0 HIGH

---

## Table Virtualization — `useVirtualTableRows`

For non-paginated flat list pages (DepartmentsPage, BlogCategoriesPage, ProductCategoriesPage) where the TABLE mode could have hundreds of rows.

**Hook**: `src/hooks/useVirtualTableRows.ts`

### Pattern overview

Uses the **spacer-row technique** — keeps normal `<table>` layout (no `display:block` on `<tbody>`), so column widths stay consistent between header and body automatically:

```tsx
const { scrollRef, height, shouldVirtualize, virtualItems, topPad, bottomPad } =
  useVirtualTableRows(items)

// Scroll container: fixed height, overflows
<div ref={scrollRef} className="rounded-xl border border-border/50 overflow-auto" style={{ height }}>
  <Table>
    {/* Sticky header — always visible while scrolling */}
    <TableHeader className="sticky top-0 z-10 bg-background shadow-sm">
      <TableRow>...</TableRow>
    </TableHeader>
    <TableBody>
      {/* Top spacer pushes visible items to correct scroll position */}
      {topPad > 0 && (
        <TableRow><TableCell colSpan={N} className="p-0 border-0" style={{ height: topPad }} /></TableRow>
      )}
      {(shouldVirtualize ? virtualItems.map(vr => items[vr.index]) : items).map(item => (
        <TableRow key={item.id}>...</TableRow>
      ))}
      {/* Bottom spacer reserves space for non-rendered rows */}
      {bottomPad > 0 && (
        <TableRow><TableCell colSpan={N} className="p-0 border-0" style={{ height: bottomPad }} /></TableRow>
      )}
    </TableBody>
  </Table>
</div>
```

### Critical: callback ref, not `useRef`

The table div only renders when `viewMode === 'table'` (default is `'tree'`). Using a plain `useRef` causes `useLayoutEffect(fn, [])` to fire at mount with `ref.current = null`, and height measurement never runs.

**Fix**: use a **callback ref** so measurement fires when the element actually attaches:

```ts
// ✅ Callback ref — fires when element mounts, even in conditional renders
const [scrollEl, setScrollEl] = useState<HTMLDivElement | null>(null)
const scrollRef = useCallback((el: HTMLDivElement | null) => setScrollEl(el), [])

useLayoutEffect(() => {
  if (!scrollEl) return
  const measure = () => {
    const { top } = scrollEl.getBoundingClientRect()
    setHeight(Math.max(200, Math.floor(window.innerHeight - top - BOTTOM_GAP)))
  }
  measure()
  window.addEventListener('resize', measure)
  return () => window.removeEventListener('resize', measure)
}, [scrollEl]) // re-runs when element mounts/unmounts
```

```ts
// ❌ useRef — useLayoutEffect(fn, []) fires at initial mount when element is not yet in DOM
const scrollRef = useRef<HTMLDivElement>(null)  // null at mount → height stays 400px forever
useLayoutEffect(() => { /* never measures */ }, [])
```

### Bottom gap constant

`BOTTOM_GAP = 48` = CardContent `p-6` bottom (24px) + main `p-6` bottom (24px).

Compare with tree's `72` = `space-y-2` (8) + `tree-wrapper p-4` (16) + `Card py-6` (24) + `main p-6` (24). Tree has extra inner wrapper padding; table does not.

### When NOT to virtualize

All other list pages in NOIR use **server-side pagination** (`page/pageSize` + `<Pagination>` component) — they already limit DOM nodes and do NOT need `useVirtualTableRows`. Only apply to pages that load ALL items at once (currently: the three category pages).

## Code Quality

- Run `pnpm run lint` before committing
- TypeScript strict mode is enabled
- Follow existing patterns in the codebase
