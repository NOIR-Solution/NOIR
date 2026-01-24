# Frontend Architecture

This document describes the architecture and conventions for the NOIR frontend application.

## Technology Stack

| Technology | Purpose |
|------------|---------|
| React 19 | UI library |
| TypeScript | Type safety |
| Vite | Build tool & dev server |
| Tailwind CSS 4 | Styling |
| React Router 7 | Client-side routing |
| shadcn/ui | UI component primitives |

## Project Structure

```
frontend/
├── src/
│   ├── App.tsx              # App shell with routing
│   ├── main.tsx             # React root entry point
│   ├── index.css            # Global styles + Tailwind
│   │
│   ├── components/          # Reusable UI components
│   │   ├── ui/              # shadcn/ui primitives (button, input, etc.)
│   │   ├── PermissionGate.tsx  # Permission-based rendering
│   │   ├── ProtectedRoute.tsx  # Route-level protection
│   │   └── *.tsx            # App-level shared components
│   │
│   ├── contexts/            # React Context providers
│   │   └── AuthContext.tsx  # Authentication state
│   │
│   ├── hooks/               # Custom React hooks
│   │   └── usePermissions.ts  # Permission checking utilities
│   │
│   ├── lib/                 # Utility libraries
│   │   └── utils.ts         # Helper functions (cn, etc.)
│   │
│   ├── pages/               # Page components (route targets)
│   │   ├── Login.tsx
│   │   └── Home.tsx
│   │
│   ├── services/            # API service functions
│   │   ├── apiClient.ts     # Centralized API client with auth
│   │   └── auth.ts          # Authentication API calls
│   │
│   └── types/               # TypeScript type definitions
│       ├── api.ts           # Common API types
│       ├── auth.ts          # Auth-specific types
│       └── index.ts         # Barrel export
│
├── package.json
├── tsconfig.json
├── vite.config.ts
└── eslint.config.js
```

## Folder Conventions

### `/components`

**Purpose:** Reusable UI components shared across pages.

**Guidelines:**
- `ui/` contains shadcn/ui primitives - do not modify heavily
- Other components should be self-contained and reusable
- Name files in PascalCase matching component name

**Key UI Components:**
| Component | Purpose | Dependencies |
|-----------|---------|--------------|
| `button.tsx` | Button with variants | - |
| `input.tsx` | Form input | - |
| `dialog.tsx` | Modal dialogs | @radix-ui/react-dialog |
| `popover.tsx` | Popover menus | @radix-ui/react-popover |
| `calendar.tsx` | Date picker calendar | react-day-picker v9 |
| `date-range-picker.tsx` | Date range selection | calendar, popover, date-fns |
| `tippy-tooltip.tsx` | Rich tooltips with headers | @tippyjs/react, tippy.js |

**Example:**
```tsx
// components/ProtectedRoute.tsx
export function ProtectedRoute({ children }) { ... }

// components/ui/date-range-picker.tsx
import { DateRange } from 'react-day-picker'

export function DateRangePicker({
  value,
  onChange,
  numberOfMonths = 2,
}: DateRangePickerProps) { ... }
```

### `/pages`

**Purpose:** Top-level page components rendered by routes.

**Guidelines:**
- One component per route
- Keep page components focused on layout and composition
- Extract reusable logic into hooks or components

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

Use `@/` alias for all imports from `src/`:

```tsx
// Correct
import { Button } from '@/components/ui/button'
import type { CurrentUser } from '@/types'

// Avoid relative paths
import { Button } from '../../components/ui/button'
```

## Naming Conventions

| Type | Convention | Example |
|------|------------|---------|
| Components | PascalCase | `LoginPage.tsx`, `Button.tsx` |
| Hooks | camelCase with `use` prefix | `useAuth.ts` |
| Services | camelCase | `auth.ts` |
| Types | PascalCase | `CurrentUser`, `ApiError` |
| Constants | SCREAMING_SNAKE_CASE | `API_BASE`, `MAX_RETRY` |

## When to Add Feature Folders

The current flat structure works well for small-to-medium apps. Consider feature-based organization when:

1. **Multiple developers** work on distinct features simultaneously
2. **Feature complexity** grows (5+ files per feature)
3. **Code ownership** needs to be clear

**Feature folder structure (future):**
```
src/
├── features/
│   ├── auth/
│   │   ├── components/
│   │   ├── hooks/
│   │   ├── services/
│   │   └── types.ts
│   ├── dashboard/
│   └── settings/
└── shared/
    ├── components/
    ├── hooks/
    └── utils/
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
4. **Type Sync:** Use `npm run generate:api` to sync types from backend (see [api-types.md](api-types.md))

## Adding New Features

1. **Simple page:** Add to `/pages`, update routes in `App.tsx`
2. **New API:** Add service in `/services`, types in `/types`
3. **Shared component:** Add to `/components`
4. **Global state:** Add context in `/contexts`

## Code Quality

- Run `npm run lint` before committing
- TypeScript strict mode is enabled
- Follow existing patterns in the codebase
