# Auto-Code Generation Pattern

Auto-incrementing, tenant-scoped codes for entities like employees (`EMP-20260301-000001`), projects (`PRJ-20260301-000001`), and task numbers (`PROJ-001`).

## Architecture

```
Application Layer                Infrastructure Layer           Database
+--------------------------+     +--------------------------+   +------------------+
| IEmployeeCodeGenerator   | --> | EmployeeCodeGenerator    |-->| SequenceCounters |
| IProjectCodeGenerator    | --> | ProjectCodeGenerator     |   | (MERGE+HOLDLOCK) |
| ITaskNumberGenerator     | --> | TaskNumberGenerator      |   +------------------+
+--------------------------+     +--------------------------+
```

## SequenceCounter Entity

Each row tracks the current counter for a unique `(TenantId, Prefix)` pair.

```csharp
// Domain/Entities/Common/SequenceCounter.cs
public class SequenceCounter
{
    public Guid Id { get; private set; }
    public string? TenantId { get; private set; }
    public string Prefix { get; private set; } = string.Empty;  // e.g. "EMP-20260301-"
    public int CurrentValue { get; private set; }

    public static SequenceCounter Create(string prefix, string? tenantId) => ...;
}
```

**EF Configuration** (`SequenceCounterConfiguration.cs`):
- Table: `SequenceCounters`
- Unique index on `(TenantId, Prefix)` -- prevents duplicate counters per tenant
- `Prefix` max length 50, `TenantId` max length 64

## Interface Pattern

Interfaces live in `Application/Common/Interfaces/`. All follow the same shape:

```csharp
public interface IEmployeeCodeGenerator
{
    Task<string> GenerateNextAsync(string? tenantId, CancellationToken ct = default);
}
```

The `ITaskNumberGenerator` variant takes an additional `projectPrefix` parameter since task numbers are scoped per project, not globally:

```csharp
public interface ITaskNumberGenerator
{
    Task<string> GenerateNextAsync(string projectPrefix, string? tenantId, CancellationToken ct = default);
}
```

## Implementation -- Atomic SQL Increment

Implementations live in `Infrastructure/Services/` and use `IScopedService` for auto-registration.

**Key design decisions:**
- **Direct `ApplicationDbContext` injection** -- `Database.SqlQueryRaw<int>()` is only available on `DbContext`, not through `IUnitOfWork`/`IRepository`. This is an intentional exception to the standard pattern.
- **SQL `MERGE` with `HOLDLOCK`** -- atomic upsert + increment in a single statement. Creates the counter row on first use (no seeding needed).
- **`ToListAsync()` over `FirstAsync()`** -- EF Core wraps `SqlQueryRaw` in a subquery when using `FirstAsync()`, breaking `MERGE OUTPUT` syntax.

```csharp
public class EmployeeCodeGenerator : IEmployeeCodeGenerator, IScopedService
{
    private readonly ApplicationDbContext _dbContext;

    public async Task<string> GenerateNextAsync(string? tenantId, CancellationToken ct = default)
    {
        var prefix = $"EMP-{DateTime.UtcNow:yyyyMMdd}-";
        var nextValue = await AtomicIncrementAsync(prefix, tenantId, ct);
        return $"{prefix}{nextValue:D6}";  // EMP-20260301-000001
    }

    private async Task<int> AtomicIncrementAsync(string prefix, string? tenantId, CancellationToken ct)
    {
        // Two SQL variants: one for tenant-scoped, one for system (TenantId IS NULL)
        var sql = tenantId != null
            ? @"MERGE INTO SequenceCounters WITH (HOLDLOCK) AS target
                USING (SELECT {0} AS Prefix, {1} AS TenantId) AS source
                ON target.Prefix = source.Prefix AND target.TenantId = source.TenantId
                WHEN MATCHED THEN UPDATE SET CurrentValue = target.CurrentValue + 1
                WHEN NOT MATCHED THEN INSERT (Id, TenantId, Prefix, CurrentValue)
                    VALUES (NEWID(), {1}, {0}, 1)
                OUTPUT INSERTED.CurrentValue;"
            : /* same structure but with TenantId IS NULL comparison */;

        var results = await _dbContext.Database
            .SqlQueryRaw<int>(sql, prefix, tenantId)
            .ToListAsync(ct);
        return results.First();
    }
}
```

