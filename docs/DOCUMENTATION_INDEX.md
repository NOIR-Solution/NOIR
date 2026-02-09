# NOIR Documentation Index

> Your guide to navigating all NOIR documentation.

**Last Updated:** 2026-02-08

---

## Quick Start

| I Want To... | Go To |
|--------------|-------|
| **Understand the codebase** | [KNOWLEDGE_BASE.md](KNOWLEDGE_BASE.md) |
| **Navigate project structure** | [PROJECT_INDEX.md](PROJECT_INDEX.md) |
| **Find a feature** | [FEATURE_CATALOG.md](FEATURE_CATALOG.md) |
| **Learn technologies** | [TECH_STACK.md](TECH_STACK.md) |
| **See API endpoints** | [API_INDEX.md](API_INDEX.md) |
| **Understand architecture** | [ARCHITECTURE.md](ARCHITECTURE.md) |
| **Run E2E tests** | [testing/E2E-TESTING-GUIDE.md](testing/E2E-TESTING-GUIDE.md) |
| **Test visual changes** | [testing/VISUAL-REGRESSION-TESTING.md](testing/VISUAL-REGRESSION-TESTING.md) |
| **Test accessibility** | [testing/ACCESSIBILITY-TESTING.md](testing/ACCESSIBILITY-TESTING.md) |
| **View test cases** | [testing/TEST_CASES.md](testing/TEST_CASES.md) |

---

## Core Documentation

| Document | Purpose | Size |
|----------|---------|------|
| [KNOWLEDGE_BASE.md](KNOWLEDGE_BASE.md) | Comprehensive codebase reference | ~500 lines |
| [PROJECT_INDEX.md](PROJECT_INDEX.md) | Project navigation and structure | ~1000 lines |
| [FEATURE_CATALOG.md](FEATURE_CATALOG.md) | All features, commands, endpoints | ~1100 lines |
| [TECH_STACK.md](TECH_STACK.md) | Technology stack reference | ~750 lines |
| [API_INDEX.md](API_INDEX.md) | REST API documentation | Reference |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Architecture overview | Reference |

---

## Backend Documentation

### Patterns (`backend/patterns/`)

| Pattern | File |
|---------|------|
| Repository & Specification | [repository-specification.md](backend/patterns/repository-specification.md) |
| DI Auto-Registration | [di-auto-registration.md](backend/patterns/di-auto-registration.md) |
| Entity Configuration | [entity-configuration.md](backend/patterns/entity-configuration.md) |
| Hierarchical Audit Logging | [hierarchical-audit-logging.md](backend/patterns/hierarchical-audit-logging.md) |
| Before-State Resolver | [before-state-resolver.md](backend/patterns/before-state-resolver.md) |
| Bulk Operations | [bulk-operations.md](backend/patterns/bulk-operations.md) |
| JSON Enum Serialization | [json-enum-serialization.md](backend/patterns/json-enum-serialization.md) |
| JWT Refresh Token | [jwt-refresh-token.md](backend/patterns/jwt-refresh-token.md) |
| Technical Checklist | [technical-checklist.md](backend/patterns/technical-checklist.md) |

### Architecture (`backend/architecture/`)

| Document | Purpose |
|----------|---------|
| [tenant-id-interceptor.md](backend/architecture/tenant-id-interceptor.md) | Multi-tenancy query filtering |

### Research (`backend/research/`)

| Document | Topic |
|----------|-------|
| [cache-busting-best-practices.md](backend/research/cache-busting-best-practices.md) | Cache invalidation |
| [ecommerce-ux-patterns-2026.md](backend/research/ecommerce-ux-patterns-2026.md) | E-commerce UX |
| [hierarchical-audit-logging-comparison-2025.md](backend/research/hierarchical-audit-logging-comparison-2025.md) | Audit design |
| [role-permission-system-research.md](backend/research/role-permission-system-research.md) | RBAC/ReBAC patterns (Consolidated) |
| [seo-meta-and-hint-text-best-practices.md](backend/research/seo-meta-and-hint-text-best-practices.md) | SEO best practices |
| [validation-unification-plan.md](backend/research/validation-unification-plan.md) | Validation strategy |
| [vietnam-shipping-integration-2026.md](backend/research/vietnam-shipping-integration-2026.md) | Vietnam shipping providers |

---

## Frontend Documentation

### Core Guides (`frontend/`)

| Document | Purpose |
|----------|---------|
| [README.md](frontend/README.md) | Frontend overview |
| [architecture.md](frontend/architecture.md) | Component structure and patterns |
| [api-types.md](frontend/api-types.md) | Type generation from backend |
| [localization-guide.md](frontend/localization-guide.md) | i18n management |
| [COLOR_SCHEMA_GUIDE.md](frontend/COLOR_SCHEMA_GUIDE.md) | Color system |
| [ui-ux-enhancements.md](frontend/ui-ux-enhancements.md) | 11 UI/UX features |
| [ecommerce-ui.md](frontend/ecommerce-ui.md) | E-commerce components |
| [vibe-kanban-integration.md](frontend/vibe-kanban-integration.md) | Task management |

