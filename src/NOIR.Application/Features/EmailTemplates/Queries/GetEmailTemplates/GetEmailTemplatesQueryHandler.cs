using Microsoft.EntityFrameworkCore;

namespace NOIR.Application.Features.EmailTemplates.Queries.GetEmailTemplates;

/// <summary>
/// Wolverine handler for getting list of email templates.
/// Implements Copy-on-Write pattern by showing:
/// - Tenant's own templates (TenantId = current tenant)
/// - Platform templates not yet customized (TenantId = null, no tenant override)
/// </summary>
public class GetEmailTemplatesQueryHandler
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public GetEmailTemplatesQueryHandler(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<Result<List<EmailTemplateListDto>>> Handle(
        GetEmailTemplatesQuery query,
        CancellationToken cancellationToken)
    {
        var currentTenantId = _currentUser.TenantId;

        // Query all templates ignoring tenant filter to get both tenant and platform templates
        var allTemplates = await _dbContext.EmailTemplates
            .IgnoreQueryFilters()
            .Where(t => !t.IsDeleted)
            .Where(t => string.IsNullOrEmpty(query.Search) ||
                        t.Name.Contains(query.Search) ||
                        (t.Subject != null && t.Subject.Contains(query.Search)))
            .OrderBy(t => t.Name)
            .TagWith("EmailTemplates_CopyOnWrite")
            .ToListAsync(cancellationToken);

        // Group by template name to handle inheritance
        var templatesByName = allTemplates.GroupBy(t => t.Name);

        var result = new List<EmailTemplateListDto>();

        foreach (var group in templatesByName)
        {
            // Check if tenant has customized this template
            var tenantTemplate = group.FirstOrDefault(t => t.TenantId == currentTenantId);
            var platformTemplate = group.FirstOrDefault(t => t.TenantId == null);

            if (tenantTemplate != null)
            {
                // Tenant has customized - show their version (not inherited)
                result.Add(new EmailTemplateListDto(
                    tenantTemplate.Id,
                    tenantTemplate.Name,
                    tenantTemplate.Subject,
                    tenantTemplate.IsActive,
                    tenantTemplate.Version,
                    tenantTemplate.Description,
                    EmailTemplateHelpers.ParseAvailableVariables(tenantTemplate.AvailableVariables),
                    IsInherited: false));
            }
            else if (platformTemplate != null)
            {
                // No tenant customization - show platform template as inherited
                // Use helper to determine inherited status based on user context
                var isInherited = EmailTemplateHelpers.IsTemplateInherited(
                    platformTemplate.TenantId,
                    currentTenantId);

                result.Add(new EmailTemplateListDto(
                    platformTemplate.Id,
                    platformTemplate.Name,
                    platformTemplate.Subject,
                    platformTemplate.IsActive,
                    platformTemplate.Version,
                    platformTemplate.Description,
                    EmailTemplateHelpers.ParseAvailableVariables(platformTemplate.AvailableVariables),
                    isInherited));
            }
        }

        return Result.Success(result.OrderBy(t => t.Name).ToList());
    }
}
