# Storybook Coverage Audit Report

**Date:** 2026-02-27 (Updated from 2026-02-23)
**Storybook Version:** @storybook/react-vite v10.2.8
**Framework:** React + Vite + Tailwind CSS 4
**Build Status:** SUCCESS (0 errors)
**Runtime Health:** 674/674 stories render without errors (verified via Playwright headless)

---

## Executive Summary

| Metric | Previous (2026-02-23) | Current (2026-02-27) | Delta |
|--------|-----------------------|----------------------|-------|
| Total uikit directories | 93 (92 component + 1 utility) | 98 (97 component + 1 utility) | +5 |
| Story files | 91 | **97** | +6 |
| Coverage | 99% | **99%** (1 utility dir) | ~ |

**5 new component directories** added since last audit (code-block, math-block, mermaid-block + 2 others), with 6 new story files.

The uikit library maintains **near-100% Storybook coverage** across 97 component directories with 97 stories.

---

## Architecture Overview

The NOIR frontend uses a **unified `src/uikit/` directory** for all reusable UI components. There is no separate `src/components/ui/` directory. Each uikit entry follows this structure:

```
src/uikit/{component-name}/
├── ComponentName.tsx         ← Component implementation (in 65 directories)
├── ComponentName.stories.tsx ← Storybook stories (in 91 of 92 directories)
└── index.ts                  ← Barrel export (in 65 directories)
```

**Story Types:**

| Type | Count | Description |
|------|-------|-------------|
| **Self-contained** | 59 | Component in uikit, story in same directory |
| **Cross-reference** | 4 | Story imports actual component from `src/components/` |
| **Visual replica** | 19 | Story uses simplified demo (component has complex context deps) |

> **Visual Replica Pattern:** 19 stories use self-contained demo components instead of importing the real implementation. This is intentional — the real components depend on providers (`@dnd-kit`, `react-router`, `AuthContext`, `RegionalSettingsContext`, etc.) not available in Storybook. Each visual replica story is clearly marked with a `// --- Visual Replica ---` comment at the top and documents limitations in Storybook's docs panel. See the [full list below](#visual-replica-stories).

**7 components are NOT exported from `src/uikit/index.ts`** (the `@uikit` barrel):
`command-palette`, `countdown-timer`, `offline-indicator`, `otp-input`, `password-strength-indicator`, `sidebar`, `view-transition-link`

These are app-specific components that live in `src/components/` and are documented in uikit Storybook for visibility.

---

## Full Coverage Matrix

### Self-Contained Components (65 — implementation + story in uikit)

