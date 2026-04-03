---
name: noir-qa
description: QA orchestrator — git-aware test case management, flow-based test suites, browser execution with visual protocol + ui-audit, and fix-retest loop. 100% system coverage.
---

# NOIR QA Orchestrator — Complete Quality Assurance Pipeline

> **Usage**:
> - `/noir-qa` — Full QA: update cases → organize flows → execute all → report
> - `/noir-qa test <feature>` — Targeted: all cases + flows related to `<feature>` keyword
> - `/noir-qa update` — Git diff → update/add test cases only (no execution)
> - `/noir-qa execute` — Execute existing test cases (no update step)
> - `/noir-qa fix` — Read latest results → fix all issues → re-execute failed cases
>
> **Full pipeline (reusable)**: `/noir-qa-run` — auto-detects first-run vs incremental. Generates/updates cases (parallel agents), executes, fixes, loops until zero bugs. Use every time.
>
> **Duration**: LONG-RUNNING. Supports resume across sessions via `.qa/state.json`.
> **Prerequisites**: NONE — auto-starts and manages all services.
>
> **Agent Teams**: For Phase 2 (case generation), spawn parallel agents per feature domain to maximize context efficiency. Each agent writes to `.qa/cases/{feature}.md`. Coordinator merges and proceeds to execution.

---

## YOUR ROLE

You are a **QA Director** orchestrating a 5-phase pipeline:
1. **Analyze** — What code changed since last QA run?
2. **Plan** — Generate/update test cases for affected features
3. **Organize** — Build prioritized test flows from cases
4. **Execute** — Run tests via browser automation + visual protocol + ui-audit
5. **Report** — Store results, fix bugs, re-execute failed cases

You do NOT skip phases. You do NOT declare done until results show 0 CRITICAL + 0 HIGH.

---

## PHASE 0: SETUP

### 0.1 Parse Command

```
Input: "/noir-qa [subcommand] [args]"

Subcommands:
  (none)           → Full pipeline: phases 1-5
  test <feature>   → Filter to feature keyword, then phases 1-5
  update           → Phases 1-2 only (no execution)
  execute          → Phases 3-4 only (skip case update)
  fix              → Phase 5 only (read results, fix, re-execute)
```

For **targeted mode** (`/noir-qa test login`):
- The `<feature>` keyword maps to test case files and flow entries
- Filter: `.qa/cases/` files where filename or content matches keyword
- Filter: `.qa/flows/` entries referencing matched test case IDs
- Execute ONLY the filtered subset, but with FULL protocol (no shortcuts)

### 0.2 Resume Check

```bash
cat .qa/state.json 2>/dev/null
```

If file exists and `status` is `"IN_PROGRESS"`:
1. Read `.qa/state.json` for last checkpoint
2. Read `.qa/results/latest.md` for prior results
3. Skip to the next uncompleted phase/page
4. Do NOT re-test passed items unless `regressionNeeded` is true

If no state file or `status` is `"COMPLETE"` → fresh start.

### 0.3 Service Lifecycle

You OWN the dev server lifecycle. Start, monitor, restart as needed.

```bash
# Check services
BACKEND_UP=$(curl -sf http://localhost:4000/robots.txt -o /dev/null -w "%{http_code}" 2>/dev/null || echo "000")
FRONTEND_UP=$(curl -sf http://localhost:3000 -o /dev/null -w "%{http_code}" 2>/dev/null || echo "000")

# Start backend if needed
if [ "$BACKEND_UP" != "200" ]; then
  cd src/NOIR.Web && dotnet build --nologo -v q -c Debug > /dev/null 2>&1
  ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS="http://localhost:4000" dotnet watch run --no-launch-profile > ../../.backend.log 2>&1 &
  for i in $(seq 1 60); do
    curl -sf http://localhost:4000/robots.txt -o /dev/null 2>/dev/null && break
    sleep 1
  done
fi

# Start frontend if needed (Windows: must use PowerShell for detached process)
if [ "$FRONTEND_UP" != "200" ]; then
  powershell -Command "Start-Process cmd -ArgumentList '/c cd /d src\NOIR.Web\frontend && pnpm run dev > ..\..\..\.frontend.log 2>&1'"
  sleep 5
fi

# Verify
curl -sf http://localhost:4000/robots.txt -o /dev/null && echo "Backend: OK" || echo "Backend: FAILED"
curl -sf http://localhost:3000 -o /dev/null && echo "Frontend: OK" || echo "Frontend: FAILED"
```

