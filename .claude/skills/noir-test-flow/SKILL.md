---
name: noir-test-flow
description: Visual + behavioral testing protocol for NOIR — triple-mode checks (light/dark/VI/responsive), interactive element testing, data consistency verification, and bug fix workflow. Used by /noir-qa orchestrator.
---

# NOIR Test Flow — Visual + Behavioral Testing Protocol

> **Purpose**: Defines HOW to test each page/feature. Called by `/noir-qa` and `/noir-qa-run` or used standalone.
> **Standalone usage**: `/noir-test-flow` — applies the full protocol to a specific page or set of pages.
> **As sub-protocol**: Referenced by `/noir-qa` Phase 4 (Execution) and `/noir-qa-run` Execute phase.

---

## CORE PRINCIPLE

**"If a human QA engineer would catch it, you must catch it."**

Dark mode, Vietnamese, and responsive are NOT separate phases — they are tested on EVERY visual state (page, tab, dialog, popup). This catches the bugs that single-mode testing misses.

---

## TRIPLE-MODE VISUAL AUDIT (Page Level)

For each page's initial view, execute 4 modes:

### Light Mode (Baseline)

```
1. browser_navigate → page URL (or SPA sidebar click — see Technical Notes)
2. browser_wait_for → network idle / specific element visible
3. browser_take_screenshot → temp/qa-{page}-light.png
4. READ screenshot and analyze:
   - Layout: alignment, spacing, overflow, content clipping
   - Components: broken images, missing icons, wrong empty states
   - Typography: font consistency, text truncation, ellipsis working
   - Colors: theme consistency, contrast, hover states visible
   - Loading: skeleton → data (no flash of empty)
   - Card shadows: shadow-sm + hover:shadow-lg per design standard
```

### Dark Mode

```
5. browser_evaluate → document.documentElement.classList.add('dark')
6. browser_take_screenshot → temp/qa-{page}-dark.png
7. READ screenshot and verify:
   - Text readable (no white-on-white, no black-on-black)
   - Borders visible (not disappearing into background)
   - Status badges maintain contrast
   - Charts/graphs readable
   - No hardcoded colors overriding theme variables
   - Input fields: background distinguishable from page background
   - Dropdown menus / popovers: correct dark background (not white)
```

### Vietnamese

```
8. browser_evaluate → localStorage.setItem('i18nextLng','vi'); location.reload()
9. browser_wait_for → content loaded
10. browser_take_screenshot → temp/qa-{page}-vi.png
11. READ screenshot and verify:
    - No raw i18n keys (e.g., "products.searchPlaceholder" as literal text)
    - No English text remaining (except allowed: CRM, API, SMTP, Blog)
    - No text overflow from longer Vietnamese strings (buttons, headers, labels)
    - Date format matches Vietnamese locale (DD/MM/YYYY)
    - Sidebar labels are pure Vietnamese (no "Blog Posts", must be "Bài viết")
    - Vietnamese + dark mode: switch to dark briefly → screenshot → verify no issues
    - Switch back to light mode after
```

### Responsive

```
12. Restore to light mode + English before responsive checks
13. For each viewport [1440, 1024, 768]:
    - browser_resize → {width}px
    - browser_take_screenshot → temp/qa-{page}-{width}px.png
    - READ and verify:
      - 1440: full layout, sidebar visible, all columns shown
      - 1024: sidebar may collapse, table columns may hide, no overflow
      - 768: mobile/tablet layout, content stacks vertically, no horizontal scroll on body
    - Specific: tables horizontal scroll within container, buttons not cut off,
      cards stack vertically, PageHeader wraps correctly, search remains usable
14. browser_resize → restore 1440px
```

---

## MINI TRIPLE CHECK (Element Level)

For EACH interactive element type on the page. Every distinct visual state gets this protocol:

1. Screenshot in light mode (baseline)
2. Switch to dark mode → screenshot → verify: text readable, borders visible, inputs distinguishable
3. Switch to Vietnamese → screenshot → verify: all text translated, no overflow, no raw i18n keys
4. Resize to 768px → screenshot → verify: layout adapts, content accessible, no overflow
5. Restore light mode + English + 1440px before proceeding

### Required Visual States (100% Matrix)

