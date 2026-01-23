
namespace NOIR.Application.Features.EmailTemplates.Commands.RevertToPlatformDefault;

/// <summary>
/// Wolverine handler for reverting a tenant's customized email template to the platform default.
/// This deletes the tenant's custom version, making the platform template visible again.
/// </summary>
public class RevertToPlatformDefaultCommandHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IRepository<EmailTemplate, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public RevertToPlatformDefaultCommandHandler(
        IApplicationDbContext dbContext,
        IRepository<EmailTemplate, Guid> repository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<EmailTemplateDto>> Handle(
        RevertToPlatformDefaultCommand command,
        CancellationToken cancellationToken)
    {
        var currentTenantId = _currentUser.TenantId;

        // Platform users cannot revert (they don't have inherited templates)
        if (string.IsNullOrEmpty(currentTenantId))
        {
            return Result.Failure<EmailTemplateDto>(
                Error.Validation("tenant", "Only tenant users can revert to platform defaults.", "NOIR-EMAIL-004"));
        }

        // Find the tenant's custom template
        var tenantTemplate = await _dbContext.EmailTemplates
            .IgnoreQueryFilters()
            .Where(t => t.Id == command.Id && t.TenantId == currentTenantId && !t.IsDeleted)
            .TagWith("EmailTemplateByIdForRevert")
            .FirstOrDefaultAsync(cancellationToken);

        if (tenantTemplate is null)
        {
            return Result.Failure<EmailTemplateDto>(
                Error.NotFound($"Email template with ID '{command.Id}' not found.", "NOIR-EMAIL-001"));
        }

        // Verify this is actually a tenant override (not a platform template)
        if (tenantTemplate.IsPlatformDefault)
        {
            return Result.Failure<EmailTemplateDto>(
                Error.Validation("template", "Cannot revert a platform template.", "NOIR-EMAIL-005"));
        }

        // Find the corresponding platform template
        var platformTemplate = await _dbContext.EmailTemplates
            .IgnoreQueryFilters()
            .Where(t => t.Name == tenantTemplate.Name
                && t.IsPlatformDefault
                && t.TenantId == null
                && !t.IsDeleted)
            .TagWith("PlatformEmailTemplateByName")
            .FirstOrDefaultAsync(cancellationToken);

        if (platformTemplate is null)
        {
            return Result.Failure<EmailTemplateDto>(
                Error.NotFound($"Platform template '{tenantTemplate.Name}' not found.", "NOIR-EMAIL-002"));
        }

        // Soft delete the tenant's custom template using repository
        _dbContext.Attach(tenantTemplate);
        _repository.Remove(tenantTemplate);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Return the platform template as the result (now visible to tenant)
        var dto = new EmailTemplateDto(
            platformTemplate.Id,
            platformTemplate.Name,
            platformTemplate.Subject,
            platformTemplate.HtmlBody,
            platformTemplate.PlainTextBody,
            platformTemplate.IsActive,
            platformTemplate.Version,
            platformTemplate.Description,
            EmailTemplateHelpers.ParseAvailableVariables(platformTemplate.AvailableVariables),
            platformTemplate.CreatedAt,
            platformTemplate.ModifiedAt,
            IsInherited: true);

        return Result.Success(dto);
    }
}
