# Real QA Lessons — 2026-03-21

## Summary
- **Pages tested**: 32 (all sidebar list pages + order detail)
- **Bugs found**: 2 (0 critical, 1 high, 1 medium)
- **Bugs fixed**: 2
- **Sessions**: 6 (across same day)
- **Build verification**: 12,699 backend tests passing, frontend build clean

## Bug Pattern Analysis

### Translation (1 bug — HIGH)
**BUG-001**: Audit activity description showed raw PascalCase enum "InProgress" instead of translated "Đang làm" in Vietnamese mode.
- **Root cause**: `translateAuditDescription()` passed status values through without translation. The `STATUS_KEYS` map initially used wrong i18n paths (`pm.statuses.*` instead of top-level `statuses.*`).
- **Prevention**: When adding `translateStatus()`-style helpers, always verify the actual i18n JSON key path by checking the translation file structure. The `pm` section has its own nested structure, but `statuses` is top-level.

### Visual/Layout (1 bug — MEDIUM)
**BUG-002**: Dashboard Quick Action card labels truncated ("Pending Or...", "Low Stock ...") at all viewport sizes.
- **Root cause**: `truncate` CSS class on `<p>` labels combined with narrow card width in 2-column grid.
- **Prevention**: For short text labels (1-3 words), prefer `leading-tight` over `truncate`. Truncation is appropriate for long dynamic text (descriptions, names), not for fixed UI labels.

## High-Risk Areas

1. **Dashboard** — Only page where bugs were found. Complex widget layout with many data sources.
2. **Audit description translation** — Backend stores English descriptions, frontend must pattern-match and translate. Any new audit format requires updating `auditDescriptionTranslator.ts`.
3. **i18n key path resolution** — The translation JSON structure has nested namespaces (`pm.statuses` vs top-level `statuses`). Easy to reference wrong path.

## Root Cause Analysis — Why Automated Tests Missed These

1. **BUG-001**: Unit tests verify the backend stores correct descriptions and the translator matches patterns, but don't verify the i18n key paths resolve to actual translations in both locales. The key path was syntactically valid but pointed to a non-existent path.
2. **BUG-002**: CSS truncation is invisible to automated tests — they check text content, not whether it's visually truncated. Only visual inspection (screenshot) or pixel-based tests would catch this.

## Prevention Recommendations

1. **Add i18n key validation test**: A test that loads both EN and VI translation files, iterates all keys referenced in `auditDescriptionTranslator.ts` STATUS_KEYS/ENTITY_TYPE_KEYS, and verifies each resolves to a non-empty string in both locales.
2. **Visual regression testing**: For dashboard widgets with fixed labels, add Playwright visual snapshot tests that catch text truncation.
3. **Translation review workflow**: When adding new `translateX()` helpers, always verify with `node -e "require('./public/locales/vi/common.json').path.to.key"` before committing.

## Quality Observations

The application demonstrated **exceptional quality** across 32 pages:
- **100% consistent DataTable implementation** across all list pages
- **100% EmptyState component usage** for empty tables (no plain text)
- **100% Vietnamese sidebar translation** (32 items, no English mixing)
- **100% status badge color consistency** across all modules
- **Proper dark mode support** on all tested pages (6 pages verified)
- **Clean responsive behavior** at 768px on all tested pages
- **No CRITICAL bugs** found across the entire application
