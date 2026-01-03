# NOIR Authentication & Authorization

## Authentication Methods

### JWT Bearer Tokens
```
Authorization: Bearer <access_token>
```

### Cookie-Based Auth
For browser clients, use `?useCookies=true`:
```
POST /api/auth/login?useCookies=true
```

## Token Lifecycle

### Access Token
- Short-lived (configurable, typically 15-60 mins)
- Contains user claims
- Validated on each request

### Refresh Token
- Long-lived (configurable, typically 7-30 days)
- Stored in database (hashed)
- Family-based rotation for security
- Device/IP tracking

### Token Refresh Flow
```
POST /api/auth/refresh
{
  "refreshToken": "<token>"
}
```

Or with cookies:
```
POST /api/auth/refresh?useCookies=true
```

## Authorization

### Role-Based (Identity)
Using ASP.NET Core Identity roles:
```csharp
[Authorize(Roles = "Admin")]
```

### Permission-Based (Database)
Flexible `resource:action:scope` format:
```
orders:read:own
orders:read:all
users:delete:all
```

### Permission Entity
```csharp
public class Permission : Entity<Guid>
{
    public string Resource { get; private set; }  // "orders"
    public string Action { get; private set; }     // "read"
    public string? Scope { get; private set; }     // "own", "all"
    public string Name => $"{Resource}:{Action}:{Scope}";
}
```

### Checking Permissions
```csharp
[Authorize(Policy = "orders:read")]
```

Or in code:
```csharp
if (await authService.HasPermissionAsync("orders:delete:all"))
{
    // ...
}
```

## API Endpoints

| Endpoint | Purpose |
|----------|---------|
| `POST /api/auth/register` | Register new user |
| `POST /api/auth/login` | Login |
| `POST /api/auth/logout` | Logout (clear cookies/revoke token) |
| `POST /api/auth/refresh` | Refresh access token |
| `GET /api/auth/me` | Get current user |
| `PUT /api/auth/me` | Update profile |

## Rate Limiting
Auth endpoints use stricter `auth` rate limit (5 req/min) to prevent brute force.

## Security Features
- Password hashing (Identity default)
- Token family tracking (detect token reuse)
- Refresh token rotation
- Device/IP logging
- Rate limiting on auth endpoints
