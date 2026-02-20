# NOIR — Best-in-Class E-commerce Admin Portal

## Primary Goals (All 5 Must Be Achieved)

### Goal 1: UI/UX Consistency — One Design Language
Every page, component, dialog, table, card, and form MUST follow ONE unified design language.

**Calibration:** Payment Provider config page = gold standard. When two pages serve the same purpose but look different, unify to match the best one.

**Pattern categories to unify:**
Cards, Tables, Dialogs, Forms, Buttons, Spacing, Typography, Colors, Shadows, Animations,
Empty states, Loading states, Toasts, Status badges, Config/Settings pages.

### Goal 2: Storybook 100% Sync
Every UI component used in the website MUST have a Storybook story showing all variants, states, and sizes.
- Story location: `src/uikit/{component-name}/{Component}.stories.tsx`
- Cross-reference `src/components/ui/` vs `src/uikit/` vs app imports
- Missing stories → create. Outdated stories → update. `pnpm build-storybook` → 0 errors.

### Goal 3: Backend Consistency — Same Pattern Everywhere
Every feature MUST follow the EXACT same code patterns. Audit and unify:
Handler structure, Specifications, Folder structure, Error handling, Audit commands, Validators,
DI registration, Entity configuration, Repository pattern, DTOs, Endpoint structure, Dead code.

### Goal 4: Fill All Test Coverage Gaps
Audit existing tests (10,595+), find what's NOT covered, fill every gap.
Phase 0 auditor MUST produce a **Test Gap Report**.

### Goal 5: Feature Completeness — Match Top E-commerce Platforms
Research: Shopify Admin, Shopee Seller Center, Haravan, Sapo, WooCommerce, Medusa.js, Saleor.
**Scope per round:** identify top 10 missing features, prioritize top 3-5 for implementation.

---

## Execution: See `.claude/rules/team-coordination.md`

### Team Patterns (Pick Per Phase)

| Phase Type | Min Agents | Recommended Roles |
|------------|-----------|-------------------|
| Research/Audit | 2-3 | `codebase-auditor` + `ui-explorer` (+ `ecommerce-researcher`) |
| UI Consistency | 2-3 | `pattern-researcher` + `component-fixer` (+ `storybook-updater`) |
| Backend Feature | 2-3 | `backend-dev` + `test-writer` (+ `migration-handler`) |
| Frontend Feature | 2-3 | `frontend-dev` + `storybook-writer` (+ `localization`) |
| Full-Stack Feature | 3 | `backend-dev` + `frontend-dev` + `test-writer` |
| QA Round | 2 | `qa-visual` + `qa-functional` |

---

## Phased Execution

### Phase 0: Research & Audit (ALWAYS START HERE)

**Prerequisites:** Start the app with `./start-dev.sh` before this phase.

**Team:** `codebase-auditor` + `ui-explorer` + `ecommerce-researcher`

**codebase-auditor:** Map every feature, entity, handler, endpoint. Scan test coverage per feature. Produce **Feature Inventory Matrix** + **Storybook Coverage Report**.

**ui-explorer:** Navigate EVERY page via Playwright MCP, screenshot at 1920x1080. Produce **UI Pattern Matrix** highlighting mismatches.

**ecommerce-researcher:** Research competitors, produce **Feature Gap Analysis** + **UI/UX Research Report**.

**You (Team Lead):** Synthesize reports into prioritized roadmap. Present to user for approval.

### Phase 1-N: Build (You Decide Scope Per Phase)

Suggested ordering: UI Consistency → Storybook Sync → Backend Consistency → Test Coverage → New Features → Final QA.

---

## QA: Playwright MCP Testing

Spawn QA agent to navigate live app via Playwright MCP tools DIRECTLY.

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
- Ship working increments. Every phase must pass ALL quality gates.

## Done Criteria

```
✅ Goal 1: Every page follows ONE consistent design language
✅ Goal 2: Every component has a Storybook story, pnpm build-storybook passes
✅ Goal 3: Every backend feature follows exact same patterns
✅ Goal 4: 100% test coverage for handlers, validators, entities, endpoints
✅ Goal 5: Feature set matches top e-commerce platforms for VN market
✅ All quality gates pass
✅ All text localized EN + VI
```
