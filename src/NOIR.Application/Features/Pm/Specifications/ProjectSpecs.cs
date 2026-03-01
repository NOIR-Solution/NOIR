namespace NOIR.Application.Features.Pm.Specifications;

/// <summary>
/// Get project by ID with members and columns for detail view (read-only).
/// </summary>
public sealed class ProjectByIdSpec : Specification<Project>
{
    public ProjectByIdSpec(Guid id)
    {
        Query.Where(p => p.Id == id)
             .Include(p => p.Owner!)
             .Include("Members.Employee")
             .Include(p => p.Columns)
             .Include(p => p.Tasks)
             .AsSplitQuery()
             .TagWith("ProjectById");
    }
}

/// <summary>
/// Get project by ID with tracking for mutations.
/// </summary>
public sealed class ProjectByIdForUpdateSpec : Specification<Project>
{
    public ProjectByIdForUpdateSpec(Guid id)
    {
        Query.Where(p => p.Id == id)
             .AsTracking()
             .TagWith("ProjectByIdForUpdate");
    }
}

/// <summary>
/// Find project by slug for uniqueness check.
/// </summary>
public sealed class ProjectBySlugSpec : Specification<Project>
{
    public ProjectBySlugSpec(string slug, Guid? excludeId = null)
    {
        Query.Where(p => p.Slug == slug.ToLowerInvariant())
             .Where(p => excludeId == null || p.Id != excludeId)
             .TagWith("ProjectBySlug");
    }
}

/// <summary>
/// Paginated, filterable project list.
/// </summary>
public sealed class ProjectsByFilterSpec : Specification<Project>
{
    public ProjectsByFilterSpec(
        string? search = null,
        ProjectStatus? status = null,
        Guid? ownerId = null,
        int? skip = null,
        int? take = null)
    {
        if (!string.IsNullOrEmpty(search))
        {
            Query.Where(p => p.Name.Contains(search));
        }

        if (status.HasValue)
            Query.Where(p => p.Status == status.Value);

        if (ownerId.HasValue)
            Query.Where(p => p.OwnerId == ownerId.Value);

        Query.Include(p => p.Owner!)
             .Include(p => p.Members)
             .Include(p => p.Tasks)
             .OrderByDescending(p => p.CreatedAt);

        if (skip.HasValue) Query.Skip(skip.Value);
        if (take.HasValue) Query.Take(take.Value);

        Query.TagWith("ProjectsByFilter");
    }
}

/// <summary>
/// Count projects matching filters (without pagination).
/// </summary>
public sealed class ProjectsCountSpec : Specification<Project>
{
    public ProjectsCountSpec(
        string? search = null,
        ProjectStatus? status = null,
        Guid? ownerId = null)
    {
        if (!string.IsNullOrEmpty(search))
        {
            Query.Where(p => p.Name.Contains(search));
        }

        if (status.HasValue)
            Query.Where(p => p.Status == status.Value);

        if (ownerId.HasValue)
            Query.Where(p => p.OwnerId == ownerId.Value);

        Query.TagWith("ProjectsCount");
    }
}
