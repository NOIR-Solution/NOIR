# NOIR Documentation Index

> Your guide to navigating all NOIR documentation.

**Last Updated:** 2026-02-05

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
| [TEST_PLAN.md](testing/TEST_PLAN.md) | Comprehensive test strategy, scope, and roadmap |
| [E2E-TESTING-GUIDE.md](testing/E2E-TESTING-GUIDE.md) | Playwright setup, configuration, and implementation guide |
| [TEST_CASES.md](testing/TEST_CASES.md) | Quick reference test cases by module (156 tests) |
| [PRODUCT_E2E_TESTS.md](testing/PRODUCT_E2E_TESTS.md) | **Comprehensive Product E2E Suite (247 tests)** - Categories, Attributes, Brands, Products, Variants, Images, Options, Bulk Ops, Import/Export |

### Test Coverage Summary

#### Quick Reference Tests (156)

| Module | Test Cases | Priority Distribution |
|--------|------------|----------------------|
| Authentication | 20 | P0: 6, P1: 10, P2: 4 |
| Products (Quick) | 36 | P0: 6, P1: 20, P2: 10 |
| Users | 14 | P0: 4, P1: 8, P2: 2 |
| Roles | 8 | P0: 3, P1: 4, P2: 1 |
| Tenants | 8 | P0: 3, P1: 4, P2: 1 |
| E-commerce (Cart/Checkout/Orders) | 30 | P0: 13, P1: 13, P2: 4 |
| Blog | 15 | P0: 2, P1: 10, P2: 3 |
| Settings | 10 | P0: 1, P1: 7, P2: 2 |
| Smoke Suite | 10 | P0: 10 |
| Activity | 5 | P0: 0, P1: 5, P2: 0 |
| **Quick Total** | **156** | **P0: 48, P1: 81, P2: 27** |

#### Comprehensive Product E2E Suite (247)

| Module | Test Cases | Priority Distribution |
|--------|------------|----------------------|
| Categories | 30 | P0: 5, P1: 18, P2: 7 |
| Attributes | 45 | P0: 6, P1: 32, P2: 7 |
| Brands | 12 | P0: 2, P1: 8, P2: 2 |
| Product CRUD | 48 | P0: 12, P1: 30, P2: 6 |
| Variants | 28 | P0: 5, P1: 18, P2: 5 |
| Images | 22 | P0: 4, P1: 14, P2: 4 |
| Options | 14 | P0: 2, P1: 10, P2: 2 |
| Filters & Search | 20 | P0: 4, P1: 14, P2: 2 |
| Bulk Operations | 16 | P0: 4, P1: 10, P2: 2 |
| Import/Export | 18 | P0: 3, P1: 13, P2: 2 |
| Integration | 14 | P0: 5, P1: 9, P2: 0 |
| **Product Total** | **247** | **P0: 52, P1: 158, P2: 35** |

#### Grand Total: **403 Test Cases**

| Suite | P0 | P1 | P2 | Total |
|-------|----|----|-----|-------|
| Quick Reference | 48 | 81 | 27 | 156 |
| Product E2E | 52 | 158 | 35 | 247 |
| **Grand Total** | **100** | **239** | **62** | **403** |

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
| **Total Docs** | 48 |
| **Backend Patterns** | 9 |
| **Backend Architecture** | 1 |
| **Backend Research** | 7 |
| **Frontend Guides** | 9 |
| **Testing Docs** | 4 |
| **ADRs** | 3 |
| **Plans** | 2 |
| **Research (General)** | 2 |
| **Designs** | 1 |
| **E2E Test Cases** | 403 |

---

**Version:** 2.4
