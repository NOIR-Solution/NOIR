namespace NOIR.Application.Features.EmailTemplates.Specifications;

/// <summary>
/// Specification to find an email template by its ID with tracking enabled for updates.
/// </summary>
public sealed class EmailTemplateByIdForUpdateSpec : Specification<EmailTemplate>
{
    public EmailTemplateByIdForUpdateSpec(Guid id)
    {
        Query.Where(t => t.Id == id)
             .AsTracking()
             .TagWith("EmailTemplateByIdForUpdate");
    }
}