| Visual State | Light | Dark | VI | 768px | Notes |
|---|---|---|---|---|---|
| Each **TAB** content | ✅ | ✅ | ✅ | ✅ | Different content layout per tab |
| **Create dialog** | ✅ | ✅ | ✅ | ✅ | Form fields, buttons, footer |
| **Edit dialog** | ✅ | ✅ | ✅ | ✅ | May differ from create (pre-populated) |
| **Detail/View dialog** | ✅ | ✅ | ✅ | ✅ | Read-only content, labels |
| **Confirmation dialog** | ✅ | ✅ | ✅ | ✅ | Destructive button styling, text |
| **Filter popover** | ✅ | ✅ | ✅ | ✅ | Dropdown options, filter labels |
| **Empty state** | ✅ | ✅ | ✅ | ✅ | Icon, title, description text |
| **Error banner** (FormErrorBanner) | ✅ | ✅ | ✅ | ✅ | Banner background, error text |
| **Validation errors** on form | ✅ | ✅ | ✅ | ✅ | Error text color vs input bg |
| **Toast/notification** | ✅ | ✅ | ✅ | ✅ | Background contrast, text, position |
| **Date picker popup** | ✅ | ✅ | ✅ | ✅ | Month/day names, calendar grid bg |
| **Dropdown menu** (actions `⋮`) | ✅ | ✅ | ✅ | ✅ | Menu item text, background, hover |
| **Select/Combobox dropdown** | ✅ | ✅ | ✅ | ✅ | Option labels, search, selected state |
| **Color picker popup** | ✅ | ✅ | ✅ | ✅ | Color swatch visibility in dark |
| **Loading/skeleton state** | ✅ | ✅ | — | ✅ | Skeleton colors in dark (no text) |
| **Bulk action toolbar** | ✅ | ✅ | ✅ | ✅ | Selection count, action buttons |
| **Tooltip** (on icon buttons) | ✅ | ✅ | ✅ | — | Content, background (cursor-attached) |

**Efficiency**: Shared components (DataTablePagination, etc.) need full Mini Triple Check only on FIRST occurrence. Subsequent pages: quick dark mode spot-check. Page-specific dialogs/tabs: always full check.

---

## INTERACTIVE ELEMENT TESTING

### Tables (DataTable)

- Column headers render correctly, all have labels
- Sort: click each sortable column header, verify order + indicator
- Pagination: page 2, change page size, verify "Showing X of Y" updates
- Search: type query → verify filtering → clear → verify reset
- Column visibility: open dropdown, hide column, verify gone, show back
- Column reorder: if supported, drag column, verify new order
- Density toggle: switch compact/normal/comfortable if available
- Empty state: search nonsense → verify `<EmptyState>` component (not plain text)
  → **Mini Triple Check** on empty state
- Row selection: check/uncheck rows, verify bulk action toolbar appears
- Actions dropdown: click `⋮` → verify menu items → verify each action works
- Group by: if supported, group → verify group headers with correct count

### Forms (Create/Edit Dialogs)

- Open Create dialog → screenshot → **Mini Triple Check**
- **Field-by-field testing** for EVERY field:
  - Focus → verify focus ring/border
  - Tab to next → verify tab order (top-to-bottom, left-to-right)
  - Required fields → verify red asterisk `*` on label
  - Type invalid → blur → verify error (NOT while typing)
  - Type valid → verify error clears
  - Dropdowns: open → select → verify selection
  - Date pickers: open → select → verify format. **Dark mode**: calendar popup screenshot
  - Rich text editors: type → verify toolbar works
  - File uploads: upload → verify preview
  - Color pickers: select → verify preview
- Submit empty → verify ALL required field errors at once
- Submit invalid → verify specific field errors (not generic toast)
- Submit valid → verify: toast + dialog closes + list updates + data matches
- Cancel → closes, no data changed
- Click outside → closes (Credenza behavior)
- ESC → closes
- **Edit mode**: open existing → verify fields pre-populated → **Mini Triple Check**

### Filters

- Apply each filter individually → verify results update
- Apply combination → verify AND logic
- Verify active filter indicator/badge shows count
- Clear individual → verify results update
- Clear all → verify full reset
- Filter persists across pagination

### Destructive Actions

- Click delete/archive → verify confirmation dialog appears (NEVER instant delete)
- **Mini Triple Check** on confirmation dialog
- Cancel → nothing changed
- Confirm → item removed/updated + toast + related data updated + activity timeline