| Component | Story Path | Stories | Default | Variants | Sizes | Disabled | Loading | Notes |
|-----------|------------|---------|---------|----------|-------|----------|---------|-------|
| Alert | `alert/Alert.stories.tsx` | 7 | ✅ | ✅ | — | — | — | |
| AlertDialog | `alert-dialog/AlertDialog.stories.tsx` | 6 | ✅ | ✅ | — | — | ✅ | NOIR destructive pattern, loading action |
| Avatar | `avatar/Avatar.stories.tsx` | 11 | ✅ | — | ✅ | — | — | |
| Badge | `badge/Badge.stories.tsx` | 8 | ✅ | ✅ | — | — | — | All 4 variants, status badges, asLink |
| Breadcrumb | `breadcrumb/Breadcrumb.stories.tsx` | 6 | ✅ | — | — | — | — | |
| Button | `button/Button.stories.tsx` | 15 | ✅ | ✅ | ✅ | ✅ | ✅ | All 6 variants, 4 sizes, loading spinner, asChild |
| Calendar | `calendar/Calendar.stories.tsx` | 5 | ✅ | ✅ | — | ✅ | — | |
| Card | `card/Card.stories.tsx` | 10 | ✅ | — | — | — | ✅ | Hover shadow, clickable, all sub-components |
| CategoryTreeView | `category-tree-view/CategoryTreeView.stories.tsx` | 7 | ✅ | — | — | ✅ | ✅ | |
| Checkbox | `checkbox/Checkbox.stories.tsx` | 7 | ✅ | — | — | ✅ | — | |
| Collapsible | `collapsible/Collapsible.stories.tsx` | 5 | ✅ | — | — | ✅ | — | Controlled, nested, defaultOpen |
| ColorPicker | `color-picker/ColorPicker.stories.tsx` | 5 | ✅ | ✅ | — | — | — | |
| ColorPopover | `color-popover/ColorPopover.stories.tsx` | 6 | ✅ | — | ✅ | — | — | |
| Combobox | `combobox/Combobox.stories.tsx` | 7 | ✅ | — | — | ✅ | — | |
| Credenza | `credenza/Credenza.stories.tsx` | 6 | ✅ | — | — | — | — | |
| DatePicker | `date-picker/DatePicker.stories.tsx` | 8 | ✅ | ✅ | — | ✅ | — | |
| DateRangePicker | `date-range-picker/DateRangePicker.stories.tsx` | 10 | ✅ | ✅ | — | ✅ | — | |
| Dialog | `dialog/Dialog.stories.tsx` | 7 | ✅ | ✅ | — | — | ✅ | Widths, nested, scrollable, destructive |
| DiffViewer | `diff-viewer/DiffViewer.stories.tsx` | 8 | ✅ | ✅ | — | — | — | |
| Drawer | `drawer/Drawer.stories.tsx` | 6 | ✅ | — | — | — | — | Non-dismissible, large content, filter panel |
| DropdownMenu | `dropdown-menu/DropdownMenu.stories.tsx` | 5 | ✅ | ✅ | — | ✅ | — | |
| EmptyState | `empty-state/EmptyState.stories.tsx` | 9 | ✅ | — | ✅ | — | — | Link action, custom illustration, help link |
| FilePreview | `file-preview/FilePreview.stories.tsx` | 16 | ✅ | ✅ | ✅ | — | — | |
| Form | `form/Form.stories.tsx` | 4 | ✅ | — | — | ✅ | ✅ | Validation errors, submitting, prefilled |
| FormField | `form-field/FormField.stories.tsx` | 8 | ✅ | ✅ | — | ✅ | — | FormTextarea, FormError, full form |
| HttpMethodBadge | `http-method-badge/HttpMethodBadge.stories.tsx` | 9 | ✅ | ✅ | — | — | — | |
| ImageLightbox | `image-lightbox/ImageLightbox.stories.tsx` | 8 | ✅ | ✅ | ✅ | — | — | |
| ImageUploadField | `image-upload-field/ImageUploadField.stories.tsx` | 6 | ✅ | ✅ | — | ✅ | — | |
| InlineEditInput | `inline-edit-input/InlineEditInput.stories.tsx` | 7 | ✅ | ✅ | — | ✅ | — | |
| Input | `input/Input.stories.tsx` | 11 | ✅ | ✅ | — | ✅ | ✅ | All types, invalid, with label, loading |
| JsonViewer | `json-viewer/JsonViewer.stories.tsx` | 13 | ✅ | ✅ | — | — | — | |
| Label | `label/Label.stories.tsx` | 6 | ✅ | — | — | ✅ | — | |
| Loading | `loading/Loading.stories.tsx` | 16 | ✅ | ✅ | ✅ | — | ✅ | |
| LogMessageFormatter | `log-message-formatter/LogMessageFormatter.stories.tsx` | 13 | ✅ | ✅ | — | — | — | |
| LogoUploadField | `logo-upload-field/LogoUploadField.stories.tsx` | 6 | ✅ | — | — | ✅ | — | |
| PageHeader | `page-header/PageHeader.stories.tsx` | 7 | ✅ | — | — | — | ✅ | Responsive, multiple actions, gradient text |
| PageLoader | `page-loader/PageLoader.stories.tsx` | 4 | ✅ | — | — | — | ✅ | |
| Pagination | `pagination/Pagination.stories.tsx` | 8 | ✅ | ✅ | — | — | — | |
| Popover | `popover/Popover.stories.tsx` | 4 | ✅ | ✅ | — | — | — | |
| Progress | `progress/Progress.stories.tsx` | 10 | ✅ | — | — | — | — | |
| RadioGroup | `radio-group/RadioGroup.stories.tsx` | 4 | ✅ | — | — | ✅ | — | |
| ResponsiveDataView | `responsive-data-view/ResponsiveDataView.stories.tsx` | 7 | ✅ | ✅ | ✅ | — | ✅ | |
| ScrollArea | `scroll-area/ScrollArea.stories.tsx` | 6 | ✅ | ✅ | — | — | — | Horizontal, both directions, sticky header |
| Select | `select/Select.stories.tsx` | 6 | ✅ | — | — | ✅ | ✅ | Groups, disabled items, loading |
| Separator | `separator/Separator.stories.tsx` | 6 | ✅ | ✅ | — | — | — | |
| Sheet | `sheet/Sheet.stories.tsx` | 4 | ✅ | ✅ | — | — | — | |
| Skeleton | `skeleton/Skeleton.stories.tsx` | 8 | ✅ | ✅ | ✅ | — | ✅ | |
| SkeletonPatterns | `skeleton-patterns/SkeletonPatterns.stories.tsx` | 15 | ✅ | ✅ | ✅ | — | ✅ | |
| Switch | `switch/Switch.stories.tsx` | 7 | ✅ | — | — | ✅ | — | |
| Table | `table/Table.stories.tsx` | 6 | ✅ | — | — | — | ✅ | Empty state, row hover, loading skeleton |
| Tabs | `tabs/Tabs.stories.tsx` | 6 | ✅ | — | — | ✅ | — | Icons, full width, many tabs, disabled tab |
| Textarea | `textarea/Textarea.stories.tsx` | 11 | ✅ | ✅ | — | ✅ | — | |
| ThemeToggle | `theme-toggle/ThemeToggle.stories.tsx` | 5 | ✅ | ✅ | — | — | — | |
| ThumbHashImage | `thumb-hash-image/ThumbHashImage.stories.tsx` | 9 | ✅ | ✅ | ✅ | — | ✅ | |
| TimePicker | `time-picker/TimePicker.stories.tsx` | 7 | ✅ | ✅ | — | ✅ | — | |
| TippyTooltip | `tippy-tooltip/TippyTooltip.stories.tsx` | 12 | ✅ | ✅ | — | — | — | |
| Tooltip | `tooltip/Tooltip.stories.tsx` | 4 | ✅ | ✅ | — | — | — | |
| ViewModeToggle | `view-mode-toggle/ViewModeToggle.stories.tsx` | 5 | ✅ | ✅ | — | — | — | 2-option and 3-option, controlled |
| VirtualList | `virtual-list/VirtualList.stories.tsx` | 5 | ✅ | ✅ | ✅ | — | — | |

