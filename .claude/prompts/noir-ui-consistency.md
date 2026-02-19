# NOIR — UI/UX Consistency & Storybook Sync

## Primary Goals (Measurable Deliverables)

### Goal 1: UI/UX Consistency — One Design Language Across ALL Pages
Every page, component, dialog, table, card, and form in the application MUST follow
ONE unified design language. No page should look like it was built by a different team.

**How to achieve:**
1. Audit every page → screenshot + catalog every UI pattern used
2. For each pattern category (cards, tables, dialogs, forms, buttons, spacing, shadows,
   colors, typography, animations), identify ALL variants currently in the codebase
3. Pick the BEST variant (either best existing OR research top admin portals and adopt better)
4. Apply the chosen pattern to EVERY instance across ALL pages
5. Verify visually: every page should feel like ONE product built by ONE designer

**Known mismatch example (use as reference for what to look for):**
> **Payment Provider config vs Shipping Provider config** — these two settings pages
> serve the same purpose (configure external providers) but look completely different.
> The **Payment Provider page is the gold standard** — its layout, card style, toggle UX,
> form structure, and overall feel is the best in the app. The Shipping Provider page
> (and likely other similar config pages) must be updated to match Payment Provider's
> design language exactly. Use this as your calibration: when you find two pages that
> do the same thing but look different, the Payment Provider pattern wins.

**Pattern categories to unify:**
| Category | What to Check |
|----------|--------------|
| Cards | Shadow, border-radius, padding, hover effect, header style |
| Tables | Header style, row height, hover, striped/plain, empty state |
| Dialogs | Width, padding, header, footer buttons, close behavior |
| Forms | Label position, input height, spacing, error display, focus ring |
| Buttons | Sizes, variants, icon placement, loading state, cursor-pointer |
| Spacing | Page padding, section gaps, card gaps, form field gaps |
| Typography | Heading sizes, body text, label text, monospace usage |
| Colors | Primary, secondary, destructive, muted, border, background |
| Shadows | Card shadow, dropdown shadow, dialog shadow, hover shadow |
| Animations | Transition duration, easing, hover effects, page transitions |
| Empty states | Icon, message, action button pattern |
| Loading states | Skeleton shape, shimmer style, spinner usage |
| Toast/Notifications | Position, duration, icon, color coding |
| Status badges | Colors for each status, pill vs tag, size |
| Config/Settings pages | Provider cards, toggle switches, form layout (Payment Provider = gold standard) |

### Goal 2: Storybook 100% Sync — Every Component Has a Story
Every UI component used anywhere in the website MUST have a corresponding
Storybook story that showcases all its variants, states, and sizes.

**How to achieve:**
1. Inventory all components in `src/components/ui/` and `src/uikit/`
2. Inventory all components actually USED in `src/portal-app/` and `src/layouts/`
3. Cross-reference: find components WITHOUT stories, find stories that are OUTDATED
4. For missing stories: create them following existing story patterns in `src/uikit/`
5. For outdated stories: update to reflect current component API and variants
6. Final check: `pnpm storybook` → navigate every story → verify renders correctly

**Storybook standards:**
- Story location: `src/uikit/{component-name}/{Component}.stories.tsx`
- Every story MUST show: default state, all variants, all sizes, disabled state, loading state (if applicable)
- Interactive stories for: forms, dialogs, dropdowns, tables
- Stories MUST use current component props (no stale/removed props)
- `pnpm build-storybook` → 0 errors, 0 warnings

---

## Execution Method: Agent Team (NON-NEGOTIABLE)

**HARD RULE: You MUST use TeamCreate + Task tool to spawn agent teammates.**
- Minimum 2 teammates per phase. No exceptions.
- You are the **Team Lead / Coordinator**. You do NOT write code yourself.
- Your job: plan phases, spawn teammates, assign tasks, review results, enforce quality gates.
- If you catch yourself writing code directly instead of delegating → STOP → spawn a teammate.

### Team Patterns (Pick Per Phase)

| Phase Type | Min Agents | Recommended Roles |
|------------|-----------|-------------------|
| Research/Audit | 2 | `ui-auditor` + `storybook-auditor` |
| UI Consistency | 2-3 | `pattern-researcher` + `component-fixer` (+ `page-fixer`) |
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
- You run quality gates yourself

---

## Phased Execution

### Phase 0: Audit (ALWAYS START HERE)

**Prerequisites:** Start the app with `./start-dev.sh` before this phase.

**Team:** `ui-auditor` + `storybook-auditor`

**ui-auditor tasks:**
- Use Playwright MCP to navigate EVERY page in the live app
- Screenshot every page at desktop viewport (1920x1080)
- For each page, catalog: card style, table style, dialog style, spacing, shadows,
  typography, colors, animations, button variants, form patterns
- Produce a **UI Pattern Matrix**: rows = pages, columns = pattern categories,
  cells = which variant is used
- Highlight inconsistencies: "Products page uses shadow-sm cards, Orders page uses shadow-md cards"
- Recommend: for each category, which variant should be the standard (best existing or researched)

**storybook-auditor tasks:**
- List all components in `src/components/ui/` (the actual components)
- List all stories in `src/uikit/` (the Storybook stories)
- List all component imports across `src/portal-app/` and `src/layouts/`
- Cross-reference and produce:
  - Components WITH stories (and whether stories are current)
  - Components WITHOUT stories (missing coverage)
  - Stories that reference removed/changed props (outdated)
  - Components used in app but not in any story (gap)
