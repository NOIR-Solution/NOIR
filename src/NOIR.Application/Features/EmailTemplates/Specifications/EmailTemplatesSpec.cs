namespace NOIR.Application.Features.EmailTemplates.Specifications;

/// <summary>
/// Specification to retrieve email templates with optional filtering by search term.
/// </summary>
public sealed class EmailTemplatesSpec : Specification<EmailTemplate>
{
    public EmailTemplatesSpec(string? search = null)
    {
        Query.Where(t => string.IsNullOrEmpty(search) ||
                         t.Name.Contains(search) ||
                         (t.Subject != null && t.Subject.Contains(search)))
             .OrderBy(t => t.Name)
             .TagWith("EmailTemplates");
    }
}
