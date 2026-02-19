# NOIR — Best-in-Class E-commerce Admin Portal

## Primary Goals (All 5 Must Be Achieved)

### Goal 1: UI/UX Consistency — One Design Language
Every page, component, dialog, table, card, and form MUST follow ONE unified design language.
No page should look like it was built by a different team.

**Calibration example:** Payment Provider config page = gold standard UI/UX in the app.
Shipping Provider config (and similar pages) currently looks different → mismatch.
When two pages serve the same purpose but look different, find the best one and unify all others to match.

**Pattern categories to audit and unify:**
Cards, Tables, Dialogs, Forms, Buttons, Spacing, Typography, Colors, Shadows, Animations,
Empty states, Loading states, Toasts, Status badges, Config/Settings pages.

For each category: identify all variants in codebase → pick BEST (existing or researched) → apply everywhere.

### Goal 2: Storybook 100% Sync
Every UI component used in the website MUST have a Storybook story showing all variants, states, and sizes.
- Story location: `src/uikit/{component-name}/{Component}.stories.tsx`
- Cross-reference `src/components/ui/` (actual) vs `src/uikit/` (stories) vs app imports
- Missing stories → create. Outdated stories → update. `pnpm build-storybook` → 0 errors.

### Goal 3: Backend Consistency — Same Pattern Everywhere
Every feature MUST follow the EXACT same code patterns. A developer reading any handler
should feel like the same person wrote all of them.

**Backend pattern categories to audit and unify:**
| Category | What to Check |
|----------|--------------|
| Handler structure | Same shape for get-by-id, get-paged-list, create, update, soft-delete |
| Specifications | Composable, TagWith() on all, AsTracking for mutations, consistent naming |
| Folder structure | Commands/{Action}/ and Queries/{Action}/ consistent across all features |
| Error handling | Result<T> flow from handler → endpoint → API → frontend, Error.Validation() param order |
| Audit commands | IAuditableCommand on ALL mutation commands, before-state resolvers for updates |
| Validators | FluentValidation on all Commands/Queries, consistent rule patterns |
| DI registration | Marker interfaces (IScopedService, etc.) on all services, no manual registration |
| Entity configuration | IEntityTypeConfiguration per entity, TenantId in unique constraints |
| Repository pattern | IRepository<T, TId> + IUnitOfWork, never direct DbContext in services |
| DTOs | Consistent mapping patterns, no leaking domain entities to API |
| Endpoint structure | Consistent route naming, HTTP verbs, response types |
| Dead code | Zero unused imports, zero commented-out blocks, zero duplicate logic |

For each category: audit all features → identify inconsistencies → unify to ONE pattern.

### Goal 4: Fill All Test Coverage Gaps
Audit existing tests (10,595+), find what's NOT covered, fill every gap.

- Every handler: unit tests (happy path + every error branch)
- Every validator: unit tests (valid input, each invalid field, boundary values)
- Every domain entity: unit tests (creation, state transitions, business rules)
- Every endpoint: integration tests (success, auth, forbidden, validation error, not found)
- Architecture tests: naming conventions, folder structure, DI registration completeness
- `dotnet test src/NOIR.sln` → ALL pass, zero skipped
- Phase 0 auditor MUST produce a **Test Gap Report**: which handlers/validators/endpoints lack tests

### Goal 5: Feature Completeness — Match Top E-commerce Platforms
Research what the best e-commerce admin portals offer and fill gaps:
Shopify Admin, Shopee Seller Center, Haravan, Sapo, WooCommerce, Medusa.js, Saleor.

Don't just copy a feature list — think critically about what a serious VN e-commerce
platform NEEDS. **Scope per round:** identify top 10 missing features, prioritize top 3-5
for implementation based on business value. Don't try to build everything at once.

---

## Execution Method: Agent Team (NON-NEGOTIABLE)

**HARD RULE: You MUST use TeamCreate + Task tool to spawn agent teammates.**
- Minimum 2 teammates per phase. No exceptions.
- You are the **Team Lead / Coordinator**. You do NOT write code yourself.
- Your job: plan phases, spawn teammates, assign tasks, review results, enforce quality gates.
- If you catch yourself writing code directly instead of delegating → STOP → spawn a teammate.
- Spawn teammates with `model: "opus"` parameter for maximum quality.

### Team Patterns (Pick Per Phase)