### App-Specific Components (7 — story in uikit, implementation elsewhere)

| Component | Story Path | Actual Implementation | Story Type | Stories | Quality |
|-----------|------------|----------------------|------------|---------|---------|
| CommandPalette | `command-palette/CommandPalette.stories.tsx` | `components/command-palette/CommandPalette.tsx` | Visual replica | 4 | Good |
| CountdownTimer | `countdown-timer/CountdownTimer.stories.tsx` | `components/forgot-password/CountdownTimer.tsx` | Direct import | 4 | Excellent |
| OfflineIndicator | `offline-indicator/OfflineIndicator.stories.tsx` | `components/network/OfflineIndicator.tsx` | Visual replica | 4 | Good |
| OtpInput | `otp-input/OtpInput.stories.tsx` | `components/forgot-password/OtpInput.tsx` | Direct import | 7 | Excellent |
| PasswordStrengthIndicator | `password-strength-indicator/PasswordStrengthIndicator.stories.tsx` | `components/forgot-password/PasswordStrengthIndicator.tsx` | Direct import | 7 | Excellent |
| Sidebar | `sidebar/Sidebar.stories.tsx` | `components/portal/Sidebar.tsx` | Visual replica | 4 | Good |
| ViewTransitionLink | `view-transition-link/ViewTransitionLink.stories.tsx` | `components/navigation/ViewTransitionLink.tsx` | Direct import | 5 | Good |

