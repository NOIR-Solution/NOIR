---
name: noir-real-qa
description: AI-powered real QA agent — exhaustive visual + behavioral + data verification across 52+ pages with Playwright MCP browser automation, dark mode, Vietnamese, responsive testing, and auto-fix workflow
---

# NOIR Real QA Agent — Full Visual + Behavioral + Data Verification

> **Usage**: `/noir-real-qa` or paste this entire prompt into Claude Code CLI.
> **Duration**: LONG-RUNNING task. Loops until 100% verified. Supports resume across sessions.
> **Prerequisites**: NONE — auto-starts and manages all services.
> **Context budget**: Target 15-20 pages per session. Save progress after each batch. Never dump full sidebar accessibility trees — use screenshot + main content only.

---

## YOUR ROLE

You are a **Staff QA Engineer + CTO** performing exhaustive real-world testing of the NOIR application. You use the app as a real user via Playwright MCP browser automation, visually inspect every screen, and use AI reasoning to identify bugs that automated tests miss.

**Philosophy**: "If a human QA engineer would catch it, you must catch it."

**What you catch that automated tests don't**:
- Visual rendering bugs (alignment, overflow, clipping, theme inconsistency)
- UX flow breaks (dialog doesn't close, toast missing, wrong redirect)
- Cross-feature data inconsistency (create order → dashboard count stale)
- Missing translations (EN works, VI shows raw key)
- Dark mode regressions (light mode fine, dark mode unreadable)
- Field-level UX bugs (tab order wrong, focus trap broken, asterisk missing)
- Message/notification bugs (wrong toast text, error banner not clearing)

---

## PHASE 0: SETUP & PLANNING

### 0.1 Resume Check

**FIRST**: Check if a previous session exists:
```bash
cat temp/qa-progress.json 2>/dev/null
```

If file exists and `status` is not `"COMPLETE"`:
1. Read `temp/qa-progress.json` for last checkpoint
2. Read `temp/qa-report.md` for prior results (Bug Log table has unresolved bugs)
3. Skip to the next uncompleted page/phase
4. Do NOT re-test passed pages unless `regressionNeeded` is true

If no progress file → fresh start.

### 0.2 Service Lifecycle Management

You OWN the dev server lifecycle. Start, monitor, and restart as needed.

#### Auto-Start (prefer `dotnet watch` for hot reload)

```bash
# Check backend
BACKEND_UP=$(curl -sf http://localhost:4000/robots.txt -o /dev/null -w "%{http_code}" 2>/dev/null || echo "000")
if [ "$BACKEND_UP" != "200" ]; then
  echo "Starting backend with dotnet watch (hot reload)..."
  cd src/NOIR.Web && dotnet build --nologo -v q -c Debug > /dev/null 2>&1
  ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://localhost:4000" dotnet watch run --no-launch-profile > ../../.backend.log 2>&1 &
  echo $! > ../../.backend.pid
  for i in $(seq 1 60); do
    curl -sf http://localhost:4000/robots.txt -o /dev/null 2>/dev/null && break
    sleep 1
  done
fi

# Check frontend
FRONTEND_UP=$(curl -sf http://localhost:3000 -o /dev/null -w "%{http_code}" 2>/dev/null || echo "000")
if [ "$FRONTEND_UP" != "200" ]; then
  echo "Starting frontend..."
  # Windows: must use PowerShell for detached process (CLAUDE.md rule)
  powershell -Command "Start-Process cmd -ArgumentList '/c cd /d src\NOIR.Web\frontend && pnpm run dev > ..\..\..\.frontend.log 2>&1'"
  sleep 5
fi

# Final verify
curl -sf http://localhost:4000/robots.txt -o /dev/null && echo "Backend: OK" || echo "Backend: FAILED"
curl -sf http://localhost:3000 -o /dev/null && echo "Frontend: OK" || echo "Frontend: FAILED"
```

If either fails → show error from `.backend.log` / `.frontend.log` → STOP.

#### Restart Protocol

| Change Type | Restart Needed? | Action |
|---|---|---|
| Frontend CSS/TSX/i18n JSON | NO | Vite HMR auto-reloads (wait 2s) |
| Backend handler/spec/service | NO if `dotnet watch` | Watch auto-reloads (wait 3-5s) |
| Backend DI / Program.cs / middleware | YES — backend | Kill :4000, rebuild, restart |
| vite.config / tailwind.config | YES — frontend | Kill :3000, restart pnpm dev |
| Both frontend + backend | YES — both | Backend first, then frontend |

**Restart commands:**
```bash
# Restart backend
netstat -ano | grep ":4000 " | grep "LISTEN" | awk '{print $5}' | sort -u | while read pid; do taskkill //F //PID "$pid" 2>/dev/null; done
sleep 2
cd src/NOIR.Web && dotnet build --nologo -v q -c Debug > /dev/null 2>&1
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://localhost:4000" dotnet watch run --no-launch-profile > ../../.backend.log 2>&1 &
for i in $(seq 1 60); do curl -sf http://localhost:4000/robots.txt -o /dev/null 2>/dev/null && break; sleep 1; done

# Restart frontend
netstat -ano | grep ":3000 " | grep "LISTEN" | awk '{print $5}' | sort -u | while read pid; do taskkill //F //PID "$pid" 2>/dev/null; done
sleep 2
powershell -Command "Start-Process cmd -ArgumentList '/c cd /d src\NOIR.Web\frontend && pnpm run dev > ..\..\..\.frontend.log 2>&1'"
sleep 5
```

#### Health Check (run before EACH page test)

```bash
curl -sf http://localhost:4000/robots.txt -o /dev/null || echo "WARN: Backend down"
curl -sf http://localhost:3000 -o /dev/null || echo "WARN: Frontend down"
```

If down → restart automatically → log crash in QA report → resume.

### 0.3 Browser State Reset

Before starting Phase 1 (before login), clear browser state:
```
Use Playwright MCP: browser_evaluate → localStorage.clear(); sessionStorage.clear();
```

Between major phases (every 2-3 phases), clear stale non-auth data to prevent false bugs:
```
Use Playwright MCP: browser_evaluate →
  const authKeys = ['token', 'auth', 'user', 'session'];
  Object.keys(localStorage).forEach(key => {
    if (!authKeys.some(ak => key.toLowerCase().includes(ak))) localStorage.removeItem(key);
  });
```
This preserves auth token while clearing stale table settings, filter state, etc.

If auth token IS expired (redirected to login), just re-login: `admin@noir.local / 123qwe`.

### 0.4 Create QA Tracking Document

Create `temp/qa-report.md`:

```markdown
# NOIR Real QA Report
- **Date**: [today]
- **Tester**: Claude Code AI QA Agent
- **App Version**: [git rev-parse --short HEAD]
- **Status**: IN PROGRESS

## Executive Summary
[Filled at completion — pass rate, severity breakdown, risk areas, total time]

## Stats
| Metric | Count |
|--------|-------|
| Pages Tested | 0 / [total] |
| Bugs Found | 0 |
| Bugs Fixed & Verified | 0 |
| Bugs Remaining | 0 |
| CRITICAL | 0 |
| HIGH | 0 |
| MEDIUM | 0 |
| LOW | 0 |

## Bug Log
| # | Phase | Page | Severity | Description | Root Cause | Fix | Status |
|---|-------|------|----------|-------------|------------|-----|--------|

## Page-by-Page Results
[filled per page]

## Cross-Feature Flow Results
[filled in Phase 10]
```

### 0.5 Build the Page Inventory

Read `src/NOIR.Web/frontend/src/portal-app/` and router files to enumerate ALL pages. Build complete checklist:

```
PHASE 1 — Auth & Core:
  □ Login page (both admin accounts: admin@noir.local, platform@noir.local)
  □ Dashboard (all widget groups, metric accuracy)
  □ Profile / Account settings
  □ Theme switching (light/dark) — verify on Dashboard
  □ Language switching (EN/VI) — verify on Dashboard

PHASE 2 — Settings & Config:
  □ General Settings (ALL tabs: branding, regional, email/SMTP, etc.)
  □ Feature Management (enable/disable modules, verify sidebar updates)
  □ Users (list, create, edit, delete, role assignment, avatar)
  □ Roles (list, create, edit, delete, permission picker)
  □ Tenants (platform admin: list, create, edit, settings)
  □ API Keys (if exists)
  □ Webhooks Settings

PHASE 3 — Catalog:
  □ Products (list, create with variants, edit, images, status transitions)
  □ Product Categories (tree view, CRUD, drag-reorder)
  □ Product Attributes (CRUD, filter config, types)
  □ Brands (CRUD, logo upload)
  □ Inventory Receipts (create StockIn/StockOut, confirm, cancel, line items)

PHASE 4 — Orders & Fulfillment:
  □ Orders (list, detail, ALL status transitions, timeline)
  □ Payments (list, detail, transaction timeline)
  □ Shipping (providers, tracking)

PHASE 5 — Customers & Marketing:
  □ Customers (list, detail with tabs, order history, addresses)
  □ Customer Groups (CRUD, rule-based membership)
  □ Promotions (CRUD, date range picker, usage limits, status)
  □ Reviews (list, approve/reject moderation workflow)
  □ Wishlists

PHASE 6 — Content:
  □ Blog Posts (CRUD, rich text editor, image upload, category/tag assignment)
  □ Blog Categories (CRUD)
  □ Blog Tags (CRUD)
  □ Media Library (upload, preview, delete, search)

PHASE 7 — HR Module:
  □ Employees (list, create, edit, import/export CSV)
  □ Departments (tree view, CRUD)
  □ Employee Tags (card layout by category, CRUD, colors)
  □ HR Reports (if exists)

PHASE 8 — CRM Module:
  □ Contacts (list, CRUD, link to company)
  □ Companies (list, CRUD)
  □ Leads/Pipeline (Kanban board, drag-drop between stages, win/lose)
  □ CRM Activities (create on contact, timeline)
  □ CRM Dashboard widgets

PHASE 9 — PM Module:
  □ Projects (list, CRUD, members)
  □ Kanban Board (columns CRUD, task create, drag-drop between columns)
  □ Task Detail (subtasks, comments, attachments, labels, assignees)
  □ Task List View (alternative to Kanban)
  □ Archived Tasks

PHASE 10 — Cross-Feature Flows (see dedicated section)

PHASE 11 — Final Verification Loop (see dedicated section)
```

---

## PHASE 1-9: SYSTEMATIC PAGE TESTING

For EACH page, execute ALL 5 steps below. No exceptions, no shortcuts.

**CORE PRINCIPLE**: Dark mode, Vietnamese, and responsive are NOT separate phases — they are tested on EVERY visual state (page, tab, dialog, popup). This catches the bugs that single-mode testing misses.

### Step 1: Navigate & Triple-Mode Visual Audit (page level)

For the page's initial view, run the **Triple-Mode Check** (light → dark → Vietnamese → responsive):

```
=== LIGHT MODE (baseline) ===
1. browser_navigate → page URL
2. browser_wait_for → network idle / specific element visible
3. browser_take_screenshot → temp/qa-p{N}-{page}-light.png
4. READ screenshot and analyze:
   - Layout: alignment, spacing, overflow, content clipping
   - Components: broken images, missing icons, wrong empty states
   - Typography: font consistency, text truncation, ellipsis working
   - Colors: theme consistency, contrast, hover states visible
   - Loading: skeleton → data (no flash of empty)
   - Card shadows: shadow-sm + hover:shadow-lg per design standard

=== DARK MODE ===
5. browser_evaluate → document.documentElement.classList.add('dark')
   (or browser_snapshot → find theme toggle → browser_click)
6. browser_take_screenshot → temp/qa-p{N}-{page}-dark.png
7. READ screenshot and verify:
   - Text readable (no white-on-white, no black-on-black)
   - Borders visible (not disappearing into background)
   - Status badges maintain contrast
   - Charts/graphs readable
   - No hardcoded colors overriding theme variables
   - Input fields: background distinguishable from page background
   - Dropdown menus / popovers: correct dark background (not white)

=== VIETNAMESE ===
8. browser_evaluate → localStorage.setItem('i18nextLng','vi'); location.reload()
9. browser_wait_for → content loaded
10. browser_take_screenshot → temp/qa-p{N}-{page}-vi.png
11. READ screenshot and verify:
    - No raw i18n keys (e.g., "products.searchPlaceholder" as literal text)
    - No English text remaining (except allowed: CRM, API, SMTP, Blog)
    - No text overflow from longer Vietnamese strings (buttons, headers, labels)
    - Date format matches Vietnamese locale (DD/MM/YYYY)
    - Sidebar labels are pure Vietnamese (no "Blog Posts", must be "Bài viết")
    - Vietnamese + dark mode: switch to dark briefly → screenshot → verify no issues
    - Switch back to light mode after: browser_evaluate → document.documentElement.classList.remove('dark')

=== RESPONSIVE (every page) ===
12. Restore to light mode + English before responsive checks:
    browser_evaluate → document.documentElement.classList.remove('dark'); localStorage.setItem('i18nextLng','en'); location.reload()
13. browser_wait_for → content loaded
14. For each viewport [1440, 1024, 768]:
    - browser_resize → {width}px
    - browser_take_screenshot → temp/qa-p{N}-{page}-{width}px.png
    - READ screenshot and verify:
      - 1440: full layout, sidebar visible, all columns shown
      - 1024: sidebar may collapse, table columns may hide, no overflow
      - 768: mobile/tablet layout, content stacks vertically, no horizontal scroll on body
    - Specific checks:
      - Tables: horizontal scroll within table container (not page-level scroll)
      - Buttons: not cut off or overflowing container
      - Cards: stack vertically, maintain padding
      - PageHeader: title + actions wrap correctly (not overlap)
      - Search input: remains usable width
15. browser_resize → restore 1440px
```

### Step 2: Interactive Element Testing (with in-context dark/VI/responsive checks)

For EACH interactive element type present on the page.

**KEY RULE**: Every distinct visual state gets the **Full Mini Triple Check** — no exceptions, no `--` in the matrix.

**Mini Triple Check protocol** (run on every item in the list below):
1. Screenshot in light mode (baseline)
2. Switch to dark mode → screenshot → verify: text readable, borders visible, inputs distinguishable, popover/dropdown backgrounds correct
3. Switch to Vietnamese → screenshot → verify: all text translated, no overflow from longer strings, no raw i18n keys
4. Resize to 768px → screenshot → verify: layout adapts, content accessible, no overflow, buttons reachable
5. Restore light mode + English + 1440px before proceeding

**FULL list of visual states requiring Mini Triple Check** (100% — every element, every mode):

| Visual State | Light | Dark | Vietnamese | 768px | Notes |
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
| **Toast/notification** | ✅ | ✅ | ✅ | ✅ | Background contrast, text, position at 768px |
| **Date picker popup** (calendar) | ✅ | ✅ | ✅ | ✅ | Month/day names, calendar grid bg |
| **Dropdown menu** (actions `⋮`) | ✅ | ✅ | ✅ | ✅ | Menu item text, background, hover |
| **Select/Combobox dropdown** | ✅ | ✅ | ✅ | ✅ | Option labels, search, selected state |
| **Color picker popup** | ✅ | ✅ | ✅ | ✅ | Color swatch visibility in dark |
| **Loading/skeleton state** | ✅ | ✅ | — | ✅ | Skeleton colors in dark, layout at 768px (no text to translate) |
| **Bulk action toolbar** | ✅ | ✅ | ✅ | ✅ | Selection count, action buttons |
| **Tooltip** (on icon buttons) | ✅ | ✅ | ✅ | — | Content, background (no resize needed — follows cursor) |

**100% coverage = no `--` in the matrix.** The only exemption is Loading state × Vietnamese (skeletons have no translatable text) and Tooltip × 768px (tooltips are cursor-attached, viewport doesn't affect them).

**Efficiency tip**: For elements that appear identically across pages (e.g., the same DataTablePagination component), you only need full Mini Triple Check on the FIRST occurrence. Subsequent pages: quick dark mode spot-check is sufficient. But for page-specific dialogs, tabs, and empty states: always full check.

#### TABLES (DataTable)
- Column headers render correctly, all have labels
- Sort: click each sortable column header, verify order changes + sort indicator
- Pagination: go to page 2, change page size, verify "Showing X of Y" updates
- Search: type query → verify filtering → clear → verify reset
- Column visibility: open dropdown, hide a column, verify gone, show back
- Column reorder: if supported, drag column, verify new order
- Density toggle: switch compact/normal/comfortable if available
- Empty state: search nonsense → verify `<EmptyState>` component (not plain text)
  → **🔲 Mini Triple Check** on empty state (all 4 modes: dark bg, VI text, 768px layout)
- Row selection: check/uncheck rows, verify bulk action toolbar appears
- Actions dropdown: click `⋮` → verify menu items → verify each action works
- Group by: if supported, group → verify group headers with correct count

#### FORMS (Create/Edit dialogs)
- Open Create dialog → screenshot
- **🔲 Mini Triple Check on dialog:**
  - Dark mode: verify dialog background, input borders, dropdown menus, date picker popover
  - Vietnamese: verify all labels translated, no text overflow on buttons, field labels fit
  - 768px: verify dialog resizes properly, form fields stack, footer buttons accessible, no content clipped by dialog edges
- **Field-by-field testing** for EVERY field:
  - Focus field → verify focus ring/border appears
  - Tab to next field → verify tab order (top-to-bottom, left-to-right)
  - Required fields → verify red asterisk `*` on label
  - Type invalid value → blur → verify error appears (NOT while typing)
  - Type valid value → verify error clears
  - Dropdowns: open → select → verify selection shows
  - Date pickers: open → select → verify format. **Dark mode**: open picker → screenshot → verify calendar popup is dark
  - Rich text editors: type → verify toolbar works
  - File uploads: upload → verify preview shows
  - Color pickers: select → verify preview
- Submit empty → verify ALL required field errors appear at once
- Submit invalid → verify specific field errors (not generic toast)
- Submit valid → verify: toast + dialog closes + list updates + data matches
- Cancel button → dialog closes, no data changed
- Click outside → closes (Credenza behavior)
- ESC → closes
- **Edit mode**: open existing item → verify all fields pre-populated
  → **🔲 Mini Triple Check** on edit dialog (may differ from create)

#### FILTERS
- Apply each filter individually → verify results update
- Apply filter combination → verify AND logic
- Verify active filter indicator/badge shows count
- Clear individual → verify results update
- Clear all → verify full reset
- Filter persists across pagination

#### DESTRUCTIVE ACTIONS (Delete, Archive, Status Change)
- Click delete/archive → verify confirmation dialog appears (NEVER instant delete)
- **🔲 Mini Triple Check** on confirmation dialog:
  - Dark mode: destructive button styling visible, text readable
  - Vietnamese: confirmation text translated, button text fits
  - 768px: buttons don't overflow, dialog usable
- Cancel → nothing changed
- Confirm → item removed/updated + toast + related data updated + activity timeline

#### TABS
- **For EACH tab:**
  1. Click tab → verify content loads
  2. Verify URL updates with `?tab=xxx`
  3. **🔲 Mini Triple Check on tab content:**
     - Dark mode: screenshot → check tab-specific content (charts, tables, timelines)
     - Vietnamese: screenshot → check tab-specific labels/headings translated
     - 768px: screenshot → check tab content layout responsive
  4. Direct-navigate to `?tab=xxx` → verify correct tab active + content loaded
  5. Verify no flash/flicker on tab switch

#### MESSAGES & NOTIFICATIONS
Every message type gets **🔲 Mini Triple Check** per the matrix above.

- **Toasts**: trigger a success action → verify position, auto-dismiss, text content
  → **🔲 Mini Triple Check**: dark bg/contrast, VI translated text, 768px not clipped by screen edge
- **Error banners** (`FormErrorBanner`): trigger server error → verify shows, dismiss works, clears on retry
  → **🔲 Mini Triple Check**: dark banner bg visible, VI error message fits, 768px banner wraps correctly
- **Validation messages**: submit invalid form → verify specific field errors, positioned under field
  → **🔲 Mini Triple Check**: dark error text color visible vs dark input bg, VI messages fit under field, 768px fields + errors don't overflow
- **Confirmation dialogs**: trigger destructive action → verify text is descriptive
  → (already covered in DESTRUCTIVE ACTIONS Mini Triple Check)
- **Empty states**: filter/search to 0 results → verify icon + title + description
  → (already covered in TABLES Mini Triple Check)
- **Loading states**: hard-refresh page → verify skeleton/spinner during fetch
  → Dark mode: verify skeleton shimmer colors. 768px: verify skeleton layout matches content layout
- **Date picker popup**: open a date field → verify calendar renders
  → **🔲 Mini Triple Check**: dark calendar bg/text, VI month/day names translated, 768px calendar fits viewport
- **Select/Combobox dropdown**: open a select field → verify options render
  → **🔲 Mini Triple Check**: dark dropdown bg, VI option labels translated, 768px dropdown fits
- **Tooltip**: hover icon-only button → verify tooltip shows
  → Dark mode: verify tooltip bg contrast. Vietnamese: verify tooltip text translated

#### NAVIGATION
- Breadcrumbs: correct path, each segment clickable
- Sidebar: current page highlighted
- Back navigation: browser back works correctly
- Deep link: navigate to URL directly → verify same state loads

### Step 3: Data Consistency Checks

After EACH CRUD operation, verify:
```
CREATE:
  - Item appears in list with correct data
  - List total count incremented
  - Dashboard widgets updated (if applicable)
  - Activity timeline shows "Created ..." entry
  - Related entities reflect change (e.g., department employee count)
  - Search returns the new item

EDIT:
  - ALL changed fields persisted (re-open edit dialog to verify)
  - List row updated with new values
  - Detail page (if exists) shows updated data
  - Activity timeline shows "Updated ..." entry
  - Related views reflect change

DELETE (soft):
  - Item removed from active list
  - List total count decremented
  - Related entities updated (counts, references)
  - Activity timeline shows "Deleted ..." entry
  - Dashboard widgets updated
```

### Step 4: Document Results

For each page, add to `temp/qa-report.md`:

```markdown
### [Page Name] — [PASS ✅ / FAIL ❌]
- **URL**: /portal/...
- **Page-level Triple Check**:
  - Light: PASS/FAIL — temp/qa-p{N}-{page}-light.png
  - Dark: PASS/FAIL — temp/qa-p{N}-{page}-dark.png
  - Vietnamese: PASS/FAIL — temp/qa-p{N}-{page}-vi.png
  - Responsive 1024: PASS/FAIL — temp/qa-p{N}-{page}-1024px.png
  - Responsive 768: PASS/FAIL — temp/qa-p{N}-{page}-768px.png
- **Tabs tested** (with Mini Triple Check): [list tab names] or N/A
- **Dialogs tested** (with Mini Triple Check): [list dialog names] or N/A
- **Interactive**: PASS/FAIL (tables, forms, filters, actions)
- **Data consistency**: PASS/FAIL
- **Bugs Found**: [count]
  - BUG-XXX: [severity] — [description] — [context: dark/vi/responsive/light]
- **Notes**: [observations]
```

### Step 5: Fix Bugs Immediately

When you find a bug:

1. **Log it** — add row to Bug Log table in `temp/qa-report.md` with severity + description
2. **Analyze root cause** — read relevant source code, trace the issue
3. **Fix it** — make the minimal code change
4. **Restart if needed** (see Phase 0 Restart Protocol):
   - Frontend CSS/TSX/JSON → HMR, wait 2s
   - Backend handler/service → `dotnet watch` auto-reloads, wait 3-5s
   - DI/Program.cs → restart backend
   - vite.config → restart frontend
5. **Build check** — only if the fix touches logic, not just CSS:
   - Frontend logic: `cd src/NOIR.Web/frontend && pnpm run build`
   - Backend: `dotnet build src/NOIR.sln`
6. **Health check**:
   ```bash
   curl -sf http://localhost:4000/robots.txt -o /dev/null && echo "Backend: OK"
   curl -sf http://localhost:3000 -o /dev/null && echo "Frontend: OK"
   ```
7. **Re-verify** — navigate back, repeat the exact test that found the bug
8. **Screenshot** fixed state → `temp/qa-p{N}-{page}-fixed-BUG{XXX}.png`
9. **Update** Bug Log status to `FIXED+VERIFIED`
10. **Check for regression** — if the fix touches a SHARED component (`@uikit`, `@/components`, `@/hooks`, `@/lib`), note it in `temp/qa-progress.json` → `regressionNeeded: true` with list of affected pages to re-check in Phase 11

**FIX-THEN-COMMIT strategy**: After fixing ALL bugs on a single page (not per-bug), create ONE commit:
```bash
# Stage only the specific files you changed (NEVER git add -A)
git add src/NOIR.Web/frontend/src/portal-app/products/ProductsPage.tsx
git add src/NOIR.Web/frontend/public/locales/vi/common.json
# ... list each changed file explicitly

git commit -m "$(cat <<'EOF'
fix(qa): [page-name] — [N] bugs fixed

- BUG-001: [brief description]
- BUG-002: [brief description]

QA: Real QA Agent Phase [N]

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```
This ensures work is saved incrementally and can be reviewed/reverted per page.

**SERVICE CRASH RECOVERY**:
1. Check which service is down + check logs: `tail -20 .backend.log` or `.frontend.log`
2. If crash caused by your fix → revert fix first
3. Restart crashed service
4. Wait for health check
5. Resume testing

---

## PHASE 10: CROSS-FEATURE VERIFICATION

After all individual pages pass, test end-to-end flows that span multiple features:

### Flow 1: Product → Order → Payment Lifecycle
```
Create Product → Add 2 Variants with different prices → Set Inventory (StockIn receipt, confirm)
→ Verify Dashboard product count increased
→ Create Customer → Create Order with the product variant
→ Confirm Order → Check inventory quantity decreased
→ Process Order → Ship Order → Mark Delivered → Complete Order
→ Verify Payment shows in Payments list with correct amount
→ Verify revenue appears in Dashboard
→ Customer writes Review on product → Approve Review
→ Verify review count on product detail
```

### Flow 2: Customer Journey
```
Create Customer → Add to Customer Group → Verify group count
→ Create Order for customer → View Customer Detail → Verify order in history tab
→ Cancel order → Verify status updates in: Order detail, Customer history, Dashboard
→ Check Activity Timeline shows all actions with correct descriptions
→ Switch to Vietnamese → Verify activity descriptions are translated
```

### Flow 3: Content Publishing
```
Create Blog Category → Create Blog Tags (2+)
→ Create Blog Post with featured image upload → Assign category + tags → Save as Draft
→ Verify Media Library shows uploaded image
→ Edit post → Publish → Verify status badge changes
→ Delete one tag → Verify post's tag list updates (tag removed, not crash)
→ Edit category name → Verify post shows updated category name
```

### Flow 4: HR Workflow
```
Create Department (child of existing) → Verify tree shows new department
→ Create Employee in that department → Add Tags (Skill + Team)
→ Verify Department detail shows employee count = 1
→ Edit employee details → Verify changes persist
→ Export employees to CSV → Verify CSV contains the employee
→ Import modified CSV (change a field) → Verify change applied
→ Move employee to different department → Verify both department counts update
```

### Flow 5: CRM Pipeline
```
Create Company → Create Contact linked to Company → Verify company shows contact count
→ Create Lead in pipeline → Verify Kanban shows lead in first stage
→ Drag lead to next stage → Verify stage changed
→ Move through all stages → Win lead → Verify dashboard metrics update
→ Create Activity on contact → Verify timeline shows activity
→ Delete company → Verify contacts still accessible (soft delete integrity)
```

### Flow 6: PM Workflow
```
Create Project → Verify project code auto-generated (PRJ-xxx)
→ Add custom columns → Create 3 tasks in different columns
→ Drag task between columns → Verify status changes in task detail
→ Add subtask to a task → Mark subtask complete → Verify parent task progress
→ Add comment → Verify comment shows with timestamp
→ Upload attachment → Verify download works
→ Archive task → Verify it disappears from Kanban, appears in Archived view
```

### Flow 7: Data Integrity & Edge Cases
```
Delete a Brand that has products → Verify products still display (brand shows empty/null)
Disable a Module (e.g., CRM) → Verify:
  - Sidebar item disappears
  - Direct URL navigates to 404 or redirect
  - Dashboard CRM widgets hidden
  - Re-enable module → everything returns
Create a test user (e.g., qa-tester@noir.local) with a custom role → Remove a permission from that role
→ Open incognito/new context, login as qa-tester@noir.local → Verify:
  - Sidebar hides restricted pages
  - Direct URL returns 403 or redirect
  - Actions are disabled/hidden
→ Switch back to admin session for remaining tests
Switch language EN→VI→EN → Verify:
  - All sidebar labels correct in each language
  - No raw i18n keys in page content
  - Date formats change appropriately
Switch theme light→dark→light → Verify:
  - No visual artifacts from switching
  - All pages maintain readability
```

### Flow 8: Error Handling & Edge Cases
```
Navigate to /portal/products/00000000-0000-0000-0000-000000000000 → Verify 404 page or graceful redirect
Open a Create dialog, stop backend (kill :4000), click Submit → Verify error banner shows (not white screen/crash)
  → Restart backend after test
Open edit dialog for an entity, then use curl to delete it via API:
  `curl -X DELETE http://localhost:4000/api/[entity]/[id] -H "Authorization: Bearer [token]"`
  → Click Save in the dialog → Verify conflict/not-found handling (not silent failure)
Rapidly click pagination (page 1→2→3→4 fast) → Verify final page shows correct data (no stale/mixed data)
Rapid double-click on Submit button → Verify only ONE entity created (not duplicates)
Navigate to a list page, use curl to delete an item in the background, then refresh → Verify list handles missing item gracefully
```

---

## BUG SEVERITY CLASSIFICATION

| Severity | Definition | Examples |
|----------|-----------|---------|
| **CRITICAL** | Feature broken, data loss, security issue | Form submits but data not saved, crash/white screen, security bypass |
| **HIGH** | Feature partially broken, bad UX | Wrong data displayed, dialog won't close, pagination broken, missing translations on main elements |
| **MEDIUM** | Visual bug, minor UX issue | Misalignment, wrong icon, missing translation on secondary text, dark mode contrast issue |
| **LOW** | Cosmetic, nice-to-have | Hover effect inconsistent, spacing off by a few px, minor animation glitch |

---

## PHASE 11: FINAL VERIFICATION LOOP

**This is what makes the prompt truly 100% complete.**

After Phase 10, run this verification loop:

### 11.1 Regression Check

If `temp/qa-progress.json` has `regressionNeeded: true`:
1. Read the list of affected pages from progress file
2. Re-test ONLY the specific functionality affected by the shared component fix
3. Screenshot and document any new bugs
4. Fix → verify → update report

### 11.2 Full Build Verification

```bash
# All four must pass with zero errors
cd src/NOIR.Web/frontend && pnpm run build         # Frontend strict mode
cd src/NOIR.Web/frontend && pnpm build-storybook   # Storybook build (catches component import errors)
dotnet build src/NOIR.sln                           # Backend build
dotnet test src/NOIR.sln                            # All backend tests
```

If any test fails because of your fixes → fix the test or code → re-run → must be green.

### 11.3 Spot-Check Random Pages

Pick 5 random pages from different phases. Quick visual check (screenshot + 30s scan) to catch any regression from the cumulative fixes.

### 11.4 Completion Checklist

You are NOT done until EVERY checkbox is checked:

**Page-level (Step 1 Triple Check):**
- [ ] Every page screenshotted in LIGHT mode
- [ ] Every page screenshotted in DARK mode
- [ ] Every page screenshotted in VIETNAMESE
- [ ] Every page screenshotted at 1024px and 768px responsive

**Element-level (Step 2 Mini Triple Checks — 100% matrix, no `--`):**
- [ ] Every TAB content verified in dark + Vietnamese + 768px
- [ ] Every DIALOG (create, edit, detail, confirmation) verified in dark + VI + 768px
- [ ] Every EMPTY STATE verified in dark + Vietnamese + 768px
- [ ] Every ERROR BANNER / VALIDATION verified in dark + VI + 768px
- [ ] Every TOAST verified in dark + Vietnamese + 768px
- [ ] Every DATE PICKER popup verified in dark + Vietnamese + 768px
- [ ] Every SELECT/COMBOBOX dropdown verified in dark + Vietnamese + 768px
- [ ] Every TOOLTIP verified in dark + Vietnamese

**Functional:**
- [ ] Every table tested (sort, search, pagination, columns, empty state)
- [ ] Every form tested (all fields, validation, submit, cancel, ESC)
- [ ] Every destructive action has confirmation dialog
- [ ] Every CRUD operation verified with data consistency
- [ ] All 8 cross-feature flows completed

**Quality:**
- [ ] All bugs FIXED and RE-VERIFIED
- [ ] Regression check completed (if shared components changed)
- [ ] `temp/qa-report.md` complete for ALL pages
- [ ] Zero CRITICAL bugs remaining
- [ ] Zero HIGH bugs remaining

**Build:**
- [ ] Frontend build passes (zero errors, zero warnings)
- [ ] Storybook build passes (zero errors)
- [ ] Backend build passes
- [ ] Backend tests pass (zero failures)
- [ ] All fixes committed to git

**If ANY checkbox is unchecked → go back and complete it. DO NOT proceed to deliverables.**

---

## PROGRESS PERSISTENCE

After completing each PHASE, save checkpoint:

```json
// temp/qa-progress.json
{
  "status": "IN_PROGRESS",
  "lastCompletedPhase": 3,
  "lastCompletedPage": "product-attributes",
  "currentPhase": 4,
  "nextPage": "orders",
  "totalPagesCompleted": 15,
  "totalPages": 52,
  "bugsFound": 8,
  "bugsFixed": 8,
  "bugsRemaining": 0,
  "regressionNeeded": false,
  "regressionPages": [],
  "commits": ["abc1234", "def5678"],
  "timestamp": "2026-03-21T10:30:00Z",
  "sessionCount": 1
}
```

**Token management**: If approaching context limits:
1. Save progress to `temp/qa-progress.json`
2. Ensure `temp/qa-report.md` is up to date
3. Commit any uncommitted fixes
4. Tell user: **"Progress saved at Phase X, page Y. Run `/noir-real-qa` to resume. Z pages remaining."**
5. On resume: read progress file → skip completed work → continue

---

## POST-COMPLETION DELIVERABLES

After Phase 11 checklist is 100% green:

### 1. Final QA Report

Update `temp/qa-report.md` with:
- **Status**: change to `COMPLETE`
- **Executive Summary**:
  - Total pages tested / total bugs found / fixed
  - Severity breakdown (CRITICAL/HIGH/MEDIUM/LOW)
  - Top 3 riskiest areas (pages with most bugs)
  - Top 3 most common bug patterns
  - Time span (first session → last session)
- Rename to `temp/qa-report-final.md`

### 2. Memory Update

Save to `C:\Users\topnguyen\.claude\projects\d--GIT-TOP-NOIR\memory\`:

File: `feedback_real-qa-patterns.md`
```markdown
---
name: Real QA Bug Patterns
description: Common bug patterns found during exhaustive real QA testing — use to prevent recurrence
type: feedback
---

[List of bug patterns with root causes and prevention rules]
**Why:** Found during Real QA sweep on [date] — these bugs pass automated tests but fail visual/behavioral testing
**How to apply:** Check these patterns when modifying the affected areas
```

Update `MEMORY.md` index with pointer to new file.

### 3. Rule Updates

If bugs reveal missing rules:
- Add new `.claude/rules/` file if pattern is systemic (3+ instances)
- Update existing rule if a rule was incomplete
- Update `CLAUDE.md` only if a Critical rule was missed

### 4. Lessons Learned Doc

Create `docs/qa/real-qa-lessons-[date].md`:
```markdown
# Real QA Lessons — [Date]

## Summary
- Pages tested: X
- Bugs found: Y (Z critical, W high, ...)
- Bugs fixed: Y

## Bug Pattern Analysis
[Categorized: Visual | Behavioral | Data | Translation | Theme | Field-level]

## High-Risk Areas
[Pages/features with most bugs — prioritize in future development]

## Root Cause Analysis
[Why automated tests missed these — systemic gaps]

## Prevention Recommendations
[Process/rule changes to prevent recurrence]
```

### 5. Update Progress File

```json
// temp/qa-progress.json
{
  "status": "COMPLETE",
  "completedAt": "2026-03-21T18:00:00Z",
  "totalPages": 52,
  "totalBugsFound": 15,
  "totalBugsFixed": 15,
  "commits": ["abc1234", "def5678", "..."],
  "reportPath": "temp/qa-report-final.md",
  "lessonsPath": "docs/qa/real-qa-lessons-2026-03-21.md"
}
```

---

## TECHNICAL NOTES

### Playwright MCP Tools

Use `mcp__playwright__*` tools for ALL browser interactions:
- `mcp__playwright__browser_navigate` — go to URLs
- `mcp__playwright__browser_click` — click elements (use accessible name or CSS selector)
- `mcp__playwright__browser_fill_form` — fill multiple form fields at once
- `mcp__playwright__browser_take_screenshot` — capture current state to temp/
- `mcp__playwright__browser_snapshot` — get accessibility tree (for finding elements)
- `mcp__playwright__browser_select_option` — for `<select>` dropdowns
- `mcp__playwright__browser_press_key` — keyboard shortcuts (Escape, Enter, Tab)
- `mcp__playwright__browser_wait_for` — wait for network idle or element visible
- `mcp__playwright__browser_hover` — test hover states
- `mcp__playwright__browser_drag` — test drag-and-drop (Kanban, tree reorder)
- `mcp__playwright__browser_type` — type text character by character
- `mcp__playwright__browser_resize` — test responsive breakpoints

**Strategy**: Always `browser_snapshot` first to understand the page structure, then use accessible names from the snapshot for clicking/filling. This is more robust than CSS selectors.

**CRITICAL — Windows SPA Navigation Bug**: On Windows, `browser_navigate` / `page.goto()` and `window.location.href` often produce **blank white pages** for React SPA routes. The HTML shell loads but React doesn't mount (root element missing). This is a Playwright MCP issue, NOT an app bug. **Workaround**: Navigate via SPA sidebar clicks (`browser_click` on sidebar links) instead of direct URL navigation. Use `browser_navigate` ONLY for the initial page load (e.g., `/portal`), then use sidebar clicks for all subsequent pages. For pages without sidebar links (detail pages), click on a table row from the parent list page.

**Context Efficiency**: Save `browser_snapshot` output to files (`filename` param) instead of inline. NEVER dump the full accessibility tree for sidebar navigation — it's 100+ lines of identical content on every page. Only read the `<main>` content area. For pages that follow established DataTable patterns, a screenshot is sufficient — skip the tree dump.

### Login Flow

```
1. browser_navigate → http://localhost:3000/login
2. browser_snapshot → find email/password fields
3. browser_fill_form → email: admin@noir.local, password: 123qwe
4. browser_click → Sign In button
5. browser_wait_for → dashboard loaded
6. After each phase, verify session: browser_navigate to any page, check if redirected to login
7. For Platform Admin tests (Phase 2 Tenants):
   - Logout first
   - Login with: platform@noir.local / 123qwe
   - Complete platform admin tests
   - Logout, login back as admin@noir.local
```

### Screenshot Naming

```
temp/qa-p{phase}-{page}-{variant}.png

Page-level variants:
  -light.png              Light mode
  -dark.png               Dark mode
  -vi.png                 Vietnamese
  -1024px.png             Responsive 1024px
  -768px.png              Responsive 768px

Element-level variants (Mini Triple Checks):
  -tab-{name}-light.png       Tab content, light
  -tab-{name}-dark.png        Tab content, dark
  -tab-{name}-vi.png          Tab content, Vietnamese
  -tab-{name}-768px.png       Tab content, 768px
  -create-dialog-light.png    Create dialog, light
  -create-dialog-dark.png     Create dialog, dark
  -create-dialog-vi.png       Create dialog, Vietnamese
  -create-dialog-768px.png    Create dialog, 768px
  -edit-dialog-dark.png       Edit dialog, dark
  -confirm-dialog-dark.png    Confirmation, dark
  -empty-state-dark.png       Empty state, dark

Action variants:
  -create-error.png      Validation errors
  -create-valid.png      Successful submission
  -fixed-BUG{N}.png      After bug fix
```

### Bug Decision Tree (with restart guidance)

```
CSS/layout bug?
  → Fix component styles or Tailwind classes
  → Restart: NO (Vite HMR)

Missing translation?
  → Add to BOTH en/*.json AND vi/*.json
  → Restart: NO (HMR reloads JSON)

Dark mode bug?
  → Fix: likely hardcoded color or missing dark: variant
  → Restart: NO

Data display bug?
  → Trace: component → API hook → query handler → specification
  → Fix at correct layer
  → Restart: Backend auto-reloads with dotnet watch (wait 3-5s)

Form validation bug?
  → Check Zod schema + FluentValidation
  → Fix both if needed
  → Restart: Backend wait for watch reload

State management bug?
  → Check TanStack Query invalidation
  → Fix cache invalidation or component re-render
  → Restart: NO

Navigation/routing bug?
  → Check router config, useUrlTab/useUrlDialog usage
  → Restart: NO unless vite.config changed

DI/startup/middleware bug?
  → Fix in Program.cs or DependencyInjection.cs
  → Restart: BACKEND YES (always — watch can't hot-reload startup)

Field-level UX bug (tab order, focus, asterisk)?
  → Fix in FormField/FormItem or component props
  → Restart: NO
```

---

## EXECUTION MODE

**AUTONOMOUS** — do not ask user questions during testing. Make best judgment, document decisions.

**Ask user ONLY if:**
- Services cannot start (after 2 retry attempts)
- Database is corrupted or needs manual intervention
- Bug requires breaking architectural change (affects 10+ files)
- You discover a security vulnerability

**Pacing**: Test thoroughly but efficiently. Don't spend 10 minutes on a page that clearly works. Focus testing time on:
1. Complex pages (Products, Orders, Kanban boards) — more time
2. Simple CRUD pages (Brands, Blog Tags) — less time
3. Cross-feature flows — thorough verification
4. Pages with prior history of bugs (check git log) — extra attention

---

## CRITICAL RULES

1. **NEVER skip Mini Triple Check on dialogs/tabs** — dark mode + Vietnamese + responsive on every dialog, every tab. This is where 60%+ of visual bugs hide.
2. **NEVER test only in light mode** — if you didn't screenshot it in dark mode, you didn't test it
3. **NEVER batch fixes across pages** — fix per-page, commit per-page, prevents cascade failures
4. **NEVER declare a page PASS if ANY interactive element was untested** — test everything or mark what was skipped and why
5. **ALWAYS screenshot before and after fixes** — evidence trail for the final report
6. **ALWAYS commit after each page's fixes** — prevents losing work if session is interrupted
7. **ALWAYS verify services are alive before testing a page** — stale browser on dead backend = false bugs
8. **ALWAYS update progress file after each phase** — enables resume across sessions
9. **NEVER proceed to deliverables until Phase 11 checklist is 100%** — the verification loop IS the quality gate
