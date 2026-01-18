namespace NOIR.Application.Features.Blog.DTOs;

/// <summary>
/// Full post details for editing.
/// </summary>
public sealed record PostDto(
    Guid Id,
    string Title,
    string Slug,
    string? Excerpt,
    string? ContentJson,
    string? ContentHtml,
    string? FeaturedImageUrl,
    string? FeaturedImageAlt,
    PostStatus Status,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ScheduledPublishAt,
    string? MetaTitle,
    string? MetaDescription,
    string? CanonicalUrl,
    bool AllowIndexing,
    Guid? CategoryId,
    string? CategoryName,
    Guid AuthorId,
    string? AuthorName,
    long ViewCount,
    int ReadingTimeMinutes,
    List<PostTagDto> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);

/// <summary>
/// Simplified post for list views.
/// </summary>
public sealed record PostListDto(
    Guid Id,
    string Title,
    string Slug,
    string? Excerpt,
    string? FeaturedImageUrl,
    PostStatus Status,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ScheduledPublishAt,
    string? CategoryName,
    string? AuthorName,
    long ViewCount,
    int ReadingTimeMinutes,
    DateTimeOffset CreatedAt);

/// <summary>
/// Post category with hierarchy support.
/// </summary>
public sealed record PostCategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? MetaTitle,
    string? MetaDescription,
    string? ImageUrl,
    int SortOrder,
    int PostCount,
    Guid? ParentId,
    string? ParentName,
    List<PostCategoryDto>? Children,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);

/// <summary>
/// Simplified category for list views and dropdowns.
/// </summary>
public sealed record PostCategoryListDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    int SortOrder,
    int PostCount,
    Guid? ParentId,
    string? ParentName,
    int ChildCount);

/// <summary>
/// Post tag details.
/// </summary>
public sealed record PostTagDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? Color,
    int PostCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt);

/// <summary>
/// Simplified tag for list views and dropdowns.
/// </summary>
public sealed record PostTagListDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? Color,
    int PostCount);

/// <summary>
/// Request to create a new post.
/// </summary>
public sealed record CreatePostRequest(
    string Title,
    string Slug,
    string? Excerpt,
    string? ContentJson,
    string? ContentHtml,
    string? FeaturedImageUrl,
    string? FeaturedImageAlt,
    string? MetaTitle,
    string? MetaDescription,
    string? CanonicalUrl,
    bool AllowIndexing,
    Guid? CategoryId,
    List<Guid>? TagIds);

/// <summary>
/// Request to update an existing post.
/// </summary>
public sealed record UpdatePostRequest(
    string Title,
    string Slug,
    string? Excerpt,
    string? ContentJson,
    string? ContentHtml,
    string? FeaturedImageUrl,
    string? FeaturedImageAlt,
    string? MetaTitle,
    string? MetaDescription,
    string? CanonicalUrl,
    bool AllowIndexing,
    Guid? CategoryId,
    List<Guid>? TagIds);

/// <summary>
/// Request to publish a post.
/// </summary>
public sealed record PublishPostRequest(
    DateTimeOffset? ScheduledPublishAt = null);

/// <summary>
/// Request to create a category.
/// </summary>
public sealed record CreateCategoryRequest(
    string Name,
    string Slug,
    string? Description,
    string? MetaTitle,
    string? MetaDescription,
    string? ImageUrl,
    int SortOrder,
    Guid? ParentId);

/// <summary>
/// Request to update a category.
/// </summary>
public sealed record UpdateCategoryRequest(
    string Name,
    string Slug,
    string? Description,
    string? MetaTitle,
    string? MetaDescription,
    string? ImageUrl,
    int SortOrder,
    Guid? ParentId);

/// <summary>
/// Request to create a tag.
/// </summary>
public sealed record CreateTagRequest(
    string Name,
    string Slug,
    string? Description,
    string? Color);

/// <summary>
/// Request to update a tag.
/// </summary>
public sealed record UpdateTagRequest(
    string Name,
    string Slug,
    string? Description,
    string? Color);
