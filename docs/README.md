# NOIR Documentation

> **Start Here:** [DOCUMENTATION_INDEX.md](DOCUMENTATION_INDEX.md)

**Last Updated:** 2026-03-01

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
├── Core (8 files)
│   ├── README.md                    # This file
│   ├── DOCUMENTATION_INDEX.md       # Master index
│   ├── KNOWLEDGE_BASE.md            # Codebase reference
│   ├── PROJECT_INDEX.md             # Project navigation
│   ├── FEATURE_CATALOG.md           # Feature catalog
│   ├── TECH_STACK.md                # Technology reference
│   ├── API_INDEX.md                 # API documentation
│   └── ARCHITECTURE.md              # Architecture overview
│
├── backend/                          # .NET backend (18 files)
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
│   └── research/ (5 files)          # Research documents
│       ├── cache-busting-best-practices.md
│       ├── role-permission-system-research.md
│       ├── seo-meta-and-hint-text-best-practices.md
│       ├── validation-unification-plan.md
│       └── vietnam-shipping-integration-2026.md
│
├── frontend/ (7 files)              # React frontend
│   ├── README.md
│   ├── architecture.md
│   ├── api-types.md
│   ├── localization-guide.md
│   ├── COLOR_SCHEMA_GUIDE.md
│   ├── design-standards.md
│   └── hooks-reference.md
│
├── decisions/ (4 files)              # Architecture Decision Records
│   ├── README.md
│   ├── 001-tech-stack.md
│   ├── 002-frontend-ui-stack.md
│   └── 003-vertical-slice-cqrs.md
│
├── designs/ (4 files)                # ERP module designs
│   ├── module-hr.md                  # HR (Implemented)
│   ├── module-crm.md                 # CRM (Implemented)
│   ├── module-pm.md                  # PM (Implemented)
│   └── module-calendar.md            # Calendar (Ready)
│
├── roadmap.md                        # Product roadmap (Now/Next/Later)
│
├── architecture/ (1 file)            # Architecture diagrams
│   └── diagrams.md
│
└── testing/ (1 file)                 # Testing documentation
    └── README.md
```

**Total: 45 documentation files**

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
- [Design Standards](frontend/design-standards.md)
- [Color Schema](frontend/COLOR_SCHEMA_GUIDE.md)
- [Hooks Reference](frontend/hooks-reference.md)

### Research

- [Role Permission System](backend/research/role-permission-system-research.md)
- [Vietnam Shipping Integration](backend/research/vietnam-shipping-integration-2026.md)

### Module Designs

- [HR Module](designs/module-hr.md) (Implemented)
- [CRM Module](designs/module-crm.md) (Implemented)
- [PM Module](designs/module-pm.md) (Implemented)
- [Calendar Module](designs/module-calendar.md) (Ready)

### Architecture Decisions

- [ADR 001: Tech Stack](decisions/001-tech-stack.md)
- [ADR 002: Frontend UI Stack](decisions/002-frontend-ui-stack.md)
- [ADR 003: Vertical Slice CQRS](decisions/003-vertical-slice-cqrs.md)

## AI Instructions

- [CLAUDE.md](../CLAUDE.md) - Claude Code specific instructions
- [AGENTS.md](../AGENTS.md) - Universal AI agent guidelines

---

**For detailed navigation, see [DOCUMENTATION_INDEX.md](DOCUMENTATION_INDEX.md)**