### Tabs

- **For EACH tab:**
  1. Click tab → verify content loads
  2. Verify URL updates with `?tab=xxx`
  3. **Mini Triple Check** on tab content
  4. Direct-navigate to `?tab=xxx` → verify correct tab active + content loaded
  5. Verify no flash/flicker on tab switch

### Messages & Notifications

- **Toasts**: trigger success action → verify position, auto-dismiss, text → **Mini Triple Check**
- **Error banners**: trigger server error → verify shows, dismiss works, clears on retry → **Mini Triple Check**
- **Validation messages**: submit invalid → verify under field → **Mini Triple Check**
- **Date picker popup**: open → verify calendar → **Mini Triple Check**
- **Select/Combobox**: open → verify options → **Mini Triple Check**
- **Tooltip**: hover icon button → verify shows → Dark + VI check (no resize needed)

### Navigation

- Breadcrumbs: correct path, each segment clickable
- Sidebar: current page highlighted
- Back navigation: browser back works correctly
- Deep link: navigate to URL directly → verify same state loads

---

## DATA CONSISTENCY CHECKS

After EACH CRUD operation, verify:

### Create
- Item appears in list with correct data
- List total count incremented
- Dashboard widgets updated (if applicable)
- Activity timeline shows "Created ..." entry
- Related entities reflect change (e.g., department employee count)
- Search returns the new item

### Edit
- ALL changed fields persisted (re-open edit dialog to verify)
- List row updated with new values
- Detail page (if exists) shows updated data
- Activity timeline shows "Updated ..." entry
- Related views reflect change

### Delete (Soft)
- Item removed from active list
- List total count decremented
- Related entities updated (counts, references)
- Activity timeline shows "Deleted ..." entry
- Dashboard widgets updated

---

## CROSS-FEATURE FLOW TEMPLATES

Use these as templates when creating flow definitions in `.qa/flows/`.

### Flow: Product → Order → Payment Lifecycle
```
Create Product → Add Variants → Set Inventory (StockIn, confirm)
→ Dashboard product count increased
→ Create Customer → Create Order with product variant
→ Confirm → inventory decreased → Process → Ship → Deliver → Complete
→ Payment in Payments list with correct amount
→ Revenue in Dashboard
→ Customer writes Review → Approve → review count on product detail
```

### Flow: Customer Journey
```
Create Customer → Add to Group → group count updated
→ Create Order → Customer Detail → order in history tab
→ Cancel order → status updates in: Order detail, Customer history, Dashboard
→ Activity Timeline shows all actions with correct descriptions
→ Vietnamese → activity descriptions translated
```

### Flow: Content Publishing
```
Create Blog Category → Create Blog Tags (2+)
→ Create Post with image → Assign category + tags → Draft
→ Media Library shows image
→ Edit → Publish → status badge changes
→ Delete tag → post's tag list updates (no crash)
→ Edit category name → post shows updated category
```

### Flow: HR Workflow
```
Create Department (child) → tree shows it
→ Create Employee in department → Add Tags
→ Department detail: employee count = 1
→ Edit employee → changes persist
→ Export CSV → CSV contains employee
→ Import modified CSV → change applied
→ Move to different department → both counts update
```

### Flow: CRM Pipeline
```
Create Company → Create Contact linked → company shows contact count
→ Create Lead → Kanban shows in first stage
→ Drag through stages → Win → dashboard metrics update
→ Activity on contact → timeline shows
→ Delete company → contacts still accessible (soft delete)
```

### Flow: PM Workflow
```
Create Project → auto-code PRJ-xxx
→ Add columns → Create tasks in different columns
→ Drag task → status changes in detail
→ Subtask → complete → parent progress
→ Comment → timestamp correct
→ Attachment → download works
→ Archive → gone from Kanban, in Archived view
```

### Flow: Data Integrity & Edge Cases
```
Delete Brand with products → products still display
Disable Module (CRM) → sidebar gone, URL → 404, dashboard widgets hidden, re-enable → returns
Test user with custom role → remove permission → verify sidebar/URL/actions restricted
Language switch EN→VI→EN → all labels correct, no raw keys, date formats change
Theme switch light→dark→light → no artifacts
```

