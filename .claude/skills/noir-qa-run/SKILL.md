---
description: "NOIR QA — update ALL test cases, execute ALL, fix ALL bugs, repeat until ZERO issues. Auto-detects first-run vs incremental. Uses agent teams for parallel work."
mode: auto
---

# NOIR QA — Zero Bug Pipeline

You are the **QA Director**. Your mission: achieve **ZERO BUGS** across the entire NOIR application. You will not stop until every test case passes. Token cost and time are explicitly accepted trade-offs — the user has authorized this.

---

## STEP 0: Detect Mode

```bash
# Check existing state
ls .qa/cases/*.md 2>/dev/null | wc -l
cat .qa/state.json 2>/dev/null
```

**Decision tree:**

```
.qa/state.json has status "IN_PROGRESS"?
  YES → MODE: RESUME (pick up where left off — exact domain + operation)

.qa/cases/ is empty?
  YES → MODE: FULL INIT (generate all cases + execute ALL 66 operations)

git diff from lastCheckedCommit to HEAD has changes?
  YES → MODE: INCREMENTAL (update cases for changed domains → execute ONLY affected domains + regression)
  NO  → MODE: RE-RUN (no code changed — re-execute ALL 66 operations from scratch, fresh results)
```

**RE-RUN mode** (status=COMPLETE, no code changes): The previous run's results are stale evidence. Re-execute all 66 operations to produce fresh results. This catches: regressions from data state changes, transient bugs that were masked, and operations that were improperly marked PASS in previous runs.

---

## MODE: FULL INIT (first time — no existing cases)

### Phase A: Generate Test Cases (5 Parallel Agents)

Read `.claude/skills/noir-qa/SKILL.md` Phase 2 for format.
Read `.claude/skills/noir-test-flow/SKILL.md` for visual protocol.

**Spawn 5 agents simultaneously**, each writing to `.qa/cases/`:

#### Agent 1: `qa-gen-auth-settings`
```
Generate ALL test cases for AUTH + SETTINGS domains.

AUTH → .qa/cases/auth.md
  Pages: Login, Dashboard, Profile, Theme switching, Language switching

SETTINGS → .qa/cases/settings.md
  Pages: General Settings (ALL tabs), Feature Management, Users, Roles, Tenants, API Keys, Webhooks

Read source: src/NOIR.Web/frontend/src/portal-app/ for these features.
Read format: .claude/skills/noir-qa/SKILL.md § "Test Case File Format"
Read visual protocol: .claude/skills/noir-test-flow/SKILL.md

Per page generate: Happy path [P1], Edge cases [P2], Visual checks [P1-P2], Data consistency [P1], Regression [P2].
Tag critical-path cases with [smoke]. Include Visual checkboxes: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px.
Check docs/qa/ for known bug patterns in these areas → add regression cases.
```

#### Agent 2: `qa-gen-catalog-orders`
```
Generate ALL test cases for CATALOG + ORDERS domains.

CATALOG → .qa/cases/catalog.md
  Pages: Products (list + create/edit + variants + images), Product Categories (tree),
  Product Attributes (13 types, filter config), Brands, Inventory Receipts (StockIn/StockOut, confirm/cancel)

ORDERS → .qa/cases/orders.md
  Pages: Orders (list + detail + ALL status transitions: Pending→Confirmed→Processing→Shipped→Delivered→Completed + Cancel/Return with inventory),
  Payments (list + detail + transaction timeline), Shipping (providers, tracking)

Read source: src/NOIR.Web/frontend/src/portal-app/ for these features.
Read format: .claude/skills/noir-qa/SKILL.md § "Test Case File Format"
Read visual protocol: .claude/skills/noir-test-flow/SKILL.md

CRITICAL: Orders — generate cases for EVERY status transition (valid + invalid paths).
CRITICAL: Products — variant creation, image upload, Draft→Active→Archived.
CRITICAL: Inventory — StockIn confirm → quantity +, StockOut → quantity -.

Per page generate: Happy path [P1], Edge cases [P2], Visual [P1-P2], Data consistency [P1], Regression [P2].
Tag [smoke]. Visual checkboxes. Check docs/qa/.
```

