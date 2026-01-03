# NOIR

[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![Tests](https://img.shields.io/badge/Tests-1739%2B-green.svg)](tests/)

A modern, enterprise-ready .NET 10 + React SaaS foundation with multi-tenancy support.

## Quick Start

```bash
# Clone and run (Windows with LocalDB)
git clone https://github.com/yourusername/noir.git
cd noir
dotnet run --project src/NOIR.Web

# Access: http://localhost:5000
# Admin: admin@noir.local / 123qwe
```

For macOS/Linux setup, see [SETUP.md](SETUP.md).

## Why NOIR?

- **Multi-tenant by design** - Finbuckle with auto-filtering from day one
- **Zero licensing costs** - All libraries are MIT/Apache 2.0
- **Enterprise patterns** - Clean Architecture, CQRS, DDD
- **Fully tested** - 1,739 tests with comprehensive coverage
- **Modern stack** - .NET 10 LTS, Wolverine, Mapperly

## Tech Stack

### Backend
| Category | Technology |
|----------|------------|
| Framework | .NET 10 LTS |
| Architecture | Clean Architecture + CQRS + DDD |
| Database | SQL Server / Entity Framework Core 10 |
| Auth | ASP.NET Core Identity + JWT (with refresh rotation) |
| Messaging | Wolverine |
| Validation | FluentValidation |
| Mapping | Mapperly (source-generated) |
| Background Jobs | Hangfire |
| Multi-Tenancy | Finbuckle.MultiTenant |

### Frontend
| Category | Technology |
|----------|------------|
| Framework | React 19 |
| Build Tool | Vite |
| Styling | Tailwind CSS 4 |
| Components | shadcn/ui + 21st.dev |
| Routing | React Router 7 |

## Features

**Implemented:**
- JWT with refresh token rotation and theft detection
- Permission-based RBAC with cache invalidation
- Hierarchical audit logging (HTTP, handler, entity levels)
- Rate limiting (fixed + sliding window)
- Health checks, security headers, response compression

**Planned:**
- React admin dashboard

## Project Structure

```
NOIR/
├── src/
│   ├── NOIR.Domain/           # Entities, interfaces
│   ├── NOIR.Application/      # Commands, queries, DTOs
│   ├── NOIR.Infrastructure/   # EF Core, handlers
│   └── NOIR.Web/              # API endpoints
│       └── frontend/          # React SPA
├── tests/                     # 1,739+ tests
├── docs/                      # Documentation
│   ├── backend/               # Backend patterns & guides
│   ├── frontend/              # Frontend architecture
│   └── decisions/             # Architecture Decision Records
└── .claude/                   # Claude Code commands
```

## Commands

```bash
dotnet build src/NOIR.sln                    # Build
dotnet run --project src/NOIR.Web            # Run
dotnet watch --project src/NOIR.Web          # Hot reload
dotnet test src/NOIR.sln                     # Test
```

## API Endpoints

| Category | Endpoints | Auth |
|----------|-----------|------|
| Auth | `/api/auth/login`, `/register`, `/refresh`, `/me` | Varies |
| Users | `/api/users/*` | Admin |
| Roles | `/api/roles/*` | Admin |
| Permissions | `/api/permissions` | Admin |
| System | `/api/health`, `/api/docs`, `/hangfire` | Varies |

## Project Management

This project uses **[Vibe Kanban](https://github.com/vibe-kanban/vibe-kanban)** for task management and sprint planning.

## Documentation

### Getting Started

| Document | Purpose |
|----------|---------|
| [SETUP.md](SETUP.md) | Full setup guide (all platforms) |
| [AGENTS.md](AGENTS.md) | AI assistant guidelines |
| [CLAUDE.md](CLAUDE.md) | Claude Code instructions |

### Backend

| Document | Purpose |
|----------|---------|
| [Backend Overview](docs/backend/README.md) | Architecture and setup |
| [Repository Pattern](docs/backend/patterns/repository-specification.md) | Data access patterns |
| [DI Registration](docs/backend/patterns/di-auto-registration.md) | Service registration |
| [Audit Logging](docs/backend/patterns/hierarchical-audit-logging.md) | Change tracking |

### Frontend

| Document | Purpose |
|----------|---------|
| [Frontend Overview](docs/frontend/README.md) | Architecture and conventions |
| [Theme Guide](docs/frontend/theme.md) | Theme customization |
| [API Types](docs/frontend/api-types.md) | Type generation |

### Architecture Decisions

| ADR | Title |
|-----|-------|
| [001](docs/decisions/001-tech-stack.md) | Technology Stack |
| [002](docs/decisions/002-frontend-ui-stack.md) | Frontend UI Stack |

## License

Apache License 2.0 - see [LICENSE](LICENSE).

## Acknowledgments

Built with: [Wolverine](https://wolverinefx.net/), [Finbuckle](https://www.finbuckle.com/MultiTenant), [Mapperly](https://mapperly.riok.app/), [FluentValidation](https://docs.fluentvalidation.net/), [Serilog](https://serilog.net/), [Hangfire](https://www.hangfire.io/), [Scalar](https://scalar.com/), [Scrutor](https://github.com/khellang/Scrutor)
