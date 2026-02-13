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
| Storybook | 10.2 | Component catalog & UIKit |
| pnpm | Latest | Package manager |

## Project Location

```
src/NOIR.Web/frontend/
├── .storybook/         # Storybook configuration
├── src/
│   ├── components/     # Reusable UI components
│   │   └── ui/         # shadcn/ui primitives (56)
│   ├── uikit/          # Storybook stories (56 components)
│   ├── pages/          # Route page components
│   ├── services/       # API communication
│   ├── contexts/       # React Context providers
│   ├── hooks/          # Custom React hooks (28)
│   ├── types/          # TypeScript definitions
│   ├── config/         # App configuration
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
| Import Alias | `@/` for src/ | `@/components/ui/button` |

## Storybook & UIKit

The project includes a **Storybook** setup for interactive component documentation with 56 component stories.

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
├── ... (56 total)
```

**Path alias:** `@uikit` maps to `src/uikit/` (`tsconfig.app.json`)

### Key Custom Components

| Component | Path | Description |
|-----------|------|-------------|
| `EmptyState` | `components/ui/empty-state.tsx` | Empty state with icon, title, description, action |
| `Pagination` | `components/ui/pagination.tsx` | Pagination with page numbers and size selector |
| `ColorPicker` | `components/ui/color-picker.tsx` | Color selector with swatches and custom picker |
| `TippyTooltip` | `components/ui/tippy-tooltip.tsx` | Rich tooltips with headers and animations |
| `VirtualList` | `components/ui/virtual-list.tsx` | Virtualized list for large datasets |
| `DiffViewer` | `components/ui/diff-viewer.tsx` | Side-by-side diff viewer |
| `JsonViewer` | `components/ui/json-viewer.tsx` | Syntax-highlighted JSON display |

## AI-Assisted Development

Use `/ui-ux-pro-max` skill for all UI/UX work (research, implementation, refinement, review):

```bash
# Example prompts in Claude Code
"Build a product card component"
"What color palette for e-commerce?"
"Review my navbar for accessibility"
```
