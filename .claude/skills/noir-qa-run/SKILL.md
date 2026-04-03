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
  YES → MODE: FULL INIT (generate all cases + execute ALL operations)

git diff from lastCheckedCommit to HEAD has changes?
  YES → MODE: INCREMENTAL (update cases for changed domains → execute ONLY affected domains + regression)
  NO  → MODE: RE-RUN (no code changed — re-execute ALL operations from scratch, fresh results)
```

**RE-RUN mode** (status=COMPLETE, no code changes): The previous run's results are stale evidence. Re-execute all operations to produce fresh results. This catches: regressions from data state changes, transient bugs that were masked, and operations that were improperly marked PASS in previous runs.

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
4. Proceed to **EXECUTE**

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
| `portal-app/products/`, `Features/Products/`, `Features/ProductAttributes/` | Domain 1: Products |
| `portal-app/orders/`, `Features/Orders/`, `Features/Payments/`, `Features/Shipping/` | Domain 2: Orders |
| `portal-app/customers/`, `Features/Customers/`, `Features/CustomerGroups/`, `Features/Promotions/`, `Features/Reviews/` | Domain 3: Customers |
| `portal-app/blog/`, `portal-app/media/`, `Features/Blog/` | Domain 4: Content |
| `portal-app/hr/`, `Features/Hr/` | Domain 5: HR |
| `portal-app/crm/`, `Features/Crm/` | Domain 6: CRM |
| `portal-app/projects/`, `Features/Pm/` | Domain 7: PM |
| `portal-app/admin/`, `Features/Settings/`, `Features/Users/`, `Features/Roles/` | Domain 8: Settings |
| `portal-app/dashboard/`, `Features/Dashboard/`, `Features/Reports/` | Domain 9: Dashboard |
| `Features/Tenants/`, `Modules/`, `Features/Webhooks/` | Domain 10: Platform |
| `uikit/`, `components/`, `hooks/`, `lib/`, `layouts/`, `contexts/` | ALL domains (shared — full re-run) |

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
- PLUS: always re-execute cross-feature linked data verification (CF-1 through CF-7)
- Record results alongside previous run's results (don't wipe passing domains)

### Phase D: Update State

```json
{
  "status": "COMPLETE",
  "lastCheckedCommit": "<current HEAD>",
  "completedAt": "<ISO timestamp>",
  "mode": "INCREMENTAL",
  "domainsExecuted": ["domain1", "domain5"],
  "domainsCarriedForward": ["domain2", "domain3", "domain4", "domain6", "domain7", "domain8", "domain9", "domain10"],
  "checkpoint": null
}
```

---

## MODE: RESUME (interrupted session)

1. Read `.qa/state.json` → find `checkpoint` field (e.g., `"domain5-op3"`)
2. **Verify data state**: Navigate to the last completed domain's list page → confirm test entities from earlier domains still exist (e.g., "QA Test Product" still in products list). If missing, the DB was reset between sessions → switch to RE-RUN mode instead.
3. Skip to the checkpoint domain and operation. Do NOT re-execute completed operations.
4. Continue sequentially from checkpoint.

---

## MODE: RE-RUN (no code changes, previous run complete)

Previous results are stale. Start fresh:
1. Execute Phase 0 (Environment Reset) — fresh DB
2. Clear `.qa/results/latest.md`
3. Execute ALL operations across ALL 10 domains from Domain 1, Operation 1.1
4. Produce completely new results

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

# 2. Drop and recreate databases (EF Core — must specify --project and --startup-project)
dotnet ef database drop --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context ApplicationDbContext --force 2>/dev/null
dotnet ef database drop --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context TenantStoreDbContext --force 2>/dev/null

# 3. Rebuild + apply migrations (TenantStore first, then App)
dotnet build src/NOIR.sln -v q
dotnet ef database update --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context TenantStoreDbContext
dotnet ef database update --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context ApplicationDbContext

# 4. Start backend (seeds run on first request via DatabaseInitializerHostedService)
powershell -Command "Start-Process cmd -ArgumentList '/c cd /d src\NOIR.Web && dotnet run --no-build > ..\..\..backend.log 2>&1'"
sleep 8
curl -sf http://localhost:4000/robots.txt -o /dev/null && echo "Backend: OK" || echo "Backend: FAILED"

# 5. Start frontend
powershell -Command "Start-Process cmd -ArgumentList '/c cd /d src\NOIR.Web\frontend && pnpm run dev > ..\..\..\.frontend.log 2>&1'"
sleep 5
curl -sf http://localhost:3000 -o /dev/null && echo "Frontend: OK" || echo "Frontend: FAILED"
```