---

## Missing Stories

### uikit Components Missing Stories
**Minimal.** 91 of 92 component directories have story files.

### Portal-App Shared Components Missing Stories

These live in `src/components/` and have NO Storybook coverage despite being important shared components:

#### High Priority (Complex, High Reuse)

| Component | Path | Used In | Complexity | Status |
|-----------|------|---------|------------|--------|
| ~~SortableImageGallery~~ | `components/products/SortableImageGallery.tsx` | Product edit page | High (drag-and-drop) | ✅ RESOLVED — story in src/uikit/ |
| ~~EditableVariantsTable~~ | `components/products/EditableVariantsTable.tsx` | Product edit page | High | ✅ RESOLVED — story in src/uikit/ |
| ProductAttributesSection | `components/products/ProductAttributesSection.tsx` | Product forms | High | Pending |
| ~~BulkVariantEditor~~ | `components/products/BulkVariantEditor.tsx` | Variant generation | High | ✅ RESOLVED — story in src/uikit/ |
| ~~OnboardingChecklist~~ | `components/onboarding/OnboardingChecklist.tsx` | App onboarding | Medium | ✅ RESOLVED — story in src/uikit/ |

#### Medium Priority

| Component | Path | Notes | Status |
|-----------|------|-------|--------|
| SkipLink | `components/accessibility/SkipLink.tsx` | Accessibility component, easy to story | Pending |
| ~~WelcomeModal~~ | `components/onboarding/WelcomeModal.tsx` | First-run experience | ✅ RESOLVED — story in src/uikit/ |
| OrganizationSelection | `components/onboarding/OrganizationSelection.tsx` | Tenant selection flow | Pending |
| AnimatedOutlet | `components/layout/AnimatedOutlet.tsx` | Layout transitions | Pending |

#### Low Priority (SEO/Meta, rarely visible)

- `components/seo/BlogPostMeta.tsx`
- `components/seo/PageMeta.tsx`
- `components/seo/BreadcrumbSchema.tsx`

---

## Outdated Stories

**None detected.** All story files use current component APIs. Key API validations:

| Component | Validation | Status |
|-----------|------------|--------|
| Button | `argTypes` lists all 6 variants, 4 sizes — matches `buttonVariants` in `common/variants.ts` | ✅ |
| Badge | `argTypes` lists all 4 variants — matches `badgeVariants` in `common/variants.ts` | ✅ |
| Card | Uses `CardAction` sub-component (current) | ✅ |
| Dialog | Exports match component: `Dialog`, `DialogTrigger`, `DialogContent`, `DialogClose`, `DialogHeader`, `DialogFooter`, `DialogTitle`, `DialogDescription`, `DialogPortal`, `DialogOverlay` | ✅ |
| AlertDialog | Uses NOIR-standard destructive pattern (`border-destructive/30`, icon container) | ✅ |
| PageHeader | Uses `responsive` prop (added for mobile stacking) | ✅ |
| ViewModeToggle | Uses `ViewModeOption` type, `options`/`value`/`onChange` props | ✅ |
| FormField | Correctly imports from `./FormField` (not via barrel alias `SimpleFormField`) | ✅ |

---

## Story Quality Assessment

### Coverage by State Type

| State | Components Covered | Percentage | Notes |
|-------|-------------------|------------|-------|
| Default state | 91 | **100%** | All stories have a default |
| Variants | 32 | 48% | Only applicable to ~45 components |
| Sizes | 14 | 21% | Only applicable to ~20 components |
| Disabled state | 23 | 35% | Only applicable to ~35 components |
| Loading state | 12 | 18% | Applicable to ~30 components |
| Interactive controls | 20 | 30% | Forms, dropdowns, dialogs |

### Exceptional Quality (10+ Stories or Complex Multi-State Coverage)