| Phase Type | Min Agents | Recommended Roles |
|------------|-----------|-------------------|
| Research/Audit | 2-3 | `codebase-auditor` + `ui-explorer` (+ `ecommerce-researcher`) |
| UI Consistency | 2-3 | `pattern-researcher` + `component-fixer` (+ `storybook-updater`) |
| Backend Feature | 2-3 | `backend-dev` + `test-writer` (+ `migration-handler`) |
| Frontend Feature | 2-3 | `frontend-dev` + `storybook-writer` (+ `localization`) |
| Full-Stack Feature | 3 | `backend-dev` + `frontend-dev` + `test-writer` |
| Storybook Sync | 2 | `story-writer` + `story-reviewer` |
| QA Round | 2 | `qa-visual` + `qa-functional` |

### Team Lifecycle (Every Phase)

```
1. TeamCreate → create team for this phase
2. Task tool → spawn teammates with clear, scoped assignments
3. Monitor → read task list, respond to teammate messages
4. Quality Gate → run build + test + frontend build + storybook build
5. Shutdown teammates → SendMessage type: "shutdown_request"
6. TeamDelete → clean up before next phase
```

### Delegation Rules
- Each teammate gets ONE focused responsibility (not "do everything")
- Teammates work in PARALLEL when tasks are independent
- You coordinate handoffs when tasks have dependencies
- You run quality gates yourself (dotnet build, dotnet test, pnpm build, pnpm build-storybook)

---

## Phased Execution

### Phase 0: Research & Audit (ALWAYS START HERE)

**Prerequisites:** Start the app with `./start-dev.sh` before this phase (needed for `ui-explorer`).

**Team:** `codebase-auditor` + `ui-explorer` + `ecommerce-researcher`

