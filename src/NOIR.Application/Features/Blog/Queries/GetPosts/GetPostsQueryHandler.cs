using NOIR.Application.Features.Blog.DTOs;
using NOIR.Application.Features.Blog.Specifications;

namespace NOIR.Application.Features.Blog.Queries.GetPosts;

/// <summary>
/// Wolverine handler for getting a list of blog posts.
/// </summary>
public class GetPostsQueryHandler
{
    private readonly IRepository<Post, Guid> _postRepository;

    public GetPostsQueryHandler(IRepository<Post, Guid> postRepository)
    {
        _postRepository = postRepository;
    }

    public async Task<Result<PagedResult<PostListDto>>> Handle(
        GetPostsQuery query,
        CancellationToken cancellationToken)
    {
        var skip = (query.Page - 1) * query.PageSize;

        ISpecification<Post> spec;

        if (query.PublishedOnly)
        {
            spec = new PublishedPostsSpec(
                query.Search,
                query.CategoryId,
                query.TagId,
                skip,
                query.PageSize);
        }
        else
        {
            spec = new PostsSpec(
                query.Search,
                query.Status,
                query.CategoryId,
                query.AuthorId,
                skip,
                query.PageSize);
        }

        var posts = await _postRepository.ListAsync(spec, cancellationToken);

        // Get total count for pagination (without skip/take)
        ISpecification<Post> countSpec;
        if (query.PublishedOnly)
        {
            countSpec = new PublishedPostsSpec(query.Search, query.CategoryId, query.TagId);
        }
        else
        {
            countSpec = new PostsSpec(query.Search, query.Status, query.CategoryId, query.AuthorId);
        }
        var totalCount = await _postRepository.CountAsync(countSpec, cancellationToken);

        var items = posts.Select(MapToListDto).ToList();

        var result = new PagedResult<PostListDto>(
            items,
            totalCount,
            query.Page,
            query.PageSize);

        return Result.Success(result);
    }

    private static PostListDto MapToListDto(Post post)
    {
        return new PostListDto(
            post.Id,
            post.Title,
            post.Slug,
            post.Excerpt,
            post.FeaturedImageUrl,
            post.Status,
            post.PublishedAt,
            post.ScheduledPublishAt,
            post.Category?.Name,
            null, // AuthorName would require user lookup
            post.ViewCount,
            post.ReadingTimeMinutes,
            post.CreatedAt);
    }
}

/// <summary>
/// Paged result for list queries.
/// </summary>
public sealed record PagedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
