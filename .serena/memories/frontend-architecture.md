# NOIR Frontend Architecture

## Location
`src/NOIR.Web/frontend/`

## Tech Stack
| Technology | Version | Purpose |
|------------|---------|---------|
| React | 19 | UI framework |
| TypeScript | 5.x | Type safety |
| Vite | Latest | Build tool |
| Tailwind CSS | 4 | Styling |
| shadcn/ui | Latest | Component library |
| React Router | 7 | Routing |

## Project Structure
```
frontend/
├── src/
│   ├── components/    # Reusable UI components (ui/, notifications/, settings/, etc.)
│   ├── config/        # App configuration (theme, etc.)
│   ├── contexts/      # React contexts (Auth, Notification)
│   ├── hooks/         # Custom React hooks (useLogin, useSignalR, etc.)
│   ├── i18n/          # Internationalization (i18next)
│   ├── layouts/       # Layout components (PortalLayout)
│   ├── lib/           # Utilities and helpers
│   ├── pages/         # Route pages (portal/, forgot-password/, etc.)
│   ├── services/      # API client functions
│   ├── types/         # TypeScript types (including generated API types)
│   └── validation/    # Generated Zod schemas from backend
├── public/            # Static assets
└── index.html         # Entry point
```

## Commands
```bash
cd src/NOIR.Web/frontend
npm install
npm run dev          # Start dev server
npm run build        # Production build
npm run lint         # Run linter
npm run generate:api # Sync types from backend
```

## API Type Generation
Backend OpenAPI spec generates TypeScript types:
```bash
npm run generate:api
```

This creates types matching backend DTOs for type-safe API calls.

## Build Output
Frontend builds to `src/NOIR.Web/wwwroot/` for embedding in .NET app.

## Theme System
- CSS custom properties for theming
- Light/dark mode support
- See `docs/frontend/theme.md` for customization

## Component Patterns
- shadcn/ui for base components
- 21st.dev for additional components
- Tailwind for custom styling

## Cross-Component Communication
Use custom events for profile data changes that affect multiple components:

```tsx
// ProfileForm.tsx - Notify after profile changes
const notifyProfileChanged = () => window.dispatchEvent(new Event('avatar-updated'))

await refreshUser()
notifyProfileChanged()  // Sidebar will refresh
```

```tsx
// Sidebar.tsx - Listen for changes
useEffect(() => {
  const handleAvatarUpdate = () => { checkAuth() }
  window.addEventListener('avatar-updated', handleAvatarUpdate)
  return () => window.removeEventListener('avatar-updated', handleAvatarUpdate)
}, [checkAuth])
```

**Dispatch `avatar-updated` when:**
- Avatar upload/remove
- Email change (avatar color uses `getAvatarColor(email)`)
- Name change (Sidebar shows displayName)
