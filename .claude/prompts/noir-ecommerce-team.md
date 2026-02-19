# NOIR — Best-in-Class E-commerce Admin Portal

## Vision
Create the most professional, robust open-source multi-tenant e-commerce
admin portal on GitHub. Targeting Vietnam market. The codebase should be
a reference implementation that developers look at and say "this is how
it's done."

## Execution Method: Agent Team (MANDATORY)

You MUST use the agent team system to execute this project:

1. **Create team**: Use `TeamCreate` tool to create a team named "noir-ecommerce"
2. **Create tasks**: Use task management to track all work items
3. **Spawn teammates**: Use `Task` tool with `team_name: "noir-ecommerce"`
   to spawn specialized agents. Give each a clear name and role.
4. **Coordinate via messages**: Use `SendMessage` to communicate with teammates,
   assign work, review results, and unblock issues.
5. **Quality gate**: After each phase, verify all quality checks yourself
   before moving to next phase.
6. All team member use opus 4.6 thinking

Do NOT try to do everything yourself in a single context.
Delegate to specialized teammates. You are the coordinator, not the builder.

## How to Work
You are the **Team Lead**. Think like a CTO building a product.

### Phase 0: Research & Audit
1. **Audit codebase** — Read CLAUDE.md, scan src/, tests/, docs/.
   Map what EXISTS (features, patterns, components, tests, pages).

2. **Research features** — Study what the best e-commerce admin portals offer:
   Shopify Admin, Shopee Seller Center, Haravan, Sapo, WooCommerce, Medusa.js,
   Saleor. Determine the complete feature set a serious VN e-commerce platform
   needs. Don't copy my list — think independently, be comprehensive.

3. **Research UI/UX** — Deep research into modern admin dashboard design:
   - Color scheme: what conveys trust, professionalism, and works for
     long-hour usage (eye strain matters for admin panels)
   - Typography: optimal for Vietnamese diacritics + data-dense tables
   - Layout patterns: navigation, information hierarchy, data visualization
   - Micro-interactions: transitions, feedback, loading states
   - Study: Linear, Vercel Dashboard, Stripe Dashboard, Shopify Polaris,
     Ant Design Pro, Tremor. Extract what makes them feel premium.

4. **Gap analysis** — Compare what EXISTS vs what SHOULD EXIST.
   This becomes your roadmap.

### Phase 1-N: Build (You Decide the Phases)
For each phase:
- Decide scope, agent count, specializations
- Spawn only what's needed for THIS phase
- Every phase must pass quality gates before next phase starts

### Continuous: Quality Gates
After EVERY phase:
```bash
dotnet build src/NOIR.sln           # 0 errors
dotnet test src/NOIR.sln            # ALL pass
cd src/NOIR.Web/frontend && pnpm run build  # 0 errors, 0 warnings (strict)
```

## Architecture & Automation Philosophy

### The Golden Rule
> Adding a new feature should require writing ONLY the unique business logic.
> Everything else — registration, wiring, routing, validation plumbing,
> test scaffolding — should be automatic or one-line.

### Backend Structure: Convention Over Configuration
- **Auto-discovery everything**: DI registration via marker interfaces
  (IScopedService, ITransientService) — never manually register.
  Endpoint mapping via assembly scanning — never manually add routes.
  Entity configuration via ApplyConfigurationsFromAssembly — never manually list.
  Validators auto-discovered by Wolverine pipeline — never manually wire.
- **Generic base patterns**: If 10 handlers do the same shaped work
  (get by id, get paged list, soft delete), extract a generic base or
  shared behavior. Don't copy-paste the same 30 lines across 10 handlers.
- **Shared specifications**: Common query patterns (paged, filtered, sorted,
  searched) should be composable, not rewritten per entity.
- **Consistent folder structure**: A developer finding one feature folder
  should know EXACTLY where to find the same file in any other feature.
  No surprises. No special cases.
- **Pipeline behaviors**: Cross-cutting concerns (logging, validation,
  caching, audit) live in pipeline middleware — never duplicated in handlers.
- **One-step feature creation**: Adding "Promotions" feature should be:
  (1) Create entity, (2) Create command/handler, (3) Create endpoint.
  DI, validation, audit, localization, error handling — all automatic via
  conventions. If it takes more than 3 steps, the architecture is wrong.

### Frontend Structure: Shared & Composable
- **Feature modules are self-contained**: Each portal-app/{feature}/ owns
  its pages, queries, mutations, types, components. No reaching across modules.
- **Shared hooks eliminate repetition**: If 10 pages do search + filter +
  paginate + sort, there's ONE hook (or composable) that handles it. Pages
  just provide config, not implementation.
- **Shared table/list pattern**: Data tables have a standard wrapper that
  handles column config, search, filter, sort, pagination, bulk select,
  export — pages just declare columns and data source.
- **Shared form pattern**: Form dialogs (create/edit) follow one pattern
  with Zod schema + react-hook-form. Adding a new form = define schema +
  define fields. No manual wiring of error states, loading, submission.