#### Agent 3: `qa-gen-customers-content`
```
Generate ALL test cases for CUSTOMERS + CONTENT domains.

CUSTOMERS → .qa/cases/customers.md
  Pages: Customers (list + detail tabs: overview, orders, addresses),
  Customer Groups (CRUD, rule-based), Promotions (date range, % vs fixed, usage limits),
  Reviews (approve/reject moderation), Wishlists

CONTENT → .qa/cases/content.md
  Pages: Blog Posts (CRUD, rich text editor, image, category/tag, Draft→Published),
  Blog Categories (CRUD), Blog Tags (CRUD), Media Library (upload, preview, delete, search)

Read source: src/NOIR.Web/frontend/src/portal-app/ for these features.
Read format: .claude/skills/noir-qa/SKILL.md § "Test Case File Format"
Read visual protocol: .claude/skills/noir-test-flow/SKILL.md

CRITICAL: Customer Detail — test all tabs (order history, addresses, timeline).
CRITICAL: Promotions — date range picker, percentage vs fixed, usage limit enforcement.
CRITICAL: Blog Posts — rich text editor, featured image upload/preview, publish flow.
CRITICAL: Media Library — FilePreviewModal click-to-preview behavior.

Per page generate: Happy path [P1], Edge cases [P2], Visual [P1-P2], Data consistency [P1], Regression [P2].
Tag [smoke]. Visual checkboxes. Check docs/qa/.
```

#### Agent 4: `qa-gen-hr-crm`
```
Generate ALL test cases for HR + CRM domains.

HR → .qa/cases/hr.md
  Pages: Employees (list, create, edit, import/export CSV, tag assignment via TagSelector),
  Departments (tree view, CRUD, employee count updates),
  Employee Tags (DataTable with category grouping, color picker, 7 categories),
  HR Reports (if exists)

CRM → .qa/cases/crm.md
  Pages: Contacts (list, CRUD, link to company), Companies (list, CRUD),
  Leads/Pipeline (Kanban board, drag-drop between stages, win/lose lead),
  CRM Activities (create on contact, timeline), CRM Dashboard widgets

Read source: src/NOIR.Web/frontend/src/portal-app/ for these features.
Read format: .claude/skills/noir-qa/SKILL.md § "Test Case File Format"
Read visual protocol: .claude/skills/noir-test-flow/SKILL.md

CRITICAL: Employees — CSV import/export, tag assignment with TagSelector grouped by category.
CRITICAL: Departments — tree view drag-reorder, parent-child relationships.
CRITICAL: CRM Pipeline — Kanban drag-drop, stage transitions, win/lose lead with dashboard update.
CRITICAL: Employee Tags — category grouping in DataTable, color picker (12 presets).

Per page generate: Happy path [P1], Edge cases [P2], Visual [P1-P2], Data consistency [P1], Regression [P2].
Tag [smoke]. Visual checkboxes. Check docs/qa/.
```

#### Agent 5: `qa-gen-pm-dashboard`
```
Generate ALL test cases for PM + DASHBOARD domains.

PM → .qa/cases/pm.md
  Pages: Projects (list, CRUD, members, auto-code PRJ-xxx),
  Kanban Board (columns CRUD, task create/edit, drag-drop between columns),
  Task Detail (subtasks, comments, attachments, labels, assignees),
  Task List View (alternative to Kanban), Archived Tasks

DASHBOARD → .qa/cases/dashboard.md
  Pages: Dashboard (7 metrics via Task.WhenAll, 4 widget groups: E-commerce, CRM, feature-gated)

Read source: src/NOIR.Web/frontend/src/portal-app/ for these features.
Read format: .claude/skills/noir-qa/SKILL.md § "Test Case File Format"
Read visual protocol: .claude/skills/noir-test-flow/SKILL.md

CRITICAL: Kanban — drag-drop tasks, column CRUD, quick-add textarea.
CRITICAL: Task Detail — subtask toggle, comment timestamps, file attachment download.
CRITICAL: Dashboard — all widget groups visible/hidden per module, revenue excludes Cancelled/Refunded.
CRITICAL: Dashboard — feature-gated widgets hide when module disabled via Feature Management.

Per page generate: Happy path [P1], Edge cases [P2], Visual [P1-P2], Data consistency [P1], Regression [P2].
Tag [smoke]. Visual checkboxes. Check docs/qa/.
```

**After all 5 agents complete:**
1. Read all `.qa/cases/*.md` — verify all pages from `src/NOIR.Web/frontend/src/portal-app/` are covered (do NOT hardcode page counts — enumerate dynamically)
2. Verify reasonable P0/P1/P2/P3 distribution
3. Verify platform admin pages covered (Tenants + Feature Management platform-level, with `platform@noir.local`)
4. Proceed to **Build Flows** (below)

---

## MODE: INCREMENTAL (code changed since last run)

### Phase A: Detect What Changed

```bash
LAST_COMMIT=$(cat .qa/state.json 2>/dev/null | jq -r '.lastCheckedCommit // empty')
[ -z "$LAST_COMMIT" ] && LAST_COMMIT="HEAD~50"
git diff --name-only "$LAST_COMMIT" HEAD
```

