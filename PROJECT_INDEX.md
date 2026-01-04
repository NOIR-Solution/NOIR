# Project Index: NOIR

**Generated:** 2026-01-04
**Type:** Enterprise .NET 10 + React SaaS Foundation
**Architecture:** Clean Architecture, CQRS, DDD

---

## Project Structure

```
NOIR/
├── src/
│   ├── NOIR.Domain/           # Core entities, interfaces (no dependencies)
│   ├── NOIR.Application/      # Commands, queries, specifications, DTOs
│   ├── NOIR.Infrastructure/   # EF Core, handlers, external services
│   └── NOIR.Web/              # API endpoints, middleware
│       └── frontend/          # React 19 SPA
├── tests/
│   ├── NOIR.Domain.UnitTests/       # 488 tests
│   ├── NOIR.Application.UnitTests/  # 993 tests
│   ├── NOIR.ArchitectureTests/      # 25 tests
│   └── NOIR.IntegrationTests/       # 302 tests
├── docs/
│   ├── backend/patterns/      # 6 pattern docs
│   ├── backend/research/      # 3 research docs
│   ├── frontend/              # 4 frontend docs
│   └── decisions/             # 2 ADRs
└── .claude/commands/          # 3 Claude Code skills
```

---

## Entry Points

| Entry Point | Path | Purpose |
|-------------|------|---------|
| **Backend API** | `src/NOIR.Web/Program.cs` | .NET 10 minimal API |
| **Frontend** | `src/NOIR.Web/frontend/src/main.tsx` | React 19 SPA |
| **Solution** | `src/NOIR.sln` | All projects |

---

## Core Modules

### Domain Layer (`src/NOIR.Domain/`)
| Module | Purpose |
|--------|---------|
| `Entities/` | EntityAuditLog, HandlerAuditLog, HttpRequestAuditLog, Permission, RefreshToken, ResourceShare |
| `Common/` | Entity, AuditableEntity, AggregateRoot, ValueObject, Result, Permissions, Roles |
| `Interfaces/` | IRepository, ISpecification, IUnitOfWork |

### Application Layer (`src/NOIR.Application/`)
| Module | Purpose |
|--------|---------|
| `Features/Auth/` | Login, Logout, Register, GetCurrentUser, UpdateProfile |
| `Features/Users/` | CRUD, AssignRoles, GetUserRoles |
| `Features/Roles/` | CRUD, Permissions management |
| `Features/Permissions/` | Assign/Remove permissions to roles |
| `Features/Audit/` | GetAuditTrail, ExportAuditLogs, EntityHistory |
| `Behaviors/` | LoggingMiddleware, PerformanceMiddleware |
| `Common/Interfaces/` | ICurrentUser, ITokenService, IEmailService, IFileStorage |

### Infrastructure Layer (`src/NOIR.Infrastructure/`)
| Module | Purpose |
|--------|---------|
| `Persistence/` | ApplicationDbContext, Repositories, Specifications |
| `Identity/` | ApplicationUser, TokenService, JwtSettings |
| `Identity/Authorization/` | PermissionHandler, ResourceAuthorization |
| `Audit/` | HandlerAuditMiddleware, HttpRequestAuditMiddleware |
| `Services/` | EmailService, FileStorage, BackgroundJobs |

### Web Layer (`src/NOIR.Web/`)
| Module | Purpose |
|--------|---------|
| `Endpoints/` | Minimal API endpoints (Auth, Users, Roles, Audit) |
| `Middleware/` | ExceptionHandling, SecurityHeaders |
| `frontend/` | React 19 SPA with Vite |

---

## Key Technologies

### Backend
| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 10.0 | Runtime (LTS until Nov 2028) |
| EF Core | 10.0 | ORM |
| SQL Server | Latest | Primary database |
| Wolverine | Latest | CQRS handlers, messaging |
| Hangfire | Latest | Background jobs |
| FluentValidation | Latest | Request validation |
| Mapperly | Latest | Compile-time mapping |
| Serilog | Latest | Structured logging |

