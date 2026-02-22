# Frontend Design Standards

> Gold Standard: Payment Provider Settings (`PaymentGatewaysTab.tsx`)

## Card Pattern
- Base: `shadow-sm hover:shadow-lg transition-all duration-300`
- Header: `CardTitle className="text-lg"` + `CardDescription`
- Content: `space-y-4` or `space-y-6` between children
- Never use `shadow-md` or other shadow variants

## Typography Scale
| Element | Classes |
|---------|---------|
| Page Title | Via `PageHeader` component |
| Card Title | `text-lg` (via CardTitle) |
| Card Description | `text-sm text-muted-foreground` |
| Form Label | `text-base` (via FormLabel) |
| Help Text | `text-xs text-muted-foreground` |
| Badge Text | `text-xs font-medium` |

## Button Rules
- ALL buttons: `cursor-pointer`
- Icon-only buttons: MUST have `aria-label={`Action ${item.name}`}`
- Primary: default variant, `shadow-lg hover:shadow-xl transition-all duration-300`
- Secondary: `variant="outline"`
- Destructive: `variant="destructive"`
- Size: `sm` for table actions, `default` for forms, `lg` for page CTAs
- Loading: `<Loader2 className="h-4 w-4 mr-2 animate-spin" />`

## Form Layout
- Grid: `grid gap-6 sm:grid-cols-2`
- Stack: `space-y-4` for vertical form fields
- Use react-hook-form + FormField/FormItem/FormControl/FormLabel/FormDescription/FormMessage

## Spacing
- Page sections: `space-y-6`
- Card gaps: `gap-6`
- Form field gaps: `gap-6` (grid) or `space-y-4` (stack)
- Tight elements: `gap-2`

## Interactive Elements
- ALL clickable rows: `cursor-pointer`
- ALL Tabs/TabsTrigger: `cursor-pointer`
- ALL Select/SelectTrigger: `cursor-pointer`
- ALL Switch: `cursor-pointer`
- ALL Checkbox: `cursor-pointer`
- ALL DropdownMenuItem: `cursor-pointer`

## Status Badges
- Use Badge component with semantic variants
- Green/Active: `text-green-500` on outline badge
- Red/Error: `text-destructive` or `variant="destructive"`
- Yellow/Warning: `text-yellow-500`
- Gray/Inactive: `text-muted-foreground`

## Dialog Pattern (Credenza)
- Width: `sm:max-w-[550px]`
- Header icon: `p-2 bg-primary/10 rounded-lg` with `h-5 w-5 text-primary` icon
- Footer: Cancel (outline) left, Primary action right
- MUST use `useUrlDialog` for create, `useUrlEditDialog` for edit

## Table Pattern
- Wrapper: `rounded-xl border border-border/50 overflow-hidden`
- Row hover for clickable: `hover:bg-muted/50 cursor-pointer`
- Loading: Skeleton rows with `animate-pulse`
- Empty: Use `EmptyState` component

## Loading States
- Tables: Skeleton rows
- Cards: Skeleton with `h-48 rounded-lg`
- Buttons: `disabled` + Loader2 spinner
- Pending transitions: `opacity-70 transition-opacity duration-200`

## Transitions & Animations
- Card hover: `transition-all duration-300`
- Color changes: `transition-colors duration-300`
- Opacity transitions: `transition-opacity duration-200`
- Icon rotation: `transition-transform duration-300 group-hover:rotate-180` (or rotate-90 for Plus icon)
- Never use transitions slower than 300ms

## Destructive Actions
- ALL delete/remove actions MUST have confirmation dialog
- Use AlertDialog or dedicated Delete*Dialog component
- Confirmation text should include item name

## URL State
- Tabs: `useUrlTab({ defaultTab: 'tabName' })`
- Create dialogs: `useUrlDialog({ paramValue: 'create-entity' })`
- Edit dialogs: `useUrlEditDialog<EntityType>(items)`

## Localization
- ALL user-facing text: `t('namespace.key', 'Fallback')`
- Both EN and VI locale files must have the same keys
- See `.claude/rules/localization-check.md` for full rules