**codebase-auditor:**
- Read CLAUDE.md, docs/FEATURE_CATALOG.md, docs/PROJECT_INDEX.md
- Scan src/: map every feature, entity, handler, endpoint, specification
- Scan tests/: map test coverage per feature (which handlers/validators/endpoints have tests, which don't)
- Scan frontend src/portal-app/: map every page, component, hook, query, mutation
- Scan src/uikit/: map all Storybook stories, cross-reference with components actually used
- Produce: **Feature Inventory Matrix** (what exists, what's missing tests, what's incomplete)
- Produce: **Storybook Coverage Report** (components with stories, without stories, outdated stories)

**ui-explorer:**
- Use Playwright MCP tools to navigate EVERY page in the live app
- Screenshot every page at desktop viewport (1920x1080)
- For each page, catalog: card style, table style, dialog style, spacing, shadows, typography, colors
- Produce: **UI Pattern Matrix** (rows = pages, columns = pattern categories, cells = which variant)
- Highlight mismatches: "Products page uses X, Orders page uses Y for same pattern"
- Flag the best patterns (Payment Provider config = known gold standard)

**ecommerce-researcher:**
- Research Shopify Admin, Shopee Seller Center, Haravan, Sapo, WooCommerce, Medusa.js, Saleor
- For each: list their feature set, identify what NOIR is missing
- Research modern admin UI/UX: Linear, Vercel Dashboard, Stripe Dashboard, Shopify Polaris
- Identify design patterns that make them feel premium
- Produce: **Feature Gap Analysis** + **UI/UX Research Report**

**You (Team Lead) then:**
- Synthesize all 3 reports into a prioritized roadmap
- Group work into phases: UI consistency first, then backend consistency, then new features, then QA
- Present roadmap to user for approval before Phase 1

### Phase 1-N: Build (You Decide Scope Per Phase)

For each phase:
1. Define scope (which goal(s) this phase advances)
2. Choose team pattern from table above
3. Spawn teammates with specific, bounded assignments
4. Monitor progress, unblock teammates
5. Run quality gates
6. Shutdown team, clean up, move to next phase

**Suggested phase ordering** (adjust based on audit findings):
1. UI Consistency — unify design patterns, establish standards
2. Storybook Sync — fill gaps, update outdated stories
3. Backend Consistency — unify handler/spec patterns, eliminate duplication
4. Test Coverage — fill testing gaps found in audit
5. New Features — implement features from gap analysis
6. Final QA — visual + functional verification

---

## Architecture Philosophy

### The Golden Rule
> Adding a new feature should require writing ONLY the unique business logic.
> Everything else — registration, wiring, routing, validation plumbing,
> test scaffolding — should be automatic or one-line.

### Backend: Convention Over Configuration
- Auto-discovery everything: DI via marker interfaces, endpoint mapping via assembly scanning,
  entity config via ApplyConfigurationsFromAssembly, validators auto-discovered by pipeline
- Generic base patterns: if 10 handlers do the same shaped work, extract shared behavior
- Shared specifications: common query patterns (paged, filtered, sorted) should be composable
- Consistent folder structure: finding one feature folder = knowing ALL feature folders
- Pipeline behaviors: cross-cutting concerns in middleware, never duplicated in handlers
- One-step feature creation: Entity → Command/Handler → Endpoint. That's it.

### Frontend: Shared & Composable
- Feature modules self-contained: each portal-app/{feature}/ owns its pages, queries, mutations
- Shared hooks: ONE hook for search + filter + paginate + sort. Pages just provide config.
- Shared form pattern: Zod schema + react-hook-form. New form = schema + fields. Done.
- Shared mutation patterns: optimistic operations via shared utility hooks
- Type-safe end-to-end: Backend DTO → API response → Frontend type → Form schema → UI

### Storybook as Living Documentation
- Storybook is NOT optional — it IS the component documentation
- Every component change MUST include a story update
- Stories show all variants, not just the default
- `pnpm storybook` at http://localhost:6006 is the component reference

---

## Quality Gates (Run After EVERY Phase)

```bash
dotnet build src/NOIR.sln                              # 0 errors
dotnet test src/NOIR.sln                               # ALL pass, zero skipped
cd src/NOIR.Web/frontend && pnpm run build             # 0 errors, 0 warnings (strict)
cd src/NOIR.Web/frontend && pnpm build-storybook       # 0 errors, 0 warnings
```

**Additional checks:**
- No hardcoded strings (all text uses `t('key')` with EN + VI translations)
- All interactive elements have `cursor-pointer`
- All icon-only buttons have `aria-label`
- All destructive actions have confirmation dialogs

---

## QA: Playwright MCP Testing (Dedicated QA Agent)

Spawn a QA agent that uses Playwright MCP tools DIRECTLY to navigate the live app.
Not writing test files — navigating, observing, reasoning about what it sees.

**QA Agent tests per feature:**
- CRUD operations (create → list → update → delete → verify)
- Form validation (empty submit, invalid data, error clearing)
- Dialogs (open, close X/Escape/overlay, submit with loading state)
- Tables (search, filter, sort, pagination, bulk select, empty state)
- Localization (switch EN↔VI, verify all text changes, VND formatting)
- Cross-feature data integrity (create product → appears in inventory)
- Page transitions (smooth via View Transitions, no full reloads)
- Error handling (API errors, duplicate data, 404 pages)

**QA Report format:**
```
Feature: [Name] | Pages: [count]
CRUD: ✅/❌ | Validation: ✅/❌ | Dialogs: ✅/❌
Localization: ✅/❌ | Consistency: ✅/❌
Issues: [list with screenshots]
```

---

## Vietnam Market Context

| Aspect | Value |
|--------|-------|
| Currency | VND (no decimals, 1.000.000₫) |
| Phone | +84 format |
| Address | Tỉnh/TP → Quận/Huyện → Phường/Xã → Chi tiết |
| Tax | VAT 8%/10% |
| Date | DD/MM/YYYY |
| Carriers | GHN, GHTK, VNPost, J&T |
| Payments | VNPay, MoMo, ZaloPay, COD |
| Language | VI primary, EN secondary |

---

## Rules

- Follow ALL rules in CLAUDE.md — no exceptions
- When unsure about a pattern, check existing code first — consistency > creativity
- Don't over-engineer. Don't under-engineer. Engineer exactly right.
- Ship working increments. Every phase must pass ALL quality gates.
- You decide features, phases, and team size. I trust your judgment. Just make it the best.

---

## Reminder: You Are The Coordinator

```
❌ WRONG: Reading files yourself, writing code, making changes directly
✅ RIGHT: Spawning teammates, assigning tasks, reviewing results, running quality gates

If you find yourself editing a file → STOP → delegate to a teammate.
The only commands you run directly are: build, test, storybook build, and quality gate checks.
```

## Done Criteria

```
✅ Goal 1: Every page follows ONE consistent design language (visual proof via screenshots)
✅ Goal 2: Every component has a Storybook story, pnpm build-storybook passes
✅ Goal 3: Every backend feature follows exact same patterns, zero duplication
✅ Goal 4: 100% test coverage — every handler, validator, entity, endpoint tested
✅ Goal 5: Feature set matches or exceeds top e-commerce platforms for VN market
✅ All quality gates pass: dotnet build, dotnet test, pnpm build, pnpm build-storybook
✅ All text localized EN + VI, zero hardcoded strings
✅ QA report shows all features pass functional + visual checks
```
