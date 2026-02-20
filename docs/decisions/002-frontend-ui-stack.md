# Frontend UI Stack Decision

**Date:** 2026-01-03
**Status:** Decided (Updated 2026-02-13)
**Decision:** React + shadcn/ui + Tailwind CSS + Storybook

---

## Context

NOIR needs a modern frontend for:
1. Admin login page (cookie auth for Hangfire dashboard)
2. Full React SPA frontend
3. AI-assisted development workflow

## Decision

| Technology | Purpose |
|------------|---------|
| **React 19** | UI framework |
| **shadcn/ui** | Component library (Radix UI + Tailwind) |
| **Tailwind CSS 4** | Utility-first CSS |

> **Note:** Initial decision was 21st.dev, but shadcn/ui was adopted for its maturity, copy-paste ownership model, and better Radix UI integration.

## Why shadcn/ui?

### Key Features
- **Built on Radix UI** - Accessible, unstyled primitive components
- **Copy-paste model** - You own the code, no lock-in, no version conflicts
- **TypeScript-first** - Full type safety for all components
- **Tailwind CSS styling** - Fully customizable with utility classes
- **Active community** - 60k+ GitHub stars, widely adopted

### Current Implementation
- 58 UI components in `src/uikit/`
- 103 total components (including custom)
- 72 Storybook stories in `src/uikit/` (interactive component catalog)
- 28 custom React hooks
- OKLCH color system (see `docs/frontend/COLOR_SCHEMA_GUIDE.md`)
- pnpm for disk-optimized dependency management

## Storybook (Added 2026-02-13)

Storybook v10.2.8 provides an interactive component catalog:

```bash
cd src/NOIR.Web/frontend
pnpm storybook          # http://localhost:6006
```

- **Config:** `.storybook/main.ts` (React + Vite + Tailwind CSS 4)
- **Stories:** 72 component stories in `src/uikit/{component}/`
- **Path alias:** `@uikit` → `src/uikit/`

## References

- [shadcn/ui](https://ui.shadcn.com) - Component library
- [Radix UI](https://www.radix-ui.com) - Accessible primitives
- [Tailwind CSS](https://tailwindcss.com) - Utility-first CSS
- [Storybook](https://storybook.js.org) - Component catalog