### Flow: Error Handling
```
Navigate to /portal/entity/00000000-...0 → 404 or graceful redirect
Stop backend → Submit form → error banner (not crash) → Restart backend
Delete entity via API while edit dialog open → Save → conflict handling
Rapid pagination clicks → final page correct (no stale data)
Rapid double-click Submit → only ONE entity created
```

---

## TEST CASE PRIORITY LEVELS

| Priority | Meaning | When to run | Typical bugs found |
|----------|---------|-------------|-------------------|
| **P0** | Critical path — app unusable if broken | Every commit (smoke) | CRITICAL, HIGH |
| **P1** | Core functionality — feature broken | Every PR (regression) | HIGH, MEDIUM |
| **P2** | Edge cases — non-obvious scenarios | Weekly / pre-release | MEDIUM, LOW |
| **P3** | Cosmetic — nice-to-have | Monthly / manual | LOW |

## BUG SEVERITY CLASSIFICATION

| Severity | Definition | Examples |
|----------|-----------|---------|
| **CRITICAL** | Feature broken, data loss, security | Form submits but data not saved, crash, security bypass |
| **HIGH** | Feature partially broken, bad UX | Wrong data displayed, dialog won't close, pagination broken |
| **MEDIUM** | Visual bug, minor UX | Misalignment, wrong icon, dark mode contrast issue |
| **LOW** | Cosmetic, nice-to-have | Hover inconsistent, spacing off by a few px |

**Priority vs Severity**: Priority is the test's urgency (when to run). Severity is the bug's impact (how bad). A P2 edge-case test can find a CRITICAL bug — that's the point of edge-case testing.

---

## BUG FIX WORKFLOW

When you find a bug:

1. **Log it** — add to results with severity + description + screenshot
2. **Analyze root cause** — read relevant source code
3. **Fix it** — minimal code change
4. **Restart if needed**:
   - Frontend CSS/TSX/JSON → HMR, wait 2s
   - Backend handler/service → `dotnet watch` auto-reloads, wait 3-5s
   - DI/Program.cs → restart backend
   - vite.config → restart frontend
5. **Build check** (only if logic change):
   - Frontend: `cd src/NOIR.Web/frontend && pnpm run build`
   - Backend: `dotnet build src/NOIR.sln`
6. **Health check**:
   ```bash
   curl -sf http://localhost:4000/robots.txt -o /dev/null && echo "Backend: OK"
   curl -sf http://localhost:3000 -o /dev/null && echo "Frontend: OK"
   ```
7. **Re-verify** — exact same test that found the bug
8. **Screenshot** fixed state → `temp/qa-{page}-fixed-{BUG-ID}.png`
9. **Update** result status to `FIXED+VERIFIED`
10. **Regression check** — if shared component changed, note affected pages

### Bug Decision Tree

```
CSS/layout bug? → Fix Tailwind classes → No restart (HMR)
Missing translation? → Add to BOTH en/*.json AND vi/*.json → No restart (HMR)
Dark mode bug? → Fix hardcoded color or missing dark: variant → No restart
Data display bug? → Trace component → hook → handler → spec → Fix at correct layer → Watch reload
Form validation? → Check Zod + FluentValidation → Fix both if needed → Watch reload
State management? → Check TanStack Query invalidation → No restart
Navigation/routing? → Check router, useUrlTab/useUrlDialog → No restart
DI/startup? → Fix Program.cs/DI → Restart backend
Field-level UX? → Fix FormField/FormItem props → No restart
```

### FIX-THEN-COMMIT Strategy

