# NOIR Documentation Index

> Your guide to navigating all NOIR documentation.

**Last Updated:** 2026-03-12

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
| SignalR Real-Time Signals | [signalr-real-time.md](backend/patterns/signalr-real-time.md) |
| Webhook System | [webhook-system.md](backend/patterns/webhook-system.md) |
| Order Lifecycle State Machine | [order-lifecycle.md](backend/patterns/order-lifecycle.md) |
| Lead Pipeline State Machine | [lead-pipeline-state-machine.md](backend/patterns/lead-pipeline-state-machine.md) |
| Code Generation (Auto-Codes) | [code-generation.md](backend/patterns/code-generation.md) |
| Caching Strategy | [caching-strategy.md](backend/patterns/caching-strategy.md) |
| Middleware & Interceptors | [middleware-interceptors.md](backend/patterns/middleware-interceptors.md) |
| SSE & Background Jobs | [sse-background-jobs.md](backend/patterns/sse-background-jobs.md) |
| Excel Import/Export | [excel-import-export.md](backend/patterns/excel-import-export.md) |
| **MCP Server (AI Agent Integration)** | [mcp-server.md](backend/patterns/mcp-server.md) |

### Architecture (`backend/architecture/`, `architecture/`)

| Document | Purpose |
|----------|---------|
| [tenant-id-interceptor.md](backend/architecture/tenant-id-interceptor.md) | Multi-tenancy query filtering |
| [diagrams.md](architecture/diagrams.md) | Architecture diagrams (ER, CQRS flow, multi-tenancy, order lifecycle) |

### Research (`backend/research/`)

| Document | Topic |
|----------|-------|
| [cache-busting-best-practices.md](backend/research/cache-busting-best-practices.md) | Cache invalidation |
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
| [design-standards.md](frontend/design-standards.md) | Design standards and guidelines |
| [hooks-reference.md](frontend/hooks-reference.md) | 50 custom React hooks reference |

---

## Testing Documentation

### Backend Testing (`testing/`)

| Document | Purpose |
|----------|---------|
| [README.md](testing/README.md) | Testing overview, conventions, and running tests |

### Test Coverage Summary (2026-03-12)

**Backend Tests:** 12,715 across 4 test projects
- 2,971 Domain unit tests
- 8,557 Application unit tests
- 1,141 Integration tests
- 46 Architecture tests

**Frontend Tests:** 831 total
- 154 Vitest unit tests
- 677 Storybook browser tests (98 UIKit components)

---

## Architecture Decisions

### ADRs (`decisions/`)

| ADR | Title |
|-----|-------|
| [001](decisions/001-tech-stack.md) | Technology Stack Selection |
| [002](decisions/002-frontend-ui-stack.md) | Frontend UI Stack |
| [003](decisions/003-vertical-slice-cqrs.md) | Vertical Slice Architecture |

---

## Designs (`designs/`)

### ERP Module Designs

| Document | Purpose | Status |
|----------|--------|--------|
| [module-hr.md](designs/module-hr.md) | HR management module design | Implemented |
| [module-crm.md](designs/module-crm.md) | CRM module design | Implemented |
| [module-pm.md](designs/module-pm.md) | Project Management module design | Implemented |
| [module-calendar.md](designs/module-calendar.md) | Calendar module design | Ready |

---

## Roadmap

| Document | Purpose |
|----------|--------|
| [roadmap.md](roadmap.md) | Product roadmap (Now/Next/Later framework) with module status |

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
| **Total Docs** | 55 |
| **Backend Patterns** | 21 |
| **Backend Architecture** | 2 |
| **Backend Research** | 5 |
| **Frontend Guides** | 7 |
| **Testing Docs** | 1 |
| **ADRs** | 3 |
| **Module Designs** | 4 |
| **Backend Tests** | 12,715 |
| **Frontend Tests** | 831 (154 unit + 677 Storybook) |
| **Total Tests** | 13,546 |

---

**Version:** 5.2 (Updated 2026-03-12 — Removed 2 obsolete frontend docs, updated all counts)