Map changed files → affected domains using this table:

| File path pattern | Affected domain(s) |
|-------------------|-------------------|
| `portal-app/products/`, `Features/Products/` | Domain 1: Products |
| `portal-app/orders/`, `Features/Orders/` | Domain 2: Orders |
| `portal-app/customers/`, `Features/Customers/` | Domain 3: Customers |
| `portal-app/blog/`, `portal-app/media/`, `Features/Blog/` | Domain 4: Content |
| `portal-app/hr/`, `Features/Hr/` | Domain 5: HR |
| `portal-app/crm/`, `Features/Crm/` | Domain 6: CRM |
| `portal-app/projects/`, `Features/Pm/` | Domain 7: PM |
| `portal-app/admin/`, `Features/Settings/`, `Features/Users/` | Domain 8: Settings |
| `portal-app/dashboard/`, `Features/Dashboard/` | Domain 9: Dashboard |
| `Features/Tenants/`, `Modules/` | Domain 10: Platform |
| `uikit/`, `components/`, `hooks/`, `lib/` | ALL domains (shared component — full re-run) |

### Phase B: Update Test Cases for Affected Domains

For each affected domain:
1. Read the changed source files to understand what changed
2. Update `.qa/cases/{domain}.md`:
   - **New page/dialog/field** → add new test cases
   - **Changed behavior** → update existing case steps
   - **Removed feature** → mark cases `[DEPRECATED]`
   - **Bug fix** (`git log --grep="fix"`) → add `[regression]` case to verify the fix holds
3. Update the CRUD matrix operations if needed:
   - New entity type → add CREATE/EDIT/DELETE operations to that domain's table
   - New status transition → add STATUS operation
   - New linked data relationship → add LINKED DATA verification

**If 3+ domains affected** → spawn parallel agents (one per domain).
**If shared component changed** → mark ALL domains for re-execution.

### Phase C: Execute ONLY Affected Domains

- Execute the CRUD matrix operations **only for affected domains** (not all 10)
- PLUS: re-execute `[regression]`-tagged operations from previously-passing domains
- PLUS: always re-execute cross-feature linked data verification (CF-1 through CF-4)
- Record results alongside previous run's results (don't wipe passing domains)

### Phase D: Update State

```json
{
  "lastCheckedCommit": "HEAD",
  "domainsExecuted": ["domain1", "domain5"],
  "domainsCarriedForward": ["domain2", "domain3", "domain4", "domain6", "domain7", "domain8", "domain9", "domain10"]
}
```

---

## MODE: RESUME (interrupted session)

Read `.qa/state.json` → find `checkpoint` field (e.g., `"domain5-op3"`) → skip to that exact domain and operation. Do NOT re-execute completed domains/operations. Continue sequentially from checkpoint.

---

## MODE: RE-RUN (no code changes, previous run complete)

Previous results are stale. Start fresh:
1. Clear `.qa/results/latest.md`
2. Execute ALL 66 operations across ALL 10 domains from Domain 1, Operation 1.1
3. Produce completely new results
4. This catches: regressions from data state changes, transient bugs masked in previous run, operations that were improperly shallow

---

## BUILD FLOWS (all modes)

Read all `.qa/cases/*.md`. Create/update flow files in `.qa/flows/`:

### `.qa/flows/smoke.md` — P0 (~15 min)
All `[smoke]` cases. Order: auth → dashboard → one CRUD per feature.

### `.qa/flows/regression.md` — P0+P1 (~1-2h)
All P0+P1 cases by feature domain, dependencies first.

### `.qa/flows/full.md` — ALL priorities (~3-4h)
Every case including P2 edge + P3 cosmetic.

### `.qa/flows/cross-feature.md` — Linked Data Flows
8 flows from `noir-test-flow` templates, mapped to specific case IDs:
1. Product → Order → Payment Lifecycle
2. Customer Journey
3. Content Publishing
4. HR Workflow
5. CRM Pipeline
6. PM Workflow
7. Data Integrity & Edge Cases
8. Error Handling

---

## EXECUTE (all modes)

Read `.claude/skills/noir-test-flow/SKILL.md` for the complete visual testing protocol.

### Phase 0: Environment Reset (new cycle only — NOT for RESUME)

**Applies to**: FULL INIT, RE-RUN, INCREMENTAL. **Skip for**: RESUME (keep existing data state).

A fresh database ensures deterministic counts and no leftover test data from previous runs. This is the foundation for reliable CRUD verification (count N→N+1 is only meaningful on a known dataset).

