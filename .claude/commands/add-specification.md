# Add Specification Command

Create a new Specification for database queries.

## Instructions

When creating specification for "$ARGUMENTS":

1. **Location**: `src/NOIR.Application/Specifications/{Entity}/`

2. **Naming**: `{Entity}{Filter}Spec.cs` (e.g., `ActiveCustomersSpec`, `OrdersByUserIdSpec`)

3. **Template**:

```csharp
using NOIR.Domain.Entities;

namespace NOIR.Application.Specifications.{Entity}s;

public class {Name}Spec : Specification<{Entity}>
{
    public {Name}Spec(/* parameters */)
    {
        Query.Where(x => /* criteria */)
             .TagWith("{Name}");  // REQUIRED for debugging

        // Optional: Add more criteria
        // Query.Where(x => x.OtherCondition);

        // Optional: Include related entities
        // Query.Include(x => x.RelatedEntity);

        // Optional: Ordering
        // Query.OrderBy(x => x.CreatedAt);

        // Optional: Pagination
        // Query.Paginate(pageIndex, pageSize);

        // Optional: Performance for multiple collections
        // Query.AsSplitQuery();
    }
}
```

4. **With Projection** (for DTOs):

```csharp
public class {Entity}SummarySpec : Specification<{Entity}, {Entity}SummaryDto>
{
    public {Entity}SummarySpec()
    {
        Query.Where(x => x.IsActive)
             .Select(x => new {Entity}SummaryDto
             {
                 Id = x.Id,
                 Name = x.Name
             })
             .TagWith("{Entity}Summary");
    }
}
```

5. **Build**: `dotnet build src/NOIR.sln`

## Performance Checklist

- [ ] Added `TagWith()` for SQL debugging
- [ ] Used `AsSplitQuery()` if loading multiple collections
- [ ] Used projection for read-only DTOs
- [ ] Avoided `AsTracking()` unless entity will be modified
