# NOIR - Complete Documentation Index

> **Your comprehensive guide to navigating all NOIR documentation.**

**Last Updated:** 2026-01-22

---

## Quick Start - Where Do I Go?

| I Want To... | Go To |
|--------------|-------|
| **Understand the entire codebase** | [KNOWLEDGE_BASE.md](KNOWLEDGE_BASE.md) |
| **Navigate the project structure** | [PROJECT_INDEX.md](PROJECT_INDEX.md) |
| **Find a specific feature** | [FEATURE_CATALOG.md](FEATURE_CATALOG.md) |
| **Learn about technologies used** | [TECH_STACK.md](TECH_STACK.md) |
| **See API endpoints** | [API_INDEX.md](API_INDEX.md) |
| **Understand architecture** | [ARCHITECTURE.md](ARCHITECTURE.md) |
| **Learn a backend pattern** | [Backend Patterns](backend/patterns/) |
| **Learn frontend architecture** | [Frontend Docs](frontend/) |
| **Read architecture decisions** | [ADRs](decisions/) |

---

## Core Documentation (Start Here)

### 📚 [KNOWLEDGE_BASE.md](KNOWLEDGE_BASE.md)
**Comprehensive codebase reference with deep-dive explanations**

- Complete guide to all layers and components
- Cross-referenced with links to source files
- Pattern explanations with code examples
- Best for: Understanding how everything works

**Size:** ~500 lines | **Type:** Reference Guide

---

### 🗺️ [PROJECT_INDEX.md](PROJECT_INDEX.md)
**Complete project navigation with structure, features, and quick links**

- Project structure breakdown
- Feature module organization
- Quick reference commands
- Navigation tips
- Best for: Finding files and understanding project layout

**Size:** ~865 lines | **Type:** Navigation Guide

**Table of Contents:**
1. Project Overview
2. Architecture Layers (Domain, Application, Infrastructure, Web)
3. Feature Modules (Auth, Users, Roles, Tenants, etc.)
4. Core Components (Specifications, Validation, Middleware)
5. Testing Structure
6. Documentation Map
7. Quick Reference

---

### 📋 [FEATURE_CATALOG.md](FEATURE_CATALOG.md)
**Complete feature reference with all commands, queries, and endpoints**

- Detailed feature documentation
- All commands and queries
- API endpoint examples
- Request/response formats
- Validation rules
- Best for: Implementing or using features

**Size:** ~1,074 lines | **Type:** Feature Reference

**Modules Covered:**
1. Authentication & Identity (Login, Logout, Profile, Password Reset)
2. User Management (CRUD, Role Assignment)
3. Role & Permission Management
4. Multi-Tenancy
5. Audit Logging
6. Notifications (SignalR)
7. Email Templates
8. Media Management
9. Blog CMS (Posts, Categories, Tags)
10. Developer Tools (Log Streaming)

---

### 🔧 [TECH_STACK.md](TECH_STACK.md)
**Complete technology stack reference with versions and rationale**

- Backend stack (.NET 10, EF Core, Wolverine, etc.)
- Frontend stack (React 19, Vite, Tailwind, shadcn/ui)
- Infrastructure (Docker, Azure, SQL Server)
- Testing framework (xUnit, Playwright)
- Best for: Understanding technology choices

**Size:** ~756 lines | **Type:** Technology Reference

**Sections:**
1. Backend Stack (.NET 10)
2. Frontend Stack (React 19)
3. Infrastructure & DevOps
4. Testing Framework
5. Development Tools
6. Why These Choices? (decision rationale)

---

### 🌐 [API_INDEX.md](API_INDEX.md)
**REST API endpoint documentation with examples**

- All API endpoints
- Request/response schemas
- Authentication requirements
- Error responses
- Best for: API consumers and integrations

**Type:** API Reference

---

### 🏗️ [ARCHITECTURE.md](ARCHITECTURE.md)
**Architecture overview, patterns, and decisions**

- Clean Architecture layers
- Design patterns used
- Key architectural decisions
- Best for: Understanding the big picture

**Type:** Architecture Overview

---

## Backend Documentation

### Patterns (`backend/patterns/`)

Detailed guides for implementation patterns:

