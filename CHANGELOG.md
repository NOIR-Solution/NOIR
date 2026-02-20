# CLAUDE.md Changelog

### Version 3.0 (2026-02-21)
- **Rewrote:** Full rewrite from v2.8 (cut 2,390→272 lines, 88% reduction)
- **Deleted:** `.claude/rules/superclaude-routing.md` (180 lines, skills self-route)
- **Deleted:** `.claude/rules/ui-tool-routing.md` (150 lines, redundant with skills)
- **Merged:** Code patterns into inline examples within rules
- **Collapsed:** E-commerce patterns into domain map table
- **Added:** Permission localization utility + 60+ EN/VI translation keys
- **Fixed:** Portable paths (removed hardcoded `D:\GIT\TOP\NOIR`)

### Version 2.8 (2026-02-21)
- **Audit:** Full CLAUDE.md + rules + prompts quality audit (8 files, 2390 lines)
- **Deleted:** `.claude/rules/ui-tool-routing.md` and `.claude/rules/superclaude-routing.md`
- **Fixed:** Story count 58→72 (verified), AGENTS.md test count 6,750+→10,595+
- **Moved:** Changelog to separate CHANGELOG.md (~56 lines saved from context)
- **Removed:** Decorative HTML header/footer (~23 lines saved)
- **Collapsed:** UI Component Building section (30→3 lines)
- **Created:** `.claude/rules/cto-team.md` (CTO Thinking Mode)
- **Created:** `.claude/rules/team-coordination.md` (shared team boilerplate)
- **Slimmed:** AGENTS.md to unique content + reference to CLAUDE.md
- **Deduplicated:** Prompt files reference shared team-coordination.md

### Version 2.7 (2026-02-18)
- **Updated:** UIKit component count from 56 to 58 (verified at time of release)
- **Audited:** Full documentation inventory (56 files across docs/, root, and .claude/rules/)
- **Updated:** Dependency versions to match actual: TypeScript 5.9.3, Vite 7.3.0, Zod 4.3.5, React Router 7.11.0
- **Fixed:** Dead links in testing README (removed references to non-existent files)
- **Updated:** docs/README.md file counts and structure tree to match reality

### Version 2.6 (2026-02-15)
- **Added:** React 19 + TanStack Query performance patterns (useDeferredValue, useTransition, optimistic mutations)
- **Added:** `useOptimisticMutation` shared utility documentation
- **Updated:** TanStack Query hooks pattern with domain-scoped query/mutation structure

### Version 2.5 (2026-02-13)
- **Added:** Storybook v10.2.8 with 58 component stories in `src/uikit/`
- **Added:** UIKit structure documentation and `@uikit` path alias
- **Migrated:** npm → pnpm for disk-optimized dependency management

### Version 2.4 (2026-02-10)
- **BREAKING:** Removed entire E2E testing infrastructure (Playwright, 490+ tests, 100 files)
- **Focus:** Backend testing only (10,595+ xUnit tests: domain, application, integration, architecture)

### Version 2.3 (2026-02-09)
- **Standardized:** Form resolver pattern across 13 files (`as unknown as Resolver<T>`)

### Version 2.2 (2026-02-08)
- **Updated:** Test count to 10,595+ (verified: 842 domain + 5,231 application + 654 integration + 25 architecture)

### Version 2.1 (2026-01-29)
- **Added:** Product Attribute System patterns (13 attribute types)

### Version 2.0 (2026-01-26)
- **Fixed:** Rule numbering, added ToC, E-commerce Patterns, TanStack Query hooks, version tracking

### Version 1.0 (Initial)
- Original CLAUDE.md with 22 critical rules, backend patterns, frontend rules