### Phase 0.5: Record Baseline Counts

**After services start, login as `admin@noir.local` / `123qwe` and record seed data counts.** These are the "N" values for CRUD verification.

Navigate to each list page and record counts in `.qa/state.json` under `baseline`:

```json
{
  "status": "IN_PROGRESS",
  "mode": "RE-RUN",
  "startedAt": "<ISO timestamp>",
  "lastCheckedCommit": "<current HEAD>",
  "checkpoint": null,
  "baseline": {
    "products": 17,
    "orders": 9,
    "customers": 13,
    "blogPosts": 10,
    "employees": 6,
    "contacts": 1,
    "companies": 0,
    "projects": 1,
    "users": 1,
    "roles": 1
  }
}
```

**Do NOT hardcode these numbers** — navigate to each page and read the actual count from the DataTable "Showing X of Y" text or stat cards. The seed data may change between versions.

**If services are already running and mode is RESUME**: Skip Phase 0 and 0.5, just verify health:
```bash
curl -sf http://localhost:4000/robots.txt -o /dev/null && echo "Backend: OK" || echo "Backend: FAILED"
curl -sf http://localhost:3000 -o /dev/null && echo "Frontend: OK" || echo "Frontend: FAILED"
```

---

### THE EXECUTION MODEL: Mandatory CRUD Matrix

**Every domain below has a MANDATORY operation checklist. You CANNOT move to the next domain until ALL operations for the current domain are completed and recorded. There is NO escape hatch — ALL domains MUST be tested with full CRUD.**

**If context limits force a session break**: save state with exact domain + operation checkpoint. Resume picks up at the exact operation, not the next domain.

**Execution order**: Domain 1 → Domain 2 → ... → Domain 10 → Cross-Feature Flows → Visual Batch → Error Handling → Cleanup

---

### DOMAIN 1: Products (Catalog)

| # | Operation | Steps | Evidence Required | PASS criteria |
|---|-----------|-------|-------------------|---------------|
| 1.1 | **LIST** | Navigate → verify DataTable columns + stat cards + count → record baseline N | Screenshot of list | Columns render, count matches stat cards, N recorded |
| 1.2 | **SEARCH** | Type product name in search → verify filtered results → clear | Screenshot of filtered results | Only matching products shown, count updates |
| 1.3 | **CREATE** | Click "Sản phẩm mới" → fill name, slug, description, SKU, price, category, brand → Save | Screenshots: (1) filled form, (2) saved product in edit mode | Product saved, redirected to edit page, status "Bản nháp" |
| 1.4 | **FORM VALIDATION** | On create page → click Save with empty name → verify inline error on name field (not toast) → fill name → error clears | Screenshot of inline error | Inline error appears under field, not as toast |
| 1.5 | **ADD VARIANT** | On edit page → scroll to Variants → fill name, SKU, price, stock → Add | Screenshot of variant in table | Variant appears in variants table with correct data |
| 1.6 | **STATUS: Publish** | Click "Xuất bản" → verify status badge changes | Screenshot showing "Đang bán" badge | Status changes from Draft to Active |
| 1.7 | **LINKED DATA** | Navigate to Products list → verify count N→N+1 → Navigate to Dashboard → verify stat cards updated | Screenshots: (1) product list with new count, (2) dashboard | List count +1, dashboard reflects change |
| 1.8 | **EDIT** | Open product via actions menu → change name + price → Save → verify in list | Screenshots: (1) edit form, (2) list showing updated values | Changed fields persisted |
| 1.9 | **DELETE** | Click actions ⋮ → Delete → confirm dialog → confirm → verify removed from list, count N+1→N | Screenshots: (1) confirm dialog, (2) list with original count N | Entity removed, count restored |
| 1.10 | **BRANDS** | Navigate to Brands → Create brand → verify in list → Delete brand → verify removed | Screenshot of brand CRUD | Brand CRUD works |
| 1.11 | **CATEGORIES** | Navigate to Product Categories → verify tree view renders → Create category → verify in tree | Screenshot of tree with new category | Tree renders, new category appears |

