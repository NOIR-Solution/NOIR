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
.qa/cases/ is empty?
  YES → MODE: FULL INIT (generate all cases from scratch)
  NO  → .qa/state.json exists with status "IN_PROGRESS"?
          YES → MODE: RESUME (pick up where left off)
          NO  → MODE: INCREMENTAL (git diff → update changed → execute → fix)
```

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

## MODE: INCREMENTAL (existing cases — update what changed)

### Phase A: Git Diff → Targeted Update

```bash
LAST_COMMIT=$(cat .qa/state.json 2>/dev/null | jq -r '.lastCheckedCommit // empty')
[ -z "$LAST_COMMIT" ] && LAST_COMMIT="HEAD~50"
git diff --name-only "$LAST_COMMIT" HEAD
```

Map changed files → affected feature domains (see `noir-qa SKILL.md` Phase 1.2 mapping table).

**If 3+ feature domains affected** → spawn parallel agents (one per affected domain), each updating its `.qa/cases/{feature}.md`:
- Read existing cases
- Read changed source files
- Add new cases for new pages/dialogs/fields
- Update existing cases if behavior changed
- Mark cases `[DEPRECATED]` if feature removed
- Add regression cases for recently-fixed bugs (`git log --grep="fix(qa)"`)

**If 1-2 domains affected** → update directly (no agent team needed).

**If shared component changed** (uikit/, components/, hooks/):
- Identify all pages using that component
- Add `[regression]` tag to affected cases across multiple feature files

After updating, proceed to **Build Flows** (below).

---

## MODE: RESUME (interrupted session)

Read `.qa/state.json` → skip to the phase/feature where it left off. Do NOT re-generate cases or re-execute passed tests. Continue from exact checkpoint.

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

### Service Setup
```bash
# Check and start services if needed — see noir-qa SKILL.md Phase 0.3
BACKEND_UP=$(curl -sf http://localhost:4000/robots.txt -o /dev/null -w "%{http_code}" 2>/dev/null || echo "000")
FRONTEND_UP=$(curl -sf http://localhost:3000 -o /dev/null -w "%{http_code}" 2>/dev/null || echo "000")
```

### What to Execute

| Mode | Suite to execute |
|------|-----------------|
| FULL INIT | `regression.md` (P0+P1) — covers all core functionality |
| INCREMENTAL | Only cases in affected feature domains + regression-tagged cases |
| RESUME | Continue from checkpoint in state.json |

### Per-Case Execution Contract

**CRITICAL: "Execute" means INTERACT, not just LOOK.** A screenshot of a page loading is NOT execution. You must follow the test case steps and interact with the application.

#### Execution Depth by Case Type

| Case Type | Minimum Required Actions | Evidence Required |
|-----------|-------------------------|-------------------|
| **CREATE** | Open dialog/page → fill ALL required fields → submit → verify success toast → verify item appears in list with correct data | Screenshots: (1) empty form, (2) filled form, (3) success state, (4) item in list |
| **UPDATE** | Find entity → open edit → change 2+ fields → save → verify changes persisted (reload page, check list) | Screenshots: (1) before edit, (2) form with changes, (3) after save showing new values |
| **DELETE** | Find entity → click delete → verify confirmation dialog → confirm → verify entity removed from list → verify count decremented | Screenshots: (1) before delete with count, (2) confirmation dialog, (3) after delete with updated count |
| **STATUS TRANSITION** | Navigate to entity detail → click status action button → confirm if needed → verify new status badge → verify timeline updated | Screenshots: (1) before status, (2) action click, (3) after status with timeline |
| **LIST/VIEW** | Navigate → verify DataTable columns render → test search (type query, verify filter) → test sort (click header) → test pagination (next page) | Screenshots: (1) full page, (2) search results, (3) sorted column |
| **LINKED DATA** | Perform action on Entity A → navigate to Entity B → verify B reflects the change (count, list entry, timeline) | Screenshots: (1) action on A, (2) B before, (3) B after showing change |
| **FORM VALIDATION** | Submit empty required fields → verify inline errors appear (not toast) → fill with invalid data → verify field-level errors → fix → submit succeeds | Screenshots: (1) errors shown, (2) specific field error, (3) successful submit |
| **VISUAL** | Screenshot in light → toggle dark mode → screenshot → toggle Vietnamese → screenshot → resize 768px → screenshot. Analyze each for issues. | All 4 screenshots with written analysis |

#### Anti-Patterns (DO NOT DO)

- ❌ Navigate to page → screenshot → "PASS" (this tests nothing)
- ❌ Skip form filling because "the dialog looks correct"  
- ❌ Mark CREATE as PASS without actually creating an entity
- ❌ Mark DELETE as PASS without actually deleting an entity
- ❌ Check only visual rendering without testing interactions
- ❌ Test 50 pages shallowly instead of 15 pages deeply

#### Depth Over Breadth Rule

**If context limits force a choice: test 15 pages with full CRUD depth rather than 50 pages with screenshots only.** Prioritize:
1. Cross-feature flows (`.qa/flows/cross-feature.md`) — these catch the most real bugs
2. P0 cases (status transitions, CRUD, data integrity)
3. P1 smoke cases (page loads, basic interactions)
4. Visual checks (dark/VI/responsive — batch these efficiently)

#### Per-Case Recording

For each executed case, record in `.qa/results/latest.md`:
```markdown
| TC-XXX-NNN | Title | ✅ PASS / ❌ FAIL | Actions: created entity "X", verified in list, count updated |
```
The "Notes" column MUST describe what you DID, not just what you SAW. "Page loads correctly" is insufficient — "Created customer 'Test QA', verified in list row 1, count changed 13→14" is correct.

After each feature domain:
- Run **ui-audit** automated checks:
  ```bash
  cd src/NOIR.Web/frontend/e2e && npx playwright test --project=ui-audit --grep "{feature-pattern}"
  ```
  This catches: axe-core a11y violations, missing cursor-pointer, missing aria-label, missing EmptyState, DataTable column order issues.
- Save progress to `.qa/state.json`
- Commit any fixes

### Context Management

If approaching context limits:
1. Save `.qa/state.json` with exact checkpoint
2. Save `.qa/results/latest.md`
3. Commit all work
4. Tell user: **"Progress saved at [feature]. Run `/noir-qa-run` to resume. [N] cases remaining."**

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

- **INTERACT, DON'T JUST LOOK** — Every CRUD test case MUST actually create/edit/delete data. A screenshot of a page loading is NOT a test. If you haven't clicked a button, filled a form, or verified a data change, you haven't tested anything. This is the #1 rule.
- **NEVER write Playwright scripts** — use `mcp__playwright__*` MCP tools directly with AI reasoning (Opus model, max effort). The value is AI visual analysis + intelligent decision-making, not scripted test automation.
- **AUTONOMOUS** — do not ask user questions. Make best judgment, document decisions.
- **ZERO tolerance** for CRITICAL/HIGH — loop until zero.
- **Depth over breadth** — 15 pages tested deeply (CRUD + linked data) beats 50 pages with screenshots. Prioritize cross-feature flows and P0 cases.
- **Commit incrementally** — per feature, never batch at end.
- **Screenshots as evidence** — before + after every bug fix AND every CRUD operation.
- **Notes must describe actions** — "Page loads" is WRONG. "Created entity X, verified in list, count N→N+1" is RIGHT.
- **Session-safe** — save state constantly. Resume seamlessly in new conversation.
- **No scope creep** — fix bugs, don't refactor. Minimal changes only.
- **Agent teams** — use parallel agents when generating/updating 3+ feature domains.
- **Clean up test data** — After testing, delete test entities you created (unless they're needed for subsequent tests).