```bash
# 1. Stop services
netstat -ano | grep ":4000 " | grep "LISTEN" | awk '{print $5}' | sort -u | while read pid; do taskkill //F //PID "$pid" 2>/dev/null; done
netstat -ano | grep ":3000 " | grep "LISTEN" | awk '{print $5}' | sort -u | while read pid; do taskkill //F //PID "$pid" 2>/dev/null; done
sleep 2

# 2. Drop and recreate databases (EF Core)
cd src/NOIR.Web
dotnet ef database drop --context ApplicationDbContext --force --no-build 2>/dev/null
dotnet ef database drop --context TenantStoreDbContext --force --no-build 2>/dev/null

# 3. Rebuild + apply migrations + seed
cd ../..
dotnet build src/NOIR.sln -v q
dotnet ef database update --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context TenantStoreDbContext
dotnet ef database update --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context ApplicationDbContext

# 4. Start backend (seeds run automatically on first request)
cd src/NOIR.Web
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://localhost:4000" dotnet run --no-build > ../../.backend.log 2>&1 &
sleep 5
curl -sf http://localhost:4000/robots.txt -o /dev/null && echo "Backend: OK" || echo "Backend: FAILED"

# 5. Start frontend
cd ../..
powershell -Command "Start-Process cmd -ArgumentList '/c cd /d src\NOIR.Web\frontend && pnpm run dev > ..\..\..\.frontend.log 2>&1'"
sleep 5
curl -sf http://localhost:3000 -o /dev/null && echo "Frontend: OK" || echo "Frontend: FAILED"
```

**After reset**: Login as `admin@noir.local` / `123qwe` → verify dashboard loads with seeded data. Record baseline counts (products, customers, orders, etc.) — these are the "N" values for CRUD verification (count N→N+1 after CREATE).

**If services are already running and mode is RESUME**: Skip Phase 0 entirely, just verify health:
```bash
BACKEND_UP=$(curl -sf http://localhost:4000/robots.txt -o /dev/null -w "%{http_code}" 2>/dev/null || echo "000")
FRONTEND_UP=$(curl -sf http://localhost:3000 -o /dev/null -w "%{http_code}" 2>/dev/null || echo "000")
```

### THE EXECUTION MODEL: Mandatory CRUD Matrix

**Every domain below has a MANDATORY operation checklist. You CANNOT move to the next domain until ALL operations for the current domain are completed and recorded. There is NO "depth over breadth" escape hatch — ALL domains MUST be tested with full CRUD.**

**If context limits force a session break**: save state with exact domain + operation checkpoint. Resume picks up at the exact operation, not the next domain.

**Execution order**: Domain 1 → Domain 2 → ... → Domain 10 → Cross-Feature Flows → Visual Batch → Cleanup

---

### DOMAIN 1: Products (Catalog)

| # | Operation | Steps | Evidence Required | PASS criteria |
|---|-----------|-------|-------------------|---------------|
| 1.1 | **LIST** | Navigate → verify DataTable columns + stat cards + count | Screenshot of list | Columns render, count matches stat cards |
| 1.2 | **SEARCH** | Type product name in search → verify filtered results → clear | Screenshot of filtered results | Only matching products shown, count updates |
| 1.3 | **CREATE** | Click "Sản phẩm mới" → fill name, slug, description, SKU, price, category, brand → Save | Screenshots: (1) filled form, (2) saved product in edit mode | Product saved, redirected to edit page, status "Bản nháp" |
| 1.4 | **ADD VARIANT** | On edit page → scroll to Variants → fill name, SKU, price, stock → Add | Screenshot of variant in table | Variant appears in variants table with correct data |
| 1.5 | **STATUS: Publish** | Click "Xuất bản" → verify status badge changes | Screenshot showing "Đang bán" badge | Status changes from Draft to Active |
| 1.6 | **LINKED DATA** | Navigate to Products list → verify count incremented (N→N+1) → Navigate to Dashboard → verify "Sản phẩm nháp" count decreased | Screenshots: (1) product list with new count, (2) dashboard | List count +1, dashboard draft count correct |
| 1.7 | **EDIT** | Open product via actions menu → change name + price → Save → verify in list | Screenshots: (1) edit form, (2) list showing updated values | Changed fields persisted |
| 1.8 | **DELETE** | Click actions ⋮ → Delete → confirm dialog → confirm → verify removed from list, count decremented | Screenshots: (1) confirm dialog, (2) list with count N→N-1 | Entity removed, count updated |

### DOMAIN 2: Orders