### DOMAIN 2: Orders

| # | Operation | Steps | Evidence Required | PASS criteria |
|---|-----------|-------|-------------------|---------------|
| 2.1 | **LIST** | Navigate → verify DataTable + status badges + filter dropdown → record baseline N | Screenshot of list | Orders with correct status badges, N recorded |
| 2.2 | **FILTER** | Select status filter (e.g., "Đã xác nhận") → verify filtered → clear filter | Screenshot of filtered results | Only matching orders shown |
| 2.3 | **DETAIL** | Click order row → verify: timeline, products table, customer info, payment info, shipping, notes, actions | Full-page screenshot | All sections render with correct data |
| 2.4 | **STATUS: Ship** | On Confirmed order → click "Giao hàng" → fill tracking + carrier → submit | Screenshots: (1) ship dialog filled, (2) updated status + tracking info | Status changes, tracking displayed, timeline step activated |
| 2.5 | **STATUS: Deliver** | On Shipping order → click "Đánh dấu đã giao" → confirm | Screenshot of delivered status | Status changes to "Đã giao", timeline updated |
| 2.6 | **STATUS: Complete** | On Delivered order → click "Hoàn thành" → confirm | Screenshot of completed status | Status changes to "Hoàn thành", all timeline steps filled |
| 2.7 | **ADD NOTE** | Type internal note → click "Thêm ghi chú" → verify note appears | Screenshot of note in notes section | Note appears with timestamp and author |
| 2.8 | **PAYMENTS** | Navigate to Payments list → verify DataTable → click payment → verify detail with timeline | Screenshot of payment detail | Payment detail renders with transaction info |
| 2.9 | **LINKED DATA** | Navigate to Dashboard → verify order metrics updated (badge counts reflect transitions from 2.4-2.6) | Screenshot of dashboard order metrics widget | Counts match actual order statuses |

### DOMAIN 3: Customers

| # | Operation | Steps | Evidence Required | PASS criteria |
|---|-----------|-------|-------------------|---------------|
| 3.1 | **LIST** | Navigate → verify stat cards + DataTable + segment/tier columns → record baseline N | Screenshot of list | Count matches stat card, N recorded |
| 3.2 | **CREATE** | Click "Khách hàng mới" → fill first name, last name, email, phone → submit | Screenshots: (1) filled dialog, (2) list with new customer | Customer in list, count N→N+1 |
| 3.3 | **DETAIL** | Click customer row → verify tabs: Overview, Orders, Addresses → click each tab | Screenshot of detail page showing each tab | All tabs render, customer info correct, URL syncs with tab |
| 3.4 | **EDIT** | Click actions ⋮ → Edit → change 2+ fields → save → verify in list | Screenshots: (1) edit dialog, (2) list with updated values | Changes persisted |
| 3.5 | **DELETE** | Click actions ⋮ → Delete → confirm → verify removed, count N+1→N | Screenshots: (1) confirm dialog, (2) list restored to count N | Removed, count restored |
| 3.6 | **SEARCH** | Search by name → verify filter → clear | Screenshot of search results | Correct filtering |
| 3.7 | **CUSTOMER GROUPS** | Navigate → Create group → verify in list → Delete group | Screenshot of group CRUD | Group CRUD works |
| 3.8 | **PROMOTIONS** | Navigate → Create promotion (date range + % discount) → verify in list → Activate → verify status | Screenshot of promotion with status | Promotion lifecycle works |
| 3.9 | **REVIEWS** | Navigate → verify list with tab filters (Pending/Approved/Rejected) → Approve a pending review | Screenshot of review moderation | Review status changes |
| 3.10 | **LINKED DATA** | Navigate to Dashboard → verify "Tổng khách hàng" matches actual count | Screenshot of dashboard customer widget | Count matches |

