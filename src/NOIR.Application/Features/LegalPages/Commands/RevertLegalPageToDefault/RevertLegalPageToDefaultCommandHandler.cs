using Microsoft.EntityFrameworkCore;

namespace NOIR.Application.Features.LegalPages.Commands.RevertLegalPageToDefault;

/// <summary>
/// Wolverine handler for reverting a tenant's customized legal page to the platform default.
/// This deletes the tenant's custom version, making the platform page visible again.
/// </summary>
public class RevertLegalPageToDefaultCommandHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IRepository<LegalPage, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public RevertLegalPageToDefaultCommandHandler(
        IApplicationDbContext dbContext,
        IRepository<LegalPage, Guid> repository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<LegalPageDto>> Handle(
        RevertLegalPageToDefaultCommand command,
        CancellationToken cancellationToken)
    {
        var currentTenantId = _currentUser.TenantId;

        // Platform users cannot revert (they don't have inherited pages)
        if (string.IsNullOrEmpty(currentTenantId))
        {
            return Result.Failure<LegalPageDto>(
                Error.Validation("tenant", "Only tenant users can revert to platform defaults.", "NOIR-LEGAL-003"));
        }

        // Find the tenant's custom page
        var tenantPage = await _dbContext.LegalPages
            .IgnoreQueryFilters()
            .Where(p => p.Id == command.Id && p.TenantId == currentTenantId && !p.IsDeleted)
            .TagWith("LegalPageByIdForRevert")
            .FirstOrDefaultAsync(cancellationToken);

        if (tenantPage is null)
        {
            return Result.Failure<LegalPageDto>(
                Error.NotFound($"Legal page with ID '{command.Id}' not found.", "NOIR-LEGAL-001"));
        }

        // Verify this is actually a tenant override (not a platform page)
        if (tenantPage.IsPlatformDefault)
        {
            return Result.Failure<LegalPageDto>(
                Error.Validation("page", "Cannot revert a platform page.", "NOIR-LEGAL-004"));
        }

        // Find the corresponding platform page
        var platformPage = await _dbContext.LegalPages
            .IgnoreQueryFilters()
            .Where(p => p.Slug == tenantPage.Slug
                && p.IsPlatformDefault
                && p.TenantId == null
                && !p.IsDeleted)
            .TagWith("PlatformLegalPageBySlug")
            .FirstOrDefaultAsync(cancellationToken);

        if (platformPage is null)
        {
            return Result.Failure<LegalPageDto>(
                Error.NotFound($"Platform page '{tenantPage.Slug}' not found.", "NOIR-LEGAL-005"));
        }

        // Soft delete the tenant's custom page using repository
        _dbContext.Attach(tenantPage);
        _repository.Remove(tenantPage);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Return the platform page as the result (now visible to tenant)
        var dto = new LegalPageDto(
            platformPage.Id,
            platformPage.Slug,
            platformPage.Title,
            platformPage.HtmlContent,
            platformPage.MetaTitle,
            platformPage.MetaDescription,
            platformPage.CanonicalUrl,
            platformPage.AllowIndexing,
            platformPage.IsActive,
            platformPage.Version,
            platformPage.LastModified,
            platformPage.CreatedAt,
            platformPage.ModifiedAt,
            IsInherited: true);

        return Result.Success(dto);
    }
}