| # | Operation | Steps | Evidence Required | PASS criteria |
|---|-----------|-------|-------------------|---------------|
| 2.1 | **LIST** | Navigate → verify DataTable + status badges + filter dropdown | Screenshot of list | 9 orders with correct status badges |
| 2.2 | **FILTER** | Select status filter (e.g., "Đã xác nhận") → verify filtered | Screenshot of filtered results | Only matching orders shown |
| 2.3 | **DETAIL** | Click order row → verify: timeline, products table, customer info, payment info, shipping, notes, actions | Full-page screenshot | All sections render with correct data |
| 2.4 | **STATUS: Ship** | On Confirmed order → click "Giao hàng" → fill tracking + carrier → submit | Screenshots: (1) ship dialog, (2) updated status + tracking info | Status changes, tracking displayed, timeline updated |
| 2.5 | **STATUS: Deliver** | On Shipping order → click "Đánh dấu đã giao" → confirm | Screenshot of delivered status | Status changes, timeline updated |
| 2.6 | **ADD NOTE** | Type internal note → click "Thêm ghi chú" → verify note appears | Screenshot of note in notes section | Note appears with timestamp |
| 2.7 | **LINKED DATA** | Navigate to Dashboard → verify order status metrics updated (badge counts reflect transitions) | Screenshot of dashboard order metrics | Counts match actual statuses |

### DOMAIN 3: Customers

| # | Operation | Steps | Evidence Required | PASS criteria |
|---|-----------|-------|-------------------|---------------|
| 3.1 | **LIST** | Navigate → verify stat cards + DataTable + segment/tier columns | Screenshot of list | Count matches stat card, columns render |
| 3.2 | **CREATE** | Click "Khách hàng mới" → fill first name, last name, email, phone → submit | Screenshots: (1) filled dialog, (2) list with new customer | Customer in list, count N→N+1 |
| 3.3 | **DETAIL** | Click customer row → verify tabs: Overview, Orders, Addresses | Screenshot of detail page with tabs | All tabs render, customer info correct |
| 3.4 | **EDIT** | Click actions ⋮ → Edit → change 2+ fields → save → verify in list | Screenshots: (1) edit dialog, (2) list with updated values | Changes persisted |
| 3.5 | **DELETE** | Click actions ⋮ → Delete → confirm → verify removed, count decremented | Screenshots: (1) confirm dialog, (2) list with count N→N-1 | Removed, count updated |
| 3.6 | **SEARCH** | Search by name → verify filter → clear | Screenshot of search results | Correct filtering |
| 3.7 | **LINKED DATA** | Navigate to Dashboard → verify "Tổng khách hàng" matches actual count | Screenshot of dashboard customer widget | Count matches |

### DOMAIN 4: Content (Blog)

| # | Operation | Steps | Evidence Required | PASS criteria |
|---|-----------|-------|-------------------|---------------|
| 4.1 | **LIST** | Navigate to Blog Posts → verify DataTable + thumbnails + status badges | Screenshot of list | Posts with correct statuses, categories |
| 4.2 | **CREATE POST** | Click "Bài viết mới" → fill title, category, Tiptap editor content → Save as Draft | Screenshots: (1) form with Tiptap, (2) saved post | Post saved, status "Bản nháp" |
| 4.3 | **PUBLISH** | On edit page → click Publish → verify status changes | Screenshot of published status | "Đã xuất bản" badge |
| 4.4 | **EDIT** | Open post → change title → save → verify in list | Screenshot of updated list | Title updated in list |
| 4.5 | **DELETE** | Delete post → confirm → verify removed from list | Screenshot of list with post gone | Removed, count updated |
| 4.6 | **BLOG TAGS** | Navigate to Blog Tags → Create tag → verify in list → Delete tag | Screenshot of tag list | CRUD works |
| 4.7 | **BLOG CATEGORIES** | Navigate to Blog Categories → verify tree/table renders | Screenshot of categories | Categories display |

### DOMAIN 5: HR

| # | Operation | Steps | Evidence Required | PASS criteria |
|---|-----------|-------|-------------------|---------------|
| 5.1 | **EMPLOYEES LIST** | Navigate → verify DataTable + dept/position/status columns | Screenshot of list | All columns render |
| 5.2 | **CREATE EMPLOYEE** | Click "Tạo nhân viên" → fill name, email, department, position → submit | Screenshots: (1) filled form, (2) list with new employee | Employee in list, auto-code generated |
| 5.3 | **EDIT EMPLOYEE** | Open edit → change position → save → verify | Screenshot of updated list | Changes persisted |
| 5.4 | **DELETE EMPLOYEE** | Delete → confirm → verify count | Screenshot of list with employee gone | Removed, count updated |
| 5.5 | **DEPARTMENTS** | Navigate → verify tree view → Create department → verify in tree | Screenshots: (1) tree, (2) new dept in tree | Tree renders, new dept appears |
| 5.6 | **EMPLOYEE TAGS** | Navigate → verify DataTable with category grouping → Create tag with color | Screenshot of tags with grouping | Grouping works, color picker works |
| 5.7 | **LINKED DATA** | After creating employee in dept → navigate to Departments → verify employee count on that department | Screenshot of dept with count | Count reflects new employee |