If either fails → show error from logs → STOP and ask user.

For restart procedures during testing (HMR, dotnet watch, full restart), see `noir-test-flow` § SERVICE LIFECYCLE.

### 0.4 Feature Registry Pre-Flight

Before generating/executing test cases, verify features are discoverable:

```bash
# Dynamically enumerate all pages from router/sidebar
ls src/NOIR.Web/frontend/src/portal-app/
```

Map directories to feature domains. **Do NOT hardcode page counts.** The page inventory is derived from:
1. `src/NOIR.Web/frontend/src/portal-app/` subdirectories (actual pages)
2. `.qa/cases/*.md` files (existing test cases)
3. Router files for any hidden/nested pages

When a **new feature is added** (per `.claude/rules/feature-registry-sync.md`):
- New portal-app subdirectory → auto-detected → prompt to generate new `.qa/cases/{feature}.md`
- New module in `Application/Modules/` → prompt to add test cases for module enable/disable

When a **feature is removed**:
- Mark all cases in `.qa/cases/{feature}.md` with `[DEPRECATED]` header
- Do NOT delete the file (historical reference)
- Add test cases verifying graceful removal: sidebar hidden, URL → 404, dashboard widgets gone

When a **module is disabled** (via Feature Management, not removed from code):
- Add test cases that verify: sidebar item absent, direct URL → 404/redirect, dashboard widgets hidden, MCP tools unavailable
- Re-enable → verify everything returns

---

## PHASE 1: GIT DIFF ANALYSIS

**Goal**: Determine what changed since last QA run → which test cases need updating.

### 1.1 Get Changes

```bash
# Read last checked commit (or default to last 50 commits)
LAST_COMMIT=$(cat .qa/state.json 2>/dev/null | jq -r '.lastCheckedCommit // empty')
if [ -z "$LAST_COMMIT" ]; then
  LAST_COMMIT="HEAD~50"
fi

CURRENT_COMMIT=$(git rev-parse HEAD)

# Get changed files
git diff --name-only "$LAST_COMMIT" HEAD
```

### 1.2 Categorize Changes → Affected Features

Map changed files to feature domains:

| Path Pattern | Feature Domain | Impact |
|---|---|---|
| `portal-app/auth/` | auth | Visual + interaction |
| `portal-app/dashboard/` | dashboard | Visual + data |
| `portal-app/orders/` | orders | Visual + interaction + data |
| `portal-app/products/` | catalog | Visual + interaction + data |
| `portal-app/customers/` | customers | Visual + interaction |
| `portal-app/blog/` | content | Visual + interaction |
| `portal-app/hr/` | hr | Visual + interaction + data |
| `portal-app/crm/` | crm | Visual + interaction + data |
| `portal-app/pm/` | pm | Visual + interaction + data |
| `portal-app/settings/` | settings | Visual + interaction |
| `Application/Features/{Feature}/` | {feature} | Data + API behavior |
| `Infrastructure/` | (multiple) | Data queries |
| `uikit/` | (all pages using component) | Visual regression |
| `components/` | (all pages using component) | Visual regression |
| `hooks/` | (all pages using hook) | Behavior |
| `locales/` | (all features) | i18n |
| `contexts/` | (all features) | State management |

### 1.3 Build Change Report

```markdown
## Git Diff Analysis
- Commits analyzed: LAST_COMMIT..CURRENT_COMMIT (N commits)
- Files changed: N

### Affected Features
- orders: 5 files changed (3 frontend, 2 backend)
- catalog: 2 files changed (1 frontend, 1 backend)
- (shared): uikit/DataTable.tsx changed → ALL list pages need regression

### Test Case Impact
- .qa/cases/orders.md: UPDATE needed (new dialog, changed status flow)
- .qa/cases/catalog.md: UPDATE needed (new filter added)
- .qa/cases/crm.md: NEW needed (feature didn't exist before)
- ALL list pages: REGRESSION check (shared DataTable changed)
```

---

## PHASE 2: TEST CASE MANAGEMENT

**Goal**: Generate or update `.qa/cases/{feature}.md` files for affected features.

### 2.1 For Each Affected Feature

1. **Read existing** `.qa/cases/{feature}.md` (if exists)
2. **Read changed source files** to understand what's new/modified
3. **Determine action**:
   - New feature → generate complete test case file
   - Modified feature → update existing cases + add new ones for new functionality
   - Shared component changed → add regression cases to affected features
   - Feature removed → mark cases as `[DEPRECATED]`