### DOMAIN 4: Content (Blog + Media)

| # | Operation | Steps | Evidence Required | PASS criteria |
|---|-----------|-------|-------------------|---------------|
| 4.1 | **POSTS LIST** | Navigate to Blog Posts → verify DataTable + thumbnails + status badges → record baseline N | Screenshot of list | Posts with correct statuses, categories, N recorded |
| 4.2 | **CREATE POST** | Click "Bài viết mới" → fill title, select category, type in Tiptap editor → Save as Draft | Screenshots: (1) form with Tiptap content, (2) saved post | Post saved, status "Bản nháp" |
| 4.3 | **TIPTAP EDITOR** | In Tiptap: type text → bold selection → add bullet list → verify toolbar formatting works | Screenshot of formatted content | Bold, list render correctly in editor |
| 4.4 | **PUBLISH** | On edit page → set status to Published → Save → verify status badge changes | Screenshot of published status | "Đã xuất bản" badge |
| 4.5 | **EDIT** | Open post → change title → save → verify in list | Screenshot of updated list | Title updated in list |
| 4.6 | **DELETE** | Delete post → confirm → verify removed from list, count N+1→N | Screenshot of list with post gone | Removed, count restored |
| 4.7 | **BLOG TAGS** | Navigate to Blog Tags → Create tag → verify in list → Delete tag → verify removed | Screenshot of tag list before/after | Tag CRUD works |
| 4.8 | **BLOG CATEGORIES** | Navigate → Create category → verify in tree/table → Delete → verify removed | Screenshot of categories before/after | Category CRUD works |
| 4.9 | **MEDIA LIBRARY** | Navigate → verify grid view → Upload file via dialog → verify appears → click for preview → Delete → verify removed | Screenshots: (1) upload dialog, (2) file in grid, (3) preview | Media upload + preview + delete works |
| 4.10 | **LINKED DATA** | Navigate to Dashboard → verify content stats (published/draft counts) match blog posts list | Screenshot of dashboard content widgets | Counts match |
| 4.11 | **i18n SPOT CHECK** | Switch to Vietnamese on create post dialog → verify all labels translated → switch back | Screenshot of Vietnamese dialog | No English labels, no raw i18n keys |

### DOMAIN 5: HR

| # | Operation | Steps | Evidence Required | PASS criteria |
|---|-----------|-------|-------------------|---------------|
| 5.1 | **EMPLOYEES LIST** | Navigate → verify DataTable + dept/position/status columns → record baseline N | Screenshot of list | All columns render, N recorded |
| 5.2 | **CREATE EMPLOYEE** | Click "Tạo nhân viên" → fill name, email, department, position → submit | Screenshots: (1) filled form, (2) list with new employee | Employee in list with auto-code EMP-xxx, count N→N+1 |
| 5.3 | **EDIT EMPLOYEE** | Open edit → change position + department → save → verify in list | Screenshot of updated list | Changes persisted |
| 5.4 | **DELETE EMPLOYEE** | Delete → confirm → verify count N+1→N | Screenshot of list with employee gone | Removed, count restored |
| 5.5 | **DEPARTMENTS** | Navigate → verify tree view → Create department → verify in tree → verify employee count | Screenshots: (1) tree, (2) new dept in tree | Tree renders, new dept appears |
| 5.6 | **EMPLOYEE TAGS** | Navigate → verify DataTable with category grouping → Create tag with color picker → verify in list | Screenshot of tags with grouping visible | Grouping works, color dot matches selected color |
| 5.7 | **ORG CHART** | Navigate to Org Chart → verify ReactFlow renders with employee nodes | Screenshot of org chart | Chart renders with connected nodes |
| 5.8 | **LINKED DATA** | After creating employee in dept → navigate to Departments → verify employee count incremented on that department node | Screenshot of dept with updated count | Count reflects new employee |
| 5.9 | **i18n SPOT CHECK** | On create employee dialog → switch to Vietnamese → verify labels → switch back | Screenshot of Vietnamese dialog | All labels translated |

