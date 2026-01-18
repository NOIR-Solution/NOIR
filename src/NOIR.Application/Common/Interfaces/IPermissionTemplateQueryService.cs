using NOIR.Application.Features.Permissions.Queries.GetPermissionTemplates;

namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Query service for permission templates.
/// Abstracts the data access for PermissionTemplate which is not an AggregateRoot.
/// </summary>
public interface IPermissionTemplateQueryService
{
    /// <summary>
    /// Gets all permission templates, optionally filtered by tenant.
    /// </summary>
    /// <param name="tenantId">Optional tenant ID to include tenant-specific templates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of permission template DTOs.</returns>
    Task<IReadOnlyList<PermissionTemplateDto>> GetAllAsync(Guid? tenantId, CancellationToken cancellationToken = default);
}
