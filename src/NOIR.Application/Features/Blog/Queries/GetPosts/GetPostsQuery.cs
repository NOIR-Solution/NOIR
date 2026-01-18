namespace NOIR.Application.Features.Blog.Queries.GetPosts;

/// <summary>
/// Query to get a list of blog posts with optional filtering and pagination.
/// </summary>
public sealed record GetPostsQuery(
    string? Search = null,
    PostStatus? Status = null,
    Guid? CategoryId = null,
    Guid? AuthorId = null,
    Guid? TagId = null,
    bool PublishedOnly = false,
    int Page = 1,
    int PageSize = 20);