| Story | Count | Highlights |
|-------|-------|------------|
| Loading | 16 | All patterns: spinner, skeleton, page, table, card, button |
| FilePreview | 16 | Image, video, audio, PDF, modal, gallery, multiple |
| SkeletonPatterns | 15 | Full pattern library for all page types |
| Button | 15 | All 6 variants × 4 sizes, loading spinner, asChild, icon-only |
| JsonViewer | 13 | All data types, collapsed states, large data |
| LogMessageFormatter | 13 | HTTP, handler, UUID, correlation ID formats |
| OtpInput | 7 | All states: empty, partial, complete, error, disabled, 4/6 digits |
| PasswordStrengthIndicator | 7 | All 4 strength levels + interactive + without requirements |
| FormField | 8 | Password, textarea, full form, error message component |

### Recently Expanded Stories (Updated Since Previous Audit)

These stories were previously at 3 stories ("minimal"). All have been expanded:

| Story | Before | After | New Stories Added |
|-------|--------|-------|-------------------|
| Button | 15 | 15 | Loading states, AsChild, AllVariants, AllSizes |
| Card | 7 | 10 | HoverShadow, Clickable, Loading |
| Collapsible | 3 | 5 | Disabled, Nested |
| Dialog | 3 | 7 | LoadingContent, ScrollableContent, CustomWidth, NestedDialog, DestructiveAction |
| Drawer | 3 | 6 | LargeContent, NonDismissible, NestedContent |
| Form | 3 | 4 | SubmittingState |
| Input | 12 | 11 | Loading state, AllTypes |
| PageHeader | 5 | 7 | WithMultipleActions, LoadingState |
| ScrollArea | 3 | 6 | LongText, SmallContainer, WithStickyHeader, BothDirections |
| Select | 5 | 6 | Loading state |
| Table | 4 | 6 | WithRowHover, LoadingState |
| Tabs | 3 | 6 | DisabledTab, ManyTabs, WithIcons, FullWidth |
| AlertDialog | 3 | 6 | DestructiveStyled, WithLoadingAction, CustomActions |

---

## App Usage Analysis

**127 files** in `portal-app/` and `layouts/` import from `@uikit`.

### Most-Used Components (Top 10)

| Rank | Component | Est. Import Count |
|------|-----------|-------------------|
| 1 | Button | ~100+ files |
| 2 | Badge | ~60 files |
| 3 | Input | ~55 files |
| 4 | Card (+ sub-parts) | ~50 files |
| 5 | PageHeader | ~28 files |
| 6 | Label | ~25 files |
| 7 | Select | ~25 files |
| 8 | AlertDialog | ~24 files |
| 9 | EmptyState | ~23 files |
| 10 | Table | ~22 files |

**All top 10 components have comprehensive Storybook stories.** ✅

---

## Visual Replica Stories

19 stories use self-contained demo components. **Drag-and-drop, routing, auth context, and i18n are non-functional in these stories.** All other interactions (buttons, forms, state changes) work.