## Code Format Summary

| Generator | Prefix Pattern | Padding | Example |
|-----------|---------------|---------|---------|
| `EmployeeCodeGenerator` | `EMP-{yyyyMMdd}-` | `D6` (6 digits) | `EMP-20260301-000001` |
| `ProjectCodeGenerator` | `PRJ-{yyyyMMdd}-` | `D6` (6 digits) | `PRJ-20260301-000001` |
| `TaskNumberGenerator` | `{projectSlug}-` | `D3` (3 digits) | `MYPROJ-001` |

Date-based prefixes reset the counter daily. Project-scoped task numbers increment continuously per project.

## Usage in Command Handlers

Inject the generator and call before entity creation:

```csharp
public class CreateEmployeeCommandHandler(
    IRepository<Employee, Guid> employeeRepository,
    IEmployeeCodeGenerator codeGenerator,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser)
{
    public async Task<Result<EmployeeDto>> Handle(CreateEmployeeCommand command, CancellationToken ct)
    {
        var tenantId = currentUser.TenantId;
        var employeeCode = await codeGenerator.GenerateNextAsync(tenantId, ct);

        var employee = Employee.Create(employeeCode, command.FirstName, ...);
        await employeeRepository.AddAsync(employee, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }
}
```

## Test-Safe Implementations

EF Core `SqlQueryRaw` composability can break in certain test contexts. Test-safe variants use `ToListAsync()` (not `FirstAsync()`) and are registered in `CustomWebApplicationFactory`:

```csharp
// tests/NOIR.IntegrationTests/Infrastructure/CustomWebApplicationFactory.cs
ReplaceService<IEmployeeCodeGenerator, TestSafeEmployeeCodeGenerator>(services);
ReplaceService<IProjectCodeGenerator, TestSafeProjectCodeGenerator>(services);
ReplaceService<ITaskNumberGenerator, TestSafeTaskNumberGenerator>(services);
```

The test-safe implementations are identical to production except they use `ToListAsync()` + `.First()` instead of `FirstAsync()` to avoid EF Core non-composable SQL wrapping.

**Unit tests** mock the interface directly:

```csharp
private readonly Mock<IEmployeeCodeGenerator> _codeGeneratorMock = new();

_codeGeneratorMock
    .Setup(x => x.GenerateNextAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync("EMP-20260301-000001");
```

## Adding a New Code Generator

1. **Define the interface** in `Application/Common/Interfaces/`:
   ```csharp
   public interface IInvoiceNumberGenerator
   {
       Task<string> GenerateNextAsync(string? tenantId, CancellationToken ct = default);
   }
   ```

2. **Create the implementation** in `Infrastructure/Services/`:
   ```csharp
   public class InvoiceNumberGenerator : IInvoiceNumberGenerator, IScopedService
   {
       private readonly ApplicationDbContext _dbContext;
       // Copy AtomicIncrementAsync from an existing generator
       // Customize: prefix format ("INV-{yyyyMMdd}-"), padding (D6), etc.
   }
   ```

3. **Add test-safe variant** in `CustomWebApplicationFactory.cs`:
   ```csharp
   internal sealed class TestSafeInvoiceNumberGenerator : IInvoiceNumberGenerator { ... }
   // Register:
   ReplaceService<IInvoiceNumberGenerator, TestSafeInvoiceNumberGenerator>(services);
   ```

4. **Inject in command handler** and call `GenerateNextAsync` before entity creation.

5. **Mock in unit tests** using `Mock<IInvoiceNumberGenerator>`.

## Key Pitfalls

| Pitfall | Solution |
|---------|----------|
| `FirstAsync()` on `SqlQueryRaw` throws composability error | Use `ToListAsync()` + `.First()` |
| Missing `HOLDLOCK` causes race conditions under concurrency | Always include `WITH (HOLDLOCK)` in `MERGE` |
| Null `TenantId` needs separate SQL branch | Use `TenantId IS NULL` comparison (not `= NULL`) |
| Forgetting test-safe registration | Integration tests fail with SQL errors -- add `ReplaceService` in `CustomWebApplicationFactory` |
