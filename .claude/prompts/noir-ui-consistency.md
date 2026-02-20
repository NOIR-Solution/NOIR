# NOIR — UI/UX Consistency & Storybook Sync

## Primary Goals

### Goal 1: UI/UX Consistency — One Design Language Across ALL Pages

**Calibration:** Payment Provider config page = gold standard. When two pages serve the same purpose but look different, unify to match the best one.

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
| Config/Settings pages | Provider cards, toggle switches, form layout |

### Goal 2: Storybook 100% Sync

Every UI component used in the website MUST have a Storybook story.
- Story location: `src/uikit/{component-name}/{Component}.stories.tsx`
- Every story MUST show: default state, all variants, all sizes, disabled state, loading state (if applicable)
- `pnpm build-storybook` → 0 errors, 0 warnings

---

## Execution: See `.claude/rules/team-coordination.md`

### Team Patterns (Pick Per Phase)

| Phase Type | Min Agents | Recommended Roles |
|------------|-----------|-------------------|
| Research/Audit | 2 | `ui-auditor` + `storybook-auditor` |
| UI Consistency | 2-3 | `pattern-researcher` + `component-fixer` (+ `page-fixer`) |
| Storybook Sync | 2 | `story-writer` + `story-reviewer` |
| QA Round | 2 | `qa-visual` + `qa-functional` |

---

## Phased Execution

### Phase 0: Audit (ALWAYS START HERE)

**Prerequisites:** Start the app with `./start-dev.sh`.

**ui-auditor:** Navigate EVERY page via Playwright MCP, screenshot at 1920x1080. Produce **UI Pattern Matrix** (rows=pages, columns=pattern categories, cells=which variant). Highlight inconsistencies and recommend standards.

**storybook-auditor:** Cross-reference `src/uikit/` vs app imports. Produce **Storybook Coverage Report** with specific file paths.

**You (Team Lead):** Combine into unified action plan, present to user.

### Phase 1: Establish Design Standards

**pattern-researcher** + **design-documenter**: Research top admin portals (Shopify, Stripe, Linear), use `/ui-ux-pro-max`, produce standards with specific Tailwind classes, spacing values, color tokens. Document in `docs/frontend/design-standards.md`.

### Phase 2-N: Apply Consistency

Group by pattern category or by page group. Always include `storybook-updater` if components are modified.

### Final Phase: Verification

**qa-visual:** Re-screenshot every page, compare before/after, check no regressions.
**storybook-verifier:** `pnpm build-storybook` → 0 errors. Navigate every story. Confirm 100% coverage.

---

## Rules

- Follow ALL rules in CLAUDE.md — no exceptions
- When unsure about a pattern, check existing code first — consistency > creativity
- Ship working increments. Every phase must pass ALL quality gates.

## Done Criteria

```
✅ Every page follows ONE consistent design language
✅ UI Pattern Matrix shows 100% alignment
✅ Every component has a story, pnpm build-storybook passes
✅ pnpm run build → 0 errors, 0 warnings
✅ dotnet build + dotnet test → 0 errors, all pass
✅ No hardcoded strings, all localized EN + VI
✅ Before/after screenshots show clear improvement
```