- Produce: **Storybook Coverage Report** with specific file paths

**You (Team Lead) then:**
- Combine both reports into a unified action plan:
  - List of UI patterns to standardize (with chosen standard for each)
  - List of Storybook gaps to fill
  - Priority order (high-traffic pages first)
- Present to user for approval before Phase 1

### Phase 1: Establish Design Standards

**Team:** `pattern-researcher` + `design-documenter`

**pattern-researcher tasks:**
- For any pattern where current codebase has no good reference,
  research top admin portals (Shopify Admin, Stripe Dashboard, Linear, Vercel Dashboard)
- Use `/ui-ux-pro-max` skill for design research and recommendations
- For each pattern category, produce: chosen standard with rationale
- Include specific Tailwind classes, spacing values, shadow values, color tokens

**design-documenter tasks:**
- Document the chosen standards in a concise reference (e.g., `docs/frontend/design-standards.md`)
- Include code snippets for each standard pattern
- This document becomes the single source of truth for all subsequent phases

### Phase 2-N: Apply Consistency (Group by Pattern Category or by Page Group)

**Team options:**
- By pattern: `card-fixer` + `table-fixer` (2 agents fix different categories in parallel)
- By page group: `products-pages-fixer` + `settings-pages-fixer` (2 agents fix different sections)
- Always include `storybook-updater` if components are being modified

**For each component/page touched:**
1. Apply the design standard from Phase 1
2. If the component is in `src/components/ui/`: update the story in `src/uikit/`
3. If a new shared component is created: create a new story
4. Verify: `pnpm run build` passes after changes
5. Verify: `pnpm build-storybook` passes after story changes

### Final Phase: Verification

**Team:** `qa-visual` + `storybook-verifier`

**qa-visual tasks:**
- Use Playwright MCP to re-screenshot every page
- Compare before/after: confirm all pages now use consistent patterns
- Check no regressions: nothing broken, no missing elements, no layout shifts
- Produce: visual comparison report

**storybook-verifier tasks:**
- Run `pnpm build-storybook` → 0 errors
- Navigate every story in Storybook → verify renders correctly
- Confirm 100% coverage: every component used in app has a story
- Produce: final Storybook coverage percentage

---

## Quality Gates (Run After EVERY Phase)

```bash
dotnet build src/NOIR.sln                              # 0 errors
dotnet test src/NOIR.sln                               # ALL pass
cd src/NOIR.Web/frontend && pnpm run build             # 0 errors, 0 warnings (strict)
cd src/NOIR.Web/frontend && pnpm build-storybook       # 0 errors, 0 warnings
```

**Additional consistency checks:**
- No hardcoded strings (all text uses `t('key')` from react-i18next)
- All interactive elements have `cursor-pointer`
- All icon-only buttons have `aria-label`
- All destructive actions have confirmation dialogs

---

## Architecture Philosophy

### Frontend: Shared & Composable
- Feature modules are self-contained: each `portal-app/{feature}/` owns its pages, queries, mutations
- Shared hooks eliminate repetition: ONE hook for search + filter + paginate + sort
- Shared table/list pattern: standard wrapper for columns, search, filter, sort, pagination
- Shared form pattern: Zod schema + react-hook-form = define schema + define fields
- Type-safe end-to-end: Backend DTO → API response → Frontend type → Form schema → UI

### Storybook as Living Documentation
- Storybook is NOT optional — it IS the component documentation
- Every component change MUST include a story update
- Stories show all variants, not just the default
- `pnpm storybook` at http://localhost:6006 is the component reference for the team

> For full backend + frontend architecture, see `/noir-ecommerce-team`.

---

## Localization Context

- **Language:** VI primary, EN secondary — all text must use `t('key')` from react-i18next
- **Currency display:** VND (1.000.000₫) — verify formatting consistency
- **Date display:** DD/MM/YYYY in VI — verify formatting consistency

---

## Rules

- Follow ALL rules in CLAUDE.md — no exceptions
- When unsure about a pattern, check existing code first — consistency > creativity
- Don't over-engineer. Don't under-engineer. Engineer exactly right.
- Ship working increments. Every phase must pass ALL quality gates.
- You decide the grouping and team size per phase. I trust your judgment.

---

## Reminder: You Are The Coordinator

```
❌ WRONG: Reading files yourself, writing code, making changes directly
✅ RIGHT: Spawning teammates, assigning tasks, reviewing results, running quality gates

If you find yourself editing a file → STOP → delegate to a teammate.
The only commands you run directly are: build, test, storybook build, and quality gate checks.
```

## Done Criteria (How to Know You're Finished)

```
✅ Every page in the app follows ONE consistent design language
✅ UI Pattern Matrix shows 100% alignment across all pages
✅ Every component in src/components/ui/ has a story in src/uikit/
✅ Every story renders correctly and shows all variants
✅ pnpm build-storybook → 0 errors, 0 warnings
✅ pnpm run build → 0 errors, 0 warnings
✅ dotnet build + dotnet test → 0 errors, all pass
✅ No hardcoded strings, all localized EN + VI
✅ Before/after screenshots show clear improvement
```
