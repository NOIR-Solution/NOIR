# Project Index: NOIR

**Generated:** 2026-01-03
**Framework:** .NET 10 LTS
**Architecture:** Clean Architecture + CQRS + DDD
**Tests:** 1,739+

## Project Structure

```
NOIR/
├── src/
│   ├── NOIR.Domain/           # Core entities, interfaces, value objects
│   ├── NOIR.Application/      # Commands, queries, specifications, DTOs
│   ├── NOIR.Infrastructure/   # EF Core, handlers, external services
│   └── NOIR.Web/              # API endpoints, middleware, Program.cs
├── tests/
│   ├── NOIR.Domain.UnitTests/       # Domain unit tests
│   ├── NOIR.Application.UnitTests/  # Application unit tests
│   ├── NOIR.ArchitectureTests/      # Architecture constraint tests
│   └── NOIR.IntegrationTests/       # Integration tests
└── .claude/                   # AI dev documentation
```

## Entry Points

| Entry Point | Path | Purpose |
|-------------|------|---------|
| Web API | `src/NOIR.Web/Program.cs:1` | Application bootstrap, middleware pipeline |
| API Docs | `/api/docs` | Scalar API reference UI |
| OpenAPI | `/api/openapi/v1.json` | OpenAPI specification |
| Health | `/api/health` | Health check endpoint |
| Hangfire | `/hangfire` | Background jobs dashboard |

## Core Modules

### NOIR.Domain
Core business entities and domain logic (no dependencies).

| Symbol | Purpose |
|--------|---------|
| `Entity<TId>` | Base entity with ID |
| `AuditableEntity<TId>` | Entity with created/modified tracking |
| `AggregateRoot<TId>` | DDD aggregate root with domain events |
| `Result<T>` | Railway-oriented result pattern |
| `IRepository<TEntity, TId>` | Generic repository interface |
| `ISpecification<T>` | Specification pattern interface |

**Entities:**
- `Permission` - RBAC permissions
- `RefreshToken` - JWT refresh token management
- `ResourceShare` - Resource sharing between users
- `EntityAuditLog` - Entity change audit trail
- `HandlerAuditLog` - CQRS handler audit
- `HttpRequestAuditLog` - HTTP request audit

### NOIR.Application
CQRS commands, queries, specifications, and DTOs.

**Features:**
| Feature | Commands | Queries |
|---------|----------|---------|
| Auth | Login, Register, RefreshToken, UpdateUserProfile | GetCurrentUser, GetUserById |
| Users | UpdateUser, DeleteUser, AssignRoles | GetUsers, GetUserRoles |
| Roles | CreateRole, UpdateRole, DeleteRole | GetRoles, GetRoleById |
| Permissions | AssignToRole, RemoveFromRole | GetRolePermissions, GetUserPermissions |
| Audit | - | GetAuditTrail, GetEntityHistory, GetHttpRequestAuditLogs, ExportAuditLogs |

**Specifications:**
- `RefreshTokens/` - Token retrieval specifications
- `ResourceShares/` - Resource sharing specifications

**Common:**
- `Interfaces/` - ITokenService, IEmailService, IFileStorage, etc.
- `Exceptions/` - NotFoundException, ValidationException, ForbiddenAccessException
- `Models/` - PagedResult, PaginatedList
- `Behaviors/` - LoggingMiddleware, PerformanceMiddleware

### NOIR.Infrastructure
EF Core implementation, external service integrations.

**Key Components:**
| Path | Purpose |
|------|---------|
| `Persistence/ApplicationDbContext.cs` | EF Core DbContext |
| `Persistence/Repositories/Repository.cs` | Generic repository |
| `Persistence/SpecificationEvaluator.cs` | Specification to IQueryable |
| `Persistence/Configurations/` | Entity type configurations |
| `Persistence/Interceptors/` | EF interceptors (audit, tenant, events) |
| `Identity/` | ASP.NET Core Identity + JWT |
| `Identity/Handlers/` | Wolverine command/query handlers |
| `Audit/` | Hierarchical audit logging |
| `Services/` | Email, file storage, datetime services |
| `BackgroundJobs/` | Hangfire job filters |

### NOIR.Web
Minimal API endpoints and middleware.

**Endpoints:**
| Group | Path | Description |
|-------|------|-------------|
| Auth | `/api/auth/*` | Login, register, refresh, profile |
| Users | `/api/users/*` | User management (admin) |
| Roles | `/api/roles/*` | Role management (admin) |
| Audit | `/api/audit/*` | Audit log queries |

**Middleware:**
- `ExceptionHandlingMiddleware` - Global error handling
- `SecurityHeadersMiddleware` - Security headers (CSP, HSTS, etc.)
- `HttpRequestAuditMiddleware` - Request/response audit capture

## Dependencies