### DOMAIN 6: CRM

| # | Operation | Steps | Evidence Required | PASS criteria |
|---|-----------|-------|-------------------|---------------|
| 6.1 | **CONTACTS LIST** | Navigate → verify DataTable → record baseline N | Screenshot of list | Contacts render, N recorded |
| 6.2 | **CREATE CONTACT** | Create via dialog → fill name, email, phone → submit → verify count N→N+1 | Screenshots: (1) dialog, (2) list | Contact in list |
| 6.3 | **EDIT CONTACT** | Edit contact → change phone + email → save → verify in list | Screenshot of updated contact | Changes persisted |
| 6.4 | **DELETE CONTACT** | Delete → confirm → verify removed, count N+1→N | Screenshot of list | Removed, count restored |
| 6.5 | **COMPANIES** | Navigate → Create company → verify in list → Delete → verify removed | Screenshot of company CRUD | Company CRUD works |
| 6.6 | **PIPELINE KANBAN** | Navigate → verify all columns render with stage names and deal counts | Screenshot of Kanban | Columns + deal cards visible |
| 6.7 | **CREATE DEAL** | Click "Tạo Deal" → fill name, value, contact → submit → verify card appears in first stage | Screenshot of deal on board | Deal appears in correct column |
| 6.8 | **DRAG DEAL** | Drag deal card from one stage to next → verify card moves → verify stage counts update | Screenshots: (1) before drag, (2) after drag with updated counts | Deal moves, counts change |
| 6.9 | **CONTACT DETAIL** | Click contact row → verify detail page with tabs (Activities, Deals, etc.) | Screenshot of contact detail | All tabs render |
| 6.10 | **LINKED DATA** | Navigate to Dashboard → verify CRM widgets (contact count, pipeline value) match actual data | Screenshot of CRM dashboard widgets | Counts match |

### DOMAIN 7: PM (Project Management)

| # | Operation | Steps | Evidence Required | PASS criteria |
|---|-----------|-------|-------------------|---------------|
| 7.1 | **PROJECTS** | Navigate → verify grid/list with project cards → record project count | Screenshot of projects | Cards render with status badge |
| 7.2 | **KANBAN BOARD** | Click project → verify columns + task cards + quick-add buttons | Screenshot of Kanban | Columns and tasks visible, column colors correct |
| 7.3 | **CREATE TASK** | Click "Thêm thẻ" (quick-add) → type title → Enter → verify card appears in column | Screenshot of new task on board | Task card appears with title |
| 7.4 | **DRAG TASK** | Drag task card from one column to another → verify card moves → verify column counts update | Screenshots: (1) before drag, (2) after drag | Task moves between columns |
| 7.5 | **TASK DETAIL** | Click task → verify detail modal/page (description, subtasks, comments, labels, assignees) | Screenshot of detail | All sections render |
| 7.6 | **ADD COMMENT** | In task detail → type comment → submit → verify appears with timestamp | Screenshot of comment | Comment with timestamp and author |
| 7.7 | **ADD SUBTASK** | In task detail → add subtask title → verify appears in subtask list | Screenshot of subtask | Subtask in list |
| 7.8 | **DELETE TASK** | Delete task → confirm → verify removed from board, column count decremented | Screenshot of board without task | Removed from column |
| 7.9 | **LINKED DATA** | Navigate to Projects list → verify task count on project card matches Kanban total | Screenshot of project card | Task count correct |