### 2.2 Test Case Generation Rules

For each page/dialog/flow, generate cases covering:

**Happy Path (P1)** — Core functionality works:
- Page loads correctly
- CRUD operations succeed
- Search/filter works
- Pagination works
- Navigation works

**Edge Cases (P2)** — Non-obvious scenarios:
- Empty state (no data)
- Boundary values (max length, special characters)
- Concurrent operations (double-click submit)
- Invalid URLs (/entity/nonexistent-id)
- State transitions (all valid + invalid paths)
- Permission boundaries (restricted user)

**Visual (P1-P2)** — Appearance across modes:
- Dark mode rendering
- Vietnamese text (overflow, translation)
- Responsive breakpoints (1440, 1024, 768)
- Component-specific visual states (see noir-test-flow protocol)

**Data Consistency (P1)** — Cross-feature data integrity:
- Create → appears in list + related counts + dashboard + timeline
- Edit → persists + reflects in related views
- Delete → removed from list + related counts updated

**Regression (P2)** — Prevent recurrence of past bugs:
- Check `docs/qa/` lessons learned files for known bug patterns
- Check git log for recent `fix(qa):` commits
- Add specific cases for previously broken scenarios

**Tags** — Apply to every test case for filtering:

| Tag | Use when |
|-----|----------|
| `[smoke]` | Critical-path case — must pass for app to be usable |
| `[regression]` | Prevents recurrence of a known bug |
| `[edge-case]` | Boundary, concurrent, or unusual scenario |
| `[visual]` | Checks appearance (layout, alignment, colors) |
| `[dark-mode]` | Specifically tests dark theme rendering |
| `[i18n]` | Tests Vietnamese translation or locale behavior |
| `[responsive]` | Tests mobile/tablet breakpoints |
| `[data-consistency]` | Verifies cross-feature data integrity |
| `[cross-feature]` | Spans multiple feature domains |
| `[security]` | Permission boundaries, auth, access control |
| `[performance]` | Load time, large dataset rendering, rapid interaction |

### 2.3 Test Case File Format

```markdown
# {Feature} — Test Cases

> Pages: /portal/... | Last updated: YYYY-MM-DD | Git ref: {sha7}
> Total: N cases | P0: X | P1: Y | P2: Z | P3: W

## Page: {Page Name} (`/portal/path`)

### Happy Path

#### TC-{FTR}-001: {Title} [P1] [smoke]
- **Pre**: {Precondition — e.g., "Logged in as admin, orders exist in DB"}
- **Steps**:
  1. Navigate to /portal/path
  2. Verify DataTable renders with columns: {list columns}
  3. Verify pagination shows "Showing X of Y items"
- **Expected**: {What should happen}
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Count correct | ☐ Sort works | ☐ Search works

#### TC-{FTR}-002: Create {entity} [P1] [smoke]
- **Pre**: On list page
- **Steps**:
  1. Click Create button
  2. Fill required fields: {list fields}
  3. Submit
- **Expected**: Toast success, dialog closes, item in list
- **Visual**: ☐ Dialog light | ☐ Dialog dark | ☐ Dialog VI | ☐ Dialog 768px
- **Data**: ☐ In list | ☐ Count +1 | ☐ Dashboard updated | ☐ Timeline entry

### Edge Cases

#### TC-{FTR}-020: {Edge case title} [P2] [edge-case]
- **Pre**: {Setup}
- **Steps**: {Steps}
- **Expected**: {Expected behavior}

### Regression

#### TC-{FTR}-040: {Regression title} [P2] [regression]
- **Bug ref**: BUG-XXX from YYYY-MM-DD QA sweep
- **Steps**: {Exact reproduction steps}
- **Expected**: {Correct behavior — not the bug}
```

### 2.4 Feature-to-File Mapping

**This table is a starting point. Dynamically discover pages from `src/NOIR.Web/frontend/src/portal-app/` — new features auto-detected.**