### DOMAIN 6: CRM

| # | Operation | Steps | Evidence Required | PASS criteria |
|---|-----------|-------|-------------------|---------------|
| 6.1 | **CONTACTS LIST** | Navigate → verify DataTable | Screenshot of list | Contacts render |
| 6.2 | **CREATE CONTACT** | Create via dialog → fill name, email, phone → submit | Screenshots: (1) dialog, (2) list | Contact in list |
| 6.3 | **COMPANIES** | Navigate → verify list → Create company → verify | Screenshot of company in list | CRUD works |
| 6.4 | **PIPELINE KANBAN** | Navigate → verify columns render with deals | Screenshot of Kanban | Columns + deal cards visible |
| 6.5 | **CREATE DEAL** | Click "Tạo Deal" → fill name, value, contact → submit → verify on Kanban | Screenshot of deal on board | Deal appears in first stage |
| 6.6 | **EDIT CONTACT** | Edit contact → change fields → save → verify | Screenshot of updated contact | Changes persisted |
| 6.7 | **DELETE CONTACT** | Delete contact → confirm → verify removed | Screenshot of list | Removed, count updated |
| 6.8 | **LINKED DATA** | Navigate to Dashboard → verify CRM widgets (contact count, pipeline value) | Screenshot of CRM dashboard widgets | Counts match |

### DOMAIN 7: PM (Project Management)

| # | Operation | Steps | Evidence Required | PASS criteria |
|---|-----------|-------|-------------------|---------------|
| 7.1 | **PROJECTS** | Navigate → verify grid/list with project cards | Screenshot of projects | Cards render with status |
| 7.2 | **KANBAN BOARD** | Click project → verify columns + task cards | Screenshot of Kanban | Columns and tasks visible |
| 7.3 | **CREATE TASK** | Click "Thêm thẻ" (quick-add) → type title → Enter → verify card appears | Screenshot of new task on board | Task appears in column |
| 7.4 | **TASK DETAIL** | Click task → verify detail modal (description, subtasks, comments, labels) | Screenshot of detail modal | All sections render |
| 7.5 | **ADD COMMENT** | In task detail → type comment → submit → verify appears with timestamp | Screenshot of comment | Comment with timestamp |
| 7.6 | **ADD SUBTASK** | In task detail → add subtask → verify appears | Screenshot of subtask | Subtask in list |
| 7.7 | **DELETE TASK** | Delete task → confirm → verify removed from board | Screenshot of board without task | Removed from column |

### DOMAIN 8: Settings & Users

| # | Operation | Steps | Evidence Required | PASS criteria |
|---|-----------|-------|-------------------|---------------|
| 8.1 | **SETTINGS TABS** | Navigate to Tenant Settings → click through ALL tabs (Branding, Regional, SMTP, Email Templates, Modules, etc.) → verify each loads | Screenshot of 3+ tabs | All tabs accessible, URL-synced |
| 8.2 | **USERS LIST** | Navigate → verify DataTable | Screenshot of users | Users render with roles/status |
| 8.3 | **ROLES LIST** | Navigate → verify roles with permissions | Screenshot of roles | Roles render |
| 8.4 | **CREATE ROLE** | Create role with 3+ permissions → verify in list | Screenshots: (1) create dialog with PermissionPicker, (2) role in list | Role created with permissions |
| 8.5 | **EDIT ROLE** | Edit role → change permissions → save → verify | Screenshot of updated role | Permissions updated |
| 8.6 | **DELETE ROLE** | Delete role → confirm → verify removed | Screenshot of list | Removed |

### DOMAIN 9: Dashboard & Reports

| # | Operation | Steps | Evidence Required | PASS criteria |
|---|-----------|-------|-------------------|---------------|
| 9.1 | **DASHBOARD** | Navigate → verify ALL widget groups: Quick Actions, Activity Timeline, Revenue, Orders, Customers, Products, Content, Inventory, CRM | Full-page screenshot | All widgets render with data |
| 9.2 | **WIDGET DATA** | Cross-check: revenue total, order count, customer count, product count against respective list pages | Record counts from dashboard vs list pages | Numbers match |
| 9.3 | **ACTIVITY TIMELINE** | Verify recent actions from this QA session appear (creates, edits, deletes) | Screenshot of timeline | Our actions visible |
| 9.4 | **REPORTS** | Navigate to Reports → verify Revenue tab loads with chart | Screenshot of reports | Charts render |

### DOMAIN 10: Platform Admin

