# Frontend Documentation

NOIR frontend is a React SPA embedded within the .NET NOIR.Web project.

## Technology Stack

| Technology | Version | Purpose |
|------------|---------|---------|
| React | 19 | UI library (useDeferredValue, useTransition) |
| TypeScript | 5.x | Type safety |
| Vite | Latest | Build tool & dev server |
| TanStack Query | 5 | Server state, caching, optimistic mutations |
| Tailwind CSS | 4 | Styling |
| React Router | 7 | Client-side routing |
| shadcn/ui | Latest | UI component primitives |
| Storybook | 10.2 | Component catalog & UIKit |
| pnpm | Latest | Package manager |

## Project Location

```
src/NOIR.Web/frontend/
├── .storybook/         # Storybook configuration
├── src/
│   ├── portal-app/     # Domain-driven feature modules
│   │   ├── blogs/          # Blog CMS (features, components, states)
│   │   ├── brands/         # Brand management
│   │   ├── customer-groups/# Customer group management
│   │   ├── customers/      # Customer management
│   │   ├── dashboard/      # Dashboard
│   │   ├── inventory/      # Inventory receipt management
│   │   ├── notifications/  # Notifications
│   │   ├── orders/         # Order management
│   │   ├── payments/       # Payment management
│   │   ├── products/       # Product catalog
│   │   ├── promotions/     # Promotions & discounts
│   │   ├── reports/        # Reporting & analytics
│   │   ├── reviews/        # Product reviews
│   │   ├── settings/       # Personal, Tenant, Platform settings
│   │   ├── shipping/       # Shipping management
│   │   ├── systems/        # Activity timeline, Developer logs
│   │   ├── user-access/    # Users, Roles, Tenants
│   │   ├── welcome/        # Landing, Terms, Privacy
│   │   └── wishlists/      # Customer wishlists
│   ├── layouts/        # Layout components + auth pages
│   ├── uikit/          # UI component library + stories (72 components, @uikit alias)
│   ├── components/     # Shared app-level components
│   ├── contexts/       # React Context providers
│   ├── hooks/          # Shared custom hooks
│   ├── services/       # API communication
│   ├── types/          # TypeScript definitions
│   └── lib/            # Utilities
├── package.json
├── pnpm-lock.yaml
├── vite.config.ts
└── tsconfig.json
```

## Quick Start

```bash
# Navigate to frontend
cd src/NOIR.Web/frontend

# Install dependencies
pnpm install

# Development (with .NET backend)
pnpm run dev

# Build for production
pnpm run build

# Lint
pnpm run lint
```

## Documentation

| Document | Description |
|----------|-------------|
| [Architecture](architecture.md) | Project structure and patterns |
| [React 19 + Query Patterns](architecture.md#react-19--tanstack-query-performance-patterns) | useDeferredValue, useTransition, optimistic mutations |
| [UI/UX Enhancements](ui-ux-enhancements.md) | Accessibility, offline support, command palette, and more |
| [UI/UX Patterns](architecture.md#uiux-standardization-patterns) | Required UI standardization patterns |
| [API Types](api-types.md) | Type generation from backend |
| [Localization Guide](localization-guide.md) | Managing translations and adding languages |

## Integration with Backend

- **API Base:** All API calls use `/api` prefix
- **Authentication:** HTTP-only cookies for security
- **Build Output:** Vite builds to `../wwwroot/` (served by .NET)
- **Type Sync:** Use `pnpm run generate:api` to sync types from backend

## Conventions

| Type | Convention | Example |
|------|------------|---------|
| Components | PascalCase | `LoginPage.tsx` |
| Hooks | camelCase with `use` prefix | `useAuth.ts` |
| Services | camelCase | `auth.ts` |
| Types | PascalCase | `CurrentUser` |
| Import Alias | `@/` for src/, `@uikit` for UI kit | `import { Button } from '@uikit'` |

## Storybook & UIKit

The project includes a **Storybook** setup for interactive component documentation with 72 component stories.

```bash
# Run Storybook (component catalog)
cd src/NOIR.Web/frontend
pnpm storybook          # http://localhost:6006

# Build static Storybook
pnpm build-storybook
```

### UIKit Structure

Each component has its own story file in `src/uikit/`:

```
src/uikit/
├── button/Button.stories.tsx
├── card/Card.stories.tsx
├── dialog/Dialog.stories.tsx
├── table/Table.stories.tsx
├── ... (72 total)
```

**Path alias:** `@uikit` maps to `src/uikit/` (`tsconfig.app.json`)

### Key Custom Components

| Component | Path | Description |
|-----------|------|-------------|
| `EmptyState` | `uikit/empty-state/` | Empty state with icon, title, description, action |
| `Pagination` | `uikit/pagination/` | Pagination with page numbers and size selector |
| `ColorPicker` | `uikit/color-picker/` | Color selector with swatches and custom picker |
| `TippyTooltip` | `uikit/tippy-tooltip/` | Rich tooltips with headers and animations |
| `VirtualList` | `uikit/virtual-list/` | Virtualized list for large datasets |
| `DiffViewer` | `uikit/diff-viewer/` | Side-by-side diff viewer |
| `JsonViewer` | `uikit/json-viewer/` | Syntax-highlighted JSON display |

## AI-Assisted Development

Use `/ui-ux-pro-max` skill for all UI/UX work (research, implementation, refinement, review):

```bash
# Example prompts in Claude Code
"Build a product card component"
"What color palette for e-commerce?"
"Review my navbar for accessibility"
```
