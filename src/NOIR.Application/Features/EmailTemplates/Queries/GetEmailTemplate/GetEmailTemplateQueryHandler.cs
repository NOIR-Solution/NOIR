namespace NOIR.Application.Features.EmailTemplates.Queries.GetEmailTemplate;

/// <summary>
/// Wolverine handler for getting a single email template by ID.
/// Supports Copy-on-Write pattern by indicating if template is inherited.
/// </summary>
public class GetEmailTemplateQueryHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetEmailTemplateQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<EmailTemplateDto>> Handle(
        GetEmailTemplateQuery query,
        CancellationToken cancellationToken)
    {
        // Query ignoring tenant filter to access both tenant and platform templates
        var template = await _dbContext.EmailTemplates
            .IgnoreQueryFilters()
            .Where(t => t.Id == query.Id && !t.IsDeleted)
            .TagWith("EmailTemplateById_CopyOnWrite")
            .FirstOrDefaultAsync(cancellationToken);

        if (template is null)
        {
            return Result.Failure<EmailTemplateDto>(
                Error.NotFound($"Email template with ID '{query.Id}' not found.", "NOIR-EMAIL-001"));
        }

        // Template is inherited if it's a platform template (TenantId = null)
        // and current user has a tenant context
        var isInherited = EmailTemplateHelpers.IsTemplateInherited(template.TenantId, _currentUser.TenantId);

        var dto = new EmailTemplateDto(
            template.Id,
            template.Name,
            template.Subject,
            template.HtmlBody,
            template.PlainTextBody,
            template.IsActive,
            template.Version,
            template.Description,
            EmailTemplateHelpers.ParseAvailableVariables(template.AvailableVariables),
            template.CreatedAt,
            template.ModifiedAt,
            isInherited);

        return Result.Success(dto);
    }
}
