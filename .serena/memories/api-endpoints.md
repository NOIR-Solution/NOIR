# NOIR API Endpoints

## Endpoint Structure
Located in `src/NOIR.Web/Endpoints/`

### Current Endpoints
- `AuthEndpoints.cs` - Authentication (login, register, refresh, logout)
- `UserEndpoints.cs` - User management
- `RoleEndpoints.cs` - Role management
- `AuditEndpoints.cs` - Audit log queries
- `EmailTemplateEndpoints.cs` - Email template CRUD and testing
- `NotificationEndpoints.cs` - User notifications and preferences
- `TenantEndpoints.cs` - Multi-tenant management (admin)
- `FileEndpoints.cs` - File upload/download operations
- `ValidationEndpoints.cs` - Form validation schemas

## Endpoint Pattern

### Minimal API with Wolverine
```csharp
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .CacheOutput("NoCache");

        group.MapPost("/login", async (
            LoginCommand command,
            IMessageBus bus,
            bool useCookies = false) =>
        {
            var result = await bus.InvokeAsync<Result<AuthResponse>>(command);
            return result.ToHttpResult();
        })
        .RequireRateLimiting("auth")
        .WithName("Login")
        .Produces<AuthResponse>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);
    }
}
```

### Key Patterns
1. Use `IMessageBus` for CQRS dispatch
2. Return `Result<T>` from handlers
3. Use `.ToHttpResult()` extension for consistent error responses
4. Apply rate limiting with named policies
5. Use `RequireAuthorization()` for protected endpoints

## Rate Limiting Policies
- `auth` - Sliding window, 5 req/min (login, register, refresh)
- `fixed` - Standard rate limit (authenticated endpoints)

## Authentication
- JWT Bearer tokens (header: `Authorization: Bearer <token>`)
- Cookie-based auth (use `?useCookies=true` query param)
- Refresh token rotation with family tracking

## API Documentation
- Scalar UI: `/api/docs`
- OpenAPI spec: `/api/openapi/v1.json`

## Development URLs
- Application: `http://localhost:3000` (frontend + API via proxy)
- API Docs: `http://localhost:3000/api/docs`

> Port 4000 serves backend directly for production-like testing.
