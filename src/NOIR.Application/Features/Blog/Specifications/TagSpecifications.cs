namespace NOIR.Application.Features.Blog.Specifications;

/// <summary>
/// Specification to retrieve tags with optional filtering.
/// </summary>
public sealed class TagsSpec : Specification<PostTag>
{
    public TagsSpec(string? search = null)
    {
        Query.Where(t => string.IsNullOrEmpty(search) ||
                         t.Name.Contains(search) ||
                         (t.Description != null && t.Description.Contains(search)))
             .OrderBy(t => t.Name)
             .TagWith("GetTags");
    }
}

/// <summary>
/// Specification to find a tag by ID.
/// </summary>
public sealed class TagByIdSpec : Specification<PostTag>
{
    public TagByIdSpec(Guid id)
    {
        Query.Where(t => t.Id == id)
             .TagWith("GetTagById");
    }
}

/// <summary>
/// Specification to find a tag by ID for update (with tracking).
/// </summary>
public sealed class TagByIdForUpdateSpec : Specification<PostTag>
{
    public TagByIdForUpdateSpec(Guid id)
    {
        Query.Where(t => t.Id == id)
             .AsTracking()
             .TagWith("GetTagByIdForUpdate");
    }
}

/// <summary>
/// Specification to find a tag by slug.
/// </summary>
public sealed class TagBySlugSpec : Specification<PostTag>
{
    public TagBySlugSpec(string slug, string? tenantId = null)
    {
        Query.Where(t => t.Slug == slug.ToLowerInvariant())
             .Where(t => tenantId == null || t.TenantId == tenantId)
             .TagWith("GetTagBySlug");
    }
}

/// <summary>
/// Specification to check if a tag slug is unique within a tenant.
/// </summary>
public sealed class TagSlugExistsSpec : Specification<PostTag>
{
    public TagSlugExistsSpec(string slug, string? tenantId = null, Guid? excludeId = null)
    {
        Query.Where(t => t.Slug == slug.ToLowerInvariant())
             .Where(t => tenantId == null || t.TenantId == tenantId)
             .Where(t => excludeId == null || t.Id != excludeId)
             .TagWith("CheckTagSlugExists");
    }
}

/// <summary>
/// Specification to find tags by IDs.
/// </summary>
public sealed class TagsByIdsSpec : Specification<PostTag>
{
    public TagsByIdsSpec(IEnumerable<Guid> ids)
    {
        var idList = ids.ToList();
        Query.Where(t => idList.Contains(t.Id))
             .AsTracking()
             .TagWith("GetTagsByIds");
    }
}

/// <summary>
/// Specification to find tag assignments for a post.
/// </summary>
public sealed class TagAssignmentsByPostIdSpec : Specification<PostTagAssignment>
{
    public TagAssignmentsByPostIdSpec(Guid postId)
    {
        Query.Where(ta => ta.PostId == postId)
             .AsTracking()
             .TagWith("GetTagAssignmentsByPostId");
    }
}

/// <summary>
/// Specification to find tag assignments for a tag.
/// </summary>
public sealed class TagAssignmentsByTagIdSpec : Specification<PostTagAssignment>
{
    public TagAssignmentsByTagIdSpec(Guid tagId)
    {
        Query.Where(ta => ta.TagId == tagId)
             .AsTracking()
             .TagWith("GetTagAssignmentsByTagId");
    }
}
