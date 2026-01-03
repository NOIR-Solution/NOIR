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
