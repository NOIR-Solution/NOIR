# NOIR

A modern, enterprise-ready .NET 10 + React SaaS foundation project with multi-tenancy support.

## Purpose

NOIR is a production-ready boilerplate designed to accelerate development of business applications of any size. It provides a comprehensive architecture with carefully selected, **free and open-source** libraries.

### Key Goals

- **Multi-tenant by design** - Finbuckle multi-tenancy from day one
- **Enterprise patterns** - Clean Architecture, CQRS, DDD
- **Zero licensing costs** - All libraries are free (MIT/Apache 2.0)
- **Future-proof** - .NET 10 LTS, modern libraries with active maintenance
- **Developer friendly** - Clear structure, best practices built-in
- **API-first** - All endpoints under `/api` prefix for React SPA integration
- **Fully tested** - 1,739 tests with comprehensive coverage

## Tech Stack

### Backend (.NET 10 LTS)

| Category | Technology |
|----------|------------|
| Framework | .NET 10 LTS (support until November 2028) |
| Architecture | Clean Architecture + CQRS + DDD |
| Database | SQL Server / Entity Framework Core 10 |
| Authentication | ASP.NET Core Identity + JWT (with refresh token rotation) |
| Authorization | Database-backed Permission System with caching |
| CQRS/Messaging | Wolverine |
| Validation | FluentValidation |
| Object Mapping | Mapperly (source-generated) |
| Logging | Serilog |
| Background Jobs | Hangfire |
| API Documentation | Scalar (OpenAPI) |
| Health Monitoring | AspNetCore.HealthChecks.SqlServer |
| File Storage | FluentStorage (Local/Azure/AWS S3) |
| Email | FluentEmail (SMTP + Razor templates) |
| Multi-Tenancy | Finbuckle.MultiTenant |
| DI Auto-Registration | Scrutor |

### Frontend

| Category | Technology |
|----------|------------|
| Framework | React (planned) |
| Hosting | Served from .NET backend |

### Testing

| Category | Technology |
|----------|------------|
| Framework | xUnit |
| Mocking | Moq |
| Assertions | FluentAssertions |
| Fake Data | Bogus |
| Architecture | ArchUnitNET |
| Coverage | Coverlet |
| Database | SQL Server LocalDB (same as production, no SQLite/InMemory) |
| Reset | Respawner (fast database cleanup between tests) |

## Features

### Implemented

- **Multi-Tenancy** - Finbuckle with header (`X-Tenant`) and JWT claim detection
- **Authentication** - ASP.NET Core Identity + JWT with access/refresh tokens
- **JWT Token Rotation** - Secure refresh token with theft detection via family tracking
- **Device Fingerprinting** - Optional token binding to device characteristics
- **Hierarchical Audit Logging** - HTTP request, handler, and entity-level change tracking
- **Permission System** - Database-backed RBAC with real-time cache invalidation
- **User Management** - Full CRUD for users with role assignment
- **Role Management** - Full CRUD for roles with permission assignment
- **Resource-Based Authorization** - Owner and share-based access control
- **CQRS** - Wolverine for command/query handling with FluentValidation pipeline
- **Rate Limiting** - Fixed window (100/min) + Sliding window for auth (5/min anti-brute-force)
- **Health Monitoring** - SQL Server health check at `/api/health`
- **API Documentation** - Scalar interactive docs at `/api/docs`
- **Structured Logging** - Serilog with console sink and request correlation
- **DI Auto-Registration** - Scrutor with marker interfaces
- **Entity Configuration** - IEntityTypeConfiguration with auto-discovery
- **Global Conventions** - Consistent string lengths, decimal precision, UTC storage
- **Security Headers** - X-Frame-Options, X-Content-Type-Options, path-specific CSP (strict for API, CDN-enabled for docs)
- **Response Compression** - Gzip + Brotli
- **Output Caching** - Server-side caching with policies
- **Background Jobs** - Hangfire with dashboard at `/hangfire`

### Planned

- **React Frontend** - Next development phase

## Getting Started

### Prerequisites

- .NET 10 SDK
- SQL Server (or SQL Server LocalDB)
- Node.js (for React frontend - planned)

### Installation

```bash
# Clone the repository
git clone https://github.com/yourusername/noir.git
cd noir

# Restore dependencies
dotnet restore src/NOIR.sln

# Run the application (database auto-migrates on startup)
dotnet run --project src/NOIR.Web
```

The application will:
1. Create the database (SQL Server LocalDB)
2. Apply migrations automatically
3. Seed admin user: `admin@noir.local` / `123qwe`
4. Start at http://localhost:5000

### Development

```bash
# Run with hot reload
dotnet watch --project src/NOIR.Web

# Run tests
dotnet test src/NOIR.sln

# Run tests with coverage
dotnet test src/NOIR.sln --collect:"XPlat Code Coverage"

# Generate coverage report
reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"TestResults/CoverageReport" -reporttypes:Html

# Add a migration
dotnet ef migrations add MigrationName --project src/NOIR.Infrastructure --startup-project src/NOIR.Web
```

## Project Structure