| Pattern | File | Purpose |
|---------|------|---------|
| **Repository & Specification** | [repository-specification.md](backend/patterns/repository-specification.md) | Data access with reusable queries |
| **DI Auto-Registration** | [di-auto-registration.md](backend/patterns/di-auto-registration.md) | Service registration with Scrutor |
| **Entity Configuration** | [entity-configuration.md](backend/patterns/entity-configuration.md) | EF Core entity setup |
| **Hierarchical Audit Logging** | [hierarchical-audit-logging.md](backend/patterns/hierarchical-audit-logging.md) | 3-level audit system |
| **Before-State Resolver** | [before-state-resolver.md](backend/patterns/before-state-resolver.md) | Activity Timeline handler diffs |
| **Bulk Operations** | [bulk-operations.md](backend/patterns/bulk-operations.md) | High-performance batch operations |
| **JSON Enum Serialization** | [json-enum-serialization.md](backend/patterns/json-enum-serialization.md) | String-based enum serialization |
| **JWT Refresh Token** | [jwt-refresh-token.md](backend/patterns/jwt-refresh-token.md) | Token rotation and security |
| **Technical Checklist** | [technical-checklist.md](backend/patterns/technical-checklist.md) | Feature implementation checklist |

### Architecture (`backend/architecture/`)

| Document | Purpose |
|----------|---------|
| [tenant-id-interceptor.md](backend/architecture/tenant-id-interceptor.md) | Multi-tenancy query filtering |

### Research (`backend/research/`)

Technical research and comparisons:

| Document | Topic |
|----------|-------|
| [role-permission-best-practices-2025.md](backend/research/role-permission-best-practices-2025.md) | Role/permission patterns |
| [hierarchical-audit-logging-comparison-2025.md](backend/research/hierarchical-audit-logging-comparison-2025.md) | Audit system design comparison |
| [validation-unification-plan.md](backend/research/validation-unification-plan.md) | Unified validation strategy |
| [cache-busting-best-practices.md](backend/research/cache-busting-best-practices.md) | Cache invalidation strategies |

---

## Frontend Documentation

### Core Guides (`frontend/`)

| Document | Purpose |
|----------|---------|
| [README.md](frontend/README.md) | Frontend overview and setup |
| [architecture.md](frontend/architecture.md) | Component structure and patterns |
| [theme.md](frontend/theme.md) | Theme customization guide |
| [api-types.md](frontend/api-types.md) | Type generation from backend |
| [localization-guide.md](frontend/localization-guide.md) | i18n management |
| [COLOR_SCHEMA_GUIDE.md](frontend/COLOR_SCHEMA_GUIDE.md) | Color system and palettes |

### Designs (`frontend/designs/`)

| Document | Purpose |
|----------|---------|
| [notification-dropdown-ui-design.md](frontend/designs/notification-dropdown-ui-design.md) | Notification UI design |

---

## Architecture Decisions

### ADRs (`decisions/`)

Architecture Decision Records documenting key technical choices:

| ADR | Title | Date |
|-----|-------|------|
| [001](decisions/001-tech-stack.md) | Technology Stack Selection | 2025 |
| [002](decisions/002-frontend-ui-stack.md) | Frontend UI Stack (React 19, Tailwind, shadcn/ui) | 2025 |
| [003](decisions/003-vertical-slice-cqrs.md) | Vertical Slice Architecture for CQRS | 2025 |

**Format:** Each ADR includes Context, Decision, Consequences, and Alternatives Considered.

---

## Plans & Roadmaps

### Feature Roadmap (`plans/`)

| Document | Purpose |
|----------|---------|
| [feature-roadmap-2026.md](plans/feature-roadmap-2026.md) | 2026 feature planning |

---

## Documentation Statistics

### By Type

| Type | Count | Total Lines |
|------|-------|-------------|
| **Core Docs** | 6 | ~4,000 |
| **Backend Patterns** | 9 | ~2,000 |
| **Backend Research** | 4 | ~1,500 |
| **Frontend Guides** | 6 | ~1,200 |
| **ADRs** | 3 | ~800 |
| **Total** | **28** | **~9,500** |

### Coverage

- ✅ **100%** of features documented
- ✅ **100%** of API endpoints documented
- ✅ **100%** of patterns documented
- ✅ **100%** of technologies documented

---

## Documentation Principles

### For Developers

