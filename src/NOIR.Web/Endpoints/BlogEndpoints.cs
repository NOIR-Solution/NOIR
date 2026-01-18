using NOIR.Application.Features.Blog.Commands.CreateCategory;
using NOIR.Application.Features.Blog.Commands.CreatePost;
using NOIR.Application.Features.Blog.Commands.CreateTag;
using NOIR.Application.Features.Blog.Commands.DeleteCategory;
using NOIR.Application.Features.Blog.Commands.DeletePost;
using NOIR.Application.Features.Blog.Commands.DeleteTag;
using NOIR.Application.Features.Blog.Commands.PublishPost;
using NOIR.Application.Features.Blog.Commands.UpdateCategory;
using NOIR.Application.Features.Blog.Commands.UpdatePost;
using NOIR.Application.Features.Blog.Commands.UpdateTag;
using NOIR.Application.Features.Blog.DTOs;
using NOIR.Application.Features.Blog.Queries.GetCategories;
using NOIR.Application.Features.Blog.Queries.GetPost;
using NOIR.Application.Features.Blog.Queries.GetPosts;
using NOIR.Application.Features.Blog.Queries.GetTags;
using NOIR.Domain.Enums;
using BlogPagedResult = NOIR.Application.Features.Blog.Queries.GetPosts.PagedResult<NOIR.Application.Features.Blog.DTOs.PostListDto>;

namespace NOIR.Web.Endpoints;

/// <summary>
/// Blog CMS API endpoints.
/// Provides CRUD operations for posts, categories, and tags.
/// </summary>
public static class BlogEndpoints
{
    public static void MapBlogEndpoints(this IEndpointRouteBuilder app)
    {
        MapPostEndpoints(app);
        MapCategoryEndpoints(app);
        MapTagEndpoints(app);
    }

    private static void MapPostEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/blog/posts")
            .WithTags("Blog Posts")
            .RequireAuthorization();