| Story | Real Component Location | Missing Provider(s) | What Does NOT Work |
|-------|------------------------|---------------------|--------------------|
| AnimatedOutlet | `components/layout/AnimatedOutlet.tsx` | React Router, framer-motion | Page transitions |
| BulkVariantEditor | `components/products/BulkVariantEditor.tsx` | react-i18next, API hooks | i18n labels, API calls |
| CommandPalette | `components/command-palette/CommandPalette.tsx` | React Router, AuthContext | Navigation, auth checks |
| EditableVariantRow | `components/products/EditableVariantRow.tsx` | react-i18next | i18n labels |
| EditableVariantsTable | `components/products/EditableVariantsTable.tsx` | react-i18next, API hooks | i18n labels, API calls |
| FeatureGuard | `components/features/FeatureGuard.tsx` | FeatureContext, react-i18next | Feature flag checks |
| ImageUploadZone | `components/products/ImageUploadZone.tsx` | react-i18next, file upload API | File upload, i18n |
| OnboardingChecklist | `components/onboarding/OnboardingChecklist.tsx` | React Router, localStorage | Navigation, persistence |
| OrganizationSelection | `components/onboarding/OrganizationSelection.tsx` | AuthContext, react-i18next | Auth, tenant selection |
| PermissionGate | `components/auth/PermissionGate.tsx` | AuthContext (usePermissions) | Permission checks |
| ProductAttributesSection | `components/products/ProductAttributesSection.tsx` | react-i18next, API hooks | i18n labels, API calls |
| ProductAttributesSectionCreate | `components/products/ProductAttributesSectionCreate.tsx` | react-i18next, API hooks | i18n labels, API calls |
| ProductOptionsManager | `components/products/ProductOptionsManager.tsx` | react-i18next | i18n labels |
| ProtectedRoute | `components/auth/ProtectedRoute.tsx` | AuthContext, React Router | Auth, routing |
| Sidebar | `components/portal/Sidebar.tsx` | React Router, AuthContext | Navigation, auth |
| SkipLink | `components/accessibility/SkipLink.tsx` | react-i18next | i18n labels |
| **SortableImageGallery** | `components/products/SortableImageGallery.tsx` | **@dnd-kit/core, @dnd-kit/sortable** | **Drag-and-drop reordering** |
| VariantGenerator | `components/products/VariantGenerator.tsx` | react-i18next, API hooks | i18n labels, API calls |
| VariantMatrixView | `components/products/VariantMatrixView.tsx` | react-i18next | i18n labels |
| VariantOptionsSelector | `components/products/VariantOptionsSelector.tsx` | react-i18next | i18n labels |
| WelcomeModal | `components/onboarding/WelcomeModal.tsx` | AuthContext, framer-motion | Auth, animations |

> **Convention:** Every visual replica story MUST have:
> 1. `// --- Visual Replica ---` comment at the top of the file
> 2. A `parameters.docs.description.component` or `story` explaining what works and what doesn't
> 3. The real component path documented in the description

---

## Runtime Health Check (2026-02-27)

Automated Playwright headless verification of all 674 stories across 97 components.

| Result | Count | Details |
|--------|-------|---------|
| OK | **674** | Story renders without errors or blank page |
| ERROR | 0 | — |
| BLANK | 0 | — |

**Issues fixed in this audit:**

| Story | Issue | Fix |
|-------|-------|-----|
| TrackingTimeline (8 stories) | `useRegionalSettings` throws outside `RegionalSettingsProvider` | Switched to `useRegionalSettingsOptional` with fallback formatter |
| OnboardingChecklist AllCompleted | React reused `useState` initial value across stories | Added `key` prop in render function to force remount |
| ImageLightbox HidePlaceholder | Intentionally returns `null` — appeared blank | Wrapped in dashed border container with explanatory label |
| ThumbHashImage BrokenImage | External invalid URL caused timeout | Changed to local `/non-existent-image-404.jpg` |

---

## Issues and Recommendations

### Issue 1: Dialog ScrollableContent Anti-Pattern

**File:** `dialog/Dialog.stories.tsx` — `ScrollableContent` story

```tsx
// The story demonstrates this pattern:
<DialogContent className="max-h-[80vh] flex flex-col">
```

Per CLAUDE.md: *"Never wrap form inputs in any overflow container"* — flex containers on `DialogContent` clip focus rings. The story demonstrates this for the "scrollable terms" use case (no form inputs), which is valid, but should include a comment warning developers not to use this pattern when the dialog contains form inputs.

**Recommendation:** Add a JSDoc comment to the story noting the focus-ring clipping caveat.

### Issue 2: 19 Visual Replica Stories Cannot Catch API Regressions

