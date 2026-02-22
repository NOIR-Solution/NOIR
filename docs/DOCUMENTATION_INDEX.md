# NOIR Documentation Index

> Your guide to navigating all NOIR documentation.

**Last Updated:** 2026-02-23

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
| **View testing guide** | [testing/README.md](testing/README.md) |

---

## Core Documentation

| Document | Purpose | Size |
|----------|---------|------|
| [KNOWLEDGE_BASE.md](KNOWLEDGE_BASE.md) | Comprehensive codebase reference | ~2275 lines |
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
| Inventory Receipt Pattern | [inventory-receipt-pattern.md](backend/patterns/inventory-receipt-pattern.md) |
| Attribute-Category Inheritance | [attribute-category-inheritance.md](backend/patterns/attribute-category-inheritance.md) |

### Architecture (`backend/architecture/`, `architecture/`)

| Document | Purpose |
|----------|---------|
| [tenant-id-interceptor.md](backend/architecture/tenant-id-interceptor.md) | Multi-tenancy query filtering |
| [diagrams.md](architecture/diagrams.md) | Architecture diagrams (ER, CQRS flow, multi-tenancy, order lifecycle) |

### Research (`backend/research/`)

| Document | Topic |
|----------|-------|
| [cache-busting-best-practices.md](backend/research/cache-busting-best-practices.md) | Cache invalidation |
| [ecommerce-ux-patterns-2026.md](backend/research/ecommerce-ux-patterns-2026.md) | E-commerce UX |
| [feature-management-design.md](backend/research/feature-management-design.md) | Feature management architecture |
| [feature-management-requirements.md](backend/research/feature-management-requirements.md) | Feature toggle requirements |
| [feature-management-workflow.md](backend/research/feature-management-workflow.md) | Implementation workflow |
| [hierarchical-audit-logging-comparison-2025.md](backend/research/hierarchical-audit-logging-comparison-2025.md) | Audit design |
| [role-permission-system-research.md](backend/research/role-permission-system-research.md) | RBAC/ReBAC patterns (Consolidated) |
| [seo-meta-and-hint-text-best-practices.md](backend/research/seo-meta-and-hint-text-best-practices.md) | SEO best practices |
| [module-feature-management-systems.md](backend/research/module-feature-management-systems.md) | Comparative feature systems analysis |
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
| [audit-storybook-coverage.md](frontend/audit-storybook-coverage.md) | Storybook coverage audit |
| [audit-ui-patterns.md](frontend/audit-ui-patterns.md) | UI patterns audit |
| [design-standards.md](frontend/design-standards.md) | Design standards and guidelines |

### Patterns (`frontend/patterns/`)

| Document | Purpose |
|----------|---------|
| [form-resolver-type-assertions.md](frontend/patterns/form-resolver-type-assertions.md) | Zod + react-hook-form type assertion pattern |

### Designs (`frontend/designs/`)

| Document | Purpose |
|----------|---------|
| [notification-dropdown-ui-design.md](frontend/designs/notification-dropdown-ui-design.md) | Notification UI |

---

## Testing Documentation

### Backend Testing (`testing/`)

| Document | Purpose |
|----------|---------|
| [README.md](testing/README.md) | Testing overview, conventions, and running tests |

### Test Coverage Summary (2026-02-23)

**Backend Tests:** 10,889+ tests across 4 test projects
- 2,586 Domain unit tests
- 7,483 Application unit tests
- 788 Integration tests
- 32 Architecture tests

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
| [feature-roadmap-ecommerce.md](plans/feature-roadmap-ecommerce.md) | Complete |

---

## Research (`research/`)

| Document | Topic |
|----------|-------|
| [admin-portal-features-2026.md](research/admin-portal-features-2026.md) | Admin portal features |
| [essential-erp-cms-features-2026.md](research/essential-erp-cms-features-2026.md) | ERP/CMS features |
| [admin-portal-ux-research.md](research/admin-portal-ux-research.md) | Admin portal UX patterns |

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
| **Total Docs** | 59 |
| **Backend Patterns** | 11 |
| **Backend Architecture** | 2 |
| **Backend Research** | 7 |
| **Frontend Guides** | 13 |
| **Testing Docs** | 1 |
| **ADRs** | 3 |
| **Plans** | 2 |
| **Research (General)** | 3 |
| **Designs** | 1 |
| **Backend Tests** | 10,889+ |

---

**Version:** 3.5 (Updated 2026-02-23 - Synced test counts 10,889+, Storybook stories 91, docs total 59)
