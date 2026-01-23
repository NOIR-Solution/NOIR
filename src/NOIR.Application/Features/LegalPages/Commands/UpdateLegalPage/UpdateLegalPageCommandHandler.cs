
namespace NOIR.Application.Features.LegalPages.Commands.UpdateLegalPage;

/// <summary>
/// Wolverine handler for updating a legal page.
/// Implements Copy-on-Write: when a tenant edits a platform page, a tenant-specific copy is created.
/// </summary>
public class UpdateLegalPageCommandHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IRepository<LegalPage, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateLegalPageCommandHandler(
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
        UpdateLegalPageCommand command,
        CancellationToken cancellationToken)
    {
        var currentTenantId = _currentUser.TenantId;

        // Query ignoring tenant filter to access both tenant and platform pages
        var page = await _dbContext.LegalPages
            .IgnoreQueryFilters()
            .Where(p => p.Id == command.Id && !p.IsDeleted)
            .TagWith("LegalPageByIdForUpdate_CopyOnWrite")
            .FirstOrDefaultAsync(cancellationToken);

        if (page is null)
        {
            return Result.Failure<LegalPageDto>(
                Error.NotFound($"Legal page with ID '{command.Id}' not found.", "NOIR-LEGAL-001"));
        }

        LegalPage resultPage;

        // Copy-on-Edit: If this is a platform page and user has tenant context
        if (page.IsPlatformDefault && !string.IsNullOrEmpty(currentTenantId))
        {
            // Check if tenant already has a copy of this page (by slug)
            var existingTenantCopy = await _dbContext.LegalPages
                .IgnoreQueryFilters()
                .Where(p => p.Slug == page.Slug && p.TenantId == currentTenantId && !p.IsDeleted)
                .TagWith("LegalPageExistingTenantCopy")
                .FirstOrDefaultAsync(cancellationToken);

            if (existingTenantCopy != null)
            {
                // Tenant copy exists - update it instead of creating new
                _dbContext.Attach(existingTenantCopy);

                existingTenantCopy.Update(
                    command.Title,
                    command.HtmlContent,
                    command.MetaTitle,
                    command.MetaDescription,
                    command.CanonicalUrl,
                    command.AllowIndexing);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                resultPage = existingTenantCopy;
            }
            else
            {
                // No tenant copy exists - create a new tenant-specific override
                var tenantCopy = LegalPage.CreateTenantOverride(
                    currentTenantId,
                    page.Slug,
                    command.Title,
                    command.HtmlContent,
                    command.MetaTitle,
                    command.MetaDescription,
                    command.CanonicalUrl,
                    command.AllowIndexing);

                await _repository.AddAsync(tenantCopy, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                resultPage = tenantCopy;
            }
        }
        else
        {
            // Update existing page in place (tenant owns it or platform admin)
            _dbContext.Attach(page);

            page.Update(
                command.Title,
                command.HtmlContent,
                command.MetaTitle,
                command.MetaDescription,
                command.CanonicalUrl,
                command.AllowIndexing);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            resultPage = page;
        }

        var dto = new LegalPageDto(
            resultPage.Id,
            resultPage.Slug,
            resultPage.Title,
            resultPage.HtmlContent,
            resultPage.MetaTitle,
            resultPage.MetaDescription,
            resultPage.CanonicalUrl,
            resultPage.AllowIndexing,
            resultPage.IsActive,
            resultPage.Version,
            resultPage.LastModified,
            resultPage.CreatedAt,
            resultPage.ModifiedAt,
            IsInherited: false);

        return Result.Success(dto);
    }
}