| Feature Domain | File | Pages Covered | Account |
|---|---|---|---|
| auth | `.qa/cases/auth.md` | Login, Profile, Account Settings | Both |
| dashboard | `.qa/cases/dashboard.md` | Dashboard (all widgets, feature-gated) | Tenant + Platform |
| settings | `.qa/cases/settings.md` | General Settings, Feature Mgmt, Users, Roles, API Keys, Webhooks | Tenant |
| settings | `.qa/cases/settings.md` | Tenants (list, create, edit, settings) | **Platform only** |
| catalog | `.qa/cases/catalog.md` | Products, Categories, Attributes, Brands, Inventory | Tenant |
| orders | `.qa/cases/orders.md` | Orders (list + detail), Payments, Shipping | Tenant |
| customers | `.qa/cases/customers.md` | Customers, Customer Groups, Promotions, Reviews, Wishlists | Tenant |
| content | `.qa/cases/content.md` | Blog Posts, Blog Categories, Blog Tags, Media Library | Tenant |
| hr | `.qa/cases/hr.md` | Employees, Departments, Employee Tags, HR Reports | Tenant |
| crm | `.qa/cases/crm.md` | Contacts, Companies, Pipeline, Activities, CRM Dashboard | Tenant |
| pm | `.qa/cases/pm.md` | Projects, Kanban, Task Detail, Task List, Archived Tasks | Tenant |

**Platform admin testing** (`platform@noir.local`): Tenants page, Feature Management (platform-level availability), cross-tenant isolation. Log out, re-login as platform admin for these pages, then switch back to tenant admin.

### 2.5 Staleness Detection

After updating, add frontmatter to each file:
```markdown
> Last updated: 2026-04-03 | Git ref: abc1234
```

On next run, if the file's `Git ref` is older than the diff range AND no source files changed for that feature → skip update (cases are still fresh).

---

## PHASE 3: FLOW ORGANIZATION

**Goal**: Build prioritized execution order from test cases → `.qa/flows/`.

### 3.1 Auto-Generate Flows

Read ALL `.qa/cases/*.md` files. Organize into flows:

**Smoke Suite** (`.qa/flows/smoke.md`) — P0 critical path, ~15 min:
- All cases tagged `[smoke]`
- Execution order: auth → dashboard → one CRUD per feature
- Purpose: "Is the app fundamentally working?"

**Regression Suite** (`.qa/flows/regression.md`) — P0+P1, ~1-2h:
- All P0 and P1 cases
- Execution order: by feature domain, dependencies first
- Purpose: "Are all features working correctly?"

**Full Suite** (`.qa/flows/full.md`) — ALL priorities, ~3-4h:
- Every case including P2 edge cases and P3 cosmetic
- Purpose: "Pre-release quality gate"

**Cross-Feature Flows** (`.qa/flows/cross-feature.md`) — Linked data flows:
- Product → Order → Payment lifecycle
- Customer journey
- Content publishing
- HR workflow
- CRM pipeline
- PM workflow
- Data integrity edge cases
- Error handling edge cases
- (See noir-test-flow for flow templates)

**Feature-Specific Flows** — One per feature for targeted testing:
- Generated automatically when `/noir-qa test <feature>` runs
- Contains all cases from `.qa/cases/{feature}.md` in dependency order

### 3.2 Flow File Format

```markdown
# {Suite Name}

> Priority: P0-P1 | Estimated: ~1h | Cases: N | Last updated: YYYY-MM-DD

## Prerequisites
- Backend running at :4000
- Frontend running at :3000
- Logged in as admin@noir.local / 123qwe
- Test data seeded (default seed data)

## Execution Order

### 1. Auth (must pass before anything else)
| # | Case ID | Title | Priority | Tags |
|---|---------|-------|----------|------|
| 1 | TC-AUTH-001 | Login with valid credentials | P0 | smoke |
| 2 | TC-AUTH-002 | Dashboard loads after login | P0 | smoke |

### 2. Orders (depends on: Auth, Catalog data exists)
| # | Case ID | Title | Priority | Tags |
|---|---------|-------|----------|------|
| 3 | TC-ORD-001 | View orders list | P1 | smoke |
| 4 | TC-ORD-002 | Create new order | P1 | smoke |
| 5 | TC-ORD-003 | View order detail | P1 | |
| ... | | | | |

## Dependencies
- TC-ORD-002 (create order) requires TC-AUTH-001 (logged in)
- TC-ORD-010 (ship order) requires TC-ORD-002 (order exists)
- TC-CRM-005 (win lead) requires TC-CRM-001 (pipeline exists)
```

### 3.3 Priority-Based Execution

