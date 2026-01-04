# ADR-003: Vertical Slice Architecture for CQRS

## Status

Accepted

## Date

2026-01-04

## Context

The NOIR backend initially followed a traditional layered architecture where:
- Commands and Validators lived in `Application/Features/{Feature}/Commands/{Action}/`
- Handlers lived in `Infrastructure/Identity/Handlers/` or `Infrastructure/Audit/Handlers/`

This separation caused several issues:
1. **Scattered concerns** - To understand or modify a feature, developers had to navigate multiple projects
2. **Increased coupling** - Handlers in Infrastructure directly depended on ASP.NET Identity types, making Application layer handlers impractical
3. **Testing friction** - Unit tests required mocking Infrastructure dependencies
4. **Maintenance overhead** - Adding a new command required changes across multiple directories

## Decision

Adopt **Vertical Slice Architecture** where each feature slice contains all its components co-located:

```
src/NOIR.Application/Features/{Feature}/
├── Commands/{Action}/
│   ├── {Action}Command.cs
│   ├── {Action}CommandHandler.cs
│   └── {Action}CommandValidator.cs
└── Queries/{Action}/
    ├── {Action}Query.cs
    └── {Action}QueryHandler.cs
```

To enable this, we introduced service abstraction interfaces:
- `IUserIdentityService` - Wraps ASP.NET Identity `UserManager`/`SignInManager`
- `IRoleIdentityService` - Wraps ASP.NET Identity `RoleManager`

These interfaces live in `Application/Common/Interfaces/` with implementations in `Infrastructure/Identity/`.

## Consequences

### Positive

1. **Cohesion** - All components for a feature live together, making the codebase easier to navigate
2. **Encapsulation** - Each slice is self-contained, reducing unintended dependencies
3. **Testability** - Handlers only depend on interfaces, enabling easy mocking
4. **Onboarding** - New developers can focus on one folder to understand a complete feature
5. **Deletion** - Removing a feature means deleting one folder (plus updating DI if needed)

### Negative

1. **Initial refactoring effort** - Required moving 26 handlers and creating abstraction interfaces
2. **Abstraction overhead** - `IUserIdentityService` and `IRoleIdentityService` add a layer of indirection
3. **EF Core considerations** - Service methods must handle EF Core translation carefully (project AFTER ordering)

### Neutral

1. **Wolverine still discovers handlers** - No changes to message bus configuration needed
2. **Architecture tests updated** - Verify handlers live in Application layer

## Implementation Notes

### Handler Pattern

Handlers use class-based DI with constructor injection:

```csharp
public class CreateOrderCommandHandler
{
    private readonly IRepository<Order, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderCommandHandler(
        IRepository<Order, Guid> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OrderDto>> Handle(
        CreateOrderCommand cmd,
        CancellationToken ct)
    {
        // Implementation
    }
}
```

### Identity Service Pattern

For ASP.NET Identity operations, use the abstraction interfaces:

```csharp
public class LoginCommandHandler
{
    private readonly IUserIdentityService _userIdentityService;

    public async Task<Result<AuthResponse>> Handle(
        LoginCommand cmd,
        CancellationToken ct)
    {
        var user = await _userIdentityService.FindByEmailAsync(cmd.Email, ct);
        if (user is null)
            return Result.Failure<AuthResponse>(Error.NotFound(...));

        var signInResult = await _userIdentityService.CheckPasswordSignInAsync(
            user.Id, cmd.Password, lockoutOnFailure: true, ct);

        // ...
    }
}
```

## References

- [Vertical Slice Architecture](https://www.jimmybogard.com/vertical-slice-architecture/)
- [CQRS Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/cqrs)
- [Wolverine Documentation](https://wolverine.netlify.app/)
