using System.ComponentModel;
using ModelContextProtocol.Server;
using NOIR.Application.Features.Blog.Commands.PublishPost;
using NOIR.Application.Features.Blog.DTOs;
using NOIR.Application.Features.Blog.Queries.GetPosts;
using NOIR.Application.Features.Blog.Queries.GetPost;
using NOIR.Web.Mcp.Filters;
using NOIR.Web.Mcp.Helpers;

namespace NOIR.Web.Mcp.Tools;

/// <summary>
/// MCP tools for blog content management.
/// </summary>
[McpServerToolType]
[RequiresModule(ModuleNames.Content.Blog)]
public sealed class BlogTools(IMessageBus bus, ICurrentUser currentUser)
{
    [McpServerTool(Name = "noir_blog_posts_list", ReadOnly = true, Idempotent = true)]
    [Description("List blog posts with pagination and filtering. Supports search, status, category, author, and tag filters.")]
    public async Task<PagedResult<PostListDto>> ListPosts(
        [Description("Search by title or content")] string? search = null,
        [Description("Filter by status: Draft, Published, Scheduled, Archived")] string? status = null,
        [Description("Filter by category ID (GUID)")] string? categoryId = null,
        [Description("Filter by author user ID (GUID)")] string? authorId = null,
        [Description("Only show published posts")] bool publishedOnly = false,
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Page size, max 100 (default: 20)")] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var postStatus = status is not null && Enum.TryParse<PostStatus>(status, true, out var s) ? s : (PostStatus?)null;
        var catId = categoryId is not null ? Guid.Parse(categoryId) : (Guid?)null;
        var aId = authorId is not null ? Guid.Parse(authorId) : (Guid?)null;

        var result = await bus.InvokeAsync<Result<PagedResult<PostListDto>>>(
            new GetPostsQuery(search, postStatus, catId, aId, null, publishedOnly, page, pageSize), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_blog_posts_get", ReadOnly = true, Idempotent = true)]
    [Description("Get full blog post details by ID or slug, including content, SEO metadata, category, tags, and author info.")]
    public async Task<PostDto> GetPost(
        [Description("Post ID (GUID) — provide either id or slug")] string? id = null,
        [Description("Post URL slug — provide either id or slug")] string? slug = null,
        CancellationToken ct = default)
    {
        var postId = id is not null ? Guid.Parse(id) : (Guid?)null;
        var result = await bus.InvokeAsync<Result<PostDto>>(
            new GetPostQuery(postId, slug), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_blog_posts_publish", Destructive = false)]
    [Description("Publish a draft blog post immediately or schedule it for future publication.")]
    public async Task<PostDto> PublishPost(
        [Description("The blog post ID (GUID)")] string postId,
        [Description("Schedule publish date (ISO 8601, optional — omit for immediate publish)")] string? scheduledAt = null,
        CancellationToken ct = default)
    {
        var scheduled = scheduledAt is not null ? DateTimeOffset.Parse(scheduledAt) : (DateTimeOffset?)null;
        var command = new PublishPostCommand(Guid.Parse(postId), scheduled)
        {
            UserId = currentUser.UserId
        };
        var result = await bus.InvokeAsync<Result<PostDto>>(command, ct);
        return result.Unwrap();
    }
}
