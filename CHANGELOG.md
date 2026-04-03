# CLAUDE.md Changelog

## [4.0] - 2026-04-03

### Added
- **RichTextEditor UIKit component**: Tiptap v3 headless editor with `full`/`minimal` presets, image upload, variable insertion, dark mode, Storybook stories
- **Memory: library update lessons**: MimeKit CVE needs latest version, Tiptap v3 named exports, safe major bumps (lucide, Wolverine, Stripe)

### Changed
- **TinyMCE → Tiptap migration**: Replaced TinyMCE 6 (EOL) with Tiptap 3 (MIT) across 4 pages (Blog, Product, Legal, Email Template)
- **Bundle size**: vendor-tinymce 1,263KB → vendor-tiptap 433KB (−69% gzipped)
- **Backend packages**: Microsoft.* 10.0.5, WolverineFx 5.27, Stripe 51, FusionCache 2.6, Hangfire 1.8.23, Finbuckle 10.0.4, and 20+ more
- **Frontend packages**: 30+ patches/minors, Tailwind 4.2.2, lucide-react 1.7.0, Storybook 10.3.4, Vitest 4.1.2
- **UIKit component count**: 98 → 99 (updated across all docs)
- **All TinyMCE references** updated to Tiptap/RichTextEditor in docs, rules, e2e tests

### Fixed
- **MimeKit CVE** (GHSA-g7hc-96xr-gvvx): Override transitive dep to 4.15.1 — 0 NuGet warnings

### Removed
- `tinymce` + `@tinymce/tinymce-react` packages
- `public/tinymce/` self-hosted skins/icons/models directory (78 files)
- TinyMCE CSS styles from `index.css`

## [3.7] - 2026-03-13

### Added
- **Enterprise DataTable**: `useEnterpriseTable` unified hook replacing `useServerTable` — density toggle, drag-reorder, pinning, resizing, settings persistence to localStorage
- **Enterprise DataTable build spec**: `docs/designs/enterprise-datatable-build-spec.md` — Phase 1-2 complete, Phases 3-5 future
- **DataTableColumnsDropdown**: New Popover-based column management with @dnd-kit drag-reorder, sort controls, visibility toggle, group-by, reset to default
- **Hooks reference**: Added `useEnterpriseTable`, `useTableParams`, `useVirtualTableRows`, `useApiKeys` to docs

### Changed
- Hook count: 50 → 41 (corrected — old count included test files; `useServerTable` deleted, `useEnterpriseTable` + `useVirtualTableRows` + `useApiKeys` added)
- All 24 production table pages migrated to `useEnterpriseTable` (verified via Playwright)
- Removed legacy `onResetColumnVisibility` prop from `DataTableToolbar` and `DataTableColumnsDropdown`
- Updated `.claude/rules/datatable-standard.md`, `datatable-migration.md`, `table-list-standard.md` to reference `useEnterpriseTable`
- Updated `docs/frontend/architecture.md` DataTable section with enterprise pattern
- Updated all documentation numbers (README, CLAUDE.md, hooks-reference, KNOWLEDGE_BASE, DOCUMENTATION_INDEX)

### Fixed
- **Products page TDZ crash**: `selectedDraftCount`/`selectedActiveCount` computed before `selectedIdsSet` declaration — moved after declaration

### Removed
- `useServerTable` hook (replaced by `useEnterpriseTable`)
- `onResetColumnVisibility` prop from DataTable components (superseded by `onResetSettings`)

## [3.6] - 2026-03-12

### Added
- **UI/UX audit automation**: Playwright-based system (52 pages, 11 custom rules + axe-core) in `e2e/tests/ui-audit/`
- **WCAG 2.1 AA compliance**: 182 accessibility violations fixed (contrast, aria, keyboard nav) — 0 fixable issues remain
- **MCP Server expansion**: 55 tools (was 28), 6 prompts, 7 resources via Streamable HTTP
- **Rule 31**: UI audit automation command reference