| # | Operation | Steps | Evidence Required | PASS criteria |
|---|-----------|-------|-------------------|---------------|
| 10.1 | **LOGIN** | Logout → login as platform@noir.local → verify platform UI | Screenshot of platform dashboard | Platform admin UI loads |
| 10.2 | **TENANTS** | Navigate to Tenants → verify list | Screenshot of tenants | Tenants render |
| 10.3 | **FEATURE MGMT** | Navigate to Feature Management → toggle a module off → verify sidebar hides feature → toggle back on | Screenshots: (1) module off, (2) sidebar without feature, (3) module on | Module toggle works |
| 10.4 | **RE-LOGIN** | Logout → login as admin@noir.local to restore tenant admin session | Verify dashboard loads | Back to tenant admin |

---

### CROSS-FEATURE LINKED DATA VERIFICATION

After ALL 10 domains are complete, execute these cross-checks:

| # | Flow | Steps | PASS criteria |
|---|------|-------|---------------|
| CF-1 | **Dashboard ↔ Lists** | Compare dashboard counts with actual list totals for: products, orders, customers, blog posts | All counts match |
| CF-2 | **Order ↔ Customer** | Navigate to customer who has orders → verify Orders tab shows correct orders | Order history matches |
| CF-3 | **Activity Timeline** | Navigate to Activity Timeline page → verify all CRUD actions from this session are logged with correct descriptions | Actions logged |
| CF-4 | **Language switch** | Switch to EN → verify 3 random pages → switch back to VI | No raw i18n keys, all text switches |

---

### VISUAL BATCH (after all CRUD is done)

Test 5 representative pages across dark mode + responsive:

| Page | Dark mode | 768px responsive |
|------|-----------|-----------------|
| Dashboard | Screenshot + analyze | Screenshot + analyze |
| Products list | Screenshot + analyze | Screenshot + analyze |
| Order detail | Screenshot + analyze | Screenshot + analyze |
| CRM Pipeline | Screenshot + analyze | Screenshot + analyze |
| Settings | Screenshot + analyze | Screenshot + analyze |

**Per screenshot**: verify no contrast issues, no overflow, no broken layouts, no hardcoded colors.

---

### GATE: Domain Completion Checklist

**Before moving to the next domain, verify ALL of these for the current domain:**

```
☐ CREATE executed — entity actually created, visible in list, count verified
☐ EDIT executed — 2+ fields changed, changes persisted after reload
☐ DELETE executed — entity removed, count decremented, confirmation dialog appeared
☐ SEARCH tested — search input filters correctly, clear resets
☐ LINKED DATA verified — at least 1 cross-entity check (dashboard count, related entity, timeline)
☐ Results recorded in .qa/results/latest.md with action descriptions (not "page loads")
```

**If ANY checkbox is unchecked → you are NOT done with this domain. Go back and complete it.**

---

### Per-Case Recording

For each operation, record in `.qa/results/latest.md`:
```markdown
| 1.3 | CREATE Product | ✅ PASS | Created "QA Test Product" (SKU: QA-001, 350000đ), verified in list, count 17→18 |
```
The Notes column MUST describe what you DID: entity name, field values, count changes. "Page loads correctly" = AUTOMATIC FAIL of the recording.

### Anti-Patterns (HARD RULES — violating any = invalid QA run)

- ❌ Navigate to page → screenshot → "PASS" (this is NOT testing)
- ❌ Skip CREATE/EDIT/DELETE for ANY domain (all 10 domains need full CRUD)
- ❌ Mark CRUD as PASS without actually mutating data
- ❌ Skip linked data verification (dashboard counts, related entity counts)
- ❌ Move to next domain with incomplete checklist
- ❌ Use "depth over breadth" as excuse to skip domains (ALL domains are mandatory)
- ❌ Record "page loads" as action description

### Context Management

If approaching context limits:
1. Save `.qa/state.json` with exact domain + operation number (e.g., `"checkpoint": "domain5-op3"`)
2. Save `.qa/results/latest.md` with all completed operations
3. Commit all work
4. Tell user: **"Progress saved at Domain [N], Operation [M]. Run `/noir-qa-run` to resume. [X] domains remaining, [Y] operations remaining."**

**On resume**: Read state.json → skip to exact operation → continue. Do NOT re-execute completed operations.

---

## FIX EVERY BUG

For every FAIL in `.qa/results/latest.md`:

1. Sort by severity: CRITICAL → HIGH → MEDIUM → LOW
2. For each bug:
   a. Analyze root cause (read source code)
   b. Apply minimal fix (noir-test-flow bug fix workflow)
   c. Restart if needed (HMR for frontend, dotnet watch for backend)
   d. Re-execute the EXACT failing test case
   e. Screenshot before + after
   f. Update result: PASS → `FIXED+VERIFIED`, still FAIL → try different fix