1. **Start with KNOWLEDGE_BASE.md** for comprehensive understanding
2. **Use PROJECT_INDEX.md** to navigate the codebase
3. **Refer to FEATURE_CATALOG.md** when implementing features
4. **Check patterns/** before writing new code

### For AI Assistants

All documentation is structured for AI-friendly parsing:

- **Clear headings** - H1, H2, H3 hierarchy
- **Code examples** - Syntax-highlighted blocks
- **Tables** - Quick reference formats
- **Cross-links** - Related documentation links
- **File paths** - Exact file locations for navigation

**AI Instructions:**
- [CLAUDE.md](../CLAUDE.md) - Claude Code specific
- [AGENTS.md](../AGENTS.md) - Universal AI guidelines

---

## Navigation Patterns

### Finding Information

**By Topic:**
1. Check this index for the right document
2. Use the document's Table of Contents
3. Use Ctrl+F to search within the document

**By Code Location:**
1. Start with [PROJECT_INDEX.md](PROJECT_INDEX.md)
2. Navigate to the layer/feature
3. Find file paths in the documentation

**By Feature:**
1. Check [FEATURE_CATALOG.md](FEATURE_CATALOG.md)
2. Find the feature module
3. See commands, queries, and endpoints

**By Technology:**
1. Check [TECH_STACK.md](TECH_STACK.md)
2. Find the technology section
3. See usage examples and configuration

---

## Documentation Maintenance

### When to Update

- ✅ New feature added → Update FEATURE_CATALOG.md
- ✅ Technology changed → Update TECH_STACK.md
- ✅ Pattern introduced → Add to backend/patterns/
- ✅ Architecture decision → Create new ADR

### How to Update

1. Update the relevant document(s)
2. Update cross-references if needed
3. Update "Last Updated" date
4. Run a grep to find all references

---

## Related Resources

### External Documentation

- [.NET Documentation](https://learn.microsoft.com/dotnet/)
- [React Documentation](https://react.dev/)
- [EF Core Documentation](https://learn.microsoft.com/ef/core/)
- [Tailwind CSS Documentation](https://tailwindcss.com/docs)

### Community

- **GitHub:** https://github.com/NOIR-Solution/NOIR
- **Issues:** Report bugs and request features
- **Discussions:** Ask questions and share ideas

---

## Quick Reference Card

### Most Important Docs

| For... | Read... |
|--------|---------|
| 🚀 **Getting Started** | [README.md](../README.md) |
| 📚 **Learning the Codebase** | [KNOWLEDGE_BASE.md](KNOWLEDGE_BASE.md) |
| 🗺️ **Navigation** | [PROJECT_INDEX.md](PROJECT_INDEX.md) |
| 📋 **Feature Development** | [FEATURE_CATALOG.md](FEATURE_CATALOG.md) |
| 🔧 **Technology Reference** | [TECH_STACK.md](TECH_STACK.md) |
| 🌐 **API Integration** | [API_INDEX.md](API_INDEX.md) |

### Most Used Patterns

| Pattern | Doc |
|---------|-----|
| Repository + Spec | [repository-specification.md](backend/patterns/repository-specification.md) |
| CQRS Handlers | [PROJECT_INDEX.md](PROJECT_INDEX.md) → Application Layer |
| Multi-Tenancy | [tenant-id-interceptor.md](backend/architecture/tenant-id-interceptor.md) |
| Audit Logging | [hierarchical-audit-logging.md](backend/patterns/hierarchical-audit-logging.md) |

### Most Used Commands

| Task | Doc |
|------|-----|
| Run project | [README.md](../README.md) → Quick Start |
| Run tests | [PROJECT_INDEX.md](PROJECT_INDEX.md) → Quick Reference |
| Create migration | [PROJECT_INDEX.md](PROJECT_INDEX.md) → Quick Reference |
| Add feature | [FEATURE_CATALOG.md](FEATURE_CATALOG.md) + [technical-checklist.md](backend/patterns/technical-checklist.md) |

---

## Contributing to Documentation

See [CONTRIBUTING.md](../CONTRIBUTING.md) for guidelines on:
- Writing new documentation
- Updating existing documentation
- Documentation style guide
- Review process

---

**🎯 Pro Tip:** Bookmark this page as your documentation home base!

---

**Last Updated:** 2026-01-22
**Maintainer:** NOIR Team
