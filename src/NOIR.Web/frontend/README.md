# NOIR Frontend

React SPA for the NOIR application.

## Quick Start

```bash
npm install
npm run dev
```

## Documentation

All frontend documentation is in `docs/frontend/`:

- **[Frontend Overview](../../../docs/frontend/README.md)** - Quick start and conventions
- **[Architecture](../../../docs/frontend/architecture.md)** - Project structure and patterns
- **[Theme](../../../docs/frontend/theme.md)** - Theme customization guide
- **[API Types](../../../docs/frontend/api-types.md)** - Type generation from backend
- **[Localization](../../../docs/frontend/localization-guide.md)** - i18n setup and languages

## Tech Stack

| Technology | Version |
|------------|---------|
| React | 19 |
| TypeScript | 5.x |
| Vite | Latest |
| Tailwind CSS | 4 |
| React Router | 7 |
| shadcn/ui | Latest |

## Commands

```bash
npm run dev          # Start dev server
npm run build        # Production build
npm run lint         # Run linter
npm run generate:api # Sync types from backend
```

## Integration

This frontend is embedded in NOIR.Web and builds to `../wwwroot/`.
