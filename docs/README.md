# NOIR Documentation

Welcome to the NOIR documentation. This folder contains comprehensive guides for both backend and frontend development.

> **📍 Start Here:** [DOCUMENTATION_INDEX.md](DOCUMENTATION_INDEX.md) - Your complete guide to navigating all documentation

> **📚 Learning the Codebase?** [KNOWLEDGE_BASE.md](KNOWLEDGE_BASE.md) - Comprehensive cross-referenced guide to the entire codebase

## Core Documentation

| Document | Description |
|----------|-------------|
| **[DOCUMENTATION_INDEX.md](DOCUMENTATION_INDEX.md)** | 📍 **Start here** - Complete navigation guide to all documentation |
| **[KNOWLEDGE_BASE.md](KNOWLEDGE_BASE.md)** | Comprehensive codebase reference with deep-dive explanations |
| **[PROJECT_INDEX.md](PROJECT_INDEX.md)** | Complete project navigation with structure, features, and quick links |
| **[FEATURE_CATALOG.md](FEATURE_CATALOG.md)** | Complete feature reference with all commands, queries, and endpoints |
| **[TECH_STACK.md](TECH_STACK.md)** | Complete technology stack reference with versions and rationale |
| [API_INDEX.md](API_INDEX.md) | REST API endpoint documentation with examples |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Architecture overview, patterns, and decisions |

## Structure

```
docs/
├── backend/           # .NET backend documentation
│   ├── patterns/      # Code patterns and best practices
│   └── research/      # Technical research and comparisons
├── frontend/          # React frontend documentation
└── decisions/         # Architecture Decision Records (ADRs)
```

## Quick Links

### Backend (.NET)

| Document | Description |
|----------|-------------|
| [Backend Overview](backend/README.md) | Backend architecture and setup |
| [Repository & Specification](backend/patterns/repository-specification.md) | Data access patterns |
| [DI Auto-Registration](backend/patterns/di-auto-registration.md) | Service registration with Scrutor |
| [Entity Configuration](backend/patterns/entity-configuration.md) | EF Core entity setup |
| [JWT Refresh Tokens](backend/patterns/jwt-refresh-token.md) | Token rotation and security |
| [Audit Logging](backend/patterns/hierarchical-audit-logging.md) | 3-level audit system |
| [Bulk Operations](backend/patterns/bulk-operations.md) | High-performance data operations |

### Frontend (React)

| Document | Description |
|----------|-------------|
| [Frontend Overview](frontend/README.md) | Frontend architecture and conventions |
| [Architecture](frontend/architecture.md) | Project structure and patterns |
| [API Types](frontend/api-types.md) | Type generation from backend |
| [Localization Guide](frontend/localization-guide.md) | Managing translations and adding languages |

### Architecture Decisions

| ADR | Title |
|-----|-------|
| [001](decisions/001-tech-stack.md) | Technology Stack Selection |
| [002](decisions/002-frontend-ui-stack.md) | Frontend UI Stack |
| [003](decisions/003-vertical-slice-cqrs.md) | Vertical Slice Architecture for CQRS |

## For AI Assistants

This documentation is structured to be AI-friendly:

- **Clear headings** with H1, H2, H3 hierarchy
- **Code examples** with syntax highlighting
- **Tables** for quick reference
- **Consistent file naming** for easy discovery

See also:
- [CLAUDE.md](../CLAUDE.md) - Claude Code specific instructions
- [AGENTS.md](../AGENTS.md) - Unified AI assistant guidelines
