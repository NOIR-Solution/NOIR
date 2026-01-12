namespace NOIR.Application.Features.EmailTemplates.Specifications;

/// <summary>
/// Specification to find an email template by its ID (read-only).
/// </summary>
public sealed class EmailTemplateByIdSpec : Specification<EmailTemplate>
{
    public EmailTemplateByIdSpec(Guid id)
    {
        Query.Where(t => t.Id == id)
             .TagWith("EmailTemplateById");
    }
}