### Core Packages
| Package | Version | Purpose |
|---------|---------|---------|
| WolverineFx | 5.9.2 | CQRS messaging/mediator |
| FluentValidation | 12.1.1 | Command/query validation |
| Riok.Mapperly | 4.3.1 | Source-generated object mapping |
| Finbuckle.MultiTenant | 10.0.1 | Multi-tenancy |
| Hangfire | 1.8.22 | Background jobs |

### Data & Storage
| Package | Version | Purpose |
|---------|---------|---------|
| EF Core SqlServer | 10.0.1 | SQL Server provider |
| ASP.NET Core Identity.EntityFrameworkCore | 10.0.1 | Identity storage |
| FluentStorage | 6.0.0 | File storage abstraction |
| FluentStorage.AWS | 6.0.1 | S3 support |
| FluentStorage.Azure.Blobs | 6.0.0 | Azure Blob support |

### Communication
| Package | Version | Purpose |
|---------|---------|---------|
| FluentEmail.Core | 3.0.2 | Email abstraction |
| FluentEmail.MailKit | 3.0.2 | SMTP via MailKit |
| FluentEmail.Razor | 3.0.2 | Razor email templates |

### Observability
| Package | Version | Purpose |
|---------|---------|---------|
| Serilog.AspNetCore | 10.0.0 | Structured logging |
| Scalar.AspNetCore | 2.11.10 | API documentation UI |
| AspNetCore.HealthChecks.SqlServer | 9.0.0 | SQL Server health check |

### Infrastructure
| Package | Version | Purpose |
|---------|---------|---------|
| Scrutor | 7.0.0 | DI auto-registration |
| SystemTextJson.JsonDiffPatch | 2.0.0 | JSON diff for audit |

## Configuration

### Settings (appsettings.json)
| Section | Key Settings |
|---------|--------------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection |
| `JwtSettings` | Secret, Issuer, Audience, Expiration |
| `Identity` | Password policy, lockout settings |
| `RateLimiting` | Request limits per window |
| `Email` | SMTP configuration |
| `Storage` | Local/S3/Azure blob config |
| `AuditRetention` | Audit log retention policy |
| `Finbuckle:MultiTenant` | Tenant configuration |

### Environment Variables
- `ASPNETCORE_ENVIRONMENT` - Development/Production/Testing
- `ConnectionStrings__DefaultConnection` - Override connection string

## Quick Start

```bash
# Build
dotnet build src/NOIR.sln

# Run (requires LocalDB on Windows, or configure SQL Server)
dotnet run --project src/NOIR.Web

# Run with hot reload
dotnet watch --project src/NOIR.Web

# Run tests
dotnet test src/NOIR.sln

# Create migration
dotnet ef migrations add MigrationName \
    --project src/NOIR.Infrastructure \
    --startup-project src/NOIR.Web
```

**Default Credentials:**
- Email: `admin@noir.local`
- Password: `123qwe`

## Test Coverage

| Project | Focus |
|---------|-------|
| NOIR.Domain.UnitTests | Entity, value object, result pattern tests |
| NOIR.Application.UnitTests | Handler, validator, specification tests |
| NOIR.ArchitectureTests | Layer dependency enforcement |
| NOIR.IntegrationTests | End-to-end API tests with real database |

## Patterns Reference

| Pattern | Location | Documentation |
|---------|----------|---------------|
| Repository + Specification | `NOIR.Infrastructure/Persistence/` | `.claude/patterns/repository-specification.md` |
| DI Auto-Registration | `ServiceLifetimes.cs` | `.claude/patterns/di-auto-registration.md` |
| Entity Configuration | `Persistence/Configurations/` | `.claude/patterns/entity-configuration.md` |
| JWT Refresh Tokens | `Identity/RefreshTokenService.cs` | `.claude/patterns/jwt-refresh-token.md` |
| Hierarchical Audit | `Audit/` | `.claude/patterns/hierarchical-audit-logging.md` |

## Architecture Decisions

| ADR | Decision |
|-----|----------|
| [001-tech-stack](.claude/decisions/001-tech-stack.md) | Technology stack choices |
| [002-frontend-ui-stack](.claude/decisions/002-frontend-ui-stack.md) | Frontend technology choices |

## Key Files for Common Tasks

| Task | Files to Check |
|------|----------------|
| Add new entity | `Domain/Entities/`, `Infrastructure/Persistence/Configurations/` |
| Add new endpoint | `Web/Endpoints/` |
| Add command/query | `Application/Features/<Feature>/Commands/` or `Queries/` |
| Add handler | `Infrastructure/Identity/Handlers/` or feature-specific |
| Add specification | `Application/Specifications/` |
| Add validation | `Application/Features/<Feature>/Commands/<Command>Validator.cs` |
| Configure DI | Add marker interface (`IScopedService`, etc.) |
