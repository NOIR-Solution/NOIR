# Before-State Resolver Pattern for Handler Diff Tracking

## Overview

The **Before-State Resolver** pattern enables automatic before/after diff tracking in the Activity Timeline's **Handler tab**. Without a resolver, the Handler tab shows "No handler diff available" - only Input Parameters are displayed.

## Why This Matters

The Activity Timeline has two types of change tracking:

| Tab | What It Shows | How It Works |
|-----|---------------|--------------|
| **Handler** | DTO-level diff (before → after) | Requires before-state resolver registration |
| **Database** | Entity-level diff (EF Core changes) | Automatic via EntityAuditLogInterceptor |

**Handler diff** is critical because it shows meaningful business-level changes (e.g., "Full Name: John → Jane") rather than raw database column changes.

## How It Works

1. **Before handler execution**: Middleware fetches current DTO state using registered resolver
2. **Handler executes**: Makes changes to entities
3. **After handler execution**: Middleware fetches updated DTO state
4. **Diff computed**: Before/after DTOs are compared and diff is stored

## Implementation

### Step 1: Command implements `IAuditableCommand<TDto>`

```csharp
// The TDto type parameter tells the middleware which DTO type to track
public sealed record UpdatePostCommand(
    Guid Id,
    string Title,
    // ... other properties
) : IAuditableCommand<PostDto>  // ← TDto is PostDto
{
    public AuditOperationType OperationType => AuditOperationType.Update;
    public object? GetTargetId() => Id;  // ← Used to fetch before/after state
    // ...
}
```

### Step 2: Register Before-State Resolver in DependencyInjection.cs

```csharp
// In Infrastructure/DependencyInjection.cs, inside AddInfrastructureServices()

// Register before-state resolvers for auditable DTOs
services.AddBeforeStateResolver<PostDto, GetPostQuery>(
    targetId => new GetPostQuery(Id: Guid.Parse(targetId.ToString()!)));

services.AddBeforeStateResolver<UserProfileDto, GetUserByIdQuery>(
    targetId => new GetUserByIdQuery(targetId.ToString()!));

services.AddBeforeStateResolver<TenantDto, GetTenantByIdQuery>(
    targetId => new GetTenantByIdQuery(Guid.Parse(targetId.ToString()!)));
```

### Step 3: Ensure Query Returns the Same DTO Type

The query must return `Result<TDto>` where TDto matches the command's generic parameter:

```csharp
// GetPostQuery returns Result<PostDto>
public async Task<Result<PostDto>> Handle(GetPostQuery query, CancellationToken ct)
{
    // ... fetch and return PostDto
}
```

## Checklist for New Auditable Commands

When creating a new auditable command with Update operation:

- [ ] Command implements `IAuditableCommand<TDto>` with correct DTO type
- [ ] Command has `GetTargetId()` returning the entity ID
- [ ] **CRITICAL**: Register before-state resolver in `DependencyInjection.cs`
- [ ] Query exists that fetches single entity by ID and returns `Result<TDto>`

## Common Mistakes

### 1. Missing Resolver Registration

**Symptom**: Handler tab shows "No handler diff available"

**Fix**: Add resolver in `DependencyInjection.cs`:
```csharp
services.AddBeforeStateResolver<YourDto, GetYourEntityByIdQuery>(
    targetId => new GetYourEntityByIdQuery(Guid.Parse(targetId.ToString()!)));
```

### 2. Wrong DTO Type

**Symptom**: Resolver registered but diff still missing

**Fix**: Ensure command's `IAuditableCommand<TDto>` matches the resolver's TDto:
```csharp
// Command
public sealed record UpdateFooCommand(...) : IAuditableCommand<FooDto>

// Resolver - must use same FooDto
services.AddBeforeStateResolver<FooDto, GetFooByIdQuery>(...)
```

### 3. Query Returns Different Type

**Symptom**: Exception in logs about type mismatch

**Fix**: Ensure query handler returns `Result<TDto>` matching the resolver registration.

## Currently Registered Resolvers

| DTO Type | Query | Location |
|----------|-------|----------|
| `UserProfileDto` | `GetUserByIdQuery` | DependencyInjection.cs:253 |
| `TenantDto` | `GetTenantByIdQuery` | DependencyInjection.cs:256 |
| `PostDto` | `GetPostQuery` | DependencyInjection.cs:259 |

## Adding New Resolvers

Add to `src/NOIR.Infrastructure/DependencyInjection.cs` in the "Register before-state resolvers" section:

```csharp
// Add imports to GlobalUsings.cs if needed
global using NOIR.Application.Features.YourFeature.DTOs;
global using NOIR.Application.Features.YourFeature.Queries.GetYourEntity;

// Add resolver registration
services.AddBeforeStateResolver<YourDto, GetYourEntityQuery>(
    targetId => new GetYourEntityQuery(Guid.Parse(targetId.ToString()!)));
```

## Related Files

- `src/NOIR.Infrastructure/Audit/HandlerAuditMiddleware.cs` - Middleware that uses resolvers
- `src/NOIR.Infrastructure/Audit/WolverineBeforeStateProvider.cs` - Resolver registry
- `src/NOIR.Infrastructure/DependencyInjection.cs` - Where resolvers are registered
- `src/NOIR.Infrastructure/GlobalUsings.cs` - Add namespace imports here
