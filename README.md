<div align="center">

# NOIR

> *Build in the shadows. Ship in the light.*

**Enterprise .NET 10 + React 19 SaaS Platform**

*Multi-Tenancy · E-commerce · ERP Modules · Clean Architecture*

[![CI](https://github.com/NOIR-Solution/NOIR/actions/workflows/ci.yml/badge.svg)](https://github.com/NOIR-Solution/NOIR/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-19-61DAFB?style=flat-square&logo=react)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.9-3178C6?style=flat-square&logo=typescript)](https://www.typescriptlang.org/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg?style=flat-square)](LICENSE)
[![Tests](https://img.shields.io/badge/Tests-13%2C546_passing-brightgreen.svg?style=flat-square)](tests/)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=flat-square)](CONTRIBUTING.md)
[![AI-Coded](https://img.shields.io/badge/Built_with-Claude_Code-blueviolet?style=flat-square&logo=anthropic)](https://claude.ai/download)

[Features](#features) · [Quick Start](#quick-start) · [Architecture](#architecture) · [Documentation](#documentation) · [Contributing](#contributing)

</div>

---

## Overview

NOIR is a production-ready foundation for building multi-tenant SaaS applications. It provides a complete vertical stack — from database to UI — with built-in e-commerce, ERP modules, and enterprise patterns out of the box.

**100% AI-coded.** Every line of code, every test, every document in this repository was written using [Claude Code](https://claude.ai/download). NOIR is proof that AI-assisted development can produce enterprise-grade software — with clean architecture, 13,546 passing tests, and full-stack functionality — when used effectively.

### Why NOIR?

Building a multi-tenant SaaS platform from scratch typically takes months of boilerplate: authentication, tenant isolation, RBAC, audit trails, feature flags, real-time updates, and more — before you even start on business logic. NOIR provides all of this out of the box, plus production-ready e-commerce and ERP modules, so you can focus on what makes your product unique.

**Use cases:**
- Multi-tenant B2B/B2C SaaS platforms
- E-commerce with order management and payments
- Enterprise portals with HR, CRM, and project management
- Internal tools requiring RBAC and audit trails

---

## Architecture

```mermaid
flowchart TB
    FE["🖥️ React 19 Frontend<br/>56 pages · 98 UIKit components · 41 hooks"]
    WEB["⚡ Web Layer — ASP.NET Core 10<br/>53 endpoint groups · JWT · Multi-tenancy · SignalR + SSE · MCP Server"]
    APP["📋 Application Layer — CQRS via Wolverine<br/>Commands · Queries · Validators · Domain Events · Audit"]
    DOM["🏛️ Domain Layer<br/>Entities · Value Objects · Repository Interfaces · Specifications"]
    INF["🔧 Infrastructure<br/>EF Core 10 · SQL Server 2022 · FusionCache · Hangfire · ClosedXML"]

    FE -->|REST / SignalR / SSE| WEB
    WEB --> APP
    APP --> DOM
    APP --> INF
    INF -->|implements| DOM
```

```
NOIR/
├── src/
│   ├── NOIR.Domain/           # Entities, value objects, repository interfaces
│   ├── NOIR.Application/      # CQRS commands/queries, DTOs, validators
│   ├── NOIR.Infrastructure/   # EF Core, external services, persistence
│   └── NOIR.Web/              # API endpoints, middleware, SPA host
│       └── frontend/          # React 19 SPA
│           ├── src/portal-app/ # Feature modules (56 pages)
│           ├── src/uikit/      # 98 UI components + Storybook stories
│           └── src/hooks/      # 41 custom hooks
├── tests/                     # 13,546 tests
└── docs/                      # Architecture, patterns, module designs
```

### Patterns

- **Clean Architecture** with strict layer dependency enforcement (architecture tests)
- **CQRS** via Wolverine — command/query handlers co-located with validators
- **Repository + Specification** — reusable, composable query objects
- **Soft Delete** — data safety by default, hard delete only for GDPR
- **Domain Events** — decoupled cross-cutting reactions to state changes
- **MCP Server** — 55 AI-callable tools via Model Context Protocol (Streamable HTTP)

---

## Features

### 🏗️ Platform

| | Feature | Description |
|--|---------|-------------|
| 🏢 | **Multi-Tenancy** | Finbuckle-based tenant isolation with per-tenant configs, automatic query filtering |
| 🔐 | **Authentication** | JWT + refresh token rotation, RBAC, granular `resource:action` permissions |
| 📜 | **Audit Trail** | 3-level logging (HTTP → Command → Entity), activity timeline with diff |
| 🎛️ | **Feature Management** | 35 modules with platform/tenant override, endpoint and command gating |
| ⚡ | **Real-Time** | SignalR hubs, SSE for job progress, multi-tab session sync |
| 📧 | **Email** | Database-driven templates with Mustache interpolation, multi-tenant inheritance |
| 📱 | **PWA** | Installable on all platforms, offline support, smart caching strategies |
| 🤖 | **MCP Server** | 55 AI-callable tools + 6 prompts + 7 resources via Model Context Protocol (Streamable HTTP) |
| 📊 | **Enterprise DataTable** | Drag-reorder columns, density toggle, pinning, resizing, settings persistence |
| 🔍 | **UI/UX Audit** | Automated WCAG 2.1 AA compliance + 11 custom consistency rules, Playwright-powered |

### 🛒 E-commerce

| | Module | Capabilities |
|--|--------|-------------|
| 📦 | **Products** | Variants with SKU/price/inventory, hierarchical categories, 13 attribute types, faceted search |
| 🛍️ | **Cart & Checkout** | Guest + authenticated carts with merge, accordion checkout with session expiry |
| 📋 | **Orders** | Full lifecycle (Pending → Delivered), cancel/return with inventory rollback |
| 💳 | **Payments** | Multi-gateway architecture, transaction tracking, refund workflow, webhook verification |
| 🚚 | **Shipping** | Provider integrations, tracking timeline, carrier management |
| 🏪 | **Inventory** | Receipt system (stock-in/stock-out), draft/confirmed workflow |
| 👥 | **Customers** | Profiles, addresses, order history, segmentation groups |
| 🎟️ | **Promotions** | Discount codes, percentage/fixed, usage limits, date-range scheduling |
| ⭐ | **Reviews** | Product reviews with moderation workflow (approve/reject) |
| 📊 | **Reports** | Revenue, orders, inventory, product performance analytics |

### 🏢 ERP Modules

| | Module | Capabilities |
|--|--------|-------------|
| 👤 | **HR** | Employees (auto-code), department tree, 7 tag categories, org chart, bulk ops, import/export |
| 🤝 | **CRM** | Contacts, companies, lead pipeline Kanban, activities, dashboard widgets |
| 📌 | **Project Management** | Projects (auto-code), Kanban board, tasks with subtasks/labels/comments |

### 📝 Content & Media

| | Feature | Description |
|--|---------|-------------|
| ✍️ | **Blog CMS** | Posts, categories, tags with rich content editor |
| 🎨 | **Rich Rendering** | Syntax highlighting (Shiki), math (KaTeX), diagrams (Mermaid) |
| 🖼️ | **Media Library** | Upload, processing, storage (local / Azure Blob / AWS S3) |
| 🔔 | **Webhooks** | Outbound subscriptions with event filtering and delivery tracking |

---

## Tech Stack

<table>
<tr>
<td width="50%" valign="top">

**Backend**

| Technology | Purpose |
|------------|---------|
| .NET 10 | Runtime |
| EF Core 10 | ORM with interceptors |
| SQL Server 2022 | Database |
| Wolverine | CQRS messaging |
| FluentValidation | Input validation |
| Mapperly | Source-generated mapping |
| Hangfire | Background jobs |
| Serilog | Structured logging |
| FusionCache | Hybrid L1/L2 cache |
| ClosedXML | Excel import/export |
| ModelContextProtocol.AspNetCore | MCP server (AI agent integration) |

</td>
<td width="50%" valign="top">

**Frontend**

| Technology | Purpose |
|------------|---------|
| React 19 | UI library |
| TypeScript 5.9 | Type safety |
| Vite 7 | Build tooling |
| Tailwind CSS 4 | Styling |
| shadcn/ui | Component library |
| React Router 7 | Routing |
| TanStack Query | Data fetching |
| React Hook Form + Zod | Forms & validation |
| i18next | Internationalization |
| Storybook 10 | Component catalog |

</td>
</tr>
</table>

See [TECH_STACK.md](docs/TECH_STACK.md) for the complete technology reference.

---

## Testing

| Suite | Count | Scope |
|-------|-------|-------|
| Domain Unit Tests | 2,971 | Business rules, entity logic |
| Application Unit Tests | 8,557 | Handlers, validators, DTOs |
| Integration Tests | 1,141 | API endpoints with real database |
| Architecture Tests | 46 | Layer dependency enforcement |
| Frontend Unit Tests | 154 | Hooks, lib utilities (96% coverage) |
| Storybook Browser Tests | 677 | 98 UIKit components in Chromium |
| **Total** | **13,546** | |

```bash
dotnet test src/NOIR.sln                                  # Backend (12,715)
cd src/NOIR.Web/frontend && pnpm test:coverage            # Frontend unit + coverage
cd src/NOIR.Web/frontend && pnpm test:storybook           # Storybook browser tests
```

---

## Quick Start

### Prerequisites

| Requirement | Version | Download |
|-------------|---------|----------|
| .NET SDK | 10.0+ | [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0) |
| Node.js | 20+ | [nodejs.org](https://nodejs.org/) |
| pnpm | 10+ | [pnpm.io](https://pnpm.io/installation) |
| SQL Server | 2022 | LocalDB (Windows) or [Docker](https://hub.docker.com/_/microsoft-mssql-server) |

### Setup

```bash
git clone https://github.com/NOIR-Solution/NOIR.git && cd NOIR
dotnet build src/NOIR.sln
```

#### Database

NOIR uses SQL Server 2022. On first run, EF Core applies migrations automatically via `DbInitializer`. No manual migration step needed.

- **Windows**: SQL Server LocalDB works out of the box (included with Visual Studio)
- **macOS/Linux**: Use the [SQL Server Docker image](https://hub.docker.com/_/microsoft-mssql-server)
- **Connection string**: `src/NOIR.Web/appsettings.Development.json` — update if using a non-default instance

#### Run

Use the convenience script (recommended):

```bash
./start-dev.sh    # Auto-detects OS, frees ports, starts backend + frontend
```

Or run backend and frontend in separate terminals:

```bash
# Terminal 1 — Backend (port 4000)
dotnet watch --project src/NOIR.Web

# Terminal 2 — Frontend (port 3000)
cd src/NOIR.Web/frontend && pnpm install && pnpm run dev
```

### Access

| Service | URL |
|---------|-----|
| Frontend | http://localhost:3000 |
| API | http://localhost:4000 |
| API Docs (Scalar) | http://localhost:4000/api/docs |
| **MCP Server** | **http://localhost:4000/api/mcp** |
| Storybook | http://localhost:6006 |
| Hangfire Dashboard | http://localhost:4000/hangfire |

Default credentials: `admin@noir.local` / `123qwe`

---

## Documentation

| Guide | Description |
|-------|-------------|
| [Documentation Index](docs/DOCUMENTATION_INDEX.md) | Navigation hub for all docs |
| [Knowledge Base](docs/KNOWLEDGE_BASE.md) | Deep-dive codebase reference |
| [Feature Catalog](docs/FEATURE_CATALOG.md) | All features, commands, endpoints |
| [MCP Server](docs/backend/patterns/mcp-server.md) | AI agent integration — tools, prompts, resources |
| [Tech Stack](docs/TECH_STACK.md) | Technologies with rationale |
| [Product Roadmap](docs/roadmap.md) | Now / Next / Later framework |

<table>
<tr>
<td width="50%" valign="top">

**Backend**

- [Overview](docs/backend/README.md)
- [Repository + Specification](docs/backend/patterns/repository-specification.md)
- [DI Auto-Registration](docs/backend/patterns/di-auto-registration.md)
- [Audit Logging](docs/backend/patterns/hierarchical-audit-logging.md)
- [Multi-Tenancy](docs/backend/architecture/tenant-id-interceptor.md)

</td>
<td width="50%" valign="top">

**Frontend**

- [Overview](docs/frontend/README.md)
- [Architecture](docs/frontend/architecture.md)
- [Hooks Reference](docs/frontend/hooks-reference.md)
- [API Type Generation](docs/frontend/api-types.md)
- [Localization](docs/frontend/localization-guide.md)

</td>
</tr>
</table>

**Module Designs:** [HR](docs/designs/module-hr.md) · [CRM](docs/designs/module-crm.md) · [PM](docs/designs/module-pm.md) · [Calendar](docs/designs/module-calendar.md) · [Enterprise DataTable](docs/designs/enterprise-datatable-build-spec.md)

---

## Common Commands

```bash
# Development
dotnet watch --project src/NOIR.Web              # Backend with hot reload
cd src/NOIR.Web/frontend && pnpm run dev          # Frontend dev server
cd src/NOIR.Web/frontend && pnpm storybook        # Component catalog

# Build
dotnet build src/NOIR.sln
cd src/NOIR.Web/frontend && pnpm run build

# Type generation
cd src/NOIR.Web/frontend && pnpm run generate:api

# Database migrations
dotnet ef migrations add <NAME> \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context ApplicationDbContext \
  --output-dir Migrations/App
```

---

## Contributing

NOIR welcomes contributions. The project is 100% AI-assisted, but we value high-quality contributions regardless of how they're written.

**Before you start:**
1. Read [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines and coding standards
2. Review [CLAUDE.md](CLAUDE.md) for architecture rules and patterns
3. Explore [docs/](docs/) for detailed architecture documentation

**Workflow:**
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/your-feature`)
3. Write tests for new functionality
4. Ensure all tests pass: `dotnet test src/NOIR.sln && cd src/NOIR.Web/frontend && pnpm run build`
5. Submit a pull request with a clear description

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Port 3000/4000 already in use | Kill existing processes or use `./start-dev.sh` (auto-frees ports) |
| SQL Server connection failed | Check connection string in `appsettings.Development.json`, ensure LocalDB or Docker is running |
| `pnpm install` fails | Ensure Node.js 20+ and pnpm 10+ are installed |
| Frontend build type errors | Run `pnpm run generate:api` to sync types from backend |
| First build is slow | Normal — NuGet restore + frontend dependency install on first run |

## License

Licensed under **Apache License 2.0** — see [LICENSE](LICENSE) for details.

---

## Acknowledgments

NOIR builds on these excellent open-source projects:

- [Clean Architecture](https://github.com/jasontaylordev/CleanArchitecture) by Jason Taylor
- [Wolverine](https://wolverinefx.net/) — CQRS messaging framework
- [shadcn/ui](https://ui.shadcn.com/) — React component library
- [Finbuckle.MultiTenant](https://www.finbuckle.com/MultiTenant/) — Multi-tenancy framework
- [Claude Code](https://claude.ai/download) by Anthropic — AI-powered development

---

<div align="center">

*Build in the shadows. Ship in the light.*

[Documentation](docs/) · [Roadmap](docs/roadmap.md) · [Report Bug](https://github.com/NOIR-Solution/NOIR/issues) · [Request Feature](https://github.com/NOIR-Solution/NOIR/issues)

</div>
