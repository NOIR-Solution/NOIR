# UI/UX Enhancements

**Last Updated:** 2026-01-29

11 UI/UX enhancements for the NOIR portal.

---

## Table of Contents

1. [Accessibility](#accessibility) - Skip Links, Screen Reader, High Contrast
2. [Network & Notifications](#network--notifications) - Offline Indicator, Toast
3. [Forms & Data Entry](#forms--data-entry) - Smart Defaults, Auto-Save, Unsaved Changes, Soft Delete
4. [Advanced UX](#advanced-ux) - Page Transitions, Virtual Scrolling, Command Palette

---

## Accessibility

### Skip Links

**File:** `src/components/accessibility/SkipLink.tsx`

```tsx
import { SkipLink } from '@/components/accessibility/SkipLink'

<SkipLink targetId="main-content" />
<main id="main-content">...</main>
```

| Prop | Type | Description |
|------|------|-------------|
| `targetId` | `string` | ID of element to skip to |
| `children` | `ReactNode` | Custom link text (default: 'Skip to main content') |

---

### Screen Reader Announcer

**File:** `src/contexts/AccessibilityContext.tsx`

```tsx
import { useAccessibility } from '@/contexts/AccessibilityContext'

const { announce } = useAccessibility()
announce('Changes saved', 'polite')    // Non-urgent
announce('Error occurred', 'assertive') // Urgent (interrupts)
```

---

### High Contrast Mode

**Files:** `src/contexts/ThemeContext.tsx`, `src/index.css`

```tsx
import { useTheme } from '@/contexts/ThemeContext'

const { theme, setTheme, isHighContrast } = useTheme()
setTheme('high-contrast') // WCAG AAA 7:1 contrast
```

Theme values: `'light' | 'dark' | 'system' | 'high-contrast'`

---

## Network & Notifications

### Offline Indicator

**Files:** `src/hooks/useNetworkStatus.ts`, `src/components/network/OfflineIndicator.tsx`

```tsx
import { useNetworkStatus } from '@/hooks/useNetworkStatus'

const { isOnline, wasOffline, reconnectedAt } = useNetworkStatus()
```

The `OfflineIndicator` is integrated in `PortalLayout.tsx` - shows amber banner when offline.

---

### Enhanced Toast

**File:** `src/lib/toast.ts`

```tsx
import { toast } from '@/lib/toast'

// Standard
toast.success('Created')
toast.error('Failed')
toast.loading('Processing...')

// With undo action
toast.undo('Deleted', { onUndo: () => restore(id) })

// Progress tracking
const p = toast.progress({ title: 'Uploading', description: '0/10' })
p.update(50, '5/10')
p.success('Done')

// With action button
toast.withAction('New message', {
  action: { label: 'View', onClick: () => navigate('/messages') }
})
```

---

## Forms & Data Entry

### Smart Defaults

**Files:** `src/lib/utils/sku.ts`, `src/hooks/useSmartDefaults.ts`

```tsx
import { generateSKU, isValidSKU } from '@/lib/utils/sku'

generateSKU('Blue Widget', 'ELC')  // "ELC-BLUE-A1B2"
isValidSKU('ELC-BLUE-A1B2')        // true
```

```tsx
import { useSmartDefaults } from '@/hooks/useSmartDefaults'

const { duplicates, isChecking } = useSmartDefaults({
  form,
  sourceField: 'name',
  targetFields: { slug: 'slug', sku: 'sku' },
  isEditing: false,
  checkDuplicate: async (field, value) => api.checkExists(field, value),
})
```

---

### Form Auto-Save

**File:** `src/hooks/useAutoSave.ts`

```tsx
import { useAutoSave } from '@/hooks/useAutoSave'

const { clearDraft, hasDraft, lastSavedAt } = useAutoSave({
  form,
  storageKey: `post-draft-${postId || 'new'}`,
  debounceMs: 2000,
  maxAge: 24 * 60 * 60 * 1000, // 24 hours
  onRestore: (data) => toast.info('Draft restored'),
})
```

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `storageKey` | `string` | required | localStorage key |
| `debounceMs` | `number` | `2000` | Save delay |
| `maxAge` | `number` | `86400000` | Max draft age (ms) |
| `onRestore` | `fn` | - | Callback on restore |

---

### Unsaved Changes Warning

**File:** `src/hooks/useUnsavedChanges.ts`

```tsx
import { useUnsavedChanges } from '@/hooks/useUnsavedChanges'

const { blocker, proceed, cancel } = useUnsavedChanges({
  isDirty: form.formState.isDirty,
})

// Show dialog when blocker.state === 'blocked'
```

---

### Soft Delete with Undo

**File:** `src/hooks/useSoftDelete.ts`

```tsx
import { useSoftDelete } from '@/hooks/useSoftDelete'

const { handleDelete, isPending } = useSoftDelete({
  onDelete: (item) => api.delete(item.id),
  onRestore: (item) => api.restore(item.id),
  getItemName: (item) => item.name,
  entityType: 'Product',
  undoDuration: 5000,
})
```

---

## Advanced UX

### Page Transitions

**File:** `src/components/layout/AnimatedOutlet.tsx`

Integrated in `PortalLayout.tsx`. Uses Framer Motion with:
- Fade + slide animation (150-200ms)
- Respects `prefers-reduced-motion`

---

### Virtual Scrolling

**File:** `src/components/ui/virtual-list.tsx`

```tsx
import { VirtualList } from '@/components/ui/virtual-list'

<VirtualList
  items={logs}
  estimateSize={48}
  renderItem={(log) => <LogEntry log={log} />}
  getItemKey={(log) => log.id}
  height="calc(100vh - 200px)"
  overscan={5}
/>
```

---

### Command Palette

**Files:** `src/components/command-palette/`

| Shortcut | Action |
|----------|--------|
| `Cmd/Ctrl + K` | Open |
| `Escape` | Close |
| `Enter` | Execute |
| `Arrow Up/Down` | Navigate |

Features: Navigation, quick actions, permission-aware, fuzzy search.

```tsx
import { useCommand } from '@/components/command-palette'

const { open, toggle } = useCommand()
```

---

## Integration Status

| Component | Location | Status |
|-----------|----------|--------|
| AccessibilityProvider | `App.tsx` | Integrated |
| CommandProvider | `App.tsx` | Integrated |
| CommandPalette | `App.tsx` | Integrated |
| SkipLink | `PortalLayout.tsx` | Integrated |
| AnimatedOutlet | `PortalLayout.tsx` | Integrated |
| OfflineIndicator | `PortalLayout.tsx` | Integrated |

---

## Dependencies

**Required:**
- `cmdk` ^1.0.4
- `@tanstack/react-virtual` ^3.10.9

**Existing (used):**
- `framer-motion` - Animations
- `sonner` - Toast base
- `react-router-dom` - Navigation blocking
- `react-hook-form` - Form integration
