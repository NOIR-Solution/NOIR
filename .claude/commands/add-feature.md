# Add Feature Command

Create a new feature following NOIR's Clean Architecture patterns.

## Instructions

When adding feature "$ARGUMENTS":

1. **Check existing patterns** in similar features under `src/NOIR.Application/Features/`

2. **Create Command/Query** in `src/NOIR.Application/Features/{Feature}/`
   - Command: `{Action}{Entity}Command.cs` with response record
   - Validator: `{Action}{Entity}CommandValidator.cs` using FluentValidation

3. **Create Handler** in `src/NOIR.Infrastructure/`
   - Use Wolverine pattern (static class, no interface)
   - Inject dependencies via method parameters
   - Always include `CancellationToken`

4. **Create Specification** if database query needed
   - Location: `src/NOIR.Application/Specifications/`
   - Always use `TagWith("MethodName")` for debugging

5. **Create Endpoint** in `src/NOIR.Web/Endpoints/`
   - Follow minimal API pattern
   - Use `/api/{feature}` prefix

6. **Build and verify**: `dotnet build src/NOIR.sln`

## Example Structure

```
src/NOIR.Application/Features/Orders/
├── Commands/
│   ├── CreateOrder/
│   │   ├── CreateOrderCommand.cs
│   │   └── CreateOrderCommandValidator.cs
│   └── CancelOrder/
│       └── ...
└── Queries/
    └── GetOrderById/
        └── GetOrderByIdQuery.cs

src/NOIR.Infrastructure/Orders/
└── Handlers/
    ├── CreateOrderHandler.cs
    └── GetOrderByIdHandler.cs

src/NOIR.Web/Endpoints/
└── OrderEndpoints.cs
```