```
NOIR/
├── src/
│   ├── NOIR.Domain/           # Entities, value objects, domain interfaces
│   ├── NOIR.Application/      # Commands, queries, DTOs, validators, specifications
│   ├── NOIR.Infrastructure/   # EF Core, Identity, external services, configurations
│   └── NOIR.Web/              # API endpoints, middleware, Program.cs
├── tests/
│   ├── NOIR.Domain.UnitTests/       # Domain entity tests (469 tests)
│   ├── NOIR.Application.UnitTests/  # Handler, service, validator tests (962 tests)
│   ├── NOIR.IntegrationTests/       # End-to-end API tests with SQL Server LocalDB (283 tests)
│   └── NOIR.ArchitectureTests/      # Layer dependency validation (25 tests)
├── .claude/                   # Development documentation
│   ├── decisions/             # Architecture decision records
│   ├── patterns/              # Code patterns documentation
│   └── brainstorming/         # Research notes
├── CLAUDE.md                  # Claude Code development instructions
└── README.md
```

## Key Patterns

### Service Registration (Auto via Scrutor)

```csharp
// Just add marker interface - auto-registered!
public class CustomerService : ICustomerService, IScopedService
{
    // Implementation
}
```

### Entity Configuration (Auto-discovered)

```csharp
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(e => e.Id);
    }
}
```

### Specifications for Queries

```csharp
public class ActiveCustomersSpec : Specification<Customer>
{
    public ActiveCustomersSpec()
    {
        Query.Where(c => c.IsActive)
             .TagWith("GetActiveCustomers");
    }
}
```

### Handlers (Wolverine)

```csharp
public static class CreateOrderHandler
{
    public static async Task<Result<OrderDto>> Handle(
        CreateOrderCommand cmd,
        IRepository<Order, Guid> repo,
        CancellationToken ct)
    {
        // Implementation
    }
}
```

## Configuration

### Database

Configure your connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=NOIR;Trusted_Connection=True;"
  }
}
```

### JWT Settings

```json
{
  "JwtSettings": {
    "Secret": "your-256-bit-secret-key",
    "Issuer": "NOIR",
    "Audience": "NOIR.Web",
    "ExpirationInMinutes": 60,
    "RefreshTokenExpirationInDays": 7,
    "EnableDeviceFingerprinting": true,
    "MaxConcurrentSessions": 5
  }
}
```

### Rate Limiting

```json
{
  "RateLimiting": {
    "PermitLimit": 100,
    "WindowMinutes": 1,
    "AuthPermitLimit": 5,
    "AuthWindowMinutes": 1
  }
}
```

### Multi-Tenancy

Tenants are resolved via header or JWT claim:

```
X-Tenant: tenant1   # Header-based
tenant_id: tenant1  # JWT claim-based
```

## API Documentation

Once running, access the API documentation at:

```
http://localhost:5000/api/docs
```

OpenAPI specification is available at:
```
http://localhost:5000/api/openapi/v1.json
```

## API Endpoints

### Authentication
| Endpoint | Method | Auth | Rate Limit | Purpose |
|----------|--------|------|------------|---------|
| `/api/auth/register` | POST | No | 5/min | Create new user account |
| `/api/auth/login` | POST | No | 5/min | Authenticate user |
| `/api/auth/refresh` | POST | No | 5/min | Refresh access token |
| `/api/auth/me` | GET | Yes | 100/min | Get current user profile |
| `/api/auth/me` | PUT | Yes | 100/min | Update user profile |

### User Management (Admin)
| Endpoint | Method | Auth | Purpose |
|----------|--------|------|---------|
| `/api/users` | GET | Admin | List users (paginated) |
| `/api/users/{id}` | GET | Admin | Get user by ID |
| `/api/users/{id}` | PUT | Admin | Update user |
| `/api/users/{id}` | DELETE | Admin | Soft delete user |
| `/api/users/{id}/roles` | PUT | Admin | Assign roles to user |

### Role Management (Admin)
| Endpoint | Method | Auth | Purpose |
|----------|--------|------|---------|
| `/api/roles` | GET | Admin | List all roles |
| `/api/roles/{id}` | GET | Admin | Get role with permissions |
| `/api/roles` | POST | Admin | Create new role |
| `/api/roles/{id}` | DELETE | Admin | Delete role |
| `/api/roles/{id}/permissions` | POST | Admin | Assign permissions to role |
| `/api/roles/{id}/permissions` | DELETE | Admin | Remove permissions from role |

### Permissions (Admin)
| Endpoint | Method | Auth | Purpose |
|----------|--------|------|---------|
| `/api/permissions` | GET | Admin | List all available permissions |

### System
| Endpoint | Method | Auth | Purpose |
|----------|--------|------|---------|
| `/api/health` | GET | No | Health check status |
| `/api/docs` | GET | No | Scalar API documentation |
| `/hangfire` | GET | Admin | Hangfire job dashboard |

## Health Check

Monitor application health at:

```
http://localhost:5000/api/health
```

Returns JSON with database connectivity status.

## License

This project is licensed under the MIT License.

## Contributing

Contributions are welcome! Please read our contributing guidelines before submitting PRs.

## Acknowledgments

Built with these amazing open-source projects:

- [Wolverine](https://wolverinefx.net/) - CQRS & Messaging
- [Finbuckle.MultiTenant](https://www.finbuckle.com/MultiTenant) - Multi-tenancy
- [Mapperly](https://mapperly.riok.app/) - Object mapping
- [FluentValidation](https://docs.fluentvalidation.net/) - Validation
- [Serilog](https://serilog.net/) - Logging
- [Hangfire](https://www.hangfire.io/) - Background jobs
- [FluentEmail](https://github.com/lukencode/FluentEmail) - Email
- [FluentStorage](https://github.com/robinrodricks/FluentStorage) - File storage
- [Scalar](https://scalar.com/) - API documentation
- [Scrutor](https://github.com/khellang/Scrutor) - DI auto-registration