### Changed
- Test count: 12,663 → 12,715 backend (2,971 domain + 8,557 application + 46 architecture + 1,141 integration) + 831 frontend (154 unit + 677 storybook) = **13,546 total**
- Module count: 33 → 35 definitions (8 core + 27 toggleable)
- Hook count: 35 → 50 custom hooks
- UIKit stories: 97 → 98
- Darkened `--destructive` CSS variable for WCAG AA white-text contrast
- Updated all documentation numbers (README, AGENTS.md, CLAUDE.md)

### Removed
- `docs/frontend/audit-storybook-coverage.md` — superseded by automated UI audit system
- `docs/frontend/patterns/form-resolver-type-assertions.md` — completed standardization

## [3.5] - 2026-03-02

### Added
- **CI/CD pipeline**: GitHub Actions with build, unit tests (parallel), integration tests, frontend build + Storybook
- **Comprehensive test suite**: 80+ new unit tests (CRM, PM, HR, Dashboard, Blog, Media), 20 integration tests
- **Playwright E2E tests**: Full E2E suite with page objects, auth fixtures, 14 test modules
- **Wiki sync improvements**: Graceful handling of uninitialized wiki, workflow_dispatch trigger
- **EmployeeTagRepository**: Production repository implementation (previously only in test infra)

### Fixed
- State machine guards in CRM Lead handlers (Win/Lose/Reopen) and PM ChangeTaskStatus
- PM ReorderTask handler and validator for float-based sort ordering
- Missing SQL semicolons in EmployeeCodeGenerator and ProjectCodeGenerator
- CI integration tests OOM: replaced Testcontainers with GitHub Actions SQL Server service container
- Dropdown menu event propagation: added `stopPropagation()` on delete/action items across 5 pages
- TagChips null safety: guard against undefined `tags` prop
- Employee deactivation status: `Suspended` → `Resigned` (correct domain value)
- E2E tests: hardened selectors, improved page objects, removed debug spec

### Changed
- Test count: 11,341 → 12,663 (2,971 domain + 8,535 application + 45 architecture + 1,112 integration)

## [3.4] - 2026-02-28

### Changed
- **Documentation overhaul**: Fixed all stale docs, removed references to 8 deleted files
- **docs/README.md**: Rewritten with accurate structure tree (~55 files), removed all dead links
- **docs/DOCUMENTATION_INDEX.md**: v3.8 — added all 13 design docs + roadmap section
- **SETUP.md**: Fixed test count (6,750 → 11,341+), fixed git clone URLs (yourusername → NOIR-Solution)
- **README.md**: Added "100% AI-Coded" badge, AI attribution, cleaned emoji headers, added roadmap link
- **diagrams.md**: Fixed EF Core version (8 → 10), updated date

### Added
- **docs/roadmap/README.md**: Product roadmap with 5 phases (Core Foundation → Marketplace & Ecosystem)
- **.github/workflows/sync-wiki.yml**: Auto-sync docs/ to GitHub Wiki on push to main (with _Sidebar.md + Home.md)
- Roadmap reference in CLAUDE.md Documentation section

## [3.1.1] - 2026-02-26

### Changed
- Replaced ShieldCheck icon with NOIR orbital logo mark (3 concentric animated circles) across all pages: landing page, login, forgot-password, reset-password, verify-OTP, auth-success, portal sidebar
- Logo uses `currentColor` + `text-primary` on light backgrounds (adapts to light/dark mode) and `stroke="white"` on dark gradient panels
- Updated `public/favicon.svg` to orbital mark (`#6366F1`)

### Added
- CSS `.orbital-animated` keyframes in `index.css` (12s/8s/5s speeds, middle ring counter-clockwise)
- `public/manifest.json` for PWA support
- PWA meta tags in `index.html` (theme-color, apple-mobile-web-app-*, viewport-fit=cover)
- `brand/noir-brandkit.html` — NOIR brand kit adapted from Top-Life with indigo color scheme

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
