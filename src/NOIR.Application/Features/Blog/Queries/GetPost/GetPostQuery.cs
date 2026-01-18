namespace NOIR.Application.Features.Blog.Queries.GetPost;

/// <summary>
/// Query to get a single blog post by ID or slug.
/// </summary>
public sealed record GetPostQuery(
    Guid? Id = null,
    string? Slug = null);
