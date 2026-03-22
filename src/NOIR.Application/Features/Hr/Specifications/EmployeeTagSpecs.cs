namespace NOIR.Application.Features.Hr.Specifications;

/// <summary>
/// Get employee tag by ID with optional tracking for mutations.
/// </summary>
public sealed class EmployeeTagByIdSpec : Specification<EmployeeTag>
{
    public EmployeeTagByIdSpec(Guid id, bool tracking = false)
    {
        Query.Where(t => t.Id == id)
             .Include(t => t.TagAssignments);

        if (tracking)
            Query.AsTracking();

        Query.TagWith("EmployeeTagById");
    }
}

/// <summary>
/// Check tag name+category uniqueness per tenant.
/// </summary>
public sealed class EmployeeTagByNameAndCategorySpec : Specification<EmployeeTag>
{
    public EmployeeTagByNameAndCategorySpec(string name, EmployeeTagCategory category, string? tenantId, Guid? excludeId = null)
    {
        Query.Where(t => t.Name == name.Trim())
             .Where(t => t.Category == category)
             .Where(t => tenantId == null || t.TenantId == tenantId)
             .Where(t => excludeId == null || t.Id != excludeId)
             .TagWith("EmployeeTagByNameAndCategory");
    }
}

/// <summary>
/// Get all employee tags with optional filters. Includes TagAssignments for EmployeeCount.
/// </summary>
public sealed class AllEmployeeTagsSpec : Specification<EmployeeTag>
{
    public AllEmployeeTagsSpec(EmployeeTagCategory? category = null)
    {
        if (category.HasValue)
            Query.Where(t => t.Category == category.Value);

        Query.Include(t => t.TagAssignments)
             .OrderBy(t => t.Category)
             .ThenBy(t => t.SortOrder)
             .ThenBy(t => t.Name)
             .TagWith("AllEmployeeTags");
    }
}

/// <summary>
/// Get tags by IDs (for bulk operations).
/// </summary>
public sealed class EmployeeTagsByIdsSpec : Specification<EmployeeTag>
{
    public EmployeeTagsByIdsSpec(List<Guid> ids)
    {
        Query.Where(t => ids.Contains(t.Id))
             .TagWith("EmployeeTagsByIds");
    }
}