### DOMAIN 8: Settings & Users

| # | Operation | Steps | Evidence Required | PASS criteria |
|---|-----------|-------|-------------------|---------------|
| 8.1 | **SETTINGS TABS** | Navigate to Tenant Settings → click through ALL tabs (Branding, Regional, SMTP, Email Templates, Modules, etc.) → verify each loads with `?tab=` URL sync | Screenshots of 3+ different tabs | All tabs accessible, URL-synced |
| 8.2 | **USERS LIST** | Navigate → verify DataTable → record baseline N | Screenshot of users | Users render with roles/status, N recorded |
| 8.3 | **ROLES LIST** | Navigate → verify roles with permission counts | Screenshot of roles | Roles render |
| 8.4 | **CREATE ROLE** | Create role with 3+ permissions via PermissionPicker → verify in list | Screenshots: (1) create dialog showing PermissionPicker with categories, (2) role in list | Role created with correct permission count |
| 8.5 | **EDIT ROLE** | Edit role → add/remove permissions → save → verify count updated | Screenshot of updated role | Permissions updated |
| 8.6 | **DELETE ROLE** | Delete role → confirm → verify removed from list | Screenshot of list without role | Removed |
| 8.7 | **ACTIVITY TIMELINE** | Navigate to Activity Timeline page → verify entries from this QA session appear | Screenshot of timeline | Recent CRUD actions visible with descriptions |

### DOMAIN 9: Dashboard & Reports

| # | Operation | Steps | Evidence Required | PASS criteria |
|---|-----------|-------|-------------------|---------------|
| 9.1 | **DASHBOARD WIDGETS** | Navigate → verify ALL widget groups: Quick Actions, Activity Timeline, Revenue, Orders, Customers, Products, Content, Inventory, CRM | Full-page screenshot (scroll to capture all) | All widgets render with data |
| 9.2 | **BASELINE RECONCILIATION** | Cross-check dashboard counts against baselines recorded in Phase 0.5: product count, order count, customer count, blog post count | Record: dashboard value vs list value for each | All counts match (accounting for CRUD operations done so far) |
| 9.3 | **REPORTS** | Navigate to Reports → verify Revenue tab loads with chart → switch to Orders tab → verify | Screenshots of both report tabs | Charts render with data |
| 9.4 | **QUICK ACTIONS** | Click a Quick Action card (e.g., "Đơn hàng chờ xử lý") → verify navigates to correct filtered list | Screenshot of destination page | Correct page with correct filter |

### DOMAIN 10: Platform Admin

| # | Operation | Steps | Evidence Required | PASS criteria |
|---|-----------|-------|-------------------|---------------|
| 10.1 | **LOGIN** | Logout tenant admin → login as `platform@noir.local` / `123qwe` → verify platform UI | Screenshot of platform view | Platform admin UI loads (different from tenant) |
| 10.2 | **TENANTS** | Navigate to Tenants → verify list with tenant details | Screenshot of tenants | Tenants list renders |
| 10.3 | **FEATURE MGMT: OFF** | Navigate to Feature Management → toggle a module OFF (e.g., CRM) → Save | Screenshot of module toggled off | Toggle saved |
| 10.4 | **FEATURE MGMT: VERIFY** | Login as tenant admin → verify: sidebar hides CRM section + dashboard hides CRM widgets | Screenshots: (1) sidebar without CRM, (2) dashboard without CRM widgets | Feature effectively hidden |
| 10.5 | **FEATURE MGMT: ON** | Login as platform admin → toggle CRM back ON → Save | Screenshot of module toggled on | Toggle restored |
| 10.6 | **FEATURE MGMT: RESTORE** | Login as tenant admin → verify CRM reappears in sidebar + dashboard | Screenshot showing CRM restored | Everything returns |
| 10.7 | **RE-LOGIN** | Ensure tenant admin session active for subsequent phases | Verify dashboard loads | Back to tenant admin |