When a flow is selected for execution:
1. Sort by dependency order (prerequisites first)
2. Within same dependency level, sort by priority (P0 → P1 → P2 → P3)
3. Within same priority, sort by feature domain order (matching sidebar)
4. Skip cases marked `[DEPRECATED]` or `[SKIP: reason]`

---

## PHASE 4: EXECUTION

**Goal**: Run selected test cases via browser automation, applying the noir-test-flow visual protocol.

### 4.1 Execution Setup

1. Login via Playwright MCP (`mcp__playwright__*` tools):
   ```
   mcp__playwright__browser_navigate → http://localhost:3000/login
   mcp__playwright__browser_fill_form → email: admin@noir.local, password: 123qwe
   mcp__playwright__browser_click → Sign In
   mcp__playwright__browser_wait_for → dashboard loaded
   ```
   For platform admin pages: logout, login as `platform@noir.local / 123qwe`, test, then switch back.

2. Create results file: `.qa/results/latest.md`

### 4.2 Per-Case Execution

For each test case in the execution order:

1. **Read case** from `.qa/cases/{feature}.md`
2. **Execute steps** via Playwright MCP:
   - Navigate (use sidebar clicks for SPA, not direct URL — see noir-test-flow Technical Notes)
   - Interact (click, fill, select, drag)
   - Verify (screenshot, read, compare)
3. **Apply noir-test-flow visual protocol** for visual-tagged cases:
   - If case has `☐ Light` → screenshot in light mode
   - If case has `☐ Dark` → switch to dark → screenshot → verify
   - If case has `☐ VI` → switch to Vietnamese → screenshot → verify
   - If case has `☐ 768px` → resize → screenshot → verify
   - Restore to light + EN + 1440px after each visual check
4. **For Mini Triple Check items** (dialogs, tabs, empty states):
   - Apply full protocol from noir-test-flow
5. **Record result**: PASS ✅ or FAIL ❌ with details

### 4.3 UI Audit Integration

After completing all cases for a feature's pages, run ui-audit checks:

```bash
cd src/NOIR.Web/frontend/e2e && npx playwright test --project=ui-audit --grep "{page-pattern}"
```

This catches:
- axe-core accessibility violations
- Missing `cursor-pointer` on interactive elements
- Missing `aria-label` on icon-only buttons
- Missing `EmptyState` component
- DataTable column order (actions first)
- And 6 more custom rules

Record ui-audit findings as additional test results.

### 4.4 Session Management

After completing each feature domain (not per-case):

```json
// .qa/state.json
{
  "status": "IN_PROGRESS",
  "lastCheckedCommit": "abc1234",
  "currentPhase": 4,
  "currentFeature": "orders",
  "completedFeatures": ["auth", "dashboard", "settings"],
  "totalCases": 250,
  "executedCases": 85,
  "passedCases": 82,
  "failedCases": 3,
  "bugsFound": 3,
  "bugsFixed": 2,
  "regressionNeeded": false,
  "regressionPages": [],
  "timestamp": "2026-04-03T10:30:00Z",
  "sessionCount": 1
}
```

**Token management**: If approaching context limits:
1. Save state to `.qa/state.json`
2. Ensure `.qa/results/latest.md` is up to date
3. Commit any uncommitted fixes
4. Tell user: **"Progress saved. Run `/noir-qa` to resume. N cases remaining."**

---

## PHASE 5: RESULTS & FIX-RETEST LOOP

**Goal**: Store results, fix bugs, re-execute failed cases until 0 CRITICAL + 0 HIGH.

### 5.1 Results Format

`.qa/results/latest.md`:

