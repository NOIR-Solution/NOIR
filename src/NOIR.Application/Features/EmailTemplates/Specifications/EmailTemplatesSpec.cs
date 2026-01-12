namespace NOIR.Application.Features.EmailTemplates.Specifications;

/// <summary>
/// Specification to retrieve email templates with optional filtering by language and search term.
/// </summary>
public sealed class EmailTemplatesSpec : Specification<EmailTemplate>
{
    public EmailTemplatesSpec(string? language = null, string? search = null)
    {
        Query.Where(t => string.IsNullOrEmpty(language) || t.Language == language)
             .Where(t => string.IsNullOrEmpty(search) ||
                         t.Name.Contains(search) ||
                         (t.Subject != null && t.Subject.Contains(search)))
             .OrderBy(t => t.Name)
             .TagWith("EmailTemplates");
    }
}
