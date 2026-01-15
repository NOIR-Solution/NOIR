# Dark/Light Mode Design

**Date:** 2026-01-16
**Status:** Implemented

## Overview

Add dark/light mode toggle to the NOIR application with:
- System preference detection on first visit
- Manual toggle in user profile dropdown
- Persistent preference storage via localStorage

## Design Decisions

### Theme Storage Strategy
- **Storage key:** `noir-theme`
- **Values:** `"light"` | `"dark"` | `"system"`
- **Default:** `"system"` (respects OS preference)
- **Behavior:** Once user manually selects a theme, their choice persists across sessions

### UI Placement
- **Location:** Sidebar footer, just above user profile section
- **Style:** Segmented toggle with animated sliding indicator (21st.dev design)
- **Labels:** "Light" with Sun icon, "Dark" with Moon icon
- **Collapsed sidebar:** Shows compact icon button that toggles theme on click

### Technical Implementation

#### Files Created
1. `src/contexts/ThemeContext.tsx` - Theme state management
2. `src/components/ui/theme-toggle.tsx` - Toggle UI component

#### Files Modified
1. `src/App.tsx` - Added ThemeProvider wrapper
2. `src/components/portal/Sidebar.tsx` - Added toggle to user dropdown
3. `public/locales/en/common.json` - Added "appearance" translation
4. `public/locales/vi/common.json` - Added Vietnamese translation

#### Dependencies Added
- `framer-motion` - Animation library for sliding indicator

## Architecture

```
ThemeProvider (App.tsx)
    ├── Detects system preference via matchMedia
    ├── Reads/writes localStorage for persistence
    ├── Applies .dark class to document.documentElement
    └── Provides context: { theme, resolvedTheme, setTheme, toggleTheme }

ThemeToggle (Sidebar dropdown)
    ├── Segmented control with Light/Dark buttons
    ├── Animated sliding background indicator
    └── Calls setTheme() on click
```

## Testing

1. First visit: Should match OS dark/light preference
2. Toggle to Dark: Should apply immediately, persist on reload
3. Toggle to Light: Should apply immediately, persist on reload
4. Clear localStorage: Should return to system preference
