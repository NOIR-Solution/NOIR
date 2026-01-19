namespace NOIR.Application.Features.EmailTemplates;

/// <summary>
/// Shared helper methods for email template operations.
/// </summary>
public static class EmailTemplateHelpers
{
    /// <summary>
    /// Parses the JSON-serialized available variables string into a list.
    /// Returns an empty list if the string is null, empty, or invalid JSON.
    /// </summary>
    public static List<string> ParseAvailableVariables(string? availableVariables)
    {
        if (string.IsNullOrWhiteSpace(availableVariables))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(availableVariables) ?? [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Determines if a template should be considered "inherited" for a given user context.
    /// A template is inherited when it's a platform template (TenantId = null)
    /// and the current user has a tenant context.
    /// </summary>
    /// <param name="templateTenantId">The TenantId of the template being viewed.</param>
    /// <param name="currentUserTenantId">The TenantId of the current user.</param>
    /// <returns>True if the template is inherited from platform.</returns>
    public static bool IsTemplateInherited(string? templateTenantId, string? currentUserTenantId)
    {
        return templateTenantId == null && !string.IsNullOrEmpty(currentUserTenantId);
    }
}
