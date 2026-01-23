namespace NOIR.Application.Features.LegalPages;

/// <summary>
/// Shared helper methods for legal page operations.
/// </summary>
public static class LegalPageHelpers
{
    /// <summary>
    /// Determines if a page should be considered "inherited" for a given user context.
    /// A page is inherited when it's a platform page (TenantId = null)
    /// and the current user has a tenant context.
    /// </summary>
    /// <param name="pageTenantId">The TenantId of the page being viewed.</param>
    /// <param name="currentUserTenantId">The TenantId of the current user.</param>
    /// <returns>True if the page is inherited from platform.</returns>
    public static bool IsPageInherited(string? pageTenantId, string? currentUserTenantId)
    {
        return pageTenantId == null && !string.IsNullOrEmpty(currentUserTenantId);
    }
}
