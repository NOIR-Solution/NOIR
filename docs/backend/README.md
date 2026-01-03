# Backend Documentation

NOIR backend is built with .NET 10 LTS using Clean Architecture, CQRS, and DDD patterns.

## Architecture Overview

```
src/
├── NOIR.Domain/           # Core business logic
│   ├── Entities/          # Domain entities
│   ├── Common/            # Shared types (Entity, IAuditableEntity)
│   └── Interfaces/        # Repository contracts
│
├── NOIR.Application/      # Use cases
│   ├── Features/          # Commands, Queries, DTOs
│   ├── Specifications/    # Query specifications
│   └── Common/            # Shared services, interfaces
│
├── NOIR.Infrastructure/   # External concerns
│   ├── Persistence/       # EF Core, Repositories
│   ├── Identity/          # Authentication
│   └── Services/          # External service implementations
│
└── NOIR.Web/              # Presentation
    ├── Endpoints/         # Minimal API endpoints
    ├── Middleware/        # HTTP middleware
    └── frontend/          # React SPA
```

## Key Technologies

| Category | Technology | Purpose |
|----------|------------|---------|
| Framework | .NET 10 LTS | Target framework (support until Nov 2028) |
| ORM | Entity Framework Core 10 | Database access |
| Database | SQL Server | Primary data store |
| Auth | ASP.NET Core Identity + JWT | Authentication/Authorization |
| Messaging | Wolverine | CQRS handlers, in-process messaging |
| Validation | FluentValidation | Request validation |
| Mapping | Mapperly | Compile-time object mapping |
| Multi-Tenancy | Finbuckle.MultiTenant | Tenant isolation |
| Background Jobs | Hangfire | Scheduled/background tasks |
| Logging | Serilog | Structured logging |

## Quick Start

```bash
# Build
dotnet build src/NOIR.sln

# Run (with hot reload)
dotnet watch --project src/NOIR.Web

# Run tests
dotnet test src/NOIR.sln

# Create migration
dotnet ef migrations add MigrationName \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web
```

## Code Patterns

| Pattern | Document | Purpose |
|---------|----------|---------|
| Repository & Specification | [patterns/repository-specification.md](patterns/repository-specification.md) | Data access abstraction |
| DI Auto-Registration | [patterns/di-auto-registration.md](patterns/di-auto-registration.md) | Service registration |
| Entity Configuration | [patterns/entity-configuration.md](patterns/entity-configuration.md) | EF Core entity setup |
| JWT Refresh Tokens | [patterns/jwt-refresh-token.md](patterns/jwt-refresh-token.md) | Secure token handling |
| Audit Logging | [patterns/hierarchical-audit-logging.md](patterns/hierarchical-audit-logging.md) | Change tracking |
| Bulk Operations | [patterns/bulk-operations.md](patterns/bulk-operations.md) | High-volume data handling |

## Research

| Topic | Document |
|-------|----------|
| Audit Logging Comparison | [research/hierarchical-audit-logging-comparison-2025.md](research/hierarchical-audit-logging-comparison-2025.md) |
| Role & Permission Systems | [research/role-permission-best-practices-2025.md](research/role-permission-best-practices-2025.md) |

## Critical Rules

1. **Use Specifications** for all database queries - never raw `DbSet` queries in services
2. **Tag all specifications** with `TagWith("MethodName")` for SQL debugging
3. **Soft delete only** - never hard delete unless explicitly required for GDPR
4. **No using statements in files** - add to `GlobalUsings.cs` in each project
5. **Use marker interfaces** for DI - `IScopedService`, `ITransientService`, `ISingletonService`

## Performance Guidelines

| Scenario | Use |
|----------|-----|
| Read-only queries | `AsNoTracking` (default) |
| Multiple collections | `.AsSplitQuery()` |
| Bulk operations (1000+ records) | Bulk extension methods |
| Complex joins | Specification with proper includes |
