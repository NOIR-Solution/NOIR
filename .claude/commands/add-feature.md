# Add Feature Command

Create a new feature following NOIR's Clean Architecture patterns.

## Instructions

When adding feature "$ARGUMENTS":

1. **Check existing patterns** in similar features under `src/NOIR.Application/Features/`

2. **Create Command/Query with co-located Handler** in `src/NOIR.Application/Features/{Feature}/`
   - Command: `{Action}{Entity}Command.cs`
   - Handler: `{Action}{Entity}CommandHandler.cs` (same folder!)
   - Validator: `{Action}{Entity}CommandValidator.cs`

3. **Handler Pattern** (constructor injection, not static):
   ```csharp
   public class CreateOrderCommandHandler
   {
       private readonly IRepository<Order, Guid> _repository;
       private readonly IUnitOfWork _unitOfWork;

       public CreateOrderCommandHandler(
           IRepository<Order, Guid> repository,
           IUnitOfWork unitOfWork) { ... }

       public async Task<Result<OrderDto>> Handle(
           CreateOrderCommand cmd,
           CancellationToken ct)
       {
           var order = Order.Create(...);
           await _repository.AddAsync(order, ct);
           await _unitOfWork.SaveChangesAsync(ct);  // REQUIRED!
           return Result.Success(order.ToDto());
       }
   }
   ```

4. **Create Specification** if database query needed
   - Location: `src/NOIR.Application/Specifications/{Entity}/`
   - Always use `TagWith("MethodName")` for debugging
   - Use `.AsTracking()` if entity will be modified

5. **Create Endpoint** in `src/NOIR.Web/Endpoints/`
   - Follow minimal API pattern
   - Use `/api/{feature}` prefix

6. **Build and verify**: `dotnet build src/NOIR.sln`

## Example Structure

```
src/NOIR.Application/Features/Orders/
├── Commands/
│   └── Create/
│       ├── CreateOrderCommand.cs
│       ├── CreateOrderCommandHandler.cs    # Co-located!
│       └── CreateOrderCommandValidator.cs
└── Queries/
    └── GetById/
        ├── GetOrderByIdQuery.cs
        └── GetOrderByIdQueryHandler.cs     # Co-located!

src/NOIR.Web/Endpoints/
└── OrderEndpoints.cs
```

## Critical Reminders

- **IUnitOfWork**: Repository methods do NOT auto-save. Always call `SaveChangesAsync()`
- **AsTracking**: Specs default to `AsNoTracking`. Add `.AsTracking()` for mutation queries
- **TagWith**: All specs must have `TagWith("MethodName")` for SQL debugging
