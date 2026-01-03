# NOIR Application Features

## Location
`src/NOIR.Application/Features/`

## Feature Modules
- `Auth/` - Authentication (login, register, refresh, logout)
- `Users/` - User management
- `Roles/` - Role management
- `Permissions/` - Permission management
- `Audit/` - Audit log queries

## Feature Structure
Each feature contains:
```
Features/
└── Auth/
    ├── Commands/
    │   ├── LoginCommand.cs
    │   ├── RegisterCommand.cs
    │   └── RefreshTokenCommand.cs
    ├── Queries/
    │   └── GetCurrentUserQuery.cs
    └── Dtos/
        ├── AuthResponse.cs
        └── CurrentUserDto.cs
```

## Command/Query Pattern

### Commands (mutations)
```csharp
public record CreateUserCommand(
    string Email,
    string Password,
    string? FirstName,
    string? LastName);
```

### Queries (reads)
```csharp
public record GetUserByIdQuery(Guid UserId);
```

### Handlers
Located in `src/NOIR.Infrastructure/` (close to implementation):
```csharp
public static class LoginHandler
{
    public static async Task<Result<AuthResponse>> Handle(
        LoginCommand command,
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        CancellationToken ct)
    {
        // Implementation
    }
}
```

## Specifications
`src/NOIR.Application/Specifications/`

Example:
```csharp
public class UserByEmailSpec : Specification<ApplicationUser>, ISingleResultSpecification<ApplicationUser>
{
    public UserByEmailSpec(string email)
    {
        Query.Where(u => u.Email == email)
             .TagWith("GetUserByEmail");
    }
}
```

## Behaviors (Pipeline)
`src/NOIR.Application/Behaviors/`

- Validation behavior (FluentValidation)
- Logging behavior
- Performance behavior

## Result Pattern
Use `Result<T>` for consistent error handling:
```csharp
public static async Task<Result<AuthResponse>> Handle(...)
{
    if (user == null)
        return Result<AuthResponse>.Fail("User not found");
    
    return Result<AuthResponse>.Success(new AuthResponse(...));
}
```