- **Shared mutation patterns**: Optimistic delete, optimistic update,
  cache invalidation — all via shared utility hooks, not copy-pasted per feature.
- **Component composition > component props**: Don't make a God component
  with 30 props. Make small composable pieces. Follow shadcn/ui philosophy.
- **Type-safe end-to-end**: Backend DTO shape → API response → Frontend type →
  Form schema → UI rendering. One source of truth, no manual type syncing.

### Automation Rules (No Manual Labor)
- **Localization**: If a pattern can auto-generate i18n keys from feature
  names, do it. Don't hand-write 500 translation keys.
- **API client generation**: Frontend API service types should derive from
  backend contracts, not be manually maintained.
- **Test patterns**: Base test classes for common scenarios (CRUD handler
  tests, validation tests, endpoint tests). Writing a test = inherit base +
  provide test data. Not rewrite 50 lines of setup.
- **Error handling**: Consistent Result<T> pattern flows from handler →
  endpoint → API response → frontend error display. No ad-hoc try/catch.
- **Audit trail**: IAuditableCommand + pipeline behavior = automatic.
  Developers don't "remember" to add audit logging — it's enforced by type system.
- **Multi-tenancy**: Finbuckle middleware handles tenant resolution globally.
  Features never manually filter by TenantId in business logic — it's automatic.

### How to Evaluate Structure Quality
Ask these questions after each phase:
1. Can a new developer add a feature by copying an existing one? → YES required
2. How many files must be touched to add a simple CRUD feature? → MINIMIZE this
3. Are there any manual registration steps? → ELIMINATE all of them
4. Is there copy-pasted logic across features? → EXTRACT to shared patterns
5. If I rename an entity, does the type system catch all the places? → YES required
6. Are cross-cutting concerns (auth, audit, cache, validation) in ONE place? → YES required

## What "Best on GitHub" Means

### Backend: Bulletproof
- **Consistency**: every feature follows the EXACT same patterns.
  A developer reading any handler should feel like the same person wrote all of them.
- **No waste**: zero dead code, zero unused imports, zero commented-out blocks,
  zero duplicate logic. Every line earns its place.
- **Complete testing**: if code exists, tests exist. No exceptions.
  Unit tests for logic, integration tests for endpoints, architecture tests for rules.
- **Maintainable**: a new developer can add a feature by copying an existing one
  and changing the names. That's how consistent patterns should be.

### Frontend: Premium Feel
- **Research-driven design**: don't guess colors or layouts. Research what top
  products use and WHY. Then make informed decisions.
- **Smooth navigation**: the project uses Browser Navigation API / View Transitions
  for smooth page transitions. Ensure ALL pages leverage this — no jarring full reloads.
- **Consistency**: every page follows the same layout grid, same spacing scale,
  same component patterns, same animation timing. It should feel like ONE product.
- **Localization**: every string in EN + VI. Zero hardcoded text.
- **Responsive**: works on desktop (primary), tablet (secondary).
- **Accessibility**: keyboard navigation, aria-labels, focus management.
- **Performance**: skeleton loading, optimistic updates, deferred search,
  transition-wrapped filters. The UI should never feel slow.

### Codebase: Reference Quality
- **README.md**: professional, badges, screenshots, quick-start in < 5 steps.
  Someone landing on this GitHub repo should immediately understand what it is
  and want to try it.
