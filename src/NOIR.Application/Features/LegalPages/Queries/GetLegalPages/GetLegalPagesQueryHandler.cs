
namespace NOIR.Application.Features.LegalPages.Queries.GetLegalPages;

/// <summary>
/// Wolverine handler for getting list of legal pages.
/// Implements Copy-on-Write pattern by showing:
/// - Tenant's own pages (TenantId = current tenant)
/// - Platform pages not yet customized (TenantId = null, no tenant override)
/// </summary>
public class GetLegalPagesQueryHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetLegalPagesQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<List<LegalPageListDto>>> Handle(
        GetLegalPagesQuery query,
        CancellationToken cancellationToken)
    {
        var currentTenantId = _currentUser.TenantId;

        // Query all legal pages ignoring tenant filter to get both tenant and platform pages
        var allPages = await _dbContext.LegalPages
            .IgnoreQueryFilters()
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Slug)
            .TagWith("LegalPages_CopyOnWrite")
            .ToListAsync(cancellationToken);

        // Group by slug to handle inheritance
        var pagesBySlug = allPages.GroupBy(p => p.Slug);

        var result = new List<LegalPageListDto>();

        foreach (var group in pagesBySlug)
        {
            // Check if tenant has customized this page
            var tenantPage = group.FirstOrDefault(p => p.TenantId == currentTenantId);
            var platformPage = group.FirstOrDefault(p => p.TenantId == null);

            if (tenantPage != null)
            {
                // Tenant has customized - show their version (not inherited)
                result.Add(new LegalPageListDto(
                    tenantPage.Id,
                    tenantPage.Slug,
                    tenantPage.Title,
                    tenantPage.IsActive,
                    tenantPage.Version,
                    tenantPage.LastModified,
                    IsInherited: false));
            }
            else if (platformPage != null)
            {
                // No tenant customization - show platform page as inherited
                var isInherited = LegalPageHelpers.IsPageInherited(
                    platformPage.TenantId,
                    currentTenantId);

                result.Add(new LegalPageListDto(
                    platformPage.Id,
                    platformPage.Slug,
                    platformPage.Title,
                    platformPage.IsActive,
                    platformPage.Version,
                    platformPage.LastModified,
                    isInherited));
            }
        }

        return Result.Success(result.OrderBy(p => p.Slug).ToList());
    }
}