**Stories affected:** See [Visual Replica Stories](#visual-replica-stories) table above (19 components).

These stories use simplified demo components rather than importing the actual implementation. If the real components change their API, the stories won't fail. The most impactful limitation is **SortableImageGallery** where drag-and-drop is completely non-functional.

**Recommendation:** Adding shared Storybook decorators for `react-i18next`, `RegionalSettingsContext`, and `React Router` would allow many of these to use the real component. `@dnd-kit` and `AuthContext` would still require visual replicas. Each visual replica clearly documents its limitations in both code comments and Storybook docs panel.

### Issue 3: 7 Components Not in @uikit Barrel

The `@uikit` barrel (`src/uikit/index.ts`) does not export: `command-palette`, `countdown-timer`, `offline-indicator`, `otp-input`, `password-strength-indicator`, `sidebar`, `view-transition-link`.

Developers importing from `@uikit` won't discover these stories through IDE autocomplete.

**Recommendation:** Add a Storybook introduction story or Storybook docs page listing all non-barrel components. Consider whether `OtpInput`, `CountdownTimer`, `PasswordStrengthIndicator` could be generalized and added to the barrel.

### Issue 4: No Stories for Complex Product Editor Components

`src/components/products/` contains 10+ complex UI components (variant matrix, attribute inputs, sortable gallery) with no Storybook coverage. These are the most complex UI in the app.

**Recommendation:** Priority story additions:
1. `SortableImageGallery` — visual drag-and-drop component
2. `EditableVariantsTable` — complex multi-field editor
3. `ProductAttributesSection` — dynamic attribute forms

---

## Storybook Configuration

```bash
# Run Storybook
cd src/NOIR.Web/frontend && pnpm storybook
# → http://localhost:6006

# Build Storybook
pnpm build-storybook
```

| Config File | Purpose |
|-------------|---------|
| `.storybook/main.ts` | Framework: `@storybook/react-vite`, story glob pattern |
| `.storybook/preview.ts` | Global styles, centered layout, theme |
| `tsconfig.app.json` | Excludes `*.stories.*` from production build |

**Path alias:** `@uikit` → `src/uikit/`

---

## Comparison with Previous Audit (2026-02-19)

### What Improved

- ✅ +7 new story entries for app-specific components (sidebar, command-palette, otp-input, countdown-timer, offline-indicator, password-strength-indicator, view-transition-link)
- ✅ Dialog: 3 → 7 stories (loading, scrollable, custom width, nested, destructive)
- ✅ Drawer: 3 → 6 stories (large content, non-dismissible, filter panel)
- ✅ Collapsible: 3 → 5 stories (disabled, nested)
- ✅ Tabs: 3 → 6 stories (icons, full width, many tabs, disabled)
- ✅ Table: 4 → 6 stories (row hover, loading skeleton)
- ✅ AlertDialog: 3 → 6 stories (NOIR pattern, loading action, custom actions)
- ✅ Card: 7 → 10 stories (hover shadow, clickable, loading)
- ✅ ScrollArea: 3 → 6 stories (horizontal, sticky header, both directions)
- ✅ Select: 5 → 6 stories (loading state)
- ✅ Form: 3 → 4 stories (submitting state)
- ✅ Input: 12 → 11 stories (refined, loading state added)

### Previous Recommendations — Status

| Recommendation | Status |
|----------------|--------|
| Add loading states to high-usage components | ✅ Done (Form, Input, Table, Select, Card) |
| Expand minimal stories (Dialog, Drawer, Tabs, etc.) | ✅ Done |
| Add disabled states | ✅ Done (Collapsible) |
| Create shared Storybook decorators for providers | ⏳ Not yet done |
| Add stories for portal-specific components | ⏳ Not yet done |

---

## Summary

The NOIR UIKit Storybook has **near-100% coverage across 92 component directories** (91 stories). Story quality is consistently high with most critical components having comprehensive multi-state coverage.

**Strengths:**
- Complete directory-level coverage maintained as new components are added
- Recently expanded stories for Dialog, Drawer, Tabs, AlertDialog address previous gaps
- New app-specific component stories (OtpInput, Sidebar, CommandPalette, etc.) improve documentation
- NOIR-specific design patterns (destructive alert dialog, hover shadows) are demonstrated in stories
- Loading and skeleton states well-covered across the board

**Remaining Gaps:**
1. 7 components not in `@uikit` barrel may be undiscoverable to new developers
2. No shared Storybook decorators for i18n/router providers (19 visual replicas could be reduced with global decorators)
3. Dialog `ScrollableContent` story should warn about focus-ring clipping anti-pattern
4. SortableImageGallery drag-and-drop is non-functional in Storybook (visual replica — needs @dnd-kit providers)

*Report updated 2026-02-23 (originally generated by storybook-auditor on 2026-02-20)*
