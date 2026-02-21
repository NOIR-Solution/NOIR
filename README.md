<div align="center">

# NOIR

**Enterprise-ready .NET 10 + React 19 SaaS Foundation**

*Multi-tenancy • E-commerce • Clean Architecture • 10,889+ Tests*

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-19-61DAFB?style=flat-square&logo=react)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.9-3178C6?style=flat-square&logo=typescript)](https://www.typescriptlang.org/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg?style=flat-square)](LICENSE)
[![Tests](https://img.shields.io/badge/Tests-10889+-green.svg?style=flat-square)](tests/)

[Features](#-features) • [Quick Start](#-quick-start) • [Documentation](#-documentation) • [Tech Stack](#-tech-stack) • [Contributing](#-contributing)

</div>

---

## 🎯 What is NOIR?

NOIR is a **production-ready foundation** for building modern SaaS applications. It combines .NET 10's performance with React 19's cutting-edge features, wrapped in Clean Architecture principles.

**Built for:**
- 🏢 Multi-tenant B2B SaaS applications
- 🛒 E-commerce platforms with payments
- 🚀 Startups needing to ship fast with quality
- 👥 Teams seeking best-practice patterns
- 🎓 Developers learning enterprise architecture

---

## ⚡ Quick Start

### Prerequisites

- **Runtime:** .NET 10 SDK ([Download](https://dotnet.microsoft.com/download/dotnet/10.0))
- **Frontend:** Node.js 20+ ([Download](https://nodejs.org/)), pnpm 10+ ([Install](https://pnpm.io/installation))
- **Database:** SQL Server 2022 (LocalDB on Windows, [Docker](https://hub.docker.com/_/microsoft-mssql-server) on macOS/Linux)

### 🚀 One-Command Start

```bash
# Clone the repository
git clone https://github.com/NOIR-Solution/NOIR.git
cd NOIR

# Build backend
dotnet build src/NOIR.sln
```

**Development Mode (Recommended):**

```bash
# Terminal 1 - Backend with hot reload (port 4000)
dotnet watch --project src/NOIR.Web

# Terminal 2 - Frontend with Vite HMR (port 3000)
cd src/NOIR.Web/frontend
pnpm install && pnpm run dev
```

**Production Mode:**

```bash
# Build everything (includes frontend)
dotnet build src/NOIR.sln -c Release

# Run (backend serves frontend)
dotnet run --project src/NOIR.Web -c Release
```

### 🌐 Access Points

| Service | URL | Credentials |
|---------|-----|-------------|
| **Frontend** | http://localhost:3000 | `admin@noir.local` / `123qwe` |
| **API** | http://localhost:4000 | - |
| **API Docs** | http://localhost:4000/api/docs | - |
| **Storybook** | http://localhost:6006 | - |
| **Hangfire** | http://localhost:4000/hangfire | Requires `system:hangfire` permission |

---

## ✨ Features

### 🏗️ Architecture

- **Clean Architecture** - Domain, Application, Infrastructure, Web layers
- **Vertical Slice Architecture** - Features organized by use case, not layer
- **CQRS** - Command/Query separation with Wolverine messaging
- **Repository + Specification Pattern** - Reusable, testable queries

### 🔐 Authentication & Authorization

- **JWT + Refresh Tokens** - Secure authentication with token rotation
- **Role-Based Access Control (RBAC)** - Flexible role management
- **Permission-Based Authorization** - Granular `resource:action` permissions
- **Multi-Factor Authentication Ready** - OTP infrastructure in place

### 🏢 Multi-Tenancy

- **Finbuckle.MultiTenant** - Enterprise-grade tenant isolation
- **Automatic Query Filtering** - Transparent tenant data separation
- **Tenant Resolution** - Header, claim, and host-based strategies
- **Per-Tenant Customization** - Isolated settings and configurations

### 📊 Audit & Monitoring

- **3-Level Audit Logging** - HTTP request, Handler command, Entity change
- **Activity Timeline** - Complete audit trail with before/after states
- **Real-Time Logs** - SignalR streaming of Serilog logs
- **Performance Monitoring** - Built-in performance behavior pipeline

### 🔔 Real-Time Communication

- **SignalR Hubs** - Notifications and log streaming
- **Push Notifications** - Real-time user notifications
- **Live Updates** - WebSocket-based instant updates

### 📧 Email System

- **Database-Driven Templates** - No .cshtml files, all in database
- **Multi-Tenant Templates** - Copy-on-write template inheritance
- **Variable Interpolation** - Mustache-style `{{variable}}` syntax
- **FluentEmail + MailKit** - Reliable SMTP delivery

### 🗃️ Data Management

- **Soft Delete** - Data safety with restore capability
- **Bulk Operations** - High-performance batch processing
- **Change Tracking** - Automatic audit of entity modifications
- **Optimistic Concurrency** - Conflict detection and resolution

### 🎨 Frontend Features

- **Modern UI** - shadcn/ui components with Tailwind CSS 4
- **Dark/Light Mode** - System-aware theme switching
- **Internationalization** - Multi-language support (i18next)
- **Type-Safe API** - Auto-generated TypeScript types
- **Form Validation** - React Hook Form + Zod schemas

### 🛒 E-commerce (Phase 8 Complete)

- **Product Catalog** - Products with variants, SKUs, and hierarchical categories
- **Shopping Cart** - Guest and authenticated user support with cart merge
- **Checkout Flow** - Hybrid accordion pattern (Address → Shipping → Payment)
- **Order Management** - Full lifecycle with inventory integration
- **Inventory Tracking** - Real-time stock with reservation system

### 💳 Payment Processing

- **Multi-Gateway Support** - Pluggable payment provider architecture
- **Transaction Tracking** - Full payment lifecycle with status transitions
- **Refund Management** - Request, approve/reject workflow
- **Webhook Processing** - Secure callback handling with signature verification
- **COD Support** - Cash-on-delivery with collection confirmation
- **Real-Time Updates** - SignalR payment status notifications

### 📦 Content Management

- **Blog CMS** - Posts, categories, tags with rich editor
- **Media Management** - Image upload with processing
- **File Storage** - Local, Azure Blob, or AWS S3 support

### 🔧 Developer Experience

- **Hot Reload** - Backend and frontend live reloading
- **Type Generation** - Swagger → TypeScript types
- **Storybook** - 72 interactive component stories with UIKit catalog
- **10,889+ Tests** - Unit, Integration, Architecture
- **Architecture Tests** - Enforce layer boundaries
- **Comprehensive Docs** - 15,000+ lines of documentation
- **AI-Assisted** - SuperClaude Framework integration

---

## 📚 Documentation

### 📖 Core Guides

| Document | Purpose |
|----------|---------|
| **[📍 Documentation Index](docs/DOCUMENTATION_INDEX.md)** | Your navigation hub for all documentation |
| **[📚 Knowledge Base](docs/KNOWLEDGE_BASE.md)** | Comprehensive codebase reference with deep-dives |
| **[🗺️ Project Index](docs/PROJECT_INDEX.md)** | Complete project navigation and structure |
| **[📋 Feature Catalog](docs/FEATURE_CATALOG.md)** | All features, commands, and endpoints |
| **[🔧 Tech Stack](docs/TECH_STACK.md)** | Technologies with versions and rationale |

### 🎯 Quick Links

<table>
<tr>
<td width="50%" valign="top">

**Backend (.NET)**

- [Backend Overview](docs/backend/README.md)
- [Repository Pattern](docs/backend/patterns/repository-specification.md)
- [DI Auto-Registration](docs/backend/patterns/di-auto-registration.md)
- [Audit Logging](docs/backend/patterns/hierarchical-audit-logging.md)
- [Multi-Tenancy](docs/backend/architecture/tenant-id-interceptor.md)

</td>
<td width="50%" valign="top">

**Frontend (React)**

- [Frontend Overview](docs/frontend/README.md)
- [Architecture](docs/frontend/architecture.md)
- [API Types](docs/frontend/api-types.md)
- [Localization](docs/frontend/localization-guide.md)

</td>
</tr>
</table>

### 🤖 AI Assistant Guides

- **[CLAUDE.md](CLAUDE.md)** - Claude Code specific instructions
- **[AGENTS.md](AGENTS.md)** - Universal AI agent guidelines

---

## 🛠️ Tech Stack

<table>
<tr>
<td width="50%" valign="top">

### Backend

| Technology | Purpose |
|------------|---------|
| **.NET 10** | Runtime and SDK |
| **EF Core 10** | ORM with interceptors |
| **SQL Server** | Primary database |
| **Wolverine** | CQRS messaging |
| **FluentValidation** | Command validation |
| **Mapperly** | Object mapping |
| **Hangfire** | Background jobs |
| **Serilog** | Structured logging |
| **FusionCache** | Hybrid L1/L2 caching |

</td>
<td width="50%" valign="top">

### Frontend

| Technology | Purpose |
|------------|---------|
| **React 19** | UI library |
| **TypeScript 5.9** | Type safety |
| **Vite 7** | Build tool |
| **Tailwind CSS 4** | Utility-first CSS |
| **shadcn/ui** | Component library |
| **Storybook 10** | Component catalog |
| **React Router 7** | Client routing |
| **React Hook Form** | Form management |
| **Zod** | Schema validation |
| **i18next** | Internationalization |
| **pnpm** | Package manager |

</td>
</tr>
</table>

**[📋 Complete Stack](docs/TECH_STACK.md)** - Detailed technology reference with versions and rationale

---

## 📂 Project Structure

```
NOIR/
├── 📦 src/
│   ├── NOIR.Domain/           # 🎯 Entities, interfaces, value objects
│   ├── NOIR.Application/      # 📋 Commands, queries, DTOs, handlers
│   ├── NOIR.Infrastructure/   # 🔧 EF Core, services, persistence
│   └── NOIR.Web/              # 🌐 API endpoints, middleware, SPA host
│       └── frontend/          # ⚛️  React application (pnpm)
│           ├── .storybook/    # 📖 Storybook configuration
│           └── src/uikit/     # 📚 72 component stories
├── ✅ tests/                  # 10,889+ tests (Unit, Integration, Architecture)
├── 📚 docs/                   # 15,000+ lines of documentation
└── ⚙️  .github/               # CI/CD workflows and templates
```

---

## 🧪 Testing

### Backend Test Coverage

| Test Type | Project | Count | Coverage |
|-----------|---------|-------|----------|
| **Unit Tests** | Domain.UnitTests | 2,586 | Domain logic, business rules |
| **Unit Tests** | Application.UnitTests | 7,483 | CQRS handlers, validators |
| **Integration** | IntegrationTests | 788 | API endpoints with database |
| **Architecture** | ArchitectureTests | 32 | Layer dependency rules |
| **Total** | **All Projects** | **10,889+** | **Comprehensive coverage** |

### Running Tests

```bash
# All backend tests
dotnet test src/NOIR.sln

# Specific test project
dotnet test tests/NOIR.Domain.UnitTests
dotnet test tests/NOIR.Application.UnitTests
dotnet test tests/NOIR.IntegrationTests

# With coverage
dotnet test src/NOIR.sln --collect:"XPlat Code Coverage"
```

**Execution Time:** ~2 minutes for full backend test suite

See [Testing Documentation](docs/testing/README.md) for comprehensive testing guide.

---

## 🚀 Common Commands

<table>
<tr>
<td width="50%" valign="top">

### Development

```bash
# Backend hot reload
dotnet watch --project src/NOIR.Web

# Frontend dev server
cd src/NOIR.Web/frontend
pnpm run dev

# Storybook (component catalog)
cd src/NOIR.Web/frontend
pnpm storybook

# Run tests
dotnet test src/NOIR.sln

# Generate API types
cd src/NOIR.Web/frontend
pnpm run generate:api
```

</td>
<td width="50%" valign="top">

### Database

```bash
# Add migration
dotnet ef migrations add NAME \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context ApplicationDbContext \
  --output-dir Migrations/App

# Update database
dotnet ef database update \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context ApplicationDbContext
```

</td>
</tr>
</table>

---

## AI-Assisted Development (SuperClaude)

This project uses [SuperClaude Framework](https://github.com/SuperClaude-Org/SuperClaude_Framework/) for AI-powered development with Claude Code. It provides auto-routing of natural language to specialized skills.

### Setup

```bash
# 1. Install SuperClaude CLI
pipx install superclaude

# 2. Install commands to Claude Code
superclaude install

# 3. Verify installation
superclaude doctor

# 4. Restart Claude Code to load commands
```

### Usage

Once installed, just speak naturally in Claude Code within this project:

- "fix this bug" → auto-routes to `/sc:troubleshoot`
- "add a new feature" → auto-routes to `/sc:brainstorm`
- "write tests for this" → auto-routes to `/sc:test`
- "explain this code" → auto-routes to `/sc:explain`
- "build a modal component" → auto-routes to `/ui-ux-pro-max`
- "design a dashboard page" → auto-routes to `/ui-ux-pro-max`

Run `/sc:help` to see all available commands, or `/sc:recommend "your task"` for suggestions.

### UI/UX Design Intelligence

The `/ui-ux-pro-max` skill provides comprehensive UI/UX capabilities:

- **Design Research**: Color palettes, typography, UX patterns, style guidelines
- **Component Generation**: Production-ready React components with shadcn/ui
- **Accessibility**: ARIA labels, keyboard navigation, screen reader support
- **Responsive Design**: Mobile-first patterns and breakpoint optimization
- **50+ Styles**: Glassmorphism, neumorphism, brutalism, minimalism, and more

**Installation:**
```bash
# Install the ui-ux-pro-max skill
npx skills add https://github.com/nextlevelbuilder/ui-ux-pro-max-skill --skill ui-ux-pro-max
```

**Example prompts:**
- "What color palette for e-commerce?" → Design research with color schemes
- "Build a product card component" → React/TypeScript implementation
- "Review my navbar for accessibility" → Comprehensive UX audit

> Skill routing hints are in `CLAUDE.md` under SuperClaude Framework.

---

## 🤝 Contributing

We welcome contributions! Here's how to get started:

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Follow** coding standards in [CLAUDE.md](CLAUDE.md)
4. **Write** tests for new features
5. **Run** tests (`dotnet test src/NOIR.sln`)
6. **Commit** with clear messages
7. **Push** to your fork
8. **Open** a Pull Request

**Read:** [CONTRIBUTING.md](CONTRIBUTING.md) for detailed guidelines.

---

## 📜 License

This project is licensed under the **Apache License 2.0** - see the [LICENSE](LICENSE) file for details.

---

## 💝 Acknowledgments

NOIR builds on the shoulders of giants:

- **[Clean Architecture](https://github.com/jasontaylordev/CleanArchitecture)** by Jason Taylor - Architectural foundation
- **[Wolverine](https://wolverinefx.net/)** - CQRS messaging framework
- **[shadcn/ui](https://ui.shadcn.com/)** - Beautiful React components
- **[Finbuckle.MultiTenant](https://www.finbuckle.com/MultiTenant/)** - Multi-tenancy framework

---

## 🌟 Why NOIR?

| Aspect | NOIR Advantage |
|--------|----------------|
| **Production-Ready** | Battle-tested patterns, 10,889+ tests, security built-in |
| **E-commerce Ready** | Products, cart, checkout, orders, payments - all included |
| **Developer-Friendly** | Hot reload, type generation, extensive docs, AI-assisted development |
| **Performance** | Source generators, compiled queries, hybrid caching, optimized builds |
| **Scalability** | Multi-tenancy, horizontal scaling, efficient data access patterns |
| **Maintainability** | Clean Architecture, clear patterns, 100% documentation coverage |

---

<div align="center">

**Built with ❤️ by the NOIR Team**

[⭐ Star on GitHub](https://github.com/NOIR-Solution/NOIR) • [📖 Read the Docs](docs/) • [🐛 Report Bug](https://github.com/NOIR-Solution/NOIR/issues) • [💡 Request Feature](https://github.com/NOIR-Solution/NOIR/issues)

</div>
