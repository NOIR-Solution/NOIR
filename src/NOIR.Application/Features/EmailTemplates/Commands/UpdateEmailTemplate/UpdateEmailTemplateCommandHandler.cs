
namespace NOIR.Application.Features.EmailTemplates.Commands.UpdateEmailTemplate;

/// <summary>
/// Wolverine handler for updating an email template.
/// Implements Copy-on-Write: when a tenant edits a platform template, a tenant-specific copy is created.
/// </summary>
public class UpdateEmailTemplateCommandHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IRepository<EmailTemplate, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ICacheInvalidationService _cacheInvalidation;

    public UpdateEmailTemplateCommandHandler(
        IApplicationDbContext dbContext,
        IRepository<EmailTemplate, Guid> repository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ICacheInvalidationService cacheInvalidation)
    {
        _dbContext = dbContext;
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _cacheInvalidation = cacheInvalidation;
    }

    public async Task<Result<EmailTemplateDto>> Handle(
        UpdateEmailTemplateCommand command,
        CancellationToken cancellationToken)
    {
        var currentTenantId = _currentUser.TenantId;

        // Query ignoring tenant filter to access both tenant and platform templates
        var template = await _dbContext.EmailTemplates
            .IgnoreQueryFilters()
            .Where(t => t.Id == command.Id && !t.IsDeleted)
            .TagWith("EmailTemplateByIdForUpdate_CopyOnWrite")
            .FirstOrDefaultAsync(cancellationToken);

        if (template is null)
        {
            return Result.Failure<EmailTemplateDto>(
                Error.NotFound($"Email template with ID '{command.Id}' not found.", "NOIR-EMAIL-001"));
        }

        EmailTemplate resultTemplate;

        // Copy-on-Edit: If this is a platform template and user has tenant context, create a copy
        if (template.IsPlatformDefault && !string.IsNullOrEmpty(currentTenantId))
        {
            // Create a new tenant-specific override using copy-on-edit pattern
            var tenantCopy = EmailTemplate.CreateTenantOverride(
                currentTenantId,
                template.Name,
                command.Subject,
                command.HtmlBody,
                command.PlainTextBody,
                command.Description,
                template.AvailableVariables);

            await _repository.AddAsync(tenantCopy, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Invalidate cache for both platform and tenant template
            await _cacheInvalidation.InvalidateEmailTemplateCacheAsync(template.Name, null, cancellationToken);
            await _cacheInvalidation.InvalidateEmailTemplateCacheAsync(template.Name, currentTenantId, cancellationToken);

            resultTemplate = tenantCopy;
        }
        else
        {
            // Update existing template in place (tenant owns it or platform admin)
            // Need to attach for tracking since we used IgnoreQueryFilters
            _dbContext.Attach(template);

            template.Update(
                command.Subject,
                command.HtmlBody,
                command.PlainTextBody,
                command.Description,
                template.AvailableVariables); // Keep existing variables

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Invalidate cache for the updated template
            await _cacheInvalidation.InvalidateEmailTemplateCacheAsync(template.Name, template.TenantId, cancellationToken);

            resultTemplate = template;
        }

        // Return updated/new DTO
        // After save, it's never inherited (either COW created a copy, or tenant updated their own)
        var dto = new EmailTemplateDto(
            resultTemplate.Id,
            resultTemplate.Name,
            resultTemplate.Subject,
            resultTemplate.HtmlBody,
            resultTemplate.PlainTextBody,
            resultTemplate.IsActive,
            resultTemplate.Version,
            resultTemplate.Description,
            EmailTemplateHelpers.ParseAvailableVariables(resultTemplate.AvailableVariables),
            resultTemplate.CreatedAt,
            resultTemplate.ModifiedAt,
            IsInherited: false);

        return Result.Success(dto);
    }
}
