namespace NOIR.Application.Features.LegalPages.DTOs;

/// <summary>
/// Full legal page details for editing.
/// </summary>
/// <param name="IsInherited">
/// True if this is a platform page (TenantId=null) that hasn't been customized by the current tenant.
/// When a tenant edits an inherited page, a copy is created with the tenant's ID.
/// </param>
public sealed record LegalPageDto(
    Guid Id,
    string Slug,
    string Title,
    string HtmlContent,
    string? MetaTitle,
    string? MetaDescription,
    string? CanonicalUrl,
    bool AllowIndexing,
    bool IsActive,
    int Version,
    DateTimeOffset LastModified,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt,
    bool IsInherited = false);

/// <summary>
/// Simplified legal page for list views.
/// </summary>
/// <param name="IsInherited">
/// True if this is a platform page (TenantId=null) that hasn't been customized by the current tenant.
/// </param>
public sealed record LegalPageListDto(
    Guid Id,
    string Slug,
    string Title,
    bool IsActive,
    int Version,
    DateTimeOffset LastModified,
    bool IsInherited = false);

/// <summary>
/// Public legal page response (for website visitors).
/// </summary>
public sealed record PublicLegalPageDto(
    string Slug,
    string Title,
    string HtmlContent,
    string? MetaTitle,
    string? MetaDescription,
    string? CanonicalUrl,
    bool AllowIndexing,
    DateTimeOffset LastModified);

/// <summary>
/// Request to update a legal page.
/// </summary>
public sealed record UpdateLegalPageRequest(
    string Title,
    string HtmlContent,
    string? MetaTitle,
    string? MetaDescription,
    string? CanonicalUrl,
    bool AllowIndexing = true);
