# NOIR

> Enterprise-ready .NET 10 + React SaaS foundation with multi-tenancy, Clean Architecture, and comprehensive testing.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-19-61DAFB)](https://react.dev/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![Tests](https://img.shields.io/badge/Tests-2100+-green.svg)](tests/)

## Quick Start

```bash
# Clone and build
git clone https://github.com/NOIR-Solution/NOIR.git
cd NOIR
dotnet build src/NOIR.sln

# Start backend (terminal 1)
dotnet run --project src/NOIR.Web

# Start frontend with hot reload (terminal 2)
cd src/NOIR.Web/frontend
npm install && npm run dev

# Access: http://localhost:3000
# API Docs: http://localhost:3000/api/docs
# Admin: admin@noir.local / 123qwe
```

> **Production-like mode:** Build with `dotnet build -c Release` (auto-builds frontend), then access `http://localhost:4000` directly.

**Requirements:** .NET 10 SDK, Node.js 20+, SQL Server (LocalDB on Windows, Docker on macOS/Linux)

## Features

- **Multi-Tenancy** - Finbuckle.MultiTenant with automatic query filtering
- **Authentication** - JWT + refresh tokens with cookie support
- **Authorization** - Role-based + permission-based (`resource:action:scope`)
- **Audit Logging** - 3-level tracking (HTTP, Handler, Entity)
- **Soft Delete** - Data safety with GDPR-ready hard delete
- **Background Jobs** - Hangfire with dashboard
- **API Documentation** - Scalar with OpenAPI spec

## Tech Stack

| Layer | Technologies |
|-------|--------------|
| **Backend** | .NET 10, EF Core 10, SQL Server, Wolverine, FluentValidation, Mapperly |
| **Frontend** | React 19, TypeScript, Vite, Tailwind CSS 4, shadcn/ui, React Router 7 |
| **Infrastructure** | Hangfire, Serilog, Finbuckle.MultiTenant, FluentStorage, FluentEmail |

## Project Structure

```
NOIR/
├── src/
│   ├── NOIR.Domain/           # Entities, interfaces, specifications
│   ├── NOIR.Application/      # Commands, queries, DTOs, behaviors
│   ├── NOIR.Infrastructure/   # EF Core, handlers, services
│   └── NOIR.Web/              # API endpoints, middleware
│       └── frontend/          # React SPA
├── tests/                     # 2,100+ tests
├── docs/                      # Documentation
│   ├── backend/               # Backend patterns & guides
│   ├── frontend/              # Frontend architecture
│   └── decisions/             # Architecture Decision Records
└── .github/                   # GitHub templates
```

## Documentation

| Document | Purpose |
|----------|---------|
| **[docs/KNOWLEDGE_BASE.md](docs/KNOWLEDGE_BASE.md)** | Comprehensive cross-referenced codebase guide |
| **[docs/](docs/README.md)** | Complete documentation index |
| **[CONTRIBUTING.md](CONTRIBUTING.md)** | How to contribute |
| **[CLAUDE.md](CLAUDE.md)** | AI assistant instructions (Claude Code) |
| **[AGENTS.md](AGENTS.md)** | Universal AI agent guidelines |

### Quick Links

- [Backend Architecture](docs/backend/README.md) - Clean Architecture, patterns, APIs
- [Frontend Guide](docs/frontend/README.md) - React SPA structure, theming
- [Architecture Decisions](docs/decisions/README.md) - ADRs for tech choices

## Commands

```bash
# Development
dotnet watch --project src/NOIR.Web          # Hot reload
dotnet test src/NOIR.sln                      # Run all tests

# Database
dotnet ef migrations add NAME --project src/NOIR.Infrastructure --startup-project src/NOIR.Web

# Frontend
cd src/NOIR.Web/frontend
npm run dev                                   # Dev server
npm run generate:api                          # Sync types from backend
```

## Contributing

We welcome contributions! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Follow coding standards in [CLAUDE.md](CLAUDE.md)
4. Run tests (`dotnet test src/NOIR.sln`)
5. Submit a pull request

## License

This project is licensed under the Apache License 2.0 - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [Jason Taylor's Clean Architecture](https://github.com/jasontaylordev/CleanArchitecture)
- [Wolverine](https://wolverinefx.net/) for CQRS messaging
- [shadcn/ui](https://ui.shadcn.com/) for React components