```markdown
# QA Results — YYYY-MM-DD

> Run: full | Git ref: abc1234 | Duration: Xh Ym
> Status: **IN_PROGRESS** / **COMPLETE** / **NEEDS_FIX**

## Summary

| Metric | Count |
|--------|-------|
| Total Cases | 250 |
| Passed ✅ | 240 |
| Failed ❌ | 8 |
| Skipped ⏭️ | 2 |
| Bugs Found | 8 |
| Bugs Fixed | 0 |
| CRITICAL | 1 |
| HIGH | 3 |
| MEDIUM | 3 |
| LOW | 1 |

## Failures

| # | Case ID | Title | Severity | Screenshot | Issue |
|---|---------|-------|----------|------------|-------|
| 1 | TC-ORD-015 | Cancel mid-ship | HIGH | temp/qa-orders-BUG-001.png | Button disabled, no tooltip |
| 2 | TC-CRM-008 | Pipeline VI text | MEDIUM | temp/qa-crm-BUG-002.png | "Pipeline" not translated |
| ... | | | | | |

## Bug Tracker

| BUG-ID | Case ID | Severity | Description | Root Cause | Fix | Status |
|--------|---------|----------|-------------|------------|-----|--------|
| BUG-001 | TC-ORD-015 | HIGH | Cancel button disabled without tooltip | Missing Tooltip wrapper | — | OPEN |
| BUG-002 | TC-CRM-008 | MEDIUM | "Pipeline" untranslated in VI | Missing i18n key | — | OPEN |

## Feature Results

### Auth ✅ (5/5 passed)
- TC-AUTH-001: PASS ✅
- TC-AUTH-002: PASS ✅
- ...

### Orders ❌ (12/15 passed, 3 failed)
- TC-ORD-001: PASS ✅
- TC-ORD-015: FAIL ❌ — BUG-001
- ...

## UI Audit Results
[Output from ui-audit per feature]
```

### 5.2 Fix-Retest Loop (`/noir-qa fix`)

This is the core loop that ensures 100% quality:

```
1. READ .qa/results/latest.md
2. FILTER: all bugs with status OPEN (sorted by severity: CRITICAL → HIGH → MEDIUM → LOW)
3. FOR EACH bug:
   a. Read the failing test case from .qa/cases/
   b. Analyze root cause (read source code)
   c. Apply fix (using noir-test-flow bug fix workflow)
   d. Re-execute the SPECIFIC failing test case
   e. If PASS → update bug status to "FIXED+VERIFIED"
   f. If FAIL → update notes, try different fix
   g. If fix touches shared component → add affected pages to regressionNeeded
4. After all bugs fixed:
   a. Run regression on affected pages (if regressionNeeded)
   b. Run full build verification:
      - cd src/NOIR.Web/frontend && pnpm run build
      - cd src/NOIR.Web/frontend && pnpm build-storybook
      - dotnet build src/NOIR.sln
      - dotnet test src/NOIR.sln
   c. Update results: status → COMPLETE (if 0 CRITICAL + 0 HIGH)
5. Commit fixes per feature (not per bug)
```

### 5.3 Commit Strategy

After fixing all bugs for a feature:
```bash
git add [specific files]
git commit -m "$(cat <<'EOF'
fix(qa): [feature] — [N] bugs fixed

- BUG-001: [description]
- BUG-002: [description]

QA: NOIR QA Orchestrator

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>
EOF
)"
```

### 5.4 Completion Criteria

You are NOT done until:

- [ ] 0 CRITICAL bugs remaining
- [ ] 0 HIGH bugs remaining
- [ ] All MEDIUM bugs either fixed or documented with reason to defer
- [ ] Regression check passed (if shared components changed)
- [ ] Frontend build passes (`pnpm run build` — zero errors)
- [ ] Storybook build passes (`pnpm build-storybook`)
- [ ] Backend build passes (`dotnet build src/NOIR.sln`)
- [ ] Backend tests pass (`dotnet test src/NOIR.sln`)
- [ ] All fixes committed
- [ ] `.qa/results/latest.md` status is `COMPLETE`
- [ ] `.qa/state.json` status is `COMPLETE`

---

## PHASE 6: POST-COMPLETION

### 6.1 Update State

```json
{
  "status": "COMPLETE",
  "lastCheckedCommit": "CURRENT_HEAD",
  "completedAt": "2026-04-03T18:00:00Z",
  "totalCases": 250,
  "passedCases": 250,
  "failedCases": 0,
  "bugsFound": 8,
  "bugsFixed": 8,
  "sessionCount": 2
}
```

### 6.2 Lessons Learned

If bugs found ≥ 3, create `docs/qa/real-qa-lessons-YYYY-MM-DD.md`:
- Bug pattern analysis (categorized: Visual | Data | i18n | Theme | UX)
- High-risk areas
- Root cause analysis
- Prevention recommendations

### 6.3 Rule Updates

If a bug reveals a missing rule (systemic pattern, 3+ instances):
- Add `.claude/rules/` file if new pattern
- Update existing rule if incomplete
- Update CLAUDE.md only if Critical rule missed

### 6.4 Memory Update

Save QA patterns to memory for future conversations:
- Common bug patterns found
- Prevention rules
- High-risk areas

---

## PAGE INVENTORY (Dynamic)

**Do NOT rely on hardcoded counts.** Enumerate pages dynamically:

