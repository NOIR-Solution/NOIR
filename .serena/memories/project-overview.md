# NOIR Project Overview

## Project Type
Enterprise .NET web application with React SPA frontend.

## Architecture
- **Pattern**: Clean Architecture + CQRS + DDD
- **Layers**:
  - `NOIR.Domain` - Core entities, interfaces, specifications
  - `NOIR.Application` - Commands, queries, DTOs, behaviors
  - `NOIR.Infrastructure` - EF Core, handlers, external services
  - `NOIR.Web` - Minimal API endpoints, middleware, React SPA

## Tech Stack

### Backend (.NET 10 LTS)
| Category | Technology |
|----------|------------|
| ORM | Entity Framework Core 10 |
| Database | SQL Server |
| Auth | ASP.NET Core Identity + JWT |
| Messaging/CQRS | Wolverine |
| Validation | FluentValidation |
| Mapping | Mapperly (compile-time) |
| Multi-Tenancy | Finbuckle.MultiTenant |
| Background Jobs | Hangfire |
| Logging | Serilog |

### Frontend (React 19)
| Category | Technology |
|----------|------------|
| Build | Vite |
| Styling | Tailwind CSS 4 |
| Components | shadcn/ui + 21st.dev |
| Routing | React Router 7 |

## Key Features
- Multi-tenant architecture
- 3-level audit logging (HTTP, Handler, Entity)
- JWT with refresh token rotation
- Role-based + permission-based authorization
- Soft delete by default (GDPR-ready hard delete)
- Bulk operations for high-volume data

## File Locations
- Source: `src/`
- Tests: `tests/` (1,800+ tests)
- Documentation: `docs/`
- AI instructions: `CLAUDE.md`, `AGENTS.md`

## Quick Commands
```bash
# Build
dotnet build src/NOIR.sln

# Run backend (terminal 1)
dotnet run --project src/NOIR.Web

# Run frontend with hot reload (terminal 2)
cd src/NOIR.Web/frontend && npm install && npm run dev

# Tests
dotnet test src/NOIR.sln
```

> Production-like: `dotnet build -c Release` (auto-builds frontend), access `localhost:4000`

## Development URLs
| URL | Purpose |
|-----|---------|
| `http://localhost:3000` | Application (frontend + API via proxy) |
| `http://localhost:3000/api/docs` | API documentation (Scalar) |
| `http://localhost:3000/hangfire` | Background jobs dashboard |

> Port 4000 serves backend directly for production-like testing.

## Database Setup
- **Windows**: SQL Server LocalDB (default, included with VS)
- **macOS/Linux**: Docker (Azure SQL Edge for ARM64/M1, SQL Server for x64)

## Admin Credentials
- Email: `admin@noir.local`
- Password: `123qwe`
