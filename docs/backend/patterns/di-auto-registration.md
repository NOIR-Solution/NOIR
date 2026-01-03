# DI Auto-Registration Pattern

**Created:** 2025-12-31
**Based on:** Scrutor library research

---

## Overview

NOIR uses **Scrutor** for automatic service registration via marker interfaces. This eliminates forgotten service registrations and reduces boilerplate.

---

## Marker Interfaces

Located in `src/NOIR.Application/Common/Interfaces/ServiceLifetimes.cs`:

| Marker Interface | Lifetime | Use Case |
|-----------------|----------|----------|
| `IScopedService` | Scoped | Repositories, Services with DB access |
| `ITransientService` | Transient | Stateless services, domain services |
| `ISingletonService` | Singleton | Caches, configuration wrappers |

---

## How to Register a Service

### 1. Create your service with interface

```csharp
public interface ICustomerService
{
    Task<CustomerDto> GetByIdAsync(Guid id, CancellationToken ct);
}

public class CustomerService : ICustomerService, IScopedService
{
    // Implementation
}
```

### 2. Done - No manual registration needed!

Scrutor automatically discovers and registers services marked with marker interfaces.

---

## Auto-Registration Setup

In `src/NOIR.Infrastructure/DependencyInjection.cs`:

```csharp
services.Scan(scan => scan
    .FromAssemblyOf<ApplicationDbContext>()

    // Register IScopedService implementations
    .AddClasses(c => c.AssignableTo<IScopedService>(), publicOnly: false)
    .AsImplementedInterfaces()
    .WithScopedLifetime()

    // Register ITransientService implementations
    .AddClasses(c => c.AssignableTo<ITransientService>(), publicOnly: false)
    .AsImplementedInterfaces()
    .WithTransientLifetime()

    // Register ISingletonService implementations
    .AddClasses(c => c.AssignableTo<ISingletonService>(), publicOnly: false)
    .AsImplementedInterfaces()
    .WithSingletonLifetime()
);
```

---

## Special Cases (No Marker Needed)

| Component | Auto-Discovery |
|-----------|---------------|
| Wolverine Handlers | By naming convention (`*Handler`) |
| FluentValidation | `AddValidatorsFromAssembly()` |
| EF Core DbContext | Manual (requires connection string) |
| Options/Settings | Manual (requires configuration binding) |

---

## Troubleshooting

**Service not resolving?**

```csharp
var service = provider.GetService<IMyService>();
if (service == null)
{
    // Check:
    // 1. Marker interface added to implementation?
    // 2. Class is in scanned assembly (Application or Infrastructure)?
    // 3. Class is public or `publicOnly: false` in scan?
}
```

---

## References

- [Scrutor GitHub](https://github.com/khellang/Scrutor)
- [Scrutor in .NET](https://codewithmukesh.com/blog/scrutor-dotnet-auto-register-dependencies/)
