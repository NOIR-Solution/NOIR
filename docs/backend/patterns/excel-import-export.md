# Excel Import/Export Pattern

## Overview

NOIR uses **ClosedXML** for Excel (.xlsx) generation and a built-in CSV parser for imports. All export handlers support dual-format output (CSV or Excel) via the `ExportFormat` enum. Import handlers use row-level validation and return structured error results.

**Key types:**

| Type | Location | Purpose |
|------|----------|---------|
| `IExcelExportService` | `Application/Common/Interfaces/` | Excel file creation abstraction |
| `ExcelExportService` | `Infrastructure/Services/` | ClosedXML implementation (IScopedService) |
| `ExportFormat` | `Features/Reports/DTOs/ReportDtos.cs` | Enum: `CSV`, `Excel` |
| `ExportResultDto` | `Features/Reports/DTOs/ReportDtos.cs` | `FileBytes`, `ContentType`, `FileName` |
| `ImportResultDto` | `Features/Hr/DTOs/HrDtos.cs` | `TotalRows`, `SuccessCount`, `FailedCount`, `Errors` |
| `ImportErrorDto` | `Features/Hr/DTOs/HrDtos.cs` | `RowNumber`, `Message` |

## IExcelExportService Interface

The service accepts a sheet name, column headers, and a list of rows (each row is a list of nullable objects). The ClosedXML implementation handles type-aware cell formatting (string, decimal, int, long, double, DateTimeOffset, DateTime, bool), freezes the header row, applies auto-filter, and auto-fits columns (max width 50).

```csharp
public interface IExcelExportService
{
    byte[] CreateExcelFile(
        string sheetName,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<object?>> rows);
}
```

Registration: `ExcelExportService : IExcelExportService, IScopedService` (auto-registered via Scrutor).

## Export Pattern

Every export handler follows this flow:

```
Specification -> Repository.ListAsync -> Build headers + rows -> Format switch -> ExportResultDto
```

### Query record

```csharp
public sealed record ExportCustomersQuery(
    ExportFormat Format,
    CustomerSegment? Segment,
    string? Search) : IQuery;
```

### Handler

```csharp
public async Task<Result<ExportResultDto>> Handle(
    ExportCustomersQuery query, CancellationToken ct)
{
    // 1. Load filtered data via Specification
    var spec = new CustomersForExportSpec(query.Search, query.Segment);
    var customers = await _customerRepository.ListAsync(spec, ct);

    // 2. Define headers
    var headers = new List<string> { "FirstName", "LastName", "Email" };

    // 3. Build rows as List<IReadOnlyList<object?>>
    var rows = new List<IReadOnlyList<object?>>();
    foreach (var c in customers)
        rows.Add(new List<object?> { c.FirstName, c.LastName, c.Email });

    // 4. Format switch
    var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
    if (query.Format == ExportFormat.Excel)
    {
        var fileBytes = _excelExportService.CreateExcelFile("Customers", headers, rows);
        return Result.Success(new ExportResultDto(fileBytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"customers-{timestamp}.xlsx"));
    }

    // CSV with BOM for Excel compatibility
    var sb = new StringBuilder();
    sb.AppendLine(string.Join(",", headers.Select(h => $"\"{h}\"")));
    foreach (var row in rows)
        sb.AppendLine(string.Join(",", row.Select(v =>
            v is null ? "" : $"\"{EscapeCsv(v.ToString())}\"")));
    var csvBytes = Encoding.UTF8.GetPreamble()
        .Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    return Result.Success(new ExportResultDto(csvBytes, "text/csv",
        $"customers-{timestamp}.csv"));
}
```

### Endpoint

```csharp
group.MapGet("/export", async (
    [FromQuery] ExportFormat? format,
    IMessageBus bus, CancellationToken ct) =>
{
    var query = new ExportCustomersQuery(format ?? ExportFormat.CSV, ...);
    var result = await bus.InvokeAsync<Result<ExportResultDto>>(query, ct);
    if (result.IsFailure) return result.ToHttpResult();
    return Results.File(
        result.Value.FileBytes, result.Value.ContentType, result.Value.FileName);
})
.RequireAuthorization(Permissions.CustomersRead)
.Produces<byte[]>(StatusCodes.Status200OK);
```

Default format is `CSV` when the query parameter is omitted.

## Tag/Relation Aggregation in Exports

When exporting entities with many-to-many relationships (e.g., employee tags), load related data separately via `IApplicationDbContext` and join in-memory. Specifications do not support `ThenInclude`.