---

### CROSS-FEATURE LINKED DATA VERIFICATION

After ALL 10 domains are complete, execute these cross-checks:

| # | Flow | Steps | PASS criteria |
|---|------|-------|---------------|
| CF-1 | **Dashboard ↔ Lists** | Compare dashboard counts with actual list totals for: products, orders, customers, blog posts, employees | All counts match |
| CF-2 | **Order ↔ Customer** | Navigate to a customer who has orders → verify Orders tab shows correct order history | Order history matches orders list |
| CF-3 | **Product ↔ Order** | Navigate to a product referenced in an order → verify product detail still loads (soft delete integrity) | Product accessible |
| CF-4 | **Activity Timeline** | Navigate to Activity Timeline page → verify CRUD actions from this session logged with correct descriptions | Actions logged with translated descriptions |
| CF-5 | **Language switch** | Switch to EN → verify 3 random pages (dashboard, products, customers) → switch back to VI | No raw i18n keys, all text switches cleanly |
| CF-6 | **Module toggle ↔ Dashboard** | Confirm CRM was restored in Domain 10 → dashboard CRM widgets visible | Widgets present |
| CF-7 | **Dept ↔ Employee count** | Navigate to Departments → verify employee counts on each department match actual employee list filtered by dept | Counts match |

---

### VISUAL BATCH (after all CRUD is done)

Test 5 representative pages + 3 dialogs across dark mode + responsive:

**Pages (dark + 768px):**

| Page | Dark mode | 768px responsive |
|------|-----------|-----------------|
| Dashboard | Screenshot + analyze | Screenshot + analyze |
| Products list | Screenshot + analyze | Screenshot + analyze |
| Order detail | Screenshot + analyze | Screenshot + analyze |
| CRM Pipeline Kanban | Screenshot + analyze | Screenshot + analyze |
| HR Employees list | Screenshot + analyze | Screenshot + analyze |

**Dialogs (dark mode only — these are where contrast bugs hide):**

| Dialog | Dark mode |
|--------|-----------|
| Create Customer dialog | Screenshot + analyze |
| Create Employee dialog | Screenshot + analyze |
| Ship Order dialog | Screenshot + analyze |

**Per screenshot**: verify no contrast issues (text readable, borders visible, inputs distinguishable from background), no overflow, no broken layouts, no hardcoded colors overriding theme.

---

### ERROR HANDLING (after visual batch)

| # | Test | Steps | PASS criteria |
|---|------|-------|---------------|
| EH-1 | **Invalid URL** | Navigate to `/portal/ecommerce/products/00000000-0000-0000-0000-000000000000/edit` | 404 page or graceful redirect (no crash/blank page) |
| EH-2 | **Empty form submit** | Open any create dialog → click submit without filling required fields | Inline field errors appear (NOT toast.error) |
| EH-3 | **Search nonsense** | Type "zzzzxxxxxnotexist" in products search | EmptyState component shown (not blank table) |

---

### GATE: Domain Completion Checklist

**Before moving to the next domain, verify ALL of these for the current domain:**

```
☐ CREATE executed — entity actually created, visible in list, count verified (N→N+1)
☐ EDIT executed — 2+ fields changed, changes persisted after save
☐ DELETE executed — entity removed, count decremented (N+1→N), confirmation dialog appeared
☐ SEARCH tested — search input filters correctly, clear resets
☐ LINKED DATA verified — at least 1 cross-entity check (dashboard count, related entity, timeline)
☐ Results recorded in .qa/results/latest.md with action descriptions (not "page loads")
```

**If ANY checkbox is unchecked → you are NOT done with this domain. Go back and complete it.**