```bash
# Discover all page directories
ls src/NOIR.Web/frontend/src/portal-app/
# Cross-reference with router files for nested/detail pages
```

**Current known domains** (update when features are added/removed):

```
AUTH: Login, Dashboard, Profile, Theme switching, Language switching
SETTINGS: General Settings (ALL tabs), Feature Management, Users, Roles,
          Tenants (platform admin only), API Keys, Webhooks
CATALOG: Products, Product Categories (tree), Product Attributes, Brands, Inventory Receipts
ORDERS: Orders (list + detail + status transitions), Payments, Shipping
CUSTOMERS: Customers (list + detail), Customer Groups, Promotions, Reviews, Wishlists
CONTENT: Blog Posts (CRUD + editor), Blog Categories, Blog Tags, Media Library
HR: Employees, Departments (tree), Employee Tags, HR Reports
CRM: Contacts, Companies, Leads/Pipeline (Kanban), CRM Activities, Dashboard widgets
PM: Projects, Kanban Board, Task Detail, Task List View, Archived Tasks
```

**When new pages appear** in `portal-app/` that have no matching `.qa/cases/` entry → generate test cases automatically.
**When pages are removed** → mark cases `[DEPRECATED]`, do NOT delete files.

---

## TARGETED EXECUTION: `/noir-qa test <feature>`

When user runs `/noir-qa test login`:

1. **Keyword mapping**: `login` → matches `auth` domain, specifically Login page
2. **Load cases**: Read `.qa/cases/auth.md`, filter to Login section
3. **Load flows**: Find all flow entries referencing `TC-AUTH-*` Login cases
4. **Also include**: Any cross-feature flows that START with login (e.g., "Customer Journey" starts with login)
5. **Execute**: Full protocol on filtered cases (no shortcuts — same visual checks, same data consistency)
6. **Report**: Results only for executed cases, but full detail

**Keyword → Feature mapping**:

| Keyword | Feature Domain | Scope |
|---|---|---|
| `login`, `auth` | auth | Login, Profile, Account |
| `dashboard` | dashboard | Dashboard + widgets |
| `users`, `roles`, `settings` | settings | Settings pages |
| `products`, `catalog`, `brands`, `inventory` | catalog | Catalog pages |
| `orders`, `payments`, `shipping` | orders | Order management |
| `customers`, `groups`, `promotions`, `reviews` | customers | Customer pages |
| `blog`, `media`, `content` | content | Content pages |
| `employees`, `departments`, `hr`, `tags` | hr | HR pages |
| `contacts`, `companies`, `leads`, `pipeline`, `crm` | crm | CRM pages |
| `projects`, `kanban`, `tasks`, `pm` | pm | PM pages |
| `all`, `full` | (all) | Everything |

---

## EXECUTION MODE

**AUTONOMOUS** — do not ask user questions during testing. Make best judgment, document decisions.

**Ask user ONLY if:**
- Services cannot start (after 2 retry attempts)
- Database corrupted or needs manual intervention
- Bug requires breaking architectural change (10+ files)
- Security vulnerability discovered

**Pacing**:
- Complex pages (Products, Orders, Kanban): more time
- Simple CRUD pages (Brands, Blog Tags): less time
- Cross-feature flows: thorough verification
- Pages with bug history (check `docs/qa/`): extra attention

**Context budget**: Target 10-15 pages per session. Save progress after each feature domain.

---

## CRITICAL RULES

1. **NEVER write Playwright scripts** — use `mcp__playwright__*` MCP tools directly with AI reasoning (Opus model, max effort). The value is AI visual analysis, not scripted automation.
2. **NEVER skip visual protocol** — dark + VI + responsive on every dialog/tab/popup
2. **NEVER test only light mode** — if not screenshotted in dark, not tested
3. **NEVER batch fixes across features** — fix per-feature, commit per-feature
4. **NEVER declare PASS if any element untested** — test everything or document skip reason
5. **ALWAYS screenshot before and after fixes** — evidence trail
6. **ALWAYS commit after each feature's fixes** — prevents work loss
7. **ALWAYS verify services alive before testing** — stale browser = false bugs
8. **ALWAYS update state file after each feature** — enables resume
9. **ALWAYS run ui-audit after feature execution** — catches automated rule violations
10. **NEVER proceed to completion until 0 CRITICAL + 0 HIGH** — the fix loop IS the quality gate
