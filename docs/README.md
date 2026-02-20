# NOIR Documentation

> **Start Here:** [DOCUMENTATION_INDEX.md](DOCUMENTATION_INDEX.md)

**Last Updated:** 2026-02-18

## Core Documentation

| Document | Description |
|----------|-------------|
| [DOCUMENTATION_INDEX.md](DOCUMENTATION_INDEX.md) | Complete navigation guide |
| [KNOWLEDGE_BASE.md](KNOWLEDGE_BASE.md) | Comprehensive codebase reference |
| [PROJECT_INDEX.md](PROJECT_INDEX.md) | Project structure and navigation |
| [FEATURE_CATALOG.md](FEATURE_CATALOG.md) | All features, commands, endpoints |
| [TECH_STACK.md](TECH_STACK.md) | Technology stack reference |
| [API_INDEX.md](API_INDEX.md) | REST API documentation |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Architecture overview |

## Structure

```
docs/
├── Core (7 files)
│   ├── README.md                    # This file
│   ├── DOCUMENTATION_INDEX.md       # Master index
│   ├── KNOWLEDGE_BASE.md            # Codebase reference
│   ├── PROJECT_INDEX.md             # Project navigation
│   ├── FEATURE_CATALOG.md           # Feature catalog
│   ├── TECH_STACK.md                # Technology reference
│   ├── API_INDEX.md                 # API documentation
│   └── ARCHITECTURE.md              # Architecture overview
│
├── backend/                          # .NET backend (20 files)
│   ├── README.md                    # Backend overview
│   ├── patterns/ (11 files)         # Implementation patterns
│   │   ├── repository-specification.md
│   │   ├── di-auto-registration.md
│   │   ├── entity-configuration.md
│   │   ├── hierarchical-audit-logging.md
│   │   ├── before-state-resolver.md
│   │   ├── bulk-operations.md
│   │   ├── json-enum-serialization.md
│   │   ├── jwt-refresh-token.md
│   │   ├── technical-checklist.md
│   │   ├── inventory-receipt-pattern.md
│   │   └── attribute-category-inheritance.md
│   ├── architecture/ (1 file)
│   │   └── tenant-id-interceptor.md
│   └── research/ (7 files)          # Research documents
│       ├── cache-busting-best-practices.md
│       ├── ecommerce-ux-patterns-2026.md
│       ├── hierarchical-audit-logging-comparison-2025.md
│       ├── role-permission-system-research.md   # Consolidated RBAC/ReBAC
│       ├── seo-meta-and-hint-text-best-practices.md
│       ├── validation-unification-plan.md
│       └── vietnam-shipping-integration-2026.md
│
├── frontend/ (10 files)              # React frontend
│   ├── README.md
│   ├── architecture.md
│   ├── api-types.md
│   ├── localization-guide.md
│   ├── COLOR_SCHEMA_GUIDE.md
│   ├── ui-ux-enhancements.md
│   ├── ecommerce-ui.md
│   ├── vibe-kanban-integration.md
│   ├── patterns/ (1 file)
│   │   └── form-resolver-type-assertions.md
│   └── designs/ (1 file)
│       └── notification-dropdown-ui-design.md
│
├── decisions/ (4 files)              # Architecture Decision Records
│   ├── README.md
│   ├── 001-tech-stack.md
│   ├── 002-frontend-ui-stack.md
│   └── 003-vertical-slice-cqrs.md
│
├── designs/ (1 file)                 # Feature designs
│   └── payment-gateway-admin-ui.md
│
├── plans/ (2 files)                  # Roadmaps
│   ├── feature-roadmap-basic.md     # Phases 1-4 (Complete)
│   └── feature-roadmap-ecommerce.md # E-commerce (In Progress)
│
├── research/ (3 files)               # General research
│   ├── admin-portal-features-2026.md
│   ├── admin-portal-ux-research.md
│   └── essential-erp-cms-features-2026.md
│
├── architecture/ (1 file)            # Architecture diagrams
│   └── diagrams.md
│
└── testing/ (1 file)                 # Testing documentation
    └── README.md
```

**Total: 53 documentation files**

## Quick Links

### Backend

- [Repository Pattern](backend/patterns/repository-specification.md)
- [DI Auto-Registration](backend/patterns/di-auto-registration.md)
- [Audit Logging](backend/patterns/hierarchical-audit-logging.md)
- [Multi-Tenancy](backend/architecture/tenant-id-interceptor.md)
- [JWT Refresh Token](backend/patterns/jwt-refresh-token.md)
- [Bulk Operations](backend/patterns/bulk-operations.md)

### Frontend

- [Architecture](frontend/architecture.md)
- [API Types](frontend/api-types.md)
- [Localization](frontend/localization-guide.md)
- [UI/UX Enhancements](frontend/ui-ux-enhancements.md)
- [E-commerce UI](frontend/ecommerce-ui.md)
- [Color Schema](frontend/COLOR_SCHEMA_GUIDE.md)

### Research

- [Role Permission System](backend/research/role-permission-system-research.md) (Consolidated)
- [Vietnam Shipping Integration](backend/research/vietnam-shipping-integration-2026.md)
- [E-commerce UX Patterns](backend/research/ecommerce-ux-patterns-2026.md)

### Architecture Decisions

- [ADR 001: Tech Stack](decisions/001-tech-stack.md)
- [ADR 002: Frontend UI Stack](decisions/002-frontend-ui-stack.md)
- [ADR 003: Vertical Slice CQRS](decisions/003-vertical-slice-cqrs.md)

## AI Instructions

- [CLAUDE.md](../CLAUDE.md) - Claude Code specific instructions
- [AGENTS.md](../AGENTS.md) - Universal AI agent guidelines

---

**For detailed navigation, see [DOCUMENTATION_INDEX.md](DOCUMENTATION_INDEX.md)**