3. If fix touches shared component → flag regression on affected pages
4. Commit per feature (not per bug):
   ```bash
   git add [specific files]
   git commit -m "$(cat <<'EOF'
   fix(qa): [feature] — [N] bugs fixed

   - BUG-001: [description]
   - BUG-002: [description]

   QA: NOIR QA Pipeline

   Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
   EOF
   )"
   ```

---

## RE-EXECUTE LOOP (until zero)

```
LOOP:
  1. Re-execute previously-failed cases
  2. Any still fail? → fix → go to LOOP
  3. Shared component fix? → regression check affected pages → any fail? → fix → LOOP
  4. 0 CRITICAL + 0 HIGH? → EXIT LOOP
  5. MEDIUM/LOW: fix if quick (<5 min), otherwise document deferral reason
```

**This loop is THE quality gate. Do not exit until zero CRITICAL + zero HIGH.**

---

## FINAL VERIFICATION

1. **Full build gate** (all 4 must pass with zero errors):
   ```bash
   cd src/NOIR.Web/frontend && pnpm run build
   cd src/NOIR.Web/frontend && pnpm build-storybook
   dotnet build src/NOIR.sln
   dotnet test src/NOIR.sln
   ```

2. **5 random page spot-checks** — screenshot + 30s visual scan

3. **Update state**:
   ```json
   { "status": "COMPLETE", "lastCheckedCommit": "HEAD", "completedAt": "..." }
   ```

4. **Summary to user**:
   - Total cases: executed / passed / failed / fixed
   - Bugs by severity: CRITICAL / HIGH / MEDIUM / LOW
   - Commits made
   - High-risk areas
   - Recommendation: areas needing extra attention next time

5. **Lessons learned** — if bugs ≥ 3, create `docs/qa/real-qa-lessons-{date}.md`

---

## App Context

| Item | Value |
|------|-------|
| Frontend | http://localhost:3000 |
| Backend | http://localhost:4000 |
| Tenant Admin | admin@noir.local / 123qwe |
| Platform Admin | platform@noir.local / 123qwe |
| Pages | Dynamically discovered from `src/NOIR.Web/frontend/src/portal-app/` — currently ~43 across 10 domains |
| Stack | React 19 + TanStack Table + shadcn/ui + Tailwind CSS 4 |
| i18n | English + Vietnamese |
| Theme | Light + Dark |
| Design standards | docs/frontend/design-standards.md, .claude/rules/ |
| Known bugs | docs/qa/ |

---

## Rules

### Execution Rules (HARD — violating = invalid run)

1. **ALL 10 DOMAINS MANDATORY** — You MUST execute CREATE + EDIT + DELETE + SEARCH + LINKED DATA for every domain. No domain may be skipped. No "depth over breadth" escape. If context runs out, save checkpoint and resume — do NOT declare "complete" with missing domains.
2. **INTERACT, DON'T JUST LOOK** — Every CRUD operation MUST actually mutate data. A screenshot of a page loading is NOT a test. If you haven't filled a form, clicked submit, and verified the data changed, you haven't tested anything.
3. **VERIFY LINKED DATA after every mutation** — After CREATE/EDIT/DELETE, navigate to at least 1 related page (dashboard, parent entity, detail page) and verify counts/data updated. This catches real bugs that single-page testing misses.
4. **COMPLETE DOMAIN GATE before moving on** — All 6 checkboxes (CREATE, EDIT, DELETE, SEARCH, LINKED DATA, RECORDED) must be checked before starting the next domain. Go back if anything is missed.
5. **RECORD ACTIONS, NOT OBSERVATIONS** — "Page loads correctly" = invalid. "Created 'QA Customer' (email: qa@test.com), verified in list row 1, count 13→14, dashboard count matches" = valid.

### Process Rules

6. **NEVER write Playwright scripts** — use `mcp__playwright__*` MCP tools directly with AI reasoning. The value is AI visual analysis, not scripted automation.
7. **AUTONOMOUS** — do not ask user questions. Make best judgment, document decisions.
8. **ZERO tolerance** for CRITICAL/HIGH — fix-retest loop until zero.
9. **Commit incrementally** — per feature, never batch at end.
10. **Session-safe** — save state with exact domain + operation checkpoint. Resume picks up at exact operation.
11. **No scope creep** — fix bugs, don't refactor. Minimal changes only.
12. **Agent teams** — use parallel agents when generating/updating 3+ feature domains.
13. **Clean up test data** — Delete test entities after testing (last domain before visual batch).
