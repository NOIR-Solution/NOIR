# NOIR Infrastructure Services

## Location
`src/NOIR.Infrastructure/`

## Key Directories
- `Persistence/` - EF Core context, repositories, configurations
- `Identity/` - Authentication, authorization, token services
- `Audit/` - Audit logging handlers and services
- `Storage/` - File storage (FluentStorage)
- `Migrations/` - EF Core migrations (auto-generated, don't modify)

## Middleware

### CurrentUserLoaderMiddleware
**Location:** `src/NOIR.Web/Middleware/CurrentUserLoaderMiddleware.cs`

Centralizes user profile loading in middleware pipeline (commit 8c411e6).

**Purpose:**
- Single DB query per request (not multiple)
- Loads complete user profile (roles, display name, avatar, tenant info)
- Caches in `HttpContext.Items` for request lifetime
- JWT claims alone don't contain full user profile

**Pipeline Position:**
```
1. Exception Handling
2. Authentication (JWT/Cookie)
3. Multi-Tenant Resolution (Finbuckle)
4. CurrentUserLoaderMiddleware  ← Runs here (needs tenant context)
5. Authorization
6. Request Logging
```

**Why After Multi-Tenant?** Needs tenant context to query correct database partition.

## Identity Services

### TokenService (IScopedService)
JWT token generation and validation:
- Access token creation
- Refresh token management
- Token validation

### RefreshTokenService (IScopedService)
Refresh token lifecycle:
- Token generation and storage
- Family-based rotation
- Revocation

### CookieAuthService (IScopedService)
Cookie-based authentication for browser clients.

### PermissionCacheInvalidator (IScopedService)
Invalidates permission cache on changes.

## Persistence

### AppDbContext
Main EF Core context with:
- Entity configurations (auto-discovered)
- Global query filters (soft delete)
- Audit interceptors

### Repository Implementation
Generic repository in `Persistence/`:
```csharp
public class Repository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : AggregateRoot<TId>
{
    // Full implementation with bulk operations
}
```

## Audit System

### 3-Level Audit Logging
1. **HTTP Level** - Request/response logging
2. **Handler Level** - Command/query execution
3. **Entity Level** - EF Core changes (before/after)

### Interceptors
- `AuditableEntityInterceptor` - Sets audit fields
- Entity change tracking for diff logging

## DI Registration
`DependencyInjection.cs`:
```csharp
services.Scan(scan => scan
    .FromAssemblyOf<AppDbContext>()
    .AddClasses(c => c.AssignableTo<IScopedService>())
    .AsImplementedInterfaces()
    .WithScopedLifetime());
```

## External Services
- FluentStorage for file storage
- FluentEmail for email sending
- Hangfire for background jobs
