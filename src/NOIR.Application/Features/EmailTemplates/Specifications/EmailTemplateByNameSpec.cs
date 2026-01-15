namespace NOIR.Application.Features.EmailTemplates.Specifications;

/// <summary>
/// Specification to find an active email template by name.
/// </summary>
public sealed class EmailTemplateByNameSpec : Specification<EmailTemplate>
{
    public EmailTemplateByNameSpec(string name)
    {
        Query.Where(t => t.Name == name && t.IsActive)
             .TagWith("EmailTemplateByName");
    }
}
