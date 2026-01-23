namespace NOIR.Application.Features.EmailTemplates.Specifications;

/// <summary>
/// Specification to find email templates by name for fallback resolution.
/// Ignores query filters (tenant and soft delete) to support the copy-on-write pattern:
/// 1. First looks for tenant-specific template
/// 2. Falls back to platform-level template (TenantId = null)
/// </summary>
public sealed class EmailTemplateByNameWithFallbackSpec : Specification<EmailTemplate>
{
    public EmailTemplateByNameWithFallbackSpec(string name)
    {
        Query.Where(t => t.Name == name && t.IsActive && !t.IsDeleted)
             .IgnoreQueryFilters() // Bypass tenant and soft delete filters for fallback logic
             .TagWith("EmailTemplateByNameWithFallback");
    }
}