- **CLAUDE.md**: concise, accurate, effective. Every rule pulls its weight.
- **docs/**: architecture diagrams, API reference, development guide.
  Enough for a new dev to be productive in day one.
- **No debt**: no TODOs left unresolved, no FIXME without a fix,
  no HACK without removal.

## Testing: 100% Coverage, Zero Blind Spots

### Backend: Unit + Integration (100% Coverage)
- **Every handler** has unit tests: happy path + every error branch
- **Every validator** has unit tests: valid input, each invalid field,
  boundary values, combination errors
- **Every domain entity** has unit tests: creation, state transitions,
  business rules, invariant enforcement
- **Every endpoint** has integration tests: success response, auth required,
  forbidden for wrong role, validation error response, not found
- **Architecture tests**: enforce naming conventions, folder structure,
  DI registration completeness, no direct DbContext usage in services
- **Coverage target**: 100% line coverage on Application + Domain layers.
  No untested handler. No untested validator. No untested entity method.
- `dotnet test src/NOIR.sln` → ALL pass, zero skipped, zero ignored

### Frontend: Type Safety as Test Suite
- `pnpm run build` in strict mode = compile-time verification of all components
- Zero `any` types, zero `@ts-ignore`, zero suppressed warnings
- Type-safe API contracts: if backend changes a DTO, frontend build breaks
  immediately — that's the design

### E2E: Playwright MCP — Claude Tests Like a Human (CRITICAL)

Spawn a dedicated **QA Agent** that uses Playwright MCP tools DIRECTLY —
not writing test files, but navigating the live application with Claude's
own intelligence, thinking about what it sees, and verifying correctness.

**How the QA Agent works:**
1. Start the application (backend + frontend)
2. Use Playwright MCP tools: `browser_navigate`, `browser_click`,
   `browser_fill`, `browser_snapshot`, `browser_take_screenshot`
3. THINK about what's on screen — read the DOM snapshot, evaluate if
   the UI is correct, check data integrity
4. Report issues with screenshots and detailed description

**The QA Agent MUST test every single one of these for EVERY feature:**

**Authentication & Authorization:**
- Login as Platform Admin → verify sees all tenants' data
- Login as Tenant Admin → verify sees only own tenant's data
- Access denied pages → verify proper 403 handling
- Session expiry → verify redirect to login

**CRUD Operations (for EVERY entity):**
- **Create**: open dialog/page → fill all fields → submit → verify success
  toast → verify item appears in list with correct data
- **Read**: verify list shows correct columns, data, formatting (VND, dates)
- **Update**: click edit → verify form pre-filled → modify fields → submit →
  verify changes reflected in list AND detail view
- **Delete**: click delete → verify confirmation dialog appears → confirm →
  verify item removed from list → verify soft delete (not hard delete)

**Form Validation (for EVERY form):**
- Submit empty form → verify all required field errors appear
- Submit invalid data (wrong format, too long, too short) → verify
  specific error messages per field
- Fix errors one by one → verify errors clear as fields become valid
- Verify validation triggers on blur (not on every keystroke)

**Dialogs & Popups:**
- Open dialog → verify it renders correctly, focus trapped inside
- Close via X button → verify dialog closes, no data saved
- Close via Escape key → verify same behavior
- Close via clicking overlay → verify same behavior
- Submit dialog → verify loading state on button → verify success

**Tables & Lists:**
- Search: type in search box → verify results filter in real-time
  (with deferred value, not janky)
- Filter: apply filter → verify table updates → clear filter → verify reset
- Sort: click column header → verify sort direction → click again → reverse
- Pagination: navigate pages → verify correct data per page → verify
  page count updates with filters
- Bulk select: select multiple → verify bulk action bar appears →
  execute bulk action → verify all selected items affected
- Export: click export → verify file downloads with correct data
- Empty state: clear all data → verify illustrated empty state, not blank page

**Workflows (multi-step processes):**
- Order lifecycle: create order → confirm → process → ship → deliver →
  complete. Verify status changes at each step, verify timeline updates.
- Checkout flow: add to cart → initiate checkout → set address →
  select shipping → select payment → complete → verify order created
- Inventory: create stock-in receipt → confirm → verify stock levels
  updated on product

**Cross-Feature Data Integrity:**
- Create a product → verify it appears in inventory view
- Create an order → verify customer's order history updates
- Delete a category → verify products in that category handle gracefully
- Apply a voucher to order → verify discount calculated correctly
- Change product price → verify existing orders keep original price

**Page Transitions & Navigation:**
- Navigate between all pages via sidebar → verify smooth transitions
  (View Transitions API, no full page reloads)
- Use browser back/forward → verify correct page state restoration
- Deep link to specific page → verify it loads correctly
- Breadcrumb navigation → verify correct hierarchy and links

**Localization:**
- Switch language EN → VI → verify ALL text changes, no untranslated strings
- Verify VND formatting (1.000.000₫)
- Verify date formatting (DD/MM/YYYY in VI)
- Verify form validation messages appear in selected language

**Responsive & Visual:**
- Take screenshots at desktop (1920x1080) and tablet (768x1024) viewports
- Verify no horizontal scrolling, no overlapping elements, no cut-off text
- Verify interactive elements have cursor-pointer

**Error Handling:**
- Trigger API error (e.g., network off) → verify error toast/message
- Submit duplicate data → verify backend validation error displayed correctly
- Access non-existent route → verify 404 page

**The QA Agent must produce a report:**
```
Feature: [Name]
Pages tested: [count]
CRUD verified: ✅/❌ (Create/Read/Update/Delete)
Validation verified: ✅/❌
Dialogs verified: ✅/❌
Workflows verified: ✅/❌
Cross-feature verified: ✅/❌
Localization verified: ✅/❌
Screenshots: [attached]
Issues found: [list with screenshots]
```

**IMPORTANT**: The QA Agent does NOT write .spec.ts files. It uses Playwright
MCP tools directly, applying Claude's reasoning to evaluate what it sees.
This is smarter than scripted tests because Claude can catch visual issues,
UX problems, and logical inconsistencies that scripts cannot.

## Vietnam Market Context
- Currency: VND (no decimals, 1.000.000₫)
- Phone: +84 format
- Address hierarchy: Tỉnh/TP → Quận/Huyện → Phường/Xã → Chi tiết
- Tax: VAT 8%/10%
- Date: DD/MM/YYYY
- Carriers: GHN, GHTK, VNPost, J&T
- Payments: VNPay, MoMo, ZaloPay, COD
- Language: VI primary, EN secondary

## Rules
- Follow ALL rules in CLAUDE.md
- When unsure about a pattern, check existing code first — consistency > creativity
- Don't over-engineer. Don't under-engineer. Engineer exactly right.
- Ship working increments. Every phase must be deployable.
- You decide the features. You decide the phases. You decide the team size.
  I trust your judgment. Just make it the best.