Exception: Domain 2 (Orders) has no CREATE/DELETE because orders come from seed data and can't be deleted. The gate for Domain 2 requires: LIST + FILTER + DETAIL + 3 STATUS TRANSITIONS + NOTE + PAYMENTS + LINKED DATA.

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
- ❌ Record "page loads" or "dialog looks correct" as action description
- ❌ Hardcode baseline counts — always read from the actual page

### Context Management

If approaching context limits:
1. Save `.qa/state.json` with exact domain + operation number:
   ```json
   { "status": "IN_PROGRESS", "checkpoint": "domain5-op3", "completedDomains": [1,2,3,4] }
   ```
2. Save `.qa/results/latest.md` with all completed operations
3. Commit all work
4. Tell user: **"Progress saved at Domain [N], Operation [M]. Run `/noir-qa-run` to resume. [X] domains remaining, [Y] operations remaining."**

**On resume**: Read state.json → verify data state → skip to exact operation → continue.

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
   {
     "status": "COMPLETE",
     "lastCheckedCommit": "<current HEAD>",
     "completedAt": "<ISO timestamp>",
     "mode": "<mode used>",
     "checkpoint": null,
     "totalOperations": 95,
     "passed": 95,
     "failed": 0,
     "bugsFound": 0,
     "bugsFixed": 0,
     "critical": 0,
     "high": 0,
     "medium": 0,
     "low": 0
   }
   ```

4. **Summary to user**:
   - Total operations: executed / passed / failed / fixed
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
| Pages | Dynamically discovered from `src/NOIR.Web/frontend/src/portal-app/` |
| Stack | React 19 + TanStack Table + shadcn/ui + Tailwind CSS 4 |
| i18n | English + Vietnamese |
| Theme | Light + Dark |
| Design standards | docs/frontend/design-standards.md, .claude/rules/ |
| Known bugs | docs/qa/ |

---

## Rules

### Execution Rules (HARD — violating = invalid run)

1. **ALL 10 DOMAINS MANDATORY** — You MUST execute CREATE + EDIT + DELETE + SEARCH + LINKED DATA for every domain. No domain may be skipped. If context runs out, save checkpoint and resume — do NOT declare "complete" with missing domains.
2. **INTERACT, DON'T JUST LOOK** — Every CRUD operation MUST actually mutate data. A screenshot of a page loading is NOT a test. If you haven't filled a form, clicked submit, and verified the data changed, you haven't tested anything.
3. **VERIFY LINKED DATA after every mutation** — After CREATE/EDIT/DELETE, navigate to at least 1 related page (dashboard, parent entity, detail page) and verify counts/data updated. This catches real bugs that single-page testing misses.
4. **COMPLETE DOMAIN GATE before moving on** — All 6 checkboxes (CREATE, EDIT, DELETE, SEARCH, LINKED DATA, RECORDED) must be checked before starting the next domain. Go back if anything is missed.
5. **RECORD ACTIONS, NOT OBSERVATIONS** — "Page loads correctly" = invalid. "Created 'QA Customer' (email: qa@test.com), verified in list row 1, count 13→14, dashboard count matches" = valid.
6. **READ COUNTS FROM THE PAGE** — Never hardcode expected counts. Always read from DataTable "Showing X of Y" or stat cards. Seed data may change between versions.

### Process Rules

7. **NEVER write Playwright scripts** — use `mcp__playwright__*` MCP tools directly with AI reasoning. The value is AI visual analysis, not scripted automation.
8. **AUTONOMOUS** — do not ask user questions. Make best judgment, document decisions.
9. **ZERO tolerance** for CRITICAL/HIGH — fix-retest loop until zero.
10. **Commit incrementally** — per feature, never batch at end.
11. **Session-safe** — save state with exact domain + operation checkpoint. Resume picks up at exact operation.
12. **No scope creep** — fix bugs, don't refactor. Minimal changes only.
13. **Agent teams** — use parallel agents when generating/updating 3+ feature domains.
14. **Clean up test data** — Delete test entities after testing (last domain before visual batch). If delete was already tested as part of domain CRUD, no separate cleanup needed.
