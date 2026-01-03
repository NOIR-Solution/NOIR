# .NET Backend/API Base Project - Library & Framework Recommendations (2025)

**Created:** 2025-12-29
**Updated:** 2025-12-30
**Purpose:** Research for building an enterprise-ready .NET + React base project
**Focus:** Backend/API libraries and frameworks
**Status:** Core implementation complete, some features deferred

---

## 1. Framework & Runtime

| Package | Purpose |
|---------|---------|
| **.NET 10 LTS** | Target framework (LTS until November 2028) |

**Decision:** .NET 10 is the chosen target framework.
- Released: November 2025
- Support: LTS (3 years until November 2028)
- Features: Improved OpenAPI, Scalar UI built-in, better performance
- [What's new in .NET 10](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview)

---

## 2. Architecture Pattern

**Recommended: Clean Architecture + CQRS + DDD**

Use a layered structure:
- **Domain** - Core business rules, entities, value objects
- **Application** - Use cases, commands, queries, interfaces
- **Infrastructure** - Database, external services, file storage
- **API/Presentation** - Controllers, endpoints, middleware

Reference templates:
- [Jason Taylor's Clean Architecture](https://github.com/jasontaylordev/CleanArchitecture)
- [Kerim Kara's Ultimate Guide](https://medium.com/@kerimkkara/net-8-9-with-clean-architecture-ddd-cqrs-the-ultimate-2025-guide-2e9169c0296d)

---

## 3. Database & ORM

| Package | Purpose |
|---------|---------|
| `Microsoft.EntityFrameworkCore` | ORM framework |
| `Microsoft.EntityFrameworkCore.SqlServer` | SQL Server provider |
| `Microsoft.EntityFrameworkCore.Design` | Migrations tooling |

**Decision:** SQL Server only (no PostgreSQL).

| Environment | Database |
|-------------|----------|
| Development | SQL Server |
| Integration Tests | SQL Server LocalDB |
| Unit Tests | Mocked (no DB) |

[EF Core Migrations Documentation](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)

---

## 4. Authentication & Authorization

| Package | Purpose |
|---------|---------|
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | User management, roles, claims |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT token authentication |

**Decision:** ASP.NET Core Identity + JWT (simple, built-in, free forever).

- No external OAuth server needed
- User management with roles and claims
- JWT Bearer token authentication
- Can upgrade to OpenIddict later if OAuth/OIDC needed

[Authentication in .NET 2025 Guide](https://medium.com/@vikpoca/authentication-and-authorization-in-net-in-2025-6f70b601028f)

---

## 5. Mediator Pattern (CQRS)

**Important: MediatR is now commercial for new versions in 2025.**

| Package | License | Notes |
|---------|---------|-------|
| `MediatR` | Commercial (v13+) | Industry standard, now requires paid license |
| `Mediator` (martinothamar) | MIT | Source generator, pure mediator only |
| `Wolverine` | MIT | Full messaging framework with mediator |
| `LiteBus` | MIT | Lightweight CQS-focused |

**Decision:** Use **[Wolverine](https://github.com/JasperFx/wolverine)** (MIT license, free forever).

Why Wolverine:
- Future-proof: Start simple, grow without changing libraries
- Includes: Mediator + messaging + sagas + outbox + scheduling
- Source generator based (good performance)
- Convention-based handlers (less boilerplate)
- [Documentation](https://wolverinefx.net/)

```csharp
// Simple handler - no interface needed
public static class GetUserHandler
{
    public static Task<UserDto> Handle(GetUserQuery query) => ...
}
```

---

## 6. Validation

| Package | Purpose |
|---------|---------|
| `FluentValidation` | Fluent validation rules |
| `FluentValidation.DependencyInjectionExtensions` | DI integration |

[CQRS Validation Pipeline Guide](https://code-maze.com/cqrs-mediatr-fluentvalidation/)

---

## 7. Object Mapping

**Important: AutoMapper is now commercial in 2025.**

| Package | License | Notes |
|---------|---------|-------|
| `Mapperly` | MIT | Source generator, compile-time, actively maintained |
| `Mapster` | MIT | Fast runtime mapper, development stalled |
| `AutoMapper` | Commercial | Traditional choice, now requires license |

**Decision:** Use **[Mapperly](https://mapperly.riok.app/)** (MIT license, free forever).

Why Mapperly:
- Compile-time code generation (zero runtime overhead)
- Errors caught at build time, not runtime
- Actively maintained
- Fastest mapper in benchmarks
- [Documentation](https://mapperly.riok.app/docs/configuration/mapper/)

```csharp
[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User user);
}
```

---

## 8. Logging & Observability

| Package | Purpose |
|---------|---------|
| `Serilog.AspNetCore` | Structured logging |
| `Serilog.Sinks.Console` | Console output |
| `Serilog.Sinks.File` | File output |

**Decision:** Serilog only (simple, sufficient for base project).

- Can add OpenTelemetry later if distributed tracing needed
- OpenTelemetry is free (Apache 2.0) when ready to add

[Serilog Documentation](https://serilog.net/)

---

## 9. Caching

| Package | Purpose |
|---------|---------|
| Built-in `IMemoryCache` | In-memory caching |

**Decision:** IMemoryCache only (built-in, no external dependencies).

- Can add Redis later if distributed caching needed
- Redis package: `Microsoft.Extensions.Caching.StackExchangeRedis`

---

## 10. Background Jobs

| Package | Purpose |
|---------|---------|
| `Hangfire.AspNetCore` | Background job processing |
| `Hangfire.SqlServer` | SQL Server storage |

**Decision:** Hangfire with SQL Server storage (same connection string as app).

Why:
- Jobs persist across app restarts
- Production-ready
- Built-in dashboard
- Uses same database (creates `Hangfire.*` tables)

```csharp
builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(connectionString));
builder.Services.AddHangfireServer();
```

[Hangfire Documentation](https://www.hangfire.io/)

---

## 11. API Security & Rate Limiting

| Package | Purpose |
|---------|---------|
| Built-in Rate Limiting Middleware | Rate limiting (.NET 7+) |
| Built-in CORS | Cross-origin policies |

**Decision:** Use built-in middleware (no external packages needed).

[Rate Limiting in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)

---

## 12. API Documentation

| Package | Purpose |
|---------|---------|
| `Scalar.AspNetCore` | Modern API documentation UI |
| `Microsoft.AspNetCore.OpenApi` | OpenAPI spec generation |

**Decision:** Scalar (default in .NET 9/10, auto-generates from controllers).

```csharp
builder.Services.AddOpenApi();
app.MapOpenApi("/api/openapi/{documentName}.json");
app.MapScalarApiReference("/api/docs"); // Visit /api/docs
```

[Scalar Website](https://scalar.com/)

---

## 13. API Versioning

**Decision:** Skip library - use manual URL versioning if needed.

```csharp
// Manual versioning - no library required
[Route("api/v1/users")]
public class UsersV1Controller : ControllerBase { ... }

[Route("api/v2/users")]
public class UsersV2Controller : ControllerBase { ... }
```

Library (`Asp.Versioning.Mvc`) only needed for header/query versioning.

---

## 14. Health Checks

| Package | Purpose | Status |
|---------|---------|--------|
| `AspNetCore.HealthChecks.SqlServer` | SQL Server checks | ✅ Working |
| `AspNetCore.HealthChecks.UI` | Dashboard with history chart | ⚠️ EF Core 10 incompatible |
| `AspNetCore.HealthChecks.UI.InMemory.Storage` | Store history in memory | ⚠️ EF Core 10 incompatible |

**Decision:** Use basic health checks only (UI unavailable with EF Core 10).

```csharp
// What we use (works)
builder.Services.AddHealthChecks()
    .AddSqlServer(connectionString);

app.MapHealthChecks("/api/health");
```

**Note:** HealthChecks.UI throws `MissingMethodException: ArgumentIsEmpty` with EF Core 10.
Waiting for upstream fix: https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks

[AspNetCore.Diagnostics.HealthChecks GitHub](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks)

---

## 15. File Storage

| Package | Purpose |
|---------|---------|
| `FluentStorage` | Storage abstraction (local, Azure, AWS, GCP) |

**Decision:** FluentStorage (switch storage without code changes).

```csharp
// Dev - local disk
var storage = StorageFactory.Blobs.FromLocalDisk("C:/uploads");

// Prod - Azure (just config change)
var storage = StorageFactory.Blobs.FromAzureBlobStorage(conn);

// Same code for both!
await storage.WriteAsync("files/doc.pdf", stream);
```

Add provider packages as needed:
- `FluentStorage.Azure.Blobs`
- `FluentStorage.AWS.S3`

[FluentStorage GitHub](https://github.com/robinrodricks/FluentStorage)

---

## 16. Email

| Package | Purpose |
|---------|---------|
| `FluentEmail.Core` | Email abstraction |
| `FluentEmail.MailKit` | MailKit sender (recommended) |
| `FluentEmail.Razor` | Razor templates |

**Decision:** FluentEmail (fluent API, templates, easy provider switch).

```csharp
// Setup
builder.Services
    .AddFluentEmail("noreply@app.com")
    .AddRazorRenderer()
    .AddMailKitSender(new SmtpClientOptions { ... });

// Usage
await email
    .To(user.Email)
    .Subject("Welcome!")
    .UsingTemplateFromFile("emails/welcome.cshtml", new { Name = user.Name })
    .SendAsync();
```

[FluentEmail GitHub](https://github.com/lukencode/FluentEmail)

---

## 17. Multi-Tenancy

| Package | Purpose |
|---------|---------|
| `Finbuckle.MultiTenant.AspNetCore` | Multi-tenant support |
| `Finbuckle.MultiTenant.EntityFrameworkCore` | EF Core auto-filtering |

**Decision:** Finbuckle (auto tenant detection, auto query filtering).

```csharp
// Setup
builder.Services.AddMultiTenant<TenantInfo>()
    .WithHostStrategy()           // Detect from subdomain
    .WithEFCoreStore<TenantDbContext>();

app.UseMultiTenant();

// Entity - add IMultiTenant
public class Order : IMultiTenant
{
    public string TenantId { get; set; }  // Auto-set!
    public int Id { get; set; }
}

// Query - auto-filtered by tenant!
var orders = await db.Orders.ToListAsync();
```

[Finbuckle.MultiTenant](https://www.finbuckle.com/MultiTenant)

---

## 18. Testing

| Package | Purpose |
|---------|---------|
| `xunit` | Test framework |
| `xunit.runner.visualstudio` | VS Test runner |
| `Moq` | Mocking |
| `FluentAssertions` | Fluent assertions |
| `Microsoft.AspNetCore.Mvc.Testing` | Integration testing |
| `Bogus` | Fake data generation |

**Decision:** xUnit (modern, used by Microsoft, better test isolation).

```csharp
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockRepo = new();

    [Fact]
    public async Task GetUser_ReturnsUser_WhenExists()
    {
        // Arrange
        _mockRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new User { Id = 1, Name = "John" });

        var service = new UserService(_mockRepo.Object);

        // Act
        var result = await service.GetUserAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("John");
    }
}
```

[xUnit Best Practices](https://codejack.com/2025/01/best-practices-for-unit-testing-in-net/)

---

## Complete Package List (Copy-Ready)

### Main Project (`*.csproj`)

```xml
<!-- Framework -->
<TargetFramework>net10.0</TargetFramework>

<!-- API Documentation -->
<PackageReference Include="Microsoft.AspNetCore.OpenApi" />
<PackageReference Include="Scalar.AspNetCore" />

<!-- Database -->
<PackageReference Include="Microsoft.EntityFrameworkCore" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" />

<!-- Authentication -->
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" />

<!-- CQRS -->
<PackageReference Include="WolverineFx" />

<!-- Validation -->
<PackageReference Include="FluentValidation" />
<PackageReference Include="FluentValidation.DependencyInjectionExtensions" />

<!-- Object Mapping -->
<PackageReference Include="Riok.Mapperly" />

<!-- Logging -->
<PackageReference Include="Serilog.AspNetCore" />
<PackageReference Include="Serilog.Sinks.Console" />
<PackageReference Include="Serilog.Sinks.File" />

<!-- Background Jobs -->
<PackageReference Include="Hangfire.AspNetCore" />
<PackageReference Include="Hangfire.SqlServer" />

<!-- Health Checks -->
<PackageReference Include="AspNetCore.HealthChecks.SqlServer" />
<!-- Note: HealthChecks.UI packages incompatible with EF Core 10 -->
<!-- <PackageReference Include="AspNetCore.HealthChecks.UI" /> -->
<!-- <PackageReference Include="AspNetCore.HealthChecks.UI.InMemory.Storage" /> -->

<!-- File Storage -->
<PackageReference Include="FluentStorage" />

<!-- Email -->
<PackageReference Include="FluentEmail.Core" />
<PackageReference Include="FluentEmail.MailKit" />
<PackageReference Include="FluentEmail.Razor" />

<!-- Multi-Tenancy -->
<PackageReference Include="Finbuckle.MultiTenant.AspNetCore" />
<PackageReference Include="Finbuckle.MultiTenant.EntityFrameworkCore" />
```

### Test Project (`*.Tests.csproj`)

```xml
<PackageReference Include="xunit" />
<PackageReference Include="xunit.runner.visualstudio" />
<PackageReference Include="Moq" />
<PackageReference Include="FluentAssertions" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
<PackageReference Include="Bogus" />
```

---

## Key 2025 Licensing Changes

| Library | Change |
|---------|--------|
| **MediatR** | Now commercial for v13+ |
| **AutoMapper** | Now commercial |
| **MassTransit** | Now commercial |
| **Duende IdentityServer** | Commercial (free Community Edition available) |

All recommendations above prioritize **MIT-licensed alternatives** where licensing has changed.

---

## Sources

- [.NET 8/9 Clean Architecture Guide](https://medium.com/@kerimkkara/net-8-9-with-clean-architecture-ddd-cqrs-the-ultimate-2025-guide-2e9169c0296d)
- [Jason Taylor's Clean Architecture Template](https://github.com/jasontaylordev/CleanArchitecture)
- [Mediator Source Generator](https://github.com/martinothamar/Mediator)
- [Mapperly - AutoMapper Alternative](https://abp.io/community/articles/best-free-alternatives-to-automapper-in-.net-why-we-moved-to-mapperly-l9f5ii8s)
- [Serilog + OpenTelemetry](https://medium.com/@mahmednisar/logging-like-a-pro-serilog-opentelemetry-in-net-3c9f219b9296)
- [Hangfire vs Quartz](https://code-maze.com/chsarp-the-differences-between-quartz-net-and-hangfire/)
- [Scalar API Documentation](https://dev.to/arashzandi/net-9-revolutionizing-documentation-of-apis-from-swashbuckle-to-scalar-527)
- [API Versioning Best Practices](https://medium.com/@venkataramanaguptha/mastering-api-versioning-in-net-web-api-best-practices-for-2025-8226529551a2)
- [FluentStorage](https://github.com/robinrodricks/FluentStorage)
- [FluentEmail](https://github.com/lukencode/FluentEmail)
- [Finbuckle.MultiTenant](https://www.finbuckle.com/MultiTenant)
- [AspNetCore.Diagnostics.HealthChecks](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks)
