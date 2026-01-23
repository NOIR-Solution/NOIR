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
        // Resolve featured image URL: prefer MediaFile.DefaultUrl, fallback to direct URL
        var featuredImageUrl = post.FeaturedImage?.DefaultUrl ?? post.FeaturedImageUrl;

        // Extract thumbnail URL from variants (prefer small webp, fallback to thumb webp)
        string? thumbnailUrl = null;
        if (post.FeaturedImage?.VariantsJson is { Length: > 2 })
        {
            thumbnailUrl = ExtractThumbnailUrl(post.FeaturedImage.VariantsJson);
        }

        return new PostListDto(
            post.Id,
            post.Title,
            post.Slug,
            post.Excerpt,
            post.FeaturedImageId,
            featuredImageUrl,
            thumbnailUrl ?? featuredImageUrl, // Fallback to full URL if no thumbnail
            post.Status,
            post.PublishedAt,
            post.ScheduledPublishAt,
            post.Category?.Name,
            null, // AuthorName would require user lookup
            post.ViewCount,
            post.ReadingTimeMinutes,
            post.CreatedAt);
    }

    /// <summary>
    /// Extracts a thumbnail URL from variants JSON.
    /// Prefers: thumb/webp -> medium/webp -> thumb/jpeg -> medium/jpeg -> first available
    /// (SEO-optimized: only thumb, medium, large variants are generated)
    /// </summary>
    private static string? ExtractThumbnailUrl(string variantsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(variantsJson);
            var variants = doc.RootElement;

            if (variants.ValueKind != JsonValueKind.Array)
                return null;

            string? thumbWebp = null;
            string? mediumWebp = null;
            string? thumbJpeg = null;
            string? mediumJpeg = null;
            string? firstUrl = null;

            foreach (var variant in variants.EnumerateArray())
            {
                // JSON uses lowercase property names: variant, format, url
                var variantName = variant.GetProperty("variant").GetString();
                var format = variant.GetProperty("format").GetString();
                var url = variant.GetProperty("url").GetString();

                if (string.IsNullOrEmpty(url))
                    continue;

                firstUrl ??= url;

                if (variantName == "thumb" && format == "webp")
                    thumbWebp = url;
                else if (variantName == "medium" && format == "webp")
                    mediumWebp = url;
                else if (variantName == "thumb" && format == "jpeg")
                    thumbJpeg = url;
                else if (variantName == "medium" && format == "jpeg")
                    mediumJpeg = url;
            }

            // Prefer thumb (150px) for list thumbnails, fallback to medium (640px)
            return thumbWebp ?? thumbJpeg ?? mediumWebp ?? mediumJpeg ?? firstUrl;
        }
        catch
        {
            return null;
        }
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
