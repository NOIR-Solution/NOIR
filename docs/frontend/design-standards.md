# NOIR Frontend Design Standards

> Auto-generated from Phase 0 audit. Patterns verified against 44 pages and 96 Storybook stories.

> Gold Standard: Payment Provider Settings (`PaymentGatewaysTab.tsx` + `GatewayCard.tsx`)

## Card Patterns

### List Page Cards
- `shadow-sm hover:shadow-lg transition-all duration-300`
- CardHeader: `pb-4`
- CardContent: opacity transitions for loading states

### Detail Page Cards
- `shadow-sm hover:shadow-lg transition-all duration-300 gap-4 py-5`
- Container: `container max-w-6xl py-6 space-y-6`
- Grid layout: `grid grid-cols-1 lg:grid-cols-3 gap-6`
- Column spacing: `space-y-6`

### Stateful Cards (Config/Gateway)
- Active: `ring-2 ring-green-500/50 shadow-green-500/10 shadow-lg`
- Disabled: `opacity-75`
- Transitions: `transition-all duration-200`

## Table Patterns
- Container: `rounded-xl border border-border/50 overflow-hidden`
- Row hover: `group transition-colors hover:bg-muted/50 cursor-pointer`
- Sticky column: `w-10 sticky left-0 z-10 bg-background`
- Loading: 5 skeleton rows with `animate-pulse`
- Empty state: `EmptyState` component, `border-0 rounded-none px-4 py-12`

## Button Patterns
- Primary action: `shadow-lg hover:shadow-xl transition-all duration-300`
- Create button icon: `group-hover:rotate-90 duration-300`
- Icon sizing: `h-4 w-4 mr-2`
- ALL buttons: `cursor-pointer`
- Loading: `<Loader2 className="h-4 w-4 mr-2 animate-spin" />`
- Size: `sm` for table actions, `default` for forms, `lg` for page CTAs

## Filter Patterns
- SelectTrigger: `h-9 cursor-pointer`
- Search input: `pl-9 h-9` with icon at `left-3 top-1/2 -translate-y-1/2`

## Typography
- Headings: `text-lg font-semibold`
- Body/labels: `text-sm`
- Meta info: `text-xs text-muted-foreground`
- Section dividers: `text-xs text-muted-foreground uppercase tracking-wider`

## Spacing
- Section gaps: `gap-6` (24px) or `space-y-4` (16px)
- Medium gaps: `gap-3` (12px)
- Button groups: `gap-2` (8px)
- Tight spacing: `gap-1` (4px)
- CardHeader bottom: `pb-3` or `pb-4`

## Color Semantics
- Primary: `bg-primary/10`, `text-primary`
- Success: `text-green-500`, `bg-green-500/10`
- Warning: `text-yellow-500`, `text-yellow-600`
- Error: `text-red-600`, `bg-red-50`, `text-destructive`, `bg-destructive/10`
- Muted: `text-muted-foreground`, `bg-muted`

## Error States
- Container: `p-4 rounded-lg bg-destructive/10 border border-destructive/20 text-destructive`
- Retry button: with `group-hover:rotate-180` icon animation

## Status Badges
- Badge: `variant="outline"` with status-specific color
- Health indicators: icon + text with matching color (`text-green-500`, `text-yellow-500`, `text-red-600`)

## Dialog Patterns
- Width: `sm:max-w-[550px]` (standard), adjust for content
- Header icon: `p-2 bg-primary/10 rounded-lg` with `h-5 w-5 text-primary`
- Footer: Cancel (outline) left, Submit (default) right
- Form spacing: `space-y-4`
- MUST use `useUrlDialog` for create, `useUrlEditDialog` for edit

## Accessibility
- `cursor-pointer` on ALL interactive elements (Buttons, Tabs, Checkboxes, Select, DropdownMenu, Switch)
- `aria-label` on ALL icon-only buttons (contextual, e.g., `Delete ${item.name}`)
- Confirmation dialogs for ALL destructive actions

## Responsive
- Grid: `grid gap-6 sm:grid-cols-2`
- Dialog: Credenza handles mobile responsiveness
- TabsList: `flex-wrap h-auto` for wrapping

## Interactive Elements
- ALL clickable rows: `cursor-pointer`
- ALL Tabs/TabsTrigger: `cursor-pointer`
- ALL Select/SelectTrigger: `cursor-pointer`
- ALL Switch: `cursor-pointer`
- ALL Checkbox: `cursor-pointer`
- ALL DropdownMenuItem: `cursor-pointer`

## Form Layout
- Grid: `grid gap-6 sm:grid-cols-2`
- Stack: `space-y-4` for vertical form fields
- Validation: react-hook-form + Zod + `mode: 'onBlur'`

## Loading States
- Tables: Skeleton rows with `animate-pulse`
- Cards: Skeleton with matching layout dimensions
- Buttons: `disabled` + Loader2 spinner
- Pending transitions: `opacity-70 transition-opacity duration-200`

## Transitions & Animations
- Card hover: `transition-all duration-300`
- Color changes: `transition-colors duration-300`
- Opacity transitions: `transition-opacity duration-200`
- Icon rotation: `transition-transform duration-300 group-hover:rotate-180` (or rotate-90 for Plus icon)
- Never use transitions slower than 300ms

## URL State
- Tabs: `useUrlTab({ defaultTab: 'tabName' })`
- Create dialogs: `useUrlDialog({ paramValue: 'create-entity' })`
- Edit dialogs: `useUrlEditDialog<EntityType>(items)`

## Localization
- ALL user-facing text: `t('namespace.key', 'Fallback')`
- Both EN and VI locale files must have the same keys
- See `.claude/rules/localization-check.md` for full rules
