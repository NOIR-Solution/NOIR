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
- Database-driven Branding Settings per tenant (Admin UI)
- CSS custom properties applied at runtime
- Light/dark mode support

## Component Patterns

### 21st.dev Component Standard (MANDATORY)

**All frontend UI components and pages MUST use 21st.dev for consistency and best UI/UX.**

**Benefits:**
- Modern design patterns (glassmorphism, animations, micro-interactions)
- Accessible components (WCAG compliant)
- Responsive layouts (mobile-first)
- Consistent spacing, typography, and color schemes

**When to use:**
- Building new UI components or pages
- Creating forms, dialogs, tables, pagination
- Need page headers, empty states, or common patterns

**DO NOT:**
- Hand-build pagination, page headers, empty states
- Create custom form validation state management
- Write inline gradient/focus styling

**Implementation:** Use `mcp__magic__21st_magic_component_builder` tool in Claude Code

### Base Component Library
- shadcn/ui for base component primitives
- Radix UI for accessible primitives
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
