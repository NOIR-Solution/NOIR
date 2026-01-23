using Microsoft.EntityFrameworkCore;

namespace NOIR.Application.Features.LegalPages.Queries.GetPublicLegalPage;

/// <summary>
/// Wolverine handler for getting a legal page by slug for public display.
/// Resolves tenant override if available, otherwise returns platform default.
/// Only returns active pages.
/// </summary>
public class GetPublicLegalPageQueryHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetPublicLegalPageQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<PublicLegalPageDto>> Handle(
        GetPublicLegalPageQuery query,
        CancellationToken cancellationToken)
    {
        var currentTenantId = _currentUser.TenantId;

        // Query ignoring tenant filter to resolve both tenant and platform pages
        var pages = await _dbContext.LegalPages
            .IgnoreQueryFilters()
            .Where(p => p.Slug == query.Slug && !p.IsDeleted && p.IsActive)
            .TagWith("PublicLegalPageBySlug")
            .ToListAsync(cancellationToken);

        // Prioritize tenant-specific page over platform default
        var tenantPage = pages.FirstOrDefault(p => p.TenantId == currentTenantId);
        var platformPage = pages.FirstOrDefault(p => p.TenantId == null);

        var page = tenantPage ?? platformPage;

        if (page is null)
        {
            return Result.Failure<PublicLegalPageDto>(
                Error.NotFound($"Legal page '{query.Slug}' not found.", "NOIR-LEGAL-002"));
        }

        var dto = new PublicLegalPageDto(
            page.Slug,
            page.Title,
            page.HtmlContent,
            page.MetaTitle,
            page.MetaDescription,
            page.CanonicalUrl,
            page.AllowIndexing,
            page.LastModified);

        return Result.Success(dto);
    }
}