        // Get all posts (paginated)
        group.MapGet("/", async (
            [FromQuery] string? search,
            [FromQuery] PostStatus? status,
            [FromQuery] Guid? categoryId,
            [FromQuery] Guid? authorId,
            [FromQuery] Guid? tagId,
            [FromQuery] bool? publishedOnly,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            IMessageBus bus) =>
        {
            var query = new GetPostsQuery(
                search,
                status,
                categoryId,
                authorId,
                tagId,
                publishedOnly ?? false,
                page ?? 1,
                pageSize ?? 20);
            var result = await bus.InvokeAsync<Result<BlogPagedResult>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.BlogPostsRead)
        .WithName("GetBlogPosts")
        .WithSummary("Get paginated list of blog posts")
        .WithDescription("Returns blog posts with optional filtering by search, status, category, author, and tags.")
        .Produces<BlogPagedResult>(StatusCodes.Status200OK);

        // Get post by ID
        group.MapGet("/{id:guid}", async (Guid id, IMessageBus bus) =>
        {
            var query = new GetPostQuery(Id: id);
            var result = await bus.InvokeAsync<Result<PostDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.BlogPostsRead)
        .WithName("GetBlogPostById")
        .WithSummary("Get blog post by ID")
        .WithDescription("Returns full blog post details including content and metadata.")
        .Produces<PostDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Get post by slug (public)
        group.MapGet("/by-slug/{slug}", async (string slug, IMessageBus bus) =>
        {
            var query = new GetPostQuery(Slug: slug);
            var result = await bus.InvokeAsync<Result<PostDto>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.BlogPostsRead)
        .WithName("GetBlogPostBySlug")
        .WithSummary("Get blog post by slug")
        .WithDescription("Returns blog post by its URL-friendly slug.")
        .Produces<PostDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Create post
        group.MapPost("/", async (
            CreatePostRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new CreatePostCommand(
                request.Title,
                request.Slug,
                request.Excerpt,
                request.ContentJson,
                request.ContentHtml,
                request.FeaturedImageUrl,
                request.FeaturedImageAlt,
                request.MetaTitle,
                request.MetaDescription,
                request.CanonicalUrl,
                request.AllowIndexing,
                request.CategoryId,
                request.TagIds)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<PostDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.BlogPostsCreate)
        .WithName("CreateBlogPost")
        .WithSummary("Create a new blog post")
        .WithDescription("Creates a new blog post in draft status.")
        .Produces<PostDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Update post
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdatePostRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdatePostCommand(
                id,
                request.Title,
                request.Slug,
                request.Excerpt,
                request.ContentJson,
                request.ContentHtml,
                request.FeaturedImageUrl,
                request.FeaturedImageAlt,
                request.MetaTitle,
                request.MetaDescription,
                request.CanonicalUrl,
                request.AllowIndexing,
                request.CategoryId,
                request.TagIds)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<PostDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.BlogPostsUpdate)
        .WithName("UpdateBlogPost")
        .WithSummary("Update an existing blog post")
        .WithDescription("Updates blog post content and metadata.")
        .Produces<PostDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Delete post (soft delete)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeletePostCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.BlogPostsDelete)
        .WithName("DeleteBlogPost")
        .WithSummary("Soft-delete a blog post")
        .WithDescription("Soft-deletes a blog post. It can be restored later.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        // Publish post
        group.MapPost("/{id:guid}/publish", async (
            Guid id,
            PublishPostRequest? request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new PublishPostCommand(id, request?.ScheduledPublishAt)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<PostDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.BlogPostsPublish)
        .WithName("PublishBlogPost")
        .WithSummary("Publish or schedule a blog post")
        .WithDescription("Publishes the post immediately or schedules it for future publication.")
        .Produces<PostDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }

    private static void MapCategoryEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/blog/categories")
            .WithTags("Blog Categories")
            .RequireAuthorization();

        // Get all categories
        group.MapGet("/", async (
            [FromQuery] string? search,
            [FromQuery] bool? topLevelOnly,
            [FromQuery] bool? includeChildren,
            IMessageBus bus) =>
        {
            var query = new GetCategoriesQuery(search, topLevelOnly ?? false, includeChildren ?? false);
            var result = await bus.InvokeAsync<Result<List<PostCategoryListDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.BlogCategoriesRead)
        .WithName("GetBlogCategories")
        .WithSummary("Get list of blog categories")
        .WithDescription("Returns all blog categories with optional filtering.")
        .Produces<List<PostCategoryListDto>>(StatusCodes.Status200OK);

        // Create category
        group.MapPost("/", async (
            CreateCategoryRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new CreateCategoryCommand(
                request.Name,
                request.Slug,
                request.Description,
                request.MetaTitle,
                request.MetaDescription,
                request.ImageUrl,
                request.SortOrder,
                request.ParentId)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<PostCategoryDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.BlogCategoriesCreate)
        .WithName("CreateBlogCategory")
        .WithSummary("Create a new blog category")
        .WithDescription("Creates a new blog category. Can be nested under a parent category.")
        .Produces<PostCategoryDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Update category
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateCategoryRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateCategoryCommand(
                id,
                request.Name,
                request.Slug,
                request.Description,
                request.MetaTitle,
                request.MetaDescription,
                request.ImageUrl,
                request.SortOrder,
                request.ParentId)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<PostCategoryDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.BlogCategoriesUpdate)
        .WithName("UpdateBlogCategory")
        .WithSummary("Update an existing blog category")
        .WithDescription("Updates category details and parent relationship.")
        .Produces<PostCategoryDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Delete category (soft delete)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeleteCategoryCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.BlogCategoriesDelete)
        .WithName("DeleteBlogCategory")
        .WithSummary("Soft-delete a blog category")
        .WithDescription("Soft-deletes a category. Will fail if it has child categories or posts.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
    }

    private static void MapTagEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/blog/tags")
            .WithTags("Blog Tags")
            .RequireAuthorization();

        // Get all tags
        group.MapGet("/", async (
            [FromQuery] string? search,
            IMessageBus bus) =>
        {
            var query = new GetTagsQuery(search);
            var result = await bus.InvokeAsync<Result<List<PostTagListDto>>>(query);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.BlogTagsRead)
        .WithName("GetBlogTags")
        .WithSummary("Get list of blog tags")
        .WithDescription("Returns all blog tags with optional search filtering.")
        .Produces<List<PostTagListDto>>(StatusCodes.Status200OK);

        // Create tag
        group.MapPost("/", async (
            CreateTagRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new CreateTagCommand(
                request.Name,
                request.Slug,
                request.Description,
                request.Color)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<PostTagDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.BlogTagsCreate)
        .WithName("CreateBlogTag")
        .WithSummary("Create a new blog tag")
        .WithDescription("Creates a new blog tag with optional color.")
        .Produces<PostTagDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Update tag
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateTagRequest request,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new UpdateTagCommand(
                id,
                request.Name,
                request.Slug,
                request.Description,
                request.Color)
            {
                UserId = currentUser.UserId
            };
            var result = await bus.InvokeAsync<Result<PostTagDto>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.BlogTagsUpdate)
        .WithName("UpdateBlogTag")
        .WithSummary("Update an existing blog tag")
        .WithDescription("Updates tag name, slug, description, or color.")
        .Produces<PostTagDto>(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Delete tag (soft delete)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            [FromServices] ICurrentUser currentUser,
            IMessageBus bus) =>
        {
            var command = new DeleteTagCommand(id) { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<bool>>(command);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.BlogTagsDelete)
        .WithName("DeleteBlogTag")
        .WithSummary("Soft-delete a blog tag")
        .WithDescription("Soft-deletes a tag. Removes tag assignments from posts.")
        .Produces(StatusCodes.Status200OK)
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }
}
