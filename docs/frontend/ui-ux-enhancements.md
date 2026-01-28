# UI/UX Enhancements Documentation

**Last Updated:** 2026-01-28
**Version:** 1.0

This document covers the 11 UI/UX enhancements implemented for the NOIR frontend portal.

---

## Table of Contents

- [Phase 1: Accessibility Foundation](#phase-1-accessibility-foundation)
  - [Skip Links](#skip-links)
  - [Screen Reader Announcer](#screen-reader-announcer)
  - [High Contrast Mode](#high-contrast-mode)
- [Phase 2: Network & Notifications](#phase-2-network--notifications)
  - [Offline Support Indicator](#offline-support-indicator)
  - [Enhanced Toast Notifications](#enhanced-toast-notifications)
- [Phase 3: Forms & Data Entry](#phase-3-forms--data-entry)
  - [Smart Defaults & Autofill](#smart-defaults--autofill)
  - [Form Auto-Save](#form-auto-save)
  - [Unsaved Changes Warning](#unsaved-changes-warning)
  - [Soft Delete with Undo](#soft-delete-with-undo)
- [Phase 4: Advanced UX](#phase-4-advanced-ux)
  - [Page Transition Animations](#page-transition-animations)
  - [Virtual Scrolling](#virtual-scrolling)
  - [Command Palette](#command-palette)
- [Integration Guide](#integration-guide)
- [Dependencies](#dependencies)

---

## Phase 1: Accessibility Foundation

### Skip Links

**File:** `src/components/accessibility/SkipLink.tsx`

Provides keyboard-accessible skip navigation for screen reader users and keyboard navigators.

#### Usage

```tsx
import { SkipLink } from '@/components/accessibility/SkipLink'

// In your layout
<SkipLink targetId="main-content" />
<main id="main-content">...</main>
```

#### Props

| Prop | Type | Default | Description |
|------|------|---------|-------------|
| `targetId` | `string` | required | ID of the element to skip to |
| `children` | `ReactNode` | `'Skip to main content'` | Custom link text |

#### Behavior

- Visually hidden by default
- Appears on focus (Tab key)
- Uses i18n for localized text
- Styled with high-contrast focus ring

---

### Screen Reader Announcer

**File:** `src/contexts/AccessibilityContext.tsx`

Provides programmatic announcements to screen readers via ARIA live regions.

#### Usage

```tsx
import { useAccessibility } from '@/contexts/AccessibilityContext'

function MyComponent() {
  const { announce } = useAccessibility()

  const handleSave = async () => {
    await saveData()
    announce('Changes saved successfully', 'polite')
  }

  const handleError = () => {
    announce('Error: Failed to save', 'assertive')
  }
}
```

#### API

| Function | Parameters | Description |
|----------|------------|-------------|
| `announce` | `(message: string, priority?: 'polite' \| 'assertive')` | Announce message to screen readers |

#### Priority Levels

- **polite**: Non-urgent announcements (default). Waits for user pause.
- **assertive**: Urgent announcements. Interrupts current speech.

---

### High Contrast Mode

**Files:**
- `src/contexts/ThemeContext.tsx`
- `src/index.css`

Adds a high contrast theme option meeting WCAG AAA (7:1) contrast requirements.

#### Theme Values

```typescript
type Theme = 'light' | 'dark' | 'system' | 'high-contrast'
```

#### Usage

```tsx
import { useTheme } from '@/contexts/ThemeContext'

function ThemeToggle() {
  const { theme, setTheme, isHighContrast } = useTheme()

  return (
    <button onClick={() => setTheme('high-contrast')}>
      High Contrast Mode
    </button>
  )
}
```

#### CSS Variables (High Contrast)

```css
.high-contrast {
  --background: 0 0% 0%;        /* Pure black */
  --foreground: 0 0% 100%;      /* Pure white */
  --primary: 210 100% 60%;      /* Bright blue */
  --destructive: 0 100% 60%;    /* Bright red */
  /* ... 7:1 contrast ratios throughout */
}
```

---

## Phase 2: Network & Notifications

### Offline Support Indicator

**Files:**
- `src/hooks/useNetworkStatus.ts`
- `src/components/network/OfflineIndicator.tsx`

Detects network connectivity and displays a non-intrusive offline banner.

#### Hook Usage

```tsx
import { useNetworkStatus } from '@/hooks/useNetworkStatus'

function MyComponent() {
  const { isOnline, wasOffline, reconnectedAt } = useNetworkStatus()

  if (!isOnline) {
    return <div>You are offline</div>
  }
}
```

#### Return Values

| Property | Type | Description |
|----------|------|-------------|
| `isOnline` | `boolean` | Current connectivity status |
| `wasOffline` | `boolean` | Was offline at some point |
| `reconnectedAt` | `Date \| null` | Timestamp of reconnection |

#### Component

The `OfflineIndicator` is already integrated in `PortalLayout.tsx`. It:
- Shows amber banner when offline
- Briefly shows "Back online" on reconnection
- Animates in/out with Framer Motion
- Announces status to screen readers

---

### Enhanced Toast Notifications

**File:** `src/lib/toast.ts`

Wraps Sonner toast with additional functionality for undo actions and progress tracking.

#### Basic Usage

```tsx
import { toast } from '@/lib/toast'

// Standard toasts
toast.success('Item created')
toast.error('Failed to save')
toast.info('Processing...')
toast.loading('Uploading...')
```

#### Undo Toast

```tsx
toast.undo('Item deleted', {
  onUndo: () => restoreItem(id),
  duration: 5000, // 5 seconds to undo
})
```

#### Progress Toast

```tsx
const progress = toast.progress({
  title: 'Uploading files',
  description: '0 of 10 files',
})

// Update progress
progress.update(50, '5 of 10 files')

// Complete
progress.success('All files uploaded')

// Or error
progress.error('Upload failed')
```

#### Action Toast

```tsx
toast.withAction('New message received', {
  action: {
    label: 'View',
    onClick: () => navigate('/messages'),
  },
})
```

---

## Phase 3: Forms & Data Entry

### Smart Defaults & Autofill

**Files:**
- `src/lib/utils/sku.ts`
- `src/hooks/useSmartDefaults.ts`

Auto-generates slugs and SKUs from user input.

#### SKU Generator

```tsx
import { generateSKU, generateUniqueSKU, isValidSKU } from '@/lib/utils/sku'

// Generate SKU from product name
const sku = generateSKU('Blue Widget', 'ELC')  // "ELC-BLUE-A1B2"

// Generate unique SKU avoiding duplicates
const uniqueSku = generateUniqueSKU('Blue Widget', existingSKUs, 'ELC')

// Validate SKU format
isValidSKU('ELC-BLUE-A1B2')  // true
```

#### Smart Defaults Hook

```tsx
import { useSmartDefaults } from '@/hooks/useSmartDefaults'

function ProductForm() {
  const form = useForm<ProductFormData>()

  const { duplicates, isChecking, generateSlug, generateSKU } = useSmartDefaults({
    form,
    sourceField: 'name',
    targetFields: {
      slug: 'slug',
      sku: 'sku',
    },
    isEditing: false, // Only auto-generate for new items
    checkDuplicate: async (field, value) => {
      const exists = await api.checkExists(field, value)
      return exists
    },
  })

  // duplicates: { slug: boolean, sku: boolean }
  // isChecking: boolean
}
```

---

### Form Auto-Save

**File:** `src/hooks/useAutoSave.ts`

Automatically saves form drafts to localStorage with restore functionality.

#### Usage

```tsx
import { useAutoSave } from '@/hooks/useAutoSave'

function PostEditor() {
  const form = useForm<PostFormData>()

  const { clearDraft, hasDraft, lastSavedAt } = useAutoSave({
    form,
    storageKey: `post-draft-${postId || 'new'}`,
    debounceMs: 2000,  // Save every 2 seconds
    maxAge: 24 * 60 * 60 * 1000,  // 24 hours
    onRestore: (data) => {
      toast.info('Draft restored', {
        action: {
          label: 'Discard',
          onClick: clearDraft,
        },
      })
    },
  })

  // Clear draft on successful save
  const onSubmit = async (data) => {
    await savePost(data)
    clearDraft()
  }
}
```

#### Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `form` | `UseFormReturn` | required | react-hook-form instance |
| `storageKey` | `string` | required | localStorage key |
| `debounceMs` | `number` | `2000` | Debounce delay |
| `maxAge` | `number` | `86400000` | Max draft age (ms) |
| `onRestore` | `(data) => void` | - | Callback when draft restored |
| `enabled` | `boolean` | `true` | Enable/disable auto-save |

---

### Unsaved Changes Warning

**File:** `src/hooks/useUnsavedChanges.ts`

Blocks navigation when form has unsaved changes.

#### Usage

```tsx
import { useUnsavedChanges } from '@/hooks/useUnsavedChanges'

function EditForm() {
  const form = useForm()

  const { blocker, proceed, cancel } = useUnsavedChanges({
    isDirty: form.formState.isDirty,
    message: 'You have unsaved changes. Are you sure you want to leave?',
  })

  // Show confirmation dialog when blocked
  return (
    <>
      <form>...</form>

      {blocker.state === 'blocked' && (
        <AlertDialog open>
          <AlertDialogContent>
            <AlertDialogTitle>Unsaved changes</AlertDialogTitle>
            <AlertDialogDescription>
              You have unsaved changes. Are you sure you want to leave?
            </AlertDialogDescription>
            <AlertDialogFooter>
              <AlertDialogCancel onClick={cancel}>Stay</AlertDialogCancel>
              <AlertDialogAction onClick={proceed}>Leave</AlertDialogAction>
            </AlertDialogFooter>
          </AlertDialogContent>
        </AlertDialog>
      )}
    </>
  )
}
```

---

### Soft Delete with Undo

**File:** `src/hooks/useSoftDelete.ts`

Implements soft delete pattern with undo toast.

#### Usage

```tsx
import { useSoftDelete } from '@/hooks/useSoftDelete'

function ProductList() {
  const { handleDelete, isPending, pendingItem, cancelDelete } = useSoftDelete({
    onDelete: async (product) => {
      await productsApi.deleteProduct(product.id)
    },
    onRestore: async (product) => {
      await productsApi.restoreProduct(product.id)
    },
    getItemName: (product) => product.name,
    entityType: 'Product',
    undoDuration: 5000,
  })

  return (
    <Button
      onClick={() => handleDelete(product)}
      disabled={isPending}
    >
      Delete
    </Button>
  )
}
```

#### Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `onDelete` | `(item) => Promise<void>` | required | Delete function |
| `onRestore` | `(item) => Promise<void>` | - | Restore function |
| `getItemName` | `(item) => string` | - | Get display name |
| `entityType` | `string` | `'Item'` | Entity type for messages |
| `undoDuration` | `number` | `5000` | Undo window (ms) |

---

## Phase 4: Advanced UX

### Page Transition Animations

**File:** `src/components/layout/AnimatedOutlet.tsx`

Provides smooth page transitions using Framer Motion.

#### Features

- Fade + slide animation (150-200ms)
- Respects `prefers-reduced-motion`
- Uses `location.key` for transition triggers

#### Usage

Already integrated in `PortalLayout.tsx`:

```tsx
import { AnimatedOutlet } from '@/components/layout/AnimatedOutlet'

<Suspense fallback={<PageLoader />}>
  <AnimatedOutlet />
</Suspense>
```

#### Animation Variants

```typescript
// Normal motion
const pageVariants = {
  initial: { opacity: 0, y: 8 },
  enter: { opacity: 1, y: 0, transition: { duration: 0.2 } },
  exit: { opacity: 0, y: -8, transition: { duration: 0.15 } },
}

// Reduced motion
const reducedVariants = {
  initial: { opacity: 0 },
  enter: { opacity: 1, transition: { duration: 0.1 } },
  exit: { opacity: 0, transition: { duration: 0.1 } },
}
```

---

### Virtual Scrolling

**File:** `src/components/ui/virtual-list.tsx`

Efficiently renders large lists using windowing/virtualization.

#### Component Usage

```tsx
import { VirtualList } from '@/components/ui/virtual-list'

function LogViewer({ logs }) {
  return (
    <VirtualList
      items={logs}
      estimateSize={48}  // Each item ~48px tall
      renderItem={(log, index) => (
        <LogEntry key={log.id} log={log} />
      )}
      getItemKey={(log) => log.id}
      height="calc(100vh - 200px)"
      overscan={5}  // Render 5 items outside viewport
    />
  )
}
```

#### Hook Usage (Advanced)

```tsx
import { useVirtualList, getVirtualItemStyle } from '@/components/ui/virtual-list'

function CustomList({ items }) {
  const {
    parentRef,
    virtualItems,
    totalSize,
    scrollToIndex
  } = useVirtualList({
    items,
    estimateSize: 48,
    getItemKey: (item) => item.id,
  })

  return (
    <div ref={parentRef} style={{ height: '400px', overflow: 'auto' }}>
      <div style={{ height: totalSize, position: 'relative' }}>
        {virtualItems.map((virtualItem) => (
          <div key={virtualItem.key} style={getVirtualItemStyle(virtualItem)}>
            {items[virtualItem.index].name}
          </div>
        ))}
      </div>
    </div>
  )
}
```

---

### Command Palette

**Files:**
- `src/components/command-palette/CommandContext.tsx`
- `src/components/command-palette/CommandPalette.tsx`
- `src/hooks/useKeyboardShortcuts.ts`

Global command palette accessible via `Cmd+K` (Mac) or `Ctrl+K` (Windows/Linux).

#### Keyboard Shortcut

| Shortcut | Action |
|----------|--------|
| `Cmd/Ctrl + K` | Open command palette |
| `Escape` | Close palette |
| `Enter` | Execute selected command |
| `Arrow Up/Down` | Navigate commands |

#### Features

- **Navigation**: Quick access to all portal pages
- **Quick Actions**: Theme toggle, create new items
- **Permission-aware**: Only shows items user can access
- **Search**: Fuzzy search across all commands
- **Keyboard hints**: Shows shortcuts in footer

#### Customizing Navigation Items

Edit `CommandPalette.tsx` to add/modify navigation items:

```typescript
const navigationItems: NavigationItem[] = [
  {
    label: 'Dashboard',
    href: '/portal',
    icon: Home,
    permission: 'dashboard.view',
  },
  // Add more items...
]
```

#### Using the Hook

```tsx
import { useCommand } from '@/components/command-palette'

function Header() {
  const { open, toggle } = useCommand()

  return (
    <Button onClick={open}>
      Search... <kbd>Cmd+K</kbd>
    </Button>
  )
}
```

---

## Integration Guide

### Already Integrated

The following are already integrated in the codebase:

| Component | Location | Status |
|-----------|----------|--------|
| AccessibilityProvider | `App.tsx` | Integrated |
| CommandProvider | `App.tsx` | Integrated |
| CommandPalette | `App.tsx` | Integrated |
| SkipLink | `PortalLayout.tsx` | Integrated |
| AnimatedOutlet | `PortalLayout.tsx` | Integrated |
| OfflineIndicator | `PortalLayout.tsx` | Integrated |

### Recommended Future Integration

| Hook | Recommended Pages |
|------|-------------------|
| `useAutoSave` | PostEditorPage, ProductFormPage |
| `useUnsavedChanges` | All form pages |
| `useSmartDefaults` | ProductFormPage, CategoryPages |
| `useSoftDelete` | All delete dialogs |
| `VirtualList` | ActivityTimelinePage, DeveloperLogsPage |

---

## Dependencies

### Required npm Packages

```json
{
  "cmdk": "^1.0.4",
  "@tanstack/react-virtual": "^3.10.9"
}
```

### Existing Dependencies Used

- `framer-motion` - Page transitions, offline indicator animations
- `sonner` - Toast notifications (wrapped by `@/lib/toast`)
- `react-router-dom` - Navigation blocking (`useBlocker`)
- `react-hook-form` - Form integration for auto-save

---

## Changelog

### Version 1.0 (2026-01-28)
- Initial implementation of all 11 UI/UX enhancements
- Full accessibility support (skip links, announcer, high contrast)
- Network awareness with offline indicator
- Enhanced toast with undo and progress support
- Form utilities (auto-save, unsaved changes, smart defaults)
- Advanced UX (page transitions, virtual scrolling, command palette)