After fixing ALL bugs on a single page (not per-bug), create ONE commit:
```bash
git add [specific files only]
git commit -m "$(cat <<'EOF'
fix(qa): [page-name] — [N] bugs fixed

- BUG-001: [brief description]
- BUG-002: [brief description]

QA: NOIR QA Agent

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## TECHNICAL NOTES

### Playwright MCP Tools

Use `mcp__playwright__*` for ALL browser interactions:
- `browser_navigate` — go to URLs (initial load only — see SPA bug below)
- `browser_click` — click elements (use accessible name or CSS selector)
- `browser_fill_form` — fill multiple form fields at once
- `browser_take_screenshot` — capture to temp/
- `browser_snapshot` — accessibility tree (for finding elements)
- `browser_select_option` — `<select>` dropdowns
- `browser_press_key` — keyboard (Escape, Enter, Tab)
- `browser_wait_for` — network idle or element visible
- `browser_hover` — hover states
- `browser_drag` — drag-and-drop (Kanban, tree reorder)
- `browser_type` — type character by character
- `browser_resize` — responsive breakpoints

**Strategy**: `browser_snapshot` first to understand page structure, then use accessible names for clicking/filling. More robust than CSS selectors.

### CRITICAL: MCP Direct — No Scripts

**NEVER** write Playwright test scripts (`.js`/`.ts` files). Always use `mcp__playwright__*` tools directly with AI reasoning (max effort, Opus model). The AI reads screenshots, analyzes accessibility trees, and makes intelligent decisions about what to click/verify — this catches visual bugs that scripted tests miss.

```
❌ WRONG: Write a .js file → run it with npx playwright test
✅ RIGHT: mcp__playwright__browser_navigate → mcp__playwright__browser_snapshot → AI analyzes → mcp__playwright__browser_click
```

The entire value of this QA system is AI visual reasoning + interactive exploration, not scripted automation.

### Windows SPA Navigation Bug (CRITICAL)

On Windows, `browser_navigate` / `page.goto()` often produces **blank white pages** for React SPA routes. HTML shell loads but React doesn't mount.

**Workaround**: Navigate via SPA sidebar clicks instead of direct URL. Use `browser_navigate` ONLY for initial page load (`/portal`), then sidebar clicks for all subsequent pages. For detail pages without sidebar links, click table rows from parent list.

### Context Efficiency

- Save `browser_snapshot` output to files (`filename` param) instead of inline
- NEVER dump full sidebar accessibility tree — 100+ lines of identical content
- Only read `<main>` content area
- For DataTable pattern pages, screenshot is sufficient — skip tree dump

### Login Flow

```
1. browser_navigate → http://localhost:3000/login
2. browser_snapshot → find email/password fields
3. browser_fill_form → email: admin@noir.local, password: 123qwe
4. browser_click → Sign In button
5. browser_wait_for → dashboard loaded
6. For Platform Admin tests: logout, login as platform@noir.local / 123qwe
```

### Screenshot Naming

```
temp/qa-{page}-{variant}.png

Page-level:  -light, -dark, -vi, -1024px, -768px
Element-level: -tab-{name}-{mode}, -create-dialog-{mode}, -edit-dialog-{mode}
               -confirm-dialog-{mode}, -empty-state-{mode}
Action:      -create-error, -create-valid, -fixed-{BUG-ID}
```

---

## SERVICE LIFECYCLE

### Restart Protocol

| Change Type | Restart? | Action |
|---|---|---|
| Frontend CSS/TSX/i18n JSON | NO | Vite HMR (wait 2s) |
| Backend handler/spec/service | NO if `dotnet watch` | Watch reloads (wait 3-5s) |
| Backend DI / Program.cs | YES — backend | Kill :4000, rebuild, restart |
| vite.config / tailwind.config | YES — frontend | Kill :3000, restart pnpm dev |

### Restart Commands

```bash
# Restart backend
netstat -ano | grep ":4000 " | grep "LISTEN" | awk '{print $5}' | sort -u | while read pid; do taskkill //F //PID "$pid" 2>/dev/null; done
sleep 2
cd src/NOIR.Web && dotnet build --nologo -v q -c Debug > /dev/null 2>&1
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://localhost:4000" dotnet watch run --no-launch-profile > ../../.backend.log 2>&1 &

# Restart frontend
netstat -ano | grep ":3000 " | grep "LISTEN" | awk '{print $5}' | sort -u | while read pid; do taskkill //F //PID "$pid" 2>/dev/null; done
sleep 1
powershell -Command "Start-Process cmd -ArgumentList '/c cd /d src\NOIR.Web\frontend && pnpm run dev > ..\..\..\.frontend.log 2>&1'"

# Health check
curl -sf http://localhost:4000/robots.txt -o /dev/null && echo "Backend: OK" || echo "Backend: FAILED"
curl -sf http://localhost:3000 -o /dev/null && echo "Frontend: OK" || echo "Frontend: FAILED"
```

### Crash Recovery

1. Check which service is down: `tail -20 .backend.log` or `.frontend.log`
2. If crash caused by your fix → revert fix first
3. Restart crashed service
4. Wait for health check
5. Resume testing
