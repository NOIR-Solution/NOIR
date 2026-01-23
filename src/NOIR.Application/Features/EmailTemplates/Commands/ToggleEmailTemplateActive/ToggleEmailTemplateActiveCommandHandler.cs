
namespace NOIR.Application.Features.EmailTemplates.Commands.ToggleEmailTemplateActive;

/// <summary>
/// Wolverine handler for toggling an email template's active status.
/// Implements Copy-on-Write: when a tenant toggles a platform template, a tenant-specific copy is created.
/// </summary>
public class ToggleEmailTemplateActiveCommandHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IRepository<EmailTemplate, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public ToggleEmailTemplateActiveCommandHandler(
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
        ToggleEmailTemplateActiveCommand command,
        CancellationToken cancellationToken)
    {
        var currentTenantId = _currentUser.TenantId;

        // Query ignoring tenant filter to access both tenant and platform templates
        var template = await _dbContext.EmailTemplates
            .IgnoreQueryFilters()
            .Where(t => t.Id == command.Id && !t.IsDeleted)
            .TagWith("EmailTemplateByIdForToggleActive")
            .FirstOrDefaultAsync(cancellationToken);

        if (template is null)
        {
            return Result.Failure<EmailTemplateDto>(
                Error.NotFound($"Email template with ID '{command.Id}' not found.", "NOIR-EMAIL-001"));
        }

        EmailTemplate resultTemplate;

        // Copy-on-Edit: If this is a platform template and user has tenant context
        if (template.IsPlatformDefault && !string.IsNullOrEmpty(currentTenantId))
        {
            // Check if tenant already has a copy of this template
            var existingTenantCopy = await _dbContext.EmailTemplates
                .IgnoreQueryFilters()
                .Where(t => t.Name == template.Name && t.TenantId == currentTenantId && !t.IsDeleted)
                .TagWith("EmailTemplateExistingTenantCopy")
                .FirstOrDefaultAsync(cancellationToken);

            if (existingTenantCopy != null)
            {
                // Tenant copy exists - toggle it
                _dbContext.Attach(existingTenantCopy);

                if (command.IsActive)
                    existingTenantCopy.Activate();
                else
                    existingTenantCopy.Deactivate();

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                resultTemplate = existingTenantCopy;
            }
            else
            {
                // Create a tenant-specific copy with the toggled state
                var tenantCopy = EmailTemplate.CreateTenantOverride(
                    currentTenantId,
                    template.Name,
                    template.Subject,
                    template.HtmlBody,
                    template.PlainTextBody,
                    template.Description,
                    template.AvailableVariables);

                if (!command.IsActive)
                    tenantCopy.Deactivate();

                await _repository.AddAsync(tenantCopy, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                resultTemplate = tenantCopy;
            }
        }
        else
        {
            // Toggle existing template in place (tenant owns it or platform admin)
            _dbContext.Attach(template);

            if (command.IsActive)
                template.Activate();
            else
                template.Deactivate();

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            resultTemplate = template;
        }

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