```csharp
var employeeIds = employees.Select(e => e.Id).ToList();
var tagAssignments = await _dbContext.EmployeeTagAssignments
    .Where(a => employeeIds.Contains(a.EmployeeId) && a.EmployeeTag != null)
    .Select(a => new { a.EmployeeId, a.EmployeeTag!.Name })
    .TagWith("ExportEmployees_TagNames")
    .ToListAsync(ct);

var tagsByEmployee = tagAssignments
    .GroupBy(a => a.EmployeeId)
    .ToDictionary(g => g.Key,
        g => string.Join(", ", g.Select(a => a.Name).OrderBy(n => n)));

// In row loop:
tagsByEmployee.TryGetValue(e.Id, out var tags);
rows.Add(new List<object?> { ..., tags ?? "" });
```

This avoids cartesian explosion from multi-Include queries while producing a clean comma-separated column.

## Import Pattern

### Command record

```csharp
public sealed record ImportEmployeesCommand(
    byte[] FileData,
    string FileName) : IAuditableCommand<ImportResultDto>
{
    [JsonIgnore] public string? UserId { get; init; }
    public AuditOperationType OperationType => AuditOperationType.Create;
    public object? GetTargetId() => null;
    public string? GetTargetDisplayName() => FileName;
}
```

### Handler structure

```
Parse CSV -> Validate headers -> Pre-load lookups -> Row-by-row validation -> Batch save -> ImportResultDto
```

Key steps:

1. **Parse header** -- handle BOM, build case-insensitive `headerMap`
2. **Validate required columns** -- fail fast with `Result.Failure` if missing
3. **Pre-load lookup data** -- e.g., departments by code (one query, avoids N+1)
4. **Track duplicates within file** -- `HashSet<string>` for unique fields like email
5. **Row-by-row processing** -- validate fields, check DB uniqueness, create entity
6. **Catch-all per row** -- `try/catch` captures unexpected errors without aborting
7. **Batch save** -- single `SaveChangesAsync` after all valid rows processed
8. **Return result** -- `ImportResultDto(totalRows, successCount, failedCount, errors)`

### Endpoint

```csharp
group.MapPost("/import", async (
    IFormFile file,
    [FromServices] ICurrentUser currentUser,
    IMessageBus bus, CancellationToken ct) =>
{
    using var ms = new MemoryStream();
    await file.CopyToAsync(ms, ct);
    var command = new ImportEmployeesCommand(ms.ToArray(), file.FileName)
    {
        UserId = currentUser.UserId
    };
    return (await bus.InvokeAsync<Result<ImportResultDto>>(command, ct))
        .ToHttpResult();
})
.DisableAntiforgery();  // Required for IFormFile uploads
```

## Error Handling

**Exports** use `Result<ExportResultDto>` -- failures return standard problem details via `ToHttpResult()`.

**Imports** capture row-level errors as `ImportErrorDto(RowNumber, Message)` without aborting:

```json
{
  "totalRows": 50,
  "successCount": 47,
  "failedCount": 3,
  "errors": [
    { "rowNumber": 5, "message": "Valid email is required." },
    { "rowNumber": 12, "message": "Department with code XYZ not found." },
    { "rowNumber": 30, "message": "Duplicate email within file." }
  ]
}
```

## Adding a New Export

1. Create `Features/{Feature}/Queries/Export{Entities}/Export{Entities}Query.cs` with `ExportFormat` + filters
2. Create handler -- inject `IRepository`, `IExcelExportService`, follow dual-format pattern
3. Create a `{Entities}ForExportSpec` if needed (or reuse existing list spec)
4. Add endpoint: `group.MapGet("/export", ...)` returning `Results.File(...)`
5. Write handler unit tests (mock `IExcelExportService`, verify headers/rows)

## Adding a New Import

1. Create `Features/{Feature}/Commands/Import{Entities}/Import{Entities}Command.cs` implementing `IAuditableCommand<ImportResultDto>`
2. Create handler with CSV parsing, row-level validation, batch save
3. Create validator -- `FileData` not empty, `FileName` ends with `.csv`
4. Add endpoint: `group.MapPost("/import", ...)` with `IFormFile`, `.DisableAntiforgery()`
5. Write handler + validator unit tests

## Existing Implementations

| Entity | Export | Import | Notes |
|--------|--------|--------|-------|
| Customers | ExportCustomersQuery | -- | Segment, tier, active filters |
| Orders | ExportOrdersQuery | -- | Status, date range, customer email |
| Products | ExportProductsFileQuery | -- | Category, status, optional attributes |
| Employees | ExportEmployeesQuery | ImportEmployeesCommand | Tag aggregation, department code lookup |
| Reports | ExportReportQuery | -- | Revenue, orders, inventory, product performance |
