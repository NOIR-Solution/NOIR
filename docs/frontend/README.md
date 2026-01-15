# Frontend Documentation

NOIR frontend is a React SPA embedded within the .NET NOIR.Web project.

## Technology Stack

| Technology | Version | Purpose |
|------------|---------|---------|
| React | 19 | UI library |
| TypeScript | 5.x | Type safety |
| Vite | Latest | Build tool & dev server |
| Tailwind CSS | 4 | Styling |
| React Router | 7 | Client-side routing |
| shadcn/ui | Latest | UI component primitives |
| 21st.dev | Latest | AI-assisted component generation |

## Project Location

```
src/NOIR.Web/frontend/
├── src/
│   ├── components/     # Reusable UI components
│   ├── pages/          # Route page components
│   ├── services/       # API communication
│   ├── contexts/       # React Context providers
│   ├── types/          # TypeScript definitions
│   ├── config/         # App configuration
│   └── lib/            # Utilities
├── package.json
├── vite.config.ts
└── tsconfig.json
```

## Quick Start

```bash
# Navigate to frontend
cd src/NOIR.Web/frontend

# Install dependencies
npm install

# Development (with .NET backend)
npm run dev

# Build for production
npm run build

# Lint
npm run lint
```

## Documentation

| Document | Description |
|----------|-------------|
| [Architecture](architecture.md) | Project structure and patterns |
| [Theme](theme.md) | Theme customization guide |
| [API Types](api-types.md) | Type generation from backend |
| [Localization Guide](localization-guide.md) | Managing translations and adding languages |

## Integration with Backend

- **API Base:** All API calls use `/api` prefix
- **Authentication:** HTTP-only cookies for security
- **Build Output:** Vite builds to `../wwwroot/` (served by .NET)
- **Type Sync:** Use `npm run generate:api` to sync types from backend

## Conventions

| Type | Convention | Example |
|------|------------|---------|
| Components | PascalCase | `LoginPage.tsx` |
| Hooks | camelCase with `use` prefix | `useAuth.ts` |
| Services | camelCase | `auth.ts` |
| Types | PascalCase | `CurrentUser` |
| Import Alias | `@/` for src/ | `@/components/ui/button` |

## AI-Assisted Development

Use 21st.dev Magic MCP for component generation:

```bash
# Example commands in Claude Code / Cursor
/ui create a modern login form
/ui create an admin dashboard sidebar
/ui create a data table with pagination
```

## Custom 21st.dev Components

The following components were created using 21st.dev for consistent, high-quality UI:

| Component | Path | Description |
|-----------|------|-------------|
| `EmptyState` | `components/ui/empty-state.tsx` | Empty state display with icon, title, description, and optional action button |
| `Pagination` | `components/ui/pagination.tsx` | Full-featured pagination with first/prev/next/last navigation and page numbers |
| `ColorPicker` | `components/ui/color-picker.tsx` | Color selector with preset swatches and custom color picker |

### EmptyState

Used in tables when no data is available.

```tsx
import { EmptyState } from '@/components/ui/empty-state'
import { Users } from 'lucide-react'

<EmptyState
  icon={Users}
  title="No users found"
  description="Create a new user to get started."
  action={{ label: "Create User", onClick: () => {} }}
/>
```

### Pagination

Used for paginated data tables.

```tsx
import { Pagination } from '@/components/ui/pagination'

<Pagination
  currentPage={1}
  totalPages={10}
  totalItems={100}
  pageSize={10}
  onPageChange={(page) => setPage(page)}
  showPageSizeSelector={true}
/>
```

### ColorPicker

Used for selecting colors (e.g., role colors).

```tsx
import { ColorPicker } from '@/components/ui/color-picker'

<ColorPicker
  value="#3B82F6"
  onChange={(color) => setColor(color)}
  showCustomInput={true}  // Shows hex input and native picker
/>
```

### TippyTooltip

Modern tooltip component powered by Tippy.js with smooth animations and custom styling.

```tsx
import { TippyTooltip, RichTooltip } from '@/components/ui/tippy-tooltip'

// Simple tooltip
<TippyTooltip content="This is a tooltip">
  <button>Hover me</button>
</TippyTooltip>

// Rich tooltip with header and list
<RichTooltip
  title="Search across:"
  items={[
    'Entity ID, Correlation ID',
    'User email',
    'Handler name, HTTP path',
  ]}
>
  <HelpCircle className="h-4 w-4" />
</RichTooltip>
```

**Features:**
- Smooth `shift-away-subtle` animation
- Blue gradient header matching theme primary color
- Arrow pointing to trigger element
- Dark mode support
- Interactive tooltips for clickable content

**Custom Theme:** Styles defined in `src/styles/tippy-custom.css` with:
- Layered shadows (Vercel-style)
- Rounded corners with proper overflow handling
- Blue arrow matching header color