### Designs (`frontend/designs/`)

| Document | Purpose |
|----------|---------|
| [notification-dropdown-ui-design.md](frontend/designs/notification-dropdown-ui-design.md) | Notification UI |

---

## Testing Documentation

### E2E Testing (`testing/`)

| Document | Purpose |
|----------|---------|
| [TESTING-INFRASTRUCTURE-SUMMARY.md](testing/TESTING-INFRASTRUCTURE-SUMMARY.md) | 📊 **Overview of entire testing infrastructure** |
| [TEST_PLAN.md](testing/TEST_PLAN.md) | Comprehensive test strategy, scope, and roadmap |
| [E2E-TESTING-GUIDE.md](testing/E2E-TESTING-GUIDE.md) | Playwright setup, configuration, and implementation guide |
| [TEST_CASES.md](testing/TEST_CASES.md) | Test case reference by module |
| [VISUAL-REGRESSION-TESTING.md](testing/VISUAL-REGRESSION-TESTING.md) | Visual comparison testing with screenshot baselines |
| [ACCESSIBILITY-TESTING.md](testing/ACCESSIBILITY-TESTING.md) | WCAG 2.1 Level AA compliance testing with axe-core |

### Test Coverage Summary (2026-02-09)

#### Functional Tests (E2E)

| Category | Spec Files | Description |
|----------|-----------|-------------|
| Authentication | 2 | Login, forgot password |
| E-commerce | 8 | Products, categories, brands, attributes, product forms |
| Admin | 5 | Users, roles, tenants, platform/tenant settings |
| Content | 5 | Blog posts, categories, tags, post editor, legal pages |
| System | 7 | Notifications, command palette, developer logs, email templates, error pages, public pages, theme/language |
| Smoke | 5 | Quick validation of critical flows |
| Other | 2 | Dashboard, user settings, activity timeline |
| **Total** | **34** | **~490 unique test scenarios across Chromium + Firefox** |

#### Specialized Tests

| Test Type | Spec Files | Test Cases | Description |
|-----------|-----------|-----------|-------------|
| **Visual Regression** | 1 | 15 tests | Screenshot comparison (505KB baselines, 3 viewports) |
| **Accessibility** | 3 | 9 tests | WCAG 2.1 AA compliance (axe-core, 90+ checks) |
| **Mobile** | 3 | ~30 tests | iOS/Android viewport testing |

**Infrastructure:** 31 Page Object Model files, auth setup with storage states, visual baselines, accessibility audit rules

---

## Architecture Decisions

### ADRs (`decisions/`)

| ADR | Title |
|-----|-------|
| [001](decisions/001-tech-stack.md) | Technology Stack Selection |
| [002](decisions/002-frontend-ui-stack.md) | Frontend UI Stack |
| [003](decisions/003-vertical-slice-cqrs.md) | Vertical Slice Architecture |

---

## Roadmaps (`plans/`)

| Document | Status |
|----------|--------|
| [feature-roadmap-basic.md](plans/feature-roadmap-basic.md) | Phases 1-4 Complete |
| [feature-roadmap-ecommerce.md](plans/feature-roadmap-ecommerce.md) | In Progress |

---

## Research (`research/`)

| Document | Topic |
|----------|-------|
| [admin-portal-features-2026.md](research/admin-portal-features-2026.md) | Admin portal features |
| [essential-erp-cms-features-2026.md](research/essential-erp-cms-features-2026.md) | ERP/CMS features |

---

## Designs (`designs/`)

| Document | Status |
|----------|--------|
| [payment-gateway-admin-ui.md](designs/payment-gateway-admin-ui.md) | Reference |

---

## AI Instructions

| Document | Purpose |
|----------|---------|
| [CLAUDE.md](../CLAUDE.md) | Claude Code instructions |
| [AGENTS.md](../AGENTS.md) | Universal AI guidelines |

---

## Statistics

| Metric | Count |
|--------|-------|
| **Total Docs** | 50 |
| **Backend Patterns** | 9 |
| **Backend Architecture** | 1 |
| **Backend Research** | 7 |
| **Frontend Guides** | 9 |
| **Testing Docs** | 6 |
| **ADRs** | 3 |
| **Plans** | 2 |
| **Research (General)** | 2 |
| **Designs** | 1 |
| **E2E Test Scenarios** | ~490 |
| **Visual Regression Tests** | 15 (16 baselines, 505KB) |
| **Accessibility Tests** | 9 (90+ WCAG checks) |
| **Backend Tests** | 6,750+ |
| **Total Frontend Tests** | ~514 (E2E + Visual + A11y) |

---

**Version:** 2.7 (Updated 2026-02-09)
