using Microsoft.EntityFrameworkCore;

namespace NOIR.Application.Features.LegalPages.Queries.GetLegalPage;

/// <summary>
/// Wolverine handler for getting a single legal page by ID.
/// Supports Copy-on-Write pattern by indicating if page is inherited.
/// </summary>
public class GetLegalPageQueryHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetLegalPageQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<LegalPageDto>> Handle(
        GetLegalPageQuery query,
        CancellationToken cancellationToken)
    {
        // Query ignoring tenant filter to access both tenant and platform pages
        var page = await _dbContext.LegalPages
            .IgnoreQueryFilters()
            .Where(p => p.Id == query.Id && !p.IsDeleted)
            .TagWith("LegalPageById_CopyOnWrite")
            .FirstOrDefaultAsync(cancellationToken);

        if (page is null)
        {
            return Result.Failure<LegalPageDto>(
                Error.NotFound($"Legal page with ID '{query.Id}' not found.", "NOIR-LEGAL-001"));
        }

        var isInherited = LegalPageHelpers.IsPageInherited(page.TenantId, _currentUser.TenantId);

        var dto = new LegalPageDto(
            page.Id,
            page.Slug,
            page.Title,
            page.HtmlContent,
            page.MetaTitle,
            page.MetaDescription,
            page.CanonicalUrl,
            page.AllowIndexing,
            page.IsActive,
            page.Version,
            page.LastModified,
            page.CreatedAt,
            page.ModifiedAt,
            isInherited);

        return Result.Success(dto);
    }
}
