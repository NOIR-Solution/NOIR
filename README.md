# NOIR

A modern, enterprise-ready .NET + React SaaS base project with multi-tenancy support.

## Purpose

NOIR is a foundation/boilerplate project designed to accelerate development of business applications of any size. It provides a production-ready architecture with carefully selected, **free and open-source** libraries.

### Key Goals

- **Multi-tenant by design** - One deployment serves multiple customers
- **Enterprise patterns** - Clean Architecture, CQRS, DDD
- **Zero licensing costs** - All libraries are free (MIT/Apache 2.0)
- **Future-proof** - Modern libraries with active maintenance
- **Developer friendly** - Clear structure, best practices built-in

## Tech Stack

### Backend (.NET 10 LTS)

| Category | Technology |
|----------|------------|
| Framework | .NET 10 LTS |
| Architecture | Clean Architecture + CQRS + DDD |
| Database | SQL Server / Entity Framework Core |
| Authentication | ASP.NET Core Identity + JWT |
| CQRS/Messaging | Wolverine |
| Validation | FluentValidation |
| Object Mapping | Mapperly |
| Logging | Serilog |
| Background Jobs | Hangfire |
| API Documentation | Scalar |
| Health Monitoring | AspNetCore.HealthChecks.UI |
| File Storage | FluentStorage |
| Email | FluentEmail |
| Multi-Tenancy | Finbuckle.MultiTenant |

### Frontend

| Category | Technology |
|----------|------------|
| Framework | React |
| Hosting | Served from .NET backend |

### Testing

| Category | Technology |
|----------|------------|
| Framework | xUnit |
| Mocking | Moq |
| Assertions | FluentAssertions |
| Fake Data | Bogus |

## Features

- **Multi-Tenancy** - Tenant isolation with subdomain/header/path detection
- **Authentication** - JWT-based auth with role/claims support
- **Background Jobs** - Scheduled and queued job processing with dashboard
- **Health Monitoring** - Real-time health dashboard with history
- **API Documentation** - Auto-generated interactive API docs
- **File Storage** - Abstracted storage (local, Azure, AWS, GCP)
- **Email Templates** - Razor-based email templating
- **Rate Limiting** - Built-in API rate limiting
- **Structured Logging** - Serilog with multiple sinks

## Getting Started

### Prerequisites

- .NET 10 SDK
- SQL Server (or SQL Server LocalDB)
- Node.js (for React frontend)

### Installation

```bash
# Clone the repository
git clone https://github.com/yourusername/noir.git
cd noir

# Restore dependencies
dotnet restore

# Update database
dotnet ef database update

# Run the application
dotnet run
```

### Development

```bash
# Run with hot reload
dotnet watch run

# Run tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Project Structure

```
NOIR/
├── src/
│   ├── NOIR.Domain/           # Entities, value objects, domain events
│   ├── NOIR.Application/      # Use cases, commands, queries, interfaces
│   ├── NOIR.Infrastructure/   # Database, external services, implementations
│   └── NOIR.API/              # Controllers, middleware, configuration
├── tests/
│   ├── NOIR.UnitTests/        # Unit tests
│   └── NOIR.IntegrationTests/ # Integration tests
└── README.md
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

### Multi-Tenancy

Tenants are resolved via subdomain by default:

```
tenant1.yourapp.com → Tenant 1
tenant2.yourapp.com → Tenant 2
```

## API Documentation

Once running, access the API documentation at:

```
https://localhost:5001/scalar/v1
```

## Health Dashboard

Monitor application health at:

```
https://localhost:5001/healthchecks-ui
```

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