### Frontend
| Technology | Version | Purpose |
|------------|---------|---------|
| React | 19.x | UI library |
| TypeScript | 5.9 | Type safety |
| Vite | 7.x | Build tool |
| Tailwind CSS | 4.x | Styling |
| React Router | 7.x | Routing |
| shadcn/ui | Latest | UI components |
| i18next | Latest | Internationalization |

---

## API Endpoints

### Authentication (`/api/auth/`)
- `POST /login` - User login
- `POST /logout` - User logout
- `POST /register` - User registration
- `POST /refresh` - Token refresh
- `GET /me` - Current user info

### Users (`/api/users/`)
- `GET /` - List users (paginated)
- `GET /{id}` - Get user by ID
- `PUT /{id}` - Update user
- `DELETE /{id}` - Delete user (soft)
- `GET /{id}/roles` - Get user roles
- `POST /{id}/roles` - Assign roles

### Roles (`/api/roles/`)
- `GET /` - List roles
- `POST /` - Create role
- `PUT /{id}` - Update role
- `DELETE /{id}` - Delete role
- `GET /{id}/permissions` - Get role permissions
- `POST /{id}/permissions` - Assign permissions

### Audit (`/api/audit/`)
- `GET /http` - HTTP request logs
- `GET /handlers` - Handler execution logs
- `GET /entities` - Entity change logs
- `GET /entities/{type}/{id}` - Entity history

---

## Configuration

| File | Purpose |
|------|---------|
| `src/NOIR.Web/appsettings.json` | Base configuration |
| `src/NOIR.Web/appsettings.Development.json` | Dev overrides |
| `docker-compose.yml` | SQL Server + MailHog |
| `Dockerfile` | Production container |

---

## Quick Commands

```bash
# Build & Run
dotnet build src/NOIR.sln
dotnet run --project src/NOIR.Web
dotnet watch --project src/NOIR.Web

# Tests (1,808 total)
dotnet test src/NOIR.sln

# Frontend
cd src/NOIR.Web/frontend
npm install && npm run dev
npm run generate:api  # Sync types from backend

# Database
dotnet ef migrations add NAME --project src/NOIR.Infrastructure --startup-project src/NOIR.Web

# Docker
docker-compose up -d  # Start SQL Server + MailHog
```

---

## Test Coverage

| Project | Tests | Duration |
|---------|-------|----------|
| Domain.UnitTests | 488 | ~250ms |
| Application.UnitTests | 993 | ~1s |
| ArchitectureTests | 25 | ~1s |
| IntegrationTests | 302 | ~40s |
| **Total** | **1,808** | ~43s |

---

## Documentation

### Backend
- [Repository & Specification](docs/backend/patterns/repository-specification.md)
- [DI Auto-Registration](docs/backend/patterns/di-auto-registration.md)
- [JWT Refresh Tokens](docs/backend/patterns/jwt-refresh-token.md)
- [Audit Logging](docs/backend/patterns/hierarchical-audit-logging.md)
- [Bulk Operations](docs/backend/patterns/bulk-operations.md)
- [Entity Configuration](docs/backend/patterns/entity-configuration.md)

### Frontend
- [Architecture](docs/frontend/architecture.md)
- [Theme](docs/frontend/theme.md)
- [API Types](docs/frontend/api-types.md)
- [Localization](docs/frontend/localization-guide.md)

### AI Instructions
- [CLAUDE.md](CLAUDE.md) - Claude Code specific
- [AGENTS.md](AGENTS.md) - Universal AI agents

---

## Critical Patterns

1. **Use Specifications** for all database queries
2. **Tag all specifications** with `TagWith("MethodName")`
3. **Use IUnitOfWork** for persistence (repos don't auto-save)
4. **Use AsTracking** for mutation specs
5. **Soft delete only** - Never hard delete
6. **Marker interfaces** for DI: `IScopedService`, `ITransientService`, `ISingletonService`

---

## File Statistics

| Category | Count |
|----------|-------|
| C# Source Files | 229 |
| C# Test Files | 98 |
| Frontend TS/TSX | 36 |
| Documentation (MD) | 46 |
| Projects (csproj) | 8 |

---

## Default Credentials

- **Email:** `admin@noir.local`
- **Password:** `123qwe`

---

*Index size: ~4KB | Full codebase read: ~60KB+ tokens*
